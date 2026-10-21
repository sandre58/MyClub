# D2 — StageRepository load shapes & measurement (AUD-0012)

**Date:** 2026-10-22 · **Code anchor:** `src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Repositories/StageRepository.cs`

## 1. Summary

`StageRepository` is the persistence port for the **Stage** aggregate. Command paths load **`StageLoadProfile.Full`** (tracked). Read paths use **`Summary`**, **`Structure`**, or **`Full`**, plus optional flags on `StageReadCapabilities` (draws, slots, penalties).

EF uses **`AsSplitQuery()`** on graph loads to avoid cartesian explosion. **`HydrateOrderedCollectionsAsync`** runs after Structure/Full loads to fix collection ordering and fixture attachments via **additional SQL** (not expressed in the initial `Include` tree).

**D2 conclusion (investigation):** no Include refactor is justified without a **D2-B** before/after on the baselines below. Highest leverage for future work is likely **hydration follow-up queries** and **call-site profile choice**, not deleting Includes blindly.

**Registre:** delivering this investigation **does not** close AUD-0012. The finding stays **dette acceptée** until an optimization decision is taken and documented (or explicitly deferred with updated registre status).

**Interpreting SQL counts:** a higher command count on `Full` vs `Summary` (e.g. 8 → 17 on cup-qf-sf) shows **load-shape cost**, not incorrectness of `Full` for tracked command paths. Latency, allocations, and hydration work may dominate; D2-B must define acceptance criteria per hypothesis.

## 2. Entry points

| API | Tracking | Shape | Notes |
| --- | --- | --- | --- |
| `GetByIdForUpdateAsync` | Yes | Full + hydration | Dominant **command** path (`UseCaseExecutor` helpers) |
| `GetByIdsForUpdateAsync` | Yes | Full batch + per-stage hydration | Competition-ordered replay |
| `GetByIdReadOnlyAsync` | No | Profile + hydration if ≥ Structure | Single-stage reads |
| `GetByIdsReadOnlyAsync` | No | Profile or `StageReadCapabilities` | Bundle loads (`CompetitionReadBundleSpec`) |
| `ListSummariesReadOnlyAsync` | No | Projection only | Cheap; no graph |

## 3. Include graphs (`ApplyProfile`)

### `Summary`

- Stage root only (no collection `Include`s).

### `Structure`

- `Groups`, `Rounds` → `Fixtures`, `Matchdays` → `Fixtures`, `MatchPlacements`, `AffectationAuthoring`, `CompositionEntries`, `FormPathResolutions`.

### `Full`

- Structure set **plus** `Slots`, `DirectAssignments`, `Draws`, `Penalties`, `SwissByeHistory`.

### `StageReadCapabilities` (partial Full)

When profile &lt; Full, `ApplyReadShape` applies the profile first, then optional:

- `IncludeDraws`, `IncludeSlots`, `IncludePenalties` (ordered where applicable).

Used by **`CompetitionReadBundleSpec.Attention`** and **`Structure`** to avoid loading the entire Full graph when the read intent is narrower.

## 4. Post-load hydration (`HydrateOrderedCollectionsAsync`)

Runs when `capabilities.Profile >= Structure`. Extra SQL per stage:

1. `GroupEntryRef` for group entry order  
2. `Round` sort keys  
3. Per round / matchday: fixture sort + **`HydrateAttachmentsAsync`** (`FixtureAttachmentRef` per fixture batch)  
4. Optional draw / penalty reorder (tracked entry metadata)

This explains why **Full** loads can exceed split-query `Include` count substantially on match-heavy stages.

## 5. Application hot paths (call sites)

| Area | Typical stage load |
| --- | --- |
| `UseCaseExecutor.*` commands | `GetByIdForUpdateAsync` → **Full** |
| `GetOverviewViewAsync` / bundles | `CompetitionReadBundleSpec.Overview` → **Full** stages |
| `GetStructureViewAsync` | Structure + draws + slots |
| `GetNeedsAttentionAsync` | Attention capabilities |
| `GetConsultationAsync` | Full stage graph, summary matches |
| `ListMatchesByStageAsync` | **Structure** for one stage |

Read bundling is intentional; AUD-0012 risk is **repeated Full loads** and **hydration cost**, not the existence of Includes per se.

## 6. Measurement protocol (reproducible)

### Infrastructure

- **Project:** `MyClub.PlayUp.Development.Tests`
- **Fixture:** `DevelopmentPostgresFixture` (PostgreSQL 18 Testcontainers, migrated schema)
- **Counter:** `SqlCommandCounterInterceptor` (counts EF `DbCommand` executions per scope)

Each measurement uses a **fresh DI scope** (≈ one HTTP request).

### Scenarios

| Test class | What it measures |
| --- | --- |
| `StageRepositoryLoadBaselineTests` | **Isolated** `IStageRepository` by profile (D2) |
| `ReadPerformanceBaselineTests` | **End-to-end** `UseCaseExecutor` read scenarios (cup-qf-sf, groups:running, champions-league:running) |

### Command

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests/MyClub.PlayUp.Development.Tests.csproj \
  -c Release \
  --filter "Category=PerformanceBaseline" \
  --logger "console;verbosity=detailed"
```

Record per-label SQL counts from **test output** (not only pass/fail). Re-run on the same machine class when comparing D2-B changes.

### Optional: SQL text

For ad-hoc analysis (local only), enable EF logging on the fixture `DbContext` options — do **not** commit connection strings or sensitive logging to CI.

## 7. Baseline snapshot

> **Refresh:** run the command in §6 on `main` after merge and paste representative numbers here.

| Scenario | Operation | SQL commands @ 2026-10-22 (local Testcontainers) |
| --- | --- | ---: |
| cup-qf-sf | `GetByIdReadOnly(Summary)` | 8 |
| cup-qf-sf | `GetByIdReadOnly(Structure)` | 14 |
| cup-qf-sf | `GetByIdReadOnly(Full)` | 17 |
| cup-qf-sf | `GetByIdForUpdate(Full)` | 17 |
| cup-qf-sf | `GetByIdsReadOnly(Full)` N=2 | 18 |
| cup-qf-sf | Attention `StageCapabilities` | 17 |
| cup-qf-sf | `GetOverviewViewAsync` | see `ReadPerformanceBaselineTests` output |
| champions-league:running | `ListMatchesByStageAsync` | see `ReadPerformanceBaselineTests` output |

Existing guard assertions in `ReadPerformanceBaselineTests` encode **regression ceilings** for several read endpoints (e.g. detail projection &lt; 10 SQL).

## 8. Recommendations (post-investigation)

1. **Keep** `AsSplitQuery` on wide graphs until a measured cartesian vs round-trip trade-off exists.  
2. **Prefer** `StageLoadProfile` / `StageReadCapabilities` at call sites (already used in `CompetitionReadBundleSpec`); avoid new `Full` reads without justification.  
3. **Defer** Include trimming; if optimizing, target **hydration** (batch attachment load, reduce per-round queries) with D2-B baselines.  
4. **Do not** merge D2-B without updated rows in §7 and green `PerformanceBaseline` tests.

## 9. References

- `docs/audit/phase-4-lot-d2-plan.md`
- `docs/audit/phase-1-wave-2.md` (wave 2 note on StageRepository)
- `tests/MyClub.PlayUp.Development.Tests/ReadPerformanceBaselineTests.cs`
- `tests/MyClub.PlayUp.Development.Tests/StageRepositoryLoadBaselineTests.cs`
