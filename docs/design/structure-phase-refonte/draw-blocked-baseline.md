# Draw blocked CTA — UX baseline (Phase 1)

Parity reference for **Tirage** when `resolveStageDrawCreateGate` returns `ok: false`. Sources: `StructurePhaseFiche.tsx`, `StructurePhaseOperationalStrip.tsx` (`DrawColumn`), `drawUi.ts`. No new rules in refonte layout work.

Related: [truth sources](./truth-sources.md), [state matrix](./state-matrix.md), [draw bracket place readiness contract](./draw-bracket-place-readiness-contract.md).

---

## Gate authority

| Input | Field / function | Notes |
|-------|------------------|--------|
| Pool size | `stage.compositionEntryCount` | **Not** `occupiedPlacesCount` or occupancy ratio |
| Places N | `compositionCapacity` ?? `resolvePlacesN(stage)` | Cup count-mismatch vs pool |
| Overview | `drawOverviewQuery` slots/draws | Cup gate waits `drawGateReady` |
| Reason | `createGate.reason` when `!createGate.ok` | `DrawCreateBlockReason` |

`blockPerformDrawCta` (Fiche): `createBlockedShort != null && !activeDraw && overviewDraws.length === 0`.

Occupancy / places gauge **never** flips this flag.

---

## Presentation kinds (`resolveDrawCreateBlockPresentation`)

| Reason | Presentation | Strip primary CTA | Inline caption (`showCreateBlockedCaption`) |
|--------|--------------|-------------------|---------------------------------------------|
| `active` | quiet | Enabled if other gates allow | No |
| `emptyPoolUpstream` | quiet | Enabled if other gates allow | No |
| `unsupported` | quiet | Enabled if other gates allow | No |
| `emptyPool` | inline info | **Disabled** + tooltip when `blockPerformDrawCta` | No (suppressed in Fiche) |
| `countMismatch` | inline warning | **Disabled** + tooltip when `blockPerformDrawCta` | No if `showPoolHint` (pool gauge visible) |
| `belowMinimumTeams` | inline warning | **Disabled** + tooltip | Yes — short text + link to `/competitions/{id}/teams` |
| `groupShape` | inline warning | **Disabled** + tooltip | Yes |
| `missingPots` | inline warning | **Disabled** + tooltip | Yes |
| `occupiedSlots` | inline warning + action | **Disabled** + tooltip | Yes (workflow / release may unblock) |
| `directAssignment` | inline warning + action | **Disabled** + tooltip | Yes |

`showCreateBlockedCaption` (Fiche): `createBlockPresentation.kind === 'inline'` AND NOT `showReleaseCta` AND NOT (`countMismatch` && `showPoolHint`) AND reason NOT `emptyPool` / `emptyPoolUpstream`.

---

## Strip (`DrawColumn`) behaviour

1. **Blocked primary** (`blockPerformDrawCta`): disabled `<button class="ds-btn ds-btn--primary ds-btn--sm">` with `DrawPendingIcon`, label `fiche.performDraw` or `fiche.openDrawWorkflow` if `activeDraw`. Wrapped in `Tooltip` with `createBlockedShort`.
2. **Inline block**: `StripOperationalMessage` attention tone with `createBlockedShort`; `belowMinimumTeams` appends teams `Link`.
3. **Unblocked**: primary calls `onOpenDrawWorkflow`.
4. **Activate path**: `showActivateDrawCta` → empty state with activate button (not `blockPerformDrawCta` path).
5. **Applied draw**: `StripDrawAppliedPanel` + view workflow secondary.

Operational strip (`DrawColumn`) is the canonical Tirage surface on the phase fiche.

---

## Dialog parity (`StructureDrawDialog`)

Uses same `createBlockedReason` / presentation; `countMismatch` may show detail copy in dialog when presentation is inline. Refonte must not diverge gate inputs from Fiche when opening workflow from composition aside.

---

## Phase 1 refonte checklist

- [ ] Composition **Affectation** column uses same props as pre-refonte operational bar (`blockPerformDrawCta`, `showCreateBlockedCaption`, pool hint).
- [ ] No new enablement path from places occupancy meter.
- [ ] Visual recette: at least one blocked reason with disabled primary + tooltip; one with inline caption (`belowMinimumTeams` or `groupShape`).
