# SLICE-C-ENF-DETECT

| Field | Value |
|-------|--------|
| **Status** | **Authorized** (DM arbitration Detect scope, 2026-10-16) — **not Closed** until dedicated commit, Release tests, DTO contract, and §11C.4 review |
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
| | **SLICE-C-ENF-APPLY**, **SLICE-C-ENF-MAT-START** (remain **Pending**) |

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

**Intersection** of current affectation authoring `EntryId` set and current composition `EntryId` set. Membership in only one side is **not** sufficient.

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

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Published + Resolved, not Applied; allowed pool shrink makes resolution incompatible | `true` |
| 2 | Resolved entry ∉ `Inputs.Entries` | `true` |
| 3 | Fixed slot or group removed from stage | `true` |
| 4 | Empty effective pool, non-empty resolution | `true` |
| 5 | Same `EntryId` set, reorder only | `false` |
| 6 | Applied draw | `false` |
| 7 | Non-consumable states (Draft, Cancelled, NoSolution, Published not Resolved) | `false` |
| 8 | Overview DTO matches predicate for representative cases | exact |
| 9 | Dedicated test project run in **Release** | pass |

G-B-3 refuse-mutation coverage stays in **GOps3 / affectation** suites — not substituted as Detect proof.

---

## Closure criteria

**Closed** only when:

1. Diff contains **Detect scope only** (predicate, overview/DTO, SPA type, tests).
2. Release tests § above green.
3. Read contract verified (no action/diagnostic side effects).
4. §11C.4 slice row updated; **G-C-INV** remains **Deferred** until Apply + Mat-Start slices are authorized and shipped.

---

## Prototype local vs contract (pre-implementation)

Uncommitted `DrawConsumptionConformance` is a **technical draft**, not ratified. Align or replace before the Detect commit:

| Topic | Contract | Local prototype (working tree) |
|-------|----------|--------------------------------|
| Effective pool | **Intersection** affectation ∩ composition | **Union** (concat sets); skips W-pool when `pool.Count == 0` |
| Published, not Resolved | `false` | `true` when `Resolution.State != Resolved` |
| NoSolution | `false` | Treated like non-Resolved → `true` |
| W-rules | Excluded | Not implemented (aligned) |
| Applied exclusion | `false` | Aligned |
| Fixed slot/group removal | Obsolete | Aligned |
| Resolution ∉ `Inputs.Entries` | Obsolete | Aligned (resolution vs inputs) |
| Apply / Mat / Start gates | Out of slice | Present in local tree — **must not** ship in Detect commit |

**Next implementation step:** adjust predicate + tests to contract, commit **Detect only**, leave Apply/Mat-Start WIP uncommitted until separately authorized.
