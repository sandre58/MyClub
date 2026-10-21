# Phase 1 — Wave 4: Critical paths — effective coverage

**Launched:** 2026-10-21  
**Closed:** 2026-10-21  
**Branch:** `feature/new_version`  
**SHA vague:** `93b09f161739f47b768caad63aed90d0d3c1469f`  
**Scenario list:** `docs/audit/critical-paths.md` (8 paths, unchanged).

No code, test, or fixture changes in this wave.

## Method

For each path: map **expected behaviours** (SoT), **tests actually executed** (by layer), **nominal / edge / error** coverage, **gaps**, and **links** to existing findings. Ratings are qualitative — absence of a test is a **coverage lacuna**, not a product defect unless stated otherwise.

**Layers**

| Layer | Scope |
| --- | --- |
| **Domain / Application** | `MyClub.PlayUp.Domain.Tests`, `MyClub.PlayUp.Application.Tests` |
| **Host / API** | `MyClub.PlayUp.Host.Tests` (Testcontainers Postgres, real HTTP) |
| **Development recette** | `MyClub.PlayUp.Development.Tests` (executable protocols, Postgres) |
| **Frontend unit** | Vitest under `src/PlayUp/web/src` (`npm run test:run`) |
| **E2E** | Playwright `e2e/smoke.spec.ts` — **stubbed API**, `npm run preview` (requires production build) |

**Controls @ SHA (reproducible)**

```text
# Backend (includes Development.Tests → 1 failure)
dotnet test --configuration Release

# Frontend
cd src/PlayUp/web
npm run typecheck          # pass (app + node only)
npm run test:run           # pass (phase 0: 1132 tests)
npm run build              # fail — AUD-0002 (34 TS errors, vitest project in tsc -b)
npx playwright test        # needs preview bundle → blocked when build fails (CI order: build then E2E)
```

**Cross-findings**

| Finding | Wave 4 role |
| --- | --- |
| **AUD-0004** | Cup C2 recette protocol — coverage exists, assertion mismatch at Start guard (see path 2–3) |
| **AUD-0002** | `tsc -b` / CI `build` fails; `typecheck` and `test:run` pass → **compile-time regression signal weak** for test sources; **E2E CI step not reached** when build fails |
| **AUD-0015** | E2E scope vs 8 paths (new, this wave) |

---

## Coverage bilan (summary)

| # | Path | Domain/App | Host/API | Dev recette | FE Vitest | E2E smoke | Overall signal |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Structure hub / fiche | Strong | Strong | Partial | **Strong** | Light | **Strong** (FE+API); E2E shallow |
| 2 | Prepare / materialize | Strong | Strong | Strong (C1) / **Broken (C2)** | Strong | None | **Strong** backend; C2 protocol **AUD-0004** |
| 3 | Draw publish/apply | Strong | Strong | Via cup protocols | **Strong** (draw dialog) | None | **Strong** unit/API; no E2E |
| 4 | Match lifecycle | Strong | Strong | Indirect | Strong | Light (stub) | **Strong**; E2E not against Host |
| 5 | Progression / assignment | Strong | Strong | Partial | Moderate | None | **Good** API/domain; FE mostly presentation/draft |
| 6 | Shell / attention | Strong | Moderate | — | Strong | None | **Good** FE shell; API via read-surface tests |
| 7 | Accueil | Moderate | Moderate | — | Strong | Light | **Adequate**; list/create mocked in FE |
| 8 | Competition overview | Strong | Strong | — | Strong | Light (shell) | **Adequate**; E2E does not assert business blocks |

---

## 1. Structure — hub, navigation, operational fiche

### Expected behaviours (sources)

- Organizer reads **Structure** as authoritative **read model** from `GET /competitions/{id}/structure` (and related reads); UI does not invent lifecycle truth (`docs/guides/http-api-contract.md`, architecture rules).
- Hub: phase topology, fiche sections driven by `StructureView` / hub summaries (format, readiness, actions).
- Operational strip, lifecycle toolbar, diagnostics aligned with backend `readiness`, `lifecycle`, `materialize` projections.

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Application | `StructureViewAssembler*` tests, `StructureStageLifecycleAssembler*`, issues/diagnostics collectors |
| Host | `CompetitionStructureEndpointTests` (configure structure, entries, roundtrip GET), `ReadSurfaceEndpointTests` (structure-related reads) |
| Development | Scenario seeds used by recette protocols (not a dedicated “hub navigation” test) |
| Frontend | `StructurePage.test.tsx` (30+ cases: hub, fiche, tiles, lifecycle header, materialize banners, draw gating, PageHead/attention hints), many `structure*.test.ts(x)` around presentation |
| E2E | `smoke.spec.ts` — open Structure, see League fiche + Match heading (**stub API**) |

### Scenarios

| Kind | Covered? | Notes |
| --- | --- | --- |
| Nominal hub + fiche | Yes | FE + Host structure configure/read |
| Multi-phase / Groups / Cup layouts | Yes | FE tests (groups fiche, topology, sections) |
| Read errors | Yes | `StructurePage` error state |
| Regression FE compile-time | **Partial** | Many `StructurePage` fixtures omit `blockerDetails` — caught by `tsc -b`, not by `test:run` (**AUD-0002**) |

### Gaps

- No E2E with **real** structure payload or multi-phase navigation.
- No single Host test whose sole goal is “organizer navigates hub”; coverage is decomposed across endpoint + FE tests.

---

## 2. Prepare and materialize

### Expected behaviours (sources)

- **Prepare** competition / stage transitions (Draft → Ready) with guards (`PrepareCompetition`, stage prepare endpoints).
- **Materialize** confrontations (championship vs cup rules), dematerialize where allowed; read-model reflects `materialize` command state.
- Cup recette: `docs/design/structure-lifecycle/cup-materialize-recette-protocol.md`, `cup-c2-spa-recette-checklist.md`.

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Application | `MaterializeCupFromOccupiedSlots*`, `MaterializeMatches*`, `DematerializeCup*`, `DematerializeChampionship*` |
| Host | `CompetitionPrepareStartEndpointTests`, materialize via `ApplyDrawEndpointTests` / `DematerializeEndpointTests` / `HttpContractEndpointTests.Materialize_returns_named_contract_shape`, `StartStageEndpointTests` (seeds with materialized matches) |
| Development | `CupMaterializeRecetteProtocolTests` (C1 / R-rules) — **passing**; `CupC2ImpasseRecetteProtocolTests` — **failing** (**AUD-0004**) |
| Frontend | `StructurePage` materialize readiness; `StructurePhaseDraftMatchsPrepare.test.tsx`; `structureMatchProductionPresentation.test.tsx`; `structurePopulationRecette.test.ts` (fixture-driven SPA recette observations, not Host) |
| E2E | None |

### AUD-0004 re-examination (no arbitration)

`CupC2ImpasseRecetteProtocolTests` **is** the automated binding for manual C2 (Notion cup arbitration / SPA recette checklist).

Repro @ `93b09f16…`:

```text
dotnet test tests/MyClub.PlayUp.Development.Tests --filter FullyQualifiedName~CupC2Impasse
# Fail line 80: expected StartBlockedUnmaterialized, actual StartBlockedIncompleteEntryRound
```

| Protocol step | Test intent | Observable @ failure |
| --- | --- | --- |
| After slot assign, before 2nd materialize | Start blocked **unmaterialized** confrontations | Domain returns **`StartBlockedIncompleteEntryRound`** |
| After 2nd materialize | Start blocked incomplete entry round | Assertions **after** line 80 not reached in failing run |

**Coverage:** C2 chain is **intended** to be guarded by this test; **signal is red** — disagreement between test expectation and current `StartStageAsync` ordering/guards, not absence of test.

Later steps in the same test (read-model `DisabledIncompleteEntryRound`) align with **post–second-materialize** behaviour documented in test body — only the **first** Start assertion is inconsistent with observed code.

### Gaps

- No E2E prepare/materialize.
- FE cup impasse not wired to Development C2 test (SPA recette is separate fixture tests).
- Full `dotnet test` fails on C2 → CI/backend regression gate **partially blind** to recette green.

---

## 3. Draw (publish / apply / cancel)

### Expected behaviours (sources)

- Draw lifecycle: create → resolve → publish → apply slot occupancy; guards on obsolete draw for consumption (`ApplicationErrorCodes`, Host 409 cases).
- UI: `StructureDrawDialog`, create gates (`resolveStageDrawCreateGate`), cup vs championship rules.

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Application | `ApplyDrawTests`, `PublishDraw*`, `StageDrawRerunLaunchGuardTests`, draw resolution |
| Host | `PublishDrawEndpointTests`, `ApplyDrawEndpointTests`, `CancelDrawEndpointTests` (if present), draw obsolete → materialize 409 in `CEnfMatStartEndpointTests` |
| Host vertical | `R2HostEndToEndTests` — Publish → Apply → … → materialize (paths 2–5 chain) |
| Frontend | `StructureDrawDialog.test.tsx` (30+ cases: create, generate, cup slot, guards, cancel states), `structurePhaseDraw*.test.ts` |
| E2E | **None** |

### Scenarios

| Kind | Covered? |
| --- | --- |
| Publish/apply happy path | Host + R2 E2E chain |
| Invalid transitions / 404 / 409 | Host endpoint tests |
| UI gates and errors | Strong Vitest on dialog |
| Full draw UI in browser | **No** |

### Gaps

- E2E draw path absent (**AUD-0015**).
- R2 chain covers API draw but not Structure draw dialog integration with live Host.

---

## 4. Match lifecycle (start, score, finish)

### Expected behaviours (sources)

- Match states: Scheduled → Running → Finished; sheet updates; domain guards on start/finish.
- HTTP: match endpoints per `http-api-contract.md`.

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Domain/Application | Match aggregate lifecycle tests |
| Host | `MatchLifecycleEndpointTests`, `CEnfMatStartEndpointTests`, `MatchSheetEndpointTests` |
| Host vertical | `R2HostEndToEndTests` (start/finish within cup chain) |
| Frontend | `MatchPage.test.tsx`, panel tests (`MatchGoalsPanel`, `MatchSheetPanel`, substitutions, disciplinary) |
| E2E | `smoke.spec.ts` — start + score + finish (**stub**, FR labels) |

### Gaps

- E2E does not call Host; stub may drift from real DTOs.
- Cup-specific match rules / progression side-effects not in smoke E2E.

---

## 5. Progression and assignment

### Expected behaviours (sources)

- Progression rules authoring, apply outcome after matches, cup slot placement, entry-to-slot assignment.
- Domain: `ProgressionApplierTests`, progression paths.

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Domain | `ProgressionApplierTests`, `ProgressionChampionshipPathTests` |
| Application | `ReplaceStageProgressionRulesGOps*`, `AssignEntryToSlotTests`, `ProgressionPathConsumptionWitnessTests` |
| Host | `ReplaceStageProgressionRulesGOps3EndpointTests`, `ApplyProgressionOutcomeEndpointTests`, `CupSlotPlacementGOps3B1EndpointTests`, `CompetitionSlice5EndpointTests` (attention includes progression pending) |
| Host vertical | `R2HostEndToEndTests` (progression after match) |
| Frontend | `structureProgressionDraft.test.ts`, `structureProgressionChampionshipPath.test.ts`, `StructureQualificationDialog`, placement/sorties tests — **authoring/presentation**, fewer full HTTP flows |
| E2E | **None** |

### Gaps

- No browser-level progression or slot assignment journey.
- FE tests rarely assert end-to-end “finish match → progression applied → structure read updates” (that chain is mainly Host/R2).

---

## 6. Shell and attention (“À traiter”)

### Expected behaviours (sources)

- `GET /competitions/{id}/attention` and workspace attention counts; drawer lists blocking/informational items; navigation to targets without inventing routes.

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Application | `NeedsAttentionAssemblerTests` |
| Host | `ReadSurfaceEndpointTests` (attention blocking items), `CompetitionSlice5EndpointTests` (attention + workspace count), overview suspended vs attention |
| Frontend | `AttentionDrawer.test.tsx` (open/close, a11y, routing, fixture attention), `ShellHeader.test.tsx` (trigger, count, drawer link), `attentionPartition.test.ts`, `structureAttentionPresentation` tests |
| E2E | **None** |

### Gaps

- No E2E opening drawer with realistic multi-item attention from stub (smoke does not click “À traiter”).
- Host coverage exists but is not consolidated into one “shell journey” test.

---

## 7. Accueil (competition hub entry)

### Expected behaviours (sources)

- List competitions, create competition, navigate into workspace (shell).

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Host | Competition create/list endpoints (within structure/workspace tests) |
| Frontend | `HomePage.test.tsx` (list, create, navigation, error states) |
| E2E | `smoke.spec.ts` — brand, create button, competition list link |

### Gaps

- E2E light; create flow not exercised end-to-end in Playwright.
- Host list/create not paired with FE in one integration test (by design — layers separate).

---

## 8. Competition overview

### Expected behaviours (sources)

- Overview read model: next actions, preparation/operational blocks, attention synthesis (`OverviewAssembler`).

### Tests executed

| Layer | Representative tests |
| --- | --- |
| Application | `OverviewAssemblerTests` |
| Host | `ReadSurfaceEndpointTests` (overview, suspended projection), slice tests touching workspace |
| Frontend | `CompetitionOverviewPage.preparation.test.tsx`, `CompetitionOverviewPage.operational.test.tsx`, `overviewActions.test.ts`, `overviewComposition.test.ts` |
| E2E | Navigate to overview region `.overview` only — **no assertion** on next action / blocks |

### Gaps

- E2E explicitly scoped to “shell reachability” (comment in `smoke.spec.ts`) — business overview content **not** regression-tested in E2E.

---

## AUD-0002 — impact on path coverage reliability

| Control | Paths affected | Effect |
| --- | --- | --- |
| `npm run test:run` | All FE unit paths | **Runs** — primary FE regression signal today |
| `npm run typecheck` | FE production sources | **Runs** — does not include `*.test.tsx` |
| `npm run build` / `tsc -b` | FE tests + app | **Fails** — mocks drift (`blockerDetails`, materialize shapes, hero stats) |
| CI `web` job | 8 paths E2E | **Build step fails** → Playwright step **not reached**; even stub E2E not exercised in CI @ SHA |
| `npm run test:e2e` locally | E2E | Script runs `build` first → blocked |

**Conclusion:** Unit/integration coverage for critical paths can be **strong** while **compile-time + browser smoke regression** is **unreliable or off** in CI — extend **AUD-0002**, no new duplicate finding.

---

## Register (wave 4)

| ID | Action |
| --- | --- |
| AUD-0004 | Reconfirmed — C2 protocol test present; failure documents contract drift; no decision taken |
| AUD-0002 | Linked — FE/E2E control reliability (see section above) |
| **AUD-0015** | **New** — critical paths 5/8 without any E2E signal; 4/8 only stub-smoke; Playwright depends on failing build in CI |

**Next free reference:** AUD-0016.

## Sequence

**Wave 5** — security, CI governance, Cursor rules inventory reconciliation (per plan). Path coverage findings feed phase 2 prioritization (E2E scope vs fixing AUD-0002 first).
