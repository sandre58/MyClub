# §11C — Addendum clarification & arbitration

| Field | Value |
|-------|--------|
| **Status** | **Signed (Git)** 2026-10-15 — matrix §11C, [E5 applied](./section-11c-e5-matrix-diff-proposal.md) |
| **Purpose** | Record A/B/C arbitrations and publish draw / obsolescence contracts in the mutability matrix |
| **Does not** | Reopen §11A A1–A6; authorize ENF backlogs outside **§11C.4** (currently **G-B-3-ENF** only); resolve full D3-1 beyond set-equality slice |
| **Git annex** | [phase-mutability-matrix.md](./phase-mutability-matrix.md) (§11A–§11C signed 2026-10-15) |
| **Date opened** | 2026-10-15 |

**Audience:** product decision maker, structure UX, backend — workbook + full Git sign (separate from §11A/§11B checkboxes).

---

## 0. Why this document exists

A coherent **UX narrative** (Brouillon = concevoir · Prête = organiser · Materialize = produire) was explored in discussion. Some ideas **tighten** or **extend** the mutability contract already signed in §11A (e.g. “remplissage only in Ready”, draw rules only in Draft, automatic draw invalidation). Those ideas must **not** be treated as decided until arbitrated here and, if accepted, recorded as an explicit **§11C amendment**.

**Rule:** §11A / §11B remain the lifecycle normative base; **§11C** (matrix §9.1.1, §10 cases, §12 G-B-1 / G-B-3 / G-C-INV) publishes draw and obsolescence contracts. This file remains the **arbitration workbook** and decision record.

---

## 1. Confirmed foundation (no arbitration required)

These points align with signed §11A / §11B and existing deferred work. They are **not** reopened by §11C.

| ID | Principle | Reference |
|----|-----------|-----------|
| **F1** | **Lifecycle** (`Draft` / `Ready` / `Running` …) is **orthogonal** to **operational health** (warnings, invalid states, gates). A stage may be **Ready** while still having operational issues to resolve. | §11A A2, A5; drawer vs Prepare/Materialize |
| **F2** | **D1** — Upstream exit/flux edits in Ready must not silently break a **Ready** downstream consumer; **block** and name consumers (no auto-cascade). | §11A A3; [g-ops](./g-ops-ready-operational-mutations-and-d1.md) |
| **F3** | **D2** — When **production** (attached matches) exists on the stage, deny **general** operational/structural edits that would desync production; Swiss uses **explicit** capabilities only. | §11A A4; §8 grid |
| **F4** | **Materialize** (and format-specific Start gates) remain the **production barrier** — incomplete or inconsistent operational state must **not** produce matches while prerequisites fail. | §11A A5; §5.2 |
| **F5** | **G-OPS3** (draw-refuse / composition vs Applied draw) stays **Deferred** per §11B; **D3-1..3** open — [g-ops3-arbitration-session](./g-ops3-arbitration-session.md). No implementation “by discussion”. | §12 G-OPS3 |
| **F6** | Avoid two design failures: (a) **silent inconsistency** after allowed Ready mutations; (b) **blanket auto-invalidation** without a defined dependency perimeter. | Discussion 2026-10-15 |

**UX narrative (refined after Arbitrage A — workbook only until §11C sign):** *Brouillon = concevoir* (including optional manual placement) · *Prête = organiser* under fixed form/regulation · *Materialize / draw execution* = workflows with their own gates — not “all filling only in Ready”. Do **not** turn UI preference into a business rule without a demonstrated use case (see §3.5).

---

## 2. Arbitration status

| ID | Topic | Workbook status | Blocks |
|----|--------|-----------------|--------|
| **Arbitrage A** | Placement (`operationalPlacement`) in Draft vs Ready only | **Decided — A-keep** (2026-10-15); see §3.4–§3.5 | UX copy only; **no** matrix §3 change |
| **Arbitrage B** | Draw lifecycle & mutability in Ready | **Signed (workbook)** 2026-10-15 — §4.10, §4.13; **does not** authorize code | **C** next; do **not** reopen B-Q1..B-Q3 |
| **Arbitrage C** | Obsolescence contract | **Signed (workbook)** 2026-10-15 — §5.5 | **C-ENF-*** backlog; slice required |
| **§11C full sign (Git)** | Matrix E5 | **Signed** 2026-10-15 — [E5](./section-11c-e5-matrix-diff-proposal.md) | **SLICE-1-G-B-3-ENF** — `G-B-3-ENF` only |

**Order:** **A → B → C → E5 → SLICE-1**. **Implementation:** **G-B-3-ENF** per [slice-1-g-b-3-enf.md](./slice-1-g-b-3-enf.md); other ENF forbidden.

**Normative policy:** §11A / §11B + **§11C** draw/obsolescence (matrix §9.1.1, §10, §12) — A-keep unchanged in §3 matrix.

---

## 3. Arbitrage A — Placement in Draft

### 3.1 Current signed contract

| Source | Policy |
|--------|--------|
| §11A A1 | Draft allows **operational** configuration (among other things). |
| Matrix §3 `operationalPlacement` | **Allow** in Draft and Ready (no production); **Deny** when production present (except Swiss row). |
| §9 `AssignEntryToSlot` / `ClearSlotAssignment` | Family **Placement**; hub actions exist in Draft competition context. |

**Conclusion:** §11A **does not** imply “placement only in Ready”. Restricting placement to Ready would be a **new** product rule.

### 3.2 Options

| Option | Description | UX narrative impact |
|--------|-------------|---------------------|
| **A-keep** | Keep matrix: manual slot placement allowed in **Draft and Ready** (subject to D2, F1 snapshots, format gates). | Reformulate UX: Brouillon = concevoir **and optionally pre-position**; Prête = organiser under committed form/regulation; emphasize **draw generate/apply** and **materialize** as Ready-centric workflows, not “all filling”. |
| **A-restrict** | **Ready only** for `AssignEntryToSlot` / `ClearSlotAssignment` (and hub equivalents). | Matches strict “organiser en Prête”; blocks pre-fill in Brouillon (e.g. seeding slots before Prepare). Requires matrix §3 + §9.1 + hub/SPA alignment. |

### 3.3 Consequences to document when choosing

| Concern | A-keep | A-restrict |
|---------|--------|------------|
| Prepare with empty slots | Allowed; completeness at Materialize | Prepare with empty slots still allowed (occupancy not Prepare gate per D3) |
| Cup “direct” on slot | May exist before Prepare | Only after Ready |
| Tests / scenarios | Current reference cases assume placement paths in Draft in places | New **R-Place-*** cases required |
| Relation to D4 | Form frozen at Ready; placement is **not** form | Same |

### 3.4 Decision record

| Field | Value |
|-------|--------|
| **Chosen option** | **A-keep** — `operationalPlacement` remains **Allow** in Draft and Ready (no production), per matrix §3 |
| **Decision date** | 2026-10-15 (workbook; pending B/C before §11C sign) |
| **Amends §11A?** | **No** — confirms existing policy; UX narrative adjusted (§3.5) |
| **Matrix §3 change** | **None** required |
| **Reopen A-restrict?** | Only if a **concrete use case** shows Draft placement causes harm that D1/D2/command gates cannot address |

### 3.5 Rationale (challenge both options)

**A-keep — aligned with §11A**

- Keeps target `operationalPlacement` (Draft + Ready).
- Separates **building/organizing** (may include manual assignments before Prepare) from **draw execution** and **match production**, which keep their own prerequisites (Ready for materialize batch, draw entity lifecycle, format gates).
- D1, D2, and per-command rules still bound what is valid; allowing placement in Draft does not imply “anything goes”.

**A-restrict — rejected for now**

- Would be a **new** restriction (Ready-only placement), not a reading of §11A.
- Would need a demonstrated product harm from pre-Prepare placement (e.g. systematic inconsistency, support burden, irreversible organizer mistakes) that existing guards do not cover.
- Risks conflating lifecycle with “effective filling” and pushing organizers to artificial Revert/Prepare cycles without added safety.

**UX consequence (signed at workbook level):** Do not message “remplissage effectif uniquement en Prête”. Instead: clarify that **tirage (génération/application)** and **materialization** are the operations with stricter stage/readiness expectations; manual placement may occur in Brouillon when the format allows it.

---

## 4. Arbitrage B — Draw rules & configuration in Ready

### 4.1 Tension

- **§11A / matrix:** `operationalDraw` **Allow** in Ready without production (target §9.1).
- **Domain today:** `ReplaceStageDrawRules` uses `EnsureStructureMutable()` → **Draft-only** for stage-level draw rules ([§9 inventory](./phase-mutability-matrix.md#9-commands--usecaseexecutorstages-inventory-current)).
- **UX discussion:** “Configure draw in Brouillon; in Prête only execute” — **stricter** than §11A for **rules**, unless reframed as contextual.

### 4.2 Options

| Option | Rule | Typical organizer story |
|--------|------|-------------------------|
| **B-strict** | All **draw rule / pot / composition constraint** authoring frozen at **Prepare** (Ready = execute draw workflow only). Any change → **Revert to Draft** (or deny command). | “Préparer fige aussi le mode de tirage.” |
| **B-contextual** | Draw **rules** editable in Ready until **no incompatible draw state** (e.g. until Applied — exact witness **TBD**, overlaps D3-1). After Applied, extra constraints (G-OPS3 scope). | “Je corrige les pots tant que le tirage n’est pas appliqué.” |

**Neither option is §11A today for B-strict** (restriction must be explicit). **B-contextual** is closer to §11A but must list **commands** and **draw entity states**.

### 4.3 Product principle (provisional — B-contextual)

> **Ready** does not, by itself, freeze all **draw parameters**. **Publish** and **Apply** are **distinct** lifecycle commitments on the **draw entity** (see [`DrawAppliedState`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs)). Post-**Applied** mutations need an **explicit contract** (G-OPS3 / Arbitrage C) — not a blanket “allow all in Ready”.

**Not authorized by this preference:** automatic invalidation, G-OPS3 implementation, or resolving **D3-1..3**. **Arbitrage C** remains independent — nothing here authorises global auto-invalidation.

#### Draw entity semantics (provisional — **B-Q1**)

| Draw state | Product meaning | Consequence (orientation) |
|------------|-----------------|-------------------------|
| **Draft** | Tirage en préparation | `ConfigureDrawInputs`, `GenerateDrawResolution`, etc., per existing guards |
| **Published**, not Applied | **Engagement métier** — published resolution is a committed outcome, not a silent preview | **No** silent rewrite of published resolution; domain immutability after `PublishDraw` ([`Stage.PublishDraw`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs)). Corrections use an **explicit** path (typically `CancelDraw` → new draw cycle), not “recalculate in place” UX |
| **Applied** | Resolution reflected on stage occupancy | Per-command post-Applied rules; **G-OPS3** + **C** still open; **R-Draw1-N2** remains reference |

**B-Q1 provisional (workbook, not sign):** **Published = engagement métier**, not “aperçu recalculable”. Wording *publier* must match this semantics.

**Clarification:** Parameters (`DrawRules`, pots, constraints) vs **published resolution** are different objects. Changing parameters while a **Published** draw exists may still be **disallowed or redirected** when it would contradict the committed resolution without going through cancel/recreate.

### 4.4 Provisional preference record (not §11C sign)

| Field | Value |
|-------|--------|
| **Provisional option** | **B-contextual** on **draw entity** lifecycle (Draft / Published / Applied); **`ReplaceStageDrawRules` stays Draft-only** per B-Q1b |
| **Recorded** | 2026-10-15 (workbook) |
| **Signed?** | **B yes** (workbook §4.13, 2026-10-15); **§11C full sign** pending **C** |
| **B-strict** | Rejected **for now** — would freeze draw rules at Prepare without §11A explicitly freezing that perimeter |
| **Amends §11A?** | **TBD at sign** — may **narrow** interpretation of `operationalDraw` for `ReplaceStageDrawRules` (product: stage Draft only); other draw commands remain per matrix |

### 4.5 Rationale — B-contextual vs B-strict (challenge)

| Option | Strength | Weakness |
|--------|----------|----------|
| **B-strict** | Simple story: Prepare freezes draw rules | Treats Prepare as functional freeze for a scope §11A does not label as form/game regulation |
| **B-contextual** | Separates **stage Ready** from **draw executed** | Requires Published vs Applied semantics and per-command post-Applied rules |

### 4.6 Product questions — status

| ID | Question | Status (workbook) |
|----|----------|-------------------|
| **B-Q1** | **Published, not Applied** — preview vs **engagement métier**? | **Provisional: engagement métier** — see §4.3 table. Aligns with domain immutability after `PublishDraw`. **Not §11C sign.** |
| **B-Q1b** | `ReplaceStageDrawRules` in Ready **R0** vs Draft-only; correction when **Published** | **Provisional: Draft-only** for `ReplaceStageDrawRules`; **R1** = P2 cancel → recreate (§4.6.1). **Not §11C sign.** |
| **B-Q2** | **Applied (R2)** — per-command contract post-application | **Workbook** §4.11 — provisional targets; **code verified** 2026-10-15; **not §11C sign** |
| **B-Q3** | Draw identity, triple boundary, consumption contract | **Workbook** §4.12 — provisional; **not §11C sign**; does **not** design **C** invalidation algorithm |

**Gate before Arbitrage C:** formal **B** sign (workbooks B-Q1..B-Q3 provisional); **B-Q3c** does **not** substitute **C**. No code from §11C until sign.

#### 4.6.1 B-Q1b — `ReplaceStageDrawRules` (provisional)

| Situation | Product target |
|-----------|----------------|
| **Stage Draft** | `ReplaceStageDrawRules` allowed per existing guards |
| **Ready R0** (no Published non-cancelled draw) | **Draft-only** for this command — **do not** widen to Ready merely because B-contextual suggested looser P1 |
| **Ready R1** (Published, not Applied) | No direct edit of published resolution; **P2** explicit **Cancel → recreate** (other draw commands) |
| **Ready R2** (Applied) | Out of B-Q1b — **B-Q2** + G-OPS3 / **C** |

**Rationale (provisional):** Parameters vs published resolution must stay distinct. If **Published = engagement métier**, opening `ReplaceStageDrawRules` in Ready **R0** would blur that contract and weaken the structural guard without a demonstrated use case. **Ready R0** would be a **domain contract change**, not UX clarification — reopen only with concrete need to edit rules after Prepare without Revert to Draft.

**Rejected for now:** Ready **R0** allow for `ReplaceStageDrawRules`.

**Code alignment:** Target matches domain today (`EnsureStructureMutable` → stage **Draft only**) — **no Δ** for this command unless product later reopens Ready R0.

#### 4.6.2 UX — Ready R1 correction path (orientation only)

- Explain that a **published** draw cannot be edited in place; offer the **correction** flow (cancel → new draw cycle) when guards allow.
- **Do not** promise Cancel if command/domain guards would refuse (D2, draw state, etc.) — copy must reflect actual hub/command availability.
- `ReplaceStageDrawRules` in Ready: steer to **Revert to Draft** (or equivalent) for **parameter** changes when no Published draw; when Published exists, steer to **P2**, not rules editor.

### 4.7 Three operation classes (provisional)

Separate UX and contract language — do not merge into “edit draw in Ready”.

| Class | Meaning | Provisional rule |
|-------|---------|------------------|
| **P1 — Parameter authoring** | `DrawRules` via `ReplaceStageDrawRules`, pots, draw **Draft** inputs | **`ReplaceStageDrawRules`:** stage **Draft only** (B-Q1b). Other P1 commands per §4.8 |
| **P2 — Correct Published, not Applied** | User regrets publish or needs new resolution | **Explicit** cancel → new draw cycle; **not** silent resolution rewrite (domain: Published immutable) |
| **P3 — After Applied** | Pool, placement, rules with occupancy impact | **Arbitrate per command**; **G-OPS3** + **C**; no general deny-all; no general auto-invalidate |

### 4.8 Command matrix — target (provisional) vs code today

**Legend**

- **Stage** = `StageStatus`; **Draw** = entity state (`Draft` / `Published` + `DrawAppliedState`).
- Columns for **Ready** split: **R0** = no non-cancelled Published draw (only Draft/none); **R1** = Published, not Applied; **R2** = Applied.
- **Target** = orientation only; **Code** = repo 2026-10-15; **Δ** = mismatch.

| Surface / command | Target — stage Draft (draw Draft) | Target — Ready **R0** | Target — Ready **R1** Published | Target — Ready **R2** Applied | Code today (summary) |
|-------------------|-----------------------------------|----------------------|----------------------------------|-------------------------------|----------------------|
| **P1** `ReplaceStageDrawRules` | Allow (guards) | **Deny** — Draft-only (B-Q1b) | **Deny** — P2 | **Deny** — §4.11.1 | **Aligned** — stage Draft only |
| **P1** `ConfigureDrawInputs` / Generate | Allow | Allow (draw Draft) | **Deny** — Published entity | **Deny** — §4.11.5 | Draw entity **Draft** only |
| **P2** `CancelDraw` + `CreateDraw` + … | Allow | Allow | Preferred path | §4.11.4 | Cancel does not unwind Apply |
| `PublishDraw` | Allow | Allow — B-Q1 | — | **Deny** (already Published) | Immutable after publish |
| `ApplyDraw` | Allow if gates | Allow if gates | Allow if gates | §4.11.5 — idempotent if conform | [`ApplyDrawTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ApplyDrawTests.cs) |
| **P3** `ReplaceStageAffectationAuthoring` | Allow (A-keep) | Allow | Allow (R-Draw1-N1) | §4.11.2 | No draw-refuse gate (**Δ** shrink + Applied) |
| **P3** `AssignEntryToSlot` / clear | Allow (A-keep) | Allow | Allow | §4.11.3 | OPS guard + F1 |

**`ReplaceStageDrawRules` (B-Q1b provisional):** product target = **Draft-only** on all Ready columns — **matches code**; implementation change **not** required for this command. UX/hub alignment is **documentation + presentation** scope until §11C sign (no code mandate in this workbook).

**Reopen Ready R0** only via new product decision + §11C amendment + explicit backlog row.

### 4.11 B-Q2 — Ready **R2** (Applied) — method & command workbook

**Principle (provisional):** `Applied` does **not** imply universal **Deny**. Each command needs an explicit contract for effect, guards, and dependencies. **No** global auto-invalidation (**C** stays closed until formal **B** sign, then **C** arbitration).

**Per-command columns (workbook):**

1. **Business effect** — rules / pool / assignments / applied resolution / occupancy  
2. **Provisional R2 target** — `Allow` | `Deny` | `Allow under condition`  
3. **Code today** — verified against domain + application use cases + cited tests  
4. **Impact** — D1/D2, divergence risk, G-OPS3 / **C** dependency, **Δ** if target ≠ code  

**Method rules:**

| Rule | Meaning |
|------|---------|
| Code | Factual statement only |
| Target | Provisional product orientation — not implementation |
| **Δ** | Requires explicit §11C sign + backlog — never implicit code change |
| **C** | Closed until B complete |

**Lower bound — `R-Draw1-N2` (signed negative contract, not general permission):**

| Field | Contract |
|-------|----------|
| **Test** | `ReplaceAffectationAuthoring_allows_replace_with_same_entries_when_group_draw_applied` |
| **Fixture** | Group stage; draw **Published** + **Applied** (`DrawAppliedState` true); stage then **Ready** (`PrepareStage`) |
| **Mutation** | `ReplaceStageAffectationAuthoring` with **same** `EntryId` set (order preserved) as before |
| **Expected** | Success — authoring unchanged in effect |
| **Not implied** | Any change to entry set, shrink, or other commands after Applied |

Reference: [`ReplaceStageAffectationAuthoringGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs).

**Upper risk — `R-Draw1+` (deferred G-OPS3):** shrink affectation while draw **Applied** — product target **refuse** when gate exists; **code today may allow** (no `StructureDrawCompositionGuard`) — **Δ**, not B-Q2 sign alone.

#### 4.11.1 `ReplaceStageDrawRules` (order 1)

| | |
|--|--|
| **Effect** | Stage-level **DrawRules** (mode, pots, constraints) — not draw entity resolution |
| **R2 target** | **Deny** — consistent with B-Q1b Draft-only; on Draft, domain also blocks replace when non-cancelled draw has **generated** resolution (`HasDrawWithGeneratedResolution`) until cancel |
| **Code** | `EnsureStructureMutable()` → fails on **Ready**; on Draft + generated draw → `DrawRulesReplaceBlockedAfterGenerate` ([`Stage.ReplaceDrawRules`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs)) |
| **Impact** | No G-OPS3/C for R2 specifically; **Aligned** with B-Q1b |

#### 4.11.2 `ReplaceStageAffectationAuthoring` (order 2)

| | |
|--|--|
| **Effect** | **Composition pool** authoring; syncs `CompositionEntries` by diff — does not by itself re-run Apply |
| **R2 target** | **Allow under condition:** (a) **same entry set** as today — **R-Draw1-N2**; (b) any **change** to set membership with Published+Applied draw → **Deny** (G-OPS3 target) or **refuse + explicit remediation** — **not** silent accept |
| **Code** | `StructureOperationalExitEditGuard` (Ready + no attached matches); D1 via `StructureReadyConsumerFeedGate`; **no** Applied-draw check — shrink with Applied **not** covered by N2 test → likely **succeeds today** (**Δ** vs G-OPS3 target) |
| **Impact** | D2 blocks when `stageMatches` attached; D1 on downstream Ready consumers; **G-OPS3** / **C** for incompatible pool changes; preserve **N2** at sign |

#### 4.11.3 Manual placement — `AssignEntryToSlot` / `ClearSlotAssignment` (order 3)

| | |
|--|--|
| **Effect** | **Slot occupancy** + `DirectAssignment`; Cup **F1** coherence vs materialized participants |
| **R2 target** | **Allow under condition:** no **D2** production on stage; coherent with materialized pairs; does not silently fix draw/group mismatch — independent manual edits **preserved** per future **C**, not auto-wiped |
| **Code** | `EnsureOperationalExitRulesMutable` (Draft/Ready); app guard + `MaterializedOpponentCoherence.EnsureCupPairsCoherent` ([`AssignEntryToSlot`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs)); **D2** via guard when matches passed |
| **Impact** | Group draw Applied: placement model is **group** membership (Apply path), not Cup slots — format-specific UX; **C** may flag inconsistency with Applied resolution without global invalidate |

#### 4.11.4 `CancelDraw` after Applied (order 4)

| | |
|--|--|
| **Effect** | Marks draw **Cancelled**; **does not** unwind group/slot occupancy from prior **Apply** ([`DrawAppliedState`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs) remarks) |
| **R2 target** | **Allow under condition:** guards pass; UX must **not** promise undo of applied placements; follow with **P2** new draw or manual reconciliation |
| **Code** | `EnsureOperationalExitRulesMutable`; **no** Applied check — **R-Draw1-N3** cancels then changes affectation on **Ready** stage |
| **Impact** | Risk of **divergence** between occupancy and active draw; **C** / diagnostics — not auto-invalidate in B-Q2 |

#### 4.11.5 Draw replacement & re-execution (order 5)

Covers **new draw cycle** after cancel and **re-apply**, plus preserving **R-Draw1-N2** boundary.

| Command | Effect | R2 target (provisional) | Code today | Notes |
|---------|--------|---------------------------|------------|-------|
| `CreateDraw` | New draw entity | **Allow under condition** — no other **non-cancelled** draw (B-Q3a) | OPS guard; SPA `hasActiveDraw` | Domain **Δ** if bypass SPA |
| `ConfigureDrawInputs` / `GenerateDrawResolution` | Draft draw only | **Deny** on Published draw — use **Cancel** first | Draw status must be **Draft** | |
| `PublishDraw` | Commit resolution | **Deny** if already Published | Domain `draw.Publish()` | B-Q1 engagement |
| `ApplyDraw` | Apply Published resolution to stage | **Allow under condition** — idempotent re-apply when occupancy already **conform** | `Execute_*_idempotent_when_already_conform` slot + group tests | **Not** a licence to change pool (see **4.11.2**) |
| Affectation **same set** (R-Draw1-N2) | Pool metadata replace | **Allow** — precise case only | Test N2 | **Do not** generalise to “edit applied draw” |

### 4.12 B-Q3 — Draw identity & triple boundary (product contract workbook)

**Scope:** define **what changes**, **which draw consumes it**, and **draw state** — not how to auto-invalidate (**C**). If multiple draws can coexist in history, **C** must later choose scope (one draw / all draws / no auto) — **not decided here**.

#### 4.12.0 Three business surfaces (do not conflate)

| Surface | Business question | Do **not** assume |
|---------|-------------------|-------------------|
| **`ReplaceStageDrawRules` / `DrawRules`** | Mode, pots, seeding, constraints at **regulation** level | Every rules change rewrites the pool or a Published resolution |
| **`ConfigureDrawInputs`** | Per-**draw** inputs used to **compute** resolution (entries subset, pots membership, fixed placements) | Configure = Apply; or that Published draw inputs can be reconfigured |
| **`ReplaceStageAffectationAuthoring`** | Phase **composition pool** (`CompositionEntries` source) | Every pool change auto-invalidates all draws |

**Consumption link (factual):** [`DrawInputsFactory`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/DrawInputsFactory.cs) builds default inputs from **`CompositionEntries`** (+ optional fixed from current form occupancy). **`DrawRules`** supply pot structure for generation; inputs are stored on the **draw entity** at configure time ([`Draw.ConfigureInputs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Draw.cs) resets resolution to NotResolved).

#### 4.12.1 B-Q3a — Multiple non-cancelled draws (identity)

| Scenario | Provisional product target | Code / UI today (verified 2026-10-15) | Gap |
|----------|---------------------------|----------------------------------------|-----|
| **Several `Draft` draws** | Do **not** assume uniqueness without semantics; product wants **one active execution** | Domain: `CreateDraw` **appends** — **no** max count ([`Stage.CreateDraw`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs)) | **Δ** API could create 2+ Draft if UI gate bypassed |
| **Several `Published` (not Applied)** | **Do not allow** two concurrent Published resolutions per phase without explicit semantics | Domain: **no** guard preventing second `PublishDraw` while another Published exists | **Δ** — SPA blocks new create when any non-cancelled draw ([`drawUi.ts`](../../../src/PlayUp/web/src/pages/structure/drawUi.ts) `hasActiveDraw`) |
| **Applied + another non-cancelled draw** | New cycle only after **Cancel** (cancelled history OK); not silent parallel Published competitor | Cancel then `CreateDraw` in tests ([`ReleaseDrawAlignedPlacementsTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReleaseDrawAlignedPlacementsTests.cs)); **Cancel does not unwind** Apply | List may hold **Cancelled** + **new** Published |
| **Chrome / reads** | Single **active** draw for badges | Newest non-cancelled ([`StructureViewAssembler.ResolveDrawExecutionBadge`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureViewAssembler.cs), `pickActiveDraw` SPA) | Cancelled draws retained in history |

**B-Q3a provisional:** **At most one non-cancelled draw execution per phase** (Draft-or-Published pipeline); **multiple cancelled** OK. **Formal B sign** should state enforcement layer: today **primarily SPA** + partial domain locks on `DrawRules` / draw mutability.

#### 4.12.2 B-Q3b — Mutation × draw state (provisional targets)

Columns = state of the **draw entity** identified by `DrawId`. Targets **not** signed.

| Mutation | Draw **Draft** | **Published**, not Applied | **Applied** |
|----------|----------------|----------------------------|-------------|
| **`ReplaceStageDrawRules`** | Per guards; blocked after non-cancelled **generated** draw until cancel | **Deny** on Ready (B-Q1b); use **P2** if resolution affected | **Deny** (§4.11.1) |
| **`ConfigureDrawInputs`** | Allow — resets that draw’s resolution | **Deny** (`DrawImmutable`) | **Deny** — pool via affectation / G-OPS3 |
| **Pool via `ReplaceStageAffectationAuthoring`** | Allow (A-keep, D1/D2) | Allow — **R-Draw1-N1**; no auto-invalidate Published | **R-Draw1-N2** same set; pool change → G-OPS3 **refuse** target (**Δ** code) |
| **`ApplyDraw` / re-apply** | N/A | Allow if Published+Resolved | Idempotent if conform only |

**Published correction:** `CancelDraw` → new cycle — not configure on Published entity.

#### 4.12.3 B-Q3c — Consumption contract (identity only — not **C** algorithm)

| Witness | Compares | Today | Notes |
|---------|----------|-------|-------|
| **W-inputs** | `Draw.Inputs` vs current composition / fixed | Snapshot on configure (Draft only) | Re-configure clears resolution on **that** draw |
| **W-rules** | `DrawRules` at generate vs current | L2 lock after generate (SPA + domain `HasDrawWithGeneratedResolution`) | |
| **W-applied** | Published resolution vs occupancy | [`DrawAppliedState`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs) | Publish ≠ Apply |
| **W-pool (G-OPS3)** | Affectation set vs Applied draw | **D3-1 open**; N2 = same **EntryId** set only | Deferred |

**B-Q3c provisional:** Consumption is **per `DrawId`** (inputs + rules at generate + occupancy when Applied). **Conformance handling** (refuse / flag / diagnostic) = **C** only — not B-Q3.

#### 4.12.4 Tests & gaps (B-Q3)

| ID | Observation | Reference |
|----|-------------|-----------|
| **T-Q3-1** | SPA blocks create when non-cancelled draw exists | `drawUi.test.ts` |
| **T-Q3-2** | Cancelled + new Applied draw coexist in list | `ReleaseDrawAlignedPlacementsTests` |
| **T-Q3-3** | No domain/app test enforcing **≤1 non-cancelled** draw | **Gap** → **G-B-1** §4.13 |
| **T-Q3-4** | Composition-only pool for inputs | `DrawInputsFactory` |
| **T-Q3-5** | Affectation × draw state negatives N1–N3 | `ReplaceStageAffectationAuthoringGOps3Tests` |

#### 4.12.5 B-Q3 decision record (not sign)

| Field | Value |
|-------|--------|
| **B-Q3a–c** | Provisional §4.12.1–§4.12.3 — consolidated §4.10 |
| **T-Q3-3** | Enforcement §4.13 Layer 3 **G-B-1** |
| **Formal B sign** | §4.13 four layers — decision maker fills gap disposition |
| **C** | Layer 4 — witnesses do not imply invalidation |

### 4.9 Compatibility checklist (before B leaves “provisional”)

| Check | Status |
|-------|--------|
| **B-Q1** — Publish semantics documented and match domain immutability | **Provisional yes** §4.3 |
| **B-Q1b** — `ReplaceStageDrawRules` Draft-only; R1 P2 UX | **Provisional yes** §4.6.1–§4.6.2 |
| Consistent with §11A `operationalDraw` Allow Ready | **Partial** — `ReplaceStageDrawRules` **exception** at sign (stage Draft); other draw ops still Allow Ready per matrix |
| D1 unaffected for qual/prog on upstream | **Yes** — B does not replace D1 |
| D2 — no edit when matches attached | **Yes** — guards already enforce |
| **R-Draw1-N2** — same entries + Applied → affectation replace OK | **Must remain** at sign |
| **B-Q2** R2 workbook §4.11 | **Provisional** |
| **B-Q3** §4.12 | **Provisional** — aligned §4.10 |
| **B sign** §4.13 | **Accepted** 2026-10-15 (workbook) |
| **G-B-1** / **G-B-3** | **Accepted** + **G-B-1-ENF** / **G-B-3-ENF** backlog — not runtime protection yet |
| No blanket auto-invalidation | **Explicit** — C / G-OPS3 only |

### 4.10 Arbitrage B — consolidated product rules (provisional, pre-sign)

Coherent package recommended for formal **B** sign (decision maker validates §4.13 gaps before signing).

| ID | Rule (product) | Workbook |
|----|----------------|----------|
| **B-Q1** | **`Published` = engagement métier** — not recalculable preview | §4.3 |
| **B-Q1b** | **`ReplaceStageDrawRules` Draft-only**; R1 correction = P2 cancel → recreate | §4.6.1 |
| **B-Q2** | Post-**Applied**: per-command contracts; **no** universal Deny; **no** global auto-invalidate | §4.11 |
| **B-Q3a** | **At most one non-cancelled draw** per phase (any status Draft/Published); **cancelled** history retained | §4.12.1 |
| **B-Q3b** | **`ConfigureDrawInputs` Draft-only** on draw entity; aligns with Published immutability | §4.12.2 |
| **B-Q3c** | Consumption **identity** witnesses (W-inputs, W-rules, W-applied, W-pool) — **not** invalidation algorithm | §4.12.3 |

**Consistency check (B-Q2 vs pool shrink):** Post-Applied **manual placement** may remain **Allow under condition** (D2, F1) — that is **occupancy**, not pool authoring. **Pool shrink** with Applied draw is **`ReplaceStageAffectationAuthoring`** → G-OPS3 target **refuse** (≠ N2 same-set replace). No contradiction if N2 boundary is preserved.

### 4.13 Formal **B** sign — decision record (**signed workbook** 2026-10-15)

Signing **B** (workbook level, still inside draft §11C until full §11C sign) must **not** imply all rules are already enforced in code. Use **four layers**:

#### Layer 1 — Product rules approved at B sign

_List = §4.10 table + invariant below._

**Invariant (B-Q3a / T-Q3-3):**

> A phase has **at most one** draw with `Status != Cancelled`. Draws with `Status == Cancelled` may accumulate in history. The rule applies to **non-cancelled** draws (Draft **or** Published), not only Published.

#### Layer 2 — Behaviour already aligned with code (no B-scope implementation)

| Item | Evidence |
|------|----------|
| `ReplaceStageDrawRules` Draft-only (stage) | `EnsureStructureMutable` |
| `ConfigureDrawInputs` / resolution edit Draft-only (entity) | `Draw.EnsureDraftMutable` |
| `Published` resolution immutable | `Draw.Publish` / `DrawImmutable` |
| **R-Draw1-N2** same `EntryId` set + Applied | `ReplaceStageAffectationAuthoringGOps3Tests` |
| SPA blocks second create when any non-cancelled draw | `resolveDrawCreateGate` / T-Q3-1 |

#### Layer 3 — Accepted gaps (explicit at sign — implementation scope separate)

Decision maker picks **per row**: **(a)** in scope before/with §11C implementation slice, or **(b)** deferred with known risk.

| Gap ID | Product target | Code today | **Sign choice** (2026-10-15) |
|--------|----------------|------------|------------------------------|
| **G-B-1** (T-Q3-3) | **Invariant:** ≤1 non-cancelled draw per phase (Draft or Published); cancelled history retained | Domain allows multiple; SPA only | **Accepted** — product contract **signed**; enforcement **not** claimed in code; **backlog G-B-1-ENF** (domain or application, not SPA-only; analyse create paths + concurrency) |
| **G-B-2** | No two concurrent **Published** without semantics | No domain guard | **Accepted** — subsumed by **G-B-1** contract; same backlog **G-B-1-ENF** |
| **G-B-3** (R-Draw1+) | Refuse pool **membership change** (shrink/change set) with **Applied** draw per G-OPS3 contract; **R-Draw1-N2** unchanged | No draw-refuse gate; shrink may succeed | **Accepted** — refusal contract **signed**; **distinct** from G-B-1; enforcement via **G-OPS3 reopen** / backlog **G-B-3-ENF** — **not** general invalidation (**C**) |

**T-Q3-3 — enforcement layer (provisional recommendation, not implementation):**

| Layer | Verdict |
|-------|---------|
| **SPA only** | **Insufficient** for a business invariant — bypass via API/tests |
| **Application** | **Viable** if all create paths use one guard; concurrency needs persistence/transaction strategy |
| **Domain** | **Preferred expression** of invariant; pair with app/infra for races |

**Product vs delivery (signed intent):** **B** fixes *what* must hold. *Where/how* (G-B-1-ENF) and *when* (G-B-3-ENF / G-OPS3) are **separate** delivery decisions. Backlog rows are **visible** — do not present rules as runtime protection until implemented.

#### Layer 3b — B-chapter implementation backlog (contract accepted; **not** auto-started)

| Backlog ID | Trigger | Scope (outline) | Authorizes code? |
|------------|---------|-----------------|------------------|
| **G-B-1-ENF** | **G-B-1** sign | Enforce ≤1 non-cancelled draw per phase; domain and/or application guard on all create paths; concurrency with persistence | **No** until §11C sign + explicit slice |
| **G-B-3-ENF** | **G-B-3** sign | Align with [G-OPS3](./g-ops3-arbitration-session.md) reopen — refuse incompatible `ReplaceStageAffectationAuthoring`; preserve **R-Draw1-N2** | **No** until G-OPS3 / §11C reopen |

**Not in B backlog:** invalidation / obsolete draw UX (**C**); **G-B-3** is **refuse mutation**, not auto-invalidate.

#### Layer 4 — Deferred to **C** (out of B sign scope)

- Invalidation algorithm, obsolete draw lifecycle, Materialize blockers beyond existing gates  
- **W-pool** conformance handling (D3-1) — not decided by B-Q3c  
- **R-Inv-1** and assignment orphan explicit states  

#### B sign status (workbook)

| Field | Value |
|-------|--------|
| **B sign (content)** | **Accepted** 2026-10-15 — Layers 1–3; G-B-1 + G-B-3 **distinct** |
| **§11C full sign (Git)?** | **Yes** 2026-10-15 (after **C** workbook) |
| **Code / implementation** | **Not** authorized by B sign — only **G-B-*-ENF** after §11C + slice decision |
| **Amends §11A?** | **At §11C sign:** `operationalDraw` nuance + draw uniqueness invariant + G-OPS3 contract alignment |

**Reopen B:** only via new product decision — not via **C** workbook.

---

## 5. Arbitrage C — Invalidation model (spec only; no implementation)

### 5.0 C — entry conditions (after B sign)

**Central question:**

> When an **allowed** mutation renders a draw **obsolete**, which **entity** is invalid, which **operations** are blocked, and how does the organizer **restore** coherence?

**Guardrails (do not violate in C):**

1. **No** implicit global invalidation (per draw / per dependency only).
2. **No** bypass of **D1**, **D2**, or production freeze on materialized matches.
3. **Do not merge** three concerns: **(a)** draw uniqueness (**G-B-1**), **(b)** pool-change **refusal** (**G-B-3** / G-OPS3), **(c)** obsolete-state handling (**C**).

**B is closed for arbitration:** C must not reopen B-Q1..B-Q3 or G-B sign choices.

### 5.1 Intent (principle — not mechanism)

When an **allowed** mutation makes existing **assignments** or **draw outcomes** incompatible:

1. Represent **invalidity explicitly** (not silent stale data).
2. **Preserve** independent assignments / manual edits outside the dependency set.
3. **Block Materialize** (and relevant Start paths) until resolved.
4. **Never** use invalidation to bypass **D2** on produced matches.

**D1 remains separate:** breaking a **Ready downstream feed contract** still **blocks** the mutation (or requires revert), not only local invalidation.

### 5.2 Arbitrage C — workbook (**signed** 2026-10-15)

**Scope:** obsolete / invalid **handling** when an **allowed** mutation (or detected drift) breaks coherence — **not** G-B-3 **refuse** (separate contract), **not** G-B-1 uniqueness, **not** implementation. **B closed** — do not reopen B-Q1..B-Q3.

**Method:** same as B — options → consequences → code today (factual) → open questions → provisional preference → sign only when coherent.

**Recommended order:** C1 → C2 → C3 → C4 → contradiction check → §5.5 C sign proposal.

#### 5.2.1 Limits (carry into every C decision)

| # | Limit |
|---|--------|
| L1 | **No** implicit global invalidation (default unit = **identified draw resolution**, not whole phase). |
| L2 | **No** bypass of **D1**, **D2**, or production freeze on materialized matches. |
| L3 | **(a)** G-B-1 uniqueness, **(b)** G-B-3 pool-change **refusal**, **(c)** C obsolescence — **three contracts**; do not merge. |
| L4 | **R-Draw1-N2** remains valid (same `EntryId` set, Ready, Applied) — not permission to change pool. |
| L5 | **G-B-1-ENF** / **G-B-3-ENF** backlog — **B sign does not** make enforcement or C behaviour live at runtime. |

#### 5.2.2 C1 — What becomes invalid? (unit of obsolescence)

| Option | Principle | Main risk | Code today |
|--------|-----------|-----------|------------|
| **C1-A — Targeted draw** | The **identified** draw’s published/applied **resolution** is **obsolete** (not deleted); dependents scoped to that draw | Must map dependencies per `DrawId` | No persisted “obsolete” flag; [`DrawAppliedState`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs) is **conformity** read (Published + occupancy match) — not obsolescence |
| **C1-B — Whole phase** | Stage declared globally inconsistent | Collateral block on independent work | **Not** used in code |
| **C1-C — Derived conformity only** | Stored resolution unchanged; **conformity** recomputed from witnesses (W-*) | Unstable UX if conformity rules opaque | Partial — `DrawAppliedState` / badges; no general assignment-invalid model |

**Provisional preference:** **C1-A**, optionally **surfaced** via **C1-C** witnesses (conformity false ⇒ “obsolete for use”, history retained). **Reject C1-B** unless a demonstrated dependency forces phase-wide block.

**Open (C1):** Persisted `Obsolete` on draw vs read-model only — **deferred to C sign** (technical storage ≠ product unit).

| C1 decision record | Value |
|--------------------|--------|
| **Provisional** | **C1-A** (+ conformity projection C1-C) |
| **Signed?** | **Yes** (workbook C sign 2026-10-15) |

#### 5.2.3 C2 — Which operations are blocked? (per operation, not blanket)

**Rule of thumb (provisional):** an operation that **consumes** a resolution as **valid** must **not** proceed when that resolution is **obsolete** for that consumption. **Unrelated** operations on the phase may continue.

Arbitrate each cell: **Allow** | **Deny** (+ diagnostic) | **Allow under condition**.

| Operation | Consumes resolution? | Provisional target when draw **obsolete** | Code today (summary) |
|-----------|-------------------|-------------------------------------------|----------------------|
| **`ApplyDraw`** (re-apply) | Yes | **Deny** or idempotent-only if still **conform** (B-Q2) | Idempotent when conform — [`ApplyDrawTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ApplyDrawTests.cs) |
| **Materialize** (batch) | Indirect via occupancy / roster / feeds | **Deny** with explicit reason if prerequisites include obsolete draw or invalid assignments | Gates: roster, groups, confrontation eligibility — **no** `DrawObsolete` code ([`StructureMaterializeAssembler`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureMaterializeAssembler.cs)) |
| **Start** (stage) | Format-specific | **Deny under condition** if start requires coherent occupancy tied to obsolete draw | Existing start guards |
| **`ReplaceStageDrawRules` / Configure / pool** | May invalidate inputs | **Separate axes:** G-B-3 **refuses** some pool changes; **allowed** mutations that orphan data → **C** (not blanket deny all edits) | Mixed — see B workbook |
| **Downstream competition ops** (qual apply, etc.) | Usually no | **Allow** unless edge consumes same occupancy | D1/D2 own contracts |
| **Manual placement** | Only if conflicts with obsolete resolution | **Allow under condition** — preserve independent slots (B-Q2, L1) | F1 + OPS guards |

**Not in C2 scope:** **G-B-3** — incompatible pool change should be **refused at mutation** (when ENF exists), not “allow + obsolete”.

**Open (C2):** Drawer vs command error vs hub persistent “attention” — align with G-OPS3 D3-3 orientation (mutation footer first).

| C2 decision record | Value |
|--------------------|--------|
| **Provisional** | Per-operation table above — **no** phase-wide freeze |
| **Signed?** | **Yes** (workbook C sign 2026-10-15) |

#### 5.2.4 C3 — Recovery paths (restore coherence)

| Path | When | Product rule |
|------|------|----------------|
| **R1 — Correct & recalc** | Draw entity still **Draft**; inputs/rules fixable | Regenerate resolution; **Published** path only via B **P2** if already published |
| **R2 — Cancel → recreate** | **Published** / regret commitment (B-Q1) | Explicit cycle; occupancy from prior Apply **not** auto-unwound |
| **R3 — Blocked by D2** | Matches **produced**; mutation would desync production | **Explicit limit** — dematerialize only via existing capabilities; **no** C shortcut around D2 |

**Provisional:** Default obsolete Applied draw → **R2** (or reconcile occupancy manually where domain allows); **R3** message must state **production** constraint, not generic “invalid”.

| C3 decision record | Value |
|--------------------|--------|
| **Provisional** | R1 / R2 / R3 as above |
| **Signed?** | **Yes** (workbook C sign 2026-10-15) |

#### 5.2.5 C4 — Conformance proof (what “obsolete” means)

Use **B-Q3c** witnesses — define **product** conformance, not storage:

| Witness | Data compared | Role in C (provisional) |
|---------|---------------|-------------------------|
| **W-inputs** | `Draw.Inputs` vs current composition / fixed promotions | Drift ⇒ draw obsolete **for apply/materialize consumption** |
| **W-rules** | `DrawRules` at generate vs current regulation | Drift ⇒ obsolete for **new** generate; may lock via existing L2 |
| **W-applied** | Published resolution vs occupancy | `DrawAppliedState` false ⇒ not “applied”; **orthogonal** to obsolete — need **obsolete ∧ still published** semantics |
| **W-pool** | Affectation set vs resolution entries | **G-B-3** target: **refuse** change when Applied (ENF); if mutation **allowed**, W-pool false ⇒ **C1-A** obsolete |

**Dependency data (checklist):** `EntryId` set, draw rules at generation, configured inputs snapshot, application state, format-specific occupancy — extend from tests when arbitrating scenarios §6.

**Open (C4):** Exact predicate for “obsolete” vs “never applied” vs “conform” — **D3-1** remains G-OPS3; **C** must not redefine **N2** (set equality).

| C4 decision record | Value |
|--------------------|--------|
| **Provisional** | Multi-witness; obsolete = **failed conformance** for identified `DrawId` |
| **Signed?** | **Yes** (workbook C sign 2026-10-15); **storage** remains Layer 4 open |

#### 5.2.6 Scenarios to resolve before C sign (link §6)

| ID | C touches | Notes |
|----|-----------|-------|
| **R-Inv-1** | **§5.2.8** — first C2 contract slice | **not** G-B-3 |
| **R-Mat-block** | Ready with invalid/obsolescent state | C2 Materialize |
| **R-Place-manual** | Placement independent of obsolete draw | L1 preserve |
| **R-Draw1+** | Pool shrink + Applied | **G-B-3 refuse** (primary); C **not** substitute if ENF missing — document **risk** until G-B-3-ENF |
| **R-Draw1-N2** | Same set | **Must remain Allow** — not obsolete |

#### 5.2.8 Reference case **R-Inv-1** — orphaned assignments & obsolete draw consumption (C2 workbook)

**Scenario (contract sketch):** On a **Draft** stage (form mutable per D4), the organizer **reduces** group capacity (or removes places). Entries remain on **removed** places → **orphaned assignments**. The mutation is **allowed**; coherence breaks afterward. If draw **D** exists whose resolution or applied occupancy **depends** on the removed structure, **D** is **obsolete for consumption** (C1-A provisional) — history retained.

**Directing principle (provisional, not signed):**

> An **allowed** mutation may render a draw resolution **obsolete** without being refused. No operation may **consume** that resolution as still **conformant**.

**Distinctions (do not merge):**

| # | Topic | Rule |
|---|--------|------|
| D1 | **G-B-3 ≠ C** | G-B-3 = **refuse** pool change post-Applied (B contract, G-B-3-ENF backlog). C / R-Inv-1 = **allow** mutation → obsolete **D**. Missing G-B-3 guard = **Δ** — C does **not** blanket-refuse mutations. |
| D2 | **Obsolete ≠ storage** | Define conformance failure first (pool, rules, inputs, application, orphans). C4 picks flag vs derived later. |
| D3 | **D2 wins** | Production present → **R3** message; no impossible recreate promise; no C bypass. |

**C2 matrix — R-Inv-1 (product target; not code claim):**

| Operation | Provisional target | Rationale |
|-----------|-------------------|-----------|
| View phase / history | **Allow** | Do not hide state |
| View draw **D** | **Allow** + diagnostic on **D** | Explain non-usability |
| Edit assignments / affectation | **Allow** under existing guards | No global mutation ban |
| **`ApplyDraw`** on obsolete **D** | **Deny** | No apply of incoherent result |
| **`Materialize`** when **D** / occupancy invalid | **Deny** + explicit diagnostic | No production on non-conformant resolution |
| **`Start`** / dependent op | **Deny** only if **consumes** **D** / orphan path | Dependency-based, not phase freeze |
| **Cancel → recreate** | **Allow** under lifecycle + **D2** | Recovery R2 |
| Ops without **D** dependency | **Allow** per own guards | L1 |

**Signability questions (provisional workbook answers):**

| # | Question | Provisional |
|---|----------|-------------|
| Q1 | **`ApplyDraw`** denies obsolete **D**? | **Yes** (idempotent only if still conform — B-Q2) |
| Q2 | **`Materialize`** checks conformance even if other prereqs OK? | **Yes** — explicit reason |
| Q3 | **`Start`** blocks only on real consumption of **D** / orphans? | **Yes** |
| Q4 | Diagnostic on **D** + in production prerequisites (not buried in generic counts)? | **Yes** — presentation TBD |

**Code today (factual, 2026-10-15):**

| Area | Observation |
|------|-------------|
| Orphan assignments | No domain invalid-assignment entity; hub diagnostics via [`StructurePlaceOccupancyIssues`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePlaceOccupancyIssues.cs) / analyzer — **≠** draw obsolete |
| **`ApplyDraw` / Materialize** | No obsolete predicate — **Δ** vs Q1–Q2 |
| **G-B-3** | Not on R-Inv-1 path (form change Draft) |

| R-Inv-1 status | **C2 provisional** — contract test _gap_ |

#### 5.2.9 Contradiction check (provisional — not runtime validation)

Confirms C does **not** conflate **mutation refuse**, **resolution obsolescence**, and **recovery limit**. Status: **coherent with C target — formal validation required** (obsolete gates **not** runtime-effective today).

| Scenario | Expected contract outcome | Contradiction to avoid |
|----------|---------------------------|-------------------------|
| **R-Draw1+** — pool shrink after **Applied** | **Refuse mutation** (G-B-3 / G-OPS3 / G-B-3-ENF) | **Do not** allow mutation then use **C** to mark draw obsolete |
| **R-Draw1-N2** — same `EntryId` set | **No** obsolescence; keep positive contract | **Do not** treat all post-Applied ops as invalid/refused |
| **R-C-R3** — D2 limit (matches produced) | **R3:** explain limit; **no** impossible recreate promise | **Do not** bypass D2 to restore conformance |

**Cross-checks:**

| Check | Requirement |
|-------|-------------|
| G-B-3 vs **C** | Two contracts — refuse vs allow-then-obsolete |
| Obsolete trigger | Only after **allowed** mutation (or drift) that **actually** breaks conformance for **D** |
| **ApplyDraw** / **Materialize** | Must not consume obsolete **D** as valid (§5.2.8 Q1–Q2) |
| Persistence | **C4 open** — flag vs derived; contradiction check does **not** decide storage |

| §5.2.9 status | **Accepted** by decision maker 2026-10-15 (coherence control; **not** runtime tests green) |

#### 5.2.7 Legacy mapping (old §5.2 checklist)

| Old ID | Absorbed into |
|--------|----------------|
| Persisted vs derived state | C1 open + C4 |
| Who invalidates whom | C1-A scope + C2 per command |
| Refuse vs allow+invalidate | **G-B-3 refuse** vs **C obsolete** (L3) |
| Diagnostics / drawer | C2 open |
| F1 / production | C2 + C3 R3 |

### 5.3 Dependency on A and B

| If … | Then invalidation spec must … |
|------|-------------------------------|
| **A-restrict** | Define invalidation primarily for **composition / draw / feed** paths in Ready |
| **A-keep** | Include **Draft placement** that becomes invalid before Prepare (optional warnings only vs hard invalid — product choice) |
| **B-strict** | Fewer Ready-side rule changes; invalidation focuses on composition, draw apply, placement |
| **B-contextual (provisional)** | Align witnesses with **draw state** — §4.6 B-Q1; **B-Q3c** lists consumption witnesses only — **C** decides invalidation |

### 5.4 G-OPS3 boundary

Arbitrage C **informs** D3-1 but **does not** authorize implementing the G-OPS3 command gate. Signed negatives **R-Draw1-N1..N3** remain the non-regression floor.

**Relationship to G-B-3 (signed B):** Incompatible pool change with Applied draw → **refuse mutation** (G-B-3 contract / G-OPS3). **C** covers cases where mutation is **allowed** but coherence breaks (orphans, drift, obsolete consumption) — **not** a second channel for the same G-B-3 case.

**Candidate C rule (signed at workbook C sign 2026-10-15):**

> For draw **D**, when conformance witnesses (§5.2.5) fail, **D**’s resolution is **obsolete for consumption**; operations in §5.2.3 that treat **D** as valid are blocked; recovery §5.2.4; independent placements outside **D**’s dependency set remain valid.

### 5.5 Formal **C** sign — four layers (**workbook signed** 2026-10-15)

**Vigilance:** **C** sign = product contract only — **not** coding authorization; **not** runtime conformance.

Use **four layers** (mirror §4.13 **B**).

#### Layer 1 — Product decisions (**signed** workbook C)

| ID | Decision (provisional workbook) |
|----|----------------------------------|
| **C1** | **C1-A** — obsolescence unit = identified draw resolution **D**; history retained; **not** whole phase (C1-B rejected) |
| **C2** | Per-operation block when consuming obsolete **D** (§5.2.3, **R-Inv-1** §5.2.8, Q1–Q4 **yes** provisional) |
| **C3** | Recovery R1 / R2 / R3 (§5.2.4); **D2** explicit limit |
| **C4** | Conformance witnesses W-* (§5.2.5); **storage TBD** at sign |
| **L3** | G-B-3 refuse **≠** C obsolete (§5.2.9) |
| **Candidate rule** | §5.4 paragraph — obsolete **for consumption** only |

#### Layer 2 — Already aligned (partial)

| Item | Evidence |
|------|----------|
| **R-Draw1-N2** | Test green — not obsolete |
| **D2** deny on attached matches | `StructureOperationalExitEditGuard` / G-OPS tests |
| **Draw conformity read** | `DrawAppliedState` (conformity ≠ obsolete — C must extend semantics) |
| **G-B-3 negatives** | R-Draw1-N1..N3 |

#### Layer 3 — Accepted implementation gaps (**arbitrated** — sign choices recorded)

| Gap ID | Contract (product) | Code today | **Sign choice** (2026-10-15 arbitration) |
|--------|-------------------|------------|------------------------------------------|
| **G-C-1** | **Obsolescence detection** — non-conform **D** identified **before** any consumption (witnesses §5.2.5 / C4) | No unified obsolete predicate | **Accepted** + backlog **C-ENF-Detect** |
| **G-C-2** | **`ApplyDraw`** — **deny** applying obsolete **D**; explicit diagnostic | No obsolete gate; idempotent when conform only | **Accepted** + backlog **C-ENF-Apply** |
| **G-C-3** | **`Materialize`** — **deny** when production path depends on obsolete **D** (or orphan prereq per R-Inv-1) | No draw-obsolete / orphan materialize gate | **Accepted** + backlog **C-ENF-Mat** |
| **G-C-4** | **Dependent ops** (e.g. **Start**) — block only when **consumes** obsolete **D**; independent ops **Allow** | Partial / format-specific | **Accepted** + backlog **C-ENF-Deps** |
| **G-B-3-ENF** | Pool change **refuse** after Applied (**B** contract) | **Δ** — no gate | **Reminder only** — **B** backlog; **do not** reopen B; **not** implemented by **C** |

**Layer 3b — C enforcement backlog (not started by Layer 3 arbitration):**

| Backlog ID | Delivers | Authorized by C sign? |
|------------|----------|------------------------|
| **C-ENF-Detect** | G-C-1 predicate + read/command surfacing | **No** — §11C full sign + slice |
| **C-ENF-Apply** | G-C-2 | **No** |
| **C-ENF-Mat** | G-C-3 (+ R-Inv-1 test when scoped) | **No** |
| **C-ENF-Deps** | G-C-4 | **No** |
| **G-B-3-ENF** | G-B-3 refuse (separate from C) | **B** chapter only |

**Runtime:** Accepting Layer 3 contracts **does not** mean obsolete protection is **effective** today.

#### §5.5.1 Pre-sign checklist (**validated** — C workbook sign 2026-10-15)

| # | Check | DM validation (2026-10-15) |
|---|--------|----------------------------|
| 1 | **Product contract** — C1–C4, L3, candidate rule unambiguous | **Confirmed** — product decisions distinct from technical choices |
| 2 | **Partial alignments** — N2 positive; R-Draw1+ refuse not obsolete-first; D2 → R3 no false recreate | **Confirmed** — §5.2.9 |
| 3 | **Explicit backlog** — every runtime **Δ** traced; no “already protected” claim | **Confirmed** — G-C-1..4 + C-ENF-*; G-B-3-ENF reminder only |
| 4 | **Technical choices open** — persistence vs derived; UX detail | **Confirmed** — Layer 4; C sign does not decide storage / UX / orphans |

**Scope of C workbook sign:** product contract + accepted gaps + backlog IDs — **not** §11C full sign; **not** `C-ENF-*` implementation; **B** unchanged.

#### §5.5.2 Sequence (workbook C)

1. ~~Fill Layer 3 sign choices~~ — **done** 2026-10-15  
2. ~~Validate **§5.2.9**~~ — **DM accepted** 2026-10-15  
3. ~~**C workbook sign**~~ — **2026-10-15**  
4. **§11C full sign** proposal — §11 (E1–E5)  
5. **Implementation** only after §11C full sign + **explicit first slice** (e.g. C-ENF-* or G-B-3-ENF — not bundled by default)

#### Layer 4 — Open technical choices (may remain open at C sign)

| Topic | Owner |
|-------|--------|
| Persisted `Obsolete` vs derived predicate | C4 + implementation |
| Diagnostic surfaces (drawer vs materialize hub vs command) | C2 / UX |
| Orphan assignment model vs draw-only obsolete | R-Inv-1 / domain model |
| Predicate composition (W-inputs ∧ W-pool …) | C4 / G-OPS3 D3-1 boundary |

#### C sign exit criteria (input to §11C full sign — see §11)

| # | Criterion | Status at C workbook sign |
|---|-----------|---------------------------|
| E1 | §5.2.9 contradiction check **accepted** by decision maker | **Met** 2026-10-15 |
| E2 | Layer 3 gap rows **disposition** filled (backlog, not silent) | **Met** — C-ENF-* |
| E3 | **R-Inv-1**, **R-Draw1+**, **R-Draw1-N2**, **R-C-R3** reflected in §6 | **Met** (workbook); §10 extend at full sign |
| E4 | **B** not reopened; G-B-*-ENF unchanged by C sign alone | **Met** |
| E5 | §11C full sign proposal: matrix amend + implementation slices named | **Next** — §11 |

#### C sign status (workbook)

| Field | Value |
|-------|--------|
| **Layer 3 arbitration** | **Done** 2026-10-15 (G-C-1..4; G-B-3-ENF reminder) |
| **§5.5.1 / §5.2.9** | **DM validated** 2026-10-15 |
| **C signed (workbook)?** | **Yes** — 2026-10-15 |
| **§11C full sign (Git)?** | **Yes** 2026-10-15 |
| **Implementation** | **Forbidden** until §11C full sign + named slice |

#### C sign decision record (workbook)

| Field | Value |
|-------|--------|
| **C sign (content)** | **Accepted** 2026-10-15 — Layer 1 (C1–C4, L3, candidate rule); Layer 3 (G-C-1..4 + C-ENF-* backlog); Layer 4 open |
| **DM validation** | §5.5.1 four checks + §5.2.9 — 2026-10-15 |
| **§11C full sign (Git)?** | **Yes** 2026-10-15 — E5; **SLICE-0-none** |
| **Amends §11A?** | **At §11C full sign only** — obsolescence contract + matrix §11.6 |
| **Code / C-ENF-*** | **Not** authorized by C workbook sign |
| **Reopen B?** | **No** |

---

## 6. Contractual reference scenarios (workbook)

Status **Draft** until linked to arbitrations A/B/C and tests. Extend §10 in [phase-mutability-matrix.md](./phase-mutability-matrix.md) after §11C sign.

| ID | Scenario | Touches | Expected (TBD per arbitration) | Existing tests |
|----|----------|---------|--------------------------------|----------------|
| **R-Inv-1** | Reduce group capacity; entries on removed places; optional draw **D** obsolete | C §5.2.8 | Orphans explicit; **D** obsolete C1-A; C2 Q1–Q4 provisional **Yes**; Materialize/Apply deny; independent ops Allow | _Gap_ (no contract test) |
| **R-D1b** | Upstream qual narrows; downstream Ready has placed entries | D1 | `ReadyConsumerWouldBreak` | `ReplaceQualificationRulesGOpsTests` |
| **R-Place-manual** | Manual slot assignment after Applied draw | A, C, G-OPS3 | Dependents only if pool change incompatible | Partial — GOps3 negatives |
| **R-Draw1+** | Shrink affectation with **Applied** draw | **G-B-3** | **Refuse** mutation — **not** C obsolete-first (§5.2.9) | Positive gap; **G-B-3-ENF** |
| **R-Draw1-N2** | Same entry set replace with Applied draw | C check §5.2.9 | **Allow**; **not** obsolete | `ReplaceAffectationAuthoring_allows_replace_with_same_entries_when_group_draw_applied` |
| **R-C-R3** | Recovery needs edit blocked by **D2** | C3 R3 | Explain limit; no D2 bypass | Related: **R-D2** deny ops |
| **R-Mat-block** | Ready with invalid assignments | C, F4 | Materialize disabled with explicit reason | _Gap_ |
| **R-D2** | Operational edit after partial materialize | D2 | Deny | `ReplaceStage*GOpsTests` |

### 6.1 Scenario note — “form change in Ready”

Examples that change **places, slots, rounds** are **structural** under §11A (**denied in Ready**). §11C scenarios must label the **mutation axis** (form vs composition vs placement vs draw) to avoid specifying impossible Ready behaviour.

---

## 7. Relationship to §11A, §11B, §12

| Layer | Role |
|-------|------|
| **§11A** | Stays in force until §11C **signed** as amendment |
| **§11B** | Unchanged; stop line on Deferred rows remains |
| **§12** | New gaps may be proposed after §11C sign (e.g. G-INV1, G-PLACE-READY) — **not** added in this draft |
| **§11C sign (future)** | Would publish: chosen A/B/C, amended matrix rows, new reference cases, and what implementation slices are authorized |

---

## 8. Suggested arbitration workshop agenda

1. **Arbitrage A** — **Done (workbook):** A-keep; UX copy per §3.5.
2. **Arbitrage B** — §4.10 rules; **B sign** §4.13 (fill G-B-1, G-B-3 disposition).
3. **Arbitrage C** — §5.2.8–§5.2.9; **C sign** §5.5 (Layer 3 fill); **do not** reopen B.
4. **Consolidation** — §11 full sign package; Notion mirror optional; matrix diff; **then** §11C full sign.

---

## 9. Maintainer checklist (before §11C signable)

- [x] Arbitrage A decided (**A-keep**); matrix §3 unchanged (2026-10-15)
- [x] **B-Q1** / **B-Q1b** provisional (§4.6.1)
- [x] **B-Q2** workbook provisional (§4.11)
- [x] **B-Q3** workbook provisional (§4.12 — no **C** algorithm)
- [x] Arbitrage **B** signed (workbook 2026-10-15) — §4.13; G-B-1 / G-B-3 accepted + ENF backlog
- [x] **C** workbook §5.2 drafted (provisional — not C sign)
- [x] **C** contradiction check §5.2.9 (provisional)
- [x] **C** Layer 3 arbitrated §5.5 (G-C-1..4 sign choices recorded)
- [x] **C** workbook signed — DM §5.5.1 + §5.2.9 (2026-10-15)
- [x] §11C full sign (Git) — E5 accepted 2026-10-15; **SLICE-0-none**
- [x] §10 extended in matrix with R-Draw1+, N2, R-Inv-1, R-C-R3, R-Mat-block
- [x] Notion Décision §11C — [mirror](https://app.notion.com/p/3fa75fb92d1081e2b96cc7a987799e3f) + link §11A page
- [ ] No code merged under “§11C assumed” before sign

---

## 11. §11C full sign — proposal package (no code)

**Purpose:** Separate **what is already decided** (A/B/C workbooks) from **what full §11C sign must publish** into normative Git (matrix §10–§11, document status) and **what implementation may start** (named slices only).

**E5 applied 2026-10-15** — see matrix §11C and [E5 record](./section-11c-e5-matrix-diff-proposal.md). §11A / §11B checkboxes unchanged; §11C publishes draw/obsolescence via matrix amend.

### 11.1 What workbook C sign did **not** change

| Topic | Status |
|-------|--------|
| **§11A / §11B** | Unchanged — still normative |
| **Arbitrage B** | Closed — not reopened by C |
| **Runtime** | No claim that G-C-* or C-ENF-* are effective |
| **G-B-1-ENF / G-B-3-ENF** | Still **B** backlog — same authorization rules |
| **C-ENF-*** | Backlog only — **forbidden** until §11C full sign + explicit slice |

### 11.2 Consolidated product decisions (for matrix / Notion mirror)

| Arbitration | Decision (summary) | Workbook ref |
|-------------|-------------------|--------------|
| **A** | **A-keep** — `operationalPlacement` Draft + Ready | §3.4 |
| **B** | Published = commitment; draw rules Draft-only; post-Applied per-command; ≤1 non-cancelled draw; G-B-3 refuse pool change | §4.10, §4.13 |
| **C** | **C1-A** obsolete unit **D**; per-op deny on consumption; R1–R3 + D2 limit; W-* witnesses; L3 vs G-B-3 | §5.2, §5.4, §5.5 |

### 11.3 Accepted gaps & enforcement backlogs (delivery catalog)

**Rule:** A backlog row is **visible contract** — not shipped behaviour until its slice is signed and implemented.

| Backlog ID | Product contract | Source | Typical slice scope (outline) |
|------------|------------------|--------|-------------------------------|
| **G-B-1-ENF** | ≤1 non-cancelled draw per phase | B §4.13 | Domain/app guard on create; concurrency |
| **G-B-3-ENF** | Refuse incompatible pool change with Applied draw | B §4.13 / G-OPS3 | `ReplaceStageAffectationAuthoring`; preserve N2 |
| **C-ENF-Detect** | G-C-1 — obsolete **D** before consumption | C §5.5 | Predicate + read/command surfacing |
| **C-ENF-Apply** | G-C-2 — deny `ApplyDraw` on obsolete **D** | C §5.5 | Command gate + diagnostic |
| **C-ENF-Mat** | G-C-3 — deny `Materialize` when path depends on obsolete **D** | C §5.5 | Materialize prerequisites; **R-Inv-1** tests when in slice |
| **C-ENF-Deps** | G-C-4 — dependent ops only | C §5.5 | Format-specific start / consumption paths |

**Open at full sign (product, not blocking workbook C):** slice **order**, bundling, and whether G-OPS3 reopen is one slice with **G-B-3-ENF** — **decision maker** at §11C sign or first implementation tranche.

### 11.4 Technical choices **remain open** (Layer 4 — not decided by C sign)

| Topic | Notes |
|-------|--------|
| Persisted obsolete flag vs derived predicate | C4; implementation |
| Diagnostic surfaces | Drawer vs hub vs command footer |
| Orphan assignments vs draw-only obsolete | **R-Inv-1**; may affect **C-ENF-Detect** / **C-ENF-Mat** |
| Witness composition | Boundary with G-OPS3 **D3-1** — do not redefine **N2** |

Full §11C sign **may** leave these open per row if matrix and slices name **minimum** behaviour for the first authorized slice.

### 11.5 E1–E5 — exit criteria for **§11C full sign**

| # | Criterion | Proposal status | DM action at full sign |
|---|-----------|-----------------|------------------------|
| **E1** | §5.2.9 accepted | **Satisfied** at C workbook sign | Record in matrix annex / changelog |
| **E2** | Layer 3 dispositions | **Satisfied** at C workbook sign | Same |
| **E3** | Reference scenarios in §6 → §10 | **Workbook done**; §10 extension **required** at full sign | Approve §10 rows for R-Inv-1, R-Draw1+, R-Draw1-N2, R-C-R3, R-Mat-block |
| **E4** | B not reopened | **Satisfied** | Confirm no B-Q* edits |
| **E5** | Matrix amend + named slices | **Met** 2026-10-15 — [E5 applied](./section-11c-e5-matrix-diff-proposal.md); **SLICE-0-none** | Recorded matrix §11C.4 |

### 11.6 Proposed matrix amendments (at §11C full sign)

**Verifiable diff (E5):** [section-11c-e5-matrix-diff-proposal.md](./section-11c-e5-matrix-diff-proposal.md) — line-level proposal for `phase-mutability-matrix.md` **§9–§11** (+ §12 backlog rows), each change tagged **NORM** / **AMEND** / **BACKLOG** / **META**. **Not applied** until DM accepts E5 (§8 of that doc).

| Area | Amendment intent (summary) |
|------|----------------------------|
| **§9.1.1** | Draw target policy table + enforcement-today column (**B** + **C** backlog) |
| **§3 footnote** | `operationalDraw` Allow Ready with B nuance (optional E5-2) |
| **§10** | R-Draw1+, N2, R-Inv-1, R-C-R3, R-Mat-block; clarify R-Draw1 |
| **§11C** | Full sign checklist, non-authorization table, **§11C.4** slice record |
| **§12** | G-B-1, G-B-3, G-C-INV deferred rows; G-OPS3 note |
| **§3 placement** | **No** change — **A-keep** |

### 11.7 Implementation authorization model (at full sign)

```
§11C full sign
    → publishes matrix + §10 cases (product truth in Git)
    → does NOT auto-start all backlogs
    → exactly one explicit "Slice N" record (Notion task or §11C changelog row):
         allowed backlog IDs, out-of-scope IDs, stop line unchanged for others
```

**Default stop line until slice:** all **G-B-*-ENF** and **C-ENF-*** forbidden.

### 11.8 Suggested full-sign workshop agenda

1. Walk §11.2 — any correction? (should be none without new arbitration)  
2. Accept §11.6 matrix diff (or trim)  
3. Accept §11.3 catalog — **pick first slice** or defer all implementation  
4. Flip document header **Status** and matrix §11C to **§11C signed (Git)**  
5. Optional: Notion **Décision §11C** mirror — link Git commit  

---

## 10. Changelog

| Date | Change |
|------|--------|
| 2026-10-15 | Initial draft workbook — open A/B/C, foundation F1–F6 |
| 2026-10-15 | Arbitrage **A-keep** recorded §3.4–§3.5; B/C still open; document remains **not signable** |
| 2026-10-15 | **B-contextual** provisional §4.3–§4.8; command matrix vs code; B-Q1..Q3 open; **not signed** |
| 2026-10-15 | **B-Q1 provisional:** Published = engagement métier; P1/P2/P3 §4.7 |
| 2026-10-15 | **B-Q1b provisional:** `ReplaceStageDrawRules` **Draft-only** (incl. Ready R0); R1 UX P2; **no code Δ** |
| 2026-10-15 | **B-Q2 workbook** §4.11 — R2 per-command (code + provisional target); **R-Draw1-N2** bounded; **R-Draw1+ Δ**; **C** still gated |
| 2026-10-15 | **B-Q3 workbook** §4.12 — triple boundary, multi-draw (B-Q3a), mutation matrix (B-Q3b), consumption witnesses (B-Q3c); SPA vs domain **Δ** |
| 2026-10-15 | **B sign package** §4.10 + §4.13 — four layers; T-Q3-3 → G-B-1; G-B-3 separate from uniqueness; B-Q2 vs pool shrink note |
| 2026-10-15 | **B sign accepted** (workbook) — G-B-1 + G-B-3 contracts; G-B-1-ENF / G-B-3-ENF backlog; **C** §5.0 opened; §11C not fully signable |
| 2026-10-15 | **C workbook** §5.2 — C1–C4 provisional; per-op C2; G-B-3 vs C split; B not reopened |
| 2026-10-15 | **R-Inv-1** §5.2.8 — C2 matrix + Q1–Q4 provisional; code Δ noted |
| 2026-10-15 | **§5.2.9** contradiction check; **§5.5** C sign proposal (4 layers); **R-C-R3** in §6 |
| 2026-10-15 | **Layer 3** arbitrated — G-C-1..4 realigned; backlog C-ENF-*; §5.5.1–§5.5.2 |
| 2026-10-15 | **C workbook signed** — DM §5.5.1 + §5.2.9; §11 full-sign proposal (E1–E5); **not** §11C full sign |
| 2026-10-15 | **E5 accepted** — matrix §9.1.1, §10, §11C, §12; **§11C full sign Git**; **SLICE-0-none** |
| 2026-10-15 | Notion Décision §11C + [implementation-plan-post-11c.md](./implementation-plan-post-11c.md) |
| 2026-10-15 | Phase A — [slice-1-g-b-3-enf.md](./slice-1-g-b-3-enf.md) draft fiche |
| 2026-10-15 | **SLICE-1-G-B-3-ENF** authorized — matrix §11C.4, Notion, fiche corrections A/B |
