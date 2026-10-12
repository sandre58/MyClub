# Organizer problems — transversal audit (working draft)

**Status:** **Working document — not normative.** Nothing in this file is product or HTTP contract until **step 2** (semantic contract arbitration) is **DECIDED** in Notion / team process. Safe to use for analysis, PR discussion, and gap identification; do not treat **PROPOSED** sections as shipped behaviour.

**Scope:** Structure, Competition Overview, and the shell **Needs attention** drawer, plus synchronous command failures (`ProblemDetails`). **Last verified against code:** 2026-10-12.

**Related:** [Structure problems contract (Matchs column — paused)](./structure-problems-contract.md) · [Structure phase lifecycle UI](./structure-phase-lifecycle-ui.md) · SPA `functionalProblemPresentation.ts` (Phase 1 presentation contrat).

**Target direction (discussion only, not adopted):** Application owns **business meaning** and **severity relative to actions**; each surface owns **channel policy** and **presentation**. See § [Three dimensions](#three-dimensions-discussion-frame).

---

## How to read this document

| Tag | Meaning |
| :-- | :------ |
| **VERIFIED** | Checked against current C# / TypeScript / i18n in Git (file references where useful). |
| **PROPOSED** | Hypothesis for step 2 — canonical codes, alias tables, or semantic levels **not** decided. |
| **GAP** | Observed mismatch or missing wire field. |

---

## Executive summary (VERIFIED)

1. **Command failures** are partially centralized: `ApplicationFailureException` + `ApplicationErrorCodes` → `PlayUpExceptionHandler` → `ProblemDetails` (`extensions.code`, optional `reasons`). See `MyClub.PlayUp.Host/PlayUpExceptionHandler.cs`, `MyClub.PlayUp.Application/Common/ApplicationErrorCodes.cs`.

2. **Read-model “problems”** use **several parallel shapes** (issues list, readiness blockers, lifecycle disabled codes, attention items, overview situations). There is **no** single Application DTO for organizer-facing semantics today.

3. **Severity / gravity** is often decided in the **SPA**: e.g. `structureDiagnosticPresentation.ts` (tone), `functionalProblemPresentation.ts` (tone + canal), drawer partition by `situation.nature === 'Blocking'`. Application encodes **Gate vs Signal** in `CompetitionFlowAnalyzer` helpers but **does not** expose a uniform semantic field on the wire for structure issues.

4. **Overview situations** are built mainly from **Needs Attention** (Blocking) plus **CompetitionSuspended** (Informational). Structure readiness blockers are **not** merged into that list (VERIFIED comment in `OverviewAssembler.BuildSituations`). Construction blockers are surfaced on Structure / Overview construction dimensions via other paths.

5. **Drawer policy** excludes construction sources via SPA denylist even if they appeared on the wire (`attentionPartition.ts`, `ATTENTION_DRAWER_CONSTRUCTION_DENYLIST`). **VERIFIED:** `GET /attention` does not emit those construction blockers today — denylist is guardrail.

---

## Three dimensions (discussion frame)

**PROPOSED** framing for step 2 — aligns with team discussion, **not** an adopted architecture.

| Dimension | Owner | Question |
| :-------- | :---- | :------- |
| 1. Business semantics | Application | What is the problem? Does it block a **specific** action, require attention, or inform only? |
| 2. Surface policy | SPA per area | Should this appear on Structure, Overview, drawer? Dedup with another surface? |
| 3. Presentation | SPA | Banner, inline diagnostic, tooltip, mutation alert, remediation link. |

**VERIFIED nuance:** One code can block **one action** without being “globally blocking” (e.g. `OpenMatches` on `CompleteStage` only — `StructureStageLifecycleAssembler.ResolveCompleteTransition`, `CompleteStage.ReasonOpenMatches`, POST `Application.StageCompletionBlocked` with `reasons` containing `OpenMatches`).

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

## PROPOSED — canonical registry (not adopted)

**Do not implement from this section without step 2 DECIDED.**

For discussion only: a **single stable code** per organizer problem (e.g. `CycleDetected`), with **documented aliases** on each transport:

| PROPOSED canonical | Example aliases observed (VERIFIED strings only) |
| :----------------- | :----------------------------------------------- |
| `OpenMatches` | Lifecycle `OpenMatches`; POST reasons; not `Application.*` as canonical |
| `CycleDetected` | `structureIssues[]`; lifecycle Prepare disabled; POST `Application.StructureCycleDetected` |
| `OverCapacityEdge` | issue, blocker, lifecycle, POST `Application.OverCapacityEdge` |
| `StructureGraphInvalid` | readiness blocker only; lifecycle `StructureGraphInvalid`; not a raw analyzer code |

**PROPOSED semantic levels (step 2):** `Gate` | `Signal` | `Attention` | `Information` plus **`blocksAction`** (e.g. `{ "CompleteStage" }`) instead of a single global severity.

**PROPOSED minimal Application enrichment (step 3):** extend existing DTOs (issues detail, attention item, lifecycle transition) — **no** universal `OrganizerProblemDto` until justified by pilot.

---

## Contradictions / risks (VERIFIED + GAP)

1. **Gravity duplication:** Application Gate/Signal vs SPA tone vs `Nature`/`severity` enums.  
2. **Alias drift:** POST `Application.*` vs short codes on reads (table above).  
3. **Drawer vs Overview vs Structure** policy encoded in SPA sets, not Application channel flags.  
4. **Matchs pilot (`structure.problems`)** adds another i18n path — risk of parallel catalogue if generalized before step 2.  
5. **`attentionSeverity` in enums** (Warning / Info) — **VERIFIED** in i18n; **GAP** Needs Attention wire uses Blocking only today.

---

## Recommended sequence (discussion — not schedule commitment)

1. **This audit** — persist as non-normative Git draft (current step).  
2. **Arbitrate** semantic contract: levels, canonical codes, alias policy, `blocksAction` shape.  
3. **Minimal Application projection** on 2–3 pilot codes.  
4. **Reconcile Matchs pilot** with adopted semantics + i18n strategy.  
5. **Overview + drawer + global mutations** only after pilot gate.

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
