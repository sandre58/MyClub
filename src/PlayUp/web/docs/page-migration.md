# Page migration — 13.5 → Design System + i18n

Checklist applied when reworking an organizer page (Phase C). Pilot: `NotFoundPage`.

Do **not** big-bang migrate all pages. Apply this list when a product task already touches the page.

## Checklist

1. **Strings** — every new or touched user-facing string goes through `useTranslation` / `t()` (`common`, `shell`, `enums`, `actions`, `matches`, `draw`, `overview`, `structure`, `stage`, `home`, `competitions`, `errors`). Prefer **wire codes → i18n** (`source`, `nextActionCode`, `blockers`, ProblemDetails `code`). See [i18n.md](./i18n.md).
2. **Visual** — replace 13.5 classes (`.btn`, `.card`, `.page`, …) with Design System foundations (`.ds-btn`, `.ds-group`, `.ds-heading`, …) under `.ds-root` (already provided by the Shell).
3. **Layout** — prefer `.shell-page` (narrow) or reuse foundations; co-locate `PageName.css` only for page-specific rules that cannot live in foundations.
4. **Data** — use `queryKeys` from `src/queryKeys.ts` for all `useQuery` / `invalidateQueries` keys.
5. **Tests** — wrap with `renderWithI18n`; assert accessible names in French (product locale).
6. **Retire 13.5** — delete unused classes from `src/index.css` only when **no** remaining page references them.

## Priority when a product task touches a page

Workspace → Match hub → Stage / `drawUi` → Structure → remaining pages.

## End of `index.css`

`src/index.css` holds global reset + shared admin chrome classes (`.page`, `.form`, `.field`, `.button-row`, Stage helpers…). Visual tokens come from `design-system/tokens/*` only — no concurrent ladder.

Admin-only literals (page width, pad, mono stack, 140ms transitions, gauge `999px`) stay inlined where used until those surfaces migrate off this file.

**Phase 6 (CSS cleanup):** removed orphaned `.row__aside`, `.form--inline`, `.form--wide`. Match score forms use `.ds-form` + page chrome; Match panel forms drop redundant `.form` when page CSS already owns layout. Structure/Teams/Regulation keep page CSS for domain chrome; dead Structure legacy blocks (band/points/entry/old phase/roster/detail/drill/overview/graph…) removed after TSX confirmation (watch dynamic `class--${tone}` modifiers).

## Retired token aliases

**SoT:** `design-system/tokens/*` on `:root` + `.ds-root`.

### Migrated (P0 + P1-A)

| Previous name                            | Replacement                  | Where                               |
| ---------------------------------------- | ---------------------------- | ----------------------------------- |
| `--text-caption`                         | `--text-label`               | pages / index (P0)                  |
| `--space-xs/sm/md`                       | `--space-8/12/16`            | index (P0)                          |
| `--space-2xs`                            | `--space-4`                  | index                               |
| `--space-lg`                             | `--space-24`                 | index                               |
| `--space-2xl`                            | `--space-48`                 | index                               |
| `--radius-sm`                            | `--radius-control`           | index + Structure dialogs (Phase 6) |
| `--radius-md`                            | `--radius-panel`             | index + Structure dialogs (Phase 6) |
| `--radius-lg`                            | `--radius-overlay`           | index                               |
| `--color-bg` / `--color-surface` / …     | `--bg` / `--surface` / …     | index + pages                       |
| `--color-text` / `--color-muted`         | `--text` / `--text-muted`    | index + pages                       |
| `--color-border`                         | `--border`                   | index + pages                       |
| `--color-danger` / `--color-warning` / … | `--danger` / `--warning` / … | index + pages                       |
| `--font-size-sm`                         | `--text-meta`                | Structure hub / prefs (Phase 6)     |
| `--color-text-muted`                     | `--color-text-secondary`     | Structure / schematic (Phase 6)     |

Do not reintroduce the previous names. New CSS uses only the replacement tokens.
