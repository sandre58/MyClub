# RunDraw — Application launch gate (design contract)

Status: **implemented** — `StructureDrawCreateLaunchGate` in Application. Each hub row exposes `drawCreateLaunch { allowed, blockReasonCode }`. PageHead `RunDraw` uses that same projection (nominal step only when `allowed` is true). The SPA displays the field.

## Goal

Expose a single read-side predicate **`DrawCreateLaunchAllowed`** (working name) that answers: “may the organizer **start** a new draw execution on this phase now?” — aligned with what `CreateDraw.Execute` and draw-input configuration allow, **without** copying `resolveStageDrawCreateGate` line-for-line from the SPA.

The SPA gate is a **parity reference**, not the SoT. The SoT is the **stack of rules already enforced on commands**, composed for reads.

## Non-goals

- Replacing draw UI checklist copy. Block-reason presentation reuses existing fiche labels for the server `blockReasonCode`.
- Treating `DrawExecutionBadge.ToLaunch` as sufficient for `RunDraw` on PageHead.

## Layered predicates (evaluate in order)

| Layer | Rule | Existing Application / Domain anchor |
| ----- | ---- | ------------------------------------ |
| L0 | Draw mechanism engaged | `stage.Regulation.DrawRules != null` → hub `HasDrawRules` |
| L1 | **First draw** window (not “resume draft”) | Domain: no non-cancelled draw (`Stage.CreateDraw`). Read proxy: `DrawExecutionBadge == ToLaunch` (not `InProgress` / `ToApply` / `Applied`) |
| L2 | Format supports draw kind | Cup/Groups with slots or groups form → `DrawResolutionKind.Slot` or `Group`; Championship/Swiss without draw path → **not launchable** |
| L3 | Composition pool | `CompositionEntryCount > 0`; competition `EntryRules.MinimumTeams` satisfied (hub + `competition.Regulation`) |
| L4 | **Group shape** (when kind = Group) | `NumberOfPots >= 2`, `GroupCount >= 1`, `CompositionEntryCount == NumberOfPots * GroupCount` (facts on `StructureStageHubSummaryDto`) |
| L5 | **Slot partial scope** (when kind = Slot) | `SlotPartialDraw`: free slots (no occupant, feed Missing) and unplaced composition teams must be a bijection. `NoFreeSlot` when nothing is free (checked first), `NotEnoughTeams` when unplaced teams are fewer, `TooManyTeams` when they are more. Occupants and reserved or invalid feeds are not free targets. |
| L6 | **Vacant launch grid** (Group only) | `StageDrawBracketLaunchClassifier` → `BlockingPlaceCount == 0`. Slot no longer uses this denial: a partial grid is launchable when L5 passes. Rerun and Redo still require a vacant grid. |
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
| `countMismatch` | Kept for older payloads. Slot launches no longer emit `PlacesCountMismatch`. |
| `directAssignment` | Kept for older payloads. Slot launches no longer emit `DirectAssignmentPresent`. Rerun and Redo still refuse a direct assignment. |
| `occupiedSlots` | L6 — Group `BlockingPlaceCount > 0` (`BracketPlacesOccupied`) |
| `noFreeSlot` / `notEnoughTeams` / `tooManyTeams` | L5 — Slot partial scope (`NoFreeSlot`, `NotEnoughTeams`, `TooManyTeams`) |

**Do not** re-port `schematicCases` fallback into C# for Structure hub: Structure read path already has `TryClassify` with slot feeds (`SlotFeedSnapshotAssembler` / `SlotFeedResolver`). Overview may still ship schematic for other surfaces; hub gate uses **classifier only**.

## Implementation

`StructureDrawCreateLaunchGate.Evaluate` returns `StructureDrawCreateLaunchDto` (`Allowed`, `BlockReasonCode`). `StructureViewAssembler` projects that value onto each hub row. `StructureHubNextStepResolver` reads `hub.DrawCreateLaunch.Allowed` and does not evaluate the gate again.

## Wire projection

`StructureStageHubSummaryDto.DrawCreateLaunch` (`drawCreateLaunch` on the wire):

- `allowed: bool`
- `blockReasonCode: string | null` when `allowed` is false

Projected in `StructureViewAssembler` when building each hub row. `StructureHubNextStepResolver` emits `RunDraw` only when `allowed` is true.

## Contract tests (step 2 — in repo)

| File | Role |
| ---- | ---- |
| `StructureDrawCreateLaunchGateContractTests.cs` | **Runnable** — asserts classifier + badge + hub facts match gate layers (precursors) |
| `StructureHubNextStepResolverContractTests.cs` | **Skipped** — full S1–S14 + conflict scenarios until resolver exists |

## Open arbitrations (gate-specific)

| # | Question | Recommendation |
| - | -------- | -------------- |
| G1 | Inbound empty pool blocks launch | **Signed yes** — `!IsRootComposition` + `CompositionEntryCount == 0` or inbound `ResolvedCount < ConfiguredVolume` |
| G2 | Block reason on the phase hub | **Exposed** — `drawCreateLaunch.blockReasonCode` for the fiche and draw dialog. PageHead still shows `RunDraw` only when `allowed` is true |
| G3 | `InProgress` hub step in a later iteration? | **Out of scope** — silence on PageHead for step 3 |

## Related

- PageHead matrix: `structure-pagehead-truth-matrix.md` §7.3
- Resolver scenarios: `structure-hub-next-step-resolver-contract.md`
