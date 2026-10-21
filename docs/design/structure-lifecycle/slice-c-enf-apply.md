# SLICE-C-ENF-APPLY

| Field | Value |
|-------|--------|
| **Status** | **Closed** (2026-10-16) — delivery commit [`85360d3`](https://github.com/sandre58/MyClub/commit/85360d3) (Apply gate + wire + tests) |
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
| **Gate placement** | After slot/group **preflight** validation, **before** placement mutations (`ApplySlot` / `ApplyGroup`); shared `ApplyDraw.Execute` path covers `PublishAndApplyDraw` |

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

## Required tests (Release) — proof (`85360d3`)

| # | Layer | Scenario | Expected | Proof |
|---|--------|----------|----------|--------|
| 1 | Application | Allowed mutation → obsolete → `ApplyDraw.Execute` | `DrawObsoleteForConsumption` | `Execute_R_Inv_1_denies_apply_when_draw_obsolete_after_allowed_pool_shrink` |
| 2 | Application | Conform published resolved draw | Apply succeeds | `ApplyDrawTests` (24/24 Release) |
| 3 | Application | Applied draw / idempotent conform Apply | unchanged | `Execute_slot_idempotent_when_already_conform` + `Applied_draw_is_not_obsolete` (Detect) |
| 4 | Host | HTTP Apply when obsolete | **409** + wire code | `Apply_when_draw_obsolete_for_consumption_returns_409_DrawObsoleteForConsumptionAsync` |
| 5 | Host / SPA | i18n + mutation key | `DrawObsoleteForConsumption` | `lifecycleMutationMessageKey.ts` + `structure.json` EN/FR in `85360d3` |
| 6 | Orchestration | `PublishAndApplyDraw` on obsolete | same code via `ApplyDraw.Execute` | **By shared use case** — no dedicated obsolete orchestration test (same path as #1) |

Predicate matrix: [slice-c-enf-detect.md](./slice-c-enf-detect.md) § Required tests.

---

## Delivery proof (`85360d3`)

- [`StructureDrawConsumptionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureDrawConsumptionGate.cs)
- [`ApplyDraw`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ApplyDraw/ApplyDraw.cs) — `EnsureDrawConsumable` after preflights
- Host **409** · i18n · [`ApplyDrawTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ApplyDrawTests.cs) · [`ApplyDrawEndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/ApplyDrawEndpointTests.cs)

**WIP:** Stash `c-enf-apply-mat-gates-wip` / `.c-enf-gates-wip/` — obsolete prototype; C-ENF shipped in Git (see [mat-start](./slice-c-enf-mat-start.md)).

---

## Closure criteria

**Closed** when:

1. Diff contains **Apply scope only**. — **Done** (`85360d3`).
2. Release tests § above green. — **Done** (filters above + full `ApplyDrawTests`).
3. Gate timing verified (preflights then consumption check; no false block on consumable draws). — **Done** (DM reserve accepted 2026-10-16).
4. §11C.4 updated at Apply closure. — **Done** (2026-10-16).

**G-C-INV** §12 **Done** after [Mat-Start](./slice-c-enf-mat-start.md) **Closed** (`ee0fc2e6`).

---

## Sequencing

**SLICE-C-ENF-APPLY** **Closed**. **SLICE-C-ENF-MAT-START** **Closed** (`ee0fc2e6`); **G-C-INV** **Done**.
