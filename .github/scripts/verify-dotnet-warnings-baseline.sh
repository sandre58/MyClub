#!/usr/bin/env bash
# Progressive .NET warnings gate (phase 4 lot D1 / AUD-0005-C).
# Counts all MSBuild warning diagnostics in the build log against the progressive baseline.
# Usage: verify-dotnet-warnings-baseline.sh <path-to-build.log>
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <build.log>" >&2
  exit 2
fi

LOG="$1"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BASELINE="$ROOT/docs/audit/dotnet-warnings-baseline.json"
CLEANUP_LOG=""

if [[ ! -f "$LOG" ]]; then
  echo "Log not found: $LOG" >&2
  exit 1
fi

# Windows `dotnet build` logs are often UTF-16; CI (Linux) uses UTF-8.
if file -b "$LOG" 2>/dev/null | grep -q 'UTF-16'; then
  CLEANUP_LOG="$(mktemp)"
  if ! iconv -f UTF-16LE -t UTF-8 "$LOG" > "$CLEANUP_LOG" 2>/dev/null; then
    iconv -f UTF-16 -t UTF-8 "$LOG" > "$CLEANUP_LOG"
  fi
  LOG="$CLEANUP_LOG"
fi

trap '[[ -n "$CLEANUP_LOG" && -f "$CLEANUP_LOG" ]] && rm -f "$CLEANUP_LOG"' EXIT

if [[ ! -f "$BASELINE" ]]; then
  echo "Baseline not found: $BASELINE" >&2
  exit 1
fi

read -r MAX_TOTAL MAX_SA1143 MAX_SA1137 < <(
  node -e "
    const b = require(process.argv[1]);
    const c = b.byCode || {};
    console.log([b.totals.warnings, c.SA1143 || 0, c.SA1137 || 0].join(' '));
  " "$BASELINE"
)

SUMMARY_LINE=$(grep -E '(Avertissement|Warning\(s\))' "$LOG" | tail -1 || true)
if [[ -z "$SUMMARY_LINE" ]]; then
  echo "Could not find warning summary in build log" >&2
  exit 1
fi

BUILD_REPORTED_TOTAL=$(echo "$SUMMARY_LINE" | grep -oE '[0-9]+' | head -1)
if [[ -z "$BUILD_REPORTED_TOTAL" ]]; then
  BUILD_REPORTED_TOTAL=0
fi

# One line per diagnostic; MSBuild may duplicate — use unique physical lines.
mapfile -t UNIQUE_LINES < <(grep -E ': warning [A-Z][A-Z0-9]*[0-9]+:' "$LOG" | sort -u)
declare -A ACTUAL_BY_CODE
for line in "${UNIQUE_LINES[@]}"; do
  if [[ "$line" =~ warning[[:space:]]([A-Z][A-Z0-9]*[0-9]+): ]]; then
    code="${BASH_REMATCH[1]}"
    ACTUAL_BY_CODE["$code"]=$(( ${ACTUAL_BY_CODE["$code"]:-0} + 1 ))
  fi
done

ACTUAL_TOTAL=0
for code in "${!ACTUAL_BY_CODE[@]}"; do
  ACTUAL_TOTAL=$((ACTUAL_TOTAL + ACTUAL_BY_CODE[code]))
done

echo "dotnet warnings (gated): $ACTUAL_TOTAL (baseline max $MAX_TOTAL); build reported $BUILD_REPORTED_TOTAL"

if (( ACTUAL_TOTAL > MAX_TOTAL )); then
  echo "ERROR: warning total $ACTUAL_TOTAL exceeds baseline $MAX_TOTAL" >&2
  exit 1
fi

check_code() {
  local code=$1 max=$2
  local actual=${ACTUAL_BY_CODE[$code]:-0}
  if (( actual > max )); then
    echo "ERROR: $code count $actual exceeds baseline $max" >&2
    exit 1
  fi
}

check_code SA1143 "$MAX_SA1143"
check_code SA1137 "$MAX_SA1137"

for code in "${!ACTUAL_BY_CODE[@]}"; do
  case "$code" in
    SA1143|SA1137) ;;
    *)
      echo "ERROR: new warning code $code (${ACTUAL_BY_CODE[$code]} occurrences) not in baseline" >&2
      exit 1
      ;;
  esac
done

echo "dotnet warnings baseline OK"
