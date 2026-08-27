# Design System (Play’up web)

Operational SoT for the foundations under `src/design-system/`. Product intent: Notion **Identité visuelle**.

## Principle

> Foundations define the language; the playground verifies it; the Shell consumes it; legacy pages stay temporarily outside.

## Layers

| Layer | Role | Rule |
|---|---|---|
| Palette primitives (`--primitive-*`) | Hex / mixes in `palettes/slate.css` | Never consume in UI |
| Semantic tokens (`--color-*`, spacing, …) | Public API | Foundations + Shell + pages |
| Foundations (`ds-*` CSS) | Recipes | Prefer these over ad-hoc CSS |
| React primitives | `Status`, `Icon`, `TeamCrest`, … | Thin wrappers over foundations |

## Color budget

Public **`--color-*`** roles: **21 / ceiling 70** (content 17 + chrome 4).

Chrome roles (`--color-chrome`, `--color-on-chrome`, `--color-chrome-border`, `--color-chrome-accent`) are Shell rail only — not content surfaces. There is no `--color-chrome-active` or `--color-chrome-muted`: active/idle use accent, opacity, and mixes.

Sole product palette: **Slate** (`data-palette="slate"`).

## Validation

Route `/dev/foundations` — `FoundationsPlayground`. Terrain of truth for tokens, foundations, Status, TeamCrest, icons, and a mini chrome rail. Not Storybook.

## Shell / Sidebar

`shell/` is **Play’up-local**. Do **not** extract a shared MyClub Shell/Sidebar package until a **second MyClub application** actually consumes the same Shell contract.

Content link ink is scoped to `.shell-main` (and `.ds-preview`) so chrome links keep `--color-on-chrome`.

## Still legacy (out of this foundations lot)

- Global `src/index.css` (13.5 `.btn` / `.card` / `.page`)
- Shared `ui.tsx` chrome (`PageHeader`, loading/empty notices) still on legacy classes
- Unmigrated organizer pages (Home, Competitions, Stage, Match, …)
- Hybrid pages that mix `ds-panel` with page-local shadows/chips

Page migration is a **separate** lot after foundations are stable.

## Related

- [page-migration.md](./page-migration.md) — when a product task touches a page
- [i18n.md](./i18n.md) — strings
- Notion Identité visuelle (product SoT)
