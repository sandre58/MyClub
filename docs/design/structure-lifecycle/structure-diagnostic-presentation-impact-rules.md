# Structure diagnostic presentation — impact rules & step 2 deliverable

**Status:** Signed product input (step 2 complete — documentation only).  
**Prerequisite:** [structure-diagnostic-presentation-contract.md](./structure-diagnostic-presentation-contract.md).  
**Next:** Presentation slice complete in Git (steps 3–5). All structure hub / stage competition mutation footers pass `structureStages` → `formatStructureMutationMessage` (no HTTP suffix). Other app surfaces (teams, match sheet, home) still use generic `formatError` + `errorWithStatus`.

This document closes footnote ² ambiguities **without** reopening signed contract rows (Ready visual danger for population shortfalls, G-UI1, masking rules).

---

## 1. Impact policy (formal)

### 1.1 Evaluation order

For each presented diagnostic code and context:

1. Apply **masking** (draw awaiting, Authored/ExpectedRank projection, tile banner rules) → may yield `hidden`.
2. Apply **matrix baseline** from [§2 implementation matrix](#2-implementation-matrix-consolidated) using `presentationPhaseBucket` (§3).
3. Apply **signed overrides** (e.g. PopulationShortfall Ready → visual `blocking`/`danger`).
4. Apply **impact promotion** only if an entry exists in [§1.2 registry](#12-operational-impact-registry) and `operationalImpact === established`.
5. If impact is **unknown** or **absent**, never promote beyond baseline; footnote ² cells stay `attention` (or `progress` where defined).

**Forbidden:** inferring `operationalImpact === established` from `stageStatus` alone (Ready / Running).

### 1.2 Operational impact registry

| Diagnostic | Promotion to presentation `danger` allowed? | Rule (testable) | Status |
|------------|-------------------------------------------|-----------------|--------|
| `OverCapacityEdge` | **No** (step 2 decision) | Stay `attention` in Draft, Ready, Running until a **future** rule names a concrete operational consequence | **Closed — no promotion** |
| `SourcePopulationShortfall` | Ready column only | **Matrix override:** visual `danger` in Ready; not impact-based | **Signed in contract** |
| `PopulationShortfall` | Ready column only | Same as above (+ U1 synthesis) | **Signed in contract** |
| `ExpectedRankUnreachable` | Only with established impact | `established` iff a documented predicate proves a **rank required for exploitation** is unreachable (format-specific) | **Open — no predicate shipped** → always baseline `attention` |
| `NonRootUnfed` | Ready / Running only | `established` iff a documented predicate proves materialize/start **requires** feed from this phase and it is impossible | **Deferred** (G-MAT3 gate deferred) → baseline `attention` in Ready/Running |
| All other matrix `danger` rows | N/A | Fixed `danger` — not footnote ² | — |

**API shape (implementation step 3):**

```text
operationalImpact(code, context) → 'unknown' | 'absent' | 'established'
```

Default for footnote ² rows: **`unknown`** → presentation stays at baseline (`attention`).

### 1.3 `incompletenessExpected` (NonRootUnfed → `progress`)

**Reliable signal (mirrors analyzer facts, no new domain gate):**

`incompletenessExpected === true` when **all** hold:

- `stageStatus === Draft`
- Code is `NonRootUnfed`
- Stage is **not** the competition entry baseline stage
- **No inbound feed** from qualification/progression paths (same intent as `HasInboundFeed` in [`AppendNonRootUnfed`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs))
- `affectationEntryCount === 0` (no composed entries on the stage)

Otherwise `incompletenessExpected === false` → use `attention` in Draft (not `progress`).

**SPA:** implement via shared helper aligned with hub summary fields (or port of inbound-feed check in tests first).

---

## 2. Implementation matrix (consolidated)

Presentation only. `S` = `stageStatus` bucket (§3). `FK` = `formatKind` where noted.

| Code | Draft | Ready | Running | Notes |
|------|-------|-------|---------|-------|
| `CycleDetected` | danger | danger | danger | |
| `RoutingNondeterminism` | danger | danger | danger | |
| `OverExtraction` | danger | danger | danger | |
| `Dangling*` / `Missing*` qual/prog | danger | danger | danger | |
| `SlotFeedInvalid` / `SlotFeedMultiple` | danger | danger | danger | Unlisted in contract — unchanged blocking |
| `SlotFeedMissing` | attention¹ | danger¹ | danger¹ | ¹ hide when draw awaiting; Draft = expected fill-in-progress |
| `OverCapacityEdge` | attention | attention | attention | No impact promotion (§1.2) |
| `PopulationShortfall` | attention | **danger** | attention | U1 synth uses same row |
| `SourcePopulationShortfall` | attention | **danger** | attention | |
| `NonRootUnfed` | progress³ \| attention | attention | attention | ³ §1.3; no Ready danger until registry rule |
| `AuthoredAwardUnreachable` | danger if Cup/KO else attention | same | same | G-UI1 |
| `ExpectedRankUnreachable` | attention | attention | attention | No promotion until §1.2 rule exists |
| `ConfrontationUndecidable` | danger | danger | danger | |

**Readiness blockers** (not `structureIssues`): present with `danger` until classified (`CupBracketInvalid`, `StructureGraphInvalid`, …). **`Completed` stage:** hide on structure hub (§3).

---

## 3. `Suspended` and `Completed`

| Hub `StageStatus` | `presentationPhaseBucket` | Rationale |
|-------------------|---------------------------|-----------|
| `Draft` | `Draft` | |
| `Ready` | `Ready` | |
| `Running` | `Running` | |
| `Suspended` | **`Running`** | **Explicit regroupement (signed step 2):** structure diagnostics describe structural facts, not match clock. Match UI keeps its own suspended copy/tone. **Not** a new product axis. |
| `Completed` | **`hidden`** | Do not present construction `structureIssues` on structure hub tiles/rails for that phase. Sportive/runtime attention may still appear elsewhere. |

Document this regroupement in code comments only as “presentation bucket”, not as domain lifecycle.

---

## 4. Reference cases (presentation tests)

IDs are for step 3 automated tests / manual recipe. **Gates unchanged** — assert presentation outputs only.

### 4.1 Population shortfalls (contract)

| ID | Setup | Expected presentation |
|----|--------|------------------------|
| **R-Pres-PS-1** | Draft, `PopulationShortfall` (or U1 synth) | `warning` / `attention` |
| **R-Pres-PS-2** | Ready, same | `blocking` / `danger` (visual only) |
| **R-Pres-PS-3** | Running, same | `warning` / `attention` (no status-only promotion) |
| **R-Pres-SPS-1** | Ready, `SourcePopulationShortfall` | `danger` visual |

### 4.2 OverCapacityEdge

| ID | Setup | Expected |
|----|--------|----------|
| **R-Pres-OC-1** | Draft / Ready / Running, `OverCapacityEdge` | always `attention` |
| **R-Pres-OC-2** | Ready + matches materialized (operational stress) | still `attention` (no rule in registry) |

### 4.3 NonRootUnfed

| ID | Setup | Expected |
|----|--------|----------|
| **R-Pres-NR-1** | Draft, non-root, no inbound qual/prog, no affectation | `progress` |
| **R-Pres-NR-2** | Draft, non-root, qual path targets stage but unfed | `attention` |
| **R-Pres-NR-3** | Ready, `NonRootUnfed` | `attention` (impact unknown) |
| **R-Pres-NR-4** | Running, foreign phase shortfall irrelevant to batch | presentation unchanged on fed phase (A5 world — optional cross-link M2b) |

### 4.4 ExpectedRankUnreachable

| ID | Setup | Expected |
|----|--------|----------|
| **R-Pres-ER-1** | Draft/Ready/Running, rank unreachable | `attention` |
| **R-Pres-ER-2** | With `AuthoredAwardUnreachable` on same stage | ExpectedRank **hidden** (projection) |

### 4.5 AuthoredAward (G-UI1)

| ID | Setup | Expected |
|----|--------|----------|
| **R-Pres-AA-1** | Cup/KO, AuthoredAward | `danger` |
| **R-Pres-AA-2** | Championship/Groups classifying | `attention` |

### 4.6 Masking & parity

| ID | Setup | Expected |
|----|--------|----------|
| **R-Pres-M-1** | `SlotFeedMissing`, draw engaged awaiting | `hidden` on presented sets |
| **R-Pres-M-2** | Same context, Host attention vs SPA fiche | same severity |
| **R-Pres-ST-1** | Completed phase with raw `structureIssues` in API | **hidden** on structure hub |
| **R-Pres-ST-2** | Suspended + `OverCapacityEdge` | **Running** column → `attention` |

### 4.7 Impact unknown (meta)

| ID | Assertion |
|----|-----------|
| **R-Pres-IMP-0** | For `OverCapacityEdge`, `ExpectedRankUnreachable`, `NonRootUnfed` (Ready/Running), `operationalImpact` default `unknown` never yields `danger` except where matrix explicitly says otherwise (PS Ready). |

---

## 5. Structure mutation wire codes — inventory

**Scope:** Errors surfaced on **Structure** pages (hub, fiche, dialogs, operational strip) via `MutationError` or lifecycle bands.

**Resolution paths:**

| Path | When | Fallback |
|------|------|----------|
| **A** `formatStructureMutationMessage` → `problems.lifecycle.*` | `MutationError` + `structureStages` (qual, prog, composition) | `problems.lifecycle._unknown` → `apiErrorLabel` → English `detail` → `errorWithStatus` |
| **B** `resolveLifecycleMutationMessage` | Lifecycle transitions on fiche (`StructurePhaseMatchsLifecycleBlock`) | same chain |
| **C** `formatError` / `apiErrorLabel` | Other `MutationError` (draw, materialize, graph, rename, …) | `errors:Application.*` nested key; missing → English `detail`; **no** HTTP suffix in path C unless common wrapper added elsewhere |

### 5.1 Lifecycle & prepare (path A/B)

| Wire code | Producer (representative) | `problems.lifecycle` / map | `errors.Application` | Structure surface |
|-----------|---------------------------|------------------------------|----------------------|-------------------|
| `Application.CompetitionClosed` | `EnsureCompetitionAllowsLifecycleMutation` | yes | yes | lifecycle |
| `Application.ConfrontationUndecidable` | Prepare / materialize gates | yes | yes | prepare, materialize |
| `Application.Attribution` | Prepare attribution, G-MAT2 gate | yes (`Attribution`) | **no** | prepare, materialize Cup |
| `Application.OverCapacityEdge` | Prepare | yes | yes | prepare |
| `Application.OverExtraction` | Prepare | yes | yes | prepare |
| `Application.StructureCycleDetected` | Prepare | yes (`CycleDetected` key) | yes | prepare |
| `Application.RoutingNondeterminism` | Prepare | yes | yes | prepare |
| `Application.SlotFeedsInvalid` | Prepare slot resolver | yes | yes | prepare |
| `Application.ReadyConsumerWouldBreak` | G-OPS2 | yes + consumers | yes | qual, prog, composition |
| `Application.AppliedDrawPoolChangeBlocked` | G-B-3 (`StructureAppliedDrawPoolChangeGate`) | yes | **no** | composition (pool replace) |
| `Application.AppliedDrawSlotPlacementProtected` | G-OPS3 B1 (`StructureAppliedDrawSlotPlacementGate`) | yes | **no** | manual Cup placement |
| `Application.RevertToDraftBlocked` | Revert | yes (`ProductionPresent`) | **no** | lifecycle |
| `Application.RevertToReadyBlocked` | Suspend / revert ready | yes (`MatchLifecycleStarted`) | **no** | lifecycle |
| `Application.StartBlockedUnmaterialized` | Start guard | yes | **no** | lifecycle |
| `Application.StageExploitationClosed` | Phase exploitation guard | yes (`CompetitionClosed` map) | **no** | lifecycle / ops |
| `Application.StageCompletionBlocked` | Complete | yes (`OpenMatches`) | yes | lifecycle |

**Anticipated (read-only, not mutation):** `problems.lifecycle` via `normalizeLifecycleAnticipated` — checklist codes `Entries`, `Places`, `Draw`, `Matches`, `Exits`, `Attribution`, `StageNotReady`, `StructureGraphInvalid`, `ConstructionFrozen`, etc.

### 5.2 Structure edit & OPS (path C unless mapped)

| Wire code | Producer | `errors.Application` | Typical surface |
|-----------|----------|------------------------|-----------------|
| `Application.StructureNotMutable` | `StageStructureEditGuard`, OPS D2, materialize guard | yes | graph, placement, OPS |
| `Application.InvalidStructureIntent` | Replace rules, slots, rounds | yes | graph, regulation |
| `Application.MaterializationFailure` | Materialize cup/ch/groups | yes | operational strip |
| `Application.ConfrontationUndecidable` | `StructureMaterializeExecutionGate` | yes | materialize |
| `Application.DematerializationBlocked` | `DematerializationCapability` | **no** | dematerialize |
| `Application.DematerializationFailure` | Dematerialize preflight | **no** | dematerialize |
| `Application.MaterializationStateInconsistent` | Dematerialize Swiss/Cup | **no** | dematerialize |
| `Application.StageNotInCompetition` | Many stage commands | yes | various |
| `Application.CupBracketNotPowerOfTwo` | Cup structure | yes | forme |

### 5.3 Draw (path C)

| Wire code | Producer | `errors.Application` |
|-----------|----------|----------------------|
| `Application.DrawRulesRequired` | Create draw | yes |
| `Application.DrawGenerationFailure` | Generate resolution | yes |
| `Application.DrawApplyFailure` | Apply draw | yes |
| `Application.DrawReleaseFailure` | Release placements | yes |
| `Application.DrawKindNotSupported` | Draw pipeline | yes |

### 5.4 Swiss / schedule (structure strip)

| Wire code | Producer | `errors.Application` |
|-----------|----------|----------------------|
| `Application.SwissRoundGenerationFailure` | `GenerateNextRound` | yes |
| `Application.ScheduleGenerationFailure` | Schedule (if exposed) | yes |
| `Application.ScheduleApplyFailure` | Schedule apply | yes |

### 5.5 Gaps for step 5 (i18n)

Add `errors.Application` (or `problems.lifecycle`) entries before unifying copy:

- `DematerializationBlocked`
- `DematerializationFailure`
- `MaterializationStateInconsistent`
- `RevertToDraftBlocked`
- `RevertToReadyBlocked`
- `StartBlockedUnmaterialized`
- `StageExploitationClosed`
- `StageExploitationNotOpen`
- `Application.Attribution` in **errors** namespace (duplicate of lifecycle message for path C)
- `MatchLiveInProgress` (lifecycle copy exists; errors path may miss)

Remove organizer-facing `errorWithStatus` from path A when implementing step 5.

### 5.6 Surfaces using structure-aware mutation copy

| Component | `structureStages` passed? |
|-----------|---------------------------|
| `StructureQualificationDialog` | yes |
| `StructureProgressionDialog` | yes |
| `StructureCompositionDialog` | yes |
| `StructurePhaseMatchsLifecycleBlock` | lifecycle resolver only |
| All other `MutationError` | **no** → path C |

---

## 6. Step 2 exit checklist

- [x] Impact rules explicit — no status-only promotion (§1)
- [x] `OverCapacityEdge` — attention only (§1.2)
- [x] `ExpectedRankUnreachable` / `NonRootUnfed` Ready danger — deferred / unknown (§1.2)
- [x] Reference cases including unknown impact (§4)
- [x] `Suspended` / `Completed` decision (§3)
- [x] Mutation wire inventory with origins and fallbacks (§5)
- [x] Consolidated implementation matrix (§2)

**Step 3 may start** when implementation owners accept §3 regroupement Suspended→Running (presentation only).

---

## 7. Link to contract checklist (recipe)

Maps to signed recipe sections:

- **A.1–A.6** — §2 matrix + §1.3 + §1.2 + R-Pres-* cases  
- **B.*** — surfaces unchanged in contract; wiring in step 4  
- **C.*** — §5 gaps + catalogue short/long (step 5)  
- **D.*** — gates untouched; R-Pres-M-2 parity tests in step 3
