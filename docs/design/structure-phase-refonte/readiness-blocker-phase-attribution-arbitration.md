# Readiness blockers — phase attribution & UI projection (arbitration)

**Status:** Contract **approved for implementation** (2026-10-18) subject to §8 pre-sign coherence checks and Notion decision record. **No code** until §7 checklist is complete.

**Scope:** Targeted fix for **attribution and projection** on Structure surfaces. Independent of Phase 3 recette closure and CI policy changes.

**Trigger:** Structure hub inconsistency on multi-phase seeds (e.g. **Démo Règlement**): PageHead shows global error (`CupBracketInvalid`), topology tiles show only local warnings, drawer label is correct but `stageId` may point at **primary** (Groups) instead of the **Cup** phase that failed validation.

**Related code (today):**

- Producer: `StructureViewAssembler` — dedupes `CupBracketInvalid` to **one** global code (`blockers.Contains` guard).
- Attention: `StructurePresentedAttentionCollector` — single item; `params.stageId = format.primaryStageId`.
- SPA: `structureReadinessSummary`, `stageWorstPresentedDiagnosticTone`, `structureReadinessActions` (`stages[0]` anchor).

---

## 1. Product decisions (signed direction)

### P1 — Global readiness severity (PageHead)

- Attributed blockers remain **competition-level readiness** facts (canonical list on `readiness`).
- **PageHead** keeps **error** (e.g. FR « Anomalies à traiter ») while **any** relevant blocker persists, **independent** of topology tile presentation.
- PageHead must **not** require “at least one tile shows danger” to show red.

### P2 — Owner phase (when identifiable)

- When the backend identifies **which Cup hub** failed bracket validation, that **stage** owns the diagnostic for phase-scoped surfaces.
- **Invalid** attribution: `format.primaryStageId`, `stages[0]` order, drawer-only heuristics.
- Reference: **Démo Règlement** — owner = **Phase finale** (Cup), not **Groupes**.

### P3 — Consistent projection per surface

| Surface | Rule |
|---------|------|
| **Topology tile** | **Danger** on each owner stage with an attributed blocker; other phases keep **local** diagnostics only. |
| **Phase fiche** | Attributed global blockers **only on owner fiche(s)**; other phases keep local warnings. |
| **Drawer « À traiter »** | Blocking rows with correct **`stageId`** per attributed entry; no spurious primary link. |
| **Readiness actions** | CTA on **owner** phase only (see §6.3). |

### P4 — `/attention` is not the source of truth

- `GET /attention` **projects** readiness (+ stage-presented issues).
- Same internal collection drives `blockerDetails` and collector rows.
- SPA must **not** re-run cup bracket rules or infer owner from drawer order.

### P5 — Unattributed blockers (true ambiguity only)

- **Ambiguity** = backend **cannot** identify owner (not “several invalid Cups” — see §6.1).
- Then: PageHead (+ competition-scoped drawer item if applicable); **no** phase tile/fiche falsely marked owner.

---

## 2. Target behaviour matrix (acceptance reference)

| Surface | Groups phase (Démo Règlement) | Cup phase — Phase finale |
|---------|------------------------------|---------------------------|
| **PageHead** | Global **error** while any `CupBracketInvalid` in canonical list | Same |
| **Topology tile** | **Attention** for local issues | **Danger** when this `stageId` is in `blockerDetails` |
| **Phase fiche** | Teams / draw / local warnings | Bracket invalid diagnostic + link to **Construction** (§6.3) |
| **Drawer** | No cup-bracket row tied to Groups | One blocking row per attributed Cup entry |
| **Readiness actions** | No cup-shape CTA on Groups | Action per owner `stageId` |

---

## 3. API contract — Option A (canonical)

### 3.1 Types

```ts
type StructureReadinessBlocker = {
  code: string;
  stageId?: string | null; // owner when known; null/omit = global-only (P5)
};
```

### 3.2 Wire shape (transition — §6.2)

```ts
interface StructureReadiness {
  // …existing flags…
  /** Legacy: unique codes for .Contains() consumers (Overview, flags). Not attribution. */
  blockers: string[];
  /** Canonical: one row per attributed failure; duplicate codes allowed. */
  blockerDetails: StructureReadinessBlocker[];
}
```

**Producer rules:**

| Rule | Detail |
|------|--------|
| Single internal collection | Assembler builds **one** list of `StructureReadinessBlocker`; derives `blockerDetails` and `blockers` from it. |
| `blockerDetails` | Full list; **one entry per invalid Cup hub** with that hub’s `stageId` (§6.1). **Duplicate `code` values with different `stageId` are required and valid** (not a schema error). |
| `blockers` | **Unique** codes only (preserve today’s `.Contains` semantics on Host and SPA). Derived from `blockerDetails` by **distinct `code`** — never by collapsing rows. |
| **Lift dedup guard** | Remove `if (!blockers.Contains(CupBracketInvalid))` on the **structured** path; that guard applied only to legacy `blockers[]` emission, never to `blockerDetails`. |
| No dedicated fields | No `cupBracketInvalidStageId`; generic model only. |

**SPA rules (transition):**

- Phase projection uses **`blockerDetails` only**.
- **Never** merge `blockers` + `blockerDetails`.
- **Never** use `blockers[]` to infer `stageId` (no attribution in legacy field).

**Wire strategy (choose one per release — not both by default):**

| Mode | When | Host JSON | Consumers |
|------|------|-----------|-----------|
| **A — Additive transition** (default for this slice unless audit proves otherwise) | Overview or other clients still require unchanged `blockers: string[]` shape in the same release | Emit `blockerDetails` **and** derived unique `blockers` | SPA phase attribution uses **`blockerDetails` only**; Overview keeps `blockers` codes-only |
| **B — Coordinated migration** | Pre-slice audit lists **all** consumers; single PR can update Host + SPA + tests together | `blockerDetails` only (or rename to single structured list) | No dual-field period |

**Rule:** the implementation slice **must document which mode (A or B)** in the PR before merge. Do not ship additive fields “just in case” while also planning an open-ended parallel path.

**Exit criteria (mode A only):** follow-up slice removes `blockers` when no external consumer needs codes-only array.

### 3.3 Consumer inventory (wire compat audit)

| Consumer | Uses `blockers` today | Migration |
|----------|----------------------|-----------|
| `StructureViewAssembler` | produces | P1 internal collection → both fields |
| `StructurePresentedAttentionCollector` | `.Contains(CupBracketInvalid)` | P2 from `blockerDetails` |
| `OverviewAssembler` | `.Contains` / `.Any` on codes | keep `blockers` unique codes |
| SPA `structureReadinessSummary` | `.includes('CupBracketInvalid')` | Global `shapeInvalid` / PageHead: **codes-only** via `blockers.includes` **or** `blockerDetails.some(code)` — **never** `stageId` from `blockers[]` |
| SPA `structureReadinessActions` | `.includes` + `stages[0]` | F4: **only** `blockerDetails` rows for attribution and CTAs |
| SPA types `StructureReadiness` | `blockers: string[]` | add `blockerDetails` |
| Tests (Application / Overview / StructureViewAssemblerIssues) | assert on `Blockers` | extend for `BlockerDetails` |

If audit shows **only** Structure hub SPA + attention need attribution, simultaneous breaking change (single structured list) remains an option — prefer dual field **only when** Overview or external clients require the legacy array unchanged in the same release.

### 3.4 `/attention` projection (not canonical)

Collector maps **from the same structured readiness collection** as `blockerDetails`:

| Mapping | Rule |
|---------|------|
| `source` | `entry.code` (e.g. `CupBracketInvalid`) |
| `params.stageId` | `entry.stageId` when set |
| Unattributed | Competition-scoped item; **no** invented `stageId` |
| Multiple same `code` | **One attention row per `blockerDetails` entry** — no merge to first stage |
| `targetType` / `targetId` | Must stay consistent with existing routing; test **independently** of `params.stageId` |

Option B (attention-only attribution) is **rejected** as canonical; attention follows readiness.

---

## 4. Acceptance scenarios (pre-dev gates)

### 4.1 Mono-phase Cup

Invalid bracket → one `blockerDetails` row with the only Cup `stageId`; tile, fiche, drawer, actions agree.

### 4.2 Multi-phase Groups → KO (Démo Règlement)

`blockerDetails` rows reference **Phase finale** `stageId`, not Groups, regardless of primary or `stages[]` order.

### 4.3 True ambiguity (P5)

When producer cannot assign owner: canonical row with `stageId: null` (or omitted); PageHead/drawer show global signal; **no** phase tile danger as owner. **Not** used when multiple Cups are invalid but each is identifiable (§6.1).

### 4.4 Recovery (full projection clear)

After bracket becomes valid, verify **all** derived surfaces in one refresh — not only that the code disappeared:

| Check | Expected |
|-------|----------|
| `blockerDetails` | No `CupBracketInvalid` rows (attributed or not) |
| `blockers` | No `CupBracketInvalid` code |
| Topology tile | No danger from this blocker on any stage |
| Owner fiche | No bracket-invalid banner / chrome |
| `GET /attention` | No blocking `CupBracketInvalid` rows |
| Readiness actions | No obsolete repair CTA |

Tests must assert **projections**, not a frontend-only cleanup path.

**Evidence for 4.2:** `regulation-demo` seed — `GET structure` + `GET attention` vs hub « Phase finale ».

---

## 5. Technical plan (authorized after §7 checklist)

### 5.1 Backend

| Step | Change |
|------|--------|
| P1 | Internal attributed collection; emit `blockerDetails` + derived unique `blockers`; one row per failing Cup hub. |
| P2 | `StructurePresentedAttentionCollector`: emit from **same collection** as P1; rules §3.4; remove `primaryStageId` fallback for cup shape. |
| P3 | Audit per-hub blockers (`MissingPotRules`, etc.) for same attribution pattern. |

### 5.2 Frontend

| Step | Change |
|------|--------|
| F1 | `blockerDetails` helper — filter by `stageId` / code; **never** read attribution from `blockers[]`. |
| F2 | Topology worst tone: danger when stage has attributed row (e.g. `CupBracketInvalid`). |
| F3 | Owner fiche: show diagnostic; deep link §6.3. |
| F4 | Readiness actions: one item per `blockerDetails` row (not `stages[0]`). |
| F5 | Drawer: regression tests for `params.stageId` + `targetType`/`targetId` routing. |

### 5.3 Tests

.NET assembler + collector parity; multi-Cup `blockerDetails`; Vitest projection + recovery §4.4; `regulation-demo` attribution.

### 5.4 Out of scope

Colour-only fixes; all diagnostics on every fiche; PageHead tied to tile danger; CI `typecheck:tests` policy; new repair UI if Construction deep link suffices.

---

## 6. Resolved contract arbitrations (integrate before sign-off)

### 6.1 Multiple invalid Cup phases (was open §6.1)

**Decision:** **One `blockerDetails` entry per invalid Cup hub**, each with its `stageId`.

```json
{
  "blockers": ["CupBracketInvalid"],
  "blockerDetails": [
    { "code": "CupBracketInvalid", "stageId": "cup-a" },
    { "code": "CupBracketInvalid", "stageId": "cup-b" }
  ]
}
```

- PageHead: single global error synthesis while **any** entry exists.
- Each owner tile/fiche/drawer row/action uses its `stageId`.
- **Ambiguity (P5)** is reserved for **unidentifiable** owner — not for multi-phase invalidity.
- **Implementation note:** today’s assembler **suppresses** second Cup failures via `blockers.Contains`; that guard must not apply to `blockerDetails`.

### 6.2 Wire compatibility (was open §6.2)

**Decision:** Prefer **mode A (additive transition)** for the first slice unless §3.3 audit, completed before coding, shows **mode B** is safe (all consumers migratable in one release).

- One producer collection → `blockerDetails` + derived unique `blockers`.
- SPA **phase attribution** → `blockerDetails` only.
- Legacy **codes-only** consumers → `blockers` only (no `stageId` inference).

### 6.3 Fiche anchor and action (was open §6.3)

**Decision:**

| Concern | Choice |
|---------|--------|
| Diagnostic owner | Cup hub `stageId` in `blockerDetails` |
| Presentation | Owner phase fiche (banner / readiness strip) |
| Action destination | **Construction** section — where topology/bracket is edited |

- Do **not** default to Confrontations (consequence, not configuration surface).
- **No new repair UI** in this slice if existing Construction path suffices.
- SPA precedent: `structureReadinessActions` already uses `section: 'construction'` for `CupBracketInvalid` — keep after attribution fix.

---

## 7. Sign-off checklist

- [x] **§8** pre-sign coherence confirmed (three checks).
- [x] §3.3 consumer inventory reviewed; wire mode **A** (default) unless pre-slice audit documents **B** — record in PR plan.
- [x] Collector routing tests scoped (§3.4 `targetType` / `targetId` + `params.stageId`).
- [x] Notion **Decision** — [[Play'Up] Structure readiness — blocker phase attribution](https://app.notion.com/p/3fd75fb92d10815eb9a2e9c340bb472f) (§9 text); links this spec.
- [ ] **Implementation slice** — authorized; start only when you explicitly open the technical slice (no code before sign-off was the prior gate).

---

## 8. Pre-sign coherence (final review)

| # | Check | Verdict |
|---|--------|---------|
| 1 | `blockerDetails` allows **multiple rows with the same `code`** and different `stageId` | **Yes** — §3.2 table, §6.1 example (`cup-a`, `cup-b`). Uniqueness applies only to legacy `blockers[]`. |
| 2 | Legacy / codes-only consumers use `blockers[]` **without** inferring attribution | **Yes** — §3.2 SPA rules; §3.3 Overview unchanged; today’s SPA uses `.includes` only for global flags (to migrate global check to `blockerDetails.some` optional, still without `stageId` from `blockers`). **Forbidden:** `stages[0]`, `primaryStageId`, merging lists. |
| 3 | Wire strategy is **explicitly one of** additive transition **or** coordinated migration | **Yes** — §3.2 wire strategy table; §6.2 default A with audit path to B; PR must declare mode. |

Acceptance scenarios §4 remain consistent: 4.2 multi-Cup uses multiple `blockerDetails` rows; 4.3 ambiguity is `stageId` null only when owner unknown; 4.4 clears both lists and all projections.

---

## 9. Notion Decision page (EN — copy for recording)

# Decision — Readiness Blocker Phase Attribution

**Status:** Approved for implementation, subject to the contract checks below  
**Date:** 2026-10-18  
**Specification:** `docs/design/structure-phase-refonte/readiness-blocker-phase-attribution-arbitration.md`

## Decision

Adopt `blockerDetails` as the canonical structured representation of readiness blockers, while retaining `blockers` as the legacy codes-only projection during the compatibility transition.

Each independently identifiable invalid Cup stage produces one attributed blocker entry containing its code and `stageId`. The legacy `blockers` collection remains unique by code.

`/attention` must project the same attribution. It must not infer ownership from `primaryStageId`, stage ordering, or independent bracket-validation logic.

## UI invariants

- PageHead severity remains global and independent of topology tile presentation.
- An attributed blocker appears as danger only on its owner stage's topology tile and fiche.
- Drawer entries and readiness actions target the same owner stage.
- Unattributed blockers remain visible at competition scope without falsely marking a stage as the owner.
- Recovery removes all derived blocker presentation and stale actions.

The existing Construction section is the intended resolution target, subject to verification of the current navigation contract.

## Implementation gate

Implementation is authorized only after the API compatibility inventory, collector routing checks, and acceptance scenarios in the specification are confirmed and this decision is recorded.

The implementation must remain a dedicated Structure readiness projection slice, outside the Phase 3 closure and unrelated CI policy work.

**Source of truth:** the linked arbitration specification and this decision record.

---

## 10. References

- Seed: `regulation-demo` / « Démo Règlement » — `ScenarioOrchestration.BuildRegulationHubDemoAsync`.
- Phase 3 recette: closed; unrelated.
- Notion decision: https://app.notion.com/p/3fd75fb92d10815eb9a2e9c340bb472f — Git spec remains the detailed contract.
