# SLICE-C-ENF-MAT-START

| Field | Value |
|-------|--------|
| **Status** | **Closed** (2026-10-16) — delivery commit [`ee0fc2e6`](https://github.com/sandre58/MyClub/commit/ee0fc2e6) (Materialize + Start gates + read parity) |
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

## Read/command parity (verified)

| Surface | Command | Read | Shared condition |
|---------|---------|------|------------------|
| **Materialize** | `EnsureNoObsoleteDrawsOnStage` on both materialize use cases | `StructureDrawConsumptionGate.StageHasObsoleteDrawForConsumption` in `Pack` + `ResolveDisabledReason` | Same stage-wide scan + `IsObsoleteForConsumption` per non-cancelled draw |
| **Start** | `EnsureNoObsoleteDrawsOnStage` after unmaterialized check | `StageHasObsoleteDrawForConsumption` after same unmaterialized check in `TryGetDisabledReasonCode` | Same ordering; read code `DrawObsoleteForConsumption` ↔ wire `Application.DrawObsoleteForConsumption` |

`EnsureNoObsoleteDrawsOnStage` delegates to `StageHasObsoleteDrawForConsumption` before throw — not a second predicate.

---

## Required tests (Release) — proof (`ee0fc2e6`)

| # | Layer | Scenario | Expected | Proof |
|---|--------|----------|----------|--------|
| 1 | Application | Obsolete → `MaterializeMatches` | `DrawObsoleteForConsumption` | `MaterializeMatches_R_Inv_1_*` |
| 1b | Application | Obsolete → Cup materialize | same | `MaterializeCupFromOccupiedSlots_denies_*` |
| 2 | Application | Obsolete → `EnsureReadyToStart` | same | `Start_R_Inv_1_*` |
| 3 | Application | Conform paths | unchanged | `StartStageReadinessTests` (5) + existing materialize suites (no regression in targeted run) |
| 4 | Reads | Materialize hub disabled reason | `StructureMaterializeDisabledReasons.DrawObsoleteForConsumption` | `Read_materialize_disabled_reason_*` |
| 5 | Reads | Start hub disabled reason | `StructureStageLifecycleAssembler.DisabledDrawObsoleteForConsumption` | `Read_start_disabled_reason_*` |
| 6a | Host | HTTP **Materialize** when obsolete | **409** + wire code | `CEnfMatStartEndpointTests.Materialize_when_draw_obsolete_*` |
| 6b | Host | HTTP **Start** when obsolete | **409** (expected) | **Not tested** — see coverage limit below |
| 7 | Parity | Groups + Cup representative formats | command/read aligned | rows 1–5 |

**Coverage limit (explicit):** Start **command** and **read** parity are proven in Application only; **HTTP Start** with `DrawObsoleteForConsumption` was **not** exercised in Host integration tests.

---

## Delivery proof (`ee0fc2e6`)

- [`StructureDrawConsumptionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureDrawConsumptionGate.cs) — `StageHasObsoleteDrawForConsumption`
- [`MaterializeMatches`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeMatches/MaterializeMatches.cs) · [`MaterializeCupFromOccupiedSlots`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs)
- [`StageStartReadinessGuard`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StageStartReadinessGuard.cs) · [`StructureMaterializeAssembler`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureMaterializeAssembler.cs)
- [`DrawConsumptionMatStartTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/DrawConsumptionMatStartTests.cs) · [`CEnfMatStartEndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/CEnfMatStartEndpointTests.cs)

Stash `c-enf-apply-mat-gates-wip` / `.c-enf-gates-wip/` — **not** applied; prototype superseded by committed delivery.

---

## Closure criteria

**Closed** when:

1. Diff Mat-Start scope only. — **Done** (`ee0fc2e6`).
2. Release tests § above green. — **Done** (5 Mat-Start + 5 `StartStageReadinessTests` in targeted Release run).
3. Read/command parity verified. — **Done** (table above + tests #4–5).
4. §11C.4 + **G-C-INV** §12 sync. — **Done** (governance commit with Mat-Start closure).

**G-C-INV** **Done** — C-ENF consumption gates shipped across Detect + Apply + Mat-Start (W-rules / drawer out of scope per slices).

---

## Sequencing

All three **SLICE-C-ENF-*** slices **Closed**. Stash remains optional cleanup — not required for **G-C-INV**.
