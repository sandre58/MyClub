# SLICE-C-ENF-MAT-START (pending)

| Field | Value |
|-------|--------|
| **Status** | **Pending** — **not** **Authorized** until **SLICE-C-ENF-APPLY** is **Closed** (DM arbitration 2026-10-16) |
| **Slice ID** | `SLICE-C-ENF-MAT-START` |
| **Backlog** | **C-ENF-Mat** (G-C-3) + **C-ENF-Deps** (G-C-4) — Start consumption path |
| **Depends on** | **SLICE-C-ENF-DETECT** **Closed**; **SLICE-C-ENF-APPLY** **Closed** (sequential authorization) |
| **Contract (target)** | Deny **Materialize** (both commands) and **Start** when the phase would **consume** an obsolete draw resolution; **read/command parity** on hubs |

**Governance:** [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) · predicate SoT [slice-c-enf-detect.md](./slice-c-enf-detect.md).

---

## DM arbitration recorded (2026-10-16) — execution blocked

| Decision | Content |
|----------|---------|
| **Single slice** | G-C-3 + G-C-4 together — no third slice, no bundle with Apply |
| **Order** | Authorize and implement **after** Apply **Closed** |
| **Read parity** | **In scope** when authorized: Materialize hub (both materialize commands) and Start hub must surface disable reasons when obsolete draw blocks the command |
| **No new UX** | Reuse existing disabled-reason contracts — no new drawer diagnostic or badge |
| **G-C-INV** | **Done** only after **both** slices **Closed** + parity verified + §12 sync — until then **Deferred** |

This fiche will be expanded to **Authorized** (same detail level as Apply/Detect) in a **separate governance step** after Apply closure.

---

## Périmètre cible (when Authorized)

| In scope (proposal) | Out of scope |
|---------------------|--------------|
| `StructureDrawConsumptionGate.EnsureNoObsoleteDrawsOnStage(stage)` on `MaterializeMatches`, `MaterializeCupFromOccupiedSlots` | `ApplyDraw` (Apply slice) |
| Start command + **`TryGetDisabledReasonCode`** parity for obsolete draw | Detect predicate changes · W-rules |
| Assembler / hub tests for disabled reasons | Unrelated read parity gaps outside C-ENF |
| Wire `DrawObsoleteForConsumption` on command paths (reuse Apply slice code) | |

**Note** — Stash `c-enf-apply-mat-gates-wip` includes Mat/Start hooks **without** read parity; **not** ratified. Gate file `.c-enf-gates-wip/StructureDrawConsumptionGate.cs` must land under `Application/Stages` in delivery.

---

## Reference case **R-Inv-1** (Mat / Start legs)

When authorized, prove: after allowed mutation rendering draw **D** obsolete, **Materialize** (relevant format path) and **Start** (when consumption-dependent) are denied with explicit reason; hub read surfaces matching disabled codes where applicable.

---

## Closure criteria (preview)

**Closed** only when Mat-Start scope shipped, Release tests green, read/command parity verified, §11C.4 updated, and **G-C-INV** eligible for **Done** review together with Apply closure evidence.
