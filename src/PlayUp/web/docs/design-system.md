# Design System (Play’Up web)

Operational SoT for `src/design-system/`. Product intent: Notion **Identité visuelle**.

## Principle

> **One visual source of truth.** Tokens + foundations live in the Design System. The Design Lab and `/dev/foundations` demonstrate; they never redefine controls or tokens.

## Layers

| Layer       | Path                                                 | Role                                                                   |
| ----------- | ---------------------------------------------------- | ---------------------------------------------------------------------- |
| Palette     | `palettes/slate.css`                                 | Hex / mixes (`--primitive-*`) — never consume in UI                    |
| Tokens      | `tokens/*.css`                                       | Public API (`--color-*`, `--space-*`, type, geometry, density, motion) |
| Foundations | `foundations/*.css`                                  | Recipes (`.ds-*` classes)                                              |
| React       | `components/`, icons, brand lockups, product visuals | Primitives without feature API / i18n                                  |

### DS boundary

**Allowed in `design-system/`:** interaction primitives (Dialog, Field, inputs, Status, Toaster…), Play’Up visual primitives reused across surfaces (TeamCrest, MatchRow, Overview*…), icons, hooks.

**Forbidden in `design-system/`:** `api/` calls, Media upload, page/route logic, feature i18n namespaces.

Feature adapters (e.g. logo upload + Media) live under `pages/` (see `pages/LogoMediaField.tsx`).

Do **not** create `brand/`, `ui/`, or `design-system/product/` folders without an explicit architecture decision.

## Color budget

Public **`--color-*`** roles on `.ds-root` (see `tokens/colors.css`). Sole product palette: **Slate** (`data-palette="slate"`).

## Theme (light / dark)

- User preference: `system` | `light` | `dark` — persisted as `playup:theme` (default `system`).
- Resolved theme on DOM: `data-theme="light|dark"` on `.ds-root` and `document.documentElement`.
- **`applyTheme()`** (`src/theme/applyTheme.ts`) is the **sole DOM authority** — do not set `data-theme` in React JSX.
- Dark content tokens: `palettes/slate-dark.css` overrides **content primitives only** under `.ds-root[data-theme='dark']`.
- **Chrome primitives** (`--primitive-chrome*`) stay in `slate.css` and are **never** overridden — shell navy is invariant.
- Preference UI: `ToggleButtonGroup` in **Préférences** (icon + label inline; binds to preference, not resolved theme).
- Accueil `PlayUpWordmark surface="home"` swaps light/dark rasters via CSS when `data-theme` changes.
- `useThemeRoot()` re-stamps `data-theme` after each React commit (React strips attributes it does not own).

## Validation surfaces

| Surface            | Role                                                                                                                  |
| ------------------ | --------------------------------------------------------------------------------------------------------------------- |
| `/dev/foundations` | Atom / token playground (Status, icons, crest, density, buttons CSS). **Not** the Form/Dialog/Toast specimen gallery. |
| `/design-lab`      | Compositions + **specimens of real DS components** (Form, Dialog, Toast, …).                                          |
| Product            | Consumes DS; must adopt form stack for new/edited forms.                                                              |

### Design Lab anti-drift

- Specimens import DS components only.
- `design-lab.css` = harness (bar, viewport, boards) — never a parallel Input/Select/Dialog stylesheet.

## Dialog

- Canonical: `components/Dialog.tsx` + `foundations/dialog.css`.
- **Close = icon X**; accessible name via `closeLabel` (default « Fermer »). Do **not** replace with a text « Fermer » button.
- Interactive specimen: Design Lab surface **Dialog** only.
- `AttentionDrawer` is Shell triage — not Dialog.

## Toast

- `Toaster` + `toastStore` (`notify`) + `foundations/toast.css`.
- Mount in a `position: relative` canvas host (`ShellMain`).
- Specimen: Design Lab **Toast**. Contract: Notion Toast decision.
- Soft fill ≈ **16%** tone wash (icon + progress carry tone; no border). Docs previously said ~8% — CSS recipe is the SoT.

## LiveStatus vs Status

|         | `LiveStatus`                                 | `Status tone="live"`                 |
| ------- | -------------------------------------------- | ------------------------------------ |
| Role    | Match clock / in-play indicator              | Chip / badge for live tone           |
| Chrome  | Pulse `ds-live-dot` + soft live wash         | Status density/variant/shape recipes |
| Product | Prefer where a pulsing live mark is required | Prefer for list/chip “live” labels   |

Do not collapse them: pulse activity ≠ tone chip.

## Chip

- Canonical: `components/Chip.tsx` + `foundations/chips.css`.
- Fact / rule token (neutral, soft, accent, win/draw/loss) — **not** a lifecycle `Status`.

## PageHead

- Canonical: `components/PageHead.tsx` + `foundations/page-head.css`.
- Workspace Shell destinations (Équipes, Règlement, Organisation, Matchs, Classements): **title `--text-display`**, optional `actions` / `note` / `tools`, **no back** (rail is enough).
- Drill-downs (Stage, fiche match…): `back` + optional `eyebrow` / badges — `PageHeader` in `ui.tsx` is a thin adapter.
- Do **not** confuse with `PanelHead` (panel/section titles, `--text-body`).

## TextLink / SkipLink

- Cross-surface CTA: `TextLink` + `.ds-text-link` (replaces page-local overview/classements links).
- Skip to main: `.ds-skip-link` (Accueil) / `.ds-skip-link--on-chrome` (Shell).

## Meter

- Capsule track + fill (+ optional marker): `Meter` + `.ds-meter`.
- Used by Teams plateau and Règlement point gauges. **Not** the Règlement capacity min/max rail (page-local).

## Alert

- Prefer `Alert` over bare `.ds-notice` when an icon helps scan. Soft-fill tone recipes in `feedback.css`.

## Attention rows (two models — do not merge)

|       | `AttentionRow` (DS)                  | `AttentionSituationRow` (Shell)          |
| ----- | ------------------------------------ | ---------------------------------------- |
| Role  | D9 Lab / Match Hub count+icon recipe | Product triage from GET /attention       |
| Where | Overview Lab, MatchHub needs-result  | AttentionDrawer + Vue d'ensemble preview |

## Form stack

Product forms (e.g. Teams identity dialogs) use:

`Field`, `TextInput`, `InputNumber`, `Select`, `Upload`, `ColorPicker`, `Alert`,
`Switch`, `SwitchPanel`, `ChoiceTile`, `ToggleButtonGroup`.

Logo identity uses the DS **Upload** picture-card via `pages/LogoMediaField` (feature adapter). Do not reintroduce a crest+file-row chrome in product forms.

Do not reintroduce raw `<input>` / `<select>` chrome for those flows.

## Shell

`shell/` is Play’Up-local chrome. It consumes the DS; it does not redefine tokens.

## Legacy `src/index.css`

Global reset + legacy admin chrome classes only. **No second token ladder.**

Token sheets are imported for early paint; product foundations still load via Shell / Lab / Accueil.

Admin-local literals (page max-width/pad, mono, 140ms transitions) stay inlined until those surfaces leave this file. See [page-migration.md](./page-migration.md).

### Capsule / circle geometry

Fully rounded ends (`border-radius: 999px`) stay **local** where needed (progress tracks, circular swatches/avatars, Status `shape="pill"`, live dots). Geometry tokens intentionally have **no** shared product `--radius-pill`.

## Related

- [page-migration.md](./page-migration.md)
- [i18n.md](./i18n.md)
- Notion Identité visuelle
