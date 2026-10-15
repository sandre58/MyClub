# G-OPS1 + G-OPS2 — Ready operational mutations & D1 upstream exits

| Field | Value |
|-------|--------|
| **Backlog IDs** | G-OPS1 (Ready ops), G-OPS2 (D1 exits) |
| **Reference cases** | **R-D1a**, **R-D1b** (b2 primary; b1 complementary, post–first slice) |
| **First slice command** | `ReplaceStageQualificationRules` only — **no** generic multi-command D1 guard in this delivery |
| **Contract status** | **First slice product contract validated** (2026-10-15) — full G-OPS matrix **not** signed |
| **§12** | **G-OPS1** and **G-OPS2** → **Ready** for this slice only (see acceptance criteria below) |
| **§11A** | Product policy signed (D1–D4, Swiss capabilities) |

**Goal:** Enable **operational** edits on **Ready** stages without production (D2), while refusing upstream **exit/flux** mutations that would **invalidate** a **Ready** downstream consumer (D1) — based on **effect on consumed flux**, not on “a Ready stage exists downstream.”

**Principle (signed):** D1 protects the **feed contract** of **Ready** downstream consumers, not their status alone. Require **business-impact proof** before refusal; avoid an over-ambitious generic guard in the first delivery.

---

## Layer separation (do not merge)

| Layer | Policy | Guard (slice 1) | Typical error |
|-------|--------|-----------------|---------------|
| **D2 — production freeze** | No general operational/structural desync when matches exist on the stage | `OperationalMutationGuard` + `StageAttachedProduction` | `Application.StructureNotMutable` |
| **D1 — Ready consumer protection** | Block when **promised inbound** to a Ready consumer drops below placed composition | `StructureReadyConsumerFeedGate` (G-OPS2) | `Application.ReadyConsumerWouldBreak` + consumer list |
| **Command / format** | Swiss progressive, draw-refuse (G-OPS3), materialize gates (G-MAT*) | Capability-specific entry points | Per use case |
| **Prepare / integrity** | Unchanged in OPS slice | Existing Prepare gates | Existing codes |

**Swiss (R-Swiss1/2):** No global “format Swiss” bypass on D1. Allowed Swiss behaviour stays **explicit per command/capability**. D1 must **not** block progression already permitted by Swiss APIs (`GenerateNextRound` / `DematerializeSwissLastRound`).

---

## Arbitration record (2026-10-15)

| Topic | Decision |
|-------|----------|
| **W3 vs W2** | **W3** suffices when incompatibility is demonstrated. **W2** is an **alternative** witness, not mandatory alongside W3. |
| **R-D1b scenario** | **(b2)** primary: Top 2 → Top 1 with **two** entries from `U` still on **Ready** `D`. **(b1)** clear qual → complementary test **after** first slice. |
| **OPS1 + OPS2** | Same **functional** slice, **two** distinct rules/guards. D1 cannot be validated in read-only mode if the command still rejects Ready edits on `U`. §12 keeps **two rows** with separate acceptance criteria. |
| **Error code** | `Application.ReadyConsumerWouldBreak` (provisional = target). Payload: blocking consumer `stageId` + names. |
| **Before/after** | Compute mutation effect **without persisting**; compare feed contracts. Reuse existing analyses when they cover the case; **do not** require W2 when W3 proves breakage; **do not** duplicate qualification rules in a divergent second implementation. |
| **Remediation slice 1** | Structured API error + `consumerStageIds`; SPA lists **blocking phase names** in qual/prog/composition mutation footers. Hub/drawer remediation remains out of scope. |
| **Scope** | First slice validates the **model** on one command; **do not** generalize D1 to other operational commands in the same delivery. |

---

## Work order (implementation)

| Step | Deliverable |
|------|-------------|
| **1** | Contract tests for OPS1 + D1 on `ReplaceStageQualificationRules` (written first) |
| **2** | **R-D1a** green: allow when `D` is Draft |
| **3** | **R-D1b (b2)** green: refuse when `D` is Ready and **W3** demonstrated |
| **4** | Implement **OPS1** (Ready operational auth) and **OPS2** (D1 guard) **separately**, both before `ReplaceQualificationRules` |
| **5** | Atomic refusal: no rule change, no status change on `D` |
| **6** | **R-Swiss1/2** + command-targeted tests green |

---

## Reference cases R-D1a / R-D1b

### Shared vocabulary

| Term | Meaning |
|------|---------|
| **Upstream `U`** | Stage whose `QualificationRules` are replaced |
| **Downstream `D`** | Stage referenced as `Destination.StageId` on an outbound qual path from `U` |
| **Exit / flux mutation** | `ReplaceStageQualificationRules` (paths cleared, reduced, or destination/selection changed) |
| **Feed contract** | What `D` relies on from `U` to stay consistent at **Ready** (composition volume, slot keys, analyzer feeds, etc.) |

**Not in scope for R-D1a/b:** rename stage, affectation-only, draw rules, placement awards, materialize.

### R-D1a — allow (downstream still Draft) — **signed graph**

| Field | Fixture |
|-------|---------|
| **Topology** | `U` Ready, qual feeds `D`; pattern aligned with [`StructurePreparePlacesGateTests`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/StructurePreparePlacesGateTests.cs) |
| **Initial** | `U` **Ready**; `D` **Draft**; qual **Top 2** → `D` |
| **Mutation** | On `U`: **Top 2 → Top 1** |
| **Expected** | Success; **no** `Application.ReadyConsumerWouldBreak`; `D` remains **Draft** |

**Intent:** Do **not** block because an downstream phase exists — D1 applies only to **Ready** consumers.

*Precondition:* **G-OPS1** allows operational qual replace on `U` while Ready (today: Draft-only via structure edit guard).

### R-D1b — refuse (Ready consumer inconsistent) — **signed graph (b2)**

| Field | Fixture |
|-------|---------|
| **Topology** | `U` Groups Ready; qual **Top 2 overall → population/form on `D`**; `D` Places N = 2, composition filled from qual |
| **Initial** | `U` **Ready**, `D` **Ready** (Prepare satisfied on both) |
| **Mutation** | **Top 2 → Top 1** on `U` while `D` still has **two** entries sourced from `U` |
| **Expected** | `Application.ReadyConsumerWouldBreak`; payload identifies **`D`**; **no** cascade; `D` stays **Ready**; **no** persisted rule change on `U` |

**Proof required:** **W3** — two entries on `D` are **incompatible** with upstream selection **after** the mutation. **Not** sufficient to observe `D.Status == Ready` alone.

### R-D1b — (b1) complementary — **implemented**

Clear qualification on `U` while `D` Ready with placed entries → `ReadyConsumerWouldBreak` (path removal / W1). Test: `ReplaceQualificationRules_R_D1b_refuses_clear_qual_when_ready_consumer_has_placed_entries`.

### Effect witnesses

| Witness | When true |
|---------|-----------|
| **W1 — path removal** | No outbound qual path from `U` to `D`, and `D` depends on `U` for population/form/slot |
| **W2 — analyzer delta** | **Optional** proof: new feed resolution failure on `D` after mutation snapshot (e.g. `SlotFeedMissing`) |
| **W3 — population contract** | Selection on paths to `D` **narrows** below count of entries already placed on `D` via qual from `U` |

**D1 fires** only if **≥1 witness** applies to a **Ready** `D` that is **affected** by the mutation. **W2 is not required** when **W3** (or W1) already proves breakage.

---

## D1 predicate (v1 — signed)

```text
affectedDownstream(U, before, after):
  stage IDs D where outbound path to D was removed, destination changed,
  or selection narrowed (e.g. Top N → Top M with M < N on same destination)

For each D in affectedDownstream:
  if D.Status != Ready → skip
  if !feedContractBroken(U, D, before, after, allStages) → skip
  else → collect D

If collect non-empty → throw ReadyConsumerWouldBreak(consumers)
```

```text
feedContractBroken:
  true if W1 or W2 or W3
  false if only Draft downstream affected, or no witness for Ready D
```

**Explicit non-goals (first slice):**

- Generic D1 on `ReplaceStageProgressionRules`, affectation, progression, etc.
- Blocking when `D` is Draft (R-D1a).
- Hub/drawer remediation for D1.
- Full replay of match results or ApplyProgression history.

---

## First command slice — `ReplaceStageQualificationRules`

| Choice | Rationale |
|--------|-----------|
| **Command** | [`ReplaceStageQualificationRules`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageQualificationRules/ReplaceStageQualificationRules.cs) |
| **Slice 2** | `ReplaceStageProgressionRules` (OPS1+D1) |
| **Slice 3** | `ReplaceStageAffectationAuthoring` (OPS1/D2 only — no D1) |

### Automated tests (first delivery)

| Test | Maps to | Expected |
|------|---------|----------|
| `ReplaceQualificationRules_R_D1a_allows_when_downstream_draft` | R-D1a | Success |
| `ReplaceQualificationRules_R_D1b_refuses_top2_to_top1_when_ready_consumer_w3` | R-D1b b2 | `ReadyConsumerWouldBreak` + `D` in payload |
| OPS1 control | `U` Ready, no matches, non-breaking edit | Success |
| Swiss regression | R-Swiss1/2 | Unchanged |

**(b1)** test added in a follow-up increment unless bundled without scope creep.

### Guard placement

1. **OPS1** — allow Ready operational replace when D2 satisfied (`!HasAttachedMatches(U)`), replacing Draft-only structure guard for this command path.
2. **OPS2 (D1)** — after OPS1, **before** `stage.ReplaceQualificationRules`: evaluate `before`/`after` on candidate rules **without persisting**.

### Error payload

```json
{
  "code": "Application.ReadyConsumerWouldBreak",
  "consumerStageIds": ["<guid>"],
  "consumerStageNames": ["KO"]
}
```

Host/i18n mapping in implementation PR; slice 1 = API surface only.

---

## Generic D1 — `StructureReadyConsumerFeedGate`

Shared **`EnsurePromisedInboundChange`**: for each affected Ready consumer `D`, compare promised inbound volume before/after; refuse when `placedCount > after` or path removed (`after == 0`).

| Mutation | Promised inbound |
|----------|------------------|
| Qual / prog rules | Sum of path volumes from upstream rules to `D` |
| Affectation | `min(configured qual+prog to D, source population)` — rules unchanged; population projected from affectation diff on composition |

---

## Third command slice — `ReplaceStageAffectationAuthoring` (2026-10-15)

| Item | Detail |
|------|--------|
| **Guards** | OPS1 + **`StructureReadyConsumerFeedGate.EnsureAffectationReplaceAllowed`** (D1: shrink source population caps promised feed; qual/prog rules unchanged) |
| **Domain** | `ReplaceAffectationAuthoringOperationally` |
| **Tests** | `ReplaceStageAffectationAuthoringGOpsTests` |

**Intents overloads:** qual/prog `Execute(..., intents, ...)` use the same OPS1+D1 gates as path specs; contract tests cover intent clear → `ReadyConsumerWouldBreak`.

---

## Second command slice — `ReplaceStageProgressionRules` (2026-10-15)

| Item | Detail |
|------|--------|
| **Guards** | Shared `StructureOperationalExitEditGuard` (OPS1) + `StructureReadyConsumerFeedGate` (OPS2) |
| **Domain** | `ReplaceProgressionRulesOperationally` (Draft/Ready) |
| **Tests** | `ReplaceStageProgressionRulesGOpsTests` — D2 (attached matches), D1 allow when D Draft, D1 refuse when inbound prog volume &lt; composition on Ready D |
| **Volume** | Progression: one path ⇒ one inbound seat (`StructurePopulationVolume` parity) |

---

## §12 acceptance criteria (split)

### G-OPS1 — Ready operational mutations (this slice)

| Criterion | Detail |
|-----------|--------|
| **Scope** | `ReplaceStageQualificationRules` on stage `U` in **Ready** when **no** attached production on `U` (D2) |
| **Behaviour** | Qual rules replace permitted when D1 would not refuse (includes R-D1a) |
| **Out of scope** | All other executor commands; structural/game-reg mutations |
| **Tests** | R-D1a + OPS1 control; existing Draft behaviour unchanged where applicable |

### G-OPS2 — D1 upstream exit guard (this slice)

| Criterion | Detail |
|-----------|--------|
| **Scope** | Same command only; qual exit mutations |
| **Predicate** | Feed-contract witnesses W1–W3; **W3** primary for b2; W2 optional |
| **Behaviour** | Atomic refuse with `Application.ReadyConsumerWouldBreak` + consumers; no state mutation on refuse |
| **Tests** | R-D1b b2; negative: R-D1a does not raise D1 |
| **Out of scope** | Generic `UpstreamExitMutationGuard` shared by all commands; b1 until follow-up |

**Coupling:** One implementation series may land both guards on `ReplaceStageQualificationRules`; §12 rows remain **separate** for sign-off and Done tracking.

---

## G-OPS1 reminder (domain today)

`ReplaceQualificationRules` → `EnsureStructureMutable` (**Draft only**). OPS1 requires application/domain path that permits **Ready** operational replace under D2, **before** R-D1a is runnable in production code.

---

## Validation checklist

### Product — first slice

- [x] **D1 predicate** — effect on feed contract (W1–W3; W2 not mandatory with W3).
- [x] **R-D1a** — Top 2 → Top 1, `D` Draft.
- [x] **R-D1b** — b2 primary; b1 deferred.
- [x] **First command** — `ReplaceStageQualificationRules`.
- [x] **Error code** — `Application.ReadyConsumerWouldBreak` + consumer payload.
- [x] **OPS1 + OPS2** — same slice, two rules; two §12 rows.
- [x] **Swiss** — per-capability exemptions, no global Swiss bypass on D1.
- [x] **Remediation** — API error only in slice 1.

### Technical (pre-code)

- [x] Before/after without persist; reuse existing analyses where possible; no divergent qual engine.
- [x] W3 — composition count vs inbound qual volume after mutation.
- [x] Host: HTTP 409, `consumerStageIds` extension (stage GUIDs in `Reasons`).
- [x] SPA: `errors.Application.ReadyConsumerWouldBreak` + `problems.lifecycle.ReadyConsumerWouldBreak`.

### Post-implementation (Done)

- [x] R-D1a / R-D1b (b2) automated — `ReplaceStageQualificationRulesGOpsTests`.
- [x] R-Swiss1/2 green (regression run with thin authoring qual test).
- [x] No change to Prepare, G-MAT*, EXEC-001 Dev.
- [x] §12 G-OPS1 / G-OPS2 → **Done** for this slice only.

---

## Related Git

- [phase-mutability-matrix.md](./phase-mutability-matrix.md) §1–§3, §5, §10 (R-D1a/b), §12
- [implementation-plan-placeholder.md](./implementation-plan-placeholder.md)
- Executor: [`UseCaseExecutor.Stages.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Pipeline/UseCaseExecutor.Stages.cs) (`ReplaceStageQualificationRulesAsync`)
