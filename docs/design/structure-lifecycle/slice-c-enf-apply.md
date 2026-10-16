# SLICE-C-ENF-APPLY

| Field | Value |
|-------|--------|
| **Status** | **Authorized** (DM arbitration 2026-10-16) — **not Closed** until dedicated commit, Release tests, and §11C.4 review |
| **Slice ID** | `SLICE-C-ENF-APPLY` |
| **Backlog** | **C-ENF-Apply** (G-C-2) |
| **Depends on** | **SLICE-C-ENF-DETECT** **Closed** (`279b6235`) — shared predicate only |
| **Contract** | Refuse **consumption** via `ApplyDraw` when the **target** draw is obsolete per `DrawConsumptionConformance.IsObsoleteForConsumption` |

**Governance:** [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) (C-ENF Apply/Mat-Start) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4 · predicate SoT [slice-c-enf-detect.md](./slice-c-enf-detect.md).

---

## Product scope (ratified)

Apply **blocks** applying a published resolved draw whose resolution is **obsolete for consumption** on the **current** stage state. It does **not** change the Detect predicate, add W-rules checks, add drawer diagnostics, or gate Materialize / Start (**SLICE-C-ENF-MAT-START**).

| In scope | Out of scope |
|----------|----------------|
| `StructureDrawConsumptionGate.EnsureDrawConsumable(draw, stage)` on **`ApplyDraw.Execute`** (Slot + Group paths), **after** existing `EnsureApplicable` preflights, **before** any placement mutation | `MaterializeMatches`, `MaterializeCupFromOccupiedSlots`, **Start** |
| `PublishAndApplyDraw` covered **only** via shared `ApplyDraw.Execute` (no duplicate gate in orchestration) | Read-side Materialize / Start disabled reasons (Mat-Start slice) |
| Wire `Application.DrawObsoleteForConsumption` · Host **409** · i18n EN/FR · `lifecycleMutationMessageKey` | **W-rules** · new overview fields · action hub behaviour beyond mutation failure |
| Application + Host tests including **R-Inv-1** Apply leg (allowed mutation → obsolete → Apply denied) | G-B-3 refuse-mutation proofs · predicate edits |

**L3 / G-B-3:** Pool change with **Applied** draw remains **refused at mutation** (SLICE-1). Apply gates **consumption** when obsolescence follows an **allowed** mutation (e.g. **R-Inv-1**).

---

## Predicate (no change)

Use **`DrawConsumptionConformance.IsObsoleteForConsumption`** exactly as in Detect — witnesses, exclusions, and effective pool are defined in [slice-c-enf-detect.md](./slice-c-enf-detect.md) § Witness decisions.

**Reserve (DM):** The gate must run on **current** `stage` + **target** `draw` after standard Apply preflights (Published, Resolved, inputs present). It must **not** block a draw that is still consumable (`IsObsoleteForConsumption` → `false`), including **Applied** draws (exclusion → idempotent Apply path unchanged).

---

## Command contract

| Item | Rule |
|------|------|
| **Failure** | `ApplicationFailureException` with `ApplicationErrorCodes.DrawObsoleteForConsumption` |
| **HTTP** | **409 Conflict** (same pattern as SLICE-3 Applied-draw feed rules) |
| **Message** | i18n `DrawObsoleteForConsumption` (structure mutation copy); FR + EN |
| **Gate placement** | `ApplyDraw.Execute` only for Apply slice; `StructureDrawConsumptionGate` internal helper acceptable if colocated in `Application/Stages` |

**Not in this slice:** `EnsureNoObsoleteDrawsOnStage` (stage-wide scan) — reserved for Mat-Start.

---

## Reference case **R-Inv-1** (Apply leg)

**Scenario (summary):** Allowed structural mutation on Draft (or other permitted path) breaks conformance of published draw **D**; **D** is obsolete for consumption; organizer must not **Apply** **D** as if still valid.

| Step | Expected |
|------|----------|
| Setup published + resolved **D**, not Applied | Detect / predicate `true` after allowed drift |
| `ApplyDraw` / HTTP Apply | **Deny** with `DrawObsoleteForConsumption` |
| Conformant **D** (predicate `false`) | Apply behaviour unchanged (including idempotent conform Apply per B-Q2) |

Materialize / Start legs of R-Inv-1 belong to **SLICE-C-ENF-MAT-START**.

---

## Required tests (Release) before **Closed**

| # | Layer | Scenario | Expected |
|---|--------|----------|----------|
| 1 | Application | Allowed mutation → obsolete target draw → `ApplyDraw.Execute` | throws `DrawObsoleteForConsumption` |
| 2 | Application | Conform published resolved draw (predicate `false`) | Apply succeeds as today |
| 3 | Application | Applied draw (predicate `false` by W-applied exclusion) | Apply idempotency / success path unchanged |
| 4 | Host | HTTP Apply when obsolete | **409** + problem code `Application.DrawObsoleteForConsumption` |
| 5 | Host | i18n key mapped for mutation surface | `DrawObsoleteForConsumption` resolvable |
| 6 | Regression | `PublishAndApplyDraw` when apply would fail obsolete | publish may persist; apply fails with same code (existing orchestration test pattern) |

Reuse or extend existing Apply / GOps test fixtures; do not duplicate full Detect predicate matrix — cite Detect tests for predicate truth.

---

## WIP prototype (not ratified as-is)

Stash `c-enf-apply-mat-gates-wip` and `.c-enf-gates-wip/StructureDrawConsumptionGate.cs` mix Apply with Mat/Start. **Apply delivery** must be a **clean diff**: gate helper + Apply hook + error wire + Apply-only tests. Pop stash **selectively** after this fiche is **Authorized** (done) and implementation is explicitly started.

---

## Closure criteria

**Closed** only when:

1. Diff contains **Apply scope only** (gate helper if new, `ApplyDraw`, error code, Host handler, i18n, tests).
2. Release tests § above green.
3. Gate timing verified (current state, target draw, no false block on consumable draws).
4. §11C.4 slice row updated; **G-C-INV** remains **Deferred** until **SLICE-C-ENF-MAT-START** is also **Closed** and §12 proof review passes.

**G-C-INV** becomes **Done** only after **both** Apply and Mat-Start are formally closed with read/command parity — not when this slice alone ships.

---

## Sequencing

**SLICE-C-ENF-MAT-START** stays **Pending** until this slice is **Closed**. No `stash pop` for Mat/Start portions until Mat-Start governance **Authorized**.
