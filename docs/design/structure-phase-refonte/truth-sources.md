# Structure phase fiche refonte — truth sources (Phase 0)

Audit of **authoritative data** for each UI indicator in the band-3 refonte. Scope: PlayUp web Structure hub (`StructurePhaseFiche` and modules it composes today). No new business rules; gaps are labelled **indeterminate** or **pre-existing defect** for separate arbitration.

Related: [state matrix](./state-matrix.md), [actions & permissions inventory](./actions-permissions-inventory.md), [visual chrome & recette](./visual-chrome-recette.md), design mocks in this folder. Stage lifecycle / mutability matrix (pre-implementation): [structure-lifecycle](../structure-lifecycle/README.md).

---

## Business notions (do not merge)

| Notion | Means | Does not prove |
|--------|--------|----------------|
| Team **resolved** | Display name known on schematic / feed | Holds a place |
| **Place occupied** | Schematic slot has team, assignment, or structural feed origin | All product criteria satisfied |
| Team **placed** (future per-entry UI) | Mapped to a nominative place for the format | Match confrontation materialized |
| **Materialized** confrontation | Production match objects exist | Matches played |
| **Draw applied** | Published draw aligned with stage occupancy (`drawUi`) | Same as manual placement |

---

## Traceability matrix (indicators)

For each indicator: **authority**, **rule**, **consumers**, **missing / contradictory data**.

### Composition — Équipes (direct entries)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Team list (names, crests) | Hub `affectationEntryIds` + `data.participants.entries` | Ordered IDs from hub; names from participant catalog; sort by `displayName` | `structurePhaseEntriesRail.tsx` (`RootEntriesRail`) | ID in hub but missing from catalog → row omitted |
| Eyebrow / count (target UI) | Same list length as rendered `composed` | `composedIds.length` after join (not raw ID count if orphans) | Refonte: Composition Équipes section | Orphan IDs → count &lt; `affectationEntryCount` |
| Population meter (U1) | `affectationEntryCount` + inbound configured volume | `meterEntries = fromAffectation + sourcesConfiguredVolume`; compare to `resolvePlacesN(stage)` | `CompositionMeter` in `structurePhaseEntriesRail.tsx` | `places == null` → E4 soft state, places shown as `—` |
| Composition dialog selection | `affectationEntryIds` | Initial set when dialog opens; save via `ReplaceAffectationAuthoring` | `StructureCompositionDialog.tsx` | — |
| Ineligible teams | `affectationIneligibleCount` | Hub assembler: authoring IDs not Active | Rail diagnostics | — |

**Not authoritative for Équipes list:** `compositionEntryIds` / `compositionEntryCount` (runtime pool — see below).

### Composition — Qualifiés (inbound flux)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Section visibility | `groupFeeds(inboundFeeds(...)).length` | Sources block only if `feedGroups.length > 0` | `StructurePhaseFiche.tsx` → `RootEntriesRail` `sources` | **No case found** in hub for “expected inbound group but empty list” without `structureIssues`; if product expects one → **indeterminate** (empty state vs hide) |
| Group volume (eyebrow) | `FeedGroup.volume` | Sum of rule volumes in `structurePhaseFeeds.ts` | `FluxGroupList` in `structureFluxRails.tsx` | — |
| “Resolved” inbound count (target meta) | — | **Not exposed** on hub today | Planned Qualifiés meter | **Indeterminate** — cannot implement faithful meta without new read model or schematic aggregation |
| Edit peer rules | `ExitKindMenu` + `stage.actions` / peer `actions` | Qual if `ReplaceQualificationRules`; Prog if `ReplaceProgressionRules` | `renderAvalSourceAction`, `exitEditOptions` | — |

### Composition — Affectation (places gauge)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Places N | `compositionCapacity` ?? `resolvePlacesN(stage)` | Hub capacity; fallback `structurePlaces.ts` | Fiche, placement strip, draw gate | null → gauge hidden / loading |
| Occupied / Places | `countSchematicOccupiedPlaces(cases, formatKind, placesN)` | Cup: entry round slots; Groups: `GroupPlace`; Champs/Swiss: `RosterPlace`; occupant = entry, assignment, or `feedOrigin` | `structurePlaceOccupancy.ts`, `StructurePhaseFiche` | Schematic loading/error → `occupancyReady: false`, count null |
| Pool filled (draw) | `compositionEntryCount` | **CompositionEntries** size on server | `poolFilled`, `resolveStageDrawCreateGate` | ≠ U1 meter (uses affectation + feeds) — see hypothesis § |
| Remaining places copy | `structureOperationalStripMessages.ts` | From `occupiedPlacesCount` vs `placesN`; `drawLaunchable` (`createGate.ok` when `showDrawCta`) for **0/N** action tone; fiche `awaitingDraw` for `SlotFeedMissing` mask only; D5 when presented — [arbitration](../structure-lifecycle/places-operational-strip-occupancy-arbitration-2026-10-16.md) §5 | Placement strip | Ratio ≠ diagnostic severity |
| Manual placement | `AssignEntryToSlot` + editable Cup places from schematic | `canManualPlace` in Fiche | `PhaseSchematic`, `StructureManualPlacementDialog` | Non-Cup: no per-slot manual in current UI |

**Conflict rule (documented):** schematic occupancy can include **unresolved** feed origins (`feedOrigin` counts as occupied in `casePlaceIsOccupied`). Équipes list can show **resolved** names while gauge counts structural feeds — not a bug if copy distinguishes resolution vs occupancy. Per-entry “placed” check (Phase 2) must **not** treat feed-only occupancy as “team placed” without Phase 0 helper contract ([state matrix](./state-matrix.md)).

### Tirage (draw workflow)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Create / Rerun gate | `resolveStageDrawCreateGate` → `resolveDrawCreateGate` | Pool = `compositionEntryCount` only; Cup: pool == placesN, no direct assignment, no slot occupants; Groups: pots × groups; block reasons fixed priority | `drawUi.ts`, `StructurePhaseFiche`, `StructureDrawDialog` | **Never** uses `occupiedPlacesCount` / occupancy ratio |
| Pool hint gauge | `poolFilled` vs `placesN` | `showPoolHint = showDrawCta && placesN > 0` | Placement section | Visual only; not a gate |
| Block copy | `createBlockedReason` + `resolveDrawCreateBlockPresentation` | i18n `fiche.drawWorkflow.createBlocked.*` | Strip / dialog | `countMismatch` caption suppressed when pool hint shown |
| Active draw | `pickActiveDraw(draws)` | Newest non-cancelled | Fiche, dialog | — |
| Applied | `stageHasPublishedAppliedDraw`, `getDrawUiProjection` | Published + occupancy alignment | Topology badge, dialog | — |
| Release placements | `pickReleasableCancelledDraw` | Cancelled draw aligned with schematic | Release CTA | — |

Tests: `drawUi.test.ts` (`resolveDrawCreateGate`). **Gap:** no dedicated test that occupancy ratio never enables draw CTA (invariant documented; add in Phase 2).

### Lifecycle (header)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Status badge | `stage.status` | `StageStatusBadge` | Fiche header | — |
| Prepare / Start / Complete CTAs | `stage.lifecycle.transition` (+ revert codes) | `StructurePhaseMatchsLifecycleBlock`, `useStructureStageLifecycleMutation` | Fiche header toolbar (`StructurePhaseFicheHeaderLifecycle`) | Disabled reasons from `problems/*` |
| Match progress | `lifecycle.matchProgress` | Shown when Running/Suspended/Completed per `structureMatchsSectionUi.ts` | Matchs column | — |

### Matchs (aside)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Section visibility | `shouldShowMatchsSection(stage)` | Draft hidden unless lifecycle/production rules; Completed needs matches or progress | Fiche operational bar | — |
| Materialize / dematerialize | `stage.materialize`, `stage.dematerialize` | `classifyMaterializePresentation`, mutations in strip | `StructurePhaseOperationalStrip.tsx` | — |
| Production gauge | `materialize.determinedCount`, `materializedCount` | `structureMaterializeCopy.ts` | Matchs cluster | Frozen running state hides primary |

### Flux (sorties / attribution)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Sorties visibility | `outboundGroups`, `canCreateExit`, issues | `showSortiesRail` | Fiche hero out column | — |
| Attribution visibility | `placementAwards`, `canEditPlacement` | KO/Cup topology | `showAttributionRail` | — |
| Create exit | `data.stages.length >= 2` && (Qual or Prog action) | `canCreateExit` | Sorties edit control | — |

### Règlement (summary strip)

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Match / standing / tie panels | Hub stage fields + `regulationRulePanels.tsx` | Same inputs as `StructurePhaseRegulationStrip` | Fiche foot compact strip (no tile) | Refonte must project, not re-derive rules |
| Edit / rebind | `canEditMatch`, `canEditStanding`, `canEditTie`, `canRebind` | Opens section editors or rebind flow | Strip callbacks | — |

### Schéma

| Indicator | Authority | Rule | Consumers | Missing / conflict |
|-----------|-----------|------|-----------|-------------------|
| Cases / occupants | GET schematic (`schematicQuery`) | `phaseSchematic.tsx` labels: entry, assignment, feed | Viewport in Fiche | Error → unavailable label |
| Cup production overlay | `stage.materialize.confrontations` | Pair eligibility for schematic chrome | `cupPairProductionStates` | Cup only |

---

## CompositionEntries vs AffectationAuthoring vs materialization

**Status: confirmed intentional split (not a refonte fix).**

| Layer | Domain | Hub fields | Used for |
|-------|--------|------------|----------|
| Authoring | `Stage.AffectationAuthoring` | `affectationEntry*` | Équipes list, composition dialog, U1 meter numerator (partial) |
| Runtime pool | `Stage.CompositionEntries` | `compositionEntry*` | Draw pool (`compositionEntryCount`), `DrawInputsFactory` |
| Materialize roster | `CompositionEntries` ∩ Active entries | (no separate list) | `ChampionshipSwissPhasePlaces.ResolveCompositionRoster`, `MaterializeMatches` |

Domain sync (`Stage.ReplaceAffectationAuthoring`): diff-sync into `CompositionEntries`; **apply-resolved** entries can remain in `CompositionEntries` when removed from authoring.

**Observed UI consequences (pre-existing):**

1. **Équipes count vs draw pool:** list follows `affectationEntryIds`; draw gate follows `compositionEntryCount`. After draw apply or upstream resolution, composition can be **larger** than authoring list.
2. **U1 meter vs draw pool:** meter uses `affectationEntryCount + inboundPopulationConfiguredVolume`, not `compositionEntryCount`. Feeds promise volume before entries land in composition.
3. **Materialize vs Équipes list:** materialize uses active `CompositionEntries`; UI list uses affectation authoring — misalignment possible when composition has non-authoring members.

**Refonte rule:** do not “fix” alignment in layout PRs; surface indicators only from the authorities above. Phase 2 per-entry placement helper must document per-format mapping ([state matrix](./state-matrix.md)).

---

## Deep links & scroll targets (unchanged)

Contract: `structureNavigation.ts` — query params `stage`, `section`, `round`, `compose`, `focus`, `manual`, `draw`. Focus IDs: `operational-placement`, `rail-exits`, `rail-attribution`, `rail-entries`. Regulation → Structure links must keep this contract.

---

## Container breakpoints (measured baseline)

Values already in CSS (Phase 1 should add `container-type` on fiche/composition per plan; thresholds below anchor recette).

| Context | Condition | Effect | Source |
|---------|-----------|--------|--------|
| Hub master–detail | viewport `≥ 52rem` | Topology ~`0.34fr`, fiche `1fr` | `structure-hub.css` |
| Hub master–detail | viewport `&lt; 52rem` | Single column stack | `structure-hub.css` |
| Fiche triptych | viewport `≤ 56rem` | In \| schematic \| out → single column | `structure-dialogs.css` |
| Operational strip | `@container operational-bar` `≤ 42rem` | Strip sections stack | `structure-operational.css` |
| Operational dual cluster | `@container operational-bar` `≥ 34rem` | Placement + draw side-by-side | `structure-operational.css` |
| Topology tile facts | `@container structure-topology` `≤ 20rem` / `15.5rem` | Wrap / stack facts | `structure-hub.css` |

**Proposed composition band thresholds (Phase 1 — container on `.structure-fiche` or composition tile):**

| Band | Container width | Layout target |
|------|-----------------|---------------|
| Large | `≥ 56rem` | 3 columns: Équipes \| Qualifiés \| Affectation |
| Medium | `34rem – 55.99rem` | 2 columns; Affectation full width below or beside per mock |
| Narrow | `&lt; 34rem` | Stack Équipes → Qualifiés → Affectation → schematic → aside |

**Reference widths for visual recette:** full viewport with hub split ≈ fiche `0.66 × (viewport − 14rem − gap)`; at `52rem` viewport ≈ **34rem** fiche (narrow band); at `80rem` viewport ≈ **42rem** fiche (medium).

---

## Scenarios not coverable with current hub data

| Scenario | Why | Refonte handling |
|----------|-----|------------------|
| Qualifiés “resolved / unresolved” aggregate meter | No hub field or stable client projection | **Indeterminate** — omit meta or link to schematic only after product decision |
| Per-entry green “placed” check | No `showPerEntryPlacement` / `structureEntryPlacementProjection` | Phase 2 only if contract validated; else gauge-only |
| Parity Équipes count vs `compositionEntryCount` when they differ | By design after apply | Show authoring list; do not infer from composition count |
| Draw CTA from full places gauge | Not in code | Forbidden — keep gates in `drawUi` |

---

## Phase 0 exit gate

Sign-off when all are true:

- [x] Truth matrix completed for refonte indicators (this document).
- [x] State dimensions + draw matrix documented ([state-matrix](./state-matrix.md)).
- [x] Actions/permissions inventory for fiche ([inventory](./actions-permissions-inventory.md)).
- [x] Composition vs materialization hypothesis **confirmed** (split collections); no new business decision required for layout.
- [x] Conflicts have a single rule or **indeterminate** + UI fallback (no per-entry check until Phase 2).
- [x] Container baseline measured; Phase 1 thresholds proposed.
- [x] **Human sign-off** — Qualifiés section remains **hidden** when `feedGroups.length === 0` (current behaviour). No code counterexample in audit; if product finds a gap later → separate arbitration (2026-10-14).

**Phase 0 full sign-off** (pending): container widths **measured in browser** on hub — procedure and formula baseline in [container-measurements](./container-measurements.md); **draw blocked CTA baseline** in [draw-blocked-baseline](./draw-blocked-baseline.md). Human sign-off table in container-measurements still open until DevTools pass.

**No new business decision required** to start Phase 1 skeleton layout, provided Phase 2 defers nominative placement checks until helper contract is written.
