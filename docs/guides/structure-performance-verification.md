# Structure performance — manual verification checklist

Use a multi-phase Cup or Groups competition.

## After PR1 (invalidation)

1. **Post-PUT dialog close**: Activate draw — time from network PUT response to dialog unmount &lt; 500 ms.
2. **Fan-out**: Activate draw must not trigger `overview` or `consultation` refetch (Network tab).
3. **Refetch failure**: Block `GET …/structure` after successful activate — UI not frozen; refresh page restores truth.
4. **Rapid mutations**: Activate then deactivate draw quickly — final UI matches last action after refetches settle.
5. Draw workflow, materialize, Prepare, Qual/Prog save.
6. **Nouveau tirage** (create + generate, draft): Network must **not** refetch `GET …/structure`, schematic, or attention; only stage overview (dialog list).
7. Publish / apply draw: hub + schematic refetch as before.

## After PR2 (loaders)

1. During background structure refetch after activate draw: operational strip columns and schematic stay visible (no full-section regional loader).
2. Hub background refresh: no per-column « Mise à jour… » text; numbered chip replaced by a same-size spinner in each section title (no layout shift).
3. Materialize / dematerialize / deactivate draw: strip section body stays visible; spinner on the action control only.
4. Schematic refetch: viewport shows centered WaitMark only (diagram hidden until fetch settles).

## After PR3 (optimistic, if shipped)

1. Simulate structure refetch failure after rename — name rolls back or reconciles on retry.

## After PR4 (schematic SQL)

1. Multi-phase Qual/Prog feeds on schematic unchanged vs baseline snapshot test in CI.
