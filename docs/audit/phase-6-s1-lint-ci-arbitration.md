# Phase 6 — S1 — Lint CI arbitration (decided 2026-10-22)

**Goal:** Separate **code quality gates** from **exploratory audit scripts**; avoid redundant or heuristic tools blocking CI by default.

## Roles

| Layer | Tool / script | Scope | Blocks CI? |
| --- | --- | --- | --- |
| **Web — JS/TS product** | Oxlint (`npm run lint`, `--deny-warnings`) | `src/**` and tooling under lint path (incl. `scripts/*.mjs` invoked by repo layout) | **Yes** (`.github/workflows/ci.yml` web job) |
| **Web — CSS product** | Stylelint (`npm run lint:css`) | `src/**/*.css` (policy per README) | **Yes** |
| **Web — types** | `tsc` via `typecheck` / `build` | App + node tsconfigs | **Yes** |
| **Web — format** | Prettier `format:check` | Package tree | **Yes** |
| **Web — i18n parity** | `i18n:check` (Vitest) | Locale parity contract | **No** in CI today (recommended keep report-only unless product asks) |
| **Structure-scoped audits** | `css:audit`, `i18n:audit`, `tile:check` | Structure fiche / tiles | **No** — local / recette |
| **Repo-wide heuristics (F1bis)** | `frontend:audit`, `i18n:audit:repo`, `css:audit:rider` | Investigation, Rider hygiene | **No** — report-only; **never** default CI gate |

**.NET:** `dotnet build` + progressive warnings baseline (`verify-dotnet-warnings-baseline.sh`) + `dotnet test` — unchanged (phase 4 D1).

**Vite:** chunk size warning remains **informational** (build exit 0); treat under S1 as debt sizing, not a new gate without arbitration.

## Decision

1. **CI web lint = Oxlint on product + scripts that live in the web package** (including audit helpers). Fix real lint issues in scripts; do not weaken `--deny-warnings` for heuristics.
2. **Heuristic audits stay out of CI** until a stream (S2–S4) produces a **scoped, proven** rule with validation criteria (per phase 6 plan).
3. **Wave 1 red `npm run lint`:** three `no-useless-escape` in `css-audit-rider.mjs` — **fixed** (targeted regex / `includes('[')`), not a script redesign.

## Evidence

- Wave 1: [`phase-6-wave-1-static-baseline.md`](phase-6-wave-1-static-baseline.md)
- Contributor table: `src/PlayUp/web/docs/conventions.md` (audit scripts §)

## Follow-up (not S1)

- Optional: oxlint `ignorePatterns` for `scripts/` only if we later split “product lint” from “node tooling” — **not** chosen; scripts must stay clean under current CI contract.
