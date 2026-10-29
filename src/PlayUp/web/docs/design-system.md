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

Feature adapters (e.g. logo upload + Media) live under `pages/` (see `pages/teams/LogoMediaField.tsx`).

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
- **Footer actions.** One right-aligned group. Order: destructive (`.ds-btn--destructive`) first, then a `--space-16` gap, then secondary actions, then the primary (`.ds-btn--primary`) last. Close stays the header X.
- **Initial focus.** Default `initialFocus="body"` focuses the first control in the body, then the footer rule below. `initialFocus="footer"` uses that footer rule only: focus never lands in the body. `pickFooterInitialFocus` order: enabled `.ds-btn--primary`, then the first enabled footer button that is neither primary nor destructive, then the header close X. Never a destructive button. Never the body. Pass `focusKey` to apply that focus again while the dialog stays open. The Structure draw dialog is the only product surface that sets `initialFocus="footer"`. `ConfirmDialog` keeps the default body path; its message is not focusable, so a danger confirm follows the same footer rule and focuses the ghost cancel (then X), not the destructive confirm.
- Interactive specimen: Design Lab surface **Dialog** only.
- `AttentionDrawer` is Shell triage — not Dialog.

## Hero facts

- Shared classes: `.structure-qualification__facts`, `.structure-qualification__fact`, `--primary` / `--secondary`, and `.structure-qualification__fact-rule` between facts.
- Order is secondary, then the rule, then primary. The container sets `aria-live="polite"`.
- The draw dialog uses the same pattern: groups show the group count then the team count; a cup shows places then the team count. The facts stay the same whichever execution is selected.

## Match row

- Canonical: `components/MatchRow.tsx` + `foundations/match.css`.
- Optional `density?: 'compact'` sets `data-density="compact"`. Compact rows use `--text-label` on `.ds-match-row__team`, `--text-meta` on the muted score and the aside, and `min-height: 2rem`.
- Omitting `density` leaves the default row unchanged. Crest size stays a design-system size (`TeamCrest` `size="sm"`).

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

## Status

- Canonical: `components/Status.tsx` + `foundations/status.css`.
- Icon + label rows: wrap children in `.ds-status-inline` / `.ds-status-inline__icon`.
- Hub notes (Structure PageHead): `context` · `outline` · `pill`, short nominal label + `.ds-status-inline`; specimen in Design Lab **Structure PageHead**. Use `multiline` + `textBody` only when a longer wrapped label is explicitly required elsewhere.

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

## EmptyState

- React: `EmptyState` in `ui.tsx`; styles in `foundations/panels.css` (`.ds-empty*`).
- **variant** — `default` (left-aligned list empty) or `idle` (centered region empty: icon + title + optional hint/actions).
- **size** — pick by vertical budget, not by page:
  - `default` — full list / panel empty (padding `space-24`, large icon when present).
  - `compact` — nested sub-regions (Flux rails, composition hints): horizontal icon + copy via `.ds-empty__copy`, meta-sized title, minimal padding. Pair with `variant="idle"` and `iconSize="sm"` when an icon is shown.
  - `comfortable` — drawer / hub idle zones that need more air than default idle.
- **iconSize** — `sm` | `md` | `lg` on the icon slot (`.ds-empty__icon--*`); defaults to `sm` when `size="compact"`. Omit to keep the legacy default idle icon scale (2.75rem) on non-compact empties.
- Do not shrink empty states with page-local padding overrides when `size="compact"` fits; reserve local CSS for layout containers only (e.g. `.structure-phase-rail__empty`).

## Surfaces / tiles / panels (three families)

There is **no** generic `Card` foundation (see `foundations/surfaces.css`). New UI must pick one of the families below — do not invent a fourth “card” chrome.

| Family                 | Mechanism                                                         | Role                                                                                 | Typical usages                                                               |
| ---------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| **A. Choice tile**     | `ChoiceTile` + `.ds-choice-tile` (`role="checkbox"`, check glyph) | Option inside a form, including a simple entity (crest + name) in a selection dialog | Structure scope, Regulation discipline, AddPhase format, Équipes affectation |
| **B. Selectable tile** | `.ds-selectable-tile` + page layout classes (no check glyph)      | Rich entity tile in a list or grid                                                   | Teams page, structure topology, qualif/prog rows, draw master                |
| **C. Panel**           | `.ds-panel` (+ `PanelHead`)                                       | Content container (not decorative card farm)                                         | Overview, Classements, Matches hub, Stage                                    |

**PanelHead** — leading section icon uses `color-brand` (`.ds-panel-head__icon`). Do not recolor section icons to neutral gray.

**Also page-local (not a fourth family):**

| Pattern                  | Class                                  | Note                                                                       |
| ------------------------ | -------------------------------------- | -------------------------------------------------------------------------- |
| Phase fiche              | `.structure-fiche`                     | Structure layout métier — keep page-local until a second surface reuses it |
| Domain sections in fiche | `.structure-domain-tile`               | Internal fiche sections (FormSection-like), **not** family B selection     |
| Attribution tiles        | `.structure-attribution__tile`         | Dialog content blocks (not selection chrome)                               |
| Schematic pots           | `.regulation-schematic__card*`         | Diagram cells — naming historical; not family B                            |
| Disciplinary tokens      | `.regulation-card-token*`              | Literal “carton” glyphs — keep `card` in the name                          |
| Overview config height   | `.overview-config-card` on `.ds-panel` | Layout helper on family **C**, not a card system                           |
| Stage draw block         | `.stage-draw-tile`                     | Page-local Stage content (family C-like block, not selection tile)         |

### Rules

1. **No new one-off “card” CSS** for selection or content chrome — reuse A / B / C.
2. **B** owns selection visuals exclusively via `.ds-selectable-tile` + `data-selected="true"` (brand border + tint tokens). Page classes may layout content only — **never** re-color selection (no hard-coded blues).
3. **A** is the checkbox option tile, including a simple entity (crest + name) chosen inside a selection form or dialog. **B** is the rich entity tile in a list or grid (Teams page, topology, draw master, qualif/prog rows) and has no check glyph. On **A**, `layout="inline"` is for an entity or option rendered on one line (the check is vertically centered). `layout="stacked"` (the default) applies as soon as there is a description or other multi-line content (the check stays on the first line).
4. **C** — no panel-in-panel card farm; prefer Group + proximity for clustering.
5. Prefer the `__tile` suffix for family **B** page classes (pilots: topology, draw master, qualification rows). Remaining `__card` names are debt to rename when the file is already open.
6. List rows with checkboxes (e.g. roster members) are **not** family B — use row layout + checkbox; do not fake tile selection chrome.

### Selectable tile (family B) recipe

- Canonical: `foundations/selectable-tile.css` (`.ds-selectable-tile`).
- React: `SelectableTile` sets `data-selected` and optional `tone="error"` (`data-tone="error"` only — no injected layout). Page classes supply triggers, chevrons, and panels.
- Compose: `className="<page-layout>"` + `selected` prop, or manual `ds-selectable-tile` + `data-selected="true|false"`.
- Hover / selected recipes live only in the foundation file (brand default; `[data-tone='error'][data-selected='true']` uses destructive error chrome, never brand).
- CI guard: `npm run tile:check`.

#### Tuile repliable (B)

Structure qualification, progression, and draw **previous runs** share one anatomy (implemented inline per dialog today):

- **Container** — `.ds-selectable-tile` with `data-selected="true"` when open; optional `data-tone="error"` for cancelled / obsolete history (base chrome at rest, destructive tint when open).
- **Trigger** — `<button class="ds-selectable-tile__trigger">` inside a heading (`h5`), with `aria-expanded` and `aria-controls` pointing at the panel id; chevron `.ds-selectable-tile__chevron` at the end (`aria-hidden`), rotates when open.
- **Panel** — sibling block inside the tile (page class e.g. `structure-draw-previous__panel`), shown only when open; page CSS handles padding and entry animation (`prefers-reduced-motion: reduce` disables animation).
- **Focus** — focus ring on the tile via `.ds-selectable-tile:has(.ds-selectable-tile__trigger:focus-visible)`; trigger itself has no box-shadow (avoids clipping under `overflow: hidden`).
- **Retired runs (draw)** — page may apply light `opacity` on the whole collapsed tile (~0.85); open / hover / focus-visible return to opacity 1. Prefer `variant="outline"` badges on tinted headers. Do not use brand selection on `tone="error"` tiles.
- **Debt** — extract a shared `DisclosureTile` and migrate Qual / Prog / Draw in a follow-up (not required for parity).

## Attention rows (two models — do not merge)

|       | `AttentionRow` (DS)                  | `AttentionSituationRow` (Shell)          |
| ----- | ------------------------------------ | ---------------------------------------- |
| Role  | D9 Lab / Match Hub count+icon recipe | Product triage from GET `/attention`     |
| Where | Overview Lab, MatchHub needs-result  | AttentionDrawer + Vue d’ensemble preview |

**Functional errors (V1 Structure / Tirage / Placement):** product surfaces use `Alert` / `Status` / PageHead note / Topology / dialog Incomplete — mapped by `functionalProblemPresentation`. Do **not** reuse Lab `AttentionRow` specimens as the product error pattern, and do **not** absorb construction blockers into AttentionDrawer (Q1).

## Form stack

Product forms (e.g. Teams identity dialogs) use:

`Field`, `TextInput`, `InputNumber`, `Select`, `Upload`, `ColorPicker`, `Alert`,
`Switch`, `SwitchPanel`, `ChoiceTile`, `ToggleButtonGroup`.

Logo identity uses the DS **Upload** picture-card via `pages/teams/LogoMediaField` (feature adapter). Do not reintroduce a crest+file-row chrome in product forms.

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
