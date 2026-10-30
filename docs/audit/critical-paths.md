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
| 2 | Prepare / materialize | `DematerializeEndpointTests`, materialize guards in Application.Tests | `StructurePage.test.tsx` (materialize flows) | **Partial** — hub only | **Yes** — `groups-materialize.spec.ts` API + UI (RECETTE G1, `Créer les matchs`, 6/6 on screen) |
| 3 | Draw (publish/apply) | `PublishDrawEndpointTests`, `ApplyDrawEndpointTests`, `CancelDrawEndpointTests` | Structure draw UI tests | **No** | **Yes** — `cup-draw.spec.ts` API + UI (draw window, launch, apply, result on screen) |
| 4 | Match lifecycle | `MatchLifecycleEndpointTests`, Host match flows | `MatchPage.test.tsx` | **Yes** — start + finish (stub) | **Yes** — `championship-ready` + stage/competition start, then match |
| 5 | Progression / assignment | `ReplaceStageProgressionRulesGOps3EndpointTests`, cup slot tests | `StructureProgressionDialog`, presentation tests | **Partial** — overview PrepareStage tip (stub) | — |
| 6 | Shell / attention | Needs-attention reads (Host + Application collectors) | `AttentionDrawer.test.tsx`, `ShellHeader.test.tsx` | **Yes** — drawer + ProgressionPending row (stub) | — |
| 7 | Accueil | Competition bootstrap/list endpoints | Home / competition list tests | **Yes** | — |
| 8 | Competition overview | Overview read endpoints | `CompetitionOverviewPage*.test.tsx` | **Yes** — shell + Prochaine action (stub progression) | — |

## Assessment

- **Stub:** Six paths touched lightly (1, 4, 5–8); materialize and draw remain Vitest / .NET heavy, plus Host UI below.
- **Host:** Business specs on seeded data. Match lifecycle `championship-ready`. Draw (`cup-composition-complete`) and group materialize (`structure-materialize-recette` G1) each keep an API slice and a Structure UI slice in the same spec file. The two slices share one competition; `beforeEach` restores the scenario (pending draw, or 0 attached matches) so either order can run.
- **Vitest:** Primary frontend regression surface for Structure/Match/Overview depth.
- **Visual:** `/design-lab` and `/dev/foundations` — see below. They run in the stub Playwright job (CI `web`), not the Host job.

Gaps that need product or a later lot: progression assignment beyond attention/overview stubs (path 5).

## Visual baselines

`e2e/stub/visual.spec.ts` captures a fixed **1280×800** viewport (`deviceScaleFactor: 1`) of `/design-lab` and `/dev/foundations` with `toHaveScreenshot`. The shot is the first screen, not a full-page scroll.

Stability, applied in that spec and in `playwright.config.ts` (project `visual`):

- `prefers-reduced-motion`, CSS animations and transitions forced off, caret hidden, scrollbars hidden
- `document.fonts.ready` plus IBM Plex Sans 400/600/700 loaded before the shot
- mask on `.ds-live-dot` (live pulse)
- `animations: 'disabled'`, `scale: 'css'`, `maxDiffPixelRatio: 0.02` (per-pixel color threshold stays the Playwright default, 0.2)

Reference files are the **Linux** snapshots (`*-visual-linux.png`) produced with the Chromium that `npx playwright install --with-deps chromium` installs on ubuntu (the CI `web` job). Commit those files. Do not commit macOS or Windows snapshots; they will not match CI.

Regenerate from `src/PlayUp/web` on that Linux environment:

```bash
npm run build
npx playwright test e2e/stub/visual.spec.ts --update-snapshots
```

Review the image diff, then commit the updated `e2e/stub/visual.spec.ts-snapshots/` files.
