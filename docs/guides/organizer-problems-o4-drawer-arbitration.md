# Organizer problems — O4 drawer scope (G3 atelier)

**Status:** O4 **D1 revision** + **D5 severity/presentation alignment** implemented (2026-10-12) — [Notion D5](https://app.notion.com/p/3f775fb92d10819e9105d84c43a390e6). **`GET /attention`** = T5 + **presented** Structure diagnostics; **`Severity`** `Blocking` \| `Warning`; Overview **`attentionSummary`** = blocking subset only; bell = all items.

**D5 (product):** [Structure severity & presentation alignment](#decision-5--structure-severity--presentation-alignment-d5) — three notions (severity, lifecycle gate, UX presentation); A4 severity matrix **Acceptée** with `SlotFeedMissing` **Blocking when presented**; Structure surfaces MUST NOT visually downgrade `Blocking` (e.g. `progress` tone on `PopulationShortfall`, orange surplus meter for `OverCapacityEdge`).

**Prerequisite question:** What user problem does each candidate fact solve that Structure, Overview, or lifecycle surfaces do not already address?

Detecting a fact in the backend is **not** sufficient for a drawer line.

---

## Two exercises (do not conflate)

| Exercise | Input | Output |
| :------- | :---- | :----- |
| **A — Generic O4 principles** | Inventories in this doc (Annexes A–B), workshop proposals below | DECIDED rules for line unit, catalogue stance, exclusions, severity mapping |
| **B — Competition-specific** | Export `GET /competitions/{id}/structure` (issues per stage, blockers) | What the banner “N problems” actually is; dedup examples grounded in one org state |

Exercise **A** can proceed **without** a live export. Exercise **B** is required to interpret a concrete count (e.g. six Structure problems) and to validate dedup against real overlaps — not to decide whether the drawer should mirror the banner in general.

**Implementation:** [`StructurePresentedAttentionCollector`](../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePresentedAttentionCollector.cs) + [`NeedsAttentionAssembler`](../../src/PlayUp/MyClub.PlayUp.Application/Reads/NeedsAttentionAssembler.cs).

---

## Decision 1 revision — Drawer = competition anomaly catalogue (PROPOSED)

**Status:** **IMPLEMENTED** (2026-10-12) — Notion [Organizer problems O4 — Drawer catalogue revision (D1 revision)](https://app.notion.com/p/3f775fb92d1081c39938c0e423f593c9). Supersedes conservative [D1](https://app.notion.com/p/3f775fb92d1081d8909ac0a9eb199b69).

### Validated product intent (exchange — not implementation gate alone)

- **Drawer** = triage hub for **competition anomalies**: **Blocking** + **Warning**; organizer chooses whether to act.
- **Bell badge** = **all retained lines** (after projection + dedup rules).
- **`MissingStage` / `MissingStructure`** = creation paths + Overview — **not** drawer.
- Requires **O4 revision** (catalogue, Overview contracts, collector) — not a collector-only patch.

### Three surfaces (target contract)

| Surface | Target contract |
| :------ | :---------------- |
| Drawer + bell | All **retained** anomalies (`Blocking` + `Warning`) |
| `Overview.attentionSummary` | **Blocking only** (subset of `/attention`) |
| `Overview.situations` | Business situations with own nature; **`CompetitionSuspended`** stays **Informational**, not in summary, not on `/attention` |

### Proposals (workshop — validate at Acceptée)

| Topic | Proposal |
| :---- | :------- |
| **Catalogue** | Include retained Structure diagnostics (Annex A3), hub blockers (A2), and targeted gates; exclude birth codes and **`StructureGraphInvalid`** aggregate when children are surfaced (E3). Keep existing **T5**. |
| **Projection** | Drawer follows Structure **presented** semantics (`presentedProblemCount` rules), not raw analyzer output alone — see [Gate 3](#gate-3--structure-projection-parity). |
| **Dedup** | Default **one line per stage × code**; merge only on explicit business rule (D4). **Do not** auto-merge graph qual/prog codes with runtime T5 `Qualification*` / `Progression*`. |
| **Line identity** | `(Source, TargetType, TargetId)` — aligned with Overview `DeduplicateSituations`; Structure rows default `TargetType=Stage`, `TargetId=stageId`. |
| **Competition status** | Structure diagnostics **MAY** on `/attention` when **Running** / **Suspended** (written rules TBD). **`InsufficientParticipants`** stays **Draft / Ready** only (E4). |
| **Severity wire** | Contract values **`Blocking` \| `Warning`** (confirm at Acceptée). Per-code matrix: [Annex A4 revision (PROPOSED)](#a4-revision--per-code-catalogue-and-severity-proposed). |

### Pre-implementation gates (must pass before collector work)

**Gate 1 — Host Severity → Overview `Nature`**

- Map `NeedsAttentionItemDto.Severity` in `BuildSituations` (today all attention items forced to `NatureBlocking`).
- Preserve **`CompetitionSuspended`** as separately injected **Informational** (not via `/attention`).

**Gate 2 — E1 revised (Overview vs `/attention`)**

- Replace strict equality in [`ReadSurfaceEndpointTests`](../../tests/MyClub.PlayUp.Host.Tests/ReadSurfaceEndpointTests.cs) (`attentionSummary.Count` ≡ `attention.Count`, same sources) with:
  - `attentionSummary` = blocking subset of `/attention` items;
  - identity `(Source, TargetType, TargetId)` between summary and matching situations;
  - **`NeedsAttentionDto.Count`** = all items (bell); Overview page keeps **`attentionSummary.count`** for blocking-only display.

**Gate 3 — Structure projection parity**

| Rule | SPA (presented) | Risk if collector uses Host raw `structureIssues[]` only |
| :--- | :-------------- | :------------------------------------------------------- |
| Silence `SlotFeedMissing` when draw engaged | [`projectPresentedStageIssueCodes`](../../src/PlayUp/web/src/pages/structure/structureDiagnosticPresentation.ts) | Banner ≠ bell count |
| Drop `ExpectedRankUnreachable` when `AuthoredAwardUnreachable` present | [`projectPresentedIssueCodes`](../../src/PlayUp/web/src/pages/structure/structureDiagnosticPresentation.ts) | Extra drawer line |
| Synthetic `PopulationShortfall` (U1) | [`StructurePresentedStageDiagnostics`](../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePresentedStageDiagnostics.cs) → `presentedIssueCodes` | SPA consumes Host only (no local synthesis) |
| `MissingPotRules` per phase | `readiness.blockerDetails` (stage-attributed) → `presentedIssueCodes` | Missing line |
| `CupBracketInvalid` | `readiness.blockerDetails` (per invalid Cup hub) → `presentedIssueCodes`; `/attention` **Stage** target | Missing or wrong attribution |

**Decision at Acceptée:** emit **presented-equivalent** items from Application **or** document + test Host ↔ SPA equivalence — do not assume factorization before comparing effective rules.

**Gate 4 — Line identity and dedup**

- Fix dedup and T5 ↔ graph cohabitation rules **before** locking severity matrix and bell line count.

### If Acceptée — related contract amendments

- **D2 E1, E2, E4** (and SPA denylist) must be updated to match extended catalogue; E5 (`CompetitionSuspended`) unchanged.
- Implementation checklist: [If DECIDED — implementation checklist](#if-decided--implementation-checklist) below.

---

## Workshop order (recommended)

1. Validate or correct **line unit / dedup** (decision 4).
2. Fill **Annex A4** using Annex A inventory + family catalogue proposals (decision 1 — per code or per family default).
3. Write **exclusion** and **severity mapping** rules into the O4 contract (decisions 2–3).
4. Record outcomes in **Notion Décisions** with status **`DECIDED`** before any Application or SPA drawer work.

---

## Four decisions (distinct)

| # | Topic | Output |
| :- | :---- | :----- |
| 1 | **Catalogue** | Which T2/T3/T4 facts become `GET /attention` items |
| 2 | **Exclusions** | Lifecycle phase, Running/Suspended rules, Overview dedup |
| 3 | **Priority / severity** | Meaning of `Severity` vs `semanticLevel` vs SPA tone — no implicit merge |
| 4 | **Line unit / dedup** | One row = one business fact vs one action vs one root cause |

**Blocker:** Decide **(4)** before designing any new Application collector or dedup rules.

---

## Current policy (unchanged until O4)

- **T5 only** on `GET /attention`: `InsufficientParticipants`, `DrawNoSolution`, `Qualification*`, `Progression*`.
- Construction denylist SPA (guardrail): [`attentionPartition.ts`](../../src/PlayUp/web/src/shell/attentionPartition.ts).
- Structure hub diagnostics (T2/T3/T4) → Structure PageHead / fiche / banner CTAs — **not** mirrored in the drawer today.

---

## Workshop proposals (submitted for validation — not DECIDED)

These rows capture recommended **discussion defaults** for the atelier. They do **not** change code or contract until validated in Notion Décisions.

| # | Topic | Proposal | Rationale |
| :- | :---- | :------- | :-------- |
| 1 | Catalogue | Do **not** bulk-import all T2/T3/T4 facts. Evaluate **each category / code** individually. | A drawer line must add **action or information** not already clear on Structure, Overview, or lifecycle. |
| 2 | Exclusions | Exclude situations already covered by Overview or lifecycle UX. Define **explicit** rules for **Running** and **Suspended** (names alone are insufficient). | Avoid duplicate triage and actions incompatible with competition state. |
| 3 | Severity | Keep **three** layers: Host `Severity`, product `semanticLevel`, drawer visual tone. | Contract severity ≠ felt urgency for organizers. |
| 4 | Line unit / dedup | Prefer **one line per actionable business problem**; group symptoms sharing the **same correction** when dedup rules allow. | Avoid N lines for one fix; do not hide independent actions. |

**Order:** arbitrate **(4)** first — it constrains collector shape and dedup.

---

## Decision 4 — Line unit and deduplication

**Status:** **DECIDED** (2026-10-12) — Notion [Organizer problems O4 — Drawer line unit & dedup (D4)](https://app.notion.com/p/3f775fb92d1081ff81a6dcdd40f33821) **✅ Acceptée**.

### Principle (DECIDED)

One drawer line represents one **business problem the organizer can act on**.

- Several diagnostics may be **grouped** when the organizer can resolve them with the **same action** in the **same context**.
- The same code on **several phases** does **not** automatically become one line.
- **Independent** corrections stay **separate lines**, even when diagnostics share a technical root cause.
- Dedup must **never hide** a problem that requires a **different** action.

**Dedup criterion:** the **realizable business action**, with its **context** and **targets** — not wire code, display copy, or an assumed root cause.

### Clarifications (part of the proposal)

**1 — Multiple phases on one line**

- Allow grouping **only** when a **shared correction** is genuinely possible.
- A grouped line must carry **all affected phases** as **explicit targets** — not a redirect to an arbitrary single phase.
- If the current `/attention` contract cannot represent multi-target rows cleanly → **prefer one line per phase** over an ambiguous payload.

**2 — Primary CTA**

- One **primary CTA** per grouped line, pointing to a destination where the **shared** correction can actually be executed.
- If diagnostics imply **incompatible** destinations → **do not group** merely because labels look similar.

---

## Workshop sequence (O4)

| Step | Topic | Status |
| :--- | :---- | :----- |
| 4 | Line unit / dedup | **DECIDED** — Notion D4 |
| 1 | Catalogue | **DECIDED** — Notion D1 (2026-10-12 conservative) |
| 1′ | Catalogue revision | **PROPOSED** — Notion [D1 revision](https://app.notion.com/p/3f775fb92d1081c39938c0e423f593c9) |
| 2 | Exclusions | **DECIDED** — Notion D2 (**E1/E2/E4 amend** if D1 revision Acceptée) |
| 3 | Severity mapping | **DECIDED** — Notion D3 (**extend matrix** if D1 revision Acceptée) |
| — | Collector / payload implementation | **Blocked** until D1 revision **Acceptée** + gates 1–4 |

---

## Decision 1 — Catalogue (superseded when D1 revision Acceptée)

**Status:** **DECIDED** (2026-10-12) — Notion [Organizer problems O4 — Drawer catalogue (D1)](https://app.notion.com/p/3f775fb92d1081d8909ac0a9eb199b69) **✅ Acceptée**. **Historical** conservative stance; see [Decision 1 revision](#decision-1-revision--drawer--competition-anomaly-catalogue-proposed) for target catalogue.

**Outcome:**

- **No bulk import** of Structure hub diagnostics (`structureIssues[]`, synthetic banner rows) into `GET /attention`.
- **T5 unchanged** — only existing runtime sources remain on `/attention` until a **future** product decision adds a code.
- **T2/T3/T4** (Annex A2/A3): **Exclude from drawer** by default; SoT stays **Structure** (Overview for birth: `MissingStage`, `MissingStructure`).
- **`StructureGraphInvalid` aggregate:** never a drawer line when detailed child issues are already surfaced.
- **“Individual review” families** (graph routing, destinations, capacity gates, population family): **Exclude by default**; reopen only via a **new** Notion decision documenting cross-page triage value.

Conservative stance retained: the drawer is **not** a mirror of the Structure banner.

| Problem family | Initial recommendation | Rationale |
| :------------- | :--------------------- | :-------- |
| `MissingStage`, `MissingStructure` | Keep creation paths + Overview SoT | Initial configuration, not runtime triage |
| `MissingPotRules` | Keep Structure SoT | Banner + navigation to draw section already contextualize action |
| `CupBracketInvalid` | Keep Structure SoT | Stage-attributed (`blockerDetails.stageId`); Construction deep-link on owner phase |
| `StructureGraphInvalid` (aggregate) | **No extra drawer line** if detailed child issues are shown elsewhere | Avoid global blocker + causes duplication |
| `OverCapacityEdge`, `OverExtraction`, `ConfrontationUndecidable` | Keep Structure SoT by default; **individual** review if drawer adds cross-page action | Different contexts and corrections |
| `CycleDetected`, `RoutingNondeterminism`, missing qual/prog destinations | **No bulk import**; per-code review vs qual/prog/exits overlap | Related but not necessarily equivalent actions |
| `SlotFeedMissing`, `SlotFeedInvalid`, `SlotFeedMultiple` | Keep Structure SoT | Places and feed configuration |
| `PopulationShortfall`, `SourcePopulationShortfall`, `NonRootUnfed` | Review **together** without assuming merge | Same theme (promised/available teams) but corrections may differ |
| `AuthoredAwardUnreachable`, `ExpectedRankUnreachable` | Keep Structure SoT | Attribution / expected ranks; specific actions |

Map codes → family via [Annex A](#annex-a--candidate-facts-inventory-from-git-for-individual-o4-rows).

---

## Decision 2 — Exclusions

**Status:** **DECIDED** (2026-10-12) — Notion [Organizer problems O4 — Drawer exclusions (D2)](https://app.notion.com/p/3f775fb92d10817c8ff0fcb5e4fc87a9) **✅ Acceptée**.

**Scope:** what must **never** appear on `GET /attention` / drawer, and parity rules vs Overview — **in addition to** D1 catalogue (T2/T3/T4 excluded).

### E1 — Overview and drawer parity (T5)

- **`GET /attention`** is the **only** catalogue for drawer lines (T5 under D1).
- **`Overview.attentionSummary`** MUST remain the **same blocking ensemble** as `/attention` (same sources; Host contract test). No second triage catalogue on Overview for construction.
- **Informational** Overview situations (e.g. `CompetitionSuspended`, nature Informational) MUST **NOT** be promoted to `/attention` or the drawer.

### E2 — Structure / construction (all competition statuses)

- T2/T3/T4 facts (D1 Annex A2/A3) MUST **NOT** be collected on `/attention` when competition is **Draft, Ready, Running, Suspended, Completed, or Archived**.
- SPA construction **denylist** ([`attentionPartition.ts`](../../src/PlayUp/web/src/shell/attentionPartition.ts)) stays a **guardrail** if Host regresses.

### E3 — Aggregates vs detail

- MUST **NOT** emit a drawer/`/attention` line for **`StructureGraphInvalid`** (or other Prepare/readiness **aggregates**) when D1 excludes them.
- If a **future** decision adds a **child** issue code to `/attention`, MUST **NOT** also emit the **parent aggregate** for the same organizer action (D4 dedup).

### E4 — `Running`

- **`InsufficientParticipants`**: MUST **NOT** appear on `/attention` when status is **not** Draft or Ready (preserve today).
- Other **T5** sources (`DrawNoSolution`, `Qualification*`, `Progression*`) MAY appear when Running — runtime consequences are in scope.
- No Running-specific **expansion** of catalogue beyond T5.

### E5 — `Suspended`

- MUST **NOT** add a **drawer-only** blocking line for “competition suspended”; Overview keeps the **informational** suspended situation (not in `attentionSummary`).
- **T5** collection **unchanged** while Suspended (pending qual/prog/draw may still require organizer action unless a **separate** lifecycle rule blocks mutations).
- Structure construction diagnostics remain **out of drawer** (D1 + E2).

### E6 — Stage lifecycle (collector input, not drawer catalogue)

- Stage status (Draft / Ready / Running / …) does **not** by itself add new `/attention` **source kinds** under D2. Eligibility of existing T5 rules follows domain evaluators (e.g. finished legs for progression).

[Annex B](#annex-b--running--suspended-code-behaviour-reconciliation) records **pre-D2 code behaviour** for reconciliation.

---

## Decision 3 — Severity and presentation layers

**Status:** **DECIDED** (2026-10-12) — Notion [Organizer problems O4 — Drawer severity layers (D3)](https://app.notion.com/p/3f775fb92d1081f59b0ff00bf957c243) **✅ Acceptée**.

Three **independent** layers — no automatic collapse.

| Layer | Wire / location | Role |
| :---- | :-------------- | :--- |
| **Host `Severity`** | `NeedsAttentionItemDto.Severity` → SPA `nature` | **Catalogue partition** for `/attention` and Overview `attentionSummary` (blocking vs non-blocking ensemble). |
| **`semanticLevel`** | Optional `problem.semanticLevel` on read DTOs | **Meaning** for organizer-problem enrichment (Gate / Signal / Attention / Information) — copy, aliases, lifecycle/structure SoT — **not** the primary drawer group key today. |
| **Drawer visual tone** | SPA `functionalProblemPresentation` / row chrome | **Presentation only** (warning vs attention vs danger) from **source + canal rules**, not from `Severity` alone. |

### S1 — No implicit mapping

- MUST **NOT** derive drawer **row colour** or **group** (`Bloquant` vs `À traiter`) from `problem.semanticLevel` alone.
- MUST **NOT** derive `semanticLevel` from Host `Severity` automatically when emitting `problem` (explicit resolver rules only).

### S2 — Host `Severity` → drawer / Overview groups

- SPA maps `NeedsAttentionItem.severity` → `OverviewSituation.nature` ([`needsAttentionToSituation.ts`](../../src/PlayUp/web/src/shell/needsAttentionToSituation.ts)).
- Drawer groups use `nature === 'Blocking'` vs else ([`attentionPartition.ts`](../../src/PlayUp/web/src/shell/attentionPartition.ts)) — same axis as Overview `attentionSummary`.
- **Today (T5):** all emitted items use **`Blocking`**; non-blocking `/attention` items are **out of scope** until a future decision adds them with an explicit `Severity` value.

### S3 — `semanticLevel` usage (optional `problem`)

- Used for **canon / presentation** (`organizerProblemPresentationCode`, structure issue semantics) per Step 2 pilot — **not** for `/attention` item inclusion (D1).
- **`Gate`**: lifecycle Prepare/Complete and structure gates — primarily **Structure / fiche**, not drawer catalogue under current T5.
- **`Attention`**: runtime intervention (e.g. `QualificationPending` pilot on R2).
- **`Signal` / `Information`**: diagnostics and echoes — **Structure** or Overview informational situations; **not** `/attention` unless a future catalogue decision says otherwise.

### S4 — Drawer visual tone (SPA)

- Tone comes from **`situationPresentation` / `functionalProblemPresentation`** (e.g. `DrawNoSolution` → warning; most T5 → attention).
- When `problem` is present, tone MAY consider **source + canal**; **`semanticLevel` MAY inform copy** but MUST NOT replace S2 grouping.

### S5 — T5 reference matrix (current contract)

| T5 `source` | Host `Severity` | Typical `problem.semanticLevel` | Drawer group (`nature`) | Visual tone (SPA) |
| :---------- | :-------------- | :------------------------------ | :---------------------- | :---------------- |
| `InsufficientParticipants` | `Blocking` | (usually omitted) | Bloquant | attention / teams canal |
| `DrawNoSolution` | `Blocking` | (omitted) | Bloquant | warning |
| `QualificationPending` | `Blocking` | `Attention` (pilot) | Bloquant | attention |
| `QualificationConflict` | `Blocking` | (omitted) | Bloquant | attention |
| `ProgressionPending` | `Blocking` | (omitted) | Bloquant | attention |
| `ProgressionConflict` | `Blocking` | (omitted) | Bloquant | attention |

Future non-`Blocking` `Severity` values require an explicit product decision and updates to this matrix, Overview parity tests, and drawer copy.

---

## Decision 5 — Structure severity & presentation alignment (D5)

**Status:** **DECIDED** (2026-10-12) — Notion [Structure severity & presentation alignment (D5)](https://app.notion.com/p/3f775fb92d10819e9105d84c43a390e6) **✅ Acceptée**. **Extends** [D3](#decision-3--severity-and-presentation-layers) for Structure catalogue codes; **does not replace** D3 layer separation.

### Product contract — three notions (do not conflate)

| Notion | Contract |
| :----- | :------- |
| **Product severity** | `Blocking` \| `Warning` — same meaning on `/attention`, drawer groups, bell aggregate colour, Structure diagnostic chrome when the **same code** is shown |
| **Lifecycle gate** | Explicit rule per transition (Prepare, Start, Complete, …) — **may** block Prepare while severity is Warning for another fact; severity describes **functional gravity**, gates describe **forbidden transitions** |
| **UX presentation** | Contextual copy (option **B**): drawer = **short triage title**; Structure fiche = **contextual detail** (counts, meter, strip). **MUST NOT** contradict product severity (no `Blocking` shown as informational orange) |

### D5-P1 — Severity vs tone

- **`tone` / `progress` on Structure** MAY express **incomplete workflow** only when product severity is **`Warning`** (or no catalogue line).
- **`Blocking`** codes MUST use **danger** chrome on Structure bandeaux and **matching** meter/strip severity when the meter expresses the **same fact** as the wire code (e.g. `OverCapacityEdge` surplus, `PopulationShortfall` deficit).
- Drawer row partition remains **`nature` from Host `Severity`** (D3 S2); drawer **title** uses short copy (enums / attentionSource or dedicated triage keys — not necessarily identical to fiche meter strings).

### D5-P2 — Projection (draw engaged silence)

When **`SlotFeedMissing`** is **silenced** because draw is engaged and not yet published ([Gate 3](#gate-3--structure-projection-parity), `isSlotFeedAwaitingDraw`):

- MUST **omit** from `/attention`, bell count, PageHead **presented** counts, and Structure banners that depend on presented codes.
- Silence MUST **not** hide independent codes or disable domain gates elsewhere.

When **`SlotFeedMissing`** is **presented** (diagnostic signal; **not** a Prepare integrity gate — D3 / `IsPrepareGateIssue`):

- Host **`Severity` = `Blocking`** (not `Warning`).

### D5-P3 — Chat-arbitrated codes (priority)

| Code | Product severity | Gate / notes | Presentation |
| :--- | :--------------- | :----------- | :----------- |
| `OverCapacityEdge` | **Warning** | Prepare gate still applies (D5: severity ≠ gate) | Fiche: « N places en trop » **orange**; drawer: groupe **À traiter**, titre court |
| `SlotFeedMissing` | **Blocking** when presented; **absent** when silenced | Not a Prepare integrity gate (D3); blocks Materialize / occupancy presentation | Strip empty-tableau copy aligned with **Blocking** when `slotFeedMissingActive` |
| `PopulationShortfall` | **Blocking** (incl. U1 synthetic) | Composition / occupancy (see Application gates) | No **`progress`** tone masking; deficit meter stays **red**; visible in Structure syntheses |
| `NonRootUnfed` | **Warning** (default) | Not a Prepare graph gate by code alone | Monitor; upgrade to **Blocking** only with an **explicit** gate + tests — do not infer from name |

### D5-P4 — Open verification (before code)

- **`NonRootUnfed`**: confirm intermediate construction vs hard anomaly per competition shape (no automatic reclass to Blocking).
- **`SlotFeedMissing`**: confirm Host can use global **`Blocking`** + presentation silence (preferred) vs conditional severity emission.

### Structure PageHead — global status pill (DECIDED 2026-10-12, amended 2026-10-18)

**Intent:** Option **A** (no PageHead problem catalogue) + **compact nominal status** so hub state is visible without duplicating fiche or drawer detail.

| Rule | Contract |
| :--- | :------- |
| **Placement** | `ds-page-head__note` — DS **`Status`** `context` · `outline` · **`pill`**, tones `error` / `attention` / `info` / **`success`**, icon + short label. |
| **Content** | **Nominal status**; **no** full sentence, **no** imperative/CTA, **no** pause-specific PageHead copy, **no** count (D1). |
| **Next step (`info`)** | Sole SoT: **`StructureView.hubNextStep`** (Application), **structure scope only** (no schedule/Swiss). Labels `structure.readiness.hubStep.*` / `*_solo` when one phase — see [truth matrix](../../design/structure-lifecycle/structure-pagehead-truth-matrix.md) §3. |
| **Calm (`success`)** | `structure.readiness.structureCalm` only when `isStructureHubCalm` — **not** when `hubNextStep === null` alone; **not** when only schedule/Swiss slice remains (`operationalSlicePending`). |
| **Negative / warning** | `anomalyHintTitle` / `warningHintTitle`; severity from `resolveStructurePageHeadNoteKind` unchanged. |
| **Priority** | `error` → `attention` → `info` → `success` → silence; competition **Completed** / **Archived** → no pill (truth matrix §1). |
| **Visibility (D2)** | No positive while `attentionPhase !== 'success'`; no SPA draw-gate or cross-phase priority recompute. |
| **Do not use** | `presentedProblemCount` for PageHead copy; `nextStep.*` imperatives in PageHead; `readyForNextSlice` / `readyForMaterialization` for pills; page-local CSS mimicking `Status`. |
| **Removed** | Long synthesis sentences; pause PageHead variants; `StructureReadinessBanner` catalogue. |

**Copy (FR / EN):** `anomalyHintTitle`, `warningHintTitle`, `structureCalm`, `hubStep.*` in Git i18n (`structure.json`).

**Shell count (D3):** SPA badge and drawer header use **`presentableAttentionCount`** (map → dedupe → denylist → blocking + warning rows). API `Count => Items.Count` unchanged.

**Implementation gate:** [`structurePageHeadReadiness.ts`](../../src/PlayUp/web/src/pages/structure/structurePageHeadReadiness.ts) + [`attentionPartition.ts`](../../src/PlayUp/web/src/shell/attentionPartition.ts) presentable helpers.

---

## D5 — Implementation order (after Notion Acceptée)

1. Host: [`StructurePresentedAttentionCollector`](../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePresentedAttentionCollector.cs) severity map + contract tests.
2. SPA: shared severity → chrome mapping ([`structureDiagnosticPresentation.ts`](../../src/PlayUp/web/src/pages/structure/structureDiagnosticPresentation.ts), meters, operational strip).
3. Drawer triage titles (option B) vs fiche contextual keys.
4. Visual QA: surplus, empty tableau, population deficit, draw-engaged silence — **done** (2026-10-13, Structure + drawer + PageHead).

---

## Per-candidate decision rule (checklist)

For each fact proposed for the drawer, answer:

1. What **concrete problem** does the organizer face?
2. Where is it **already visible**? (Structure banner/fiche, Overview, lifecycle, current drawer)
3. What **extra action or signal** does the drawer add?
4. Does it share a **root cause** with another candidate line (dedup under decision 4)?
5. In which **competition / stage statuses** is the line valid?

If the drawer adds neither new information nor useful action → keep SoT on Structure or Overview.

---

## What “6 problems” on Structure means (code — not your competition’s exact list)

The PageHead banner count is **`presentedProblemCount`** ([`structureReadinessSummary.ts`](../../src/PlayUp/web/src/pages/structure/structureReadinessSummary.ts)):

- Sum of **projected** `structureIssues[]` per stage (after SPA rules, e.g. `SlotFeedMissing` silenced while draw awaiting publish).
- **Plus** `+1` per phase with engaged draw and **missing pot rules** (`MissingPotRules` synthetic — E1/E6).
- **Plus** `+1` when readiness blocker **`CupBracketInvalid`** (competition-level shape).

So **6 ≠ automatically six distinct codes** — it can be several issues across phases plus pot gaps plus bracket.

**To fill the workshop table for a real competition:** export `GET /competitions/{id}/structure` and list, per stage, `structureIssues` + whether `MissingPotRules` / `CupBracketInvalid` apply. Match banner rows via [`collectAllStructureReadinessActionItems`](../../src/PlayUp/web/src/pages/structure/structureReadinessActions.ts) (CTA + deep link).

---

## Annex A — Candidate facts (inventory from Git, for individual O4 rows)

Use columns **Proposal** / **Decision (validated)** during the atelier. French labels below from [`structure.json`](../../src/PlayUp/web/src/i18n/locales/fr/structure.json) (`graph.issues.*` / readiness copy).

### A1 — Already T5 on `GET /attention` (reference — not O4 expansion by default)

| Source (wire) | FR label (short) | Typical SoT today | Drawer today |
| :------------ | :----------------- | :---------------- | :----------- |
| `InsufficientParticipants` | (Teams / enums) | Teams + Overview when Draft/Ready; **not** Structure count when phases exist | Yes (Draft/Ready only) |
| `DrawNoSolution` | Tirage sans solution | Structure draw fiche + toast | Yes |
| `QualificationPending` / `Conflict` | Qualification… | Structure / Apply flows | Yes |
| `ProgressionPending` / `Conflict` | Progression… | Structure / Match hub | Yes |

### A2 — Readiness blockers (competition / hub — mostly **not** in `presentedProblemCount` per issue code)

| Code | Role | Structure UX | In Overview `attentionSummary` today |
| :--- | :--- | :----------- | :------------------------------------- |
| `MissingStage` | Birth | Empty state | No (excluded) |
| `MissingStructure` | Birth | Empty / configure | No |
| `MissingPotRules` | Groups + draw engaged | Banner synthetic row → tirage section | No |
| `CupBracketInvalid` | Cup shape | Banner + construction | No |
| `StructureGraphInvalid` | Prepare gate aggregate | Lifecycle + issues children | No |
| `ConfrontationUndecidable` | Prepare gate | Fiche confrontation | No |
| `OverCapacityEdge` | Prepare gate | Capacity / graph | No |
| `OverExtraction` | Prepare gate | Exits / qual | No |

### A3 — Stage `structureIssues[]` (T2/T3/T4 — usual content of the “N problems” banner)

| Code | FR label (graph.issues) | CTA family (SPA) | Tile |
| :--- | :---------------------- | :--------------- | :--- |
| `CycleDetected` | Liaison circulaire entre phases | sorties | exits |
| `RoutingNondeterminism` | Même sélection → plusieurs destinations | qualification | exits |
| `DanglingQualificationTarget` | Destination de qualification introuvable | qualification | exits |
| `DanglingProgressionTarget` | Destination de progression introuvable | progression | exits |
| `MissingQualificationDestinationSlot` | Place de qualification manquante | qualification | exits |
| `MissingQualificationDestinationGroup` | Groupe de qualification manquant | qualification | exits |
| `MissingProgressionDestinationSlot` | Place de progression manquante | progression | exits |
| `MissingProgressionDestinationGroup` | Groupe de progression manquant | progression | exits |
| `SlotFeedMissing` | Place pas encore alimentée | places | places |
| `SlotFeedInvalid` | Plusieurs sources du même type… | places | places |
| `SlotFeedMultiple` | Plusieurs sources différentes… | places | places |
| `NonRootUnfed` | Aucune équipe n’alimente encore cette phase | affectation | capacity |
| `OverCapacityEdge` | Plus d’équipes promises que de places | structure | capacity |
| `OverExtraction` | Sortie demande plus d’équipes que… | qualification | exits |
| `PopulationShortfall` | Pas assez d’équipes promises pour cette phase | affectation | capacity |
| `SourcePopulationShortfall` | Pas assez d’équipes dans la phase source | affectation | capacity |
| `ConfrontationUndecidable` | Confrontation qui ne peut pas toujours départager | tieFormat | confrontation |
| `AuthoredAwardUnreachable` | Paire source introuvable | attribution | attribution |
| `ExpectedRankUnreachable` | Rang ne peut pas être produit… | attribution | attribution |

CTA navigation: [`resolveStructureDiagnosticNavigation`](../../src/PlayUp/web/src/pages/structure/structureReadinessActions.ts) → deep link Structure (`section`, `focus`, dialog).

### A4 — Per-code catalogue decisions (D1 DECIDED — historical conservative)

Superseded for product target by [A4 revision (PROPOSED)](#a4-revision--per-code-catalogue-and-severity-proposed) when D1 revision is **Acceptée**.

| Code | Decision (D1 2026-10-12) |
| :--- | :------------------------- |
| `InsufficientParticipants` | **Include (T5)** — Draft/Ready only |
| `DrawNoSolution` | **Include (T5)** |
| `QualificationPending` / `Conflict` | **Include (T5)** |
| `ProgressionPending` / `Conflict` | **Include (T5)** |
| `MissingStage` / `MissingStructure` | **Exclude** — birth |
| All other A2/A3 codes | **Exclude** (conservative) — see A4 revision |

### A4 revision — Per-code catalogue and severity (DECIDED — D1 revision + D5)

**Include** = emit on `/attention` when projection rules say the anomaly is **presented** (Gate 3). **Severity** = Host `Severity` (`Blocking` \| `Warning`). **Presentation** rules: [D5](#decision-5--structure-severity--presentation-alignment-d5).

| Code | Include | Severity | Notes |
| :--- | :------ | :------- | :---- |
| `InsufficientParticipants` | Yes (T5) | Blocking | Draft / Ready only |
| `DrawNoSolution` | Yes (T5) | Blocking | |
| `QualificationPending` | Yes (T5) | Blocking | Dedup vs graph qual codes — explicit rule |
| `QualificationConflict` | Yes (T5) | Blocking | |
| `ProgressionPending` | Yes (T5) | Blocking | Dedup vs graph prog codes — explicit rule |
| `ProgressionConflict` | Yes (T5) | Blocking | |
| `MissingStage` | **No** | — | Birth |
| `MissingStructure` | **No** | — | Birth |
| `MissingPotRules` | Yes | Blocking | Synthetic per stage (presented) |
| `CupBracketInvalid` | Yes | Blocking | Stage (`blockerDetails.stageId`; `/attention` TargetType=Stage) |
| `StructureGraphInvalid` | **No** | — | Aggregate when children listed |
| `ConfrontationUndecidable` | Yes | Blocking | |
| `OverCapacityEdge` | Yes | **Warning** | D5: surplus meter **orange**; Prepare may still refuse Ready |
| `OverExtraction` | Yes | Blocking | Stage |
| `CycleDetected` | Yes | Blocking | Stage |
| `RoutingNondeterminism` | Yes | Blocking | Stage |
| `DanglingQualificationTarget` | Yes | Blocking | Stage; no auto-merge with T5 qual |
| `DanglingProgressionTarget` | Yes | Blocking | Stage |
| `MissingQualificationDestinationSlot` | Yes | Blocking | Stage |
| `MissingQualificationDestinationGroup` | Yes | Blocking | Stage |
| `MissingProgressionDestinationSlot` | Yes | Blocking | Stage |
| `MissingProgressionDestinationGroup` | Yes | Blocking | Stage |
| `SlotFeedMissing` | Yes* | **Blocking*** | *Omit when draw-engaged silence (D5-P2); **Blocking** when presented |
| `SlotFeedInvalid` | Yes | Blocking | Stage |
| `SlotFeedMultiple` | Yes | Blocking | Stage |
| `NonRootUnfed` | Yes | Warning | D5: no Blocking without explicit gate |
| `PopulationShortfall` | Yes | Blocking | U1 synthetic; D5: no `progress` masking |
| `SourcePopulationShortfall` | Yes | Blocking | Stage |
| `AuthoredAwardUnreachable` | Yes | Warning | Presented merge drops `ExpectedRank…` |
| `ExpectedRankUnreachable` | Yes* | Warning | *Omit when `AuthoredAwardUnreachable` on same stage (presentation) |

**T5 ↔ Structure dedup (DECIDED):** never collapse graph destination codes with runtime `QualificationPending` / `ProgressionPending` solely because labels overlap; if both exist, **both lines** unless a future explicit merge rule documents the same organizer action.

Extended template (optional per row):

| Code | User problem (1) | Already visible (2) | Drawer adds (3) | Dedup group (4) | Phase/competition rules (5) | Proposal | Decision |
| :--- | :----------------- | :------------------ | :-------------- | :-------------- | :-------------------------- | :------- | :------- |

---

## Annex B — Running / Suspended (code behaviour reconciliation)

Product rules: **Decision 2** (E4–E5). Below is **behaviour before explicit D2** — use for audit only:

| Topic | Behaviour today |
| :---- | :-------------- |
| `InsufficientParticipants` | Emitted on `/attention` only when competition is **Draft** or **Ready** ([`NeedsAttentionAssembler`](../../src/PlayUp/MyClub.PlayUp.Application/Reads/NeedsAttentionAssembler.cs)). |
| Qual / prog / draw attentions | Evaluated from domain state; **not** gated on Running vs Ready in the same way as insufficient participants. |
| Overview `CompetitionSuspended` | Informational **situation** on Overview; **not** in `attentionSummary` / `/attention` ensemble ([Host test `Get_overview_suspended_…`](../../tests/MyClub.PlayUp.Host.Tests/ReadSurfaceEndpointTests.cs)). |
| Structure diagnostics | Shown from Structure read regardless of Running/Suspended unless UX hides sections (verify per surface in atelier). |

---

## If DECIDED — implementation checklist

- Notion Décision row (D5) + update [pilot aliases](./organizer-problems-pilot-aliases.md) when copy keys added
- Host: `StructurePresentedAttentionCollector` severity ([`SlotFeedMissing` Blocking when presented](#decision-5--structure-severity--presentation-alignment-d5))
- Host `/attention` contract tests (severity per A4 + Overview `attentionSummary` blocking subset)
- SPA: severity-aligned chrome (diagnostics, meters, strip); drawer triage titles (option B)
- Revisit [`functionalProblemPresentation.ts`](../../src/PlayUp/web/src/shell/functionalProblemPresentation.ts) — titles vs `nature`

## Current contract (code today vs target)

**Shipped (D1 revision):**

- **`GET /attention`:** T5 + presented Structure anomalies (A4). Exclusions E1–E6 (D2). Dedup D4.
- **Bell:** all items; **`attentionSummary`:** blocking subset.

**D5 implementation (2026-10-12):**

- Host: `SlotFeedMissing` → **`Blocking`** when emitted (silence unchanged when draw engaged).
- SPA: [`structureDiagnosticSeverity.ts`](../../src/PlayUp/web/src/pages/structure/structureDiagnosticSeverity.ts) + tone derived from severity; surplus meter **red**; strip places **danger** when `slotFeedMissingActive`; drawer triage titles via `enums.attentionSource` ([`functionalProblemPresentation.ts`](../../src/PlayUp/web/src/shell/functionalProblemPresentation.ts) + `situationPresentation`).
