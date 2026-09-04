# Design System (Play’Up web)

Operational SoT for `src/design-system/`. Product intent: Notion **Identité visuelle**.

## Principle

> **One visual source of truth.** Tokens + foundations live in the Design System. The Design Lab and `/dev/foundations` demonstrate; they never redefine controls or tokens.

## Layers

| Layer | Path | Role |
|---|---|---|
| Palette | `palettes/slate.css` | Hex / mixes (`--primitive-*`) — never consume in UI |
| Tokens | `tokens/*.css` | Public API (`--color-*`, `--space-*`, type, geometry, density, motion) |
| Foundations | `foundations/*.css` | Recipes (`.ds-*` classes) |
| React | `components/`, icons, brand lockups, product visuals | Primitives without feature API / i18n |

### DS boundary

**Allowed in `design-system/`:** interaction primitives (Dialog, Field, inputs, Status, Toaster…), Play’Up visual primitives reused across surfaces (TeamCrest, MatchRow, Overview*…), icons, hooks.

**Forbidden in `design-system/`:** `api/` calls, Media upload, page/route logic, feature i18n namespaces.

Feature adapters (e.g. logo upload + Media) live under `pages/` (see `pages/LogoMediaField.tsx`).

Do **not** create `brand/`, `ui/`, or `design-system/product/` folders without an explicit architecture decision.

## Color budget

Public **`--color-*`** roles on `.ds-root` (see `tokens/colors.css`). Sole product palette: **Slate** (`data-palette="slate"`).

## Validation surfaces

| Surface | Role |
|---|---|
| `/dev/foundations` | Atom / token playground (Status, icons, crest, density, buttons CSS). **Not** the Form/Dialog/Toast specimen gallery. |
| `/design-lab` | Compositions + **specimens of real DS components** (Form, Dialog, Toast, …). |
| Product | Consumes DS; must adopt form stack for new/edited forms. |

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
- Soft-fill % polish is deferred (P2); do not invent a second toast system.

## Form stack

Product forms (e.g. Teams identity dialogs) use:

`Field`, `TextInput`, `InputNumber`, `Select`, `Upload`, `ColorPicker`, `Alert`.

Do not reintroduce raw `<input>` / `<select>` chrome for those flows.

## Shell

`shell/` is Play’Up-local chrome. It consumes the DS; it does not redefine tokens.

## Legacy `src/index.css`

Still hosts admin layout CSS and a shrinking `:root` alias ladder. **SoT for new work = DS tokens on `.ds-root`.**

See [page-migration.md](./page-migration.md) § Legacy tokens for inventory and remaining debt.

## Related

- [page-migration.md](./page-migration.md)
- [i18n.md](./i18n.md)
- Notion Identité visuelle
