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

**Risk:** `composition` uses vertical stack + `joinCompositionPlaceAssignmentSections`; `aside-matchs` is a single Matchs column.

## GradientSeparator

New DS component (`GradientSeparator.tsx`). Composition grid and regulation digest use gradient separators; the old three-column operational strip grid is removed.

## Placement projection

`structureEntryPlacementProjection.ts` — same case filter as `placedEntryIdsFromSchematic`; labels via `structurePlaceLabel.ts`.

## DTO audit (phase 4)

See [structure-hub-dto-audit.md](./structure-hub-dto-audit.md) — hub/stage field usage, dual path/intent projections, removal log.
