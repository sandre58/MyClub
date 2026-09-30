# Play’Up Web (organizer SPA)

Independent Vite + React + TypeScript app under `src/PlayUp/web/`. **Not** a .NET project (no `.csproj`).

## Prerequisites

1. PostgreSQL with Play’Up schema (local compose).
2. Host running on `http://localhost:5287` (`Properties/launchSettings.json`).
3. Host reachable (create competitions from the Accueil hub on `/` — Phase 19.1 / Accueil hub). DevRunner remains optional for seeded UX demos.

### Seed competitions (optional, local)

Requires User Secrets `ConnectionStrings:PlayUpDev` (see [local persistence](../../../docs/guides/local-persistence.md)). Point Host `ConnectionStrings:PlayUp` at the same `*_dev` database to browse seeded data.

```bash
dotnet run --project ../MyClub.PlayUp.DevRunner -- --reset --templates ligue-1:running
# or structured scenarios:
dotnet run --project ../MyClub.PlayUp.DevRunner -- --scenarios groups:running,cup:finished
```

Seeded competitions appear in the Accueil list (`GET /competitions`). Open a row to enter its Overview.

Progress: `prepared` | `running` (default) | `finished` via `id:progress`.

## Dev

```bash
cp .env.example .env.local   # optional: VITE_API_PROXY_TARGET
npm install
npm run dev
```

Open `http://127.0.0.1:5173/` — Accueil hub (list + create). `/competitions` redirects to `/`.

### Rider (Host + Vite in one click)

Shared Run Configurations live in the repo under [`.run/`](../../../.run/) (not under gitignored `.idea/`):

| Configuration         | Role                                                                                       |
| --------------------- | ------------------------------------------------------------------------------------------ |
| **PlayUp Host**       | ASP.NET Host (`launchSettings` → `http://localhost:5287`)                                  |
| **PlayUp Web**        | `npm run dev` in this folder (Vite → `http://127.0.0.1:5173/`)                             |
| **PlayUp Host + Web** | Compound — starts Host + Vite in parallel                                                  |
| **PlayUp DevRunner**  | One-shot CLI reset/seed (run when you need workspace data; not part of the daily compound) |

**Daily use:** select **PlayUp Host + Web** in the Run widget → Run (or Debug for the Host). Stop the compound to stop both.

If the Build tool window shows **MSB3026 / MSB3027** (“file is locked by MyClub.PlayUp.Host”) while the site still works: a Host process was still running when Rider tried to rebuild and overwrite its DLLs. The old process keeps serving → Vite works → page looks fine, but the new build failed.

- Prefer **Stop** on the compound (or Host tab), then Run again.
- `PlayUp Host` is configured as **singleton** so a re-Run should replace the previous Host.
- If a zombie remains: end `MyClub.PlayUp.Host` in Task Manager, then rebuild.

**One-time (this machine):**

1. Plugins: enable **JavaScript and TypeScript**; install/enable **Vite** if Marketplace offers it.
2. Attach the frontend for editing (does **not** change `.slnx`): Solution node → **Add | Existing Folder** → `src/PlayUp/web`.
3. Ensure Node.js is installed and `npm install` has been run in `src/PlayUp/web`.
4. PostgreSQL + Host connection string configured as usual.

If Rider does not list the shared configs after pull: **File | Invalidate Caches** or reopen the solution; or **Run | Edit Configurations** and confirm the three `.run/*.run.xml` files were loaded.

### Navigation path (11.3.2)

```text
/competitions/:competitionId
  → /stages/:stageId
    → /stages/:stageId/matches
      → /matches/:matchId
```

### Request path

```text
Browser
  → React UI
  → React Router (URL → page)
  → TanStack Query (useQuery / useMutation + query keys)
  → fetch('/competitions|stages|matches/…')
  → Vite proxy (JSON only; HTML deep-links stay on the SPA)
  → ASP.NET Host Minimal API
  → Read DTO or 204 command
  → invalidate → refetch → React render
```

No CORS in development: the browser only talks to Vite; Vite forwards `/competitions`, `/stages`, and `/matches` to the Host. Document navigations (`Accept: text/html`) are not proxied so deep-links like `/matches/{id}` load the SPA.

### Command loop (11.3.3)

On Match detail:

1. **Start** → `POST /matches/{id}/start` → invalidate match + stage match list → status Live
2. **Finish** (controlled form) → `POST /matches/{id}/finish` → invalidate match + stage match list
3. **Apply progression** → `POST /stages/{stageId}/fixtures/{fixtureId}/apply-progression` → invalidate match + list + stage (slot fill)

Re-seed after pulling DevRunner / Development scenario changes (`Winner` → slot `SF1-A`).

## Scripts

| Script                 | Role                                              |
| ---------------------- | ------------------------------------------------- |
| `npm run dev`          | Vite HMR server                                   |
| `npm run build`        | Typecheck + production bundle                     |
| `npm run format`       | Prettier write                                    |
| `npm run format:check` | Prettier check (CI)                               |
| `npm run lint`         | Oxlint with `--deny-warnings` (React / TS / oxc)  |
| `npm run lint:css`     | Stylelint on `src/design-system/**/*.css` (CI)    |
| `npm run typecheck`    | `tsc -b`                                          |
| `npm run test`         | Vitest watch                                      |
| `npm run test:run`     | Vitest single run (CI)                            |
| `npm run test:coverage`| Vitest + V8 coverage report (local / optional)    |
| `npm run test:e2e`     | Build + Playwright smoke (Chromium)               |
| `npm run test:e2e:ui`  | Same with Playwright UI                           |
| `npm run preview`      | Serve the production bundle                       |

First-time Playwright browser install (local): `npx playwright install chromium`.

E2E smokes stub Host JSON via Playwright routes (no Postgres / DevRunner required). For a Host-backed manual smoke: start Postgres + Host (`localhost:5287`) + `npm run dev`, then exercise Accueil → Overview → Structure → Match with DevRunner seed.

### Quality gates (CI `web` job)

Order: `format:check` → `lint` → `lint:css` → `typecheck` → `test:run` → `build` → Playwright smoke.  
**Errors and Oxlint warnings fail CI** (`oxlint --deny-warnings`). Stylelint covers **design-system** CSS only (page/shell CSS deferred). Coverage (`test:coverage`) is available locally — **no** CI threshold.

Config: `.prettierrc.json`, `.oxlintrc.json`, `.stylelintrc.json`. Format on save is an optional editor setting (Prettier); this package does **not** ship a committed `.vscode/` folder.

## Conventions

File / component / CSS / hook / i18n-key naming: [docs/conventions.md](./docs/conventions.md). Product glossary: [docs/i18n.md](./docs/i18n.md).

## Internationalization

Locales **`fr`** (default) and **`en`**. Language preference lives in **Préférences** (shell + Accueil). See [docs/i18n.md](./docs/i18n.md) for conventions, namespaces, parity, and the rule that new UI strings must go through i18n.

## Design System

Foundations live under `src/design-system/`. Operational rules: [docs/design-system.md](./docs/design-system.md). Validate at `/dev/foundations` (Slate-only playground). Product SoT: Notion Identité visuelle.

Shell/Sidebar stay **Play’Up-local** until a second MyClub app consumes the same Shell.

## Page migration (13.5 → Design System)

When reworking a business page, follow [docs/page-migration.md](./docs/page-migration.md). Pilot: `NotFoundPage`. Do not mass-migrate. Stabilise foundations first (see design-system.md) before a dedicated migration lot.

## Out of scope (later — explicit trigger only)

| Item                           | Trigger                                       |
| ------------------------------ | --------------------------------------------- |
| `features/`                    | Domain collision / ownership pain in `pages/` |
| OpenAPI / generated types      | Frequent DTO drift or a second HTTP consumer  |
| Playwright E2E (large suite)   | Beyond the 3–5 smoke scenarios already wired  |
| Storybook                      | Reused DS components across many screens      |
| Sass / Tailwind / UI libraries | Notion decision to reopen styling stack       |
| Auth / Host CORS / deploy prod | Product/platform need                         |

Until then: Vitest + RTL, manual DTO mirrors in `types.ts`, progressive `pages/`.
