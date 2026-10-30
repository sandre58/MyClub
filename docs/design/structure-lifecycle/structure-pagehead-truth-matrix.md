# Structure PageHead — truth matrix & `hubNextStep`

Status: **shipped (2026-10-18)** — Application resolver + `StructureViewDto.HubNextStep`, SPA PageHead, i18n `hubStep.*` / `structureCalm`, Design Lab specimen.  
Signed contract (§2) unchanged; resolver tiers and gate documented in linked Application contracts.

## 1. PageHead contract (implemented)

| Global situation | Pill | Tone | Example (FR) |
| ---------------- | ---- | ---- | ------------ |
| Blocking anomaly or structure invalid (danger) | Yes | `error` | Anomalies à traiter |
| Incomplete structure, no danger | Yes | `attention` | Structure incomplète |
| Exactly one reliable structural next step | Yes | `info` | Phase à préparer · Poules |
| Calm hub — no step to surface | Yes | `success` | Structure prête |
| Ambiguous, incomplete data, or `/attention` pending | No positive pill | — | Silence |
| Competition `Completed` / `Archived`, or zero phases | No pill | — | — |

**Semantics**

- Green = **no structural work to push at hub level**, not “an operation is available”.
- Info pill = **nominal next step** (not imperative, not clickable CTA).
- Negative / warning resolution **unchanged** (`resolveStructurePageHeadNoteKind` + presented problems + presentable attention).
- **Resolution order (SPA):** `error` → `attention` → (`attentionPhase === 'success'` then) `info` (`hubNextStep`) → `success` (`isStructureHubCalm`) → **silence**.
- Positive pills render **only when** negative and warning branches are false and `attentionPhase === 'success'`.
- Competition **`Completed` / `Archived`** or **zero phases** → **no pill** (`lifecycleClosed` / `stagesEmpty`).

## 2. Signed PageHead contract (2026-10-18)

| Rule | Decision |
| ---- | -------- |
| `error` / `attention` | Keep **strict priority** over any action pill |
| `info` | Only when Application resolver finds **one** next step: unique, **actually activatable**, correctly named |
| Ambiguity | `hubNextStep = null` → **prudent silence** (not success) |
| `success` | **Separate proof** of calm: all phases ≥ `Ready`, no remaining structural work to produce/apply, no presentable attention — **not** fallback for `null` |
| Closed competition | No positive pill when competition **Completed** / **Archived** (or zero phases) |
| Frontend | **Display only** `hubNextStep` + calm rules — **no** draw-gate or cross-phase priority recompute |

| Topic | Decision |
| ----- | -------- |
| Pause-specific PageHead copy | **No** |
| Overview `naturalProgression` priority | **Do not reuse** for Structure PageHead |
| SPA scan replicating priority | **Reject** as interim SoT |
| SoT for next step | **`hubNextStep` on `StructureView`** (Application), fail-closed |
| `readyForNextSlice` | **Not** synonymous with “Structure prête” |
| Linear P1–P8 table | **Not signed** — priority must reflect **useful next work**, with **context-dependent** ordering (§4) |

## 3. `hubNextStep` shape (Application — on wire)

```csharp
/// <summary>
/// Single hub-level structural next step for PageHead info pill; null when calm or ambiguous.
/// </summary>
public sealed record StructureHubNextStepDto(
    string Code,
    Guid StageId,
    string StageName);
```

Wire on `StructureViewDto`:

```csharp
StructureHubNextStepDto? HubNextStep = null,
```

**Hub scope (Structure-only):** phases, composition, draw, match materialization. **`BuildSchedule` / `RunSwissRounds` are not `hubNextStep` candidates** — calendar and Swiss progression stay on operational surfaces; `readiness.nextConstructionStepHint` may still reference them elsewhere.

**Signed labels (FR)** — grammar **à + infinitif** for lifecycle; draw/materialize keep object noun; i18n `structure.readiness.hubStep.*` / `*_solo` when `stages.length === 1` (presentation only). EN mirrors (`To complete` aligns with lifecycle CTA « Complete phase », not “close”).

| Code | Multi-phase (FR) | Solo phase (FR) |
| ---- | ---------------- | --------------- |
| `PrepareStage` | À préparer · {name} | À préparer |
| `StartStage` | À démarrer · {name} | À démarrer |
| `CompleteStage` | À clôturer · {name} | À clôturer |
| `ResumeStage` | À reprendre · {name} | À reprendre |
| `RunDraw` | Tirage à lancer · {name} | Tirage à lancer |
| `ApplyDraw` | Tirage à appliquer · {name} | Tirage à appliquer |
| `MaterializeMatches` | Matchs à créer · {name} | Matchs à créer |

**Activatability:** `enabled` / `commandEnabled` on the read model is necessary but not sufficient where domain gates exist (draw launch, apply consumability, Prepare checklist).

**Resolver:** `StructureHubNextStepResolver.Decide` invoked from `StructureViewAssembler` after the view is assembled. Tests: `StructureHubNextStepResolverContractTests`, `StructureViewAssemblerHubNextStepTests`, `StructureDrawCreateLaunchGateContractTests`.

## 4. Priority principles (signed — encoded in `StructureHubNextStepResolver`)

Priority must reflect **the next structurally useful work for the competition**, not the order of fields on `StructureView`.  
Scan stages in **`stages[]` order** (competition `StageIds`). Emit a candidate only when the step is **activatable** on that snapshot. If **more than one** stage yields a competing candidate **without** a signed dominance rule, return **`null`**.

| Arbitrage | Signed decision | Implementation note |
| --------- | ----------------- | --------------------- |
| ApplyDraw vs RunDraw | **Apply before Launch** | Apply only when apply is **authorized** (published, resolved, not applied — same family as `OverviewAssembler` draw actions) |
| Lifecycle vs materialize | **Materialize before Start** (and similar exploitation transitions) | **Prepare before materialize** when Prepare is the blocking transition on Draft (see §8.1) |
| Complete vs construction | **Complete not default** | Prefer draw / apply / materialize / schedule paths when still required; surface Complete only when it is the **expected** closure step, not merely lifecycle-allowed |
| StartStage vs draw (same phase) | **Draw before Start when draw is required** for initial confrontations; else lifecycle | Depends on format + draw engagement — resolver must branch on domain signals, not badge alone |

**Deprecated for implementation:** the earlier linear **P1–P8** stack (Resume → Prepare → Start → …) is **not** signed. Replace with a **tiered resolver** that applies the table above per stage, then picks a single hub winner or `null`.

**Illustrative per-stage evaluation order (draft — encode in Application tests):**

1. `ResumeStage` when revert enabled (Suspended).
2. `PrepareStage` when transition enabled (Draft).
3. `ApplyDraw` when authorized.
4. `RunDraw` when authorized (launch gate).
5. `Materialize*` when `materialize.commandEnabled`.
6. `StartStage` when transition enabled and no higher-priority construction remains on that stage.
7. `CompleteStage` when transition enabled and no higher-priority construction remains.

Tiers 3–7 are **context-sensitive**; exact predicates belong in the resolver + contract tests, not in the SPA.

## 5. Calm / `success` pill rules (SPA — `isStructureHubCalm`)

Show **`success` · Structure prête** (`readiness.structureCalm`) only when:

- `hubNextStep == null` (resolver output),
- PageHead not `negative` or `warning`,
- `attentionPhase === 'success'` (pending → **no** positive pill, **no** success),
- Presentable attention counts zero and no presented structure problems,
- `stages.length >= 1`,
- No stage `Draft` or **`Suspended`** (fail-closed: resume / ambiguity → info via API or silence),
- Not every stage `status` is `Completed`,
- Competition not `Completed` / `Archived` (handled via `lifecycleClosed` before calm),
- **`operationalSlicePending` false** — `readyForNextSlice` with `nextConstructionStepHint` `BuildSchedule` or `RunSwissRounds` (schedule/Swiss work remains, but out of Structure PageHead scope → **silence**, not success).

**Not** the resolver `else`: calm is a **separate** predicate in `structurePageHeadReadiness.ts`.

**Explicit non-calm cases (expect info pill or warning/error, not green):**

- Any stage `Draft`.
- Any activatable hub step the resolver would emit (§4).
- `readyForNextSlice == true` with a resolvable hub step (should map to info, not green).
- Construction frozen (`ConstructionFrozen`) with Draft stage while competition Running → typically **warning** or **error**, not green (align with lifecycle disabled semantics).

## 6. Truth matrix (scenarios)

| # | Scenario | Expected pill | Tone | SoT (target) | Ambiguity / fail-closed |
| - | -------- | ------------- | ---- | ------------ | ------------------------ |
| S1 | Single Draft, `PrepareStage` enabled | À préparer (solo) or À préparer · {name} | info | `HubNextStep` | — |
| S2 | Two Draft phases, both Prepare enabled | **None** (positive) | — | Resolver `null` | Multi-stage same tier (Prepare) |
| S3 | Suspended, `ResumeStage` revert enabled | À reprendre · {name} | info | `HubNextStep` | — |
| S4 | `DrawExecutionBadge` ToLaunch, gate OK | Tirage à lancer · {name} | info | Hub stage + draw gate | If gate false → no draw pill |
| S5 | Badge ToApply, apply authorized | Tirage à appliquer · {name} | info | Resolver + Overview parity | Badge without authorization → silence |
| S6 | `materialize.commandEnabled` | Matchs à créer · {name} | info | Stage `materialize` | If `commandEnabled` false → never |
| S7 | Running, Complete enabled, no draw/mat left on that stage | À clôturer · {name} | info | Lifecycle when §4 “expected closure” | Complete enabled but construction pending → info on construction, not Complete |
| S8 | Competition Running + stage Draft (frozen) | Structure incomplète / anomaly per diagnostics | attention/error | Existing PageHead synthesis | **Not** green |
| S9 | All stages ≥ Ready, none Completed, no hub step | Structure prête | success | Calm rules + `HubNextStep null` | — |
| S10 | `/attention` pending | No positive pill | — | `attentionPhase !== 'success'` | Existing gate |
| S11 | Competition Completed/Archived | No pill | — | `lifecycleClosed` | Existing gate |
| S12 | `PrepareStage` present but **disabled** | No action pill | — | `enabled == false` | Never invent |
| S13 | Primary `readyForNextSlice` + `BuildSchedule` / `RunSwissRounds` hint only | **None** (positive) | — | Resolver `null`; calm blocked in SPA | Operational surfaces own schedule/Swiss |
| S14 | Errors + enabled Prepare on one stage | Anomalies à traiter | error | PageHead negative wins | Positive suppressed |

## 7. Domain verification (targeted code pass — 2026-10-18)

Answers the three checks requested **before** `StructureHubNextStepDto` / resolver implementation.

### 7.1 Is `PrepareStage` a prerequisite to materialization?

**Yes, structurally.**

- `StructureMaterializeAssembler` only sets `commandEnabled` when the stage is **`Ready`** (`IsChampionshipOrGroupsMutable`, `IsCupMutable`: competition not closed + `stage.Status is StageStatus.Ready`). On **`Draft`**, materialize is not mutable → no hub materialize step.
- `PrepareStage.Execute` is the Application path **Draft → Ready**; read-side `ResolvePrepareTransition` mirrors the same gates (`CollectPrepareGateCodes`, attribution, graph).
- `StageStartReadinessGuard` blocks **Start** while eligible confrontations remain unmaterialized (`DisabledUnmaterializedConfrontations`), aligning “produce matches before exploitation” with product intent.

**Resolver implication:** on a **Draft** phase, **`PrepareStage` (when enabled) dominates** materialize on that phase. Materialize competes with **Start** on **Ready**, not with Prepare on Draft.

### 7.2 Is `CompleteStage` expected work or only lifecycle-allowed?

**Both, but the read model already separates them.**

- `StructureStageLifecycleAssembler.ResolveCompleteTransition` projects `CompleteStage` with `enabled: true` only when `matchProgress.CanComplete` (Running/Suspended, **no open matches**).
- When matches remain open, the same code is shown with `enabled: false` and `CompleteStage.ReasonOpenMatches`.

**Resolver implication:** signed rule “Complete not default” means: even when `CompleteStage` is **enabled**, the hub must **not** surface it if a **higher-priority construction step** still applies on that competition snapshot (draw, apply, materialize, schedule/Swiss). When enabled **and** no such step remains, Complete is the **expected** closure action.

### 7.3 Are `ApplyDraw` / `RunDraw` gates available in Application without SPA badge logic?

| Step | Application today | Gap for `hubNextStep` |
| ---- | ----------------- | --------------------- |
| **ApplyDraw** | `DrawExecutionBadge.ToApply` from `ResolveDrawExecutionBadge` (Published + not `DrawAppliedState.IsApplied`). Overview adds `ActionApplyDraw` only for **Resolved** published draws — resolver should **reuse that predicate**, not badge alone. | Small: align hub step with Overview draw-action eligibility + `ApplyDraw` consumability guards. |
| **RunDraw** | `StructureDrawCreateLaunchGate` projects `stages[].drawCreateLaunch` (`allowed`, `blockReasonCode`). PageHead emits `RunDraw` only when `allowed` is true. The SPA displays that field. | Closed on the hub row. |

**Conclusion:** Apply can be made authoritative with existing Application data + Overview parity. RunDraw uses the hub `drawCreateLaunch` projection — the frontend displays it.

## 8. SPA implementation (Structure hub N1)

| Piece | Location |
| ----- | -------- |
| Note kind (`error` / `attention` / `info` / `success` / none) | `structurePageHeadReadiness.ts` — `resolveStructurePageHeadNoteKind`, `isStructureHubCalm` |
| Info copy from DTO code | `structurePageHeadHubStepCopy.ts` → `readiness.hubStep.*` + `stageName` |
| Calm copy | `readiness.structureCalm` |
| Negative / warning copy | `structureAnomalyHintCopy.ts` — unchanged |
| Render | `StructurePage.tsx` — DS `Status` `context` · `outline` · `pill` |
| Visual QA | `design-lab/LabStructurePageHead.tsx` (long EN labels + responsive grid) |

**SPA does not:** scan phases for hub priority, recompute RunDraw gate, or map `readyForNextSlice` / `readyForMaterialization` to PageHead pills.

**i18n (PageHead):** `readiness.hubStep.*`, `structureCalm`, `anomalyHintTitle`, `warningHintTitle`. Legacy keys `readyForNextSlice`, `readyForMaterialization`, `readyForCupSkeleton` remain in `structure.json` but are **not** referenced by PageHead (readiness API fields elsewhere).

## 9. Test evidence

| Layer | Suite |
| ----- | ----- |
| Application | `StructureHubNextStepResolverContractTests`, `StructureDrawCreateLaunchGateContractTests`, `StructureViewAssemblerHubNextStepTests` |
| SPA | `structurePageHeadReadiness.test.ts`, `structurePageHeadHubStepCopy.test.ts`, `StructurePage.test.tsx` (PageHead scenarios) |

## 10. Related Git docs

- PageHead product contract (O4): `docs/guides/organizer-problems-o4-drawer-arbitration.md` (§ Structure PageHead).
- Lifecycle projection: `StructureStageLifecycleAssembler.cs`.
