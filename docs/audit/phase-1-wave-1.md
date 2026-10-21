# Phase 1 — Wave 1: Static quality and reproducibility

**Launched:** 2026-10-21  
**Branch:** `feature/new_version`  
**SHA vague:** `93b09f161739f47b768caad63aed90d0d3c1469f`  
**Historical reference (phase 0):** `main` @ `24075eaff1b934cb63dfea9fd5c6d05277ccb56c` — not re-measured in this wave unless noted.

No application fixes in this wave — investigation and register entries only.

## Scope

Qualify phase 0 signals: web build, .NET recette test, CSS lint, NU1608, oxlint, Prettier check, missing npm scripts, CI alignment.

## Confirmed mechanisms (not yet “fix authorized”)

### Web `npm run build` vs `npm run typecheck`

- Root `tsconfig.json` references `tsconfig.vitest.json`, which **includes** `src/**/*.test.ts(x)`.
- `npm run build` runs `tsc -b` → typechecks **test sources** as part of the build graph.
- `npm run typecheck` runs only `tsconfig.app.json` + `tsconfig.node.json` → **excludes** tests.
- Failures are TS errors in test fixtures (e.g. `StructureReadiness.blockerDetails` missing on mocks), not in production `src` excluded paths.
- **CI** runs both `typecheck` and `build` (`.github/workflows/ci.yml`) → pipeline can pass typecheck and still fail build, or fail build on this branch.

### npm scripts vs filesystem

| Script | Declared path | On disk (execution SHA) |
| --- | --- | --- |
| `css:audit` | `scripts/frontend-audit.mjs` | **Missing** |
| `i18n:audit` | `scripts/i18n-key-audit.mjs` | **Missing** |
| `tile:check` | `scripts/tile-family-check.mjs` | **Missing** |
| (alternate) | `scripts/audit-structure-fiche-css.mjs` | Present |
| (alternate) | `scripts/audit-structure-i18n.mjs` | Present |

Docs still mention `css:audit` / `frontend-audit.mjs` (e.g. `src/PlayUp/web/docs/page-migration.md`, `conventions.md`).

### Stylelint — duplicate selector

`structure-fiche.css`: `.structure-fiche__header-lifecycle-toolbar` at ~L124 and ~L238. Second block repeats the selector with a **subset** of properties (`gap` only; first block also sets `justify-content: flex-end`). Reproducible: `npm run lint:css`.

### `CupC2ImpasseRecetteProtocolTests`

After slot assignment and before second materialize, test expects `StartBlockedUnmaterialized` (L80–81). Observed: `StartBlockedIncompleteEntryRound`. Later steps in the same test expect `IncompleteEntryRound` after second materialize — first assertion may be **out of date** relative to domain/lifecycle ordering. Requires contract review (Notion C2 recette vs code), not a blind assertion update.

### NU1608

18 restore/build warnings: `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0-rc.1.1 requires EF Core **=** 11.0.0-rc.1.26425.128; solution resolves EF **11.0.0-rc.2.26504.105**. Affects Host, Infrastructure, Media.Infrastructure, multiple test projects. Compatibility at runtime not proven by warning text alone.

### Oxlint (15 warnings)

All on execution SHA with `npm run lint` — see `docs/audit/_web-controls.log` (lines ~460–475): mostly `react(only-export-components)` and two `react-hooks/exhaustive-deps)`, one `no-unused-vars`.

### Prettier `format:check`

447 files reported with style drift vs Prettier config — command output lists each path; treat as “files not matching formatter”, not necessarily 447 independent defects.

## `main` comparison

Not executed in wave 1. Constats above are **observed on `feature/new_version` @ 93b09f16…** only. Cross-branch attribution deferred to explicit comparison or post-merge SHA.

## Register

Findings entered in Notion with `Référence audit` AUD-0001… (wave 1, static quality).
