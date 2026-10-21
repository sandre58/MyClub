#!/usr/bin/env bash
set -euo pipefail

slnx="${1:-MyClub.slnx}"
diag_dir="${RUNNER_TEMP:-/tmp}/myclub-restore-diag"
mkdir -p "$diag_dir"

echo "=== dotnet --info ==="
dotnet --info | tee "$diag_dir/dotnet-info.txt"

echo "=== global.json ==="
cat global.json | tee "$diag_dir/global.json.txt"

echo "=== dotnet --list-sdks ==="
dotnet --list-sdks | tee "$diag_dir/sdks.txt"

echo "=== restore ($slnx, verbosity normal) ==="
set +e
dotnet restore "$slnx" --force-evaluate -v:n 2>&1 | tee "$diag_dir/restore-normal.log"
restore_exit=${PIPESTATUS[0]}
set -e

if [[ "$restore_exit" -eq 0 ]]; then
  exit 0
fi

echo "=== restore FAILED (exit $restore_exit) — MSBuild diagnostic ==="
set +e
dotnet restore "$slnx" -v:diag >"$diag_dir/restore-diag.log" 2>&1
set -e

echo "=== last 500 lines of diagnostic log ==="
tail -n 500 "$diag_dir/restore-diag.log"

echo "=== grep errors / NuGet codes ==="
grep -E '(^|\s)(error|warning) [A-Z]+[0-9]+|NU[0-9]{4}|MSB[0-9]{4}' "$diag_dir/restore-diag.log" | tail -n 100 || true

echo "Diagnostic files written under: $diag_dir"
exit "$restore_exit"
