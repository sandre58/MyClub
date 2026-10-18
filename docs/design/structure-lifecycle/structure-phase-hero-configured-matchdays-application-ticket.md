# Application ticket — Expose configured Championship matchday count

Status: **implemented** (Application contract — 2026-10-18).  
Parent: [structure-phase-hero-stats-matrix.md](./structure-phase-hero-stats-matrix.md).

---

## Context

The Structure phase fiche hero must show **configured** matchdays, independent of whether matches are attached.

Today, `StructureStageHubSummaryDto.MatchdayCount` is **matchdays with at least one attached match**, assembled via `CountMatchdaysWithAttachedMatches` in `StructureViewAssembler`:

```csharp
stage.Matchdays.Count(matchday =>
    matchday.Fixtures.Any(fixture => fixture.MatchIds.Count > 0));
```

That counter remains required for exploitation-oriented consumers. It must **not** be repurposed for the hero.

## Objective

Expose, **per stage**, the configured matchday count without changing existing field semantics.

## Scope

- Add `ConfiguredMatchdayCount` to `StructureStageHubSummaryDto` (name subject to review).
- Set it to `stage.Matchdays.Count`.
- Document semantics in XML (`StructureViewDto.cs` / record param docs).
- Keep `MatchdayCount` and current assembly logic unchanged.
- Add Application tests covering configured vs attached-match counts.

## Acceptance criteria

1. `ConfiguredMatchdayCount` equals the number of matchdays on the stage skeleton.
2. Correct when **no** matches are attached to any matchday.
3. `MatchdayCount` keeps current semantics (matchdays with attached matches).
4. No existing consumer sees a changed meaning for `MatchdayCount`.
5. Tests explicitly demonstrate the distinction between the two counters.
6. No equivalent business calculation added on the frontend in this ticket.
7. No SPA presentation changes in this ticket.

## Minimal test cases

### Case 1 — Skeleton matchdays, no attached matches

**Given** a Championship stage with 5 configured matchdays and no attached matches.

**When** the stage hub summary is assembled.

**Then**

- `ConfiguredMatchdayCount == 5`
- `MatchdayCount == 0`

### Case 2 — Partially exploited matchdays

**Given** a stage with 5 configured matchdays, exactly 2 matchdays having at least one fixture with `MatchIds.Count > 0`.

**When** the stage hub summary is assembled.

**Then**

- `ConfiguredMatchdayCount == 5`
- `MatchdayCount == 2`

### Case 3 — No configured matchdays

**Given** a stage with no matchdays on the skeleton.

**When** the stage hub summary is assembled.

**Then**

- `ConfiguredMatchdayCount == 0`
- `MatchdayCount == 0`

### Case 4 — Multiple stages

**Given** several stages with different configured matchday counts.

**When** hub summaries are assembled.

**Then** each `ConfiguredMatchdayCount` matches that stage only (not primary-stage topology).

## Out of scope

- Changing `MatchdayCount` semantics or `CountMatchdaysWithAttachedMatches`.
- Changing `StructureTopologySummaryDto`.
- Hero / SPA components.
- Swiss rounds hero semantics.
- Places strip occupancy.
- `CompositionCapacity` contract (unless an unrelated defect is found — report, do not expand scope).

## Follow-up

After merge: SPA ticket — hero Championship reads `configuredMatchdayCount`, hide when `0`. Swiss hero remains blocked on separate contract arbitration.

## Expected verification

- Targeted Application tests.
- Grep / review of existing `MatchdayCount` consumers (hub row).
- Host JSON exposes the new property (camelCase `configuredMatchdayCount`); OpenAPI / `structure.ts` update may land in SPA ticket or a thin contract sync — **not** hero UI.

---

## Pre-flight verification (2026-10-18)

### 1. Data path for phase fiche (no `StructureTopologySummaryDto` change)

| Step | Source |
| ---- | ------ |
| HTTP | `GET /competitions/{id}/structure` → `StructureViewDto` |
| Per-phase row | `StructureViewDto.Stages[]` → `StructureStageHubSummaryDto` |
| SPA | `StructureView.stages` → `StructurePhaseFiche` selected `stage` (`src/PlayUp/web/src/types/structure.ts`) |

The fiche does **not** use `StructureView.structure.matchdayCount` (primary topology summary) for per-phase hero. Adding the field on **`StructureStageHubSummaryDto`** is the correct contract surface.

### 2. “Attached match” definition for tests

Authoritative rule: `CountMatchdaysWithAttachedMatches` (above). A matchday counts toward `MatchdayCount` only if **any** fixture on that matchday has **at least one** `MatchId`.

Do not use `MatchCount` or materialization alone as a proxy in new tests.

### 3. Existing test anchor (Case 1)

`StructureMaterializeAssemblerTests.Hub_championship_ready_before_materialize_exposes_zero_matchdays` already asserts:

- `hub.MatchdayCount == 0`
- `stage.Matchdays.Count > 0`

Extend or add a sibling test asserting `ConfiguredMatchdayCount == stage.Matchdays.Count` (expected 5 or whatever the championship skeleton produces after `PreparePrimaryStageForMaterialization`).

### 4. Case 2 implementation hint

Build domain state with 5 `AddMatchday` calls, attach matches to fixtures on **two** matchdays only (same pattern as `ScenarioAndTemplateIntegrationTests` with `matchSummariesByStage` for `MatchdayCount > 0`), then assert both counters.

### 5. `MatchdayCount` consumers to regression-check (hub row)

- Application reads / lifecycle / materialize tests asserting `hub.MatchdayCount`.
- SPA: `matchdayCount` on `StructureStageHubSummary` (Swiss guidance, topology cards, etc.) — behaviour must stay **attached-match** semantics until a deliberate migration.

---

## Changelog

| Date | Change |
| ---- | ------ |
| 2026-10-18 | Ticket drafted; pre-flight verification added |
| 2026-10-18 | `ConfiguredMatchdayCount` on `StructureStageHubSummaryDto`; tests in `StructureViewAssemblerConfiguredMatchdayCountTests` |
