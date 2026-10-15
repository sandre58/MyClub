# Structure diagnostic presentation — technical audit (pre-implementation)

**Prerequisite:** [structure-diagnostic-presentation-contract.md](./structure-diagnostic-presentation-contract.md) (signed).  
**Rule:** No authorization / §11B gate changes in this track.

---

## 1. Code inventory — structure issue codes

### 1.1 Analyzer + `structureDiagnosticPresentation` (`BY_CODE`)

| Code | In signed matrix | Current SPA `structureProductSeverity` | Current tone (SPA) | Host `ResolveSeverity` |
|------|------------------|----------------------------------------|--------------------|-------------------------|
| `SlotFeedMissing` | yes | blocking | danger | blocking (hidden if awaiting draw) |
| `SlotFeedInvalid` | no | blocking (default) | danger | blocking |
| `SlotFeedMultiple` | no | blocking (default) | danger | blocking |
| `CycleDetected` | yes | blocking | danger | blocking |
| `RoutingNondeterminism` | yes | blocking | danger | blocking |
| `NonRootUnfed` | yes | warning | **always progress** | warning |
| `DanglingQualificationTarget` | yes (pattern) | blocking | danger | blocking |
| `DanglingProgressionTarget` | yes | blocking | danger | blocking |
| `MissingQualificationDestinationSlot` | yes | blocking | danger | blocking |
| `MissingQualificationDestinationGroup` | yes | blocking | danger | blocking |
| `MissingProgressionDestinationSlot` | yes | blocking | danger | blocking |
| `MissingProgressionDestinationGroup` | yes | blocking | danger | blocking |
| `OverCapacityEdge` | yes | warning | attention | warning |
| `OverExtraction` | yes | blocking | danger | blocking |
| `PopulationShortfall` | yes | warning (no Ready bump) | attention | warning (+ U1 synth) |
| `SourcePopulationShortfall` | yes | warning | attention | warning |
| `ConfrontationUndecidable` | yes | blocking | danger | blocking |
| `AuthoredAwardUnreachable` | yes | Cup blocking / classifying warning | OK with `formatKind` | Cup blocking / else warning |
| `ExpectedRankUnreachable` | yes | warning | attention | warning |

### 1.2 Synthesis and blockers (presentation track)

| Source | Mechanism | Contract note |
|--------|-----------|---------------|
| `PopulationShortfall` (U1) | `stagePresentedIssueCodes` / `StructurePopulationVolume.ShouldSynthesizePopulationShortfall` | Must follow Ready visual danger; keep parity Host/SPA |
| `MissingPotRules` | `stageHasEngagedMissingPots` → attention item, not `structureIssues` | Classify in catalogue or keep separate; topology/readiness already count it |
| `CupBracketInvalid` | readiness blocker → attention | Not in matrix; keep danger until classified |
| `StructureGraphInvalid` | readiness / overview | danger presentation |

---

## 2. Context and masking — existing rules (reuse)

| Rule | SPA | Host | Contract |
|------|-----|------|----------|
| Hide `SlotFeedMissing` when draw engaged, not ToApply/Applied | `projectPresentedStageIssueCodes` / `isSlotFeedAwaitingDraw` | `StructurePresentedAttentionCollector` | ¹ preserve |
| Suppress `ExpectedRankUnreachable` when `AuthoredAwardUnreachable` present | `projectPresentedIssueCodes` | same | preserve |
| Hide `PopulationShortfall` / `OverCapacityEdge` on capacity tile banners when meter shows gap | `structureIssueBannersForTile` | N/A (fiche only) | presentation only |
| Hide `NonRootUnfed` on capacity when meter expresses absence | `meterExpressesPopulationAbsence` | N/A | align gauge tone with shortfall diagnostic when shown |

**Missing today for contract:**

- `stageStatus` in severity/presentation (Draft / Ready / Running).
- `incompletenessExpected` for NonRootUnfed (today: unconditional `progress` tone when warning).
- `operationalImpact` evaluator for footnote ² rows (no code path; **product + tests required before implementation**).

---

## 3. Gap analysis vs signed contract

| Requirement | Current state | Work |
|-------------|---------------|------|
| Population shortfall danger in Ready | Always warning | Extend presentation context + tests A.1–A.2 |
| Drawer/fiche parity | Drawer uses `enums.attentionSource.*` + tone without `formatKind` | Single catalogue + shared presentation + stage lookup |
| Topology always red | `structure-topology__issue { color: error }` | Worst presented tone per stage |
| PageHead negative on any issue | `hasAny` triggers generic anomaly hint | Only when worst presented severity is `danger` |
| Capacity gauge vs diagnostic | `ratioToneForOperationalMeter` independent | Drive pool-tone from same presentation as `PopulationShortfall` |
| NonRootUnfed progress | Always progress if warning | Gate on `incompletenessExpected` |
| Footnote ² danger | N/A | Define impact rules per diagnostic (see §4) |
| Short/long copy | `graph.issues.*` only; drawer enums | `titleKeyShort` / `titleKeyLong` under one namespace |
| Mutations | `errorWithStatus`, sparse `problems.mutation.*` | Map structure-related `Application.*`; drop HTTP suffix for organizers |

**G-UI1:** Already aligned for AuthoredAward + formatKind on fiche; **must propagate context to attention**.

---

## 4. Footnote ² — exploitation impact (to formalize before coding)

Contract: promotion to `danger` only with explicit, testable rules. **Unknown ⇒ attention.**

Proposed engineering placeholders (need product confirmation + reference cases):

| Diagnostic | Candidate impact signal | Test idea |
|------------|-------------------------|-----------|
| `OverCapacityEdge` | Running + outbound promises exceed places **and** phase has production or Start attempted | Application read / fixture state — **confirm** |
| `ExpectedRankUnreachable` | Ready/Running + placement award **requires** rank for imminent materialize/start | Format-specific — likely **unknown** until spec |
| `NonRootUnfed` | Ready/Running + eligible confrontation batch depends on unfed inbound | Overlaps deferred G-MAT3 — default **unknown** → attention |
| `PopulationShortfall` / `SourcePopulationShortfall` in Running | **Signed:** visual danger in Ready only; Running column is attention² unless impact rule passes | Do not promote on status alone |

**Action:** Notion task or short decision rows per diagnostic before step 3 implementation.

---

## 5. `stageStatus` mapping

Contract columns: Draft, Ready, Running.

| Hub `StageStatus` | Suggested presentation bucket |
|-------------------|-------------------------------|
| `Draft` | Draft |
| `Ready` | Ready |
| `Running` | Running |
| `Suspended` | **TBD** — treat as Running for exploitation, or own column |
| `Completed` | Hide structure construction diagnostics or force attention-only |

---

## 6. `incompletenessExpected` (NonRootUnfed progress)

Reliable signals to investigate (no implementation yet):

- Phase **Draft** with no inbound qual/prog paths configured and no affectation entries (analyzer already encodes “unfed”).
- Opposite: partial qual/prog authoring in progress → `attention` not `progress`.

Reference case to add at design time: multi-phase tree, root fed / child unfed in Draft (M2b world — presentation only).

---

## 7. Mutation `Application.*` — structure slice

**Mapped to `problems.lifecycle` today** (`lifecycleMutationMessageKey.ts`):  
`CompetitionClosed`, `ConfrontationUndecidable`, `Attribution`, `OverCapacityEdge`, `OverExtraction`, `RevertToDraftBlocked`, `RevertToReadyBlocked`, `RoutingNondeterminism`, `StartBlockedUnmaterialized`, `StageExploitationClosed`, `StructureCycleDetected`, `ReadyConsumerWouldBreak`, plus `MATCHS_LIFECYCLE_WIRE_CODES` subset.

**Structure mutations likely missing FR/EN `problems.lifecycle` or dedicated mutation keys:**

- `Application.MaterializationFailure`
- `Application.StructureNotMutable`
- `Application.DematerializationBlocked` / `DematerializationFailure`
- `Application.ReadyConsumerWouldBreak` — OK
- `Application.SlotFeedsInvalid` (if exposed on structure commands)
- `Application.InvalidStructureIntent`
- `Application.CupBracketNotPowerOfTwo`

Audit Host `PlayUpExceptionHandler` / structure command surfaces for **actual** wire codes returned to SPA.

---

## 8. Parity test plan (shared)

| Layer | Location |
|-------|----------|
| SPA unit | Extend `structureDiagnosticSeverity.test.ts` → full matrix × format × status × masks |
| SPA presentation | `structureDiagnosticPresentation.test.ts`, `functionalProblemPresentation.test.ts` |
| Host unit | `StructurePresentedAttentionCollectorTests` — mirror table-driven cases |
| Cross | Optional golden JSON: `(code, context) → severity` consumed by both (generated or duplicated with sync test) |

Include: Cup vs Championship AuthoredAward; Ready vs Draft PopulationShortfall; SlotFeedMissing hidden with draw; U1 synthetic shortfall.

---

## 9. UI wiring order (after presentation core)

1. `structureDiagnosticPresentation` + deprecate direct `structureProductSeverity` calls from surfaces.  
2. `functionalProblemPresentation` + attention partition (pass stage hub context).  
3. `StructurePresentedAttentionCollector.ResolveSeverity` → shared table or tested duplicate.  
4. Topology, PageHead (`resolveStructurePageHeadNoteKind` / `classifyReadinessHeaderTone`), operational strip pool-tone.  
5. i18n: short/long keys; remove drawer-only enums for structure codes; `formatStructureMutationMessage` without HTTP wrapper.

---

## 10. Non-regression boundaries

- Do not change `IsPrepareGateIssue`, materialize gates, G-OPS/G-MAT tests.  
- Presentation tests must not assert command success/failure changes.  
- Existing G-UI1/G-UI2 Done regression suite remains green after presentation-only refactors.

---

## 11. Exit criteria for “audit complete”

- [x] Code ↔ matrix mapping documented  
- [x] Footnote ² impact rules signed — [impact-rules.md](./structure-diagnostic-presentation-impact-rules.md) §1–2  
- [x] `Suspended` / `Completed` bucket signed — impact-rules §3  
- [x] Host wire-code inventory for structure mutations — impact-rules §5  
- [x] Reference cases named — impact-rules §4 (`R-Pres-*`)  

**Step 3** (shared presentation + parity tests) may start per impact-rules §6.
