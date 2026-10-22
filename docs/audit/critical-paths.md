# Critical paths — coverage snapshot (phase 3 / C5)

Purpose: validate the 8 agreed scenarios and where regression signal exists today.

## E2E layers (AUD-0015 hybrid)

| Layer | Config / location | Infra | Role |
| --- | --- | --- | --- |
| **Stub shell** | `playwright.config.ts` → `e2e/stub/*.spec.ts` | `npm run preview` only | Fast CI smokes; API intercepted (`stubApi.ts`) |
| **Host métier** | `playwright.host.config.ts` → `e2e/business/*.spec.ts` | PostgreSQL + DevRunner seed + Host + Vite dev | Real Read/Command on at least one vertical slice |

CI: `web` job runs stub E2E after build; `e2e-integration` job runs Host E2E (no silent skip if Postgres/seed/Host fail).

## Path matrix

| # | Scenario | Backend / integration signal | Frontend signal | Stub E2E | Host E2E |
| --- | --- | --- | --- | --- | --- |
| 1 | Structure hub / fiche | `CompetitionStructureEndpointTests`, structure reads in Host.Tests | `StructurePage.test.tsx`, operational strip tests | **Yes** — Structure / League fiche | — |
| 2 | Prepare / materialize | `DematerializeEndpointTests`, materialize guards in Application.Tests | `StructurePage.test.tsx` (materialize flows) | **Partial** — hub only | — |
| 3 | Draw (publish/apply) | `PublishDrawEndpointTests`, `ApplyDrawEndpointTests`, `CancelDrawEndpointTests` | Structure draw UI tests | **No** | **Yes** — `cup-composition-complete` + Host API slice (`cup-draw.spec.ts`) |
| 4 | Match lifecycle | `MatchLifecycleEndpointTests`, Host match flows | `MatchPage.test.tsx` | **Yes** — start + finish (stub) | **Yes** — `championship-ready` + stage/competition start, then match |
| 5 | Progression / assignment | `ReplaceStageProgressionRulesGOps3EndpointTests`, cup slot tests | `StructureProgressionDialog`, presentation tests | **Partial** — overview PrepareStage tip (stub) | — |
| 6 | Shell / attention | Needs-attention reads (Host + Application collectors) | `AttentionDrawer.test.tsx`, `ShellHeader.test.tsx` | **Yes** — drawer + ProgressionPending row (stub) | — |
| 7 | Accueil | Competition bootstrap/list endpoints | Home / competition list tests | **Yes** | — |
| 8 | Competition overview | Overview read endpoints | `CompetitionOverviewPage*.test.tsx` | **Yes** — shell + Prochaine action (stub progression) | — |

## Assessment

- **Stub:** Six paths touched lightly (1, 4, 5–8); materialize and draw remain Vitest / .NET heavy.
- **Host:** Business specs on seeded data (match lifecycle `championship-ready`; draw publish/apply `cup-composition-complete`) — complement Domain/Host tests; draw slice is API-driven, not full Structure UI.
- **Vitest:** Primary frontend regression surface for Structure/Match/Overview depth.

Gaps that need product or a later lot: materialize E2E (path 2), draw **UI** E2E (path 3 depth), progression assignment beyond attention/overview stubs (path 5).
