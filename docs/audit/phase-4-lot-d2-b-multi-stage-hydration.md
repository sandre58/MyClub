# Phase 4 / 7 — Lot D2-B — Multi-stage structure hydration batch

**Hypothesis (AUD-0012):** Multi-id stage loads hydrated structure **per stage**, repeating `GroupEntryRef`, round `SortOrder`, and attachment SQL. One batched hydration pass over all loaded stages removes duplicate round-trips.

**Scope:** `StageRepository` — `GetByIdsReadOnlyAsync`, `GetByIdsForUpdateAsync`, shared `HydrateOrderedCollectionsAsync(IReadOnlyList<Stage>)`. **No** `Include` changes.

## Acceptance (2026-10-22)

| Scenario | Before | After |
| --- | ---: | ---: |
| cup-qf-sf `GetByIdsReadOnly(Full)` N=2 | 18 | **17** |
| cup-qf-sf overview / structure / consultation / attention bundles | 21 | **20** |
| cup-qf-sf `GetMatchHubViewAsync` | 19 | **18** |

Lot doc (phase 7): [`phase-7-lot-perf-aud-0012.md`](phase-7-lot-perf-aud-0012.md).

AUD-0012 remains **dette acceptée**.
