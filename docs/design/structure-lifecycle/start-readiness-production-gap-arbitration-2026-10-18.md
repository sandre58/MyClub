# Start readiness — production gap codes (lot C)

**Status:** **Shipped** (2026-10-18).

**Problem:** `StageStartReadinessGuard` treated championship roster gaps and groups-not-ready like unmaterialized confrontations, so the drawer showed « Matchs non créés » when `materializedCount === determinedCount` (e.g. after a withdrawal).

**Decision:** `TryGetProductionStartGap` evaluates in order:

1. `UnmaterializedConfrontations` — eligible confrontations remain unmaterialized.
2. `PhaseRosterIncomplete` — Championship/Swiss roster below Places N (`ChampionshipSwissPhasePlaces.IsRosterComplete`).
3. `GroupsNotReady` — Groups phase without ready group assignment.

**Application mutations:** `Application.StartBlockedPhaseRosterIncomplete`, `Application.StartBlockedGroupsNotReady`.

**SPA:** `problems.lifecycle.*`, `lifecycle.disabled.*`, `graph.issues*` / drawer semantics via existing lifecycle presentation; remediation deep-link → `rail-entries`.

**Recette:** [start-readiness-attention-recette-2026-10-18.md](./start-readiness-attention-recette-2026-10-18.md) — **C-B** (withdrawn entry on root Ch) remains manual DevRunner; guard/read covered by `StartStageReadinessTests` (Qual→Form roster gap → `PhaseRosterIncomplete`, not `UnmaterializedConfrontations`).
