# Structure phase refonte — consumer audit (2026-10-14)

Reference for header / strip / separator work. No product behaviour change beyond Composition band-3.

## PanelHead

| Area | Files |
|------|--------|
| Structure fiche | `StructurePhaseFiche.tsx` (Composition sections) |
| Overview | `CompetitionOverviewPage.tsx` |
| Classements | `ClassementsPage.tsx` |
| Match | `MatchHubPage.tsx`, `MatchGoalsPanel.tsx` |
| DS | `OverviewNextAction.tsx`, `OverviewReading.tsx`, `AttentionGroup.tsx` |
| Lab | `LabOverview.tsx`, `LabMatchSheet.tsx`, … |

**Risk:** default density unchanged; Structure uses `density="inset"` and `metaVariant="count"`.

## FluxRail

`structureFluxRails.tsx` — Sorties / aside flux tiles. Not migrated in this tranche (tile head remains rail-local).

## StructureOperationalStrip

| Consumer | `layoutVariant` |
|----------|-----------------|
| `StructurePhaseFiche` | `composition`, `aside-matchs` |
| `StructurePhaseRegulationStrip` | `full` (default) |

**Risk:** `composition` uses vertical stack + `joinCompositionPlaceAssignmentSections`; `full` / `aside-matchs` unchanged.

## GradientSeparator

New DS component (`GradientSeparator.tsx`). Composition grid uses `orientation="responsive"` with `@container phase-composition` rules in `structure-phase-layout.css`. Regulation strip keeps legacy `.structure-operational-strip__separator` until a later dedup.

## Placement projection

`structureEntryPlacementProjection.ts` — same case filter as `placedEntryIdsFromSchematic`; labels via `structurePlaceLabel.ts`.
