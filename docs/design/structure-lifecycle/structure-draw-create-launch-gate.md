# RunDraw — Application launch gate (design contract)

Status: **implemented** — `StructureDrawCreateLaunchGate` in Application; consumed by `StructureHubNextStepResolver` for `RunDraw` (G2: block codes still **not** on HTTP; PageHead shows nominal step only when gate passes).

## Goal

Expose a single read-side predicate **`DrawCreateLaunchAllowed`** (working name) that answers: “may the organizer **start** a new draw execution on this phase now?” — aligned with what `CreateDraw.Execute` and draw-input configuration allow, **without** copying `resolveStageDrawCreateGate` line-for-line from the SPA.

The SPA gate is a **parity reference**, not the SoT. The SoT is the **stack of rules already enforced on commands**, composed for reads.

## Non-goals

- Dedicated `DrawCreateLaunchAllowed` field on `StructureView` (gate is internal to resolver unless product asks for HTTP exposure).
- Replacing draw UI checklist copy or block-reason presentation in the SPA.
- Treating `DrawExecutionBadge.ToLaunch` as sufficient for `RunDraw` on PageHead.

## Layered predicates (evaluate in order)

| Layer | Rule | Existing Application / Domain anchor |
| ----- | ---- | ------------------------------------ |
| L0 | Draw mechanism engaged | `stage.Regulation.DrawRules != null` → hub `HasDrawRules` |
| L1 | **First draw** window (not “resume draft”) | Domain: no non-cancelled draw (`Stage.CreateDraw`). Read proxy: `DrawExecutionBadge == ToLaunch` (not `InProgress` / `ToApply` / `Applied`) |
| L2 | Format supports draw kind | Cup/Groups with slots or groups form → `DrawResolutionKind.Slot` or `Group`; Championship/Swiss without draw path → **not launchable** |
| L3 | Composition pool | `CompositionEntryCount > 0`; competition `EntryRules.MinimumTeams` satisfied (hub + `competition.Regulation`) |
| L4 | **Group shape** (when kind = Group) | `NumberOfPots >= 2`, `GroupCount >= 1`, `CompositionEntryCount == NumberOfPots * GroupCount` (facts on `StructureStageHubSummaryDto`) |
| L5 | **Slot shape** (when kind = Slot) | `CompositionCapacity` (Places N) > 0, `CompositionEntryCount == CompositionCapacity`, `DirectAssignmentCount == 0` |
| L6 | **Vacant launch grid** | `StageDrawBracketLaunchClassifier.TryClassify` → `BlockingPlaceCount == 0` (same facts as Overview `DrawBracketLaunchFacts` and `StageDrawRerunLaunchGuard` for occupied/reserved/invalid places) |
| L7 | Operational mutability | `StructureOperationalExitEditGuard` would allow draw on stage snapshot (production / exit rules) — mirror read guard used before `CreateDraw` |
| L8 | No obsolete draw blocking consumption | `!StructureDrawConsumptionGate.StageHasObsoleteDrawForConsumption(stage)` (also disables materialize today) |

**Emit `RunDraw` on PageHead only when L0–L8 pass** on exactly one hub stage after priority rules (see resolver contract doc).

### Badge vs gate

| Badge | Meaning | Launch allowed? |
| ----- | ------- | ---------------- |
| `ToLaunch` | DrawRules on, no active non-cancelled draw | **Only if L3–L8 pass** |
| `InProgress` | Draft draw exists | **No** (not “lancer” — organizer continues existing draft; hub should stay silent or future distinct code, **out of RunDraw scope**) |
| `ToApply` | Published, not applied | **ApplyDraw** path, not RunDraw |
| `Applied` | Consumed | Neither |

## Mapping SPA `resolveDrawCreateGate` → Application

| SPA reason | Application layer |
| ---------- | ------------------- |
| `unsupported` | L2 — no Slot/Group kind |
| `active` | L1 — domain one active draw / badge `InProgress` |
| `emptyPool` / `emptyPoolUpstream` | L3 + inbound composition semantics (`IsRootComposition`, inbound band on hub) |
| `belowMinimumTeams` | L3 — `competition.Regulation.EntryRules.MinimumTeams` |
| `missingPots` / `groupShape` | L4 |
| `countMismatch` | L5 — pool vs Places N |
| `directAssignment` | L5 — `DirectAssignmentCount` |
| `occupiedSlots` | L6 — `BlockingPlaceCount > 0` (classifier; supersedes naive slot `entryId` scan) |

**Do not** re-port `schematicCases` fallback into C# for Structure hub: Structure read path already has `TryClassify` with slot feeds (`SlotFeedSnapshotAssembler` / `SlotFeedResolver`). Overview may still ship schematic for other surfaces; hub gate uses **classifier only**.

## Proposed API (step 3 implementation sketch)

```csharp
// Reads — internal, called from StructureViewAssembler + StructureHubNextStepResolver
internal static readonly record struct StructureDrawCreateLaunchGateResult(
    bool Allowed,
    string? BlockReasonCode);

internal static class StructureDrawCreateLaunchGate
{
    public static StructureDrawCreateLaunchGateResult Evaluate(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        StructureDrawExecutionBadge? badge,
        IReadOnlyList<MatchSummaryRow>? stageMatches = null);
}
```

`BlockReasonCode` stable strings for tests/logging (not necessarily exposed on HTTP in v1).

## Wire projection (step 3)

Add to `StructureStageHubSummaryDto` (names TBD):

- `DrawCreateLaunchAllowed: bool`
- optional `DrawCreateLaunchBlockReason: string?` when badge is `ToLaunch` but allowed is false (debug / future drawer only — **not** required on PageHead)

Recompute in `StructureViewAssembler` when building each hub row (same place as badge + classifier inputs).

## Contract tests (step 2 — in repo)

| File | Role |
| ---- | ---- |
| `StructureDrawCreateLaunchGateContractTests.cs` | **Runnable** — asserts classifier + badge + hub facts match gate layers (precursors) |
| `StructureHubNextStepResolverContractTests.cs` | **Skipped** — full S1–S14 + conflict scenarios until resolver exists |

## Open arbitrations (gate-specific)

| # | Question | Recommendation |
| - | -------- | -------------- |
| G1 | Inbound empty pool blocks launch | **Signed yes** — `!IsRootComposition` + `CompositionEntryCount == 0` or inbound `ResolvedCount < ConfiguredVolume` |
| G2 | Block reason on HTTP for PageHead | **Signed no** — `Allowed` only for resolver; diagnostics stay in operational UI |
| G3 | `InProgress` hub step in a later iteration? | **Out of scope** — silence on PageHead for step 3 |

## Related

- PageHead matrix: `structure-pagehead-truth-matrix.md` §7.3
- Resolver scenarios: `structure-hub-next-step-resolver-contract.md`
