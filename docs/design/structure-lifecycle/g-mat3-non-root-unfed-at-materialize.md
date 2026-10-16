# G-MAT3 — `NonRootUnfed` at materialize (spec draft)

| Field | Value |
|-------|--------|
| **Backlog ID** | G-MAT3 |
| **Reference case** | **R-Mat-M2** (topology signal → materialize gate when production impossible) |
| **Contract status** | **Spec draft (§12)** — **gate deferred** 2026-10-15; **R-Mat-M2b** signed ([decision](./g-mat3-arbitration-session.md)); not §11B; **not** Ready |
| **§11B** | **Not signed** by this document |
| **Scope** | G-MAT3 only — does **not** authorize G-MAT4 |

**Product policy anchor (signed §11A, reserve A5):** §5.2 — `NonRootUnfed` is a **capacity/topology signal** in Draft/Ready hub; **Materialize** must block only when an **eligible confrontation on the production path** cannot be fed because the stage has no inbound Qual/Prog **and** no affectation population. **No blanket** “any `NonRootUnfed` ⇒ deny materialize.”

---

## Guardrails

1. **Diagnostic ≠ veto** — Hub may keep emitting `NonRootUnfed` as **warning** for unfed non-root phases that are not on the materialize production path.
2. **Format-specific proof** — Predicate must name **which confrontations** require inbound feed for the command (`MaterializeCupFromOccupiedSlots`, `MaterializeMatches`, Swiss progressive APIs are out of scope here).
3. **Command-level tests** — Acceptance uses a **materialize use case**, not only `CompetitionFlowAnalyzer`.

---

## Diagnostic today (read model)

[`AppendNonRootUnfed`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) emits when, for stage `S`:

| Condition | Meaning |
|-----------|---------|
| `S` is not the competition entry (first) stage | Non-root |
| No Qual/Prog inbound from another stage | `!HasInboundFeed(S, …)` |
| `S.AffectationAuthoring` is empty | No manual population |

Severity in hub: **warning** ([`structureDiagnosticSeverity.ts`](../../../src/PlayUp/web/src/pages/structure/structureDiagnosticSeverity.ts)). Prepare does **not** treat `NonRootUnfed` as a prepare gate issue today.

---

## Question to decide

**When must an unfed non-root stage block match production?**

**Proposed answer (candidate contract):** Refuse materialize for stage `S` when **all** of the following hold:

| Step | Predicate | Notes |
|------|-----------|--------|
| Unfed shape | Same as diagnostic: non-root, no inbound Qual/Prog, empty affectation | Align with `AppendNonRootUnfed` |
| Production need | `S` has at least one **addressable** confrontation on the **materialize batch** (occupied slots / fixtures / bracket pairs the command will create or activate) | Mirror G-MAT1 “necessary + addressable” vocabulary |
| Missing feed | That confrontation’s sides cannot be resolved from **on-stage** population (slots, affectation, or local placement) | **Open:** exact resolver per format (Cup vs Ch/Groups) |

**Explicit non-goals for G-MAT3:**

- Blocking materialize when `NonRootUnfed` is informational only (e.g. optional parallel phase not on the batch path).
- Replacing affectation/qual/prog authoring — only **production impossibility** at command time.

---

## Current runtime (gap)

| Layer | Behaviour today |
|-------|-----------------|
| Read / hub | `NonRootUnfed` warning on affected stages |
| Prepare | No `NonRootUnfed` gate |
| Materialize | **No** topology feed gate — roster/format/execution gates only |

---

## Positive scenario (must block materialize — target)

**Story:** Cup KO phase `S` is non-root, has bracket slots on the materialize path, **no** qual/prog inbound and **no** affectation entries, organizer attempts `MaterializeCupFromOccupiedSlots` on confrontations that require fed participants.

**Expected after contract:**

- Refuse with a dedicated **Application** code (TBD — e.g. extend family or reuse feed code with stage id list).
- Hub **may** still show `NonRootUnfed` (unchanged).

**Negative scenario (must allow materialize):**

- `NonRootUnfed` on a stage **not** in the materialize batch, or stage fed via affectation despite empty Qual/Prog graph edges.

---

## Open arbitrations (decision maker)

| # | Topic | Options |
|---|--------|---------|
| **M3-1** | Error surface | New wire code vs reuse existing feed attribution family |
| **M3-2** | Cup vs Championship | Single predicate vs per-command adapters (like G-MAT1 Cup-first) |
| **M3-3** | Relationship to `SlotFeedMissing` | Materialize already fails on missing slot feed — clarify overlap so organizers are not double-penalized with conflicting copy |

---

## Decision (2026-10-15) — gate deferred, M2b signed

See [g-mat3-arbitration-session.md](./g-mat3-arbitration-session.md). **R-Mat-M2b** is implemented as a non-regression contract; **no** `Application.NonRootUnfedAtMaterialize` gate. Reopen G-MAT3 only under conditions listed in the decision.

## Acceptance criteria (if reopened toward §12 Ready)

- [ ] **R-Mat-M2a** attainable under revised predicate **or** explicit « feed slot impossible » decision.
- [ ] Written predicate + M3-1..3 closed.
- [ ] Command tests block + allow negative.
- [ ] §12 promotion **Ready** only after §11B or narrow exception.

---

## Related

- [phase-mutability-matrix.md](./phase-mutability-matrix.md) §5.2 `NonRootUnfed`, §10 R-Mat-M2, §12 G-MAT3
- [g-mat1-confrontation-undecidable-at-materialize.md](./g-mat1-confrontation-undecidable-at-materialize.md) — template for “necessary + addressable” materialize gates
