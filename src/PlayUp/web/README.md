# Play’up Web (organizer SPA)

Independent Vite + React + TypeScript app under `src/PlayUp/web/`. **Not** a .NET project (no `.csproj`).

## Prerequisites

1. PostgreSQL with Play’up schema (local compose).
2. Host running on `http://localhost:5287` (`Properties/launchSettings.json`).
3. A competition in the database (KEEP SEED) that includes at least one stage with an attached match.

### Seed a competition (local)

Requires Host User Secrets `ConnectionStrings:PlayUp` (same as running the Host — see [local persistence](../../../docs/guides/local-persistence.md)).

```bash
dotnet run --project ../MyClub.PlayUp.DevSeed
```

Copy the printed `competitionId` into `.env.local` as `VITE_SEED_COMPETITION_ID`. The seed also prints `stageId` and `matchId` for deep-link checks.

## Dev

```bash
cp .env.example .env.local   # then set VITE_SEED_COMPETITION_ID
npm install
npm run dev
```

Open `http://127.0.0.1:5173/` → redirects to `/competitions/<seed-id>`.

### Rider (Host + Vite in one click)

Shared Run Configurations live in the repo under [`.run/`](../../../.run/) (not under gitignored `.idea/`):

| Configuration | Role |
|---|---|
| **PlayUp Host** | ASP.NET Host (`launchSettings` → `http://localhost:5287`) |
| **PlayUp Web** | `npm run dev` in this folder (Vite → `http://127.0.0.1:5173/`) |
| **PlayUp Host + Web** | Compound — starts both in parallel |

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
  → TanStack Query (useQuery + stable query keys)
  → fetch('/competitions|stages|matches/…')
  → Vite proxy
  → ASP.NET Host Minimal API
  → Read DTO (JSON)
  → React render
```

No CORS in development: the browser only talks to Vite; Vite forwards `/competitions`, `/stages`, and `/matches` to the Host.

## Scripts

| Script | Role |
|---|---|
| `npm run dev` | Vite HMR server |
| `npm run build` | Typecheck + production bundle |
| `npm run preview` | Serve the production bundle |

## Out of scope (until 11.3.3+)

Mutations (Start / Finish / Progression), features/ architecture, Sass/Tailwind, UI libraries, OpenAPI, auth, Host CORS.
