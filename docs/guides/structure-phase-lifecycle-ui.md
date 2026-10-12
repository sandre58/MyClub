# Structure — phase lifecycle UI (spec appendix)

**Purpose:** Git-side design reference for the Structure phase fiche **status band**, stepper, and match stats. Product intent and durable UX decisions remain in **Notion** (*MyClub*); this document records **agreed direction**, **open product choices**, and **facts verified in code** so mockups and PRs do not drift from the domain.

**Last code audit:** 2026-10-11 (repository `MyClub`, paths below).

**PR gate (DECIDED):** No SPA PR for 2b/2c until the **2b matrix** is arbitrated in Notion and **2c** is decided.

**Notion arbitration (product):** [Arbitrage matrice 2b — bandeau cycle Structure (2026-10-11)](https://app.notion.com/p/3f675fb92d1081dba56afcbfacbd32e7) (under [Lifecycle phase — UI Structure](https://app.notion.com/p/3f575fb92d10816a96a9cf3c70de7bca)). Record validation date in § Notion sync when signed off.

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

## Notion sync

| Artifact | Role |
| :------- | :--- |
| **Notion — Arbitrage matrice 2b** | [Arbitrage matrice 2b (2026-10-11)](https://app.notion.com/p/3f675fb92d1081dba56afcbfacbd32e7) — line-by-line decisions |
| **Notion — Lifecycle phase UI Structure** | UX intent / double surface; may diverge from shipped SPA until reconciled |
| **This Git appendix** | Code-verified facts + matrix draft + post-validation PR scope |

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
