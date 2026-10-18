# Phase roster — inbound presentation audit (A3 / A2 / A1)

**Status:** Read-only audit (2026-10-19). **Product decision signed:** downstream `Ready` + roster incomplete solely due to qual flux not yet produced → informative **« En attente des qualifiés »** on the destination phase; otherwise **« Composition incomplète »**. **Start gate** unchanged (`PhaseRosterIncomplete` while `IsRosterComplete` is false). **No** materialize / `determinedCount` / domain changes.

**Related:** [composition-teams-resolution-vs-eligibility-arbitration-2026-10-18.md](./composition-teams-resolution-vs-eligibility-arbitration-2026-10-18.md) · [inbound-qual-resolution-recette-2026-10-18.md](./inbound-qual-resolution-recette-2026-10-18.md) · `ChampionshipSwissPhasePlaces.cs` · `StructureInboundCompositionAssembler.cs` · `DestinationSatisfaction.cs`

**Work order:** (1) this audit → (2) diagnostic A3 in read + tests → (3) gauge A2 → (4) gauge A1 → (5) regression.

---

## 1. Problem summary (observed)

| Signal | Symptom | Root cause (read/presentation) |
|--------|---------|--------------------------------|
| Matchs gauge | « Il reste N matchs à créer » in `attention` | Aside fallback uses **prospective** round-robin capacity at full Places N while `materialize.commandEnabled === false` and `PhaseRosterIncomplete` (not actionable production). |
| Teams gauge | « 4 équipes non résolues » with 2 directs + 2 qual waits | UI used `inboundComposition.resolvedCount` vs Places N; **ignores** composition roster entries already on the phase. Hub `compositionBandResolvedCount` has the same inbound-only bias for non-root phases (`StructureCompositionGaugeAssembler`). |
| À traiter | « Composition incomplète » on championship while groups not started | `PhaseRosterIncomplete` emitted from lifecycle on destination **Ready** without distinguishing **expected qual wait** vs **organizer deficit**. |

---

## 2. Operational gate (unchanged)

| Check | SoT (Application) |
|-------|-------------------|
| Roster complete? | `ChampionshipSwissPhasePlaces.IsRosterComplete(competition, stage, stages)` — active entries in `stage.CompositionEntries` vs `ResolvePlacesN(...)`. |
| Start blocked | `StageStartReadinessGuard` → `DisabledPhaseRosterIncomplete` when championship/swiss and `!IsRosterComplete`. |
| Materialize blocked | `StructureMaterializeAssembler` → `PhaseRosterIncomplete` when championship and `!phaseRosterComplete` (`determinedCount` stays 0). |

Presentation must **not** weaken these gates.

---

## 3. Hub fields (Structure GET) — what exists today

### 3.1 Roster vs capacity (A2)

| Field | Meaning | Use for A2 |
|-------|---------|------------|
| `compositionCapacity` | Places N | Denominator |
| `compositionEntryCount` | Composition membership size (includes withdrawn in composition per product arbitration) | **Not** Active-only roster; align with product rule for meter |
| `compositionEntryIds` / preview names | Wire identities | SPA lists |
| `compositionBandResolvedCount` | `StructureCompositionGaugeAssembler.ResolveResolvedSeatCount` | For non-root: **`inbound.ResolvedCount` only**, not `compositionEntryCount` — **misaligned** with `IsRosterComplete` for fed phases |

**A2 recommendation:** For roster-gap UI, numerator = count aligned with **`ResolveCompositionRoster`** (active composition entries), denominator = Places N from hub (`compositionCapacity` / same resolver as materialize). Prefer **reusing** assembler helper or projecting one hub field if SPA must not re-derive domain rules.

### 3.2 Inbound qual/prog flux (A3)

| Field | Meaning |
|-------|---------|
| `inboundComposition` | Omitted when root composition; else present on fed phases |
| `configuredVolume` | Sum of seat counts on all inbound rules |
| `resolvedCount` | Seats with non-null `entryId` on the wire |
| `rules[].sourceStageId` | Upstream stage |
| `rules[].qualificationPath` / `progressionPath` | Regulation projection |
| `rules[].seats[]` | Per seat: `entryId`, `displayName`, **`placed`** (destination placement / feed structural satisfaction — not the same as roster membership) |

**Important:** `resolvedCount < configuredVolume` is **necessary but not sufficient** for « attente qualifiés ». It also happens when qual is **computable** but not applied/consumed, or when progression/fixture outcomes exist but composition is incomplete.

### 3.3 Lifecycle / materialize (presentation triggers)

| Field | Role |
|-------|------|
| `lifecycle.transition.disabledReasonCode` | e.g. `PhaseRosterIncomplete` → folded into `presentedIssueCodes` on Ready |
| `materialize.disabledReason` | Same code family for Matchs strip |
| `presentedIssueCodes` | Gate 3 parity SPA + `StructurePresentedAttentionCollector` |

### 3.4 Attention catalogue (partial overlap)

| Source | When |
|--------|------|
| `QualificationPending` (`NeedsAttentionAssembler`) | Per qual path: standing + `QualificationApplier` yields instruction; `DestinationSatisfaction` is **Pending** (not Satisfied / Conflict). Requires **match slices** on source to build standings — **often absent** before source exploitation. |
| `PhaseRosterIncomplete` (presented) | Destination Ready, Start disabled — **independent** of whether `QualificationPending` was emitted on source |

**Gap:** Destination can show blocking « composition incomplète » while source shows **no** `QualificationPending` (e.g. groups Draft/Ready, zero finished matches). Supports **informative diagnostic on destination**, not source-only UX.

---

## 4. How inbound seats get `entryId` (classification inputs)

Pipeline: `StructureInboundCompositionAssembler` → `ResolveSeat` →

1. **Qual:** `FormPathResolution` witness, else `InboundQualificationSeatProjection.TryResolveSeat`, else schematic inbound case.
2. **`InboundQualificationSeatProjection`** returns non-null only if:
   - source status is **`Running` or `Completed`**, and
   - match summaries exist for source, and
   - standings + `QualificationApplier` succeed.

So for **qual-form-champ-ready-partial** (groups not exploited, champ Ready):

- Qual seats: `entryId == null` because projection **cannot run yet** — **upstream flux not produced**.
- Direct entries: already in `CompositionEntries` (domain test: count **2**) but **not** counted in `inbound.resolvedCount`.

This explains **2/4 roster** vs **0/2 inbound resolved** vs misleading **4 unresolved** if UI uses inbound resolved against Places N.

---

## 5. Proposed presentation classifier (testable predicate)

Pure read helper (name illustrative): `ClassifyPhaseRosterPresentation(view, stageHub)`.

### 5.1 Outputs

| Kind | User-facing (FR product) | Presented severity (target) | Start / materialize |
|------|--------------------------|---------------------------|---------------------|
| `Complete` | — | Hidden | Unblocked (roster) |
| `AwaitingInboundQualification` | En attente des qualifiés | **Info / progress** on **destination** | Still `PhaseRosterIncomplete` |
| `CompositionIncomplete` | Composition incomplète | **Attention / blocking** (existing D5 for `PhaseRosterIncomplete`) | Still `PhaseRosterIncomplete` |

Replace or **suppress** raw `PhaseRosterIncomplete` in `presentedIssueCodes` / drawer when kind is `AwaitingInboundQualification`, and emit a **new presented code** (e.g. `AwaitingInboundQualification`) mapped in SPA D5 matrix to info/progress.

### 5.2 Preconditions

- Phase is **downstream** (`!IsRootComposition`) and **Ready** (classifier scope per decision; Running/Suspended may reuse later).
- `!IsRosterComplete` (domain evaluation on stage + competition graph).

### 5.3 Core decomposition

```
placesN     = ResolvePlacesN (hub: compositionCapacity)
rosterCount = |ResolveCompositionRoster| (active composition entries)
rosterGap   = placesN - rosterCount   ( > 0 )

inboundSeats = flatten(inboundComposition.rules[].seats)
unresolvedSeats = seats where entryId is null
```

### 5.4 Rule `AwaitingInboundQualification` (strict)

**Do not** use `resolvedCount < configuredVolume` alone.

Proposed **sufficient** conditions (all must hold):

1. **`rosterGap > 0`**
2. **Every unit of roster gap is accounted for by unresolved inbound seats that are « not yet producible »** from upstream:
   - For each unresolved seat tied to a **qualification** rule, `QualificationProductState(source, path, matches) == NotYetProducible` (see §5.5).
   - **No** unresolved seat in state `ProducibleButNotConsumed` or `Conflict`.
3. **No « orphan » gap:** `rosterGap == count(unresolvedSeats where state == NotYetProducible)`  
   (equivalently: `rosterCount + count(producible-or-resolved inbound) == placesN` with no extra missing non-inbound slots).
4. **Direct / non-inbound portion of roster** already reflected in `rosterCount` (no requirement that inbound resolvedCount include directs).

For **qual-form partial** (2 directs, 2 qual seats, 0 qual resolved, places 4):  
`rosterGap = 2`, two qual seats `NotYetProducible` → **AwaitingInboundQualification**.

### 5.5 Per-seat qual product state (to implement in Application tests)

Evaluate per inbound qual seat (mirror attention logic without duplicating business rules in SPA):

| State | Meaning | Typical signals |
|-------|---------|-----------------|
| `NotYetProducible` | Flux not available yet | Source not Running/Completed; or no exploitable match rows; or standings/applier cannot run (`InboundQualificationSeatProjection` would return null **and** no `FormPathResolution` witness) |
| `ProducibleButNotConsumed` | Qual outcome knowable but not in destination composition | Instruction exists from standings; `DestinationSatisfaction` Pending; or witness missing but projection **would** return `EntryId` |
| `Conflict` | `DestinationSatisfaction.Conflict` | Wrong occupant / path mismatch |
| `Satisfied` | `entryId` on seat or satisfaction Satisfied | Counts toward inbound resolved / roster |

**`Running` on source does not imply `ProducibleButNotConsumed`:** matches may be ongoing, standings incomplete, or applier returns null — still `NotYetProducible` until producible.

Progression paths: same pattern using `InboundFeedSeatResolver` / fixture projection (include in tests when champ fed by prog).

### 5.6 Default

If `!IsRosterComplete` and not `AwaitingInboundQualification` → **`CompositionIncomplete`** (present existing `PhaseRosterIncomplete`).

---

## 6. Reference scenarios (test matrix for step 2)

| Id | Arrange | `IsRosterComplete` | Expected presentation | Notes |
|----|---------|--------------------|-------------------------|-------|
| **R-A** | `qual-form-champ-ready-partial` | false | AwaitingInboundQualification on **champ** | Groups not exploited; 2 directs in composition |
| **R-B** | Source Running, matches in progress, qual not determined | false | AwaitingInboundQualification | Not `Running` ⇒ producible |
| **R-C** | Source Completed, qual applied, 2/4 on roster | false | CompositionIncomplete if 2 qual seats producible but not consumed | After apply witness, inbound seats should have `entryId` — if still gap, anomaly |
| **R-D** | `qual-form-champ-ready-full` before materialize | true | No roster presentation code | Materialize may still block on unmaterialized confrontations — **out of scope** |
| **R-E** | Missing direct + qual wait | false | CompositionIncomplete | `rosterGap` ≠ not-yet-producible qual seats only |
| **R-F** | Withdrawn in composition | per product arbitration | Meter uses occupancy policy; classifier uses roster resolver | See composition-teams-resolution doc |

Automated seeds: Development `qual-form-champ-ready-partial` / `qual-form-champ-ready-full`; Application tests on `ClassifyPhaseRosterPresentation` with constructed graphs.

---

## 7. Downstream implementation notes (steps 3–4, not in this audit)

### A1 — Matchs gauge

When `materialize.disabledReason === PhaseRosterIncomplete` (and command disabled): **do not** show `stateMessage.partial` (« matchs à créer ») from prospective capacity. Show neutral non-actionable production state or 0 with caption « production indisponible » (copy TBD in i18n). **Do not** upgrade to `danger` solely for tone.

### A2 — Teams gauge

Numerator/denominator per §3.1; message like « 2 équipes en attente » (`inboundUnresolved` pattern with **correct** unresolved count). Align `compositionBandResolvedCount` if bande Équipes shares the same bug.

### A3 — Diagnostics / drawer

Implement classifier in **Application** (`StructurePresentedStageDiagnostics` or dedicated read), expose result via **new presented code** + severity in `StructureDiagnosticPresentation` (progress/info). SPA matrix entry parallel to `NonRootUnfed` progress rule. **Keep** lifecycle `disabledReasonCode` as `PhaseRosterIncomplete`.

---

## 8. Open points (no code yet)

1. **Exact presented code string** for « En attente des qualifiés » (new constant vs reuse `QualificationPending` on destination — **not recommended**, different semantics).
2. **Whether `placed: true` on inbound seat** without `entryId` should affect `NotYetProducible` (structural feed vs roster).
3. **Swiss / mixed qual+prog** graphs — extend test matrix beyond qual-form championship.
4. **Notion Décision row** optional — link this file after step 2 ships.

---

## 9. Audit conclusion

Existing hub data is **sufficient** to implement the classifier **without new domain rules**, by composing:

- `ChampionshipSwissPhasePlaces` (roster gate),
- `inboundComposition` seat-level detail,
- the same qual projection stack as `InboundQualificationSeatProjection` / `NeedsAttentionAssembler` (per-seat producibility).

**Critical vigilance:** never classify from `configuredVolume - resolvedCount` alone; always reconcile **`rosterGap`** with per-seat **producibility** states and exclude **conflict / consumable-but-missing** cases.

Next step: **step 2** — implement `ClassifyPhaseRosterPresentation` + Application tests **R-A…R-F** before any aside or SPA diagnostic change.
