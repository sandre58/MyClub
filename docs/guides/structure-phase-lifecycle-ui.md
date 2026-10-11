# Structure — phase lifecycle UI (spec appendix)

**Purpose:** Git-side design reference for the Structure phase fiche **status band**, stepper, and match stats. Product intent and durable UX decisions remain in **Notion** (*MyClub*); this document records **agreed direction**, **open product choices**, and **facts verified in code** so mockups and PRs do not drift from the domain.

**Last code audit:** 2026-10-11 (repository `MyClub`, paths below).

---

## How to read this document

| Tag | Meaning |
| :-- | :------ |
| **DECIDED** | Direction validated in conversation; treat as contract unless explicitly re-opened in Notion. |
| **OPEN** | Wording or product choice not fixed; do not ship copy as final. |
| **VERIFIED** | Statement checked against current C# / TypeScript implementation (file references). |
| **GAP** | Observed mismatch between UI projection and domain/read model. |

---

## Three notions (DECIDED)

These must stay **separate** in copy and layout; none replaces another.

1. **Cycle de vie** — domain `StageStatus`: `Draft`, `Ready`, `Running`, `Suspended`, `Completed`.
2. **État de construction sportive** — structure, équipes, places, tirage, sorties, matérialisation (read-model checklist + structure issues).
3. **Actions organisateur** — `PrepareStage`, `StartStage`, `CompleteStage` (Structure band today); suspend/resume/competition-level actions live elsewhere.

**Presentation rule (DECIDED):**

| Surface | Role |
| :------ | :--- |
| Header **badge** | Official lifecycle status only. |
| **Band** stepper (Draft) | Construction criteria toward Prepare — not a fake status enum. |
| **Band** match stats (Running / Suspended) | Match activity (`finished`, `cancelled`, `open`, `total`) — not lifecycle status. |
| **Band** primary button | One projected lifecycle transition from the read model. |

---

## Lifecycle contract (DECIDED narrative, VERIFIED mechanics)

### Status semantics

| Status | Organiser question | VERIFIED domain / application behaviour (summary) |
| :----- | :----------------- | :------------------------------------------------ |
| **Draft** | Still configuring; Prepare not (yet) acquired. | `Stage.Prepare` only from Draft (`Stage.cs`). Many structural edits call `DemoteToDraftIfReady()` when status was Ready. |
| **Ready** | Configuration passed **Prepare** at time T. | `PrepareStage.Execute` then `Stage.Prepare` → Ready. **Not** “matches started” or “exploitation opened”. |
| **Running** | Exploitation **opened** via Start. | `Stage.Start` Ready → Running; `EnsureStructureMutable` blocks structural mutation (`Stage.cs`). |
| **Suspended** | Paused; not closed. | Domain suspend/resume exists; **Structure lifecycle read does not project Suspend/Resume actions** (only Complete while Running/Suspended). |
| **Completed** | Phase closed. | `CompleteStage` requires every match Finished or Cancelled (Application); domain allows Complete with **zero** matches (assembler test). |

### Actions (Structure read model today)

| Action | From → To | VERIFIED projection |
| :----- | :-------- | :------------------ |
| **Prepare** | Draft → Ready | `StructureStageLifecycleAssembler.ResolvePrepareTransition` |
| **Start** | Ready → Running | `ResolveStartTransition`; blocked with `InitialRoundNotPlayable` when first cup round has slotted but non-playable fixtures |
| **Complete** | Running / Suspended → Completed | `ResolveCompleteTransition`; disabled with `OpenMatches` when any match not Finished/Cancelled |

**Competition-level gates (VERIFIED):** If competition is `Completed` / `Archived` → `CompetitionClosed`. If competition is `Running` and phase is Draft or Ready → `ConstructionFrozen` (Prepare or Start projected but disabled).

**Copy intent (DECIDED, not final strings):**

- **Ready:** “Configuration validée pour lancer la phase” — distinct from exploitation opened.
- **Start:** Opens sportive phase and **locks structure** for that phase.

Suspend/Resume for **stages** are out of scope for the current Structure band unless added to the read model later.

---

## Read model sources (VERIFIED)

| Concern | Location |
| :------ | :------- |
| Lifecycle DTO (transition, checklist, match progress) | `StructureStageLifecycleAssembler.cs` |
| Per-hub assembly inputs | `StructureViewAssembler.cs` → `StructureStageLifecycleInput` |
| Prepare execution (gates + slot feeds + domain) | `PrepareStage/PrepareStage.cs` |
| Flow facts / gate vs graph policy | `CompetitionFlowAnalyzer.cs` (`IsStructureGraphGateIssue`, `IsPrepareGateIssue`) |
| SPA band | `StructurePhaseStatusBand.tsx`, `structurePhaseStatusBandModel.ts` |
| Transition copy keys | `structure.json` → `lifecycle.disabled.*`, `lifecycle.draftStep.*` |

HTTP: structure hub includes `lifecycle.transition`, `lifecycle.prepareChecklist` (Draft only), `lifecycle.matchProgress` (Running / Suspended / Completed).

---

## Prepare: what blocks the transition (VERIFIED)

Order in `ResolvePrepareTransition`:

1. **`DomainAllowsPrepare`** → `disabledReasonCode: StageNotReady`  
   - Swiss: `SwissSettings` required.  
   - Groups: ≥1 matchday and ≥1 group with an entry.  
   - Rounds: always allowed (elimination).  
   - Else: ≥1 matchday.

2. **Structure graph gate** → `disabledReasonCode: StructureGraphInvalid` (always this **aggregate** code on the wire, not the underlying issue id).  
   - True when any `structureIssues` entry satisfies `IsStructureGraphGateIssue`.

3. **Prepare gate diagnostics** → `disabledReasonCode` = **first** matching analyzer code on this stage:  
   `SlotFeedInvalid`, `SlotFeedMultiple`, `SlotFeedMissing`, `CycleDetected`, `RoutingNondeterminism`, `OverCapacityEdge`, `OverExtraction`, `ConfrontationUndecidable`.

4. Else → `enabled: true`.

**Checklist (`prepareChecklist`)** is built in parallel for Draft; `allSatisfied` is **not** equivalent to `transition.enabled` (VERIFIED).

### Prepare checklist items (Host)

| Code | Applicability (summary) | Satisfied when |
| :--- | :---------------------- | :------------- |
| `Entries` | Root composition | Active entries ≥ competition minimum |
| `Places` | `compositionCapacity > 0` | Pool filled (Champ/Swiss: max(composition, active entries)) |
| `Draw` | Draw rules engaged | Draw badge `Applied` |
| `Matches` | Materialize command exposed | Label variant `complete` |
| `Exits` | Qual or prog rules | No exit-related structure issues (incl. `ConfrontationUndecidable` in issue list) |
| `Attribution` | Root Groups/Cup | Affectation / composition rules |

**Attribution** is on the wire but **not** shown in the six-step SPA stepper (DECIDED product layout).

### POST `/stages/{id}/prepare` errors (VERIFIED)

Read model may show `enabled: true` only when gates pass; execution can still fail on race or load skew. Application throws include:

| Problem `code` (representative) | When |
| :------------------------------ | :--- |
| `Application.SlotFeedsInvalid` | Non-unique slot feed resolutions (also used as bucket for several gate codes in `EnsurePrepareGateDiagnostics`) |
| `Application.StructureCycleDetected` | Cycle in Qual/Prog graph |
| `Application.RoutingNondeterminism` | Routing ambiguity |
| `Application.OverCapacityEdge` / `Application.OverExtraction` | Capacity gates |
| `Application.ConfrontationUndecidable` | Tie format cannot decide necessary confrontation |
| Domain `StageErrorCodes.NotReady` / `InvalidConfiguration` | Local structure rules inside `Stage.Prepare` after Application gates |

SPA mutation surfaces failures via generic `MutationError` (`useStructureStageLifecycleMutation.ts`) — **no** dedicated Prepare error → i18n mapping in the band yet (GAP for 2b).

---

## What `StructureGraphInvalid` actually covers (VERIFIED)

**Readiness blocker** (`StructureViewAssembler.BuildReadiness`): competition-level flag when **any** hub has a structure issue with `IsStructureGraphGateIssue(code) == true`.

**Prepare transition** uses the **same** predicate on **this stage’s** `structureIssues` list before prepare-gate codes.

`IsStructureGraphGateIssue` (`CompetitionFlowAnalyzer.cs`):

- **Excluded** from graph gate (treated as Signal or Prepare-only elsewhere):  
  `SlotFeedMissing`, `NonRootUnfed`, `OverCapacityEdge`, `OverExtraction`, `SourcePopulationShortfall`, `PopulationShortfall`, `AuthoredAwardUnreachable`, `ExpectedRankUnreachable`, `ConfrontationUndecidable`.
- **Included** (default `_ => true`): among others, dangling/missing Qual/Prog destination issues from `BuildStructureIssues`, `SlotFeedInvalid`, `SlotFeedMultiple`, `CycleDetected`, `RoutingNondeterminism`, and other analyzer codes not in the exclusion list.

**Important (GAP):** Several codes that **block Prepare** via step 3 (`OverCapacityEdge`, `OverExtraction`, `ConfrontationUndecidable`, `SlotFeedMissing`) are **not** counted as `StructureGraphInvalid`. The Draft stepper step `Regulation` only marks KO when `disabledReasonCode === 'StructureGraphInvalid'`, so Prepare can be disabled while that step stays **green**.

**Not covered by `StructureGraphInvalid` or the six checklist rows (VERIFIED):**

- `StageNotReady` (`DomainAllowsPrepare`) — e.g. Swiss without settings, poule without matchday.
- Prepare gate codes listed above when they are the **first** disabled reason.
- `Attribution` checklist failures.
- Outbound path destination failures detected only at **execute** in `PrepareStage.EnsureOutboundPathDestinations` (may not appear in lifecycle transition).

---

## SPA stepper mapping (VERIFIED)

| Step id (`draftStep`) | Checklist / logic | i18n key (fr today) |
| :-------------------- | :---------------- | :------------------ |
| `Regulation` | KO iff `disabledReasonCode === 'StructureGraphInvalid'` | « Réglement » (**OPEN** label — misaligned with measured signal) |
| `Teams` | `Entries` | « Équipes » |
| `Places` | `Places` | « Places » |
| `Draw` | `Draw` | « Tirage » |
| `Exits` | `Exits` | « Sorties » |
| `Matches` | `Matches` | « Matchs » |

---

## Band messaging rules (VERIFIED + DECIDED 2b)

`shouldShowPrimaryBlockMessage` suppresses the band paragraph when:

- Draft + `StageNotReady` or `StructureGraphInvalid` (rely on stepper / remediation instead).
- Running or Suspended + `OpenMatches` (Complete disabled; no duplicate prose).

**All other** disabled codes show `lifecycle.disabled.{code}` if present, else the **raw code** string.

**Current i18n keys** under `lifecycle.disabled`:  
`CompetitionClosed`, `ConstructionFrozen`, `InitialRoundNotPlayable`, `StageNotReady`, `StructureGraphInvalid`, `OpenMatches`.

**GAP:** Prepare gate codes (`OverCapacityEdge`, `SlotFeedMissing`, …) have **no** `lifecycle.disabled.*` entries → poor fallback today.

### Short-term UX policy (DECIDED 2c + 2b)

1. **2c — Relabel** the first step from « Réglement » to text that matches what is **actually** measured (`StructureGraphInvalid` only). **OPEN:** final FR/EN label after product review (working hypothesis « Graphe & flux » is **not** decided).
2. **2b — Stepper limits:** Document in Notion that six steps = checklist **A**, not full Prepare enforcement; when Prepare is disabled and the cause is **not** reflected in the stepper, show **one** band message (or mutation error) mapped from `disabledReasonCode` / ProblemDetails `code`, without duplicating the same cause in two places.
3. Extend suppression rules when a step already shows KO for the **same** remediation code (e.g. `ConfrontationUndecidable` vs Exits step — **OPEN** exact dedup matrix in implementation PR).

### Conditional evolution (DECIDED 2a)

Enrich `prepareChecklist` or stepper inputs on the Host so each Prepare gate has a visible construction row **if** field feedback shows persistent “false green” or opaque blocks.

---

## Edge cases (validation checklist)

| Case | Expected status | UI expectation |
| :--- | :-------------- | :------------- |
| Ready, 0 matches | Ready | Start allowed (if round playable); stats N/A |
| Running, 0 matches played | Running | Complete allowed; “no matches” meta if total 0 |
| Draft, matches materialized | Draft | Matches step may be OK; Prepare still required |
| Dematerialize after materialize | Draft/Ready unchanged | Matches step may flip to KO |
| Competition Running, phase Draft/Ready | Draft/Ready | `ConstructionFrozen` message + overview link |
| Checklist all satisfied, Prepare disabled | Draft | Must explain gate C / `StageNotReady` (GAP today) |
| Completed | Completed | **No band** (VERIFIED) |

---

## Mockups and PR gate (DECIDED)

Do **not** treat Structure lifecycle mockups as final until:

1. This appendix and Notion lifecycle copy are aligned on § lifecycle contract.  
2. Step 2b fallback matrix is specified against **actual** `disabledReasonCode` and POST prepare ProblemDetails codes.  
3. First-step label decision (2c) is recorded in Notion.  

Then implement copy/stepper/fallback in a focused PR (no unrelated band polish).

---

## Notion sync

When the decision maker validates the lifecycle contract and stepper labelling, mirror **product-facing** prose in Notion (Phase Contract / Structure UX). Keep this file as the **code-verified** appendix; link Notion ↔ this path from the Structure UX page.
