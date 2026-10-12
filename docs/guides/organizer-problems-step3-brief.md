# Organizer problems — Step 3 brief (documentation phase)

**Status:** **Phase 1 implemented (Application read models + tests).** SPA consumption and further codes remain optional. Matchs pilot **still Paused.**

**Purpose:** Record Step 2 follow-up: documentation perimeter, alias registry, and **Phase 1** additive read-model enrichment (approved 2026-10-12). Phase 1 **does not** change ProblemDetails, drawer policy, or the Paused Matchs pilot.

**References:**

- [Organizer problems audit](./organizer-problems-audit.md) — Step 2 record, VERIFY checklist, V1–V5 report  
- [Structure problems contract (Matchs — Paused)](./structure-problems-contract.md)

**Last updated:** 2026-10-12

---

## 1. Objectives

1. **Document** how Step 2 semantic principles would map onto **three existing read families** for **three pilot problems**, without inventing a universal DTO.
2. **Produce a pilot alias registry** (canon ↔ wire transports), grounded in VERIFY observations — especially POST ↔ read equivalence where it holds, and **documented divergence** where it does not (V2).
3. **Draft conceptual enrichment** per read family (what Application *would* expose later: semantic level, action-relative blocking, canon code). **Wire field names remain DEFERRED** until product resolves Step 2 wire shape.
4. **Prepare an implementation proposal** (DTO touch list, migration notes) as a **separate appendix** — submitted for explicit approval; **not** executed under this brief.

---

## 2. Authorization boundary

| Phase 1 — done (2026-10-12) | Still not allowed without explicit approval |
| :-------------------------- | :------------------------------------------ |
| Additive optional `problem` on R1/R2 DTOs; `structureIssueSemantics` on R3 | `ProblemDetails` / HTTP error contract changes |
| `OrganizerProblemSemanticsResolver` + Application tests | Matchs `structure.problems` pilot unpause or generalization |
| Pilot alias doc + TS type mirrors (no UI consumption) | Drawer denylist / Overview presentation refactor |
| Git docs (this brief, audit, aliases) | Universal organizer-problem DTO or cross-surface merge |

| Documentation phase (ongoing) | |
| :---------------------------- | :- |
| Alias tables, VERIFY extensions, product calls on V2/V3 (O1/O2) | |

---

## 3. Pilot scope

### 3.1 Read families (max three)

| # | Read family | Host / Application entry | Role in Step 3 |
| :- | :---------- | :----------------------- | :------------- |
| R1 | **Stage lifecycle transition** | `StructureStageLifecycleAssembler` → `lifecycle.transition` (and related disabled codes) | Anticipated **Gate** on actions (`PrepareStage`, `CompleteStage`, …) |
| R2 | **Needs attention item** | `NeedsAttentionAssembler` → `GET /attention` | **Attention** / runtime intervention (`QualificationPending`) |
| R3 | **Structure hub issue fact** | `StructureViewAssembler` → `structureIssues[]` | **Signal** / **Gate** source fact (`CycleDetected`); link to aggregates (V5) |

Other transports (readiness blockers, Overview situations, materialize disabled) are **referenced** in alias tables where they touch pilot codes but are **not** enrichment targets in the first documentation phase.

### 3.2 Pilot problems (three)

| Code | Primary read surfaces (today) | Step 2 semantic (target doc) | VERIFY summary |
| :--- | :---------------------------- | :--------------------------- | :------------- |
| **`OpenMatches`** | R1 Complete transition; POST Complete | Gate on **`CompleteStage`** only | **V1 closed** (engineering): same rule read vs POST; POST code `Application.StageCompletionBlocked` + `reasons` alias |
| **`CycleDetected`** | R3 issue; R1 Prepare (often via aggregate); POST Prepare | Gate on **`PrepareStage`** (when prepare gate applies) | **V2 open**: lifecycle may show `StructureGraphInvalid` while POST shows `Application.StructureCycleDetected` |
| **`QualificationPending`** | R2 attention | **Attention**; remediation **`ApplyQualification`** | **V3 open**: not lifecycle disabled shape; `blocksAction` vs actionable attention |

Stable **action vocabulary** for documentation (reuse existing Host / Overview codes — do not invent new action enums):

- `PrepareStage`, `CompleteStage`, `ApplyQualification` (from `OverviewAssembler` action constants and lifecycle transition codes).

---

## 4. Deliverables

### D1 — Pilot alias and canon registry (Markdown + optional CSV)

**Content:**

- One row per **pilot problem** with columns: hypothesized **canon**, **semantic level** (Gate / Signal / Attention), **candidate `blocksAction`**, and every **observed wire** variant (R1 disabled, R3 issue, POST code, `reasons[]`, readiness aggregate if applicable).
- Explicit **equivalence** column: `same rule` | `presentation aggregate` | `divergent — see notes` (from VERIFY).
- **V1 row:** document POST alias `Application.StageCompletionBlocked` + `OpenMatches` reason ↔ canon `OpenMatches`.
- **V2 row:** document three layers (issue `CycleDetected`, lifecycle `StructureGraphInvalid`, POST `Application.StructureCycleDetected`) — **requires product choice** (see §5.1).
- **V3 row:** source `QualificationPending`; no POST alias required for Step 3 unless a command refusal is identified in audit.

**Acceptance:** No alias marked `same rule` without pointer to VERIFY observation or test reference.

### D2 — Conceptual read-model enrichment (per family)

For **R1, R2, R3** separately, a short spec describing **conceptual** additions (names illustrative until wire DECIDED):

| Concept | Description |
| :------ | :---------- |
| `canonCode` | Stable fact id (pilot trio only) |
| `semanticLevel` | Gate \| Signal \| Attention \| Information (Step 2 definitions) |
| `blocksAction` | Zero or more stable action codes; empty = no action gate in this contract sense |
| `transportCode` | Actual string on this DTO today (may differ from canon — alias) |
| `sourceKind` | e.g. `anticipated` \| `attention` \| `issueFact` \| `aggregate` |

**Acceptance:** Spec states **compatibility**: existing string fields remain; enrichment is additive or documented mapping only — no breaking wire change in the proposal without a flagged migration section.

### D3 — Semantic assignment sheet (testable phrases)

One **testable phrase** per pilot row, e.g.:

- `OpenMatches` blocks `CompleteStage`.
- `CycleDetected` blocks `PrepareStage` when prepare gate policy applies (canon on hub; lifecycle may show aggregate disabled code).
- `QualificationPending` expects organizer intervention; `ApplyQualification` is the projected remediation (not necessarily a `blocksAction` gate).

**Acceptance:** Phrases align with Step 2 definitions; drawer inclusion **not** implied.

### D4 — Open decisions log

Track resolution of:

| ID | Topic | Owner | Blocks |
| :- | :---- | :---- | :----- |
| O1 | V2: one problem vs presentation layers for cycle prepare | Product + tech | Final alias row for `CycleDetected` |
| O2 | V3: `blocksAction` for qualification attention | Product | D2 row for R2 |
| O3 | Wire shape (enum vs dual fields) | Product + tech lead | Implementation proposal |

### D5 — Implementation proposal (appendix — proposal only)

After D1–D4 approved:

- Ordered list of **DTOs** and **fields** to add or map (max: lifecycle transition DTO, attention item DTO, optional issue detail on hub).
- Explicit **non-touch**: `ProblemDetails`, drawer SPA, Matchs pilot, Overview situation assembly.
- Test plan outline (Application read tests + existing Host contract tests to extend).

**This appendix is not executed** until a separate sign-off (product + tech) after brief approval.

---

## 5. Product decisions required during Step 3 doc work

### 5.1 V2 — Cycle prepare (blocking D1 completion)

Choose **one** documentation strategy (product DECIDED):

- **Option A — Single problem, multiple aliases:** One canon `CycleDetected`; lifecycle `StructureGraphInvalid` and POST `Application.StructureCycleDetected` documented as **presentation/transport aliases** for the same prepare gate when a graph gate issue is present.
- **Option B — Layered model:** Canon `CycleDetected` on hub; **aggregate disabled** `StructureGraphInvalid` documented as a **read projection**; POST code documented as **command transport** — SPA must not assume one string everywhere.

Neither option authorizes code change by itself; both must match VERIFY observations in D1.

### 5.2 V3 — Qualification attention

Choose whether `QualificationPending` carries:

- **Option A:** `semanticLevel: Attention` only; `actionCode: ApplyQualification` as remediation hint (current Overview mapping).
- **Option B:** Explicit `blocksAction` listing actions that are unsafe or unavailable until qualification is applied (requires engineering audit of ApplyQualification preconditions).

---

## 6. Work plan (documentation phase)

| Step | Activity | Output |
| :--- | :------- | :----- |
| 1 | Review Step 2 + VERIFY report with product | O1/O2/O3 logged |
| 2 | Draft D1 alias table (pilot trio) | MR-ready Markdown in Git |
| 3 | Draft D2 conceptual enrichment (R1–R3) | Section in this brief or audit appendix |
| 4 | Draft D3 phrase sheet | Same |
| 5 | Resolve O1/O2 (product) | Update D1/D2 |
| 6 | Draft D5 implementation proposal | Appendix — **for approval only** |
| 7 | Brief review sign-off | Record date + approvers in Git header |

**Estimated effort (doc only):** 1–2 working sessions after O1/O2 resolution.

---

## 7. Exit gate (implementation)

Step 3 documentation phase is **complete** when:

- [ ] D1–D4 merged and linked from [organizer-problems-audit.md](./organizer-problems-audit.md)
- [ ] O1 and O2 **DECIDED** (or explicitly DEFERRED with owner + date)
- [ ] D5 approved as **proposal** (or explicitly deferred)
- [ ] Product + tech sign-off recorded (Notion decision link optional; Git commit message or brief header)
- [ ] `structure-problems-contract.md` remains **Paused** unless a **separate** decision includes Matchs in implementation scope

**Implementation start** requires a **new** explicit authorization referencing approved D5 — not this brief alone.

---

## 8. Non-goals (explicit)

- Universal organizer problem DTO or shared SPA error framework  
- Changing Needs attention drawer selection policy (V4 aligned — maintain intent)  
- i18n consolidation (`structure.problems`, `graph.issues`, `errors:*`, `enums.attentionSource`)  
- Reconciliation of Matchs column local pilot with transversal contract  
- Backend changes to Prepare/Complete/Qualification business rules  

---

## 9. Suggested Git layout after Step 3 doc merge

| Artifact | Location |
| :------- | :------- |
| This brief | `docs/guides/organizer-problems-step3-brief.md` |
| Alias registry (D1) | `docs/guides/organizer-problems-pilot-aliases.md` *(create when D1 drafted)* |
| Implementation proposal (D5) | `docs/guides/organizer-problems-implementation-proposal.md` *(create after brief approval)* |

Update audit **Recommended sequence** item 3 to point here when brief is approved.
