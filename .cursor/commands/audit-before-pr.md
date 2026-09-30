# Audit before PR

Assist preparing a pull request. This is **not** a second CI — recommend the right local checks; do not claim to replace GitHub Actions.

## Steps

1. Inspect the current diff (staged + unstaged) and list impacted areas (Domain / Application / Infrastructure / Host / TestKit / Development / web / docs / migrations).
2. Check architectural boundaries against `architecture.mdc` (Host ↛ Development/TestKit; TestKit ↛ Infra; etc.). Flag violations; do not “fix” silently unless asked.
3. Check source comments against `documentation.mdc` (English; no Notion/history/V1 framing).
4. If Play’Up web is touched: remind data path (`api.ts`, `queryKeys.ts`, TanStack Query) and suggest:
   `cd src/PlayUp/web && npm run format:check && npm run lint && npm run typecheck && npm run test:run`
5. If .NET is touched, suggest:
   - `dotnet build`
   - `dotnet test --filter Category!=Integration` (full `dotnet test` when Docker is available)
6. If Host HTTP contracts or SPA types change: point at `docs/guides/http-api-contract.md` and flag contract drift.
7. If EF migrations are added/changed: point at `docs/guides/local-persistence.md` and note migration review.
8. Tests: are behaviour/regression tests present or missing for the change? Prefer TestKit for Domain/Application situations.
9. Summarize: ready / not ready, with a short checklist and exact commands to run. If asked to create or draft a PR: **English**, professional title and body; fill `.github/PULL_REQUEST_TEMPLATE.md`.

## Do not

- Re-run the entire CI matrix as a substitute for GitHub.
- Lecture on Conventional Commits or clean code — conventions live in CONTRIBUTING / `.gitmessage`.
- Expand scope beyond the diff.
