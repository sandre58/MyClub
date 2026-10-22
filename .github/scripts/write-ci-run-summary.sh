#!/usr/bin/env bash
# CI-UX-2: three-pillar workflow run summary (status + links; no detailed test output).
set -euo pipefail

: "${GITHUB_STEP_SUMMARY:?GITHUB_STEP_SUMMARY must be set}"
: "${RUN_URL:?RUN_URL must be set}"
: "${BUILD_RESULT:?BUILD_RESULT must be set}"
: "${WEB_RESULT:?WEB_RESULT must be set}"
: "${E2E_RESULT:?E2E_RESULT must be set}"

NET_JOB_NAME=".NET — Build and tests"
WEB_JOB_NAME="Web — Quality and unit tests"
E2E_JOB_NAME="E2E — Host and PostgreSQL"

format_result() {
  case "$1" in
    success) echo "success" ;;
    failure) echo "failure" ;;
    cancelled) echo "cancelled" ;;
    skipped) echo "skipped" ;;
    *) echo "${1:-unknown}" ;;
  esac
}

lookup_job_url() {
  local display_name="$1"
  if [[ -z "${JOBS_JSON:-}" ]]; then
    echo "$RUN_URL"
    return
  fi
  local url
  url="$(echo "$JOBS_JSON" | jq -r --arg n "$display_name" '.jobs[] | select(.name == $n) | .html_url' | head -n 1)"
  if [[ -z "$url" || "$url" == "null" ]]; then
    echo "$RUN_URL"
  else
    echo "$url"
  fi
}

JOBS_JSON=""
if [[ -n "${GITHUB_TOKEN:-}" && -n "${GITHUB_API_URL:-}" && -n "${GITHUB_REPOSITORY:-}" && -n "${GITHUB_RUN_ID:-}" ]]; then
  if JOBS_JSON="$(curl -fsSL \
    -H "Authorization: Bearer ${GITHUB_TOKEN}" \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "${GITHUB_API_URL}/repos/${GITHUB_REPOSITORY}/actions/runs/${GITHUB_RUN_ID}/jobs?per_page=100" 2>/dev/null)"; then
    :
  else
    JOBS_JSON=""
  fi
fi

NET_URL="$(lookup_job_url "$NET_JOB_NAME")"
WEB_URL="$(lookup_job_url "$WEB_JOB_NAME")"
E2E_URL="$(lookup_job_url "$E2E_JOB_NAME")"

{
  echo "## Play'Up CI — pillars"
  echo ""
  echo "Quick view of the three parallel gates. Details stay in each job (.NET Summary, Playwright, logs)."
  echo ""
  echo "| Pillar | Job | Result |"
  echo "| --- | --- | --- |"
  echo "| .NET | [${NET_JOB_NAME}](${NET_URL}) | $(format_result "$BUILD_RESULT") |"
  echo "| Web | [${WEB_JOB_NAME}](${WEB_URL}) | $(format_result "$WEB_RESULT") |"
  echo "| E2E | [${E2E_JOB_NAME}](${E2E_URL}) | $(format_result "$E2E_RESULT") |"
  echo ""
  echo "[Full workflow run](${RUN_URL})"
} >> "$GITHUB_STEP_SUMMARY"
