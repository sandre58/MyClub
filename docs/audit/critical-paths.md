# Critical paths — coverage snapshot (phase 0)

Execution SHA `93b09f161739f47b768caad63aed90d0d3c1469f`.  
Purpose: validate the 8 agreed scenarios and where regression signal exists today.

| # | Scenario | Backend / integration signal | Frontend signal | E2E smoke (`e2e/smoke.spec.ts`) |
| --- | --- | --- | --- | --- |
| 1 | Structure hub / fiche | `CompetitionStructureEndpointTests`, structure reads in Host.Tests | `StructurePage.test.tsx` (large), operational strip tests | **Yes** — open Structure / League fiche |
| 2 | Prepare / materialize | `DematerializeEndpointTests`, materialize guards in Application.Tests | `StructurePage.test.tsx` (materialize flows), lifecycle blocks | **Partial** — hub only |
| 3 | Draw (publish/apply) | `PublishDrawEndpointTests`, `ApplyDrawEndpointTests`, `CancelDrawEndpointTests` | Structure draw UI tests, `resolveStageDrawCreateGate` tests | **No** |
| 4 | Match lifecycle | `StartMatch` / finish flows in Host.Tests, `CEnfMatStartEndpointTests` | `MatchPage.test.tsx`, panels | **Yes** — start + finish (stub API) |
| 5 | Progression / assignment | `ReplaceStageProgressionRulesGOps3EndpointTests`, cup slot tests | `StructureProgressionDialog`, progression presentation tests | **No** |
| 6 | Shell / attention | Needs-attention reads (Host + Application collectors) | `AttentionDrawer.test.tsx`, `ShellHeader.test.tsx`, presentation tests | **No** |
| 7 | Accueil | Competition bootstrap/list endpoints | Home / competition list tests | **Yes** |
| 8 | Competition overview | Overview read endpoints | `CompetitionOverviewPage*.test.tsx` | **Yes** — shell reachability only |

## Phase 0 assessment

- **E2E:** Single smoke file, API stubbed — covers 4/8 paths lightly; no draw, progression, or attention drawer.
- **Vitest:** Broad Structure/Match coverage; many fixtures **out of sync** with `StructureReadiness.blockerDetails` (blocks `npm run build`, not `npm run test:run`).
- **.NET:** Strong Domain/Application/Host coverage; one **Development.Tests** recette protocol failure (C2 cup impasse).

No change to the 8-scenario list in phase 0; gaps feed phase 1 findings, not scope expansion.
