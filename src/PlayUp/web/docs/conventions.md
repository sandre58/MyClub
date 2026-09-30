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

Surfaces: Canvas / Groupe / Panneau (`.ds-panel`) / Overlay — no fifth “card farm” level. Tile/card families are documented separately in [design-system.md](./design-system.md) (Phase 4).

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

## Routes & query keys

- Product routes may stay French (`classements`, …) — intentional.
- Query keys: English, stable, only via `queryKeys.ts`.

## Out of scope here

- DS tile/card unification → Phase 4.
- Structure file splits → Phase 5.
- Large CSS cleanup → Phase 6.
- `api.ts` / `types.ts` modularization → Phase 7.
