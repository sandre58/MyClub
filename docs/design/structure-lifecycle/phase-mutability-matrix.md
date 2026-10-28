# Phase mutability — diagnostic & command matrix

**Version:** §11A + **§11B signed 2026-10-15**; **§11C full sign (Git) 2026-10-15** — [E5 record](./section-11c-e5-matrix-diff-proposal.md); arbitrations #1–#3 closed; factual conditions verified against repository.  
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

**Prepare command path:** `PrepareStage.Execute` (slot feed resolution when slots exist) → `target.Prepare(clock)`.  
**Prepare read path:** `StructureStageLifecycleAssembler.ResolvePrepareTransition` mirrors gates (graph, attribution, `CollectPrepareGateCodes`).

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

**`operationalDraw` (§11C):** Ready without production remains **Allow** for draw **workflow** and pool/affectation paths per **§9.1.1**. Stage-level `ReplaceStageDrawRules` and draw-entity configure/generate stay **Draft-only** (B-Q1b, B-Q3b). **≤1 non-cancelled draw** per phase is a **product contract** (B-Q3a) — delivery **G-B-1-ENF** (§12).

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
| `OverCapacityEdge` | — | **—** | — | — | **W** | **W** | — | — | — |
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
| `OverCapacityEdge` | I | **No block** (warning only — surplus meter / drawer) | Block if still true at materialize preflight | **Warning** (drawer) | `expected > places` ([`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) |
| `OverExtraction` | I | Block | Block if still true at materialize preflight | Error | `outbound > capacity` ([`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) |
| `ConfrontationUndecidable` | M | Block | Block | Error | Necessary confrontation `TieFormat` cannot `CanAlwaysDecideDraw()` |
| `AuthoredAwardUnreachable` | M | **Block** on **non-classifying** phases only (keep gate) | Block if placement award paths still unreachable **and** awards are required for production (format-specific — **spec TBD §12**) | **Error** on non-classifying only; **warning or omit** on classifying (Prepare does not gate) — **Δ code** | `PlacementAward` path `SourcePairKey` has no `BracketPair` ([`AnalyzeResult`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)); Prepare gate ignores classifying ([R-Prep2](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePrepareAttributionTests.cs)) |
| `ExpectedRankUnreachable` | M | **No block** by default | Block if rank needed for production cannot be produced | **Warning**; suppress when `AuthoredAwardUnreachable` present (keep) | Authored rank ∉ reachable ranks on stage |
| `PopulationShortfall` | C | **No block** | Block when roster required for production incomplete | **Warning in Draft** | `expected < places` ([`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) + U1 synthesis ([`StructurePopulationVolume`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePopulationVolume.cs)); materialize uses **`IsRosterComplete`** for Ch/Swiss ([`ChampionshipSwissPhasePlaces`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/ChampionshipSwissPhasePlaces.cs)) — same intent, different code path (**Δ** unify or document) |
| `SourcePopulationShortfall` | C | No block | Block **only if** a materialize/start path needs the missing extraction volume (**format-specific — spec TBD §12**) | Warning (informative) | `outbound > min(available, capacity)` ([`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) |
| `NonRootUnfed` | C | No block | Block **only if** an **eligible** confrontation for materialize cannot be fed (**format-specific — spec TBD §12**) | Warning | Non-root, no Qual/Prog inbound, no affectation ([`AppendNonRootUnfed`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) — **not** a blanket Materialize gate |

---

## 6. Resolved arbitration #1 — `SlotFeedMissing` (updated D3, 2026-10-15)

- **Prepare:** **does not** block on schematic occupancy, groups composition pool gap, or `FeedResolutionStatus.Missing` (D3 — operational completeness is Materialize-only); **does** block on `InvalidFeed` / `MultipleFeeds` and other integrity prepare gates (`CollectPrepareGateCodes`, `PrepareStage`).
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

Domain default: `EnsureStructureMutable()` ⇒ **Draft only** unless noted. Application `StageStructureEditGuard` requires stage Draft, and denies heavy structure when the competition is Running or Suspended (`AddRound` / `AddSlot` / `AddMatchday` / `AddGroup`, rebuild, draw-rules replace, match and tie regulation). Downstream Draft hub actions match Ready operational authoring; an orphan Draft phase projects `RenameStage` only.

**2B.1 (E1A–E6A)** denies every Draft extra outside that Ready operational set while the competition is Running or Suspended. Suspended matches Running. The deny list is `ReplaceDrawRules`, `AddSlot`, `AddRound`, `AddGroup`, `AddMatchday`, `RebuildStructure`, `RemoveStage`, `ReplaceMatchRules`, `BindToCompetition`, `ReplaceDefaultTieFormat`, `ReplaceRoundTieFormat`, `ReplaceStandingRules` on a **Draft** stage, `ReplaceMatchGenerationFormat`, `ReplaceSwissSettings`, and `ConfigureStructure`. Peuplement stays on the Ready operational set (`AssignEntryToSlot` / `ClearSlotAssignment`). `ReplaceStandingRules` on a **Running** classifying stage stays allowed and is outside this pack. Application is the deny source (`StageStructureEditGuard`, plus the competition-status checks on `ConfigureStructure`, `RebuildStageStructure`, and `RemoveCompetitionStage`). The hub only projects that result. The competition-less `ReplaceStageDrawRules` overload is internal and is not a product entry. Proof: [`DraftUnderRunningAuthoringTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/DraftUnderRunningAuthoringTests.cs).

**§9 table = runtime today.** Target draw policy = **§9.1.1**. Do not read **Allow** in §9.1 as “already enforced” where §9 shows Draft-only.

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
| `RenameStageAsync` | `RenameStage` | — | deny when `Stage.Completed` (`Stage.DisplayNameLocked` / C-18) | `RenameStage` when `CanRenameDisplayName()` | Meta |
| `AddStageMatchdayAsync` | `AddStageMatchday` | — | App guard (structure matchday) | `AddMatchday` | Forme |
| `AddStageGroupAsync` | `AddStageGroup` | — | App guard | `AddGroup` | Forme |
| `ReplaceStageMatchGenerationFormatAsync` | `ReplaceStageMatchGenerationFormat` | — | App guard | — | Forme |
| `ReplaceStageSwissSettingsAsync` | `ReplaceStageSwissSettings` | — | App guard | — | Forme |
| `ReplaceStageProgressionRulesAsync` | `ReplaceStageProgressionRules` | — | App guard | `ReplaceProgressionRules` | Sorties |
| `ReplaceStageProgressionIntentsAsync` | same | — | App guard | same | Sorties |
| `ReplaceStageQualificationRulesAsync` | `ReplaceStageQualificationRules` | — | App guard | `ReplaceQualificationRules` | Sorties |
| `ReplaceStageQualificationIntentsAsync` | same | — | App guard | same | Sorties |
| `ReplaceStagePlacementAwardRulesAsync` | `ReplaceStagePlacementAwardRules` | — | App guard | `ReplacePlacementAwardRules` | Attribution |
| `ReplaceStageStandingRulesAsync` | `ReplaceStageStandingRules` | — | deny `Ready` (`Stage.StandingRulesLocked` / C-20) ; Running+ classifying | `ReplaceStandingRules` | Règlement jeu |
| `ReplaceStageMatchRulesAsync` | `ReplaceStageMatchRules` | — | `EnsureStructureMutable` | `ReplaceMatchRules` | Règlement jeu |
| `BindStageRegulationAsync` | `BindStageRegulation` | — | `EnsureStructureMutable` | `BindToCompetition` | Règlement jeu |
| `ReplaceStageDrawRulesAsync` | `ReplaceStageDrawRules` | — | `EnsureDraftHeavyStructureAllowed` + `EnsureStructureMutable` + draw locks | `ReplaceDrawRules` | Operational draw |
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

**Draw family (§11C — product target, partially enforced):** In Ready without production, **draw workflow** commands (`PublishDraw`, `ApplyDraw`, `CancelDraw`, create when allowed) are in scope; **not** unrestricted editing of all draw parameters. See **§9.1.1**.

### 9.1.1 Draw commands — target policy (§11C signed 2026-10-15)

**Status:** Product contract from Arbitrage **B** (+ **C** consumption gates as backlog). **Code** may differ — see §9 table and §12.

| Command / concern | Stage **Draft** | Stage **Ready**, no production | **Applied** draw (entity) | Enforcement today |
|-------------------|-----------------|-------------------------------|---------------------------|-------------------|
| `ReplaceStageDrawRules` | Allow while the competition is Draft or Ready; **Deny** when the competition is Running or Suspended (2B.1 E1A) | **Deny** (B-Q1b) — use cancel → recreate (P2) if needed | **Deny** | **Aligned** — `EnsureDraftHeavyStructureAllowed` + `EnsureStructureMutable` |
| `CreateDraw` | Allow | Allow if **≤1 non-cancelled** draw (B-Q3a) | — | **Δ** — SPA partial; **G-B-1-ENF** |
| `ConfigureDrawInputs` / `GenerateDraw` | Draw entity **Draft** only | **Deny** on entity (B-Q3b) | **Deny** | **Aligned** — `Draw.EnsureDraftMutable` |
| `PublishDraw` / `ApplyDraw` | Per lifecycle | **Allow** per B-Q2 (not universal deny post-Applied) | Idempotent **Apply** only if conform; **deny** obsolete **D** on **Apply** (**C-ENF-Apply** `85360d3`) | **Aligned** — Apply gate shipped |
| `ReplaceStageAffectationAuthoring` (pool) | Allow + D1/D2 | Allow | Same **EntryId** set (**R-Draw1-N2**); pool **change** → **refuse** (**R-Draw1+** / G-B-3) | **Aligned** — `StructureAppliedDrawPoolChangeGate` (**SLICE-1**) |
| Obsolete draw **consumption** | — | Deny **Apply** / **Materialize** / dependent **Start** when **D** obsolete (**C**) | **C-ENF** Detect + Apply + Mat-Start (`279b6235`, `85360d3`, `ee0fc2e6`) | **Aligned** — **G-C-INV** §12 **Done** |

**Invariant (B-Q3a):** At most **one** draw with `Status != Cancelled` per phase; cancelled history retained.

**Three contracts (L3 — do not merge):** (a) **G-B-1** uniqueness, (b) **G-B-3** pool-change **refuse** after Applied, (c) **C** obsolete-for-consumption after **allowed** mutation.

**Annex:** [section-11c-addendum](./section-11c-addendum-clarification-arbitration.md) §4.10, §5.4–§5.5.

---

## 10. Reference business cases

| ID | Scenario | Expected (signed policy) | Verification |
|----|----------|------------------------|--------------|
| **R-D1a** | U Ready, D Draft; Top 2→Top 1 on qual | Allowed (no D1) | [g-ops spec](./g-ops-ready-operational-mutations-and-d1.md) § R-D1a — test with G-OPS1 |
| **R-D1b** | U+D Ready; Top 2→Top 1 or clear qual, entries on D | Rejected + consumer list (**b2** W3, **b1** W1) | `ReplaceStageQualificationRulesGOpsTests` |
| **R-Draw1** | Legacy umbrella — see **R-Draw1+** (refuse) and **R-Draw1-N2** (allow same set) | Refuse incompatible pool change (target) | [G-OPS3 decision](./g-ops3-arbitration-session.md); [`ReplaceStageAffectationAuthoringGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs) |
| **R-Draw1+** | Shrink or change affectation **EntryId** set with **Applied** draw | **Refuse mutation** (G-B-3 / G-OPS3) — not allow-then-obsolete | `ReplaceAffectationAuthoring_refuses_shrink_when_group_draw_applied`, `ReplaceAffectationAuthoring_refuses_swap_with_same_count_when_group_draw_applied` |
| **R-Draw1-N2** | Replace affectation with **same** `EntryId` set, Applied draw | **Allow** — not obsolete | `ReplaceAffectationAuthoring_allows_replace_with_same_entries_when_group_draw_applied` |
| **R-Inv-1** | Draft form change orphans placements; draw **D** obsolete for consumption | Allow mutation; **Deny** `ApplyDraw` / **Materialize** on obsolete **D** | **Done** — `DrawConsumptionMatStartTests` + Apply R-Inv-1; Host Start HTTP not integration-tested — [addendum §5.2.8](./section-11c-addendum-clarification-arbitration.md) |
| **R-C-R3** | Recovery blocked by **D2** (matches produced) | **R3** — no false recreate promise; no C bypass | Related **R-Mat2** / D2 ops tests |
| **R-Mat-block** | Ready; invalid assignments or obsolete draw on materialize path | **Materialize** denied with explicit reason (F4 + **C**) | **Done** — `DrawConsumptionMatStartTests` + `CEnfMatStartEndpointTests` (Groups HTTP) |
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

## 11. Sign-off scope (three layers)

Signing **§11A** does **not** commit to every §12 line as an implementation contract. **§11B (signed 2026-10-15)** classifies every §12 row as **Done**, **Deferred**, or **Constraint** — the pre-§11B **Litigious** label is retired for backlog rows. **§11C (signed Git 2026-10-15)** publishes A/B/C draw and obsolescence contracts without authorizing ENF backlogs until an explicit slice (§11C.4).

### 11A — Product policy (**SIGNED** 2026-10-15)

By signing **§11A**, the decision maker validates **intent only** — not that the codebase already behaves this way. **§11B** (separate signature, 2026-10-15) records the implementation contract for §12 without obligating deferred gates.

- [x] **A1** — **Draft** allows structural and operational configuration (no materialize).
- [x] **A2** — **Ready** freezes **form** and **game regulation** (match, standing, confrontation/tie); **operational** mutations remain possible **under conditions** (no production on phase, D1, draw-refuse rules).
- [x] **A3** — **D1:** operational exit/flux edits in Ready must not silently invalidate a **Ready** downstream consumer; block and name phases to revert (no auto-cascade).
- [x] **A4** — **D2:** materialized production on the phase forbids **general** operational mutations; **Swiss** keeps explicit capabilities (`GenerateNextRound`, `DematerializeSwissLastRound`) — no generic `HasAttachedMatches ⇒ deny all`.
- [x] **A5** — **Prepare** and **Materialize** have **distinct responsibilities**; diagnostic policy in §5.2 guides gates but **a diagnostic is not an automatic veto** — blocking must match demonstrated business impossibility for the format and production path. *(Reserve: §5.2 rows are directional policy, not “every I blocks Prepare / every C blocks Materialize” without format-specific proof.)*
- [x] **A6** — Gaps between policy and code are tracked in **§12** and closed via tested changes; no silent drift.

**Signatory note:** Product policy validated; runtime gaps remain to be fixed in tested increments. Authorized without §11B: **G-UI1**, **G-UI2** — **shipped 2026-10-15** (see §12).

**§11C maintainer note:** Full §11C sign **narrows interpretation** of operational draw via §9.1.1; does **not** reopen §11A A1–A6 without new arbitration.

**Documentation completeness (maintainer, not product signature):**

- [x] Inventory diagnostics & commands (§4–§5, §9)
- [x] Arbitrations #1–#3 closed (§6–§8)
- [x] Reference cases mapped (§10)

### 11B — Implementation contract (**SIGNED** 2026-10-15)

**Maintainer pack:** [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md).

- [x] **B1** — All §12 rows **Done**, **Deferred**, or **Constraint** (no orphan **Litigious**) — §12 below
- [x] **B2** — Reference tests §10 agreed for deferred materialize/draw paths (A5 contracts)
- [x] **B3** — Notion Décision §11B published (canonical; cross-link from §11A)
- [x] **B4** — Follow-up per [implementation-plan-post-11b.md](./implementation-plan-post-11b.md)

**Stop line (post-sign):** Do **not** implement **Deferred** gates (G-MAT3, G-MAT4, G-OPS3, **G-B-1**, **G-B-3**) without **§11C.4** slice authorization (or G-OPS3 reopen where applicable). **G-C-INV** is **Done** (C-ENF slices **Closed**). **Done** rows are shipped; changes = regression fixes or Notion Task scope only.

**Product signature:** §11B does **not** require delivering deferred gates — it records reserves and authorizes process for future reopen.

**§11C stop line:** §11C adds **G-B-1-ENF**, **G-B-3-ENF**, and **C-ENF-*** delivery tracks (§12). Implementing them requires **§11C full sign** plus an explicit **slice** record (§11C.4) — not workbook sign alone.

### 11C — Clarification & arbitration (**SIGNED Git 2026-10-15**)

| Layer | Status |
|-------|--------|
| Workbooks **A**, **B**, **C** | **Signed** 2026-10-15 — [addendum](./section-11c-addendum-clarification-arbitration.md) |
| **Matrix amend (E5)** | **Applied** — [E5 record](./section-11c-e5-matrix-diff-proposal.md) |
| **§11C full sign (Git)** | **2026-10-15** — §9.1.1, §10, §12 per E5 |

#### 11C.1 Product decisions published at full sign

| ID | Summary | Source |
|----|---------|--------|
| **A-keep** | `operationalPlacement` Draft + Ready | Addendum §3.4 |
| **B-Q1..B-Q3** | Publish commitment; draw rules Draft-only; ≤1 non-cancelled draw; post-Applied per-command; G-B-3 refuse pool change | Addendum §4.10, §4.13 |
| **C1–C4** | Obsolete unit **D**; per-op deny on consumption; R1–R3; W-* witnesses; L3 ≠ G-B-3 | Addendum §5.2, §5.5 |

#### 11C.2 Enforcement — explicit non-authorization

Workbook and full **§11C** sign record **contracts** and **backlogs**. They do **not** imply runtime gates unless §12 row is **Done** with tests.

| Backlog | Contract | Authorized by C workbook? | Authorized by §11C full sign alone? |
|---------|----------|---------------------------|-------------------------------------|
| **G-B-1-ENF** | ≤1 non-cancelled draw | **No** | **No** — needs **slice** |
| **G-B-3-ENF** | Pool change refuse (G-B-3) | **No** | **Yes** — **SLICE-1** only (§11C.4) |
| **C-ENF-Detect** | G-C-1 | **No** | **No** — needs **slice** |
| **C-ENF-Apply** | G-C-2 | **No** | **No** — needs **slice** |
| **C-ENF-Mat** | G-C-3 | **No** | **No** — needs **slice** |
| **C-ENF-Deps** | G-C-4 | **No** | **No** — needs **slice** |

#### 11C.3 Full sign checklist (DM — E5 2026-10-15)

- [x] E5: Accept [matrix diff proposal](./section-11c-e5-matrix-diff-proposal.md) §9–§11 (+ §12 rows)
- [x] E3: Accept §10 new/amended case rows
- [x] E4: Confirm **B** not reopened
- [x] **First implementation slice:** **SLICE-1-G-B-3-ENF** (2026-10-15) — `G-B-3-ENF` only

#### 11C.4 Slice records

| Slice ID | Backlog | Status | Fiche |
|----------|---------|--------|-------|
| **SLICE-1-G-B-3-ENF** | G-B-3-ENF only | **Closed** (2026-10-15) | [slice-1-g-b-3-enf.md](./slice-1-g-b-3-enf.md) |
| **SLICE-2-G-B-1-ENF** | G-B-1-ENF only (B-Q3a) | **Closed** (2026-10-16) — `c1a12ec4` ; SPA reserve · §12 **G-B-1 Deferred** | [slice-2-g-b-1-enf.md](./slice-2-g-b-1-enf.md) |
| **SLICE-3-G-OPS3-QUAL-PROG-ENF** | G-OPS3 reliquat qual/prog | **Closed** (2026-10-16) — `472546bb` ; proof reserves · §12 **G-OPS3 Partial** | [slice-3-g-ops3-qual-prog-enf.md](./slice-3-g-ops3-qual-prog-enf.md) |
| **SLICE-4-G-OPS3-PROG-IMPACT-ENF** | G-OPS3 progression impact gate | **Closed** (2026-10-16) — P0–P6 · §12 **G-OPS3 Partial** | [slice-4](./slice-4-g-ops3-prog-impact-enf.md) · [arbitrage](./g-ops3-progression-impact-arbitration.md) |
| **SLICE-C-ENF-DETECT** | C-ENF-Detect (G-C-1) | **Closed** (2026-10-16) — `279b6235` | [slice-c-enf-detect.md](./slice-c-enf-detect.md) |
| **SLICE-C-ENF-APPLY** | C-ENF-Apply (G-C-2) | **Closed** (2026-10-16) — `85360d3` | [slice-c-enf-apply.md](./slice-c-enf-apply.md) |
| **SLICE-C-ENF-MAT-START** | C-ENF-Mat + C-ENF-Deps (G-C-3, G-C-4) | **Closed** (2026-10-16) — `ee0fc2e6` | [slice-c-enf-mat-start.md](./slice-c-enf-mat-start.md) |
| **SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT** | G-OPS3 B1 — Cup Assign/Clear only | **Closed** (2026-10-16) — `c9029ba0` ; réserves R4/T7 · §12 **G-OPS3 Partial** | [slice-g-ops3-b1-cup-slot-placement.md](./slice-g-ops3-b1-cup-slot-placement.md) |

**Arbitration SoT (2026-10-16):** [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md).

**SLICE-1** — Known risk: cross-request concurrency (fiche §4).

**SLICE-G-OPS3-B1** — R4 defensive branch (no reflection test); T7 delivery = F1 when B1 does not apply, not combined Applied+F1 on protected slot. No mandatory SPA disable parity.

**§11C full sign (Git):** 2026-10-15 — matrix §9.1.1, §10, §12 rows per [E5 proposal](./section-11c-e5-matrix-diff-proposal.md).

**Implementation plan:** [implementation-plan-post-11c.md](./implementation-plan-post-11c.md) · Notion [Décision §11C](https://app.notion.com/p/3fa75fb92d1081e2b96cc7a987799e3f).

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
| **G-OPS3** | Draw-refuse on composition change | Refuse if applied draw invalidated | **SLICE-3 Closed** — qual/prog (`472546bb`); **G-B-3** pool **Done**; **SLICE-G-OPS3-B1 Closed** — Cup Assign/Clear Applied slot gate (`c9029ba0`); draw-rules / autres reliquat | Medium | **Partial** | Slice **Closed** ≠ §12 **Done** — [slice-3](./slice-3-g-ops3-qual-prog-enf.md) · [slice B1](./slice-g-ops3-b1-cup-slot-placement.md); [decision](./g-ops3-arbitration-session.md). **C** separate. |
| **G-B-1** | ≤1 non-cancelled draw per phase | B-Q3a invariant | **SLICE-2 Closed** — domain/app guard (`c1a12ec4`); SPA UX parity **not** in slice | Medium | **Deferred** | SPA backlog; slice **Closed** ≠ §12 **Done** — [slice-2](./slice-2-g-b-1-enf.md) |
| **G-B-3** | Refuse pool membership change with Applied draw | G-B-3 / G-OPS3; **R-Draw1+** | `StructureAppliedDrawPoolChangeGate` on affectation replace | Medium | **Done** | **SLICE-1-G-B-3-ENF**; distinct from **C**; N2 preserved |
| **G-C-INV** | Obsolete draw **consumption** gates | C1–C4; **R-Inv-1**, **R-Mat-block** | **C-ENF** three slices (`279b6235`, `85360d3`, `ee0fc2e6`); read/command parity Materialize + Start (Application); Host Start HTTP limit per fiche | Medium | **Done** | [slice-c-enf-detect](./slice-c-enf-detect.md) · [apply](./slice-c-enf-apply.md) · [mat-start](./slice-c-enf-mat-start.md) |
| **G-SWISS** | Generic production guard | Must not break Swiss progressive | Swiss APIs separate today | — | **Constraint** | §8 capability grid; no global guard |
