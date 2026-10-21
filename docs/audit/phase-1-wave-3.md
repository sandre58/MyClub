# Phase 1 — Wave 3: Frontend, CSS, i18n, dead-code candidates

**Launched:** 2026-10-21  
**Closed:** 2026-10-21  
**Branch:** `feature/new_version`  
**SHA vague:** `93b09f161739f47b768caad63aed90d0d3c1469f`  
**Comparison to `main`:** not performed (per SHA arbitration).

No code, CSS, i18n, script, or Cursor-rule changes in this wave — proof-based audit only.

## Scope

| Area | Method | Register |
| --- | --- | --- |
| npm audit scripts (AUD-0001) | Git history, `package.json`, docs grep, `.github` | **Extend AUD-0001** |
| Stylelint duplicate selector (AUD-0003) | CSS cascade analysis + DOM usage | **Extend AUD-0003** |
| Structure fiche CSS candidates | `scripts/audit-structure-fiche-css.mjs` + manual grep | **AUD-0013** |
| Structure locale keys candidates | `scripts/audit-structure-i18n.mjs` + repo grep | **AUD-0014** |
| i18n parity | `npm run i18n:check` | (no new finding — passes) |
| TypeScript / build boundary | `typecheck`, `typecheck:tests`, `tsc -b` | **AUD-0002** (wave 1; extended evidence) |
| Global CSS / DS / repo-wide dead code | Not run (global `frontend-audit.mjs` absent) | Deferred to post-AUD-0001 remediation |

## 1. AUD-0001 — npm scripts vs filesystem (extended)

### What is missing vs replaced

| Script | Declared path | On disk @ SHA | Git narrative |
| --- | --- | --- | --- |
| `css:audit` | `scripts/frontend-audit.mjs` | **Absent** | Removed in `0e60554a` (*chore: clean*, 2026-10-12) with `frontend-audit-ts.mjs`, split helpers, etc. |
| `i18n:audit` | `scripts/i18n-key-audit.mjs` | **Absent** | Added `d6a03155` (*chore: audit*), removed `0e60554a` |
| `tile:check` | `scripts/tile-family-check.mjs` | **Absent** | Removed `0e60554a` (introduced earlier e.g. `d416d510` tile unification) |

**Partial replacement (not wired in `package.json`):**

- `scripts/audit-structure-fiche-css.mjs` — heuristic unused-class scan for `structure-fiche.css` only.
- `scripts/audit-structure-i18n.mjs` — suspect leaf keys in `locales/en/structure.json` only.

Only these two files exist under `src/PlayUp/web/scripts/` on the execution SHA.

### Documentation drift (obsolete commands)

| Location | Reference |
| --- | --- |
| `src/PlayUp/web/package.json` | `css:audit`, `i18n:audit`, `tile:check` → missing files |
| `src/PlayUp/web/docs/page-migration.md` | P6: `npm run css:audit` / `frontend-audit.mjs` |
| `src/PlayUp/web/docs/conventions.md` | `tile:check`, `css:audit` |
| `src/PlayUp/web/docs/design-system.md` | CI guard `npm run tile:check` |
| `docs/audit/baseline.md`, `phase-1-wave-1.md` | Already document breakage |

**CI:** `.github/workflows/ci.yml` does **not** invoke `css:audit`, `i18n:audit`, or `tile:check` — impact is contributor/docs/tooling, not a failing CI step for those scripts.

### Reproducible proof

```text
cd src/PlayUp/web
npm run css:audit    # Error: Cannot find module ... frontend-audit.mjs
git show 0e60554a --stat -- src/PlayUp/web/scripts/
```

**Confidence:** High for “scripts removed, npm entries and docs not updated”.  
**False positives:** None for missing files; scoped `audit-structure-*` scripts are intentional partial substitutes, not drop-in replacements for global audits.

**Treatment options (no choice made):** restore global scripts; repoint npm scripts to scoped tools + document limits; remove npm entries and rewrite docs; add CI only after scope is agreed.

---

## 2. AUD-0003 — duplicate `.structure-fiche__header-lifecycle-toolbar` (extended)

### Proof

```text
cd src/PlayUp/web && npm run lint:css
# structure-fiche.css 238:1  no-duplicate-selectors (first use ~124)
```

### CSS blocks

- **L124–130:** `display`, `flex-wrap`, `align-items`, **`justify-content: flex-end`**, `gap`.
- **L132–138:** nested rule for `.structure-operational-strip__section-actions-end` inside the toolbar (only attached to the first block in source order).
- **L238–243:** same selector again — repeats `display`, `flex-wrap`, `align-items`, `gap`; **does not** redeclare `justify-content`.

### Cascade impact (not assumed “merge blindly”)

For elements matching `.structure-fiche__header-lifecycle-toolbar`:

- `justify-content: flex-end` from the **first** rule remains in effect (second rule does not reset it).
- `gap` is duplicated with the same token (`var(--space-4)` in both blocks today) — redundant, not conflicting.
- Risk is **maintainability** and **future drift** (edit one block only), plus stylelint/CI failure — not a proven current visual regression from dropped `justify-content`.

### DOM usage

Class used in `StructurePhaseMatchsLifecycleBlock.tsx` (parts `header` and `actions`) inside `structure-fiche__header-lifecycle-panel`. Header layout elsewhere uses `structure-fiche__header-row-end`, not `structure-fiche__header-end`.

**Confidence:** High for lint failure and duplicate selector; **medium** for user-visible impact.  
**False positives:** Treating naive merge as zero-risk without visual/regression check.

**Treatment options:** consolidate into one rule block; split modifiers if two layouts were intended; adjust stylelint config only if policy changes (out of audit scope).

---

## 3. AUD-0013 — structure-fiche CSS class candidates (orphan)

**Heuristic:** `node scripts/audit-structure-fiche-css.mjs` @ SHA → **4 / 122** unique class tokens with no string match under `src/` (excluding `structure-fiche.css`).

| Class | Manual check @ SHA |
| --- | --- |
| `structure-fiche__header-end` | Not in any `.tsx`; header uses `structure-fiche__header-row-end` |
| `structure-fiche__header-remediation` | CSS only |
| `structure-fiche__title-row` | CSS only |
| `structure-fiche__title-row--primary` | CSS only |

Script allowlists dynamic stems (`structure-phase-draw-hint--`, etc.) — these four are not covered.

**Qualification:** Candidate dead CSS after header refactor (aligns with P6 narrative in `page-migration.md`).  
**Severity:** P3 — Opportunity (bytes + confusion, no proven runtime break).  
**Confidence:** Medium–high for “no static reference”; low for “safe to delete” without screenshot/regression pass.  
**False positives:** Rare dynamic class assembly, E2E selectors, future branch not merged.

**Treatment options:** confirm in browser + tests, then remove in authorized CSS lot; or document as reserved for upcoming UI.

---

## 4. AUD-0014 — structure.json leaf keys candidates (orphan)

**Heuristic:** `node scripts/audit-structure-i18n.mjs` @ SHA → **6 suspect / 989** leaf keys in `locales/en/structure.json`.

| Key | Repo grep (`src/`, en+fr JSON) |
| --- | --- |
| `fiche.binding.general` | No TS/TSX reference |
| `fiche.binding.personalized` | No TS/TSX reference |
| `composition.tile.strip.drawNotApplied` | JSON only |
| `composition.tile.strip.drawHistoricalApplied` | JSON only |
| `composition.tile.strip.drawHistoricalNotApplied` | JSON only |
| `composition.tile.strip.drawHistoricalUnavailable` | JSON only |

Other `composition.tile.*` keys **are** used (e.g. `StructurePhaseCompositionBand.tsx`, `structurePhaseEntriesRail.tsx`). Suspect strip keys sit beside used composition copy — likely leftover from UI removal or never wired.

**Parity:** `npm run i18n:check` — **16/16 tests pass** (en/fr structure parity includes these keys). No missing-key signal; this is **unused-key** candidacy only.

**Confidence:** Medium (script corpus is TS/TSX under `src/` only; no backend string scan).  
**False positives:** Planned copy, Storybook not in tree, dynamic `t()` patterns not matching heuristic.

**Treatment options:** product confirm then remove keys; wire UI if still required; extend audit script corpus if backend emits codes mapped to these keys.

---

## 5. Frontend typing and build (AUD-0002 extension)

No new register ID — same mechanism as wave 1.

| Command | Includes tests? | Result @ SHA |
| --- | --- | --- |
| `npm run typecheck` | No (`tsconfig.app` + `node`) | **Pass** |
| `npm run typecheck:tests` | Yes (`tsconfig.vitest.json`) | **Fail** (same family of errors as build) |
| `npm run build` (`tsc -b`) | Yes (root references vitest project) | **Fail** — **34** TS errors |
| `npm run test:run` | N/A | **Pass** (1132 tests, phase 0 log) |

Dominant failure: mocks omit required `StructureReadiness.blockerDetails` after type extension (`src/types/structure.ts`). Additional drift: `StructureMaterialize.confrontations`, `DematerializeDisabledReason`, `StructureFormatSummary` fields, hero stat key `"matches"`.

**E2E:** `test:e2e` runs `npm run build` first — blocked on this branch until AUD-0002 is addressed or build graph split is decided.

**Treatment options:** sync fixtures; exclude tests from `tsc -b` production build; dedicated `typecheck:tests` in CI — architectural choice, not applied in audit.

---

## 6. Legacy / dead-code survey (limited)

- **No** repo-wide dead-file or unused-export scan (would duplicate missing `frontend-audit.mjs`).
- **Deprecated API** `structureDiagnosticPresentationPolicy.ts` (`@deprecated` exports) — still referenced from multiple structure/shell modules → **not** a deletion candidate on evidence alone.
- **Legacy data paths** (e.g. `destinationSlotKey` coercion) — intentional compatibility, covered by tests; not catalogued as dead code.

Wave 5 / post-remediation global audit may widen scope once AUD-0001 is resolved.

---

## 7. Register summary (wave 3)

| ID | Action |
| --- | --- |
| AUD-0001 | Extended — git removal commit, doc map, CI negative finding |
| AUD-0003 | Extended — cascade + DOM + merge risk |
| AUD-0002 | Extended in this journal only (wave 1 entry remains) |
| AUD-0013 | New — structure-fiche orphan CSS classes |
| AUD-0014 | New — structure.json orphan leaf keys |

**Next free reference:** AUD-0015.

## Sequence

Per plan directeur: **wave 4** (critical path coverage) → **wave 5** (security, CI, Cursor governance). AUD-0002 / AUD-0004 may drive architecture decisions but do not block wave 4 audit work.
