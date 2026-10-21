# SLICE-C-ENF-DETECT

| Field | Value |
|-------|--------|
| **Status** | **Closed** (2026-10-16) — delivery commit [`279b6235`](https://github.com/sandre58/MyClub/commit/279b6235) (predicate + overview/DTO/SPA + tests; no consumption gates) |
| **Slice ID** | `SLICE-C-ENF-DETECT` |
| **Backlog** | **C-ENF-Detect** (G-C-1) |
| **Contract** | Derived predicate `IsObsoleteForConsumption` (W-inputs + W-pool; explicit exclusions) + read surfacing only |

**Governance:** [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) (Detect section) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4.

---

## Product scope (ratified)

Detect **identifies** draw resolution drift **before consumption**. It does **not** refuse mutations (G-B-3, G-OPS3), compare draw-rules history (W-rules), block Apply/Materialize/Start, add drawer diagnostics, or change action behaviour.

| In scope | Out of scope |
|----------|----------------|
| Single named predicate `IsObsoleteForConsumption(draw, stage)` | `StructureDrawConsumptionGate` / consumption gates |
| `StageDrawDto.isObsoleteForConsumption` + SPA type field | `Application.DrawObsoleteForConsumption` wire on mutations (Apply slice) |
| Tests proving detection + overview alignment | G-B-3 mutation-refuse tests as Detect proof |
| | **W-rules** comparison (explicit exclusion — see below) |
| | **SLICE-C-ENF-APPLY** (consumption gate), **SLICE-C-ENF-MAT-START** |

**L3:** Obsolescence after **allowed** mutation (e.g. R-Inv-1). **G-B-3** still **refuses** pool change when draw is **Applied** — Detect does not replace that.

---

## Witness decisions (C4)

| Witness | Detect | Rule |
|---------|--------|------|
| **W-inputs** | **In** | Resolved entries must stay compatible with current composition and fixed structure (inputs + fixed slot/group references). |
| **W-pool** | **In** | Every **resolved** entry must belong to the **effective pool** (see below). |
| **Empty pool** | **In, no exception** | Effective pool may be empty; any non-empty resolved placement set then fails conformance → obsolete. |
| **W-rules** | **Out** | No compare of rules-at-generate vs current regulation in this slice. Do not add implicitly to the predicate. |
| **W-applied** | **In (exclusion)** | If draw is **Applied** (`DrawAppliedState`) → `IsObsoleteForConsumption` is **false** (already consumed). |
| **Draft / Cancelled / NoSolution** | **Out of calculation** | `false`; no extra diagnostic in this slice. |
| **Published, not Resolved** | **Out of calculation** | `false` — not yet in consumable resolution state. |
| **Published + Resolved + not Applied** | **In** | Evaluate conformance; any included witness violation → **true**. |

### Effective pool

**Intersection** of current affectation authoring `EntryId` set and current composition `EntryId` set when affectation is non-empty. When affectation is **empty**, the effective pool is the composition set (runtime population from Qual/Prog without duplicate authoring). Membership in only one side is **not** sufficient once affectation is authored.

### Other product rules

- **EntryId order** in lists does not matter; set equality only.
- **Fixed structure:** missing slot or group referenced by the draw → non-conformance.
- **Resolved vs inputs:** resolved entry not in `Draw.Inputs.Entries` → non-conformance (W-inputs).

---

## Read contract

- **Derived, recomputed on read** — no persisted obsolete flag at this stage (C4 storage remains open).
- **`isApplied`** — consumption / occupancy (`DrawAppliedState`).
- **`isObsoleteForConsumption`** — pre-consumption conformance (this slice).
- Expose on stage draw DTO and propagate to SPA `StageDraw` type.
- **No** new badges, drawer diagnostic changes, or action enable/disable in Detect.

Downstream slices (Apply, Mat-Start) must call the **same** `IsObsoleteForConsumption` implementation — not a second definition in overview vs gates.

---

## Required tests (Release) before **Closed**

| # | Scenario | Expected | Application test (integration) |
|---|----------|----------|--------------------------------|
| 1 | Published + Resolved, not Applied; allowed pool shrink makes resolution incompatible | `true` | `Published_resolved_draw_obsolete_when_effective_pool_shrinks_after_allowed_mutation` |
| 2 | Resolved entry ∉ `Inputs.Entries` (product rule) | `true` at read | **Domain proof:** [`RecordResolution_rejects_entry_outside_pool`](../../../tests/MyClub.PlayUp.Domain.Tests/Stages/StageDrawTests.cs). **Read:** defensive loop in predicate; **not** a separate integration test — Published+Resolved+missing input is **not constructible** via `PublishDraw` / `RecordDrawResolution`. **Happy path:** `Published_resolved_when_inputs_pool_and_resolution_align_is_not_obsolete`. |
| 3 | Fixed slot or group removed from stage | `true` | `Removed_fixed_slot_reference_is_obsolete` |
| 4 | Empty effective pool, non-empty resolution | `true` | `Empty_effective_pool_with_resolved_entries_is_obsolete` |
| 5 | Same `EntryId` set, reorder only | `false` | `Same_entry_set_reordered_in_affectation_is_not_obsolete` |
| 6 | Applied draw | `false` | `Applied_draw_is_not_obsolete` |
| 7 | Non-consumable states (Draft, Cancelled, NoSolution) | `false` | `Draft_draw_*`, `Cancelled_draw_*`, `NoSolution_draw_*`, `Resolved_draft_draw_*` |
| 7b | **Published** ∧ resolution **not** `Resolved` | `false` | **Not integration-tested** — see § Test constructibility below |
| 8 | Overview DTO matches predicate for representative cases | exact | `Assemble_draw_obsolete_flag_matches_IsObsoleteForConsumption_predicate` |
| 9 | Dedicated test project run in **Release** | pass | Filter `DrawConsumptionConformanceTests` + overview obsolete test |

G-B-3 refuse-mutation coverage stays in **GOps3 / affectation** suites — not substituted as Detect proof.

### Test constructibility (no reflection tests in Detect)

**A — `resolution ⊆ Inputs.Entries`**

- **Product rule (unchanged):** any resolved entry not listed in `Draw.Inputs.Entries` ⇒ obsolete for consumption when the predicate runs on Published + Resolved + not Applied.
- **Domain:** inconsistent resolution vs inputs is **rejected at record** (`StageDrawTests.RecordResolution_rejects_entry_outside_pool`); `Publish()` also requires non-null inputs and `Resolution == Resolved`.
- **Detect slice:** keep the defensive check in `IsObsoleteForConsumption`; prove alignment with `Published_resolved_when_inputs_pool_and_resolution_align_is_not_obsolete` and pool/structure drift tests. **Do not** use reflection to simulate corrupt Published draws.

**B — `Published` ∧ not `Resolved`**

- **Product rule:** predicate returns `false` (not yet consumable).
- **Domain:** `Draw.Publish()` requires `Resolution.State == Resolved`, so **Published + not Resolved** is **not reachable** through `PublishDraw`.
- **`Resolved_draft_draw_is_not_obsolete`** exercises **Draft** + Resolved (early exit on status), **not** this branch.
- **Detect slice:** document the unreachable combination; **no** reflection test and **no** extra helper extracted solely for branch coverage.

---

## Closure criteria

**Closed** when:

1. Diff contains **Detect scope only** (predicate, overview/DTO, SPA type, tests). — **Done** (`279b6235`).
2. Release tests § above green. — **Done** (11/11 targeted Release).
3. Read contract verified (no action/diagnostic side effects). — **Done**.
4. §11C.4 slice row updated; **G-C-INV** remains **Deferred** until Apply + Mat-Start slices are authorized and shipped. — **Done** (governance sync with closure).

**C-ENF** trilogy **Closed**; **G-C-INV** §12 **Done** (see [mat-start](./slice-c-enf-mat-start.md) for Host Start HTTP coverage limit).

---

## Out-of-scope WIP (stash)

Apply / Mat / Start gates and `DrawObsoleteForConsumption` wiring live in **`git stash`** `c-enf-apply-mat-gates-wip` and `.c-enf-gates-wip/` — **must not** ship in the Detect commit.
