# Organizer problems — transversal audit

**Document role:** Git-side **audit + semantic contract record**. Historical **VERIFIED** facts remain evidence from code; **Step 2** records product arbitration (2026-10-12). **Step 3 Phase 1** (2026-10-12) adds optional `problem` / `structureIssueSemantics` on three read families — additive only; **ProblemDetails** and Matchs pilot unchanged.

**Step 2 status:** **PARTIALLY DECIDED** (2026-10-12 product arbitration). **Step 3 Phase 1 status:** **IMPLEMENTED** (Application read models + TS types + pilot alias doc; SPA presentation not wired). Wire shape (single enum vs paired fields), full alias registry, and drawer/Overview consumption remain **DEFERRED** / **VERIFY** as stated per topic.

**Scope:** Structure, Competition Overview, and the shell **Needs attention** drawer, plus synchronous command failures (`ProblemDetails`). **Last code audit:** 2026-10-12.

**Related:** [Structure problems contract (Matchs column — paused)](./structure-problems-contract.md) · [Step 3 brief](./organizer-problems-step3-brief.md) · [Pilot aliases (D1)](./organizer-problems-pilot-aliases.md) · [Structure phase lifecycle UI](./structure-phase-lifecycle-ui.md) · SPA `functionalProblemPresentation.ts` (Phase 1 presentation contrat).

---

## How to read this document

| Tag | Meaning |
| :-- | :------ |
| **VERIFIED** | Checked against current C# / TypeScript / i18n in Git (file references where useful). |
| **DECIDED (Step 2)** | Product arbitration 2026-10-12 — normative for contract narrative; not implementation approval. |
| **DEFERRED** | Explicitly not decided; owner and resolution condition listed under Step 2. |
| **VERIFY** | Decision depends on code/API proof before claiming equivalence or behaviour. |
| **PROPOSED** | Legacy menu / hypotheses **not** adopted — safe to ignore unless re-opened. |
| **GAP** | Observed mismatch or missing wire field. |

---

## Step 2 — Semantic contract (2026-10-12 arbitration)

Arbitration order used: **1 → 3 → 2 → 4 → 5**. Use **testable phrase shapes** for pilot codes (illustration only — not adopted assignments until VERIFY). Example shape: *« {code} blocks {ActionCode} »* — for `OpenMatches` / `CompleteStage`, action assignment remains **VERIFY (V1)**, not Step 2 DECIDED.

### 1. Semantic levels — DECIDED (definitions); DEFERRED (wire shape)

**DECIDED — definitions (contractual phrasing):**

| Level | Meaning |
| :---- | :------ |
| **Gate** | Blocks one or more **specific** organizer actions (not “globally blocking” everywhere). |
| **Signal** | Diagnosed fact worth surfacing; **not**, by itself, the same as blocking an action in this contract. |
| **Attention** | Organizer intervention is expected; **does not** imply inclusion in the Needs attention drawer. |
| **Information** | Context for the organizer; no remediation required **within this contract**. |

These four terms are **not** assumed to be a single exclusive enum until wire shape is decided.

**DEFERRED:** Wire representation (single enum vs paired fields such as fact kind + organizer priority). **Owner:** product + tech lead. **Resolved when:** Step 3 documentation phase mock documents consumer needs without duplicating SPA tone logic.

**Rejected (for this perimeter):** Collapsing semantic level with UI severity (danger / warning) or with drawer visibility.

---

### 3. Action-relative blocking — DECIDED (principles); VERIFY (current contracts)

**DECIDED — principles:**

- Blocking is **relative to actions**, not a global “severity”.
- **Same business rule** should apply between read-model anticipation and POST refusal where both exist.
- **Distinct diagnostic causes** remain distinct (no silent merge of unrelated codes).
- At **command execution**, **POST outcome is authoritative** when read and command diverge.

**VERIFY before normative alias or DTO work:**

- Read vs POST alignment for pilot codes (`OpenMatches`, `CycleDetected`, `QualificationPending`).
- Documented behaviour for **abnormal** read/command divergence (support / retry policy — product if needed).

**DEFERRED:** Exact wire field name and shape (e.g. `blocksAction` list vs per-action capability map). **Owner:** tech lead. **Resolved when:** Step 3 documentation phase work on lifecycle transition + attention item (documentation only).

**Rejected:** A single global “Blocking” flag as the only semantic output.

---

### 2. Canonical codes and aliases — DECIDED (rule); DEFERRED (full registry); VERIFY (aliases)

**DECIDED — rule:**

- **Canonical code** = the **most precise, diagnostically useful** stable fact for the problem.
- **Aggregates** (e.g. readiness summaries) stay **distinct** from canonical analyzer facts.
- **Transport aliases** (HTTP `Application.*`, lifecycle disabled strings, `reasons[]` entries) are documented mappings — **not** separate business truths.
- Canonical choice **does not** determine drawer inclusion (see point 5).

**VERIFY (semantic equivalence — not DECIDED until checklist closes):**

- **V1:** `Application.StageCompletionBlocked` + `OpenMatches` in `reasons` vs lifecycle `OpenMatches` — same business rule?
- **V2:** `Application.StructureCycleDetected` vs `CycleDetected` on issues / Prepare disabled — same business rule?
- Scope and mapping of aggregate `StructureGraphInvalid` vs underlying issue codes (**V5**).

**DECIDED:** only the **canonicalization rule** above. **VERIFY:** which code is canon for each pilot row, and whether each POST alias is semantically equivalent (not merely string-related).

**DEFERRED:** Complete alias registry and deprecation timeline. **Owner:** product + engineering. **Resolved when:** VERIFY complete for pilot trio + first DTO enrichment spec.

**Rejected:** Big-bang rename of all wire strings; universal `OrganizerProblemDto`.

---

### 4. Read models and Step 3 investigation perimeter — DECIDED (scope)

**DECIDED — Step 3 investigation perimeter (documentation phase only — no implementation authorization):**

| Priority | Read surface (conceptual) | Pilot codes |
| :------- | :------------------------ | :---------- |
| Lifecycle transitions | Stage lifecycle projection (disabled / anticipated) | `OpenMatches`, `CycleDetected` |
| Needs attention | Attention items | `QualificationPending` |
| Structure issues | Hub `structureIssues` facts (detail preserved) | `CycleDetected` (and graph context as needed) |

**Constraints (DECIDED):**

- At most **three** read families in the first documentation phase.
- **`ProblemDetails` unchanged** (HTTP contract frozen).
- **No** universal problem DTO.
- **Matchs column local pilot** (`structure.problems`) **excluded** from Step 3 documentation phase — [structure-problems-contract.md](./structure-problems-contract.md) stays **Paused**.

**Does not authorize:** DTO/Application/Host/SPA changes, or any implementation work. Step 3 defines an **investigation and documentation perimeter** only, until a separate explicit approval after doc review + closed VERIFY.

---

### 5. Needs attention drawer — DECIDED (policy for current perimeter); VERIFY (implementation)

**DECIDED — contractual phrasing:**

> Application problem semantics **do not** automatically determine inclusion in the “Needs attention” drawer. For the **current arbitration perimeter**, **existing drawer selection policy is maintained**; any extension requires a **separate** product arbitration.

**DECIDED — consequences (product policy intent):**

- No drawer expansion in Step 3 documentation phase.
- No new allowlist/denylist rules in this arbitration — **maintain current policy intent**.
- **Attention** (semantic level) ≠ drawer placement.

**DECIDED (intent):** maintaining existing drawer **selection policy** for this arbitration perimeter (see contractual phrasing above). This is **policy intent**, not a claim about exact runtime behaviour.

**VERIFY (V4):** Exact current policy in code (`GET /attention` payload, `attentionPartition.ts`, `ATTENTION_DRAWER_CONSTRUCTION_DENYLIST`, Overview echo paths). Implementation details were **not** re-verified during the arbitration session; do not treat this Step 2 record as proof of code behaviour until V4 is closed.

**DEFERRED:** Whether current policy is optimal long-term. **Owner:** product. **Resolved when:** dedicated drawer/Overview surface review (out of Step 3 scope).

**Rejected:** Auto-including all Gate-level problems in the drawer based on Application semantics alone.

---

### Step 2 — What this session authorizes vs forbids

**Authorizes (documentation / planning only):**

- This Step 2 section and [Step 3 brief](#step-3-brief-documentation-only) below.
- Technical VERIFY checklist execution (read-only code audit).
- Notion decision page mirroring DECIDED principles (product SoT).

**Does not authorize:**

- Application, Host, DTO, or SPA changes.
- `ProblemDetails` changes.
- Resuming or generalizing the Matchs `structure.problems` pilot.
- Declaring an alias **verified** without code evidence.

---

## Step 3 — documentation phase + Phase 1 read enrichment

**Full brief:** [organizer-problems-step3-brief.md](./organizer-problems-step3-brief.md).

**Phase 1 (implemented 2026-10-12):** Optional **`OrganizerProblemReadDto`** on R1 lifecycle transitions and R2 attention items; **`StructureIssueSemantics`** on R3 hub issues — populated via **`OrganizerProblemSemanticsResolver`** for pilot codes only. Alias table: [organizer-problems-pilot-aliases.md](./organizer-problems-pilot-aliases.md). TS mirrors: `web/src/types/organizerProblem.ts` (optional fields on structure/attention DTOs). **Not in Phase 1:** Host/ProblemDetails changes, drawer/Overview UI branching, Matchs `structure.problems` unpause.

**Documentation still open:** **O2** (V3 qualification); wire shape **O3** DEFERRED. **O1 (V2 cycle prepare):** DECIDED Option B (layered model) — 2026-10-12.

---

## Technical VERIFY checklist (read-only — before alias claims)

**How to use this checklist (three distinct levels):**

1. **Observed (VERIFIED)** — What the code and tests actually emit (paths, fields, strings). No product interpretation yet.
2. **Conclusion (engineering)** — Whether read and POST (or alias pair) appear to enforce the **same** business rule; gaps and anomalies noted.
3. **Product DECIDED** — Only after (1) and (2) are written and reviewed (e.g. alias marked equivalent, canon chosen). **Filling the checklist does not auto-promote anything to DECIDED.**

| Item | Question | Entry points |
| :--- | :------- | :----------- |
| V1 | Lifecycle `OpenMatches` vs POST `StageCompletionBlocked` + `reasons` | `CompleteStage.cs`, `StructureStageLifecycleAssembler`, Host stage complete tests |
| V2 | `CycleDetected` on issue, Prepare disabled, POST `Application.StructureCycleDetected` | `CollectPrepareGateCodes`, `ApplicationErrorCodes`, Prepare use case |
| V3 | `QualificationPending` attention vs blocked actions | `NeedsAttentionAssembler`, qual apply preconditions |
| V4 | Drawer sources vs denylist vs `GET /attention` | `NeedsAttentionAssembler`, `attentionPartition.ts`, `AttentionDrawer.tsx` |
| V5 | `StructureGraphInvalid` aggregate vs hub issues | `StructureViewAssembler.BuildReadiness`, `CompetitionFlowAnalyzer.IsStructureGraphGateIssue` |

---

## VERIFY report (2026-10-12 — read-only code review)

**Status:** Engineering conclusions only. Does **not** extend Step 2 DECIDED. Alias/canonical rows move to normative contract only after product review of this report.

### V1 — `OpenMatches` vs `Application.StageCompletionBlocked`

| Level | Finding |
| :---- | :------ |
| **Observed (VERIFIED)** | Lifecycle: `ResolveCompleteTransition` sets `disabledReasonCode` to `CompleteStage.ReasonOpenMatches` (`"OpenMatches"`) when `CanComplete` is false; `CanComplete` when Running/Suspended and `open == 0` (matches not Finished/Cancelled). POST: `CompleteStage.Execute` throws `ApplicationFailureException` with `ApplicationErrorCodes.StageCompletionBlocked` and `Reasons` containing `"OpenMatches"` when any match status is not Finished/Cancelled. Host tests assert ProblemDetails code + reasons (`CompleteStageEndpointTests`, `HttpContractEndpointTests`). |
| **Conclusion (engineering)** | Same predicate (open matches block complete) on read anticipation and command. POST code is a **transport alias**; machine reason `"OpenMatches"` is the shared fact. **Candidate canon:** `OpenMatches`. **blocksAction (candidate):** `CompleteStage`. |
| **Product DECIDED** | Not extended by this report. Step 2 already treats POST/read alignment as VERIFY — this checklist item is **closed** for equivalence at engineering level; product may adopt canon/alias in Step 3 doc. |

### V2 — `CycleDetected` vs `Application.StructureCycleDetected`

| Level | Finding |
| :---- | :------ |
| **Observed (VERIFIED)** | `structureIssues[]` can contain `CycleDetected`. POST Prepare: `PrepareStage.EnsurePrepareGateDiagnostics` throws `Application.StructureCycleDetected` when `CycleDetected` is among prepare gate codes. Lifecycle Prepare: if `HasStructureGraphGate` (any issue where `IsStructureGraphGateIssue` — **includes** `CycleDetected`), transition uses `disabledReasonCode` **`StructureGraphInvalid`**, not `CycleDetected`, **before** `CollectPrepareGateCodes` runs. Gate-only issues such as `OverCapacityEdge` (graph gate false, prepare gate true) can show **`OverCapacityEdge`** on lifecycle and `Application.OverCapacityEdge` on POST. |
| **Conclusion (engineering)** | **Not** a single stable string alias across all read surfaces for `CycleDetected`: lifecycle often **aggregates** to `StructureGraphInvalid` while POST emits **`Application.StructureCycleDetected`**. Underlying prepare refusal still uses `CompetitionFlowAnalyzer` gate facts. Document **three** wire shapes (issue fact, lifecycle disabled, POST code) in Step 3 alias table — do not assume lifecycle disabled equals canon. |
| **Product DECIDED (O1, 2026-10-12)** | **Option B — layered model:** canon **`CycleDetected`** on hub `structureIssues[]`; lifecycle **`StructureGraphInvalid`** = **read projection** (aggregate disabled on Prepare); POST **`Application.StructureCycleDetected`** = **command transport**. Same underlying prepare gate rule; **consumers must not assume one wire string everywhere** — use `problem.canonCode` / `problem.transportCode` when present. |

### V3 — `QualificationPending` vs blocked actions

| Level | Finding |
| :---- | :------ |
| **Observed (VERIFIED)** | `NeedsAttentionAssembler` emits `QualificationPending` with `SeverityBlocking` when qualification destination is pending (`DestinationSatisfaction.Kind.Pending`) or destination stage missing. Overview maps source → `actionCode` `ApplyQualification`, impact `BlocksProgression`. Attention item is **not** a lifecycle disabled code; drawer shows items from `GET /attention` mapped via `needsAttentionItemsToSituations` (`nature` = severity, typically `Blocking`). |
| **Conclusion (engineering)** | Attention item signals **pending qualification work** and suggests **ApplyQualification** — it is **not** the same shape as lifecycle `disabledReasonCode`. Apply may be the **remediation**, not a blocked action in the CompleteStage sense. **Candidate semantic level (Step 2 defs):** **Attention**, not Gate on a single named transition. |
| **Product DECIDED** | Open: explicit `blocksAction` list for qualification (if any) vs “actionable attention only”. **Owner:** product. **Condition:** Step 3 doc for attention item semantics. |

### V4 — Drawer policy vs `GET /attention`

| Level | Finding |
| :---- | :------ |
| **Observed (VERIFIED)** | `NeedsAttentionAssembler` emits: `InsufficientParticipants`, `DrawNoSolution`, `QualificationPending` / `Conflict`, `ProgressionPending` / `Conflict` — **not** readiness blockers (`MissingStage`, `StructureGraphInvalid`, etc.). `AttentionDrawer` loads `fetchNeedsAttention`, maps to situations, then `partitionAttentionItems`: drops sources in `ATTENTION_DRAWER_CONSTRUCTION_DENYLIST`; splits remaining by `nature === 'Blocking'` vs else. All attention DTOs use `SeverityBlocking` today → mapped `Blocking` nature. |
| **Conclusion (engineering)** | Step 2 **policy intent** (semantics ≠ drawer; construction not in drawer payload) **matches current code**. Denylist is **defence in depth** if construction sources ever appeared on situations. Drawer does **not** auto-include Gate-level structure problems from structure read model. |
| **Product DECIDED** | Step 2 drawer maintenance **DECIDED** (intent) — **aligned with V4 observed behaviour**. Long-term optimality remains **DEFERRED** per Step 2 §5. |

### V5 — `StructureGraphInvalid` aggregate vs hub issues

| Level | Finding |
| :---- | :------ |
| **Observed (VERIFIED)** | `BuildReadiness` adds blocker `StructureGraphInvalid` when any hub has any `structureIssues` entry with `IsStructureGraphGateIssue(code)` (default true except e.g. `SlotFeedMissing`, `OverCapacityEdge`, population shortfalls, …). Hub rows still expose full `structureIssues[]` list. Lifecycle Prepare uses same `IsStructureGraphGateIssue` helper for early block with disabled `StructureGraphInvalid`. |
| **Conclusion (engineering)** | **Aggregate** `StructureGraphInvalid` is a **readiness/lifecycle summary** over a **subset** of issue codes; **canon fact** for a cycle remains **`CycleDetected`** on the hub. Multiple distinct graph gate issues collapse to one blocker string on readiness. |
| **Product DECIDED** | Step 2 rule “aggregates distinct from canon” **supported**. Open: remediation copy when lifecycle disabled is aggregate but remediation uses first graph gate issue code. **Owner:** product/UX. **Condition:** optional follow-up, not blocking Step 3 doc. |

---

## Three dimensions (DECIDED framing)

| Dimension | Owner | Step 2 |
| :-------- | :---- | :----- |
| 1. Business semantics | Application | Levels + action-relative blocking (wire DEFERRED) |
| 2. Surface policy | SPA per area | Drawer policy maintained; no auto-inclusion from semantics |
| 3. Presentation | SPA | Unchanged in Step 2/3 planning |

**VERIFIED nuance:** One code can block **one action** without being “globally blocking” (e.g. `OpenMatches` on `CompleteStage` — see lifecycle assembler and `CompleteStage.ReasonOpenMatches`).

---

## Executive summary (VERIFIED)

1. **Command failures** are partially centralized: `ApplicationFailureException` + `ApplicationErrorCodes` → `PlayUpExceptionHandler` → `ProblemDetails` (`extensions.code`, optional `reasons`). See `MyClub.PlayUp.Host/PlayUpExceptionHandler.cs`, `MyClub.PlayUp.Application/Common/ApplicationErrorCodes.cs`.

2. **Read-model “problems”** use **several parallel shapes** (issues list, readiness blockers, lifecycle disabled codes, attention items, overview situations). There is **no** single Application DTO for organizer-facing semantics today.

3. **Severity / gravity** is often decided in the **SPA**: e.g. `structureDiagnosticPresentation.ts` (tone), `functionalProblemPresentation.ts` (tone + canal), drawer partition by `situation.nature === 'Blocking'`. Application encodes **Gate vs Signal** in `CompetitionFlowAnalyzer` helpers but **does not** expose a uniform semantic field on the wire for structure issues.

4. **Overview situations** are built mainly from **Needs Attention** (Blocking) plus **CompetitionSuspended** (Informational). Structure readiness blockers are **not** merged into that list (VERIFIED comment in `OverviewAssembler.BuildSituations`). Construction blockers are surfaced on Structure / Overview construction dimensions via other paths.

5. **Drawer policy** excludes construction sources via SPA denylist even if they appeared on the wire (`attentionPartition.ts`, `ATTENTION_DRAWER_CONSTRUCTION_DENYLIST`). **VERIFIED:** `GET /attention` does not emit those construction blockers today — denylist is guardrail.

---

## Transport families (VERIFIED)

| # | Shape | Typical wire | Produced by (Application) |
| :- | :---- | :------------- | :--------------------------- |
| T1 | Sync command failure | HTTP + `ProblemDetails.code` (+ `reasons`) | Use cases → `ApplicationFailureException`; Host maps status |
| T2 | Lifecycle anticipation | `lifecycle.*.disabledReasonCode` | `StructureStageLifecycleAssembler` |
| T3 | Analyzer facts | `structureIssues[]` (string codes per hub) | `StructureViewAssembler.BuildStructureIssues` + `CompetitionFlowAnalyzer` |
| T4 | Readiness aggregate | `readiness.blockers[]` | `StructureViewAssembler.BuildReadiness` |
| T5 | Needs attention | `attention.items[].{ source, severity, … }` | `NeedsAttentionAssembler` |
| T6 | Overview situations | `situations[].{ source, nature, … }` | `OverviewAssembler` (mostly from T5 + suspended info) |
| T7 | Workspace anticipation | e.g. materialize `disabledReason` | `StructureMaterializeAssembler`, dematerialize projection |

Domain rule violations use `DomainException` → 409 + domain `Code` (VERIFIED `PlayUpExceptionHandler`).

---

## Application semantics today (VERIFIED)

### Gate vs Signal (analyzer — not on wire as enum)

`CompetitionFlowAnalyzer` (VERIFIED):

- **`IsStructureGraphGateIssue(code)`** — contributes to readiness blocker `StructureGraphInvalid` when any hub issue matches (with explicit **false** list: e.g. `SlotFeedMissing`, population shortfalls, `ConfrontationUndecidable`, …).
- **`IsPrepareGateIssue(code)`** — Prepare must refuse when issue present on stage (e.g. `CycleDetected`, `OverCapacityEdge`, …).

Comments in `StructureViewAssembler` and `CompetitionFlowAnalysis` state analyzer output is **facts only**; Prepare / readiness apply policy.

### Readiness blockers (T4)

Constants in `StructureViewAssembler` (VERIFIED strings on wire):

| Blocker code | Origin (summary) |
| :----------- | :----------------- |
| `InsufficientParticipants` | Active entries &lt; minimum |
| `MissingStage` | No primary stage |
| `MissingStructure` | No format kind |
| `MissingPotRules` | Groups + draw engaged, no pots |
| `CupBracketInvalid` | Cup entry places invalid |
| `StructureGraphInvalid` | Any hub has graph gate issue |
| `ConfrontationUndecidable` | Issue on a hub |
| `OverCapacityEdge` | Issue on a hub |
| `OverExtraction` | Issue on a hub |

### Structure issue codes (T3)

Issued on hub rows (VERIFIED constants / analyzer codes in `StructureViewAssembler` + flow analyzer), including:

`DanglingQualificationTarget`, `MissingQualificationDestinationSlot`, `MissingQualificationDestinationGroup`, `DanglingProgressionTarget`, `MissingProgressionDestinationSlot`, `MissingProgressionDestinationGroup`, `SlotFeedInvalid`, `SlotFeedMultiple`, `SlotFeedMissing`, `ConfrontationUndecidable`, `CycleDetected`, `NonRootUnfed`, `RoutingNondeterminism`, `OverCapacityEdge`, `OverExtraction`, `SourcePopulationShortfall`, `PopulationShortfall`, `AuthoredAwardUnreachable`, …

SPA also maps `ExpectedRankUnreachable` in `structureDiagnosticPresentation.ts` — **verify** Host emission if auditing parity (GAP if analyzer does not emit).

### Needs Attention sources (T5)

`NeedsAttentionAssembler` (VERIFIED) — all items use `SeverityBlocking = "Blocking"` today:

| Source | Target (typical) |
| :----- | :---------------- |
| `InsufficientParticipants` | Competition |
| `DrawNoSolution` | Draw |
| `QualificationPending` / `QualificationConflict` | Slot |
| `ProgressionPending` / `ProgressionConflict` | Slot |

### Overview situation nature (T6)

| Source | Nature (VERIFIED) |
| :----- | :---------------- |
| Attention items | `Blocking` |
| `CompetitionSuspended` | `Informational` |

Impact / action codes mapped in `OverviewAssembler` (e.g. `ApplyProgression`, `AddEntry`, impact `BlocksDraw`, …).

### Lifecycle disabled codes (T2)

Constants in `StructureStageLifecycleAssembler` (VERIFIED), including:

`CompetitionClosed`, `ConstructionFrozen`, `InitialRoundNotPlayable`, `StageNotReady`, `StructureGraphInvalid`, `ProductionPresent`, `UnmaterializedConfrontations`, `MatchLifecycleStarted`, `MatchLiveInProgress`, Prepare checklist segments (`Places`, `Draw`, `Entries`, `Matches`, `Exits`, `Attribution`), and **direct analyzer codes** from `CollectPrepareGateCodes` (e.g. `CycleDetected`, `OverCapacityEdge`, …).

Complete transition uses `CompleteStage.ReasonOpenMatches` (`"OpenMatches"`) when `CanComplete` is false.

### Materialize / dematerialize anticipation (T7)

`StructureMaterializeDisabledReasons`: `StructureNotMutable`, `NoDeterminedConfrontations`, `GroupsNotReady`, `NoEligibleConfrontations`.

Dematerialize: `DematerializeDisabledReasons` (+ POST mapping in `DematerializationCapability.ThrowForDisabledReason`).

### HTTP Application codes (T1)

Full catalogue: `ApplicationErrorCodes.cs` (VERIFIED ~70+ constants, `Application.*` prefix). Host maps a **subset** to specific HTTP statuses; remainder → 400. SPA copy: `web/src/i18n/locales/*/errors.json` namespace `Application.*` via `apiErrorLabel`.

---

## Surface matrix (VERIFIED — where codes appear today)

Legend — **semantics on wire today:**

- **App-B** = readiness blocker / explicit blocking attention  
- **App-G** = prepare or lifecycle gate (implicit via disabled / exception)  
- **App-S** = analyzer signal (issue list, not necessarily blocker)  
- **App-I** = informational situation  
- **SPA-T** = tone/gravity chosen in SPA only  

### Runtime attention (drawer + Overview preview)

| Source | Origins | Semantics today | Structure | Overview situations | Drawer |
| :----- | :------ | :-------------- | :-------- | :------------------ | :----- |
| `DrawNoSolution` | T5 | App-B | Draw UX | Yes | Yes (runtime) |
| `QualificationPending` / `Conflict` | T5 | App-B | Dialogs / apply | Yes | Yes |
| `ProgressionPending` / `Conflict` | T5 | App-B | Dialogs / apply | Yes | Yes |
| `InsufficientParticipants` | T5 (+ T4 blocker) | App-B | Teams / readiness | Yes | Yes |
| `CompetitionSuspended` | T6 only | App-I | — | Yes | Partition non-Blocking group |

Drawer: `partitionAttentionItems` skips `ATTENTION_DRAWER_CONSTRUCTION_DENYLIST` sources if ever present.

### Construction (Structure-heavy)

| Code / family | Origins | Semantics today | Structure | Overview | Drawer |
| :------------ | :------ | :-------------- | :-------- | :------- | :----- |
| `structureIssues[]` (per code) | T3 | App-G / App-S + **SPA-T** | Topology, rails, dialogs | Echo via construction dims | Denylist (not in T5) |
| `readiness.blockers[]` | T4 | App-B | Hub / pagehead via presentation helper | Construction cards / readiness | Denylist |
| `StructureGraphInvalid` | T4 aggregate | App-B | Echo + enums copy | Echo | Denylist |
| Lifecycle disabled (Prepare/Start/Complete/…) | T2 | App-G (action-specific) | Matchs column (+ local SPA visibility) | — | — |

### Lifecycle + POST overlap (example VERIFIED)

| Wire code | T2 disabled | T1 POST | Notes |
| :-------- | :---------- | :------ | :---- |
| `OpenMatches` | Complete CTA | `Application.StageCompletionBlocked` + `reasons: ["OpenMatches"]` | Same string in reasons; not same field as lifecycle |
| `OverCapacityEdge` | Prepare (gate list) | `Application.OverCapacityEdge` | Prepare command |
| `CycleDetected` | Prepare (gate list) | `Application.StructureCycleDetected` | **Different string** POST vs issue/lifecycle |

---

## i18n map (VERIFIED — primary namespaces)

| Content | Namespace / keys | Consumers |
| :------ | :--------------- | :-------- |
| Structure graph diagnostics | `structure:graph.issues.*` | Topology, `structureDiagnosticPresentation` |
| Structure lifecycle disabled | `structure:lifecycle.disabled.*` | Legacy / some surfaces |
| Structure lifecycle (Matchs pilot) | `structure:problems.lifecycle.*`, `problems.mutation._unknown` | Matchs column module (`pages/structure/problems/`) |
| Attention / overview labels | `enums:attentionSource.*`, `attentionDescription.*` | Drawer, Overview, `functionalProblemPresentation` |
| Command failures | `errors:Application.*`, `errors:Domain.*` | Mutations, `apiErrorLabel` |
| Materialize / dematerialize | `structure:fiche.materialize.disabledReason.*`, `fiche.dematerialize.*` | Structure production strip |

**GAP:** Same underlying fact may have **three** key paths (e.g. `OverCapacityEdge` in `graph.issues`, `lifecycle.disabled`, `problems.lifecycle`, `enums.attentionSource`, `errors.Application`).

---

## SPA presentation layer (VERIFIED)

| Module | Role |
| :----- | :--- |
| `structureDiagnosticPresentation.ts` | Maps `structureIssues` code → family, tile, CTA, **tone** (danger / attention / progress) |
| `functionalProblemPresentation.ts` | Maps source / issue → **tone**, **canal**, titleNs, href; hard-coded sets `CONSTRUCTION_BLOCKERS`, `RUNTIME_ATTENTION`, draw create blocks |
| `pages/structure/problems/*` | Matchs lifecycle: normalize, resolve message, **column visibility** (pilot) |
| `attentionPartition.ts` | Drawer blocking vs attention groups + construction denylist |

---

## PROPOSED — legacy menu (superseded for principles)

**Superseded by [Step 2](#step-2--semantic-contract-2026-10-12-arbitration)** for semantic levels, blocking model, drawer policy, and Step 3 investigation perimeter. Remaining **PROPOSED** content: full alias registry beyond the pilot trio — **DEFERRED** until VERIFY completes.

Pilot alias candidates (wire strings **VERIFIED** in code audit 2026-10-12; **canon choice VERIFY**; **POST semantic equivalence VERIFY V1/V2**):

| Hypothesized canon (canonicalization **rule** DECIDED; code choice **VERIFY**) | Aliases — document mapping; POST equivalence **VERIFY** |
| :----------------------------------------------------------------------------- | :-------------------------------------------------------- |
| `OpenMatches` (VERIFY) | Lifecycle; POST `Application.StageCompletionBlocked` + `reasons` (**V1**) |
| `CycleDetected` (VERIFY) | `structureIssues[]`; Prepare disabled; POST `Application.StructureCycleDetected` (**V2**) |
| `StructureGraphInvalid` (aggregate, VERIFY vs issues **V5**) | Readiness aggregate — **distinct from** single-issue canon |

---

## Contradictions / risks (VERIFIED + GAP)

1. **Gravity duplication:** Application Gate/Signal vs SPA tone vs `Nature`/`severity` enums.  
2. **Alias drift:** POST `Application.*` vs short codes on reads (table above).  
3. **Drawer vs Overview vs Structure** policy encoded in SPA sets, not Application channel flags.  
4. **Matchs pilot (`structure.problems`)** adds another i18n path — risk of parallel catalogue if generalized before step 2.  
5. **`attentionSeverity` in enums** (Warning / Info) — **VERIFIED** in i18n; **GAP** Needs Attention wire uses Blocking only today.

---

## Recommended sequence

1. **Audit + Step 2 record** — this document (2026-10-12).  
2. **VERIFY checklist** — read-only code proof for pilot aliases and drawer policy.  
3. **Step 3 documentation phase** — [organizer-problems-step3-brief.md](./organizer-problems-step3-brief.md); implementation only after brief + proposal sign-off.  
4. **Matchs pilot** — only if explicitly unpaused in a future decision (currently excluded).  
5. **Overview + drawer evolution** — separate product arbitration if policy change is desired.

---

## Code index (VERIFIED entry points)

| Area | Path |
| :--- | :--- |
| HTTP errors | `MyClub.PlayUp.Host/PlayUpExceptionHandler.cs` |
| Application codes | `MyClub.PlayUp.Application/Common/ApplicationErrorCodes.cs` |
| Analyzer | `MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs` |
| Structure read model | `MyClub.PlayUp.Application/Reads/StructureViewAssembler.cs` |
| Lifecycle projection | `MyClub.PlayUp.Application/Reads/StructureStageLifecycleAssembler.cs` |
| Attention | `MyClub.PlayUp.Application/Reads/NeedsAttentionAssembler.cs` |
| Overview situations | `MyClub.PlayUp.Application/Reads/OverviewAssembler.cs` |
| SPA functional presentation | `PlayUp/web/src/shell/functionalProblemPresentation.ts` |
| SPA Matchs pilot | `PlayUp/web/src/pages/structure/problems/` |
