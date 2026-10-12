# Structure — phase lifecycle UI (spec appendix)

**Purpose:** Git-side design reference for Structure phase **lifecycle UI in the Matchs operational column** and match stats during exploitation. Product intent and durable UX decisions remain in **Notion** (*MyClub*); this document records **agreed direction**, **open product choices**, and **facts verified in code** so mockups and PRs do not drift from the domain.

**Last code audit:** 2026-10-12 (V2 gap audit + verification matrix, § below). Prior audit: 2026-10-11.

**Notion validation:** 2026-10-12 — [Arbitrage Structure — cycle de vie et gel construction](https://app.notion.com/p/3f775fb92d10817eb795ddb5fc52dd9c).

**Product SoT (V2 lifecycle contract):** [Lifecycle phase — UI Structure](https://app.notion.com/p/3f575fb92d10816a96a9cf3c70de7bca) — **Contrat cycle de phase V2** (2026-10-12, DECIDED). This appendix records **code facts** and **gaps vs that contract**; narrative contract lives in Notion only.

**PR gate (DECIDED):** No SPA PR for 2b/2c until the **2b matrix** is arbitrated in Notion and **2c** is decided.

**Notion arbitration (product):** [Arbitrage matrice 2b — bandeau cycle Structure (2026-10-11)](https://app.notion.com/p/3f675fb92d1081dba56afcbfacbd32e7) — **superseded** by Matchs-column lifecycle UI (2026-10-12). Update [Lifecycle phase — UI Structure](https://app.notion.com/p/3f575fb92d10816a96a9cf3c70de7bca) after merge.

---

## Product arbitrations (2026-10-11)

| Topic | Status |
| :---- | :----- |
| Lifecycle — five domain statuses | **DECIDED** |
| UX separation — badge / stepper / match stats | **DECIDED** |
| Rename « Réglement » (step `Regulation`) | **OPEN** — confirm exact scope of `StructureGraphInvalid` first (§ below) |
| Prepare gates absent from stepper (false green) | **GAP** — must not imply Prepare is available |
| Error fallback (2b) | **OPEN** — matrix below; implement only after review |
| Read-model extension (2a) | **CONDITIONAL** — after 2b limits evaluated in use |

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
3. **Actions organisateur** — `PrepareStage`, `StartStage`, `CompleteStage`, `SuspendStage` / `ResumeStage`, reverts (Matchs operational column); competition-level actions may also appear on Overview.

**Presentation rule (DECIDED, shipped 2026-10-12):**

| Surface | Role |
| :------ | :--- |
| Header **badge** | Official lifecycle status only. |
| **Matchs** operational column | Lifecycle transition + revert buttons, disabled-reason copy, remediation links; match stats in Running/Suspended. |
| **Matchs** operational column (same) | Match **production** (materialize/dematerialize gauge) — construction sportive, not lifecycle status. |

**Removed:** header status band and Prepare stepper (false-green checklist).

**Matchs column by status (2026-10-12):** Draft — Prepare-only empty state, no production. Ready — production gauge then lifecycle (Start) then materialize (secondary when Start shown). Running/Suspended — exploitation stats + Complete/pause; production gauge only when read `presentation === 'actionable'` or dematerialize enabled. Completed — match progress + view link when matches exist. Visibility helpers: `structureMatchsSectionUi.ts`.

---

## Lifecycle contract (DECIDED narrative, VERIFIED mechanics)

### Status semantics

| Status | Organiser question | VERIFIED domain / application behaviour (summary) |
| :----- | :----------------- | :------------------------------------------------ |
| **Draft** | Still configuring; Prepare not (yet) acquired. | `Stage.Prepare` only from Draft (`Stage.cs`). Many structural edits call `DemoteToDraftIfReady()` when status was Ready. |
| **Ready** | Configuration passed **Prepare** at time T. | `PrepareStage.Execute` then `Stage.Prepare` → Ready. **Not** “matches started” or “exploitation opened”. |
| **Running** | Exploitation **opened** via Start. | `Stage.Start` Ready → Running; `EnsureStructureMutable` blocks structural mutation (`Stage.cs`). |
| **Suspended** | Paused; not closed. | Domain suspend/resume exists; Structure read projects `SuspendStage` / `ResumeStage` on `lifecycle.revertTransition` (icon actions in Matchs column). |
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

**Secondary transitions (VERIFIED):** `RevertToDraftStage`, `SuspendStage`, `ResumeStage` on `lifecycle.revertTransition`; `RevertToReadyStage` on `lifecycle.cancelStartTransition` (Running / Suspended, engagement gate = dematerialize predicate).

**Action order in Matchs column (VERIFIED DOM):** primary transition (Prepare / Start / Complete) → Suspend / Resume icons → cancel start → Revert to draft. Manual browser pass on desktop and narrow viewport recommended before release.

---

## Read model sources (VERIFIED)

| Concern | Location |
| :------ | :------- |
| Lifecycle DTO (transition, checklist, match progress) | `StructureStageLifecycleAssembler.cs` |
| Per-hub assembly inputs | `StructureViewAssembler.cs` → `StructureStageLifecycleInput` |
| Prepare execution (gates + slot feeds + domain) | `PrepareStage/PrepareStage.cs` |
| Flow facts / gate vs graph policy | `CompetitionFlowAnalyzer.cs` (`IsStructureGraphGateIssue`, `IsPrepareGateIssue`) |
| SPA Matchs lifecycle UI | `StructurePhaseMatchsLifecycleBlock.tsx`, `StructurePhaseOperationalStrip.tsx` (`MatchesColumn`), `structurePhaseStatusBandModel.ts` |
| Transition copy keys | `structure.json` → `lifecycle.disabled.*`, `lifecycle.statusBand.*` (legacy key prefix) |

HTTP: structure hub includes `lifecycle.transition`, optional `lifecycle.revertTransition`, and `lifecycle.matchProgress` (Running / Suspended / Completed). **Prepare stepper / `prepareChecklist` removed** (transition + disabled reasons only).

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

## 2b — Fallback matrix (OPEN — spec for arbitration, VERIFIED codes)

Two channels must stay distinct:

| Channel | When | Source of truth |
| :------ | :--- | :-------------- |
| **Anticipated block** | Primary action projected, `enabled: false` | Structure read `lifecycle.transition.disabledReasonCode` (+ optional `remediation`) |
| **Mutation refusal** | User clicked while `enabled: true`, or state changed between read and POST | HTTP ProblemDetails `code` (+ optional `reasons`) |

**Global rules (DECIDED — implement only after Notion signs off the matrix):**

1. **Dedup = same cause, not semantic proximity** — suppress the band paragraph only when the **same gate / same wire code** is already explained elsewhere (e.g. `StructureGraphInvalid` ↔ step `Regulation` KO; `ConfrontationUndecidable` ↔ Exits KO). Do **not** dedup because topics “sound related” (e.g. open-match **stats** do **not** cover every POST completion refusal).
2. **No raw codes in product UI** — every **known** code maps to localized copy; **unknown** codes use a generic localized fallback (not the wire string). Keys `_unknown` remain **OPEN** until the translation contract is validated.
3. **Do not mask gaps** — if `enabled: false` and no step explains it, the UI must still signal blockage. **`StageNotReady`** (VERIFIED): aggregate code only — no sub-cause on `StructureStageLifecycleTransitionDto`; copy must be **honest** (blocked without naming a false precise rule) or the read model must be extended (2a).
4. **Checklist ≠ transition** (VERIFIED) — `prepareChecklist.allSatisfied` does not drive `transition.enabled`.
5. **Anticipated vs POST** — separate rules; a POST refusal must stay understandable when the read had `enabled: true` (stale state, execute-only gates, checklist KO with button enabled). Use **MutationError** + refresh; map `errors:{code}` when known.

**Copy namespaces (today):**

| Surface | Namespace | Notes |
| :------ | :-------- | :---- |
| Band anticipated block | `structure:lifecycle.disabled.{disabledReasonCode}` | Six keys exist today; prepare-gate codes missing |
| Band mutation error | `errors:{ProblemDetails.code}` via `apiErrorLabel` | Full wire code, e.g. `Application.SlotFeedsInvalid` → `errors:Application.SlotFeedsInvalid` |
| Unknown mutation | Falls back to English `detail` inside `common:errorWithStatus` | **GAP** — no dedicated generic lifecycle mutation fallback yet |

**Proposed generic fallbacks (OPEN — keys not implemented):**

| Situation | Proposed key |
| :-------- | :----------- |
| Unknown `disabledReasonCode` on band | `structure:lifecycle.disabled._unknown` |
| Unknown ProblemDetails `code` on lifecycle mutation | `structure:lifecycle.mutation._unknown` |

---

### Prepare — anticipated block (`disabledReasonCode`, Draft)

Projection order: `StructureStageLifecycleAssembler.ResolvePrepareTransition`.  
Action code on wire: `PrepareStage`.

| `disabledReasonCode` | Stepper already explains? | Proposed band message (OPEN copy) | i18n key (target) | If already explained elsewhere | Unknown / today |
| :------------------- | :-------------------------- | :-------------------------------- | :---------------- | :----------------------------- | :-------------- |
| `CompetitionClosed` | No | Competition closed — lifecycle actions unavailable | `lifecycle.disabled.CompetitionClosed` | Show band only | **VERIFIED** today |
| `ConstructionFrozen` | No | Competition live — phase prep frozen | `lifecycle.disabled.ConstructionFrozen` | Show band + overview link | **VERIFIED** today |
| `StageNotReady` | **No** — aggregate `DomainAllowsPrepare` only; no sub-facts on wire | **Honest** band: preparation blocked; optional link to construction remediation — **must not** pretend to name the exact rule | `lifecycle.disabled.StageNotReady` and/or OPEN `lifecycle.disabled.PrepareBlocked` (generic) | **Reject** “healthy UI” (suppress band + green stepper). **Target:** show band or 2a | Suppressed today (**GAP**) |
| `StructureGraphInvalid` | Yes — step `Regulation` KO | (use stepper + remediation link) | `lifecycle.disabled.StructureGraphInvalid` | **Suppress band** | **VERIFIED** today |
| `ConfrontationUndecidable` | Yes — step `Exits` KO (issue in exit set) | Tie format / exits | `lifecycle.disabled.ConfrontationUndecidable` or reuse `errors:Application.ConfrontationUndecidable` | **Suppress band** (to add) | Today: band may show **raw code** |
| `OverCapacityEdge` | No | Capacity / places | `lifecycle.disabled.OverCapacityEdge` or `errors:Application.OverCapacityEdge` | Show band | Today: **raw code** |
| `OverExtraction` | No | Outbound extraction vs capacity | `lifecycle.disabled.OverExtraction` or `errors:Application.OverExtraction` | Show band | Today: **raw code** |
| `SlotFeedMissing` | No | Missing slot feed | `lifecycle.disabled.SlotFeedMissing` | Show band | Today: **raw code**; not `StructureGraphInvalid` |
| `SlotFeedInvalid` | Partial — may appear in `structureIssues` but step `Regulation` only if aggregate `StructureGraphInvalid` | Invalid slot feed | `lifecycle.disabled.SlotFeedInvalid` | If `StructureGraphInvalid`: suppress; else show band | Often **`StructureGraphInvalid`** instead of this code |
| `SlotFeedMultiple` | Same as above | Conflicting slot feeds | `lifecycle.disabled.SlotFeedMultiple` | Dedup with `StructureGraphInvalid` when aggregate used | Same |
| `CycleDetected` | Partial — included in graph gate → often `StructureGraphInvalid` on wire | Cycle in phase graph | `lifecycle.disabled.CycleDetected` or `errors:Application.StructureCycleDetected` | Suppress if `StructureGraphInvalid` + Regulation KO | Wire may be aggregate |
| `RoutingNondeterminism` | Partial — graph gate | Ambiguous routing | `lifecycle.disabled.RoutingNondeterminism` | Suppress if aggregate graph block | Same |

**Checklist-only gaps (transition may still be `enabled: true`) (VERIFIED + GAP):**

| Checklist code | Stepper step | If unsatisfied but Prepare enabled |
| :------------- | :----------- | :-------------------------------- |
| `Entries` | Teams | Step KO; button may still enable (non-root: N/A) |
| `Places` | Places | Step KO; button may still enable |
| `Draw` | Draw | Step KO; button may still enable |
| `Matches` | Matches | Step KO; button may still enable (championship test) |
| `Exits` | Exits | Step KO; button may still enable if issues empty |
| `Attribution` | *(not in stepper)* | **No step**; button may enable — **GAP** |

---

### Prepare — mutation refusal (POST `/stages/{id}/prepare`)

When `transition.enabled` was true (or stale client). ProblemDetails `code` (VERIFIED in `PrepareStage.cs`, `Stage.Prepare`).

| ProblemDetails `code` | Also on read model? | Stepper | Target copy | Dedup vs band/stepper |
| :-------------------- | :------------------ | :------ | :---------- | :-------------------- |
| `Application.SlotFeedsInvalid` | Sometimes (feeds) | Usually no | `errors:Application.SlotFeedsInvalid` | Show **MutationError** only; optional Structure SoT link (existing `persistentStructureMutationSotHref`) |
| `Application.StructureCycleDetected` | Often as graph / gate | Regulation if `StructureGraphInvalid` | `errors:Application.StructureCycleDetected` | Prefer single surface after refresh |
| `Application.RoutingNondeterminism` | Same | Regulation if aggregate | `errors:Application.RoutingNondeterminism` | Same |
| `Application.OverCapacityEdge` | In `structureIssues`, not stepper | No | `errors:Application.OverCapacityEdge` | MutationError; refresh should set `disabledReasonCode` |
| `Application.OverExtraction` | Same | No | `errors:Application.OverExtraction` | Same |
| `Application.ConfrontationUndecidable` | Exits step | Exits KO | `errors:Application.ConfrontationUndecidable` | Do not duplicate if Exits already KO |
| `Application.DanglingFeedTarget` | May be missing on read (authoring vs execute) | Exits partial | `errors:Application.DanglingFeedTarget` | MutationError + Structure link |
| `Application.StageNotInCompetition` | Rare | No | `errors:Application.StageNotInCompetition` | MutationError |
| `Stage.NotReady` | Partial (`StageNotReady`) | No | `errors:Stage.NotReady` | MutationError |
| `Stage.InvalidConfiguration` | Partial | Places / Teams partial | `errors:Stage.InvalidConfiguration` | MutationError |
| `Stage.InvalidTransition` | N/A (already Ready) | No | `errors:Stage.InvalidTransition` | MutationError |
| `Application.StageNotFound` | N/A | No | `errors:Application.StageNotFound` | MutationError |
| Unknown / unmapped | — | — | `structure:lifecycle.mutation._unknown` (proposed) | Never show raw code |

---

### Start — anticipated block (`disabledReasonCode`, Ready)

| `disabledReasonCode` | Stepper (Draft only) | Target i18n | Dedup | Today |
| :------------------- | :------------------- | :------------ | :---- | :---- |
| `CompetitionClosed` | N/A | `lifecycle.disabled.CompetitionClosed` | Band | OK |
| `ConstructionFrozen` | N/A | `lifecycle.disabled.ConstructionFrozen` | Band + overview | OK |
| `InitialRoundNotPlayable` | No stepper in Ready | `lifecycle.disabled.InitialRoundNotPlayable` | Band | OK |

---

### Start — mutation refusal (POST `/stages/{id}/start`)

| ProblemDetails `code` | Read model analogue | Target i18n | Notes |
| :-------------------- | :------------------ | :---------- | :---- |
| `Stage.NotReady` | `InitialRoundNotPlayable` (similar intent) | `errors:Stage.NotReady` | Positional KO — slots not playable (`Stage.Start`) |
| `Stage.InvalidTransition` | — | `errors:Stage.InvalidTransition` | Wrong status |
| `Application.CompetitionClosed` | `CompetitionClosed` | `errors:Application.CompetitionClosed` | If host enforces |

---

### Complete — anticipated block (`disabledReasonCode`, Running / Suspended)

| `disabledReasonCode` | Match stats | Target i18n | Dedup | Today |
| :------------------- | :---------- | :------------ | :---- | :---- |
| `OpenMatches` | Yes — `open` > 0 | `lifecycle.disabled.OpenMatches` | **Suppress band** (stats + disabled button) | **VERIFIED** |
| `CompetitionClosed` | N/A | `lifecycle.disabled.CompetitionClosed` | Band | OK |

---

### Complete — mutation refusal (POST `/stages/{id}/complete`)

| ProblemDetails `code` | `reasons` | Target i18n | Dedup |
| :-------------------- | :-------- | :---------- | :---- |
| `Application.StageCompletionBlocked` | `OpenMatches` (when present) | If `reasons` contains `OpenMatches` → same copy as anticipated `OpenMatches`; else dedicated message — **do not** dedup from stats on proximity alone | Dedup with stats **only** for open-match gate |
| `Stage.InvalidTransition` | — | `errors:Stage.InvalidTransition` | MutationError — not dedup with stats |

**GAP (VERIFIED):** `Application.StageCompletionBlocked` has **no** entry in `errors.json` today → English detail in mutation alert.

---

## Coherence addendum (arbitration — 2026-10-11)

### `StageNotReady`

The Host exposes a **single** `disabledReasonCode` (`StageNotReady`) when `DomainAllowsPrepare` fails (Swiss without settings, groups without matchday/entry, championship without matchday, etc.). The lifecycle DTO does **not** include structured sub-reasons.

| Tier | UX | When |
| :--- | :--- | :--- |
| **A — Honest generic band** | “La préparation est bloquée — complétez la structure de la phase” (no false precision) + remediation `href` when present | Default until 2a |
| **B — Existing `StageNotReady` copy** | Slightly more specific but still aggregate | Only if product accepts it as tier A |
| **C — 2a read model** | New checklist row or facts | If tier A is insufficient |

**Rejected:** suppress band while stepper stays all-green.

### Dedup reference

| Pair | Dedup justified? |
| :--- | :--------------- |
| `StructureGraphInvalid` ↔ `Regulation` step KO | **Yes** — same aggregate gate |
| `ConfrontationUndecidable` ↔ Exits KO | **Yes** |
| `OpenMatches` ↔ match stats + disabled Complete (anticipated) | **Yes** |
| Stats ↔ all POST complete errors | **No** |
| `StageCompletionBlocked` + `reasons: ["OpenMatches"]` | **Yes** with open-match copy |
| OverCapacity / SlotFeed* ↔ Exits or Regulation | **No** |

### 2c — First step label (OPEN)

Step id remains `Regulation` in code until renamed. The label must describe what the step **measures today**: KO when `disabledReasonCode === 'StructureGraphInvalid'`, i.e. at least one `structureIssues` entry with `IsStructureGraphGateIssue` (see § What `StructureGraphInvalid` covers). **Do not** label it « Réglement » in final copy. The candidate « Graphe & flux » is **unapproved**.

### 2a — Conditional read-model extension

If, after 2b, organizers still hit **false green** steppers or unexplained `enabled: false`, add Host rows (checklist or step inputs) for: prepare gates not mapped to steps, `Attribution`, and execute-only outbound path failures — without recomputing lifecycle status from match facts.

---

## Band messaging — current implementation (VERIFIED)

`shouldShowPrimaryBlockMessage` suppresses the band paragraph when:

- Draft + `StageNotReady` or `StructureGraphInvalid`.
- Running or Suspended + `OpenMatches`.

All other disabled codes call `lifecycle.disabled.{code}` or emit the **raw code**. Mutation failures use `MutationError` → `apiErrorLabel` / English detail.

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

1. Lifecycle contract (§ above) is validated in Notion.  
2. This **2b matrix** is reviewed (dedup rows + generic fallbacks + checklist-only gaps).  
3. **2c** first-step label is recorded in Notion.  

Then implement copy / stepper / fallback in one focused PR (no unrelated band polish). **Order:** arbitrate 2c + 2b in Notion → PR → mockups.

---

## V2 contract — code gap audit (2026-10-12)

**Scope:** Compare Notion **Contrat cycle de phase V2** (2026-10-12) to current repository behaviour. **No implementation plan** — input for test matrix and incremental work.

**Legend:** **ALIGNED** = matches V2 today · **GAP** = change needed · **PARTIAL** = subset aligned · **N/A** = not applicable yet.

### Cross-cutting

| V2 rule | Current behaviour | Status | Primary locations |
| :------ | :---------------- | :----- | :---------------- |
| Structure mutable **Draft only** | **Incrément 1 (2026-10-12):** `EnsureStructureMutable` allows **Draft only**; `DemoteToDraftIfReady` removed. Application `StageStructureEditGuard` on structure use cases. Match **production** spine (materialize/dematerialize) uses `*ForMatchProduction` APIs in **Draft/Ready**. | **ALIGNED** | `Stage.cs`, `StageStructureEditGuard.cs` |
| Materialize **Ready only** (not Draft) | **Incrément 2 (2026-10-12):** `StageMaterializeGuard` + read assembler mirror **Ready** only. | **ALIGNED** | `StageMaterializeGuard.cs`, `MaterializeMatches.cs`, `StructureMaterializeAssembler.cs` |
| Faits sportifs **Running/Suspended only** | `StagePhaseExploitationGuard` via `RequireMatchForOperationAsync` (all match commands incl. Start/Finish). Overview match actions still match-centric (UI follow-up). | **ALIGNED** (executor) | `StagePhaseExploitationGuard.cs`, `UseCaseExecutor.Helpers.cs`, `UseCaseExecutor.Matches.cs` |
| Planification in **Ready** | **Incrément 2:** `EnsureScheduleExploitation` on `GenerateScheduleAsync` / `ApplyScheduleAsync`. | **ALIGNED** | `UseCaseExecutor.Stages.cs` |
| **Completed** strict (no planif/sportif) | **Incrément 2:** `StageExploitationClosed` on sportive + schedule guards when stage Completed. | **PARTIAL** (executor; Overview unchanged) | `StagePhaseExploitationGuard.cs` |
| `RevertToDraft` / `RevertToReady` | **Incrément 2:** domain transitions, use cases, HTTP `revert-to-draft` / `revert-to-ready`, lifecycle `RevertTransition` projection. | **ALIGNED** | `RevertToDraft.cs`, `RevertToReady.cs`, `StructureStageLifecycleAssembler.cs` |
| `RevertToReady` ≠ dematerialize | Revert preflight uses `TryMatchLifecycleReason` (same engagement as dematerialize); does not remove planned matches. | **ALIGNED** (2026-10-12 remediation) | `DematerializationCapability.cs`, `RevertToReady.cs`, `StructureStageLifecycleAssembler.ResolveCancelStartTransition` |

### Transitions

| Transition | V2 | Current | Status | Locations |
| :--------- | :--- | :------ | :----- | :-------- |
| Prepare | Draft → Ready, existing gates | Same | **ALIGNED** | `PrepareStage.cs`, `Stage.Prepare` |
| Start readiness | Per-format initial scope (Ch/G: no eligible unmaterialized; Cup: first playable round materialized; Swiss: progressive, 0 match OK) | `StageStartReadinessGuard` + `StageConfrontationMaterializationState` before `Stage.Start`; read via `TryGetDisabledReasonCode`. Cup positional still enforced in Domain. | **ALIGNED** | `StageStartReadinessGuard.cs`, `UseCaseExecutor.Stages.cs`, `StructureStageLifecycleAssembler.ResolveStartTransition` |
| Complete | All matches Finished/Cancelled | Same (`CompleteStage.Execute`) | **ALIGNED** | `CompleteStage.cs` |
| RevertToDraft | Explicit; no attached matches | **Incrément 2** — `RevertToDraft` + `Application.RevertToDraftBlocked` | **ALIGNED** | `RevertToDraft.cs`, `Stage.RevertToDraft` |
| RevertToReady | Pause; block sportive engagement (Live, Finished, Cancelled+observed Live) | `TryMatchLifecycleReason`; cancel-start projected disabled when blocked; **Suspended** included | **ALIGNED** | `RevertToReady.cs`, `StructureStageLifecycleAssembler` |

### Format exceptions (Swiss — option A)

| V2 | Current | Status | Locations |
| :--- | :------ | :----- | :-------- |
| Materialize progressive in **Running** | `GenerateNextRound` requires `StageStatus.Running` | **ALIGNED** | `GenerateNextRound.cs` |
| Start without full calendar / 0 matches | Start allowed without materialize gate for Swiss | **ALIGNED** (by absence of gate) | `Stage.Start`, `StageLifecycleTests` |
| Materialize in Draft | Allowed today for Ch/G/Cup | **GAP** vs V2 Draft ban (non-Swiss path) | Materialize use cases |

### Read model & SPA

| Surface | V2 direction | Shipped | Status |
| :------ | :----------- | :------ | :----- |
| Lifecycle transitions | Prepare / Start / Complete + Revert* | Prepare, Start, Complete + `revertTransition` (`StructureStageLifecycleAssembler`) | **ALIGNED** |
| UI placement | Matchs section / mise en service; **no** stepper band | `StructurePhaseMatchsLifecycleBlock` in operational Matchs column | **ALIGNED** (2026-10-12) |
| Match progress gauge | Clos / total (finished + cancelled) | `finished`, `cancelled`, `open`, `total` — usable for clos/total copy | **PARTIAL** |
| Competition Running + phase Draft/Ready (orphan) | ConstructionFrozen on **Prepare** and **Start** | Orphan = no peer Running/Suspended/Completed | **ALIGNED** | `StructureStageLifecycleAssembler.ResolvePrepareTransition`, `IsConstructionFrozenWhileCompetitionRunning` |

### Domain vs Application boundaries (2026-10-12, DECIDED)

| Category | Role | Examples |
| :------- | :--- | :------- |
| **A — Domain invariants** | Must hold for any entry point | Invalid status transitions; structural integrity of aggregates |
| **B — Application use-case policy** | Orchestration, loaded context, cross-phase gates | `RevertToReady` engagement preflight; `CompleteStage` open matches; materialize Ready-only |
| **C — Read / HTTP** | Project actions + disabled reasons; not source of truth | `StructureStageLifecycleAssembler`, ProblemDetails codes |

**Remediation default:** keep **RevertToReady** and **Complete** engagement checks in Application (category B) with integration tests; extend Domain (category A) only when a seed or tool bypass is demonstrated.

### Gardes réutilisables (audit — no new policy type yet)

| Predicate | Already exists? | Reuse candidate for V2 |
| :-------- | :-------------- | :--------------------- |
| Match « engagé sportivement » (blocks dematerialize / RevertToReady) | Yes — `DematerializationCapability.BlocksDematerialization` / `TryMatchLifecycleReason` | **Yes** — shared preflight for `RevertToReady` |
| Stage structure locked | Domain `EnsureStructureMutable` (Running+) vs app `StructureNotMutable` (Running+) | Extend to **Ready** in both layers |
| Materialize allowed | Duplicated in materialize use cases + `StructureMaterializeAssembler` | Tighten to **Ready only** in same places |
| Start initial scope Ch/G | Eligible/determined counts in `StructureMaterializeAssembler` | **Yes** — compose Start gate from existing materialize read inputs |

### Gap prioritization (implementation order)

| Tier | What | Examples from audit | Typical test home |
| :--- | :--- | :------------------ | :---------------- |
| **P0 — Domain invariants** | Aggregate rules; status transitions | Structure locked in **Ready**; remove `DemoteToDraftIfReady`; `RevertToDraft` / `RevertToReady` | `MyClub.PlayUp.Domain.Tests` (`StageLifecycleTests`, format fixtures) |
| **P1 — Application guards** | Use-case preflight before domain | Materialize Ready-only; sportive ops require Running/Suspended; Completed blocks match mutations; Start readiness Ch/G/Cup | `MyClub.PlayUp.Application.Tests` (vertical slices per use case) |
| **P2 — Read model** | Projected transitions and disabled reasons | `RevertToDraft` / `RevertToReady` on Structure lifecycle DTO; new Start disabled codes | `StructureStageLifecycleAssemblerTests` |
| **P3 — UI** | Visibility must match API | Matchs-section CTAs; no stepper reliance; do not hide blocked ops without disabled reason | Vitest band / operational strip (later PR) |

**Incrément 1 scope (P0 + part of P1):** freeze structure in Ready, remove implicit demotion, regression tests for Swiss / Cup / `ConstructionFrozen` **unchanged** unless contract explicitly changes them.

---

## V2 verification matrix (2026-10-12)

**Purpose:** Turn Notion **Contrat cycle de phase V2** into **checkable expected behaviour** for automated tests. **Target = V2 contract**, not current code († = row known to **fail today** per § gap audit).

**Axes:** `StageStatus` × operation × `StructureFormatKind` (Championship, Groups, Cup, Swiss) where relevant.

**Code columns:**

| Kind | Wire shape | Examples |
| :--- | :--------- | :--------- |
| Domain | `Stage.*` / `Match.*` | `Stage.StructureLocked`, `Stage.InvalidTransition`, `Stage.NotReady` |
| Application | `Application.*` | `Application.StructureNotMutable`, `Application.StageCompletionBlocked` |
| Read (Structure lifecycle) | short reason on DTO | `ConstructionFrozen`, `InitialRoundNotPlayable`, `OpenMatches` |
| **PROPOSED** | not in repo yet — register before implement | See § Proposed error codes |

**Competition context:** unless noted, competition is **Draft** or **Ready** (not Completed/Archived). Rows with competition **Running** and phase Draft/Ready use **`ConstructionFrozen`** on Prepare/Start (read), not a domain throw on arbitrary ops.

---

### A — Lifecycle transitions

| From | Command | To (success) | Preconditions (V2) | Blocked — domain / app code | Read `disabledReasonCode` (anticipate) |
| :--- | :------ | :------------- | :------------------- | :-------------------------- | :------------------------------------- |
| Draft | `PrepareStage` | Ready | Existing Prepare gates pass | `Stage.NotReady`, `Stage.InvalidConfiguration`, Prepare gate `Application.*` | `StageNotReady`, `StructureGraphInvalid`, prepare-gate codes, `CompetitionClosed`, `ConstructionFrozen`† |
| Ready | `RevertToDraft` | Draft | **Zero** stage-attached matches (same attachment notion as dematerialize scope) | **PROPOSED** `Stage.RevertBlockedProductionPresent` or `Application.RevertToDraftBlocked` | **PROPOSED** `ProductionPresent` |
| Ready | `StartStage` | Running | Format Start readiness (§ H) | `Stage.InvalidTransition`, `Stage.NotReady` (cup positional), **PROPOSED** `Application.StartBlockedUnmaterialized` (Ch/G) | `InitialRoundNotPlayable`, **PROPOSED** `UnmaterializedConfrontations`, `ConstructionFrozen`, `CompetitionClosed` |
| Running | `RevertToReady` | Ready | No match **sportively engaged** (Live, Finished, Cancelled+observed Live); **Scheduled/Postponed kept** | **PROPOSED** `Application.RevertToReadyBlocked` + reason `MatchLifecycleStarted` (reuse dematerialize predicate) | **PROPOSED** `MatchLifecycleStarted` |
| Suspended | `RevertToReady` | Ready | Same as Running → Ready | Same | Same |
| Running | `CompleteStage` | Completed | All matches Finished or Cancelled | `Application.StageCompletionBlocked` (`OpenMatches`) | `OpenMatches` |
| Suspended | `CompleteStage` | Completed | Same | Same | Same |
| Running | `Suspend` | Suspended | — | `Stage.InvalidTransition` | — |
| Suspended | `Resume` | Running | — | `Stage.InvalidTransition` | — |
| Completed | any lifecycle | — | — | `Stage.InvalidTransition` | `transition.code` null |

---

### B — Structure mutations (representative ops)

Applies to: add matchday/round/group, slot assign, replace Qual/Prog/Draw rules, rebuild structure, etc.

| Stage status | Expected V2 | Domain code | Application code (typical) | Notes |
| :----------- | :---------- | :---------- | :------------------------- | :---- |
| Draft | **Allow** (subject to format rules) | — | — | Status stays Draft |
| Ready | **Deny** — no silent demotion | `Stage.StructureLocked` | `Application.StructureNotMutable` | † today: often **allows** + `DemoteToDraftIfReady` |
| Running / Suspended | Deny | `Stage.StructureLocked` | `Application.StructureNotMutable` | ALIGNED |
| Completed | Deny | `Stage.StructureLocked` | `Application.StructureNotMutable` | ALIGNED (app) |

**Regression (incrément 1):** `Rename` same name / cosmetic rename in Ready → **still Ready**, no event († today: ALIGNED in `StageLifecycleTests.Rename_real_change_keeps_Ready`).

**Regression:** competition **Running**, phase **Draft** — structural ops that today are blocked only via app/competition policy must **remain** blocked; Prepare/Start read **`ConstructionFrozen`**.

---

### C — Materialize / dematerialize

| Stage | Operation | Ch / Groups | Cup | Swiss | Expected V2 | Code |
| :---- | :-------- | :---------- | :-- | :---- | :---------- | :--- |
| Draft | Materialize (batch) | Deny | Deny | Deny (no Swiss batch path) | No production in Draft | `Application.StructureNotMutable` |
| Ready | Materialize | Allow if eligible>0 | Allow if eligible | N/A — use Running | Success | — |
| Running | Materialize Ch/G/Cup | Deny | Deny | — | — | `Application.StructureNotMutable` |
| Running | `GenerateNextRound` | N/A | N/A | **Allow** (option A) | Success | — |
| Ready | Dematerialize full phase | Allow if gates pass | Allow if gates pass | N/A | Success / disabled | `Application.DematerializationBlocked`, `DematerializeDisabledReasons.*` |
| Running | Dematerialize full phase Ch/G/Cup | Deny | Deny | — | — | `StageLifecycleNotSupported` → `Application.StructureNotMutable` |
| Running | Dematerialize last Swiss round | N/A | N/A | Allow if gates pass | ALIGNED | Same dematerialize codes |

---

### D — Planification (schedule apply)

| Stage | `ApplySchedule` / placements | Expected V2 |
| :---- | :--------------------------- | :---------- |
| Draft | No matches to place | N/A or no-op scope |
| Ready | **Allow** on materialized Scheduled/Postponed targets | Success |
| Running / Suspended | **Allow** | Success (ALIGNED) |
| Completed | **Deny** | **PROPOSED** `Application.StageExploitationClosed` or `Application.StructureNotMutable` |

---

### E — Sportive match operations (application guard required)

Parent stage must be loaded (or match carries stage id) for guard. **V2:** sportive = start live, finish, running score mutations, sheet changes that imply exploitation (align with existing `MatchErrorCodes` where domain already enforces match state).

| Stage | Operation | Expected V2 | Code |
| :---- | :---------- | :---------- | :--- |
| Draft | `StartMatch` | Deny | **PROPOSED** `Application.StageExploitationNotOpen` |
| Ready | `StartMatch` | **Deny** | `Application.StageExploitationNotOpen` |
| Ready | `FinishMatch` / running score edit | Deny | Same |
| Running / Suspended | `StartMatch` (Scheduled) | Allow | — |
| Completed | **Any** sportive or planif mutation | **Deny** | **PROPOSED** `Application.StageExploitationClosed` |

Domain may still throw `Match.InvalidTransition` if invoked without app guard — tests should assert **application** rejects first for Completed/Ready.

---

### F — Read model (Structure `lifecycle.transition`)

| Stage | Projected primary `code` | `enabled: true` when |
| :---- | :----------------------- | :------------------- |
| Draft | `PrepareStage` | Prepare gates pass; not `ConstructionFrozen` / `CompetitionClosed` |
| Ready | `StartStage` | Start readiness + not frozen/closed |
| Ready | **PROPOSED** `RevertToDraftStage` | No attached matches |
| Running / Suspended | `CompleteStage` | `open == 0` |
| Running / Suspended | **PROPOSED** `RevertToReadyStage` | Revert preflight pass |
| Completed | null | — |

Secondary transitions (reverts) must not hide primary without explicit product layout (Matchs section — later P3).

---

### G — Reference scenarios (must-have tests)

#### G1 — `RevertToReady` with planned matches only

| Step | State | Action | Expected |
| :--- | :---- | :----- | :------- |
| 1 | Ready, Ch, all eligible materialized, matches **Scheduled** | `StartStage` | Running |
| 2 | Running, only Scheduled/Postponed | `RevertToReady` | **Ready**, matches **unchanged**, placements preserved |
| 3 | Ready, same matches Scheduled | `StartMatch` | **Reject** `Application.StageExploitationNotOpen` |
| 4 | Ready | `ApplySchedule` (valid targets) | **Allow** |
| 5 | Ready | `StartStage` | Running |
| 6 | Running | `StartMatch` | Live allowed |

#### G2 — `RevertToReady` blocked after engagement

| Step | State | Action | Expected |
| :--- | :---- | :----- | :------- |
| 1 | Running, one match **Live** or **Finished** | `RevertToReady` | Reject — **PROPOSED** `Application.RevertToReadyBlocked` (reason aligns with `DematerializeDisabledReasons.MatchLifecycleStarted`) |
| 2 | Running, Cancelled **with** observed Live | Same | Reject |

#### G3 — `Completed` — application blocks match mutations

| Step | State | Action | Expected |
| :--- | :---- | :----- | :------- |
| 1 | Completed, match Scheduled | `StartMatch` via executor | **Reject** `Application.StageExploitationClosed` |
| 2 | Completed | `ApplySchedule` | **Reject** same |
| 3 | Completed | `FinishMatch` | **Reject** same |
| 4 | Completed | Structural hub | Only rename (read) — ALIGNED |

#### G4 — Incrément 1 regression bundle

| Case | Assert unchanged |
| :--- | :----------------- |
| Swiss | Prepare without matchdays → Start → `GenerateNextRound` still Running-only |
| Cup | Start blocked when initial positional round not playable (`Stage.NotReady` / `InitialRoundNotPlayable`) |
| Multi-phase | Complete poules while finale Draft — still allowed (Gate B) |
| Competition Running | Phase Draft: Prepare/Start disabled with `ConstructionFrozen`; **no** accidental unlock of structure edit via domain |

---

### H — Start readiness by format (V2)

| Format | Start **allowed** when | Start **blocked** — code |
| :----- | :--------------------- | :----------------------- |
| **Championship / Groups** | Every **eligible** confrontation materialized | **PROPOSED** read `UnmaterializedConfrontations`; app gate before `Stage.Start` |
| **Championship / Groups** | **Zero** eligible confrontations | **Product edge:** Start **denied** if format implies matches (document per scenario); else explicit “empty exploitation” decision — default test: **deny** when `determined > 0` and `eligible > 0` |
| **Cup** | First **playable** round fully materialized | `Stage.NotReady`, read `InitialRoundNotPlayable` (+ **PROPOSED** `InitialRoundIncomplete` if slotted but not materialized) |
| **Swiss** | After Prepare; **0 matches OK** | No full-calendar gate; Start then GenerateNextRound in Running |

---

### Proposed error codes (register before P1)

| Code | Layer | Use |
| :--- | :---- | :-- |
| `Application.StageExploitationNotOpen` | Application | Ready/Draft sportive ops |
| `Application.StageExploitationClosed` | Application | Completed planif + sportive |
| `Application.RevertToReadyBlocked` | Application | Revert when match engaged |
| `Application.RevertToDraftBlocked` | Application | Revert when production present |
| `Application.StartBlockedUnmaterialized` | Application | Ch/G Start gate |
| `Stage.RevertBlocked` (or split Ready/Draft) | Domain | Optional if domain owns revert |
| Read: `UnmaterializedConfrontations`, `ProductionPresent`, `MatchLifecycleStarted` (revert) | Structure lifecycle DTO | Anticipated disable on transitions |

Reuse existing **`DematerializeDisabledReasons.MatchLifecycleStarted`** as **reason detail** inside revert preflight where possible — avoid duplicate predicates.

---

### Matrix coverage checklist

- [x] § A — RevertToReady / ConstructionFrozen (Structure remediation 2026-10-12)  
- [ ] § B sample: one op per category (matchday, slot, qual rules) × {Draft, Ready, Running}  
- [ ] § C × four formats where applicable  
- [x] § G1–G2 — RevertToReady scheduled vs engaged (`RevertStageLifecycleTests`, assembler cancel-start)  
- [ ] § G3–G4 full bundle  
- [x] RevertToReady / ConstructionFrozen † rows aligned post-remediation  

---

## Notion sync

| Artifact | Role |
| :------- | :--- |
| **Notion — Lifecycle phase UI Structure** | **SoT** — V2 contract (2026-10-12) + legacy UX sections (some superseded; see page callouts) |
| **Notion — Arbitrage matrice 2b** | Superseded — copy lives in Matchs column; sync Notion Lifecycle UI page |
| **This Git appendix** | Code-verified facts + **V2 gap audit** + 2b matrix draft |

**Implementation (2026-10-11):** SPA PR aligned with matrix defaults (product “Go”) — `StageNotReady` band, dedup Regulation/Exits/OpenMatches, prepare-gate `lifecycle.disabled.*`, lifecycle mutation copy + `_unknown` fallbacks, `errors:Application.StageCompletionBlocked`. **2c step label unchanged** (« Réglement »). Full Notion line-by-line sign-off still recommended.

**After formal Notion validation:** add `Notion validation: YYYY-MM-DD` and list any deviations from shipped behaviour.

---

## Post-validation PR scope (template — do not implement until gate)

- [ ] Notion matrix signed (`StageNotReady`, gates without stepper, dedup table)
- [ ] 2c label decided
- [ ] `_unknown` keys approved or explicitly deferred
- [ ] SPA: band visibility / dedup only for **approved** rows
- [ ] SPA: no read-model invention in copy layer
- [ ] `errors:Application.StageCompletionBlocked` if Complete POST in scope
- [ ] Tests: model helpers for dedup + one band test per critical row
