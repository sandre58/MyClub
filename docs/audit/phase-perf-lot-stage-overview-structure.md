# Stage overview — Structure + draws + slots (AUD-0012, partial)

**Registre:** AUD-0012 stays **dette acceptée**. This lot records a measured gain on one read. It does not close the finding. Overview and Consultation bundles stay `StageLoadProfile.Full`. Command loads stay Full. Attachment batching is unchanged.

## Hypothesis

`GetStageOverviewAsync` was the Structure-adjacent read that still loaded its **target** stage as `StageLoadProfile.Full`, then loaded peer stages as a **second** Structure graph. The assembler needs rounds, fixtures, groups, slots, draws, and bracket pairs (owned collections already come with any profile). It does not read penalties. The Structure hub (`GetStructureViewAsync` / `CompetitionReadBundleSpec.Structure`) already uses Structure + draws + slots — this lot does not convert that hub.

Measured on the tip before the change (Testcontainers, `Category=PerformanceBaseline`):

- Structure hydration is real: `GetByIdReadOnly(Summary)` **8** SQL, `GetByIdReadOnly(Structure)` **14** SQL (**+6** for includes plus `HydrateOrderedCollectionsAsync`).
- Structure + draws + slots is **16** SQL; Full is **17** (the extra command is penalties). Attention (those flags plus penalties) matches Full at **17**.
- `GetByIdsReadOnly(Full)` for two cup stages is **17**, because hydration is already batched inside one call. Two separate loads therefore pay hydration twice.
- `GetStageOverviewAsync` on cup-qf-sf (two stages) was **32** SQL. On groups:running (one stage) it was **20** SQL.
- `GetStructureViewAsync` was already **20** (cup-qf-sf) and **21** (groups:running), level with Overview. Not a Full graph.

**Expected:** one `GetByIdsReadOnly` of every competition stage with Structure + draws + slots (same flags as the hub and the schematic target), after a competition-id projection that does not materialize the stage entity (owned collections would otherwise add the Summary-shaped 8 SQL). Cup multi-stage SQL should drop. Single-stage SQL should stay about flat (projection replaces the dropped penalty query). Hub and Overview SQL should not move.

## Change

- `GetStageOverviewAsync` resolves the competition id, then loads every competition stage once with `StructureDrawsAndSlotsCapabilities` (`IncludeDraws`, `IncludeSlots`, no penalties).
- Peers are no longer a second Structure load. The target is no longer Full.
- `GetCompetitionIdReadOnlyAsync` is a projection (`SELECT` of `CompetitionId` only).

Out of scope: Overview / Consultation Full, command Full, generic attachment batching, Include-graph edits, hub profile changes.

## Before / after

Same filter, same fixture, tip `8b42e85e71f1076f6222552f72e5651bf1c4fd1b` versus this lot. Counts are EF `DbCommand` executions per scope (`SqlCommandCounterInterceptor`).

| Scenario | Metric | Before | After |
| --- | --- | ---: | ---: |
| cup-qf-sf | `GetStageOverviewAsync` | 32 | **19** |
| groups:running | `GetStageOverviewAsync` | 20 | 20 |
| cup-qf-sf | `GetStructureViewAsync` | 20 | 20 |
| groups:running | `GetStructureViewAsync` | 21 | 21 |
| cup-qf-sf | `GetOverviewViewAsync` | 20 | 20 |
| cup-qf-sf | `GetByIdReadOnly(Structure)` | 14 | 14 |
| cup-qf-sf | Structure hydration surplus over Summary | 6 | 6 |
| cup-qf-sf | `GetByIdsReadOnly(Structure + Draws + Slots)` | 16 | 16 |
| cup-qf-sf | `GetByIdReadOnly(Full)` | 17 | 17 |
| groups:running / champions-league | `ListMatchesByStage` | 18 | 18 |

Single-stage overview stays **20**: the new id projection costs one command and dropping penalties saves one, and there is no second hydration to remove. That is the measured justification for leaving attachment batching alone — Structure hydration on one stage is still **+6** SQL over Summary, and this call site no longer pays it twice.

## Acceptance

| Criterion | Result |
| --- | --- |
| `Category=PerformanceBaseline` | Green, ceilings at 19 (cup stage overview) and 20 (groups stage overview) |
| Behaviour | Same overview DTO inputs (slots, draws, rounds, groups, bracket pairs, feed classification). Penalties are not part of `StageOverviewDto`. |
| AUD-0012 | Still dette acceptée |

## References

- `docs/audit/phase-4-d2-stage-repository-investigation.md` §5–§7
- `tests/MyClub.PlayUp.Development.Tests/ReadPerformanceBaselineTests.cs`
- `tests/MyClub.PlayUp.Development.Tests/StageRepositoryLoadBaselineTests.cs`
