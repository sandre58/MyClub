# SLICE (draft) — G-OPS3 B1 — Cup slot placement

| Field | Value |
|-------|--------|
| **Status** | **Draft** — product contract **closed** (DM arbitration 2026-10-16); **implementation not authorized**; **no** §11C.4 slice record |
| **Proposed slice ID** | `SLICE-G-OPS3-B1-CUP-SLOT-PLACEMENT` (TBD at §11C.4 authorization) |
| **Allowed backlog (when authorized)** | Reliquat **G-OPS3** — Cup manual placement only |
| **Contract** | Refuse **conflicting** `AssignEntryToSlot` / `ClearSlotAssignment` when they would break a placement guaranteed by an **Applied** draw resolution (`SlotResults`); **not** a global Cup bracket lock |

**Governance:** [wave-b-programme-after-gel-a.md](./wave-b-programme-after-gel-a.md) (piste B) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §12 **G-OPS3** (**Partial** — unchanged by this fiche) · [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md).

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
2. **B1 gate (new)** — `StructureAppliedDrawSlotPlacementGate` (name TBD) on proposed `slotKey` + resulting occupancy
3. Domain `AssignEntryToSlot` / `ClearSlotAssignment` (F1 inside domain) — **unchanged**

**Clear:** B1 must run **before** domain no-op on draw-applied-only occupancy.

### No shared gate order with G-B-3

**G-B-3** runs only on [`ReplaceStageAffectationAuthoring.Execute`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageAffectationAuthoring/ReplaceStageAffectationAuthoring.cs). **B1** runs only on Assign/Clear. **Do not** impose a single ordering between B1 and G-B-3 — they are disjoint use cases.

**T8 (documentation):** shrink composition with Applied draw is a **G-B-3** scenario (`AppliedDrawPoolChangeBlocked`), not a B1 Assign/Clear test.

---

## 5. Errors and presentation

| Situation | Target |
|-----------|--------|
| B1 conflict | **HTTP 409**, `Application.AppliedDrawSlotPlacementProtected` |
| User copy (FR product) | *« Cette affectation est protégée par le tirage appliqué. Elle ne peut pas être modifiée directement. »* |
| Same pair re-assign | Success; no B1 error |
| D2 `StructureNotMutable` | Unchanged |
| F1 / cup coherence | Unchanged (independent of B1) |
| G-B-3 pool | Unchanged — different command |

### Open before implementation (not closed in arbitration)

| Topic | Note |
|-------|------|
| **R4 diagnostic** | Wire code, surface (read vs mutation), link to B-Q3a integrity — **TBD in implementation slice** |
| **SPA / hub** | Map wire code in placement mutation footer (see [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md)); i18n EN + FR; extend inventory §5 |
| **Constants** | Add `AppliedDrawSlotPlacementProtected` in [`ApplicationErrorCodes`](../../../src/PlayUp/MyClub.PlayUp.Application/Common/ApplicationErrorCodes.cs); map in [`PlayUpExceptionHandler`](../../../src/PlayUp/MyClub.PlayUp.Host/PlayUpExceptionHandler.cs) |
| **Recovery copy** | Do **not** promise cancel/recreate draw in error text until product validates D3-3 path |

**Do not** reuse `Application.AppliedDrawPoolChangeBlocked` (G-B-3 semantics).

---

## 6. Acceptance tests (planned — not implemented)

Suggested files: `AssignEntryToSlotGOps3B1Tests.cs`, `ClearSlotAssignmentGOps3B1Tests.cs`; Host mirrors for at least one Assign refuse and one Clear refuse.

| ID | Scenario | Expected |
|----|----------|----------|
| **T1** | Applied draw, `ClearSlotAssignment` on `slotKey ∈ SlotResults` | Deny, `AppliedDrawSlotPlacementProtected` |
| **T2** | Applied, `AssignEntryToSlot` different entry on protected slot | Deny, same code |
| **T3** | Applied, assign same `EntryId` to same slot | Allow |
| **T4** | Applied, mutate slot ∉ `SlotResults` | Allow on B1 (F1 may still deny) |
| **T5** | Published resolved, **not** Applied, clear slot in resolution | Allow on B1 |
| **T6** | Drift: `IsApplied` false, slot still matches resolution visually | Allow on B1 |
| **T7** | Applied, F1 violation on non-protected slot | F1 denies independently |
| **T8** | Applied, composition shrink | **G-B-3** only — `AppliedDrawPoolChangeBlocked` on affectation replace |

---

## 7. Transactional boundary (expected)

Same pattern as SLICE-1: load tracked `Stage` (+ matches for D2/F1) → guards → mutate → single `SaveChangesAsync`. **Concurrency:** same accepted limitation as [slice-1 §4](./slice-1-g-b-3-enf.md) unless a separate slice authorizes stronger guarantees.

---

## 8. Authorization record

| Field | Value |
|-------|--------|
| **§11C.4** | **Not authorized** — this document is **contract + delivery spec draft** only |
| **§12 G-OPS3** | Remains **Partial** until a future DM closes reliquat or promotes backlog |
| **Implementation** | Requires explicit DM + new §11C.4 row + governance close commit |
| **Arbitration closed** | R0–R4, A1–A3, conflict witness, wire code name, HTTP 409, user message |

---

## 9. Closure criteria (when implementation is authorized)

**Closed** when:

1. Diff contains **B1** scope only (Assign/Clear gate + tests + error/presentation items agreed in §5).
2. **T1–T7** green in Release (Host proof for Assign + Clear refuse at minimum).
3. Clear refuses **before** domain no-op on protected draw-applied slots.
4. Fiche updated to **Closed** + §11C.4 row added; **G-OPS3** §12 status per DM (likely still **Partial** if other reliquat remains).
5. No conflation with G-B-3 wire codes or C-ENF obsolete consumption.

---

## 10. Code references (SoT today)

| Concern | Location |
|---------|----------|
| Applied predicate | [`DrawAppliedState.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs) |
| Slot results | `draw.Resolution.SlotResults` |
| Assign/Clear use cases | [`AssignEntryToSlot.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/AssignEntryToSlot/AssignEntryToSlot.cs), [`ClearSlotAssignment.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ClearSlotAssignment/ClearSlotAssignment.cs) |
| G-B-3 pattern (candidates) | [`StructureAppliedDrawPoolChangeGate.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawPoolChangeGate.cs) |
