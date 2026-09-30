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

## Popover

- Canonical: `components/Popover.tsx` + `foundations/popover.css`.
- Anchored surface: portal to `document.body`, fixed placement, Escape + outside click.
- Shared caret notch: `data-side="below|above"` + `--ds-popover-caret-inset` toward the trigger.
- Enter/exit: `data-state="open|closed"` — opacity + 6px translate (tokens `--motion-*`).
- Consumers: **ColorPicker** panel, Shell **Preferences** menu.
- Density tokens (`--control-height`) are on `:root` so portaled panels resolve them.

## Tooltip

- Canonical: `components/Tooltip.tsx` + `foundations/tooltip.css`.
- V1 = **Hint** + **DisabledReason** + short **Labels** on icon-only controls (case-by-case; do not mass-migrate every `title`). Specimen: Design Lab **Tooltip**.
- Chrome: Overlay family — `surface` + border + `radius-control` 4px. Local shadow `0 2px 6px` (lighter than `--shadow-overlay`; no `--shadow-tooltip` token). Not `ink`/`on-ink` (explored in Design Lab only).
- Desktop: hover open **400 ms**, focus **0**, leave close **100 ms**, Escape/blur immediate.
- Mobile: long-press (**500 ms**) on primary-action triggers; tap-toggle otherwise; auto-dismiss **~3 s**.
- `role="tooltip"` + `aria-describedby`; text only; interactive content → Popover.
- Prefer short Tooltip when `aria-label` is richer (e.g. named entity). Crest: Tooltip only when the crest is the sole name cue. Do not infer Tooltip from the mere presence of `title` (Label vs Hint vs DisabledReason vs duplicate of visible text).

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
- Hint copy: wrap with `Tooltip` (native `title` is not the V1 tip path).

## PageHead

- Canonical: `components/PageHead.tsx` + `foundations/page-head.css`.
- Workspace Shell destinations (Équipes, Règlement, Structure, Matchs, Classements): **title `--text-display`**, optional `actions` / `note` / `tools`, **no back** (rail is enough).
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

## Surfaces / tiles / panels (three families)

There is **no** generic `Card` foundation (see `foundations/surfaces.css`). New UI must pick one of the families below — do not invent a fourth “card” chrome.

| Family                 | Mechanism                                            | Role                                                         | Typical usages                                                          |
| ---------------------- | ---------------------------------------------------- | ------------------------------------------------------------ | ----------------------------------------------------------------------- |
| **A. Choice tile**     | `ChoiceTile` + `.ds-choice-tile` (`role="checkbox"`) | Parameter / option choice inside a **form**                  | Structure dialogs scope, Regulation discipline, AddPhase format         |
| **B. Selectable tile** | `.ds-selectable-tile` + page layout classes          | **Entity** selectable in a list/grid (selection chrome only) | `teams-tile`, `structure-topology__tile`, qualif/prog rows, draw master |
| **C. Panel**           | `.ds-panel` (+ `PanelHead`)                          | Content container (not decorative card farm)                 | Overview, Classements, Matches hub, Stage                               |

**Also page-local (not a fourth family):**

| Pattern                  | Class                                  | Note                                                                       |
| ------------------------ | -------------------------------------- | -------------------------------------------------------------------------- |
| Phase fiche              | `.structure-fiche`                     | Structure layout métier — keep page-local until a second surface reuses it |
| Domain sections in fiche | `.structure-domain-tile`               | Internal fiche sections (FormSection-like), **not** family B selection     |
| Attribution tiles        | `.structure-attribution__tile`         | Dialog content blocks (not selection chrome)                               |
| Schematic pots           | `.regulation-schematic__card*`         | Diagram cells — naming historical; not family B                            |
| Disciplinary tokens      | `.regulation-card-token*`              | Literal “carton” glyphs — keep `card` in the name                          |
| Overview config height   | `.overview-config-card` on `.ds-panel` | Layout helper on family **C**, not a card system                           |
| Stage draw block         | `.draw-card`                           | Page-local Stage content — migrate naming when Stage is touched (Phase 6+) |

### Rules

1. **No new one-off “card” CSS** for selection or content chrome — reuse A / B / C.
2. **B** owns selection visuals exclusively via `.ds-selectable-tile` + `data-selected="true"` (brand border + tint tokens). Page classes may layout content only — **never** re-color selection (no hard-coded blues).
3. **A** is for form options only — not team/phase lists.
4. **C** — no panel-in-panel card farm; prefer Group + proximity for clustering.
5. Prefer the `__tile` suffix for family **B** page classes (pilots: topology, draw master, qualification rows). Remaining `__card` names are debt to rename when the file is already open.
6. List rows with checkboxes (e.g. roster members) are **not** family B — use row layout + checkbox; do not fake tile selection chrome.

### Selectable tile (family B) recipe

- Canonical: `foundations/selectable-tile.css` (`.ds-selectable-tile`).
- Compose: `className="<page-layout> ds-selectable-tile"` + `data-selected="true|false"`.
- Hover / selected recipes live only in the foundation file.

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

## Shared `src/index.css`

Global reset + shared admin chrome classes only. **No second token ladder.**

Token sheets are imported for early paint; product foundations still load via Shell / Lab / Accueil.

Admin-local literals (page max-width/pad, mono, 140ms transitions) stay inlined until those surfaces leave this file. See [page-migration.md](./page-migration.md).

### Capsule / circle geometry

Fully rounded ends (`border-radius: 999px`) stay **local** where needed (progress tracks, circular swatches/avatars, Status `shape="pill"`, live dots). Geometry tokens intentionally have **no** shared product `--radius-pill`.

## Icons

- SoT wrappers: `src/design-system/icons/` — `Icon.tsx` + `contentIcons` / `shellIcons` / `metaIcons` / `toastIcons`.
- Pages and shell **must not** import `lucide-react` directly; add a named wrapper when a glyph is missing.
- `design-lab/` may still import Lucide for playground specimens (prefer wrappers when they already exist).
- Naming & placement rules: [conventions.md](./conventions.md) · Cursor rule `09-icons`.

## Related

- [conventions.md](./conventions.md)
- [page-migration.md](./page-migration.md)
- [i18n.md](./i18n.md)
- Notion Identité visuelle
