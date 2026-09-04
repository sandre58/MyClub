# Page migration — 13.5 → Design System + i18n

Checklist applied when reworking an organizer page (Phase C). Pilot: `NotFoundPage`.

Do **not** big-bang migrate all pages. Apply this list when a product task already touches the page.

## Checklist

1. **Strings** — every new or touched user-facing string goes through `useTranslation` / `t()` (`common`, `shell`, `enums`, `actions`, `matches`, `draw`, `overview`, `organisation`, `stage`, `home`, `competitions`, `errors`). Prefer **wire codes → i18n** (`source`, `nextActionCode`, `blockers`, ProblemDetails `code`). See [i18n.md](./i18n.md).
2. **Visual** — replace 13.5 classes (`.btn`, `.card`, `.page`, …) with Design System foundations (`.ds-btn`, `.ds-group`, `.ds-heading`, …) under `.ds-root` (already provided by the Shell).
3. **Layout** — prefer `.shell-page` (narrow) or reuse foundations; co-locate `PageName.css` only for page-specific rules that cannot live in foundations.
4. **Data** — use `queryKeys` from `src/queryKeys.ts` for all `useQuery` / `invalidateQueries` keys.
5. **Tests** — wrap with `renderWithI18n`; assert accessible names in French (product locale).
6. **Retire 13.5** — delete unused classes from `src/index.css` only when **no** remaining page references them.

## Priority when a product task touches a page

Workspace → Match hub → Stage / `drawUi` → Organisation → remaining pages.

## End of `index.css`

`src/index.css` holds global reset + legacy admin chrome classes (`.page`, `.form`, `.row`, draws…). Visual tokens come from `design-system/tokens/*` only — no concurrent ladder.

Admin-only literals (page width, pad, mono stack, 140ms transitions, gauge `999px`) stay inlined where used until those surfaces migrate off this file.

## Legacy tokens

**SoT:** `design-system/tokens/*` on `:root` + `.ds-root`.

### Migrated (P0 + P1-A)

| Legacy | Replacement | Where |
|---|---|---|
| `--text-caption` | `--text-label` | pages / index (P0) |
| `--space-xs/sm/md` | `--space-8/12/16` | index (P0) |
| `--space-2xs` | `--space-4` | index |
| `--space-lg` | `--space-24` | index |
| `--space-2xl` | `--space-48` | index |
| `--radius-sm` | `--radius-control` | index |
| `--radius-md` | `--radius-panel` | index |
| `--text-eyebrow` | `--text-meta` | index |
| `--text-secondary` (size) | `--text-body` | index |
| `--color-surface-muted` | `--color-surface-secondary` | teams.css |
| `--radius-pill` | literal `999px` | teams.css gauge (P1-B SoT) |
| Soft-tone aliases / unused `:root` ladder | removed | index `:root` |

### Remaining local (not a second token ladder)

| Value | Where | Notes |
|---|---|---|
| `74rem` / `46rem` / `clamp(1rem, 4vw, 2rem)` | `.page` | layout chrome local to admin `.page` |
| `999px` | teams gauge / circular swatches | Capsule or circle geometry — no shared pill token (P1-B) |
| `140ms` easing / focus ring / mono stack | index admin chrome | preserve timing; DS motion is 160ms |

## Deferred (do not add without an explicit trigger)

See README **Out of scope** — OpenAPI, Playwright, Storybook, `features/`, Tailwind / UI kits.
