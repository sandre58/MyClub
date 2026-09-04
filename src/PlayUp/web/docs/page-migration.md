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

`src/index.css` dies when the last `.btn` / `.card` / `.page` / legacy-token consumer is migrated. Until then both systems coexist by design (14.5).

## Legacy tokens (P0 inventory → P1 finish)

**SoT:** `design-system/tokens/*` on `.ds-root`.

### Migrated (P0)

| Legacy | Replacement | Where |
|---|---|---|
| `--text-caption` | `--text-label` | `pages/matches.css`, `index.css` usages |
| `--space-xs` | `--space-8` | `index.css` usages |
| `--space-sm` | `--space-12` | `index.css` usages |
| `--space-md` | `--space-16` | `index.css` usages |
| `--color-primary-border` | `color-mix(… brand 35%, border)` | `index.css` usage |
| Unused `:root` defs removed | — | `--space-xs/sm/md`, `--text-caption`, `--color-primary*` |

### Remaining (P1 — do not “fix” visually without a decision)

| Token | Files | Notes |
|---|---|---|
| `--radius-pill` | `pages/teams.css` (plateau gauge) | DS geometry forbids product pills; gauge track needs explicit SoT |
| `--space-2xs`, `--space-lg`, `--space-xl`, `--space-2xl` | `index.css` | Map to `--space-4` / `--space-24` / `--space-32` / `--space-48` |
| `--radius-sm/md/lg` | `index.css` | Map to `--radius-control` / `--radius-panel` |
| `--text-eyebrow`, `--text-secondary` (size), `--page-pad`, `--layout-max` | `index.css` | Page chrome leftovers |
| Soft tone aliases (`--color-danger*`, `--color-warning*`, …) | `index.css` | Prefer `--color-error` / `--color-attention` + mixes |

## Deferred (do not add without an explicit trigger)

See README **Out of scope** — OpenAPI, Playwright, Storybook, `features/`, Tailwind / UI kits.
