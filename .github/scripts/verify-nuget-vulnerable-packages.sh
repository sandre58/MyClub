#!/usr/bin/env bash
# Fail when a NuGet advisory is reported for the solution or project.
#
# `dotnet list package --vulnerable` on SDK 11.0.100-rc.2 exits 0 when advisories
# are present, and the console report is localized, so the raw command does not
# fail CI. This script requests the documented machine-readable report
# (`--format json --output-version 1`) of the same `--vulnerable --include-transitive`
# query and fails unless that report is present, is version 1, and contains no
# advisory and no NuGet problem. Feed names (`sources`) and projects with no
# vulnerability list are informational and do not fail the audit.
set -euo pipefail

if [[ $# -ne 1 || -z "${1}" ]]; then
  echo "Usage: $0 <solution-or-project>" >&2
  exit 2
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "ERROR: python3 is required to read the NuGet JSON report." >&2
  exit 1
fi

target="$1"
stdout_file="$(mktemp)"
stderr_file="$(mktemp)"
trap 'rm -f "$stdout_file" "$stderr_file"' EXIT

set +e
dotnet list "$target" package --vulnerable --include-transitive --format json --output-version 1 >"$stdout_file" 2>"$stderr_file"
cmd_exit=$?
set -e

if [[ -s "$stderr_file" ]]; then
  cat "$stderr_file" >&2
fi
if [[ -s "$stdout_file" ]]; then
  cat "$stdout_file"
fi

if [[ "$cmd_exit" -ne 0 ]]; then
  echo "ERROR: dotnet list package --vulnerable exited ${cmd_exit}. The audit did not complete; this is not a clean report." >&2
  exit 1
fi

if [[ ! -s "$stdout_file" ]]; then
  echo "ERROR: dotnet list package --vulnerable produced no report. The result is not interpretable." >&2
  exit 1
fi

python3 - "$stdout_file" <<'PY'
import json
import sys

path = sys.argv[1]
raw = open(path, encoding="utf-8-sig").read()
if not raw.strip():
    print("ERROR: NuGet report is empty. The result is not interpretable.", file=sys.stderr)
    sys.exit(1)

try:
    document = json.loads(raw)
except json.JSONDecodeError as error:
    print(f"ERROR: NuGet report is not valid JSON ({error}). The result is not interpretable.", file=sys.stderr)
    sys.exit(1)

def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    sys.exit(1)

if not isinstance(document, dict):
    fail("NuGet report is not a JSON object. The result is not interpretable.")

version = document.get("version")
if version != 1:
    fail(f"NuGet report version is {version!r}; expected 1. The result is not interpretable.")

sources = document.get("sources")
if not isinstance(sources, list) or len(sources) == 0:
    fail("NuGet report has no vulnerability sources. The result is not interpretable.")

problems = document.get("problems")
if problems is not None:
    if not isinstance(problems, list):
        fail("NuGet report field 'problems' is not a list. The result is not interpretable.")
    if problems:
        print("ERROR: NuGet reported problems while building the vulnerability report:", file=sys.stderr)
        for problem in problems:
            print(f"  - {problem}", file=sys.stderr)
        sys.exit(1)

projects = document.get("projects")
if not isinstance(projects, list) or len(projects) == 0:
    fail("NuGet report has no projects. The result is not interpretable.")

findings = []

def read_packages(packages, kind: str):
    if packages is None:
        return []
    if not isinstance(packages, list):
        fail(f"NuGet report field '{kind}' is not a list. The result is not interpretable.")
    return packages

for project in projects:
    if not isinstance(project, dict):
        fail("NuGet report project entry is not an object. The result is not interpretable.")
    project_path = project.get("path", "")
    frameworks = project.get("frameworks")
    if frameworks is None:
        continue
    if not isinstance(frameworks, list):
        fail("NuGet report field 'frameworks' is not a list. The result is not interpretable.")
    for framework in frameworks:
        if not isinstance(framework, dict):
            fail("NuGet report framework entry is not an object. The result is not interpretable.")
        framework_name = framework.get("framework", "")
        for kind in ("topLevelPackages", "transitivePackages"):
            for package in read_packages(framework.get(kind), kind):
                if not isinstance(package, dict):
                    fail(f"NuGet report {kind} entry is not an object. The result is not interpretable.")
                vulnerabilities = package.get("vulnerabilities")
                if vulnerabilities is None:
                    continue
                if not isinstance(vulnerabilities, list):
                    fail("NuGet report field 'vulnerabilities' is not a list. The result is not interpretable.")
                for vulnerability in vulnerabilities:
                    if not isinstance(vulnerability, dict):
                        fail("NuGet report vulnerability entry is not an object. The result is not interpretable.")
                    findings.append(
                        {
                            "id": package.get("id", ""),
                            "resolved": package.get("resolvedVersion", ""),
                            "severity": vulnerability.get("severity") or "unspecified",
                            "advisory": vulnerability.get("advisoryurl") or vulnerability.get("advisoryUrl") or "",
                            "project": project_path,
                            "framework": framework_name,
                            "kind": kind,
                        }
                    )

if findings:
    print(f"ERROR: {len(findings)} NuGet advisory(ies) on the audited dependency graph:", file=sys.stderr)
    for finding in findings:
        print(
            "  - {id} {resolved} [{severity}] {advisory} ({kind}, {project}, {framework})".format(**finding),
            file=sys.stderr,
        )
    sys.exit(1)

print("NuGet vulnerable package audit: no advisories.")
PY
