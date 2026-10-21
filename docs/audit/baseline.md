# Technical audit — baseline (phase 0)

**Status:** Phase 0 measurement snapshot (no application code changed).  
**Captured:** 2026-10-21 (local workspace, Windows).  
**Full command output:** `docs/audit/_build-release.log`, `docs/audit/_test-release.log`, `docs/audit/_web-controls.log` (copy of the phase 0 run).

## Git reference

| Role | Value |
| --- | --- |
| **Chantier SHA (`main`, arbitré)** | `24075eaff1b934cb63dfea9fd5c6d05277ccb56c` (2025-07-11 — *refacto*) |
| **SHA d’exécution baseline** | `93b09f161739f47b768caad63aed90d0d3c1469f` on branch `feature/new_version` (2026-10-21 — *chore: clean warnings*) |
| **Écart** | `main` is **577 commits behind** the execution SHA. All measurements below apply to the **execution SHA** unless noted. |

## Controls matrix

Legend: **CI** = `.github/workflows/ci.yml` · **Retenu** = target for “zero warning” policy (phase 2) — empty = not decided yet.

| Control | Command / location | Local (2026-10-21) | CI | Retenu |
| --- | --- | --- | --- | --- |
| .NET restore | `dotnet restore` | OK; NU1608 on multiple projects | yes | TBD |
| .NET build | `dotnet build -c Release` | OK; **18× NU1608** (Npgsql EF vs EF Core rc mismatch); **0** CS/SA/IDE build warnings in log | yes | TBD |
| .NET test (full) | `dotnet test -c Release --no-build` | **1 failed** / 2195+ passed (see below) | yes | TBD |
| Host publish isolation | `dotnet publish` Host + check DLLs | OK; Dev/DevRunner/TestKit **not** in output | yes | TBD |
| Web format | `npm run format:check` | **FAIL** — 447 files need Prettier | yes | TBD |
| Web lint | `npm run lint` (oxlint `--deny-warnings`) | **FAIL** — 15 warnings | yes | TBD |
| Web CSS lint | `npm run lint:css` | **FAIL** — 1 error (`structure-fiche.css` duplicate selector) | yes | TBD |
| Web typecheck (app) | `npm run typecheck` | **OK** (app + node tsconfigs) | yes | TBD |
| Web build | `npm run build` (`tsc -b` + vite) | **FAIL** — TS errors in test files (`blockerDetails`, etc.) | yes | TBD |
| Web unit tests | `npm run test:run` | **OK** — 141 files, 1132 tests | yes | TBD |
| Web i18n parity | `npm run i18n:check` | **OK** — 16 tests | no | TBD |
| Web i18n key audit | `npm run i18n:audit` | **N/A** — script missing (`scripts/i18n-key-audit.mjs`) | no | TBD |
| Web CSS audit | `npm run css:audit` | **N/A** — script missing (`scripts/frontend-audit.mjs`) | no | TBD |
| Web tile check | `npm run tile:check` | **N/A** — script missing (`scripts/tile-family-check.mjs`) | no | TBD |
| Playwright smoke | `npx playwright test` | **Not run** in phase 0 (CI runs on Ubuntu) | yes | TBD |
| Architecture deps | `AssemblyDependencyTests` | Included in Infrastructure.Tests (passing) | via `dotnet test` | TBD |
| Host/TestKit/Dev isolation | `*ArchitectureIsolationTests` | Included in test run (passing) | via `dotnet test` | TBD |
| Structure CSS audit (alt) | `scripts/audit-structure-fiche-css.mjs` | Present; not executed in this pass | no | TBD |
| Structure i18n audit (alt) | `scripts/audit-structure-i18n.mjs` | Present; not executed in this pass | no | TBD |

### .NET test summary (execution SHA)

| Assembly | Result |
| --- | --- |
| Domain, Application, Infrastructure, Host, Media, TestKit, DevRunner | All green |
| **Development.Tests** | **1 failure:** `CupC2ImpasseRecetteProtocolTests.C2_manual_recette_chain_documents_backend_and_read_modelAsync` — expected blocker `StartBlockedUnmaterialized`, got `StartBlockedIncompleteEntryRound` |

### Web control notes

- **CI vs local:** CI uses Node 22 and `npm ci`; local run used existing `node_modules` (Node v24.18.0). npm emitted `Unknown env config "devdir"` (environment).
- **package.json vs repo:** `i18n:audit`, `css:audit`, and `tile:check` point to scripts that are **not** in the tree (only `audit-structure-*.mjs` exist). Docs (`page-migration.md`, `conventions.md`) still reference `css:audit` / `frontend-audit.mjs` — drift to track in phase 1.
- **typecheck vs build:** `npm run typecheck` does not typecheck Vitest test projects; `npm run build` uses `tsc -b` and **fails** on outdated test fixtures.

## Analyzer suppressions (`NoWarn`)

Global (`build/code-analysis.props`): `SYSLIB0013`, `SYSLIB1054`, `NU5104`, `NU5017`, `NETSDK1206`, `NETSDK1057`.

`Directory.build.props`: `IDE0005`.

Per-project (representative):

| Project | Additional `NoWarn` |
| --- | --- |
| Host | CA1515, CA2007 |
| DevRunner | CA2007, CA1303, CA1515, SA0001 |
| Development | CA2007 |
| Host.Tests | CA1515, CA2007, CA1031, CA2234, SA1204 |
| Infrastructure.Tests, Media.Infrastructure.Tests | CA1515, CA2007, CA1031 |
| Application.Tests, Development.Tests, DevRunner.Tests, Media.Application.Tests | CA2007 |

Analyzers enabled: `Microsoft.CodeAnalysis.NetAnalyzers`, StyleCop, Roslynator (`AnalysisMode=AllEnabledByDefault`, `EnforceCodeStyleInBuild=true`). **No `TreatWarningsAsErrors`** in shared props.

## Dead-code / hygiene tooling (inventory)

| Area | Tool | State |
| --- | --- | --- |
| CSS (documented) | `npm run css:audit` | **Broken** — missing `frontend-audit.mjs` |
| CSS (structure) | `audit-structure-fiche-css.mjs` | Present |
| i18n keys (documented) | `npm run i18n:audit` | **Broken** — missing `i18n-key-audit.mjs` |
| i18n (structure) | `audit-structure-i18n.mjs` | Present |
| i18n parity | `localeParity.test.ts` | **Works** |
| Tiles | `tile:check` | **Broken** — missing script |
| C# dead code | No dedicated CLI in repo | Phase 1 — IDE / analyzer / manual |
| NuGet audit | No `dotnet list package --vulnerable` in CI | Phase 1 |

## Cursor rules inventory

See [`cursor-rules-inventory.md`](./cursor-rules-inventory.md) (22 rule files under `.cursor/rules/`).

## Critical paths

See [`critical-paths.md`](./critical-paths.md) (8 scenarios + test coverage snapshot).

## Phase 1 SHA policy (2026-10-21)

| Reference | SHA | Role |
| --- | --- | --- |
| `main` | `24075eaf…` | Phase 0 historical anchor — do not attribute branch-only issues to `main` without comparison |
| `feature/new_version` | `93b09f16…` | Primary audit branch (wave 1+) |
| Post-merge | TBD | New anchor only after explicit decision; re-evaluate dependent findings |

Journals: `docs/audit/phase-1-wave-1.md` · `phase-1-wave-2.md` · Notion `AUD-0001`…

## Gaps for phase 2 (policy, not findings)

- Decide **Retenu** column and whether missing scripts are restored or package.json/docs are corrected.
- NU1608 / Npgsql–EF alignment.
- Prettier drift (447 files) — intentional backlog vs CI failure.
- Whether baseline execution SHA should track `main` after merge or stay branch-based during active development.
