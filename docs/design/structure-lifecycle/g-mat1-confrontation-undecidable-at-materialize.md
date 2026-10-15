# G-MAT1 — `ConfrontationUndecidable` at materialize (spec draft)

| Field | Value |
|-------|--------|
| **Backlog ID** | G-MAT1 |
| **Reference case** | R-Mat-M1 |
| **QA scenario** | EXEC-001 — [`InvariantsExecUndecidableScenario`](../../../src/PlayUp/MyClub.PlayUp.Development/Scenarios/InvariantsExecUndecidableScenario.cs) |
| **Contract status** | **Ready (§12)** — implementation contract validated; **Cup-only**; no §11B |
| **§11B** | **Not signed** by this document |
| **Scope** | G-MAT1 only — does **not** authorize G-MAT2, G-MAT3, or G-MAT4 |

**Product policy anchor (already signed §11A):** §5.2 row for `ConfrontationUndecidable` — block at Materialize when the predicate below holds. **Reserve A5 applies:** the diagnostic code alone is not a veto; the **materialize command** must enforce the same **demonstrated impossibility** predicate, not a blanket “any `ConfrontationUndecidable` in the hub” check.

---

## Guardrails (non-negotiable for this spec)

1. **No automatic diagnostic veto** — Materialize blocks only when an **addressable** confrontation is **necessary** for progression or placement awards on the production path, and the **effective** `TieFormat` for that confrontation cannot `CanAlwaysDecideDraw()`.
2. **Command-level proof** — Acceptance tests must call a **production materialize use case** (`MaterializeCupFromOccupiedSlots` for Cup; `MaterializeMatches` for Championship/Groups when a qualifying scenario exists), not only `CompetitionFlowAnalyzer` or `StructureViewAssembler`.
3. **Business vs technical** — This file proposes a contract for decision-maker validation. Implementation waits for explicit sign-off of **G-MAT1** under §11B (or an agreed narrow exception), not general §11B.

---

## Question to decide

**When must an undecidable confrontation prevent match production (materialize)?**

**Proposed answer (candidate contract):** Refuse materialize for stage `S` when there exists at least one `sourcePairKey` such that:

| Step | Predicate | Code anchor |
|------|-----------|-------------|
| Necessary | `sourcePairKey` appears on a `ProgressionRules` or `PlacementAwardRules` path on `S` | [`CollectNecessaryConfrontationKeys`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) |
| Addressable | `S` has a `BracketPair` or a `Fixture` with that `BracketPairKey` | [`IsAddressableConfrontation`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) |
| Undecidable | `!ResolveEffectiveTieFormat(S, sourcePairKey).CanAlwaysDecideDraw()` | [`ResolveEffectiveTieFormat`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs), [`TieFormat.CanAlwaysDecideDraw`](../../../src/PlayUp/MyClub.PlayUp.Domain/Rules/TieFormat/TieFormat.cs) (V1: requires `PenaltyShootoutRule`) |

**Materialize scope:** Evaluate the predicate for the **stage being materialized** (and, if the command loads the full competition stage list, use the same stage set as Prepare for cross-stage progression — mirror [`PrepareStage.EnsurePrepareGateDiagnostics`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/PrepareStage/PrepareStage.cs), which today runs `AnalyzeExecution([target])` on the **target only**).

**Cup vs Championship/Groups:** Cup production uses [`MaterializeCupFromOccupiedSlots`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs), not [`MaterializeMatches`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeMatches/MaterializeMatches.cs) (which rejects Cup explicitly). **R-Mat-M1 canonical proof is on Cup** (EXEC-001). A Championship/Groups materialize test is **optional** until a format scenario shows necessary + addressable bracket confrontations at materialize time.

---

## Current runtime (gap)

| Layer | Behaviour today |
|-------|-----------------|
| Read / hub | `AnalyzeExecution` emits `ConfrontationUndecidable`; hub + readiness blocker ([`StructureViewAssembler`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureViewAssembler.cs)) |
| Prepare | Blocks via `IsPrepareGateIssue` + `Application.ConfrontationUndecidable` ([`PrepareStage`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/PrepareStage/PrepareStage.cs)) |
| Materialize | **No** execution gate — only [`StageMaterializeGuard.EnsureStageReadyForMaterialize`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StageMaterializeGuard.cs) (+ format/roster guards) |
| Domain `Stage.Prepare` | Does **not** evaluate tie decidability (application gate only) |

**Why a materialize gate still matters:** Defense in depth if Ready is reached without application Prepare (data repair, future operational regulation edits under §11A), and to align production commands with §5.2 without assuming Prepare always ran.

---

## Positive scenario (must block materialize)

**Story:** Cup knockout stage with a **necessary** winner progression from bracket pair `P1`, slots occupied, **no** penalty shootout on the effective tie format (default single leg).

**Fixtures in repo:**

- Application: [`PrepareStageTests.Execute_rejects_when_necessary_progression_confrontation_cannot_decide_draw`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/PrepareStageTests.cs)
- Read model: [`StructureViewAssemblerIssuesTests.Assemble_projects_ConfrontationUndecidable_without_StructureGraphInvalid`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructureViewAssemblerIssuesTests.cs)
- Development seed: [`BuildInvariantsExecUndecidableAsync`](../../../src/PlayUp/MyClub.PlayUp.Development/Orchestration/ScenarioOrchestration.cs) (multi-stage Demi → Finale, progression wired, **no TAB**)

**Expected after contract implementation:**

- `MaterializeCupFromOccupiedSlots.Execute(...)` on the **semi / KO stage** that owns `P1` refuses with `Application.ConfrontationUndecidable` (same family as Prepare), **before** creating fixtures/matches for that confrontation.
- Hub may still show `ConfrontationUndecidable` (unchanged); materialize does not rely on the UI read model.

**Test harness note:** Today `PrepareStage.Execute` prevents reaching Ready with this shape. For a **command test**, use a **Ready** stage in the undecidable state by:

1. Building the same domain graph as the positive Prepare rejection test (occupied slots, bracket pair, progression on `P1`, round tie format `null` → effective default one leg without TAB).
2. Calling **domain** [`Stage.Prepare`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) after domain prepare preconditions are met (application Prepare gate intentionally **not** invoked — documents “Ready without application gate” / future post-Ready drift).
3. Invoking `MaterializeCupFromOccupiedSlots.Execute` with eligible pair keys for `P1`.

This is **not** claiming product allows reaching Ready in this state through normal UX; it proves the **materialize command** enforces the predicate.

---

## Decision-maker arbitration

**§12 (2026-10-15):** G-MAT1 → **Ready** for Cup gate + P+/N1–N3 tests only. **No code** until §12 row matches this status.

| Criterion | Status | Condition |
|-----------|--------|-----------|
| **C1** Prédicat métier | **Accepted** | Mono-phase P+ graph is canonical; EXEC-001 Dev seed not in scope (see § EXEC-001). |
| **C2** Cas positif | **Accepted as test contract** (not proven in repo) | Assert `Application.ConfrontationUndecidable`; gate after Ready; no fixtures/matches created. |
| **C3** N1–N3 | **Accepted** | P+, N1, N3 validated; **N2** validated with dual-path contract (§ N2). Tests not yet in repo. |
| **C4** Harness Ready | **Accepted with condition** | Domain `Stage.Prepare` only; same graph as Prepare rejection fixture; materialize preconditions met. |
| **C5** Cause du refus | **Accepted** | Prove G-MAT1 gate, not an earlier guard; no partial materialization. |
| **C6** Périmètre | **Accepted** | Gate + tests G-MAT1 only. |
| **C7** Cup-only | **Accepted** | No `MaterializeMatches` gate in first slice. |

### EXEC-001 confirmation (C1 reserve)

- **Intent:** Semi stage (`Demi-finales`) — winners progress to finale slots `F-A` / `F-B` via [`WireWinnerProgressionToSlots`](../../../src/PlayUp/MyClub.PlayUp.Development/Orchestration/ScenarioOrchestration.cs). Each semi fixture’s `BracketPairKey` referenced in `ProgressionRules` is **necessary** for that outbound progression.
- **Pair keys:** On a typical entry-round semi, [`SeedEntryRoundBracketPairs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) yields **`P1`**, **`P2`** (same convention as [`MaterializeCupFromOccupiedSlotsTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeCupFromOccupiedSlotsTests.cs)).
- **Out of G-MAT1 scope:** `WireWinnerProgressionToSlots` calls [`EnsureDecisiveRoundTieFormats`](../../../src/PlayUp/MyClub.PlayUp.Development/Orchestration/ScenarioOrchestration.cs) (may add TAB on EXEC-001 seed). Do **not** change EXEC-001 Dev in this slice. **Canonical command proof** = mono-phase graph in [`PrepareStageTests.Execute_rejects_when_necessary_progression_confrontation_cannot_decide_draw`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/PrepareStageTests.cs).

---

## Test-first contracts (Cup — `MaterializeCupFromOccupiedSlots`)

**File (proposed):** `MaterializeCupFromOccupiedSlotsGMat1Tests.cs` (or extend [`MaterializeCupFromOccupiedSlotsTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeCupFromOccupiedSlotsTests.cs)).

**Shared harness**

- `competition` = `CreateCompetition.Execute(...)`.
- **Ready:** `stage.Prepare(clock)` when isolating G-MAT1 (P+); or `PrepareStage.Execute` when the graph is Prepare-legal (N1, N3).
- **Never** call `PrepareStage.Execute` on P+ graph (application gate would refuse first).
- **Invoke:** `MaterializeCupFromOccupiedSlots.Execute(competition, stage, pairKeys, existingMatches: [], clock)`.
- **G-MAT1 assertion:** `ApplicationFailureException.Code == ApplicationErrorCodes.ConfrontationUndecidable`.
- **No partial effects (P+ / C5):** after refused call, `stage.Rounds[*].Fixtures` count unchanged vs before; `existingMatches` still empty; no new `Match` attached on stage.

**Guard order (implementation contract):** run execution gate **immediately after** [`EnsureStageReadyForMaterialize`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StageMaterializeGuard.cs) (inside `EnsureMutable`), **before** `ResolveTargetPairs` creates fixtures.

### P+ — positive (G-MAT1 must refuse)

| Field | Value |
|-------|--------|
| **Graph** | Copy [`PrepareStageTests.Execute_rejects_when_necessary_progression_confrontation_cannot_decide_draw`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/PrepareStageTests.cs): round + fixture, slots `KO-A`/`KO-B`/`SF1-A`, `BracketPair("P1", ...)`, entries on `KO-A`/`KO-B`, `ProgressionRules` winner → `SF1-A` on `P1`, round `TieFormat` **null** (effective one leg, no TAB). |
| **Ready** | `stage.Prepare(clock)` (domain only). |
| **pairKeys** | `["P1"]` (eligible: both slots occupied, no fixture yet for `P1` — same eligibility rules as today). |
| **Expected** | Throws `Application.ConfrontationUndecidable`. |
| **Also assert** | No fixtures added for `P1`; `CreatedMatches` not returned (exception before result). |

### N1 — not necessary (no G-MAT1 refusal)

| Field | Value |
|-------|--------|
| **Graph** | Copy [`PrepareStageTests.Execute_does_not_apply_exec001_when_confrontation_is_ornamental`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/PrepareStageTests.cs): `P1` + occupied slots, **no** `ProgressionRules` / `PlacementAwardRules` on `P1`. |
| **Ready** | `PrepareStage.Execute(stage, [stage], clock)` **or** `stage.Prepare(clock)` (both valid). |
| **pairKeys** | `["P1"]`. |
| **Expected** | **Success** — `CreatedMatches` count 1; **no** `ConfrontationUndecidable`. |
| **Note** | Hub may still show other diagnostics; immaterial to this test. |

### N2 — not addressable (no G-MAT1 refusal)

**Goal:** Show a **necessary** `sourcePairKey` that is **not addressable** (`AnalyzeExecution` empty) while proving the command **reaches** post–G-MAT1 materialization logic — not a false negative from Draft status, bad Cup shape, or an unrelated guard.

**Canonical graph (dual-path — preferred):**

| Piece | Setup |
|-------|--------|
| Stage shape | Same as [`MaterializeCupFromOccupiedSlotsTests.CreateSemiStage`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeCupFromOccupiedSlotsTests.cs): round, slots `SF1-A`/`SF1-B`/`SF2-A`/`SF2-B`, `SeedEntryRoundBracketPairs()` → **`P1`**, **`P2`**. |
| **Necessary, not addressable** | `ReplaceProgressionRules` with winner on **`P1`** → `SF1-A` (as in P+), then **`ClearBracketPairs()`** and re-add **only** `BracketPair("P2", "SF2-A", "SF2-B")` — progression still references **`P1`**, but no `BracketPair` / fixture for `P1`. |
| **Eligible ornamental path** | Occupied distinct entries on `SF2-A` / `SF2-B` (`ApplyResolvedEntry`). **No** progression/placement on **`P2`**. |
| **Ready** | `PrepareStage.Execute(stage, [stage], clock)` — graph is Prepare-legal (no `ConfrontationUndecidable` because `P1` is not addressable). |
| **Pre-invoke assertions** | `stage.Status` is `Ready`; `CompetitionFlowAnalyzer.AnalyzeExecution([stage])` is **empty**; `IsEligible` for `P2` is true (occupied slots, no fixture on `P2`). |

| Field | Value |
|-------|--------|
| **pairKeys** | `["P2"]` only (do **not** request `P1`). |
| **Expected** | **Success** — `CreatedMatches` count 1; fixture on `P2`; **no** `Application.ConfrontationUndecidable`. |
| **Why this proves the three N2 conditions** | (1) **Materialization path reached** — match created on `P2`. (2) **G-MAT1 silent** — necessary-but-non-addressable `P1` does not emit undecidable at analyzer or command. (3) **Not a bogus “no eligible pair”** — failure would have meant mis-built occupancy or Ready, but `P2` eligibility was asserted up front. |

**Alternate graph (failure-only — use only if dual-path cannot be built in tests):**

- Single pair key: placement on `FINAL` + `ClearBracketPairs()` ([`CompetitionFlowAnalyzerTests.AnalyzeResult_emits_authored_award_unreachable_when_source_pair_missing`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/CompetitionFlowAnalyzerTests.cs)).
- **Ready:** `stage.Prepare(clock)` after round + slots satisfy domain prepare rules.
- **Pre-invoke:** `AnalyzeExecution` empty; `stage.Status` is `Ready`.
- **pairKeys:** `null` (request all eligible).
- **Expected:** `ApplicationErrorCodes.MaterializationFailure` (“No eligible bracket pairs…”), **explicitly not** `ConfrontationUndecidable`.
- **Mandatory comment in test:** this variant proves (2) and Ready preconditions only; **(1) full materialize path is not exercised** — prefer the **dual-path** variant for R-Mat-M1− addressable.

| Scenario arbitration (2026-10-15) | |
|-----------------------------------|---|
| **P+** | Accepted |
| **N1** | Accepted |
| **N2** | Accepted **with dual-path contract** above |
| **N3** | Accepted |

### N3 — always decidable (no G-MAT1 refusal)

| Field | Value |
|-------|--------|
| **Graph** | Copy [`PrepareStageTests.Execute_prepares_when_necessary_progression_has_penalty_shootout`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/PrepareStageTests.cs): necessary progression on `P1`, round tie includes `PenaltyShootoutRule`. |
| **Ready** | `PrepareStage.Execute(stage, [stage], clock)`. |
| **pairKeys** | `["P1"]`. |
| **Expected** | **Success** — 1 created match; **no** `ConfrontationUndecidable`. |

**C3 rule:** “No G-MAT1 refusal” ≠ “command always succeeds”. **N1**, **N2** (dual-path), and **N3** require **success** on the stated `pairKeys`; the alternate N2 failure-only variant is documented but not the signed contract.

---

## Proposed implementation shape (for review only — no code)

1. Extract a single application predicate, e.g. `StructureMaterializeExecutionGate` (name TBD), reusing the **same rules** as `AnalyzeExecution` for the target stage (ideally one shared internal helper to avoid drift from `CompetitionFlowAnalyzer`).
2. Call it from:
   - `MaterializeCupFromOccupiedSlots.Execute` (required for G-MAT1)
   - `MaterializeMatches.Execute` (required when R-Mat-M1 Ch/G scenario exists; otherwise defer)
3. **Do not** change Prepare behaviour, Ready/D1 guards, or add generic “block if hub contains code X”.

**Error surface:** Reuse `ApplicationErrorCodes.ConfrontationUndecidable` for parity with Prepare.

---

## Command tests to add (names map to § Test-first contracts)

| ID | R-Mat-M1 alias | Spec section |
|----|----------------|--------------|
| **P+** | R-Mat-M1+ | § P+ |
| **N1** | R-Mat-M1− necessary | § N1 |
| **N2** | R-Mat-M1− addressable | § N2 |
| **N3** | R-Mat-M1− decidable | § N3 |

**EXEC-001 Dev:** optional integration after unit P+; fix seed vs `EnsureDecisiveRoundTieFormats` drift before using DB snapshot as undecidable QA.

**Explicitly not sufficient:** Asserting `AnalyzeExecution` or hub `StructureIssues` only.

---

## Out of scope

- Prepare gate changes, drawer/attention severity, `StructureGraphInvalid` mapping
- G-OPS (Ready operational mutations), D1 upstream exit guards, draw-refuse composition rules
- G-MAT2 (`AuthoredAwardUnreachable`), G-MAT3 (`NonRootUnfed`), G-MAT4 (`SourcePopulationShortfall`)
- Blocking materialize when the hub lists `ConfrontationUndecidable` but the **predicate** is false
- Signing §11B or marking G-MAT1 **Done** in §12

---

## Validation checklist (before **Litigious → Ready**)

Sign-off is **two-step**: (1) product validates this checklist; (2) §12 G-MAT1 → **Ready**, then implement gate + tests only. **§11B** remains unsigned unless separately agreed.

| # | Criterion | Evidence expected |
|---|-----------|-------------------|
| **C1** | **Business predicate** — “necessary” and “addressable” are defined by **observable `AnalyzeExecution` behaviour** (progression/placement paths, bracket pair or fixture by key), not property names alone. | Decision maker confirms § “Question to decide” table + walkthrough of [`CollectNecessaryConfrontationKeys`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) / [`IsAddressableConfrontation`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) on Cup EXEC-001 semi. |
| **C2** | **Positive command proof** — `MaterializeCupFromOccupiedSlots` refuses when the predicate holds; assertion on **`Application.ConfrontationUndecidable`**, not a generic failure. | **R-Mat-M1+** test implemented or dry-run reviewed; gate runs **after** `EnsureStageReadyForMaterialize` so refusal is not “not Ready”. |
| **C3** | **Negative command proofs (N1–N3)** — each is a **distinct business scenario** on the **real Cup materialize path** (`pairKeys`, occupied slots, eligible pairs). | Three tests; each asserts **no** `Application.ConfrontationUndecidable` (and for N2/N1, materialize **succeeds** or fails only for an **unrelated** code documented in test). |
| **C4** | **Ready precondition** — Ready state is **domain-valid** (`Stage.Prepare` preconditions met); domain `Prepare` is an **isolation harness** for the materialize gate, not a bypass of unrelated domain invariants. | Document which domain checks `Prepare` enforces vs application gate; confirm graph matches positive Prepare rejection fixture. |
| **C5** | **Refusal cause** — positive test would **fail** if the execution gate were removed (regression intent); negatives do not pass only because another guard fires first with a different code. | Review guard order in `MaterializeCupFromOccupiedSlots.Execute`; optional: assert no fixtures/matches created on refusal. |
| **C6** | **Scope** — no implicit Prepare / Ready–D1 / G-MAT2–4 work in the same slice. | Diff limited to shared predicate helper + materialize call sites agreed in § “Proposed implementation shape”. |
| **C7** | **Ch/G deferral** — `MaterializeMatches` gate deferred until a scenario exists, or explicit scenario added. | Decision recorded (default: **deferred**, Cup-only R-Mat-M1). |

- [x] **C1** — Predicate accepted **with reserve** (EXEC-001 § above; decision maker 2026-10-15).
- [x] **C2** — Positive test **contract** accepted (implementation pending).
- [x] **C3** — N1–N3 **specified** in § Test-first contracts (tests **not** implemented).
- [x] **C4** — Ready harness accepted (domain `Prepare` on P+ graph).
- [x] **C5** — Refusal cause / guard order accepted.
- [x] **C6** — Scope accepted.
- [x] **C7** — Cup-only accepted.
- [x] §12 **G-MAT1** → **Ready** (2026-10-15, after N2 dual-path contract) — **then** implement gate + tests (no other scope).

---

## Related matrix rows

- Policy: [phase-mutability-matrix.md §5.2](./phase-mutability-matrix.md) (`ConfrontationUndecidable`)
- Backlog: [phase-mutability-matrix.md §12](./phase-mutability-matrix.md) (G-MAT1)
