# SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT — G-OPS3 B1 — Cup slot placement

| Field | Value |
|-------|--------|
| **Status** | **Closed** (2026-10-16) — delivery [`c9029ba0`](https://github.com/sandre58/MyClub/commit/c9029ba0) ; governance `docs(structure-lifecycle): close SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT with G-OPS3 partial reserve` (Cup Assign/Clear vs Applied slots ; réserves R4/T7) |
| **Slice ID** | `SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT` |
| **Allowed backlog** | Reliquat **G-OPS3** — Cup `AssignEntryToSlot` / `ClearSlotAssignment` only |
| **Contract** | Refuse **conflicting** `AssignEntryToSlot` / `ClearSlotAssignment` when they would break a placement guaranteed by an **Applied** draw resolution (`SlotResults`); **not** a global Cup bracket lock |

**Governance:** [wave-b-programme-after-gel-a.md](./wave-b-programme-after-gel-a.md) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4 · §12 **G-OPS3** (remains **Partial**).

**Distinct slices (do not conflate):** [SLICE-1-G-B-3-ENF](./slice-1-g-b-3-enf.md) (pool replace) · [SLICE-3-G-OPS3-QUAL-PROG-ENF](./slice-3-g-ops3-qual-prog-enf.md) (qual/prog rules).

---

## 1. Product scope

| In scope | Out of scope |
|----------|----------------|
| Cup **Slot** draws only (`DrawResolutionKind.Slot`, `Resolution.SlotResults`) | **Groups** draw placement (separate arbitration; no auto-extension from B1) |
| [`AssignEntryToSlot.Execute`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/AssignEntryToSlot/AssignEntryToSlot.cs) | [`ReplaceStageAffectationAuthoring`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageAffectationAuthoring/ReplaceStageAffectationAuthoring.cs) — **G-B-3** / SLICE-1 |
| [`ClearSlotAssignment.Execute`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ClearSlotAssignment/ClearSlotAssignment.cs) | Global “lock all Cup slots in Ready” |
| Explicit **refuse** when clearing or changing a **protected** `(SlotKey, EntryId)` pair | Recovery journey (cancel draw, new draw, revert) — **not** decided by B1 |
| HTTP **409** + `Application.AppliedDrawSlotPlacementProtected` + user copy below | Promotion **G-OPS3** §12 → **Done** |
| Tests **T1–T7** (see §6) | Mandatory SPA disable-button parity (may follow in a separate UX slice) |

### Protected guarantee (what Applied resolution commits)

- **Yes:** each `(SlotKey, EntryId)` in the **Applied** draw’s `Resolution.SlotResults` that still matches [`DrawAppliedState.IsApplied`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs).
- **No:** bracket pairings as a separate artifact (confrontations remain **F1** / materialization rules).
- **No:** slots occupied but **not** listed in `SlotResults` for the Applied draw (mutations may still hit D2, F1, etc.).

### Clear behaviour (DM arbitration)

Today, domain [`ClearSlotAssignment`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) **no-ops** when there is no `DirectAssignment` even if the slot is occupied from **Apply** (`ApplyResolvedEntry`). **Target:** Application **refuses** with `AppliedDrawSlotPlacementProtected` when the clear would remove or break a protected pair — **before** that no-op path.

---

## 2. Provenance (A1–A3)

| Rule | Contract |
|------|----------|
| **A1 — Reference draw** | `Published` + `Resolved` + `DrawAppliedState.IsApplied(draw, stage)` |
| **A2 — Protected set** | Only `(SlotKey, EntryId)` pairs in `draw.Resolution.SlotResults` for a candidate draw |
| **A3 — Cannot evaluate** | Ambiguity or inconsistency does **not** by itself trigger a B1 refuse; other guards (F1, D2, G-B-3, C-ENF, integrity) still apply |

`IsApplied` is **whole-draw**: if any `SlotResults` placement mismatches stage occupancy, **no** candidate Applied draw exists for B1 (even if other slots still match the resolution on disk).

---

## 3. Reference draw and conflict (R0–R4)

### Candidates (R0)

```
candidates = { draw ∈ stage.Draws | Status ≠ Cancelled ∧ DrawAppliedState.IsApplied(draw, stage) }
```

| Rule | Contract |
|------|----------|
| **R1** | Under **B-Q3a** ([SLICE-2](./slice-2-g-b-1-enf.md)), at most one non-cancelled draw ⇒ **|candidates| ≤ 1** in conforming data |
| **R2** | **B1 conflict** on `slotKey` iff ∃ candidate with `slotKey ∈ SlotResults` and the proposed mutation **removes or changes** the guaranteed `EntryId` for that slot |
| **R3** | `|candidates| = 0` ⇒ **no** B1 refuse |
| **R4** | Multiple Applied candidates (corruption) ⇒ **no** blanket B1 deny; **R2** applies per identifiable protection; **diagnostic** (not B1 refuse) when protected pair identity cannot be established without product heuristics |

### Conflict witness (normative)

| Mutation | B1 |
|----------|-----|
| Clear protected slot | **Refuse** |
| Replace entry on protected slot | **Refuse** |
| Move entry leaving protected slot empty | **Refuse** |
| Re-assign **same** `EntryId` to **same** `slotKey` | **Allow** (no B1 conflict if guaranteed state unchanged) |
| Mutate slot **not** in `SlotResults` | **Allow** on B1 grounds alone |
| No Applied candidate (A1) | **Allow** on B1 grounds alone |

---

## 4. Guard placement and independence

### Commands (unchanged entry points)

`UseCaseExecutor.AssignEntryToSlotAsync` / `ClearSlotAssignmentAsync` → Application use cases → domain.

### Proposed guard order (B1 slice)

1. `StructureOperationalExitEditGuard` (D2) — **unchanged**
2. **B1 gate** — `StructureAppliedDrawSlotPlacementGate` on proposed `slotKey` + resulting occupancy
3. Domain `AssignEntryToSlot` / `ClearSlotAssignment` (F1 inside domain) — **unchanged**

**Clear:** B1 must run **before** domain no-op on draw-applied-only occupancy.

### No shared gate order with G-B-3

**G-B-3** runs only on [`ReplaceStageAffectationAuthoring.Execute`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageAffectationAuthoring/ReplaceStageAffectationAuthoring.cs). **B1** runs only on Assign/Clear. **Do not** impose a single ordering between B1 and G-B-3 — they are disjoint use cases.

**T8 (documentation):** shrink composition with Applied draw is a **G-B-3** scenario (`AppliedDrawPoolChangeBlocked`), not a B1 Assign/Clear test.

---

## 5. Errors and presentation (delivered)

| Situation | Delivered |
|-----------|-----------|
| B1 conflict | **HTTP 409**, `Application.AppliedDrawSlotPlacementProtected` |
| User copy (FR product) | *« Cette affectation est protégée par le tirage appliqué. Elle ne peut pas être modifiée directement. »* (EN/FR `structure.json` + `lifecycleMutationMessageKey.ts`) |
| Same pair re-assign | Success; no B1 error |
| D2 / F1 / G-B-3 | Unchanged — independent guards |

**Not delivered in this slice:** separate **read-side R4 diagnostic** wire or hub signal (mutation gate only). **Recovery copy** does not promise cancel/recreate draw. **Mandatory SPA disable-button** parity for placement (may follow elsewhere). **Do not** reuse `Application.AppliedDrawPoolChangeBlocked` (G-B-3).

Inventory: [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md).

---

## 6. Acceptance tests (Release — `c9029ba0`)

Application — [`CupSlotPlacementGOps3B1Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/CupSlotPlacementGOps3B1Tests.cs) (**9** tests):

| ID | Scenario (as exercised) | Expected |
|----|-------------------------|----------|
| **T1** | Applied · `ClearSlotAssignment` on protected slot | Deny · `AppliedDrawSlotPlacementProtected` |
| **T2** | Applied · `AssignEntryToSlot` changes protected entry | Deny · same code |
| **T3** | Applied · same `EntryId` on same slot | Allow |
| **T4** | Applied · slot ∉ `SlotResults` | Allow on B1 |
| **T5** | Draw resolved on slot, **not** Applied (`apply: false`) · assign/clear on resolution slot | Allow on B1 |
| **T6** | Occupancy drift → `IsApplied` false · clear on formerly protected slot | Allow on B1 |
| **T7** | Cup F1 path · B1 does not apply (`Assign_still_hits_f1_domain_gate_when_b1_does_not_apply`) | **F1** denies (`MaterializedOpponentConflict`) — not a combined Applied+F1-on-protected-slot proof |
| — | Canonical slot key (whitespace padding) | Aligns with `FindSlot` / gate |

Host — [`CupSlotPlacementGOps3B1EndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/CupSlotPlacementGOps3B1EndpointTests.cs) (**2** tests): `DELETE` and `PUT` assignment refuse with **409** + `Application.AppliedDrawSlotPlacementProtected`.

**T8** (composition shrink / **G-B-3**) — **out of scope** for B1; see SLICE-1.

---

## 7. Transactional boundary

Same pattern as SLICE-1: load tracked `Stage` (+ matches for D2/F1) → guards → mutate → single `SaveChangesAsync`. **Concurrency:** same accepted limitation as [slice-1 §4](./slice-1-g-b-3-enf.md).

---

## Réserves de clôture (DM 2026-10-16)

| Réserve | Contenu |
|---------|---------|
| **R4** | Defensive gate branch when multiple Applied candidates disagree on a slot — documented **unreachable** with valid domain data; **no** reflection test |
| **T7 (proof limit)** | Delivered T7 proves F1 when B1 does not apply; does **not** prove « Applied elsewhere + F1 on protected slot » in one mutation |

**§12 G-OPS3** reste **Partial** après clôture de cette slice (draw-rules sémantique, autres commandes composition, reliquats hors B1).

---

## Delivery proof (`c9029ba0`)

- Gate : [`StructureAppliedDrawSlotPlacementGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawSlotPlacementGate.cs)
- Commands : [`AssignEntryToSlot`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/AssignEntryToSlot/AssignEntryToSlot.cs) · [`ClearSlotAssignment`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ClearSlotAssignment/ClearSlotAssignment.cs) — order D2 → B1 → domain
- Wire / Host : `Application.AppliedDrawSlotPlacementProtected` · [`PlayUpExceptionHandler`](../../../src/PlayUp/MyClub.PlayUp.Host/PlayUpExceptionHandler.cs) **409**
- SPA : `lifecycleMutationMessageKey.ts` · EN/FR `structure.json` · `structureProblems.test.ts` (key + message)

**Not claimed:** Host mirror for every Application variant; full placement SPA disable parity; read-side R4 diagnostic.

---

## Closure criteria

**Closed** when:

1. Diff contains **B1** scope only. — **Done** (`c9029ba0`).
2. **T1–T7** Application + Host Assign/Clear refuse minimum. — **Done** (with reserves above).
3. Clear refuses **before** domain no-op on protected Applied slots. — **Done**.
4. Proof limits and §12 **G-OPS3 Partial** recorded. — **Done** (reserves above).
5. §11C.4 + matrix / wave B sync (governance commit). — **Done** (DM 2026-10-16).

**§12:** **G-OPS3** remains **Partial** — slice **Closed** ≠ backlog **Done**.

---

## Sequencing

**SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT** **Closed**. Reliquat **G-OPS3** (draw-rules, autres commandes) : arbitrage / livraisons **distinctes** de B1. **SLICE-3** · **SLICE-1** · **C-ENF** **Closed** — do not reopen without anomaly.

---

## 10. Code references

| Concern | Location |
|---------|----------|
| Applied predicate | [`DrawAppliedState.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs) |
| Slot results | `draw.Resolution.SlotResults` |
| Assign/Clear use cases | [`AssignEntryToSlot.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/AssignEntryToSlot/AssignEntryToSlot.cs), [`ClearSlotAssignment.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ClearSlotAssignment/ClearSlotAssignment.cs) |
| B1 gate | [`StructureAppliedDrawSlotPlacementGate.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawSlotPlacementGate.cs) |
| G-B-3 pattern (candidates) | [`StructureAppliedDrawPoolChangeGate.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawPoolChangeGate.cs) |
