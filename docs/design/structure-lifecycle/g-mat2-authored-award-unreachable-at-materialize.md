# G-MAT2 — `AuthoredAwardUnreachable` at materialize (spec draft)

| Field | Value |
|-------|--------|
| **Backlog ID** | G-MAT2 |
| **Reference cases** | R-Prep3 (Prepare), **R-Mat-M2-*** (command tests below) |
| **Contract status** | **Done (§12)** — option **B** signed 2026-10-15 |
| **Scope** | G-MAT2 only — separate gate from G-MAT1; no G-MAT3/4, no OPS |

**Product policy (§5.2 / §11A):** Materialize blocks only when unreachable placement awards affect the **requested production scope** — not a blanket hub veto (reserve A5).

**Prepare (unchanged):** [`StructurePrepareAttributionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePrepareAttributionGate.cs) — **global** refusal on non-classifying stages when [`AnalyzeResult`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) reports `AuthoredAwardUnreachable` (`Application.AttributionUnreachable`).

**Materialize (target):** **production-scoped policy (option B)** — **not** strict Prepare parity. Prepare guards **transition to Ready**; Materialize guards **whether the requested confrontations may be produced** given placement reachability.

---

## Decision-maker arbitration (2026-10-15)

| Topic | Decision |
|-------|----------|
| **A vs B** | **Option B preferred** — targeted materialize must not fail solely because an unrelated unreachable award exists elsewhere on the stage. |
| **§12** | **Ready** → **Done** (implementation 2026-10-15). |

| | Option A (global) | **Option B (production-scoped)** |
|---|-----------------|----------------------------------|
| Prepare alignment | Strong | **Difference assumed** |
| `pairKeys` partial production | Risk of over-blocking | **Fits command model** |
| Risk | Block realizable production | Incomplete phase if scope mis-defined |
| Complexity | Lower | Must tie award keys to **requested / eligible** pairs |

---

## Guardrails

1. **No hub veto** — use `AnalyzeResult` facts, scoped to the materialize request.
2. **Non-classifying only** — no G-MAT2 on classifying phases ([`StructurePrepareAttributionTests`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePrepareAttributionTests.cs) R-classifying).
3. **Command tests** — `MaterializeCupFromOccupiedSlots`; not analyzer-only.
4. **Error** — `ApplicationErrorCodes.AttributionUnreachable` (not `ConfrontationUndecidable`).
5. **Gate separation** — `StructureMaterializeAttributionGate` **distinct** from [`StructureMaterializeExecutionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureMaterializeExecutionGate.cs) (G-MAT1); separate tests; order after Ready check: **G-MAT1 → G-MAT2 →** `ResolveTargetPairs`.

---

## Factual inputs (code)

For stage `S` (Cup / non-classifying):

```text
unreachableKeys(S) = {
  d.SlotKey  // Analyzer stores SourcePairKey here
  | d in AnalyzeResult([S])
  , d.Code == AuthoredAwardUnreachable
  , d.StageId == S.Id
}
```

(`CompetitionFlowDiagnostic.SlotKey` carries `PlacementAwardPath.SourcePairKey` for this code.)

**Placement “obligatory” for production (v1):** a placement award path exists on `S` for pair key `K` and `K ∈ unreachableKeys(S)`. There is **no** separate “optional award” flag in Domain v1 — scoping is by **which confrontations are being materialized**, not by optional vs mandatory metadata.

---

## Option B — blocking predicate (candidate contract)

Let `requestedKeys` = explicit `pairKeys` argument (may be empty / null).

Let `eligibleTargets` = bracket pairs that [`MaterializeCupFromOccupiedSlots`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs) would resolve **after** gates (same rules as today: occupied slots, no fixture yet, etc.).

| Call shape | G-MAT2 refuses when |
|------------|---------------------|
| **Targeted** (`requestedKeys` non-empty) | ∃ `k ∈ requestedKeys` such that `k ∈ unreachableKeys(S)` |
| **Global** (`requestedKeys` null or empty) | ∃ pair `p ∈ eligibleTargets` such that `p.PairKey ∈ unreachableKeys(S)` |

**Does not refuse (B):** `unreachableKeys` non-empty but **disjoint** from `requestedKeys` and from `eligibleTargets` (e.g. unreachable award on `P1`, materialize only eligible `P2`).

**Does not replace other failures:** if no intersection but no eligible pairs → `MaterializationFailure` as today. G-MAT2 tests must assert **error code**, not only “no throw”.

**Not in scope for B:** refusing global materialize solely because *some* unreachable award exists while **no** eligible target is affected — partial Cup production is **allowed** under B (product accepts incomplete phase until a later command or Prepare policy elsewhere).

---

## Conditions 1–5 (before Ready)

| # | Condition | Status in this fiche |
|---|-----------|----------------------|
| **1** | Award vs production: block only when unreachable key ∈ requested or ∈ eligible global set | § Predicate |
| **2** | Targeted `pairKeys` partial behaviour | § Targeted row + **R-Mat-M2-partial** |
| **3** | Global call: refuse only on intersection with **eligible** targets, not on unrelated unreachable keys | § Global row + **R-Mat-M2-global-ok** |
| **4** | Tests with explicit expected codes | § Test-first contracts |
| **5** | G-MAT1 / G-MAT2 separate gates and tests | § Guardrails + implementation |

---

## Test-first contracts (Cup)

**File (proposed):** `MaterializeCupFromOccupiedSlotsGMat2Tests.cs`

### R-Mat-M2+ — targeted, award blocks requested key (P+)

| Field | Value |
|-------|--------|
| **Graph** | R3 — `CreateFedCupFinal`, placement on `FINAL`, `bracketIntact: false`, slots fed ([`StructurePrepareAttributionTests`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePrepareAttributionTests.cs)). |
| **Ready** | `stage.Prepare(clock)` |
| **pairKeys** | `["FINAL"]` |
| **Expected** | `Application.AttributionUnreachable`; no fixtures/matches created |

### R-Mat-M2-partial — unreachable award unrelated to request (N, B)

| Field | Value |
|-------|--------|
| **Graph** | Semi: `SeedEntryRoundBracketPairs` → `P1`,`P2`; `ReplacePlacementAwardRules` on **`P1`**; `ReplaceBracketPairs` **only `P2`**; occupy `SF2-A`/`SF2-B` (mirror G-MAT1 N2 pattern). |
| **Ready** | `stage.Prepare(clock)` |
| **Pre** | `unreachableKeys` contains `P1`; `AnalyzeExecution` independent |
| **pairKeys** | `["P2"]` |
| **Expected** | **Success** (1 match); **not** `AttributionUnreachable` |

### R-Mat-M2-global-ok — global materialize skips unrelated unreachable award (N, B)

| Field | Value |
|-------|--------|
| **Graph** | Same as **R-Mat-M2-partial** |
| **Ready** | `stage.Prepare(clock)` |
| **pairKeys** | `null` (all eligible) |
| **Expected** | **Success** on `P2` only; **not** `AttributionUnreachable` |

### R-Mat-M2-global-block — global call intersects unreachable (P+, B)

**Logic:** `pairKeys` null/empty → refuse iff `eligibleTargets ∩ unreachableKeys ≠ ∅`.

**V1 factual note:** With current [`AnalyzeResult`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs), `AuthoredAwardUnreachable` means **no `BracketPair`** for that key — such a key is **not** materialize-eligible. So in practice `eligibleTargets ∩ unreachableKeys` is often **empty**; global refusal then does not apply and the command may still fail with `MaterializationFailure` for other reasons.

| Field | Value |
|-------|--------|
| **Mandatory v1 tests** | **R-Mat-M2+** (targeted bad key) + **R-Mat-M2-partial** + **R-Mat-M2-global-ok** |
| **Optional** | **R-Mat-M2-global-block** only if a future graph or analyzer change allows a key to be both unreachable and eligible — otherwise predicate stays for defense in depth |

**Product confirmation before Ready:** accept that **global block** may be **vacuous** in v1 while **targeted** `pairKeys` carrying an unreachable key remains the main Attribution refusal path.

### R-Mat-M2-coherent — N2

R2 `CreateFedCupFinal` coherent; `PrepareStage.Execute`; `pairKeys` `["FINAL"]`; success.

### R-Mat-M2-classifying — N1

Gate unit test: classifying stage → `Ensure` no-op (no `AttributionUnreachable` from G-MAT2).

### G-MAT1 separation

At least one test where **G-MAT1** would fire (`ConfrontationUndecidable`) and G-MAT2 does not, and vice versa — different `ApplicationErrorCodes`.

---

## Implementation shape (after §12 Ready only)

- `StructureMaterializeAttributionGate.EnsureCupStage(Stage, IReadOnlyList<string>? pairKeys)` — implements § Option B predicate; needs eligible preview or duplicate `ResolveTargetPairs` eligibility logic **without** side effects.
- Invoke from `MaterializeCupFromOccupiedSlots.EnsureMutable` **after** G-MAT1.

---

## Validation checklist (Litigious → Ready)

- [x] Decision maker prefers **option B** (2026-10-15).
- [x] Global block = `eligibleTargets ∩ unreachableKeys` (often empty in v1; no veto on unrelated awards).
- [x] R-Mat-M2-partial / global-ok / + / R2 / G-MAT1 separation — tests in `MaterializeCupFromOccupiedSlotsGMat2Tests`.
- [x] §12 **Done**.

---

## Related

- [g-mat1](./g-mat1-confrontation-undecidable-at-materialize.md)
- [phase-mutability-matrix.md](./phase-mutability-matrix.md) §5.2 `AuthoredAwardUnreachable`
- Notion Prepare attribution: [Structure — Prepare gate Attribution](https://www.notion.so/3f875fb92d108110a0a9d3fb904a3d37)
