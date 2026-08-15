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
