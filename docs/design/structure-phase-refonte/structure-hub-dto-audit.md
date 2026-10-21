# Structure hub — DTO & backend audit (2026-10-15)

Scope: **GET** `StructureViewDto` / `StructureStageHubSummaryDto`, Application assemblers, and Play’Up web consumption after the phase fiche refonte. Not a breaking-change plan by itself — lists verified usage and removal candidates.

## Sources of truth

| Layer | SoT |
|--------|-----|
| HTTP contract | `MyClub.PlayUp.Application/Reads/StructureViewDto.cs`, Host `StageEndpoints` / competition structure route |
| Assembly | `StructureViewAssembler.cs`, `StructureInboundCompositionAssembler`, `StructureMaterializeAssembler`, `StructureDematerializeAssembler`, lifecycle assemblers |
| Web types | `src/PlayUp/web/src/types/structure.ts` (camelCase mirror) |
| UX | Phase fiche (`StructurePhaseFiche`), hub shell (`StructurePage`), dialogs, schematic query (separate `StageSchematicDto`) |

Automated guards: `StructureViewAssembler*Tests`, `StructurePlacesContractTests`, `StructureMaterializeAssemblerTests`, `StructureStageLifecycleAssemblerTests`, `StructurePage.test.tsx`, `structureHostStructureIssues.test.ts`.

---

## `StructureViewDto` (competition-level)

| Field / section | Web usage | Notes |
|-----------------|-----------|--------|
| `competitionId`, `name`, `status` | Hub shell, fiche | — |
| `participants` | Composition, draw, manual place, rails | `entries`, `activeCount`, `occupyingCount` |
| `format` | Stage selection, deep links, badges | `primaryStage*` when multi-phase |
| `regulation` | Competition-level regulation dialogs (legacy competition strip); stage row duplicates most tokens | Still required for competition-wide editors |
| `structure` (`StructureTopologySummaryDto`) | `structureResolveStages` (N1 tile facts), skeleton dialog `matchGenerationFormat` | **Primary-stage snapshot** — not a substitute for `stages[]` on N2 |
| `actions` | Hub `AddCompetitionStage`, entry actions | Distinct from `stages[].actions` |
| `readiness` | PageHead notes, `StructureAnomalyHint`, overview cross-links | All flags referenced |
| `stages[]` | Fiche, all phase dialogs | Main N2 payload |
| `shortName`, `logoMediaId`, `scheduled*` | Shell / metadata | Low traffic but wired |

No orphan top-level sections identified for removal without a product decision on competition-level regulation UI.

---

## `StructureStageHubSummaryDto` — consumption summary

**Actively used on fiche / dialogs** (non-exhaustive): `lifecycle`, `materialize`, `dematerialize`, `formatKind`, `compositionCapacity`, `compositionPreviewNames`, `compositionEntryIds`, `affectation*`, `inboundComposition`, `qualificationIntents` / `qualificationPaths`, `progressionIntents` / `progressionPaths`, `placementAwards`, `structureIssues`, `structureIssueSemantics`, `drawExecutionBadge`, `defaultsBinding`, `confrontationSegments`, `defaultTieFormat`, topology (`groupCount`, `roundCount`, `matchdayCount`, `slotCount`, `teamCount`, `matchCount`), regulation tokens for strip/digest, `actions`, `directAssignmentCount`, `placesPerGroup`, `swissRoundCount`.

### Removed in this audit tranche

| Field | Backend | Web | Action |
|-------|---------|-----|--------|
| `compositionPreviewOverflow` | Always `0` (`EntrySetProjection.PreviewOverflow`) | Type only; **zero reads** | **Removed** (API + TS) |

### Dual projections (feeds migrated; API still dual)

| Pair | Web read model | Backend |
|------|----------------|---------|
| `qualificationPaths` / `qualificationIntents` | **`structureStageQualProgProjection`**: non-empty `*Intents` wins; else paths. Used by population volume, inbound detection, Qualifiés/Sorties path feeds, hub graph edges. Dialogs still map paths → intent when intents absent. | Both assembled; writes via intent PUTs |
| `progressionPaths` / `progressionIntents` | Same projection; prog volume = `expandedPathCount` on intent | Both assembled |

**API (2026-10-15)**: GET hub exposes **intents only** (`qualificationIntents` / `progressionIntents`); flat `qualificationPaths` / `progressionPaths` removed from `StructureStageHubSummaryDto`. Legacy path-list domain rules are synthesized into intents on read (`StructureQualProgHubIntentsMapper`). Path counts on the DTO still come from expanded domain paths.

### `StructureStageHubSummary` — field matrix (web `src/`, grep 2026-10-15)

| Field | Consumers (representative) | Keep / notes |
|-------|---------------------------|--------------|
| `stageId`, `name`, `status` | Hub, fiche, all dialogs | Keep |
| `teamCount`, `matchCount` | Topology cards, regulation digest, schematic context | Keep |
| `groupCount`, `roundCount`, `matchdayCount`, `slotCount` | `structureTopologyFacts`, schematic, Places N | Keep |
| `numberOfPeriods`, `durationPerPeriod`, `halfTimeDuration` | Regulation strip / dialogs | Keep |
| `hasExtraTime`, `extraTime*`, `hasPenaltyShootout`, `penalty*` | Regulation | Keep |
| `hasStandingRules`, `winPoints`, `drawPoints`, `lossPoints`, `rankingCriteria` | Standing editors, digest | Keep |
| `hasDrawRules`, `drawMode`, `numberOfPots`, `drawExecutionBadge` | Draw UI, operational strip | Keep |
| `hasQualificationRules`, `qualificationPathCount` | Hub edge labels, empty states (`StructurePage`) | Keep (counts; not a substitute for edges) |
| `hasProgressionRules`, `progressionPathCount` | Same | Keep |
| `hasTieFormat`, `numberOfLegs`, `aggregateScoring`, `hasAwayGoalsRule`, `hasTie*Shootout` | Tie format dialog, digest | Keep |
| `hasPlacementAwardRules`, `placementAwardCount`, `placementAwards` | Placement award dialog, sorties placement rail | Keep |
| `formatKind`, `swissRoundCount` | Badges, matchs guidance, schematic | Keep |
| `forfeitWinnerGoals`, `forfeitLoserGoals` | Match rules dialog | Keep |
| `numberOfSeeds`, `drawConstraints` | Draw dialog | Keep |
| `defaultsBinding`, `confrontationSegments` | Regulation strip (frame binding) | Keep |
| `actions` | Gating mutations on fiche / strip | Keep |
| `qualificationIntents`, `progressionIntents` | Authoring read model (GET); feeds, dialogs, sorties | Keep |
| `structureIssues`, `structureIssueSemantics` | Anomaly hint, diagnostic icons | Keep |
| `directAssignmentCount` | Composition / places | Keep |
| `compositionEntryCount`, `compositionEntryIds`, `compositionPreviewNames`, `compositionIneligibleCount` | Draw, composition band, manual place | Keep |
| `compositionCapacity`, `placesPerGroup` | Occupancy meter, Places N (`structurePlaces`) | Keep |
| `affectationEntryCount`, `affectationEntryIds`, `affectationPreviewNames`, `affectationIneligibleCount` | Population / affectation tile | Keep |
| `isRootComposition` | Qualifiés visibility, draw eligibility (`drawUi`, `structurePlaces`) | Keep |
| `materialize`, `dematerialize` | Aside Matchs strip | Keep |
| `lifecycle` | Phase header lifecycle | Keep |
| `inboundComposition` | Qualifiés resolved seats (server-side inbound feed) | Keep |
| `defaultTieFormat` | Type + `StageTieFormatDialog` only — **not** read from hub stage row in structure pages | Keep on DTO; hub UI uses confrontation segments |

### Duplication (intentional, not dead)

- **Competition `structure.*` vs stage topology fields**: N1 hub card uses primary-stage `data.structure`; N2 fiche uses selected `stage.*`. Same metrics, different scope — not safe to delete one side without UX change.
- **Regulation on `StructureViewDto.Regulation` vs per-stage match/standing fields**: competition editors vs phase digest — both needed.

### Low-traffic but legitimate

- `compositionEntryIds` — manual placement pool (`StructureManualPlacementDialog`)
- `numberOfSeeds`, `drawConstraints` — regulation / draw dialogs
- `boundFixtureId` on placement awards — schematic overlay reads

---

## Backend surface (Structure-related reads)

| Component | Role | Dead? |
|-----------|------|-------|
| `StructureViewAssembler` | Hub DTO | Active |
| `StructureInboundCompositionAssembler` | Qualifiés band | Active |
| `StructureMaterializeAssembler` / `Dematerialize` | Aside Matchs | Active |
| `StructureStageLifecycleAssembler` | Header lifecycle | Active |
| `StructureDiagnosticPresentation` | Issue semantics | Active |
| `StructurePresentedAttentionCollector` | Needs attention | Active |
| `OverviewAssembler` | Reuses structure view | Active |
| `StageSchematicDto` + schematic endpoint | Phase schematic viewport | **Separate** from hub — not duplicate of hub |

No legacy “old structure page” Host endpoints found beyond the single structure GET and existing stage mutation routes.

---

## Recommended follow-up (prioritized)

1. **Done**: Drop `compositionPreviewOverflow`.
2. **Done**: Web feeds / volume / inbound detection use `structureStageQualProgProjection` (intents preferred).
3. **Contract test**: Optional snapshot test on `StructureStageHubSummaryDto` JSON property set to catch accidental reintroduction of removed fields.
4. **Done**: Hub DTO intents-only; legacy paths synthesized on read; web feeds/dialogs consume intents only.
6. **Rename clarity** (non-breaking docs only): `compositionCapacity` = Places **N** at T, not current pool size k (already documented on DTO).
7. **Competition `regulation` vs stage rows**: Audit whether competition-level regulation strip on hub is still user-facing or only stage digest — out of scope for DTO deletion without UX sign-off.

---

## Verification after DTO edits

```bash
# Application
dotnet test tests/MyClub.PlayUp.Application.Tests --filter "FullyQualifiedName~StructureView"

# Web
cd src/PlayUp/web && npm run typecheck && npm test -- --run StructurePage.test.tsx
```
