# G-OPS3 — Draw-refuse on composition change (spec draft)

| Field | Value |
|-------|--------|
| **Backlog ID** | G-OPS3 |
| **Reference case** | **R-Draw1** |
| **Contract status** | **Spec draft (§12)** — gate **deferred** 2026-10-15 ([decision](./g-ops3-arbitration-session.md)); R-Draw1 **negatives** signed; not Ready |
| **§11B** | **Not signed** by this document |
| **Scope** | G-OPS3 only — operational mutations that change **composition** (affectation / population) while a **published applied** draw is in force |

**Product policy anchor (signed §11A):** Operational edits remain possible in Ready without production (D2), but must not silently invalidate an **applied** draw outcome organizers rely on for placement and match production.

---

## Guardrails

1. **UI partial today** — [`drawUi.ts`](../../../src/PlayUp/web/src/pages/structure/drawUi.ts) gates **starting** draw workflows; **command refuse** on save is the gap (R-Draw1).
2. **Not D1** — Consumer feed guard (`ReadyConsumerWouldBreak`) is orthogonal; G-OPS3 is **draw integrity**, not downstream qual volume.
3. **Not D2 alone** — Attached matches already freeze many ops; this spec covers **draw applied without** necessarily having materialized matches yet.

---

## Question to decide

**When must a composition (affectation) mutation be refused because it would invalidate an applied draw?**

**Proposed answer (candidate contract):** Refuse `ReplaceStageAffectationAuthoring` (and later other composition mutators) on stage `S` when:

| Step | Predicate | Notes |
|------|-----------|--------|
| Draw state | `S` has a draw in **Applied** resolution state (per [`DrawAppliedState`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StageOverviewAssembler.cs) / domain draw status) | Cancelled draws ignored |
| Invalidation | Proposed affectation set is **not** consistent with draw-fixed placements / pool partition the applied resolution committed | **Open:** exact diff algorithm (entry set, pot membership, fixed slots) |
| Phase status | Typically **Ready** (operational path); Draft may use lighter UX-only gates until command parity | Align with G-OPS1 scope |

**Expected failure:** `409` with wire code TBD (new `Application.DrawCompositionConflict` or reuse draw family).

---

## Current runtime (gap)

| Layer | Behaviour today |
|-------|-----------------|
| SPA | Draw rules lock when execution badge Applied ([`areDrawRulesLockedByExecution`](../../../src/PlayUp/web/src/pages/structure/drawUi.ts)); composition dialog **not** uniformly blocked on save |
| Application | `ReplaceStageAffectationAuthoring` runs G-OPS1/2 only — **no** draw-refuse guard |
| Domain | Draw resolution records outcomes on stage; invalidation rules **not** centralized for affectation replace |

---

## Positive scenario (must refuse — target)

**Story:** Cup/Groups phase Ready, draw **Applied**, composition lists entries A,B,C,D fixed by draw; organizer removes C from affectation via Composition dialog.

**Expected after contract:**

- Save rejected before persist; no partial affectation write.
- Copy names draw state and remediation (cancel draw / revert applied / adjust composition within allowed delta — **product copy TBD**).

---

## Negative scenarios (must allow)

- Draw **Draft** or **Generated** but not Applied — composition may change subject to other gates (D1, D2).
- Draw **Cancelled** — no refuse.
- Mutation that preserves draw-respected entry set (pure reorder or cosmetic — if product defines one).

---

## Open arbitrations (decision maker)

| # | Topic | Options |
|---|--------|---------|
| **D3-1** | Invalidation witness | Strict entry-set equality vs pot/slot-level structural diff |
| **D3-2** | Commands in scope | Affectation only first vs qual/prog that indirectly changes effective pool |
| **D3-3** | Remediation | Error-only (slice) vs hub persistent issue + Structure SoT link (like G-OPS D1 consumers UX) |

---

## Decision (2026-10-15)

Gate deferred; **R-Draw1-N1..N3** contract tests signed. **R-Draw1 positive** pending **D3-1**. Full text: [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md).

## Acceptance criteria (when promoted to §12 Ready)

- [ ] **D3-1** signed + **R-Draw1** positive command test (refuse before persist).
- [ ] R-Draw1-N1..N3 remain green (non-regression).
- [ ] Wire code + HTTP 409 mapping explicit.
- [ ] SPA parity (composition save) per **D3-3** when product asks.
- [ ] §12 **Ready** after §11B or narrow exception.

---

## Related

- [phase-mutability-matrix.md](./phase-mutability-matrix.md) §10 R-Draw1, §12 G-OPS3
- [g-ops-ready-operational-mutations-and-d1.md](./g-ops-ready-operational-mutations-and-d1.md) — G-OPS1/2 (shipped); G-OPS3 listed under command/format layer
