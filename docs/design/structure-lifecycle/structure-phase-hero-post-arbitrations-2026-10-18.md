# Structure phase fiche — post-hero arbitrations (2026-10-18)

**Status:** Product decisions **validated** · **implemented** (2026-10-18).

**Parent:** [structure-phase-hero-stats-matrix.md](./structure-phase-hero-stats-matrix.md) · **Notion:** [Stats hero — réflexion par type × statut](https://app.notion.com/p/3fd75fb92d1081f0af6fc64d3476815c).

---

## Decisions (summary)

| ID | Topic | Decision | Implementation |
| -- | ----- | -------- | -------------- |
| **Swiss hero** | Generated vs planned on wire | **(a) + S1** — generated = hub `matchdayCount` (matchdays with attached matches); planned = `swissRoundCount`; no new DTO field | SPA `swissRoundCounts.ts` + hero selector; **Completed** shows scalar generated only (no `N/K` ratio) |
| **B2 chrome** | « Modifier la forme » placement | **F2** — ghost CTA beside hero stats; **remove** header icon toolbar entry | SPA `StructurePhaseSchematicAndAside` + Design Lab |
| **Places occupation** | Strip vs schematic rules | **O3** — **A:** parity tests FE ↔ `StructurePlaceOccupancyIssues.CountOccupiedPlaces`; **B:** hub `SchematicOccupiedPlaceCount` + strip reads hub | Application + SPA |

---

## Execution order

1. Swiss Completed copy (SPA)
2. B2 F2 hero chrome (SPA + Design Lab)
3. O3-A parity tests (Application + shared vectors)
4. O3-B hub field + strip consumption

---

## Out of scope

- Hero occupancy ratio `X/N` (decision **A** — strip only).
- New HTTP route for hero or occupancy.
- Dedicated `SwissRoundsGenerated` hub field unless product reopens contract #3.
- Materialized match count in hero (decision **E**).

---

## Swiss hub vs Overview (documented divergence)

| Surface | « Round / matchday » count semantics |
| ------- | ------------------------------------ |
| **Per-stage hub** `matchdayCount` | `CountMatchdaysWithAttachedMatches` — progressive Swiss generation |
| **Overview / domain** (e.g. `Stage.Matchdays.Count`) | Configured skeleton matchdays |
| **Championship hero** | `configuredMatchdayCount` — configured only (not hub `matchdayCount`) |

Do not conflate these when auditing reads or adding fields.

---

## O3 convergence

**Target SoT for strip occupied count:** `StructurePlaceOccupancyIssues.CountOccupiedPlaces` projected on `StructureStageHubSummaryDto` as `SchematicOccupiedPlaceCount`. See [truth-sources.md](../structure-phase-refonte/truth-sources.md) and [places-operational-strip-occupancy-arbitration-2026-10-16.md](./places-operational-strip-occupancy-arbitration-2026-10-16.md) §11.

---

## Changelog

| Date | Change |
| ---- | ------ |
| 2026-10-18 | Initial addendum — Swiss S1, B2 F2, O3 A/B |
