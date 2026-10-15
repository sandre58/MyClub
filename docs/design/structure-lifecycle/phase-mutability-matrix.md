# Phase mutability — diagnostic & command matrix

**Version:** §11A + **§11B signed 2026-10-15**; arbitrations #1–#3 closed; factual conditions verified against repository.  
**Audience:** product sign-off, then Agent implementation.  
**Rule:** §5.2 = **target policy**; §5 = **current code**; §12 = **gaps** to implement.

---

## 1. Product decision (confirmed)

> A phase **Ready** has **fixed form** and **fixed game regulation** (match, standing, confrontation/tie), but may still receive **controlled operational adjustments** (entries, exits, attribution, placement, draw). Those adjustments must neither silently invalidate **Ready downstream consumers**, nor desynchronize **structure from production**. **Materialization** remains a strict validation barrier.

| ID | Rule |
|----|------|
| **D1** | Upstream exit edits allowed in Ready only if no **Ready** consumer is made inconsistent; otherwise **block** and name phases to revert to Draft (**no auto-cascade**). |
| **D2** | When **any match is materialized** on the stage, deny operational/structural edits that would desync production (explicit allow-list per capability). |
| **D3** | **Integrity** stays blocking for Prepare and is revalidated after relevant mutations; **operational completeness** may warn in Draft and block at Materialize; **business (M)** codes classified explicitly (see Open #2). |
| **D4** | Frozen in Ready: match / standing / confrontation rules. **Draw** remains operational (`DrawRules` + workflow). |

---

## 2. Prepare vs Ready (do not conflate)

| Notion | Meaning today (code) | Target policy |
|--------|----------------------|---------------|
| **Stage status `Draft`** | Structure edits allowed via `EnsureStructureMutable()` | Unchanged for **form**; drawer severities per §5.2 (population signals → warning) |
| **Stage status `Ready`** | Domain `EnsureStructureMutable()` **rejects** almost all structure ops; hub exposes `RenameStage` only | Allow **operational** capabilities when **no production** (D2) + D1 on exits |
| **Prepare** | Command `PrepareStageAsync` → `PrepareStage.Execute` — **one-shot Draft → Ready** | Stays **transition gate** only; **not** continuous revalidation while editing in Draft |
| **Post-mutation revalidation** | Not implemented as commands; next read refreshes `structureIssues` | **New (future):** integrity (I) + D1 on operational mutations in Ready — **after** matrix sign-off |

**Prepare command path:** `StructurePreparePlacesGate.Ensure` → `PrepareStage.Execute` (slot feed resolution when slots exist) → `target.Prepare(clock)`.  
**Prepare read path:** `StructureStageLifecycleAssembler.ResolvePrepareTransition` mirrors gates (graph, places, attribution, `CollectPrepareGateCodes`).

---

## 3. Capability model (target — not implemented)

Capabilities are **orthogonal axes**; avoid a single guard “match exists ⇒ deny all” (Swiss progressive is the counter-example).

| Capability | Typical formats | Draft | Ready, no attached matches | Production present | Running Swiss |
|------------|-----------------|-------|----------------------------|--------------------|---------------|
| `structuralAuthoring` | All | Allow | **Deny** (D4 form) | Deny | Deny (non-Swiss structure) |
| `gameRegulation` | All | Allow | **Deny** (D4) | Deny | Standing exception after Start (existing) |
| `operationalEntries` | All | Allow | **Allow** (target) | **Deny** (D2) | **Deny** (D2) |
| `operationalExits` | All | Allow | **Allow** + D1 (target) | Deny | Deny |
| `operationalAttribution` | KO/Cup | Allow | Allow (target) | Deny | Deny |
| `operationalPlacement` | Cup (+ gates) | Allow | Allow (target) | Deny (+ F1 snapshots today) | Deny |
| `operationalDraw` | Cup/Groups | Allow | Allow (target) | Deny | Deny |
| `materializeBatch` | Ch/G/Cup | Deny (stage not Ready) | Allow if gates pass | N/A (partial state) | Not used (Swiss) |
| `swissProgressiveRound` | Swiss | N/A | N/A | N/A | **Allow** when `GenerateNextRound` preconditions pass |
| `dematerialize` | All | Via `DematerializationCapability` | Same | Scoped removal | **Swiss last round only** (`DematerializeSwissLastRound`) |

---

## 4. Diagnostics — producers

### 4.1 `CompetitionFlowAnalyzer` (facts)

| Code | Family | Producer method(s) | XML / comment |
|------|--------|-------------------|---------------|
| `SlotFeedMissing` | Feed | `Analyze` → feed slice | No inbound WhoFeeds for required Cup slot |
| `SlotFeedInvalid` | Feed | Feed slice | Same-kind duplication |
| `SlotFeedMultiple` | Feed | Feed slice | Cross-kind rivalry |
| `CycleDetected` | Topology | `AnalyzeTopology` | Qual/Prog cycle |
| `NonRootUnfed` | Topology | `AnalyzeTopology` | Non-root, no inbound, no affectation (signal) |
| `RoutingNondeterminism` | Topology | `AnalyzeTopology` | Ambiguity outside WhoFeeds |
| `OverCapacityEdge` | Capacity | `AnalyzeCapacity` | Inbound + affectation > places |
| `OverExtraction` | Capacity | `AnalyzeCapacity` | Outbound extraction > source capacity |
| `SourcePopulationShortfall` | Capacity | `AnalyzeCapacity` | Source population vs extraction (signal) |
| `PopulationShortfall` | Capacity | `AnalyzeCapacity` + **synthesized** `StructurePopulationVolume` (U1) | Destination Places N not fully promised |
| `AuthoredAwardUnreachable` | Result | `AnalyzeResult` | Placement award path cannot be produced |
| `ExpectedRankUnreachable` | Result | `AnalyzeResult` | Expected rank cannot be produced |
| `ConfrontationUndecidable` | Execution | `AnalyzeExecution` | TieFormat cannot always decide draw |

Source: [`CompetitionFlowAnalyzer.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs).

### 4.2 Hub-only authoring issues

| Code | Producer |
|------|----------|
| `DanglingQualificationTarget` | `StructureViewAssembler.BuildStructureIssues` |
| `MissingQualificationDestinationSlot` | Same |
| `MissingQualificationDestinationGroup` | Same |
| `DanglingProgressionTarget` | Same |
| `MissingProgressionDestinationSlot` | Same |
| `MissingProgressionDestinationGroup` | Same |

`BuildStructureIssues` also merges **flow** diagnostics per stage and calls `StructurePlaceOccupancyIssues.AppendSchematicPlaceOccupancyIssue` (may emit `SlotFeedMissing`).

### 4.3 Competition readiness blockers (not per-stage `structureIssues`)

| Code | Producer |
|------|----------|
| `InsufficientParticipants` | `StructureViewAssembler.BuildReadiness` |
| `MissingStage` | Same |
| `MissingStructure` | Same |
| `StructureGraphInvalid` | Any hub issue where `IsStructureGraphGateIssue` |
| `ConfrontationUndecidable` | Hub contains code (also Prepare gate) |
| `OverCapacityEdge` | Hub contains code (also Prepare gate) |
| `OverExtraction` | Hub contains code (also Prepare gate) |
| `CupBracketInvalid` | Cup places not power of two |
| `MissingPotRules` | Groups + draw engaged, no pots |

### 4.4 Materialize read disabled reasons

| Code | Producer |
|------|----------|
| `StructureNotMutable` | `StructureMaterializeAssembler` (wrong status/competition) |
| `PhaseRosterIncomplete` | Championship roster gate |
| `GroupsNotReady` | Groups assignment gate |
| `NoDeterminedConfrontations` | Cup/Ch/G eligibility |
| `NoEligibleConfrontations` | Same |

### 4.5 Lifecycle transition disabled reasons

| Code | Producer |
|------|----------|
| `CompetitionClosed` | `StructureStageLifecycleAssembler` |
| `ConstructionFrozen` | Lone Draft/Ready phase while competition Running |
| `StageNotReady` | `DomainAllowsPrepare` false |
| `StructureGraphInvalid` | Prepare read |
| `Entries` / `Places` / `Draw` / `Matches` / `Exits` / `Attribution` | Prepare checklist codes |
| `ProductionPresent` | Revert to Draft |
| `UnmaterializedConfrontations` | Start (`StageStartReadinessGuard`) |
| `InitialRoundNotPlayable` | Start (Cup) |
| `MatchLifecycleStarted` / `MatchLiveInProgress` | Revert / Suspend |

---

## 5. Diagnostics — consumers (current)

Legend: **Y** = blocks or maps to blocking; **W** = warning in drawer/attention; **S** = signal only; **—** = not used; **H** = hidden/filtered in some UI contexts.

| Code | `IsStructureGraphGate` | `IsPrepareGate` | Prepare cmd extras | Prepare read | Drawer web (`structureDiagnosticSeverity`) | Attention host (`StructurePresentedAttentionCollector`) | Materialize read | Materialize cmd | Start |
|------|------------------------|-----------------|-------------------|--------------|---------------------------------------------|------------------------------------------------------|------------------|-----------------|-------|
| `SlotFeedInvalid` | Y | Y | Slot resolver | Y | blocking (default) | blocking | — | — | — |
| `SlotFeedMultiple` | Y | Y | Slot resolver | Y | blocking | blocking | — | — | — |
| `SlotFeedMissing` | — | **—** | not blocked at Prepare (D3 occupancy / missing feed config) | **—** | attention in Draft (D5) | **H** if draw awaiting | indirect (Cup places) | materialize gates | — |
| `CycleDetected` | Y | Y | — | Y | blocking | blocking | — | — | — |
| `RoutingNondeterminism` | Y | Y | — | Y | blocking | blocking | — | — | — |
| `OverCapacityEdge` | — | Y | — | Y | **W** | **W** | — | — | — |
| `OverExtraction` | — | Y | — | Y | blocking | blocking | — | — | — |
| `ConfrontationUndecidable` | — | Y | — | Y + readiness blocker | blocking | blocking | — | — | — |
| `NonRootUnfed` | — | — | — | — | **W** | **W** | — | — | — |
| `SourcePopulationShortfall` | — | — | — | — | blocking (default) | blocking | — | — | — |
| `PopulationShortfall` | — | — | — | — | blocking (default) | blocking (synth U1) | Ch `PhaseRosterIncomplete` | roster checks | — |
| `AuthoredAwardUnreachable` | — | — | `StructurePrepareAttributionGate` | **Y** (Cup) | **W** | **W** | — | — | — |
| `ExpectedRankUnreachable` | — | — | — | — | **W** | **W** (suppressed if Authored present) | — | — | — |
| Dangling/Missing * | **Y** (default true) | — | `PrepareStage` outbound checks | indirect | blocking | blocking | — | — | — |

**Known presentation gap:** `AuthoredAwardUnreachable` is **warning** in SPA/host severity maps but **blocks Prepare** on non-classifying phases ([`StructurePrepareAttributionTests` R3](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePrepareAttributionTests.cs)).

### 5.2 Signed target policy (diagnostics)

**Principle:** Ready validates skeleton + game regulation; **Prepare** = structural integrity; **Materialize** = production can be created; **drawer** reflects policy and may **hide** presentation-only (never weakens gates).

| Diagnostic | Cat. | Prepare (target) | Materialize (target) | Drawer (target) | Blocking condition (factual, from code) |
|------------|------|------------------|----------------------|-----------------|----------------------------------------|
| `SlotFeedMissing` | **C** (occupancy synth) / **I** (analyzer `Missing` only when conflated) | **No block** (D3 — complete in Ready/Draft attention; block at Materialize) | Block when pair/slot production requires resolved occupancy / feeds | **Attention in Draft**; **hide** when `IsSlotFeedAwaitingDraw` (presentation only) | Schematic gap ([`StructurePlaceOccupancyIssues`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePlaceOccupancyIssues.cs)); `InvalidFeed` / `MultipleFeeds` still block Prepare via [`PrepareStage`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/PrepareStage/PrepareStage.cs) |
| `SlotFeedInvalid` / `SlotFeedMultiple` | I | Block | Revalidate after operational mutation | Error | Feed analysis per slot |
| `CycleDetected` / `RoutingNondeterminism` | I | Block | — | Error | Topology analyzer |
| `OverCapacityEdge` / `OverExtraction` | I | Block | Block if still true at materialize preflight | Error / warn (OverCapacity drawer **warn** may stay) | `expected > places` / `outbound > capacity` ([`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) |
| `ConfrontationUndecidable` | M | Block | Block | Error | Necessary confrontation `TieFormat` cannot `CanAlwaysDecideDraw()` |
| `AuthoredAwardUnreachable` | M | **Block** on **non-classifying** phases only (keep gate) | Block if placement award paths still unreachable **and** awards are required for production (format-specific — **spec TBD §12**) | **Error** on non-classifying only; **warning or omit** on classifying (Prepare does not gate) — **Δ code** | `PlacementAward` path `SourcePairKey` has no `BracketPair` ([`AnalyzeResult`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)); Prepare gate ignores classifying ([R-Prep2](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePrepareAttributionTests.cs)) |
| `ExpectedRankUnreachable` | M | **No block** by default | Block if rank needed for production cannot be produced | **Warning**; suppress when `AuthoredAwardUnreachable` present (keep) | Authored rank ∉ reachable ranks on stage |
| `PopulationShortfall` | C | **No block** | Block when roster required for production incomplete | **Warning in Draft** | `expected < places` ([`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) + U1 synthesis ([`StructurePopulationVolume`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePopulationVolume.cs)); materialize uses **`IsRosterComplete`** for Ch/Swiss ([`ChampionshipSwissPhasePlaces`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/ChampionshipSwissPhasePlaces.cs)) — same intent, different code path (**Δ** unify or document) |
| `SourcePopulationShortfall` | C | No block | Block **only if** a materialize/start path needs the missing extraction volume (**format-specific — spec TBD §12**) | Warning (informative) | `outbound > min(available, capacity)` ([`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) |
| `NonRootUnfed` | C | No block | Block **only if** an **eligible** confrontation for materialize cannot be fed (**format-specific — spec TBD §12**) | Warning | Non-root, no Qual/Prog inbound, no affectation ([`AppendNonRootUnfed`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) — **not** a blanket Materialize gate |

---

## 6. Resolved arbitration #1 — `SlotFeedMissing` (updated D3, 2026-10-15)

- **Prepare:** **does not** block on schematic occupancy or `FeedResolutionStatus.Missing`; **does** block on `InvalidFeed` / `MultipleFeeds` and other integrity prepare gates. Groups composition pool gap remains via [`StructurePreparePlacesGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePreparePlacesGate.cs) only.
- **Materialize:** unchanged — format-specific occupancy / confrontation rules.
- **Drawer:** D5 attention in Draft for occupancy; `IsSlotFeedAwaitingDraw` suppression unchanged.

---

## 7. Resolved arbitration #2 — business diagnostics (M)

Policy table: **§5.2**. Immediate documentation-only action: treat `AuthoredAwardUnreachable` as **drawer error** to match Prepare on Cup/KO.

**Reserve (product):** do not remove Prepare block for `AuthoredAwardUnreachable` until attribution semantics are fully specified (impossible vs incomplete); current code treats missing `BracketPair` as **unreachable**, not incomplete.

---

## 8. Resolved arbitration #3 — Swiss capability grid

D2 unchanged for Cup / Championship / Groups. Swiss uses a **separate capability row** — not a global `HasAttachedMatches ⇒ deny all`.

| Capability | Swiss Running (matches may exist) | Standard formats |
|------------|-----------------------------------|------------------|
| `swissProgressiveRound` | **Allow** — `GenerateNextRoundAsync` ([`GenerateNextRound`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/GenerateNextRound/GenerateNextRound.cs): Running, `IsRosterComplete`, round ≤ K) | N/A |
| `dematerializeSwissLastRound` | **Allow** in scope — [`DematerializeSwissLastRound`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/Dematerialization/DematerializeSwissLastRound.cs) + [`DematerializationCapability.EvaluateSwiss`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/Dematerialization/DematerializationCapability.cs) | Full-phase dematerialize per format |
| `operationalEntries` / exits / placement / draw | **Deny** (structure locked Ready/Running except Swiss progressive APIs) | Deny when production present (D2) |
| `gameRegulation` after Start | Standing replace still allowed (existing) | Same |

Tests to preserve: [`GenerateNextRoundTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/GenerateNextRoundTests.cs), [`DematerializeSwissLastRoundTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/DematerializeSwissLastRoundTests.cs).

---

## 9. Commands — `UseCaseExecutor.Stages` inventory (current)

Domain default: `EnsureStructureMutable()` ⇒ **Draft only** unless noted. Application `StageStructureEditGuard` mirrors Draft-only.

| Executor method | Use case | App guard | Domain / other guard | Hub action (Draft comp.) | Family |
|-----------------|----------|-----------|----------------------|--------------------------|--------|
| `PrepareStageAsync` | `PrepareStage` | `EnsureCompetitionAllowsLifecycleMutation` | Places + attribution + feed resolution | Lifecycle | Lifecycle |
| `StartStageAsync` | `StartStage` | lifecycle | `StageStartReadinessGuard` | Lifecycle | Lifecycle |
| `CompleteStageAsync` | `CompleteStage` | lifecycle | open matches | Lifecycle | Lifecycle |
| `RevertStageToDraftAsync` | `RevertToDraft` | lifecycle | no attached matches | Lifecycle | Lifecycle |
| `SuspendStageAsync` / `ResumeStageAsync` | Suspend/Resume | lifecycle | live match rules | Lifecycle | Lifecycle |
| `RevertStageToReadyAsync` | `RevertToReady` | lifecycle | dematerialization gates | Lifecycle | Lifecycle |
| `ApplyProgressionOutcomeAsync` | `ApplyProgressionOutcome` | `EnsureCompetitionAllowsConsequenceOperation` | sportive | — | Sportive |
| `ApplyQualificationAsync` | `ApplyQualification` | consequence | sportive | — | Sportive |
| `PublishDrawAsync` | `PublishDraw` | lifecycle mutation | `EnsureStructureMutable` | `ReplaceDrawRules` ecosystem | Operational draw |
| `PublishAndApplyDrawAsync` | orchestration | same | same | — | Operational draw |
| `CancelDrawAsync` | `CancelDraw` | lifecycle mutation | `EnsureStructureMutable` | — | Operational draw |
| `ReleaseDrawAlignedPlacementsAsync` | `ReleaseDrawAlignedPlacements` | lifecycle mutation | draw state + F1 snapshots | — | Operational draw |
| `ApplyDrawAsync` | `ApplyDraw` | lifecycle mutation | **App** `StageStructureEditGuard` + F1 | — | Operational draw |
| `AddStageRoundAsync` | `AddStageRound` | — | **App** `StageStructureEditGuard` | `AddRound` | Forme |
| `AddStageSlotAsync` | `AddStageSlot` | — | App guard | `AddSlot` | Forme |
| `AssignEntryToSlotAsync` | `AssignEntryToSlot` | lifecycle mutation | App guard + F1 | `AssignEntryToSlot` | Placement |
| `ClearSlotAssignmentAsync` | `ClearSlotAssignment` | lifecycle mutation | App guard + F1 | `ClearSlotAssignment` | Placement |
| `RenameStageAsync` | `RenameStage` | — | always allowed | `RenameStage` | Meta |
| `AddStageMatchdayAsync` | `AddStageMatchday` | — | App guard (structure matchday) | `AddMatchday` | Forme |
| `AddStageGroupAsync` | `AddStageGroup` | — | App guard | `AddGroup` | Forme |
| `ReplaceStageMatchGenerationFormatAsync` | `ReplaceStageMatchGenerationFormat` | — | App guard | — | Forme |
| `ReplaceStageSwissSettingsAsync` | `ReplaceStageSwissSettings` | — | App guard | — | Forme |
| `ReplaceStageProgressionRulesAsync` | `ReplaceStageProgressionRules` | — | App guard | `ReplaceProgressionRules` | Sorties |
| `ReplaceStageProgressionIntentsAsync` | same | — | App guard | same | Sorties |
| `ReplaceStageQualificationRulesAsync` | `ReplaceStageQualificationRules` | — | App guard | `ReplaceQualificationRules` | Sorties |
| `ReplaceStageQualificationIntentsAsync` | same | — | App guard | same | Sorties |
| `ReplaceStagePlacementAwardRulesAsync` | `ReplaceStagePlacementAwardRules` | — | App guard | `ReplacePlacementAwardRules` | Attribution |
| `ReplaceStageStandingRulesAsync` | `ReplaceStageStandingRules` | — | `EnsureStandingRulesMutable` (Running+ classifying) | `ReplaceStandingRules` | Règlement jeu |
| `ReplaceStageMatchRulesAsync` | `ReplaceStageMatchRules` | — | `EnsureStructureMutable` | `ReplaceMatchRules` | Règlement jeu |
| `BindStageRegulationAsync` | `BindStageRegulation` | — | `EnsureStructureMutable` | `BindToCompetition` | Règlement jeu |
| `ReplaceStageDrawRulesAsync` | `ReplaceStageDrawRules` | — | `EnsureStructureMutable` + draw locks | `ReplaceDrawRules` | Operational draw |
| `ReplaceStageAffectationAuthoringAsync` | `ReplaceStageAffectationAuthoring` | lifecycle mutation | `EnsureStructureMutable` | `ReplaceAffectationAuthoring` | Entrées |
| `ReplaceStageDefaultTieFormatAsync` | `ReplaceStageDefaultTieFormat` | — | `EnsureStructureMutable` | `ReplaceDefaultTieFormat` | Règlement jeu |
| `ReplaceRoundTieFormatAsync` | `ReplaceRoundTieFormat` | — | `EnsureStructureMutable` | `ReplaceRoundTieFormat` | Règlement jeu |
| `CreateDrawAsync` | `CreateDraw` | lifecycle mutation | App guard | — | Operational draw |
| `ConfigureDrawInputsAsync` | `ConfigureDrawInputs` | lifecycle mutation | draw Draft only | — | Operational draw |
| `GenerateDrawAsync` | `GenerateDrawResolution` | lifecycle mutation | draw Draft only | — | Operational draw |
| `MaterializeMatchesAsync` | `MaterializeMatches` | — | `StageMaterializeGuard` + roster/groups | materialize hub | Production |
| `MaterializeCupFromOccupiedSlotsAsync` | `MaterializeCupFromOccupiedSlots` | — | same + Cup competition rules | materialize hub | Production |
| `DematerializePhaseAsync` | format-specific | — | `DematerializationPreflight` | dematerialize hub | Production |
| `GenerateNextRoundAsync` | `GenerateNextRound` | — | Swiss Running + roster | — | Swiss progressive |
| `GenerateScheduleAsync` | schedule read | — | `EnsureScheduleExploitation` Ready+ | — | Schedule |
| `ApplyScheduleAsync` | `ApplySchedule` | lifecycle mutation | `EnsureScheduleExploitation` | — | Schedule |

**Related (not on `UseCaseExecutor.Stages`):** `RebuildStageStructure`, `ConfigureStructure`, `RemoveCompetitionStage` — use `StageStructureEditGuard` where applicable.

### 9.1 Target command policy (signed)

| Family | Ready, no production | Production present (D2) | Swiss Running |
|--------|---------------------|-------------------------|---------------|
| Forme / game regulation | Deny | Deny | Deny structure; standing exception after Start |
| Entrées, sorties, attribution, placement, draw | Allow + **D1** + draw-refuse rules | Deny | Deny (use progressive APIs only) |
| Materialize batch | Allow if §5.2 gates pass | Partial state — no new operational edits | `GenerateNextRound` only |
| Dematerialize | Per `DematerializationCapability` | Per scope | Last round only |

**New (implementation):** capability-based guards — `OperationalMutationGuard` must **not** block `GenerateNextRound` / scoped Swiss dematerialize.

**Shipped (G-OPS2):** [`StructureReadyConsumerFeedGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureReadyConsumerFeedGate.cs) — D1 on qual/prog/affectation replace; `Application.ReadyConsumerWouldBreak` + consumer ids.

---

## 10. Reference business cases

| ID | Scenario | Expected (signed policy) | Verification |
|----|----------|------------------------|--------------|
| **R-D1a** | U Ready, D Draft; Top 2→Top 1 on qual | Allowed (no D1) | [g-ops spec](./g-ops-ready-operational-mutations-and-d1.md) § R-D1a — test with G-OPS1 |
| **R-D1b** | U+D Ready; Top 2→Top 1 or clear qual, entries on D | Rejected + consumer list (**b2** W3, **b1** W1) | `ReplaceStageQualificationRulesGOpsTests` |
| **R-Draw1** | Change composition with published applied draw | Refuse mutation (target) | **Gap** — UI partial; gate deferred — [G-OPS3 decision](./g-ops3-arbitration-session.md); negatives [`ReplaceStageAffectationAuthoringGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs) |
| **R-Draw2** | Draw engaged; `SlotFeedMissing` hidden in drawer | Prepare not gated by occupancy; drawer hides only | [`StructurePresentedAttentionCollectorTests`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePresentedAttentionCollectorTests.cs) |
| **R-Mat1** | Ch Ready, incomplete roster | `PhaseRosterIncomplete` | [`StructureMaterializeAssemblerTests`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructureMaterializeAssemblerTests.cs) |
| **R-Mat2** | Ready, partial materialize, operational edit | Denied (D2) | `ReplaceStageQualificationRulesGOpsTests`, `ReplaceStageProgressionRulesGOpsTests`, `ReplaceStageAffectationAuthoringGOpsTests` (D2) |
| **R-Prep1** | Cup `AuthoredAwardUnreachable` | Prepare blocked | [`StructurePrepareAttributionTests`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePrepareAttributionTests.cs) R3 |
| **R-Prep2** | Championship + `AuthoredAwardUnreachable` issue | Prepare **not** blocked by attribution gate | Same file `BlocksPrepare_ignores_authored_award_on_classifying_phase` |
| **R-Prep3** | Cup slots with `Missing` feed config + Prepare | **Allowed** | [`PrepareStageTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/PrepareStageTests.cs) `Execute_prepares_when_slot_feed_configuration_is_missing` |
| **R-Mat-M1** | `ConfrontationUndecidable` at materialize | Block (Cup) | `MaterializeCupFromOccupiedSlotsGMat1Tests` — [g-mat1 spec](./g-mat1-confrontation-undecidable-at-materialize.md) |
| **R-Mat-M2** | `NonRootUnfed` blocks materialize when confrontation unfed | Block (target) **deferred** — A5 **M2b** signed | **M2b** — [`MaterializeCupFromOccupiedSlotsRMatM2bTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeCupFromOccupiedSlotsRMatM2bTests.cs); positive gate **not** implemented (G-MAT3 spec draft) |
| **R-Mat-M4b** | `SourcePopulationShortfall` on foreign phase | No G-MAT4 veto on **S** (A5) | [`MaterializeMatchesRMatM4bTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeMatchesRMatM4bTests.cs); gate **deferred** ([G-MAT4 decision](./g-mat4-arbitration-session.md)) |
| **R-Swiss1** | Running, generate next round | Allowed | [`GenerateNextRoundTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/GenerateNextRoundTests.cs) |
| **R-Swiss2** | Dematerialize last Swiss round | Allowed in scope | [`DematerializeSwissLastRoundTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/DematerializeSwissLastRoundTests.cs) |
| **R-Life1** | Revert Ready→Draft with attached matches | `ProductionPresent` | [`RevertToDraftReadAlignmentTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/RevertToDraftReadAlignmentTests.cs) |
| **R-Stand1** | Replace standing after Start | Allowed | [`RegulationIntegrationTests`](../../../tests/MyClub.PlayUp.Domain.Tests/Rules/RegulationIntegrationTests.cs) |
| **R-UI1** | Drawer `AuthoredAwardUnreachable` on Cup/KO vs classifying | Error vs warning per phase class | G-UI1 **Done** — `structureDiagnosticSeverity.ts` + `StructurePresentedAttentionCollector` |

---

## 11. Sign-off scope (two layers)

Signing **§11A** does **not** commit to every §12 line as an implementation contract. **§11B (signed 2026-10-15)** classifies every §12 row as **Done**, **Deferred**, or **Constraint** — the pre-§11B **Litigious** label is retired for backlog rows.

### 11A — Product policy (**SIGNED** 2026-10-15)

By signing **§11A**, the decision maker validates **intent only** — not that the codebase already behaves this way. **§11B** (separate signature, 2026-10-15) records the implementation contract for §12 without obligating deferred gates.

- [x] **A1** — **Draft** allows structural and operational configuration (no materialize).
- [x] **A2** — **Ready** freezes **form** and **game regulation** (match, standing, confrontation/tie); **operational** mutations remain possible **under conditions** (no production on phase, D1, draw-refuse rules).
- [x] **A3** — **D1:** operational exit/flux edits in Ready must not silently invalidate a **Ready** downstream consumer; block and name phases to revert (no auto-cascade).
- [x] **A4** — **D2:** materialized production on the phase forbids **general** operational mutations; **Swiss** keeps explicit capabilities (`GenerateNextRound`, `DematerializeSwissLastRound`) — no generic `HasAttachedMatches ⇒ deny all`.
- [x] **A5** — **Prepare** and **Materialize** have **distinct responsibilities**; diagnostic policy in §5.2 guides gates but **a diagnostic is not an automatic veto** — blocking must match demonstrated business impossibility for the format and production path. *(Reserve: §5.2 rows are directional policy, not “every I blocks Prepare / every C blocks Materialize” without format-specific proof.)*
- [x] **A6** — Gaps between policy and code are tracked in **§12** and closed via tested changes; no silent drift.

**Signatory note:** Product policy validated; runtime gaps remain to be fixed in tested increments. Authorized without §11B: **G-UI1**, **G-UI2** — **shipped 2026-10-15** (see §12).

**Documentation completeness (maintainer, not product signature):**

- [x] Inventory diagnostics & commands (§4–§5, §9)
- [x] Arbitrations #1–#3 closed (§6–§8)
- [x] Reference cases mapped (§10)

### 11B — Implementation contract (**SIGNED** 2026-10-15)

**Maintainer pack:** [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md) · Notion copy [notion-decision-11b-draft.md](./notion-decision-11b-draft.md).

- [x] **B1** — All §12 rows **Done**, **Deferred**, or **Constraint** (no orphan **Litigious**) — §12 below
- [x] **B2** — Reference tests §10 agreed for deferred materialize/draw paths (A5 contracts)
- [x] **B3** — Notion Décision §11B published (canonical; cross-link from §11A)
- [x] **B4** — Follow-up per [implementation-plan-post-11b.md](./implementation-plan-post-11b.md)

**Stop line (post-sign):** Do **not** implement **Deferred** gates (G-MAT3, G-MAT4, G-OPS3) without a new product decision and reopen of the arbitration doc. **Done** rows are shipped; changes = regression fixes or Notion Task scope only.

**Product signature:** §11B does **not** require delivering deferred gates — it records reserves and authorizes process for future reopen.

---

## 12. Policy vs code — implementation backlog

| ID | Gap | Product policy (§5.2 / §11A) | Current code | Priority | §11B status | Notes / next spec step |
|----|-----|------------------------------|--------------|----------|-----------------|------------------------|
| **G-UI1** | Drawer `AuthoredAwardUnreachable` | Error **non-classifying only**; no error on classifying | Shipped | **High** | **Done** | `structureDiagnosticSeverity.ts` + host collector |
| **G-UI2** | Drawer `PopulationShortfall` / `SourcePopulationShortfall` | Warning in Draft | Shipped | Medium | **Done** | Warning severity SPA + host |
| **G-MAT1** | Materialize `ConfrontationUndecidable` | Block when undecidable **necessary** confrontation | Cup gate shipped | Medium | **Done** | [g-mat1](./g-mat1-confrontation-undecidable-at-materialize.md); GMat1 tests |
| **G-MAT2** | Materialize `AuthoredAwardUnreachable` | Block when **requested ∩ unreachable** or **eligible ∩ unreachable** (opt. **B**) | Cup gate shipped | Low | **Done** | [g-mat2](./g-mat2-authored-award-unreachable-at-materialize.md); GMat2 tests |
| **G-MAT3** | Materialize `NonRootUnfed` | Block **only if** production impossible | Signal only; **no** materialize gate | Low | **Deferred** | [decision](./g-mat3-arbitration-session.md); M2b test; spec draft Git |
| **G-MAT4** | Materialize `SourcePopulationShortfall` | Block **only if** extraction required for production | Not gated; `MaterializationFailure` on target roster | Low | **Deferred** | [decision](./g-mat4-arbitration-session.md); M4b + analyzer tests |
| **G-OPS1** | Ready operational mutations | Allow without production (D2) on qual/prog/affectation replace | Shipped | **Major** | **Done** | [g-ops](./g-ops-ready-operational-mutations-and-d1.md); D2 tests |
| **G-OPS2** | D1 upstream feed guard | Block on **feed effect** (rules or source population) | Shipped | **Major** | **Done** | `StructureReadyConsumerFeedGate`; SPA consumer names |
| **G-OPS3** | Draw-refuse on composition change | Refuse if applied draw invalidated | Partial UI; command **no** gate | Medium | **Deferred** | [decision](./g-ops3-arbitration-session.md); R-Draw1-N1..N3; **D3-1** |
| **G-SWISS** | Generic production guard | Must not break Swiss progressive | Swiss APIs separate today | — | **Constraint** | §8 capability grid; no global guard |
