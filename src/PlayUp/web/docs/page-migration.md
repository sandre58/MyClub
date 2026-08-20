# Page migration — 13.5 → Design System + i18n

Checklist applied when reworking an organizer page (Phase C). Pilot: `NotFoundPage`.

Do **not** big-bang migrate all pages. Apply this list when a product task already touches the page.

## Checklist

1. **Strings** — every new or touched user-facing string goes through `useTranslation` / `t()` (`common`, `shell`, or `enums`). See [i18n.md](./i18n.md).
2. **Visual** — replace 13.5 classes (`.btn`, `.card`, `.page`, …) with Design System foundations (`.ds-btn`, `.ds-group`, `.ds-heading`, …) under `.ds-root` (already provided by the Shell).
3. **Layout** — prefer `.shell-page` (narrow) or reuse foundations; co-locate `PageName.css` only for page-specific rules that cannot live in foundations.
4. **Data** — use `queryKeys` from `src/queryKeys.ts` for all `useQuery` / `invalidateQueries` keys.
5. **Tests** — wrap with `renderWithI18n`; assert accessible names in French (product locale).
6. **Retire 13.5** — delete unused classes from `src/index.css` only when **no** remaining page references them.

## End of `index.css`

`src/index.css` dies when the last `.btn` / `.card` / `.page` consumer is migrated. Until then both systems coexist by design (14.5).

## Deferred (do not add without an explicit trigger)

See README **Out of scope** — OpenAPI, Playwright, Storybook, `features/`, Tailwind / UI kits.
