# Structure phase fiche refonte — state matrix (Phase 0)

Independent dimensions for Composition / schematic / draw. **Do not collapse** into one icon or badge.

References: `drawUi.ts`, `structurePlaceOccupancy.ts`, `phaseSchematic.tsx`, tests `structurePlaceOccupancy.test.ts`, `phaseSchematic.test.tsx`, `drawUi.test.ts`.

---

## Dimensions

1. **Entry kind** — direct (affectation authoring) vs qualified (inbound Qual/Prog rule / schematic feed).
2. **Resolution** — team name known (`entry` / `assignment` on schematic) vs rule-only (`feedOrigin`, wait chrome).
3. **Placement** — nominative place occupied for the format (schematic case), when the format supports per-entry placement UI.
4. **Draw** — orthogonal; states from draw overview + `drawUi` only.

---

## Entry × resolution × placement (target UI)

Applies when Phase 2 introduces per-entry cells (`structureEntryPlacementProjection.ts` **not validated** — signature TBD).

| Context | Resolved | Placed (if format supports) | Text | Placement chrome | Opacity |
|---------|----------|---------------------------|------|------------------|---------|
| Direct team | Yes (name) | Yes / No | Club name | Trailing check if placed | Reduced if not placed **and** format shows per-entry placement |
| Qualified | Yes | Yes / No | Name or rule label | Check if placed | Reduced if not placed **and** per-entry placement active |
| Qualified unresolved | No | n/a | Rule + wait (`WaitMark` / feed label) | No check | **No** “unplaced” opacity — use wait styling |
| Format without per-entry placement | * | n/a | Name / rule | **No check** | **No** placement opacity |

### Per-format placement (audit)

| Format | Per-entry placement in current product | Authority for “placed” | Ambiguity / fallback |
|--------|----------------------------------------|------------------------|----------------------|
| **Cup** | Manual slot assign + draw apply to slots | Schematic `CupSlot` with `entry`/`assignment`; not `feedOrigin` alone for “team placed” | Feed awaiting draw: occupied for **gauge**, not for nominative check |
| **Groups** | Draw group placements + schematic `GroupPlace` | `countSchematicOccupiedPlaces` + case occupants | Pots/groups shape enforced by draw gate, not gauge |
| **Championship** | Roster places, no slot UI | `RosterPlace` occupancy | List is affectation; roster may include composition-only entries — **indeterminate** for per-entry check |
| **Swiss** | Same as championship roster | `RosterPlace` | Same as championship |

**Phase 2 fallback:** if helper cannot map entry → place without new domain rules → **no nominative check**; Affectation section keeps aggregate gauge only.

### Documented conflict resolution

| Conflict | Single rule |
|----------|-------------|
| Team name known, gauge counts place, no nominative slot | Gauge uses schematic occupancy; cell “placed” check **off** unless helper maps slot; resolution shown in text |
| `compositionEntryCount` ≠ `affectationEntryCount` | Draw/pool indicators use **composition**; Équipes list uses **affectation** |
| Schematic loading | No placement check; occupancy gauge shows pending (`occupancyReady: false`) |

---

## Draw workflow matrix

Gates: `resolveStageDrawCreateGate` / `resolveDrawCreateGate`. CTAs: `StructurePlacementOperationalSection` (same as operational strip).

### Create (Rerun) block reasons

| Reason | Typical cause | Presentation (`resolveDrawCreateBlockPresentation`) |
|--------|---------------|-----------------------------------------------------|
| `unsupported` | Format not Cup/Groups | quiet |
| `active` | Non-cancelled draw exists | quiet |
| `emptyPool` / `emptyPoolUpstream` | `compositionEntryCount == 0` | inline info / quiet |
| `belowMinimumTeams` | Pool &lt; regulation minimum | inline warning |
| `missingPots` / `groupShape` | Groups config | inline warning |
| `countMismatch` | Pool ≠ Places N (Cup) or ≠ pots×groups | inline warning; strip may hide short text if pool hint shown |
| `directAssignment` / `occupiedSlots` | Cup manual/direct fill | inline warning + action hint |

**Invariant:** `occupiedPlacesCount / placesN` is **not** an input to `resolveDrawCreateGate`. Pool hint uses `compositionEntryCount` vs `placesN`, not schematic occupancy.

### UI states → CTAs (Cup / Groups with draw rules)

| Situation | Primary CTA | Secondary | Notes |
|-----------|-------------|-----------|-------|
| Draw rules not engaged (`!hasDrawRules`) | **Activate** draw (`showActivateDrawCta`) | — | Requires `ReplaceDrawRules`, Cup or Groups |
| Rules engaged, no active draw, gate OK | **Perform draw** / open workflow | Settings, manual place (Cup) | `blockPerformDrawCta` if create blocked and no history |
| Rules engaged, create blocked | **Parity baseline required** — per `createBlockedReason`: either disabled primary (`blockPerformDrawCta`) or workflow + inline caption (`showCreateBlockedCaption`); document in Phase 1, do not choose during refonte | Release if `pickReleasableCancelledDraw` | Release unblocks occupied slots scenario |
| Active draw (`pickActiveDraw`) | **Open** workflow (view) | Deactivate (if `canEditDraw`) | `activeDraw` changes primary label |
| Applied published draw | View / consult | Deactivate per policy | Read-only nuances in dialog |
| Phase Running | Lifecycle in header | Draw consult; config locked per `areDrawRulesLockedByExecution` | — |

### Transitions (behaviour to preserve)

| Event | Expected UI shift |
|-------|-------------------|
| Composition changes after draw configured | Create gate may flip to `countMismatch` / `occupiedSlots`; CTAs disabled with draw messages |
| Draw applied | `stageHasPublishedAppliedDraw`; schematic occupants; create gate often `active` or `occupiedSlots` |
| Draw cancelled with aligned placements | `showReleaseCta` may appear |
| Deactivate draw | Returns to rules-engaged, pool-based gates |
| Schematic fetch completes (Cup) | `drawGateReady` true; gate recomputed with `overviewSlots` |

---

## Tests to extend (Phase 2+)

| Area | Existing | Gap |
|------|----------|-----|
| Occupancy counting | `structurePlaceOccupancy.test.ts` | Per-format entry↔place projection |
| Schematic labels | `phaseSchematic.test.tsx` | — |
| Draw gates | `drawUi.test.ts` | Assert no code path passes `occupiedPlacesCount` into gate |
| Strip CTAs | `StructurePlacementOperationalSection.test.tsx` | Ratio full but create still blocked |
