#!/usr/bin/env bash
# Non-regression fixtures for verify-dotnet-warnings-baseline.sh (no full dotnet build).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
VERIFY="$SCRIPT_DIR/verify-dotnet-warnings-baseline.sh"
FIXTURES="$SCRIPT_DIR/fixtures"

echo "Fixture: clean log (expect exit 0)"
bash "$VERIFY" "$FIXTURES/dotnet-warnings-baseline-clean.log"

echo "Fixture: xUnit1030 log (expect exit 1)"
set +e
output=$(bash "$VERIFY" "$FIXTURES/dotnet-warnings-baseline-xunit1030.log" 2>&1)
status=$?
set -e
if (( status == 0 )); then
  echo "ERROR: expected non-zero exit for xUnit1030 fixture" >&2
  exit 1
fi
if ! grep -q 'xUnit1030' <<<"$output"; then
  echo "ERROR: xUnit1030 fixture output should mention xUnit1030" >&2
  echo "$output" >&2
  exit 1
fi
if grep -qE 'gated\): 0 \(baseline max 0\); build reported 1' <<<"$output"; then
  echo "ERROR: xUnit1030 must not be silently gated to 0 when build reported 1" >&2
  echo "$output" >&2
  exit 1
fi

echo "Fixtures OK"