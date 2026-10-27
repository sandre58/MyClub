# Conventions — Play’Up web

Source of truth for **file / component / CSS / hook / i18n key naming** in `src/PlayUp/web`.

Product vocabulary (Stage → Phase, Entry → Équipe, …) lives in [i18n.md](./i18n.md) (Phase 2 glossary). This document does **not** redefine product copy.

Preference for change: **suppression > simplification > factorisation > abstraction**. No mass renames for cosmetic homogeneity.

## Stack & tooling (facts, not promises)

| Concern | Tool                                                                        |
| ------- | --------------------------------------------------------------------------- |
| Lint    | **Oxlint** (`.oxlintrc.json`) — not ESLint                                  |
| Format  | **Prettier** (`.prettierrc.json`, `npm run format` / `format:check`)        |
| Types   | `tsc -b` (`npm run typecheck`)                                              |
| Tests   | Vitest + Testing Library (`npm run test:run`)                               |
| Editor  | Rider/VS Code optional — **no** committed `.vscode/` folder in this package |

CI `web` job: `format:check` → `lint` → `typecheck` → `test:run` → `build`. Oxlint **warnings** do not fail CI today.

## Files

| Kind                              | Convention                 | Examples                                   |
| --------------------------------- | -------------------------- | ------------------------------------------ |
| React components / pages          | `PascalCase.tsx`           | `TeamsPage.tsx`, `StructurePhaseFiche.tsx` |
| Helpers / pure modules            | `camelCase.ts`             | `structureInvalidation.ts`, `queryKeys.ts` |
| Tests                             | colocated `Nom.test.ts(x)` | `TeamsPage.test.tsx`                       |
| Page CSS (multi-component domain) | `domaine.css`              | `structure.css`, `regulation.css`          |
| Page CSS (single isolated page)   | `PageName.css`             | `StagePage.css`                            |
| DS foundations                    | `kebab-case.css`           | `panels.css`, `selectable-tile.css`        |
| DS React                          | `PascalCase.tsx`           | `Dialog.tsx`, `ChoiceTile.tsx`             |

`pages/` is grouped by **functional domain** when a domain has multiple related files (`pages/structure/`, `match/`, `teams/`, `regulation/`, `competition/`, `stage/`, `classements/`). Isolated pages (e.g. `HomePage`, `NotFoundPage`) stay at the `pages/` root. This is not `features/`.

Do **not** invent app-level `hooks/`, `services/`, `features/`, or `components/` folders without an explicit architecture decision ([README](../README.md) out-of-scope table).

## React & hooks

- Hooks named `useX`, **colocated** with the consumer (page/shell/DS) — no catch-all `hooks/` dump.
- Server state = TanStack Query + `queryKeys` factory only.
- Local UI state = `useState` next to the UI; no global store / Context “god”.
- Comments in source: **English**. User-facing copy: **French default** via i18n (see [i18n.md](./i18n.md)).

## CSS

| Layer          | Prefix / tokens                                                       |
| -------------- | --------------------------------------------------------------------- |
| Public tokens  | `--color-*`, `--space-*`, type, geometry, density, motion             |
| DS foundations | `.ds-*`                                                               |
| Page / domain  | domain prefix (`structure-`, `teams-`, `overview-`, `regulation-`, …) |

Surfaces: Canvas / Groupe / Panneau (`.ds-panel`) / Overlay — no fifth “card farm” level. Tile families **A** (ChoiceTile) / **B** (`.ds-selectable-tile`) / **C** (panel) — see [design-system.md](./design-system.md) § Surfaces / tiles / panels.

## i18n keys (naming only)

- Semantic keys, not French prose (`competition.change`, not `changer`).
- One key = one product meaning; named interpolation (`{{count}}`, `{{name}}`).
- Namespaces stay the existing set (`common`, `shell`, `structure`, …) — see [i18n.md](./i18n.md).
- Wire / DTO enums stay English PascalCase; labels go through i18n.

## Icons

- **Always** consume icons via DS wrappers (`Icon` / `*Icons.tsx` under `design-system/icons/`).
- `from 'lucide-react'` is allowed **only** inside `design-system/icons/**` (and optionally `design-lab/` playground specimens).
- Distinguish **technical chrome** (chevron, close, theme sun/moon) in `shellIcons` / `metaIcons` from **product content** glyphs in `contentIcons` / `toastIcons`.
- Do not invent one-off semantic wrappers for a single decorative use; reuse an existing named icon when the concept matches.
- Usage rules (where icons belong / are forbidden): `.cursor/rules/design/09-icons.mdc`.

### Content glyph map

Same meaning shares one glyph. A repeated object family may share one glyph.

| Wrapper                             | Glyph     | Meaning                                                               |
| ----------------------------------- | --------- | --------------------------------------------------------------------- |
| `NextActionIcon`                    | Flag      | Next action, and completing a phase (finish)                          |
| `RoundsStatIcon`                    | Milestone | Cup / Swiss round count                                               |
| `MatchdayStatIcon`                  | Calendar  | Matchdays                                                             |
| `DrawPendingIcon`, `DrawConfigIcon` | Shuffle   | Draw object (pending state and draw configuration). Shared on purpose |
| `RandomIcon`                        | Dices     | Random draw action                                                    |

Decorative per-row glyphs stay out. `lucide-react` stays inside `design-system/icons/`.

## Routes & query keys

- Product routes may stay French (`classements`, …) — intentional.
- Query keys: English, stable, only via `queryKeys.ts`.

## Out of scope here

- DS tile/card unification → Phase 4 (`SelectableTile`, `npm run tile:check`; Stage `stage-draw-tile`).
- Structure file splits → Phase 5 (`structureFluxRails` / `structure-fiche.css`, `npm run` scripts under `scripts/split-structure-*.mjs`).
- Large CSS cleanup → Phase 6 (done: drop global `.field`; match `.form-row` / `.fieldset` in `matches.css`; dead Structure operational + fiche CSS; `npm run css:audit` → `scripts/audit-structure-fiche-css.mjs`).

## CSS tooling and audits

| Goal                                         | Command                                                                                                                                             |
| -------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| **CI / normative CSS**                       | `npm run lint:css` (Stylelint, `src/**/*.css`)                                                                                                      |
| **Repo-wide hygiene report**                 | `npm run frontend:audit` (orphan classes, import graph, duplicate decl blocks — heuristic)                                                          |
| **FE module graph (dead-code audit)**        | `npm run frontend:module-graph` (static reachability from `main.tsx` + test-only support — report-only)                                             |
| **CSS class buckets (S4)**                   | `npm run frontend:css:qualify` (companion to `frontend:audit` — dynamic `className` false positives)                                                |
| **Repo-wide i18n keys**                      | `npm run i18n:audit:repo`                                                                                                                           |
| **i18n suspicious buckets (S3)**             | `npm run i18n:audit:qualify` (report-only; pluriels `_one`/`_other` ≠ défauts)                                                                      |
| **Structure fiche only**                     | `npm run css:audit`, `npm run i18n:audit`                                                                                                           |
| **Rider selector / unknown `var(--*)` scan** | `npm run css:audit:rider` (report); `npm run css:audit:rider -- --fix` joins broken selector lines only — **never** merges comment lines with rules |

### JetBrains Rider

Rider shows **built-in CSS inspections** (duplicate properties, unknown properties, etc.) in addition to Stylelint. Those warnings are **not** identical to `npm run lint:css`. To align the editor with CI:

1. **Settings → Languages & Frameworks → Style Sheets → Stylelint** — enable and use the project `node_modules` (`.stylelintrc.json` at web root).
2. Treat **`npm run lint:css`** as the merge gate; triage Rider-only highlights separately (often duplicate declarations across selectors or DS modifier classes that Stylelint allows).

Hygiene scripts (`frontend:audit`, …) are **report-only** — verify before deleting CSS or keys.

## HTTP contract modules (Phase 7)

- Callers keep importing from barrels `src/api.ts` and `src/types.ts`.
- Implementation lives under `src/api/*` and `src/types/*` by Host domain (competitions, entries, stages, matches, draws, …).
- One HTTP path only (`src/api/http.ts`). No OpenAPI codegen in this phase.
- Dead Stage construction wrappers removed when unused after Phase 1 (`addStageMatchday` / group / round / slot, `replaceStageMatchGenerationFormat`, `replaceStageSwissSettings`).
- `queryKeys.competitions.workspace` removed (invalidate-only no-op; SPA has no GET workspace reader — Host route + `WorkspaceSummary` create response remain).
