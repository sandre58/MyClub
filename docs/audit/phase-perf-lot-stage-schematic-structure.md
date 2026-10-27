# Stage schematic — one Structure + draws + slots batch (AUD-0012, partial)

**Registre:** AUD-0012 stays **dette acceptée**. This lot records a measured gain on `GetStageSchematicAsync`. It does not close the finding. Overview and Consultation bundles stay `StageLoadProfile.Full`. Command loads stay Full. Attachment batching is unchanged. `ListSummaryRowsByStageIdsReadOnlyAsync` is unchanged.

## Hypothesis

`GetStageOverviewAsync` already loads every competition stage once with Structure + draws + slots. `GetStageSchematicAsync` still loaded the **target** with those flags, then loaded **peer** stages as a second Structure graph. The assembler needs the target slots, draws, and structure, plus peer rules and the structure graph for WhoFeeds, pending group feeds, and progression origins. It does not read peer draw aggregates. A second `GetByIds` pays `HydrateOrderedCollectionsAsync` again. Peers receiving draws and slots inside the single batch is accepted: one hydration is cheaper than a Structure-only second load.

Measured on the tip before the change (Testcontainers, `Category=PerformanceBaseline`, labels added on that tip):

- `GetStageSchematicAsync` on cup-qf-sf (two stages) was **32** SQL. On groups:running (one stage) it was **20** SQL.
- The same runs kept `GetStageOverviewAsync` at **19** (cup-qf-sf) and **20** (groups:running).

**Expected:** one `GetByIdsReadOnly` of every competition stage with Structure + draws + slots, after a competition-id projection that does not materialize the stage entity. Cup multi-stage SQL should drop. Single-stage SQL should rise by about one command (the projection) because the target was already Structure + draws + slots and there is no second hydration to remove. Match summary rows stay.

## Change

- `GetStageSchematicAsync` resolves the competition id, then loads every competition stage once with `StructureDrawsAndSlotsCapabilities` (`IncludeDraws`, `IncludeSlots`, no penalties).
- Peers are no longer a second Structure load. They receive draws and slots the assembler does not read.
- `ListSummaryRowsByStageIdsReadOnlyAsync` is still called with the competition stage ids.

Out of scope: Overview / Consultation / Attention Full, command Full, the Structure hub, generic attachment batching, Include-graph edits, an asymmetric multi-capability repository API.

## Before / after

Same filter, same fixture, tip `a3ab32c343522f2ef995b223acedfe275b5950b7` versus this lot. Counts are EF `DbCommand` executions per scope (`SqlCommandCounterInterceptor`).

| Scenario | Metric | Before | After |
| --- | --- | ---: | ---: |
| cup-qf-sf | `GetStageSchematicAsync` | 32 | **20** |
| groups:running | `GetStageSchematicAsync` | 20 | 21 |
| cup-qf-sf | `GetStageOverviewAsync` | 19 | 19 |
| groups:running | `GetStageOverviewAsync` | 20 | 20 |
| cup-qf-sf | `GetStructureViewAsync` | 20 | 20 |
| groups:running | `GetStructureViewAsync` | 21 | 21 |
| cup-qf-sf | `GetOverviewViewAsync` | 20 | 20 |

Cup schematic drops by **12** SQL: the peer Structure hydration is gone, and the extra draws and slots on that peer ride inside the one batched load. The remaining command above stage overview (19) is the match-summary query the schematic assembler still needs.

Single-stage schematic moves **20 → 21**. The new id projection costs one command, and there is no second hydration and no dropped penalty include to offset it. That is the measured justification for leaving attachment batching and `ListSummaryRowsByStageIdsReadOnlyAsync` alone.

## Acceptance

| Criterion | Result |
| --- | --- |
| `Category=PerformanceBaseline` | Green, ceilings at 20 (cup stage schematic) and 21 (groups stage schematic) |
| Behaviour | Same schematic DTO inputs (WhoFeeds, slots, connections, pending feeds). Match summary rows are still loaded per competition stage. |
| AUD-0012 | Still dette acceptée |

## References

- `docs/guides/schematic-peer-load-audit.md`
- `docs/audit/phase-perf-lot-stage-overview-structure.md`
- `tests/MyClub.PlayUp.Development.Tests/ReadPerformanceBaselineTests.cs`
