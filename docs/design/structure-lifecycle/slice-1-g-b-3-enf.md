# SLICE-1 — G-B-3-ENF

| Field | Value |
|-------|--------|
| **Status** | **Closed** (livraison code + tests application/SPA) — DM 2026-10-15; revue Wave 2 2026-10-16 |
| **Slice ID** | `SLICE-1-G-B-3-ENF` |
| **Allowed backlog** | `G-B-3-ENF` only |
| **Contract** | G-B-3 — refuse pool **EntryId** set change when draw **Applied** ([§11C](./section-11c-addendum-clarification-arbitration.md) B §4.13, matrix §9.1.1) |
| **D3-1 witness** | **Strict set equality** on `EntryId` (DM validated 2026-10-15) |
| **Phase C** | **Permitted** for `G-B-3-ENF` only — no other ENF backlogs |

---

## 1. Périmètre produit

| In scope | Out of scope |
|----------|----------------|
| Refuse mutation when proposed affectation **EntryId set ≠** current set and an **Applied** draw applies (see **Draw scope** below) | **C-ENF-*** (obsolete consumption) |
| Single command: [`ReplaceStageAffectationAuthoring.Execute`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageAffectationAuthoring/ReplaceStageAffectationAuthoring.cs) | `G-B-1-ENF`, qual/prog, `ConfigureDrawInputs` |
| HTTP 409 + stable wire code + mutation footer (path A) | Global `OperationalMutationGuard`, domain draw rewrite |
| Tests §2 | Mandatory SPA disable-button parity |

### Draw scope (correction A — DM)

The gate evaluates **draw entities on the target stage** (`stage` passed to `Execute`):

- Consider only draws where `Status != Cancelled` (cancelled history ignored for this check — aligns **R-Draw1-N3**).
- **Applied** is determined **only** by [`DrawAppliedState.IsApplied(draw, stage)`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs) — the contract reference read model; **no** parallel SPA badge logic or local reinterpretation of Applied in the guard.
- Under **B-Q3a**, at most one non-cancelled draw is expected; the guard still iterates non-cancelled draws on the stage and refuses if **any** satisfies Applied and the set would change.

### Guard order (unchanged relative to today)

1. `StructureOperationalExitEditGuard` (D2)  
2. `StructureReadyConsumerFeedGate` (D1) when `competitionStages` provided  
3. **G-B-3 gate (new)** — set equality vs Applied draw (draw scope above)  
4. `stage.ReplaceAffectationAuthoringOperationally`

### D3-1 rule (set equality)

Let `current` = set of `EntryId` in `stage.AffectationAuthoring`, `proposed` = set from command.

| Condition | Result |
|-----------|--------|
| `proposed` **equals** `current` (same members) | **Allow** — **R-Draw1-N2** |
| `proposed` **≠** `current` and ∃ **non-cancelled** draw on stage with `DrawAppliedState.IsApplied(draw, stage)` | **Deny** — **R-Draw1+** |
| Draw **Published** not Applied, or **Cancelled** | **Allow** pool change (existing N1, N3) |

**Not** obsolete-first (C): mutation is **refused**, not allowed then marked obsolete.

---

## 2. Acceptance tests (Phase C)

File: [`ReplaceStageAffectationAuthoringGOps3Tests.cs`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs).

| ID | Scenario | Expected | Obligation |
|----|----------|----------|------------|
| **R-Draw1-N2** | Same `EntryId` set, Applied draw | Allow | **Required** (existing test) |
| **R-Draw1+ shrink** | Remove one entry, Applied draw | Deny before persist | **Required** (new) |
| **R-Draw1+ swap** | Replace entry A with B, **same count**, Applied draw | Deny before persist | **Required** (new) |
| **R-Draw1+ add** | Strict superset only | Deny | Optional if swap covered |
| **R-Draw1-N1** | Published not Applied, shrink | Allow | Regression |
| **R-Draw1-N3** | Cancelled draw, change pool | Allow | Regression |

Plus D2 regression: [`ReplaceStageAffectationAuthoringGOpsTests.cs`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOpsTests.cs).

**PR review:** no obsolete predicate, no `ApplyDraw` / Materialize gates.

---

## 3. Error and presentation

| Situation | Expected |
|-----------|----------|
| Different `EntryId` set, Applied draw | **HTTP 409**, stable `Application.*` code, copy in **composition mutation footer** (path A) |
| Same `EntryId` set | Success; no new error |
| D1 `ReadyConsumerWouldBreak` | Unchanged |
| D2 `StructureNotMutable` | Unchanged |
| C obsolete consumption | Out of scope — no Apply/Materialize block |
| Hub drawer | **Do not** map G-B-3 code as obsolescence diagnostic |

**G-B-3** = mutation **refused**. **C-ENF** = obsolete **consumption** (later slice). Do not conflate wire codes.

Generic `_unknown` footer on refuse = **incomplete slice**.

### Phase C deliverables (errors)

- New constant in [`ApplicationErrorCodes`](../../../src/PlayUp/MyClub.PlayUp.Application/Common/ApplicationErrorCodes.cs) (name TBD, e.g. `AppliedDrawPoolChangeBlocked`)
- `ApplicationFailureException` → [`PlayUpExceptionHandler`](../../../src/PlayUp/MyClub.PlayUp.Host/PlayUpExceptionHandler.cs) **409 Conflict**
- Map wire code in [`lifecycleMutationMessageKey.ts`](../../../src/PlayUp/web/src/pages/structure/problems/lifecycleMutationMessageKey.ts) → `structure.problems.lifecycle.*`
- i18n **en** + **fr**; [`structureProblems.test.ts`](../../../src/PlayUp/web/src/pages/structure/problems/structureProblems.test.ts) key parity
- Update [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md) §5 inventory

---

## 4. Transactional boundary

### Flow (single HTTP request)

`StageEndpoints` → `UseCaseExecutor.ReplaceStageAffectationAuthoringAsync`: load tracked `Stage` → `Execute` (guards + mutate) → single `SaveChangesAsync`. G-B-3 check and affectation replace share the same in-memory `Stage` before flush.

### Concurrency limitation accepted for SLICE-1 (correction B — DM)

**Concurrency limitation accepted for SLICE-1:** G-B-3 checks the Applied-draw state and the proposed `EntryId` set **within the current request flow**. It does **not** guarantee cross-request linearizability when concurrent operations affect the same stage. This residual risk is **accepted** for this slice; concurrency-control mechanisms are **out of scope** and require a **separately authorized** slice.

**Known risk (product):** without EF row version / lock on `Stage`, interleaved HTTP requests (e.g. `ApplyDraw` then `ReplaceStageAffectationAuthoring`) can still persist a pool change that the gate would refuse if evaluated after the other commit. **Partial guarantee only** — not general coherence under all concurrency.

**Follow-up:** Notion Task [suivi risque concurrence G-B-3](https://app.notion.com/p/3fa75fb92d10811080dfd19ee51973c5) — escalate if real incidents occur.

**Not in SLICE-1:** distributed lock, concurrency column, serializable transactions.

---

## 5. Authorization record

| Field | Value |
|-------|--------|
| **Matrix §11C.4** | **SLICE-1-G-B-3-ENF** — 2026-10-15 |
| **Allowed backlog IDs** | `G-B-3-ENF` only |
| **Out of scope** | `G-B-1-ENF`, all `C-ENF-*` |
| **Notion** | [Décision §11C](https://app.notion.com/p/3fa75fb92d1081e2b96cc7a987799e3f) — slice block updated |

---

## 6. Related Git / Notion

- [implementation-plan-post-11c.md](./implementation-plan-post-11c.md)  
- [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md)  
- [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4  

---

## 7. DM review checklist

- [x] D3-1: shrink + swap + N2  
- [x] Transactional §4: residual limit accepted (partial guarantee)  
- [x] Error matrix §3  
- [x] Draw scope correction A  
- [x] Concurrency wording correction B  
- [x] **Authorize** `SLICE-1-G-B-3-ENF` — 2026-10-15  
