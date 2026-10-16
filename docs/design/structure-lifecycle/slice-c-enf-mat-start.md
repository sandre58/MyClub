# SLICE-C-ENF-MAT-START

| Field | Value |
|-------|--------|
| **Status** | **Authorized** (DM arbitration 2026-10-16) — **not Closed** until dedicated commit, Release tests, read/command parity proof, and §11C.4 review |
| **Slice ID** | `SLICE-C-ENF-MAT-START` |
| **Backlog** | **C-ENF-Mat** (G-C-3) + **C-ENF-Deps** (G-C-4) — Start consumption path |
| **Depends on** | **SLICE-C-ENF-DETECT** **Closed** (`279b6235`); **SLICE-C-ENF-APPLY** **Closed** (`85360d3`) |
| **Contract** | Deny **Materialize** (both commands) and **Start** when the phase has a non-cancelled draw **obsolete for consumption**; **read/command parity** on structure hubs |

**Governance:** [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) (C-ENF Mat-Start) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4 · predicate SoT [slice-c-enf-detect.md](./slice-c-enf-detect.md) · Apply wire [slice-c-enf-apply.md](./slice-c-enf-apply.md).

---

## Product scope (ratified)

Mat-Start **blocks production paths** that would **consume** an obsolete draw resolution on the **current** stage. It does **not** change the Detect predicate, add W-rules, add drawer diagnostics, or re-open **Apply** (already **Closed**).

| In scope | Out of scope |
|----------|----------------|
| `StructureDrawConsumptionGate.EnsureNoObsoleteDrawsOnStage(stage)` on **`MaterializeMatches.Execute`** and **`MaterializeCupFromOccupiedSlots`** (after existing materialize preflights, before mutations) | `ApplyDraw` (Apply slice) |
| **`StageStartReadinessGuard`**: command `EnsureReadyToStart` + read **`TryGetDisabledReasonCode`** parity | Unrelated hub read gaps outside C-ENF |
| **Read parity:** `StructureMaterializeAssembler` disabled reason when materialize would be blocked by obsolete draw; Start lifecycle DTO disabled reason when Start would be blocked | New badges · drawer diagnostic codes · overview `isObsoleteForConsumption` behaviour change |
| Reuse **`Application.DrawObsoleteForConsumption`** on command failures (Host **409** already mapped in Apply slice) | **W-rules** · predicate edits |
| i18n for **read** disabled codes (`structure.json` materialize + lifecycle) aligned with existing copy for `DrawObsoleteForConsumption` where applicable | Global `stash pop` |

**Scan rule:** `EnsureNoObsoleteDrawsOnStage` evaluates every draw with `Status != Cancelled` via `EnsureDrawConsumable` → same predicate as Detect/Apply. **Applied** draws remain non-obsolete (W-applied exclusion).

---

## Predicate and gate (no change)

**`DrawConsumptionConformance.IsObsoleteForConsumption`** — [slice-c-enf-detect.md](./slice-c-enf-detect.md).

**`StructureDrawConsumptionGate`** — shipped in Apply (`85360d3`); this slice wires **`EnsureNoObsoleteDrawsOnStage`** only.

---

## Command contract

| Path | Placement (target) | Failure |
|------|-------------------|---------|
| `MaterializeMatches` | After `EnsureMutable` / format preflights, before format-specific materialization | `DrawObsoleteForConsumption` |
| `MaterializeCupFromOccupiedSlots` | After `StageMaterializeGuard` + execution/attribution gates, before cup materialization | same |
| `Start` (`EnsureReadyToStart`) | After existing unmaterialized confrontation check, before return | same |
| HTTP | Existing `PlayUpExceptionHandler` **409** mapping (Apply slice) | same wire code |

**Read contract (parity):**

| Surface | Mechanism |
|---------|-----------|
| Materialize hub | Add causal code under [`StructureMaterializeDisabledReasons`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureMaterializeDisabledReasons.cs) (e.g. `DrawObsoleteForConsumption`) in [`StructureMaterializeAssembler.ResolveDisabledReason`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureMaterializeAssembler.cs) when stage-wide obsolete scan would fail |
| Start hub | Extend [`StageStartReadinessGuard.TryGetDisabledReasonCode`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StageStartReadinessGuard.cs) with the **same** stable code string the UI already maps for lifecycle (reuse `DrawObsoleteForConsumption` / `problems.lifecycle` copy — no new drawer code) |

**Priority:** Obsolete draw block should surface when it would block the command, consistent with other disabled reasons (document order in implementation; prefer obsolete before generic “eligible” materialize idle when both apply).

---

## Reference cases

### **R-Inv-1** (Mat / Start legs)

After **allowed** mutation → published draw **D** obsolete (`IsObsoleteForConsumption` → `true`):

| Step | Expected |
|------|----------|
| `MaterializeMatches` or `MaterializeCupFromOccupiedSlots` (relevant format) | **Deny** — `DrawObsoleteForConsumption` |
| `Start` when consumption-dependent | **Deny** — same code |
| Structure read (materialize + start actions) | **Disabled** with matching causal code (parity) |

Apply leg — **Closed** (`85360d3`).

### **R-Mat-block** (obsolete draw on materialize path)

Align §10 / §12: explicit materialize denial when obsolete draw is on the consumption path — not only roster/groups gates.

---

## Required tests (Release) before **Closed**

| # | Layer | Scenario | Expected |
|---|--------|----------|----------|
| 1 | Application | Obsolete draw on stage → `MaterializeMatches` or Cup path | `DrawObsoleteForConsumption` |
| 2 | Application | Obsolete draw → `EnsureReadyToStart` | `DrawObsoleteForConsumption` |
| 3 | Application | Conform stage (no obsolete non-cancelled draw) | Materialize/Start unchanged |
| 4 | Reads | `StructureMaterializeAssembler` when obsolete | `DisabledReason` = materialize code |
| 5 | Reads | Start lifecycle assembler when obsolete | `DisabledReasonCode` matches Start guard |
| 6 | Host | HTTP Materialize / Start when obsolete | **409** + `Application.DrawObsoleteForConsumption` |
| 7 | Regression | Read disabled ⇔ command would fail (representative format: Groups + Cup) | assembler tests |

Reuse fixtures from **DrawConsumptionConformanceTests** / Apply R-Inv-1 setup; do not duplicate full predicate matrix.

---

## WIP prototype (not ratified as-is)

Stash `c-enf-apply-mat-gates-wip` contains **command-only** hooks for Materialize/Start **without** read parity. **Delivery** = selective extraction (Mat/Start commands + assemblers + i18n + tests) — **no** `stash pop`, **no** Apply-draw changes. `.c-enf-gates-wip/` is redundant with committed `StructureDrawConsumptionGate.cs`.

---

## Closure criteria

**Closed** only when:

1. Diff contains **Mat-Start scope only** (materialize + start commands, read assemblers/guard, i18n read copy, tests).
2. Release tests § above green.
3. Read/command parity verified for Materialize and Start hubs.
4. §11C.4 updated; **G-C-INV** reviewed for **Done** (Apply + Mat-Start both **Closed**, parity proof, §12 sync).

Until Mat-Start **Closed**, **G-C-INV** remains **Deferred**.

---

## Sequencing

**SLICE-C-ENF-APPLY** **Closed** — prerequisite met. Implementation authorized; **no** code until executor starts slice delivery after this governance commit.
