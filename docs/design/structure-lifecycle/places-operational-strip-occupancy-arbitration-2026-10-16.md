# Places operational strip — occupancy vs diagnostic (arbitration)

**Status:** Product arbitration **validated** (2026-10-16). **§8 signed off** (2026-10-16). **SPA slice §9 implemented** (`drawLaunchable` on Places strip). **§10 addendum signed** (2026-10-18) — Places **0/N lançable** → ton **brand** + copy action (replaces §5.2/§9 **attention** for that case). **Implementation:** slice `places-strip-draw-launch-brand` — **shipped** (SPA).  
**Scope:** Structure fiche **Places** cluster (`RatioMessageGauge` in `StructurePhaseOperationalStrip`), `drawExecutionBadge`, and `SlotFeedMissing` presentation.  
**Related:** [structure-diagnostic-presentation-contract.md](./structure-diagnostic-presentation-contract.md) · [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md) (`R-Pres-M-1`, `R-Pres-M-1b`) · [truth-sources](../structure-phase-refonte/truth-sources.md) (Affectation / Tirage).

---

## 1. Three dimensions (do not conflate)

| Dimension | Question it answers | Source of truth (Git) |
|-----------|---------------------|------------------------|
| **Occupation** | How many schematic places are filled vs Places N? | `countSchematicOccupiedPlaces` / hub occupancy; strip ratio `occupiedPlacesCount` / `placesN` |
| **Diagnostic** | Is there a structural feed or occupancy **problem**? | Analyzer + `StructurePlaceOccupancyIssues` → `structureIssues`; presentation via D5 (`structureDiagnosticPresentation`) |
| **Lifecycle / workflow** | What is **expected** before the next step (draw publish, apply, start)? | `StageStatus`, `drawExecutionBadge`, `isSlotFeedAwaitingDraw` / `IsSlotFeedAwaitingDraw` |

The **gauge ratio** (filled number styling) expresses **occupation progress**, not diagnostic severity by itself. **Severity** for blocking UX comes from **diagnostics** (e.g. `SlotFeedMissing` when presented), not from inferring anomaly from `drawExecutionBadge` or `hasDrawRules` alone.

**Action signal on the Places strip** (message kind + pool tone for **0/N**) must follow **real draw launchability**, not badge alone and not checklist rows in parallel (§8).

---

## 2. `drawExecutionBadge` semantics

Server SoT: `StructureViewAssembler.ResolveDrawExecutionBadge` · SPA mirror: `drawUi.resolveTopologyDrawExecutionBadge`.  
**Applied** derivation: `DrawAppliedState.IsApplied` (Publish ≠ Apply).

| Badge | Meaning (product) | Places N fully filled? |
|-------|-------------------|-------------------------|
| **`ToLaunch`** | Draw rules engaged; no active non-cancelled draw yet | No implication |
| **`InProgress`** | Active draw in **Draft** (generation / resolution in progress) | No implication |
| **`ToApply`** | Draw **Published** + resolution **Resolved**, but stage slots/groups **do not yet match** draw results (Apply pending) | No — apply still expected |
| **`Applied`** | Published + resolved draw **aligned** with stage slots (slot entry IDs match results, or group placements reflected) | **No** — only guarantees alignment for **places covered by the draw**, not full bracket N/N |

**Decision (retained):** Keep this contract. **`Applied` confirms draw ↔ stage alignment for affected placements, not complete bracket occupancy.**

Code reference: `DrawAppliedState.cs` (remarks + `IsSlotApplied` / `IsGroupApplied`).

---

## 3. `SlotFeedMissing` as anomaly source of truth

| Decision | Detail |
|----------|--------|
| **Retain** | Do **not** promote strip severity because `drawExecutionBadge` became `Applied` or `ToApply` alone. |
| **Emit** | `CompetitionFlowAnalyzer` (feed resolution per slot) and/or `StructurePlaceOccupancyIssues` (schematic occupied &lt; Places N). |
| **Suppress (presentation)** | While `IsSlotFeedAwaitingDraw` — `hasDrawRules` and badge **not** `ToApply` or `Applied` — remove `SlotFeedMissing` from **presented** issue sets (host + SPA). Tests: `StructurePlaceOccupancyIssuesTests`, `StructurePresentedAttentionCollectorTests`. |
| **Severity when shown** | D5 matrix unchanged — e.g. `SlotFeedMissing` **danger** in **Ready** when visible ([impact rules](./structure-diagnostic-presentation-impact-rules.md) §2, `R-Pres-M-1`). |

**Decision (retained):** After the awaiting-draw window ends (`ToApply` or `Applied`), an empty or partial bracket **may** surface `SlotFeedMissing` if schematic occupancy is still below Places N — that is diagnostic truth, not badge inference.

---

## 4. `ToApply` and masking

**Decision (retained):** **`ToApply` is outside** the “draw awaiting” silence window (`IsSlotFeedAwaitingDraw` is false when badge is `ToApply`).  
Product reading: the result is **published** and must be **applied**; until occupancy catches up, missing feed / schematic gap **may** be diagnosed (subject to analyzer + occupancy projection).

No change to masking rules or D5 gravity in this arbitration.

---

## 5. Places strip — action signal gated on draw launchability (validated)

### 5.1 Pivot

| Concept | Definition (SPA) |
|---------|------------------|
| **Draw launchable** | `showDrawCta && drawGateReady && createGate?.ok === true`, where `createGate = resolveStageDrawCreateGate(...)` — same inputs as **Create + Generate** on the fiche / draw dialog (`StructurePhaseFiche.tsx`, `StructureDrawDialog.tsx`). |
| **Not launchable** | Draw rules engaged (`showDrawCta`) but gate closed (`createGate.ok === false`) or gate not yet computed (`createGate == null` while overview loading for Cup). |

**Decision (validated):** Use **`createGate.ok`** as the primary pivot for whether the Places strip should urge **action on the journey** (attention on **0/N**). Do **not** derive that signal from `hasDrawRules`, badge `ToLaunch` alone, or “all checklist rows met” unless those criteria are already encoded in the gate (§8).

`isSlotFeedAwaitingDraw` / `slotFeedAwaitingDraw` remain authoritative for **`SlotFeedMissing` masking** only — not for substituting launchability on the gauge.

### 5.2 Presentation matrix (Ready / Running, draw rules engaged)

Applies when `showDrawCta` and occupancy is known (`occupancyReady`, capacity known). Outside draw workflow, existing draft/suspend/completed copy is unchanged.

| Situation | Places message | Gauge (`resolvePlaceOccupancyTone` / `data-pool-tone`) |
|-----------|----------------|--------------------------------------------------------|
| **Tirage impossible à lancer** — amont / config / journey incomplete (`!createGate.ok` or gate pending) | **Info** — attente normale sur le parcours | **Neutre** (including **0/N**) |
| **Tirage lançable**, **0/N** occupé | **Attention** — action attendue sur le parcours (lancer le tirage) | **Attention** (not neutral) — **superseded by §10** (brand + copy explicite) |
| **Tirage lançable**, occupation partielle **k/N** | **Attention** sur les places restantes (copy existante) | Reflète l’occupation (`partial` / pool tone) — **pas** de diagnostic inventé |
| **`ToApply` / `Applied`**, `SlotFeedMissing` **presented** en Prêt | Gravité portée par le **diagnostic D5** (`slotFeedMissingActive`) | Idem D5 via `presentationToneToMeterKind` |

**Rejected (2026-10-16):** Adoucir visuellement **k/N** pendant `ToLaunch` / `InProgress` indépendamment de la lançabilité — le ratio reste une lecture d’occupation ; le signal d’action pour **0/N** est réglé par le gate, pas par un assouplissement orange global.

### 5.3 SPA (slice §9 — shipped)

| Area | Behaviour |
|------|-----------|
| **0/N** tone | `drawLaunchable` from fiche (`createGate.ok` when `showDrawCta`) — neutral + info when not launchable; attention + `partial` pool tone when launchable |
| **Masking** | `slotFeedAwaitingDraw` remains on fiche for `SlotFeedMissing` presentation only — not passed to Places meter |
| **Checklist** | Informational only — not a second gate |

---

## 6. Summary table (decisions)

| Topic | Decision |
|-------|----------|
| `Applied` | Keep current contract (draw alignment, not full N/N) |
| `SlotFeedMissing` | Remains anomaly SoT; badge alone does not create anomaly; masking unchanged |
| `ToApply` | Keep current behaviour (outside awaiting-draw silence) |
| Places **0/N** action signal | **Launchability** (`createGate.ok`), not badge / `hasDrawRules` alone |
| Places **0/N** when launchable (2026-10-18) | **Brand** gauge + caption **`RunDraw`**; message kind **info** — not attention (§10) |
| Places **k/N** when launchable | Occupation-led message + tone; no extra “soften” proposal |
| Checklist vs strip | Checklist ≠ parallel gate for strip (§8) |

---

## 7. Notion / changelog

When copying to Notion **Décision**, link this file and state:

- **No change** required for §2–4 (`Applied`, `SlotFeedMissing`, `ToApply`, masking, D5).
- **Validated:** §5 launchability-gated Places strip (replaces k/N visual soften proposal).
- **Next:** SPA slice §9 after §8 contract sign-off.

---

## 8. `createGate.ok` — contract verification (pre-implementation)

**Purpose:** Confirm the pivot matches “possibilité effective de lancer le tirage” on the concerned stage, and document overlap with `resolveDrawReadinessChecklist` without duplicating it in UI.

### 8.1 What `resolveDrawCreateGate` encodes

Authoritative comment in `drawUi.ts`: client gate for **Create + Generate** (Rerun workflow start). Pool = **`compositionEntryCount` only** (not schematic occupancy).

| Check | `createGate` reason when failing | Checklist item |
|-------|----------------------------------|----------------|
| Format supports draw | `unsupported` | — |
| No active non-cancelled draw | `active` | — |
| Pool &gt; 0 | `emptyPool` / `emptyPoolUpstream` | `teamsFillPlaces` (root: `pool === places`; fed: `configured === places`) — **not equivalent** when configured full but pool still 0 |
| `minimumTeams` | `belowMinimumTeams` | — |
| Groups: pots ≥ 2, `pool === pots × groups` | `missingPots`, `groupShape` | — |
| Cup: `pool === placesN` | `countMismatch` | `teamsFillPlaces` (root) |
| Cup: no direct assignment | `directAssignment` | — |
| Cup: no slot `entryId` occupants | `occupiedSlots` | `noManualPlacement` (same `countOccupiedSlots`) |
| Inbound qual/prog **resolved** vs configured | **Not in gate** | `teamsAllKnown` |

**Conclusion for product:** For Cup/Groups with draw rules, **`createGate.ok` is the same predicate that enables the primary draw CTA** (modulo `drawGateReady` / overview load). That matches “can launch draw now” in the UI sense.

### 8.2 Deliberate non-overlap (checklist ≠ second gate)

| Scenario | `createGate.ok` | Checklist | Strip (§5) |
|----------|-----------------|-----------|------------|
| Fed phase: configured = N, pool = 0, qual incomplete | `false` (`emptyPoolUpstream`) | `teamsFillPlaces` may be **met** (configured line) | **Info + neutral** (not launchable) |
| Fed phase: pool = N, places = N, qual not all resolved | **`true`** (if no occupied slots / DA) | `teamsAllKnown` **unmet** | **Attention on 0/N** if schematic empty — launchable per CTA |
| Root: pool &lt; N | `false` (`countMismatch`) | `teamsFillPlaces` unmet | Info + neutral |

**Decision (validated):** The Places strip follows **`createGate.ok`**, not “all checklist items met”. The checklist may still show amber rows while the strip urges action on **0/N** when the CTA would be enabled — that is acceptable and avoids two divergent “can proceed” rules on the same surface.

**Optional follow-up (not in this slice):** If product ever requires “launchable only when qual fully resolved”, that belongs in **`resolveDrawCreateGate`** (single gate), not in strip logic reading the checklist separately.

### 8.3 Inputs wired on fiche (reference)

`StructurePhaseFiche.tsx`: `resolveStageDrawCreateGate` with `compositionEntryCount`, `placesN`, `isRootComposition`, pots/groups, `minimumTeams`, `directAssignmentCount`, overview `slots` / `draws`, `formatKind`.  
`drawGateReady`: overview success, or Groups, or no draw CTA.

**Sign-off (2026-10-16):** §8.1–8.2 accepted by product — including fed + `teamsAllKnown` gap; no gate extension in this slice.

---

## 9. SPA slice (implemented)

**Slice id (working name):** `places-strip-launchability-gate`  
**Goal:** Wire Places strip message + pool tone to **`drawLaunchable`** (§5.1) while leaving §3–4 masking untouched.

### 9.1 Touch points

| File | Change |
|------|--------|
| `structurePlacementOperationalProps.ts` | Add `drawLaunchable?: boolean` (or `createGateOk` + `drawRulesEngaged` if clearer at call site). |
| `StructurePhaseFiche.tsx` | `drawLaunchable = showDrawCta && drawGateReady && createGate?.ok === true`; pass to operational strip / placement props. **Stop** using `slotFeedAwaitingDraw` for **0/N** meter kind and pool tone (keep passing it only where masking/diagnostics need it). |
| `structureOperationalStripMessages.ts` | In `resolvePlaceOccupancyMeterCopy` / `incompleteKind`: for `occupiedPlacesCount === 0` and `showDrawCta` context, `info` iff `!drawLaunchable`, `attention` iff `drawLaunchable` (subject to draft-like / `slotFeedMissingActive` ordering unchanged for §5.2 row 4). |
| `structureOperationalCopy.ts` | `resolvePlaceOccupancyTone`: same launchability rule for **0/N** neutral vs attention. |
| `StructurePhaseOperationalStrip.tsx` | `placesPoolTone` for 0 occupied: align with `drawLaunchable` instead of `slotFeedAwaitingDraw`. |

### 9.2 Tests to add (minimum)

| Case | Setup | Expected |
|------|--------|----------|
| **T-LAUNCH-0** | `drawLaunchable: false`, `occupiedPlacesCount: 0`, capacity N, Ready | Message kind `info`; tone **neutral** |
| **T-LAUNCH-1** | `drawLaunchable: true`, `occupiedPlacesCount: 0`, capacity N, Ready | Message kind `attention`; tone **attention** (not neutral) |

Extend `structureOperationalStripMessages.test.ts` and `structureOperationalCopy.test.ts`. Optional integration: fiche props from mocked `createGate` — only if cheap.

### 9.3 Out of scope for this slice

- P3 composition band visibility matrix implementation.
- Changing `resolveDrawCreateGate` or checklist criteria.
- Host/API changes to `SlotFeedMissing` emission or masking.

### 9.4 Done criteria

- §8 sign-off recorded (Notion or this doc status line).
- T-LAUNCH-0/1 green; no regression on `R-Pres-M-1` masking tests.
- Recette spot-check: fed qual incomplete (gate closed) vs pool full + vacant grid (gate open) on DevRunner structure fiche.

---

## 10. Addendum (2026-10-18) — 0/N lançable : brand + prochaine action (signed)

**Context:** Recette Structure (2026-10-18) — le ton **`attention`** sur la jauge Places quand le tirage est **lançable** mais le tableau est encore à **0/N** est lu comme un **warning**, alors que le parcours attend une **prochaine action** normale (alignement principe design : attention ≠ erreur ; « Prochaine action » en brand sur Overview).

**Scope:** Uniquement le cluster **Places** (`resolvePlaceOccupancyMeterCopy`, `resolvePlaceOccupancyTone`, `StructureOperationalRatioGauge` sur la bande placement). Hors scope : checklist tirage, drawer, diagnostics `SlotFeedMissing` (§3–4 inchangés).

### 10.1 Decision (validated)

| Situation | Message (FR) | Message kind / chrome | Gauge pool + fill |
|-----------|----------------|----------------------|-------------------|
| Draw rules engaged, **0/N**, **`!drawLaunchable`** | Copy existante `fiche.operational.placement.stateMessage.empty` (attente parcours) | **`info`** — pas d’icône attention | **Neutre** |
| Draw rules engaged, **0/N**, **`drawLaunchable`** | **« Lancer le tirage au sort »** — même intention que le CTA primaire tirage | **`info`** (pas `attention`) — **ne pas** utiliser `OverviewAttentionIcon` | **`brand`** (`poolTone` + `fillVariant: 'brand'` sur `StructureOperationalRatioGauge`) |
| **k/N** partiel, tirage lançable ou non | Copy partielle existante (`partial` / places restantes) | **`attention`** seulement si la situation relève d’un **écart réel** (places restantes à remplir, diagnostic D5 actif, etc.) — pas parce que le tirage n’a pas encore été lancé | Occupation-led (`partial` / pool tone) |
| Diagnostic **`SlotFeedMissing`** présenté en Ready | Inchangé — D5 | `presentationToneToMeterKind` | Inchangé §5.2 row 4 |

**i18n (implementation):**

- **FR:** réutiliser la clé existante `structure:actions.RunDraw` (« Lancer le tirage au sort ») pour le caption strip **ou** ajouter `fiche.operational.placement.stateMessage.emptyDrawLaunchable` avec la **même** chaîne (éviter deux libellés divergents).
- **EN:** `structure:actions.RunDraw` (« Run the random draw ») ou clé jumelle.

**Rejected:** Garder **`attention`** pour 0/N lançable — trop proche d’une anomalie.

**Retained from §5:** Pivot **`drawLaunchable`** (`createGate.ok`) inchangé ; seuls **ton + copy** changent pour le cas 0/N lançable.

### 10.2 Supersedes (doc only until slice shipped)

| Prior text | Replacement |
|------------|-------------|
| §5.2 row « Tirage lançable, 0/N » → attention + attention gauge | §10.1 table |
| §9.2 **T-LAUNCH-1** → `kind: attention`, tone attention | **T-LAUNCH-1b:** `kind: info`, caption = `RunDraw`, `poolTone: brand`, `fillVariant: brand` |
| §9.3 shipped behaviour for 0/N launchable | **Pending** re-implementation per §10 |

### 10.3 SPA slice (shipped)

**Slice id:** `places-strip-draw-launch-brand`

| File | Change |
|------|--------|
| `structureOperationalStripMessages.ts` | Branche `drawRulesEngaged && occupiedPlacesCount === 0 && drawLaunchable` → texte `RunDraw` (ou clé dédiée), `kind: 'info'` |
| `structureOperationalCopy.ts` | `resolvePlaceOccupancyTone` → retourner **`brand`** pour 0/N + `drawLaunchable` (au lieu de `partial` / attention) |
| `StructurePhaseOperationalStrip.tsx` | Passer `fillVariant="brand"` quand pool tone brand sur Places |
| `structureOperationalStripRatioTone.ts` | Si besoin : ne pas mapper `brand` message vers `partial` fill |
| Tests | Remplacer / compléter T-LAUNCH-1 ; régression T-LAUNCH-0 |

### 10.4 Recette (minimum)

| Id | Setup | Expected |
|----|--------|----------|
| **T-BRAND-0** | Ready, Groups/Cup, `showDrawCta`, 0/N, `createGate.ok === false` | Message attente neutre ; pas de brand |
| **T-BRAND-1** | Ready, draw engagé, 0/N, `createGate.ok === true` | Caption « Lancer le tirage au sort » ; jauge **brand** ; **pas** d’icône attention sur la ligne message |
| **T-BRAND-2** | Même que T-BRAND-1, puis occupation 1/N | Copy partielle / occupation — **pas** le message RunDraw |

### 10.5 Notion

Optional one-liner in **Décisions** DB: « Places 0/N lançable = prochaine action (brand), pas warning » → link **this §10**.

---

## 11. O3 — schematic occupancy projection (**validated 2026-10-18**)

**Problem:** strip `occupiedPlacesCount` is computed on the SPA from schematic cases, duplicating Application rules in `StructurePlaceOccupancyIssues.CountOccupiedPlaces`.

**Decision (O3):**

| Phase | Deliverable |
| ----- | ----------- |
| **O3-A** | Shared test vectors + Application parity tests (FE `structurePlaceOccupancy.test.ts` ↔ backend counter) |
| **O3-B** | Hub field **`SchematicOccupiedPlaceCount`** on `StructureStageHubSummaryDto`; strip prefers hub when present |

**Not hero:** occupancy ratio stays on the Places strip only ([hero matrix](./structure-phase-hero-stats-matrix.md) decision A).

**Reference:** [structure-phase-hero-post-arbitrations-2026-10-18.md](./structure-phase-hero-post-arbitrations-2026-10-18.md) · [truth-sources](../structure-phase-refonte/truth-sources.md).
