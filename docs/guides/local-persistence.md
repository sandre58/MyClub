# Local persistence environment

This guide describes how Play'up developers run **PostgreSQL locally** for day-to-day Host work, and how that differs from automated tests and the **Development Workspace** (DevRunner).

Product vision and architecture decisions live in **Notion**. This document is the Git-side bootstrap for the local database.

## Why Docker

Play'up persists with **EF Core + PostgreSQL 18**. Local development uses **Docker Desktop** and **Docker Compose** so every developer gets the same PostgreSQL version, port layout, and volume strategy without installing PostgreSQL natively.

Docker is also required for **Testcontainers** integration tests (including Development.Tests scenario/template seeds). That usage is separate from the persistent Compose database (see below).

## Architecture (runtime)

```text
                 Domain
                   ↑
              Application
              ↑          ↑
       Infrastructure  Development
              ↑          ↑
             Host     DevRunner
```

| Layer | Responsibility |
| :---- | :------------- |
| **Host** | Product HTTP only. PostgreSQL via `ConnectionStrings:PlayUp`. No Development reference, no seed, no InMemory mode. |
| **Development** | Scenario/template catalog, JSON datasets, generators, orchestration (library; not shipped with Host). |
| **DevRunner** | CLI: reset (`*_dev` DB only), `--templates`, `--scenarios`, progress `prepared\|running\|finished`. |
| **Infrastructure** | EF Core + Npgsql. |
| **Docker Compose** | Persistent local PostgreSQL for developers (`compose.yml`). |
| **Testcontainers** | Ephemeral PostgreSQL for automated integration tests only. |

Target local flow:

```text
docker compose up
        ↓
DevRunner --reset --templates … and/or --scenarios …
        ↓
PostgreSQL (*_dev)
        ↓
Host (ConnectionStrings:PlayUp → same DB to browse seeded data)
        ↓
React
```

## Progress (`prepared` | `running` | `finished`)

Applies to **templates** and most **structured scenarios** (`championship`, `groups`, `cup`, `swiss-8x3`, `random`):

| Value | Meaning |
| :--- | :------ |
| `prepared` | Structure ready; matches Scheduled (or Swiss: Running with **0** rounds yet); no / incomplete results |
| `running` | ~50% matches Finished (default when omitted). Swiss: round 1 finished + round 2 half-played |
| `finished` | All matches Finished; competition Completed. Swiss: all planned rounds generated then finished |

Syntax: `id` or `id:progress` (e.g. `ligue-1:prepared`, `groups:finished`, `swiss-8x3:running`).

Fixed UX scenarios (`empty-workspace`, `draft-empty`, `registration-open`) and the multi-stage demo `cup-qf-sf` do **not** accept `:progress`.

### Aliases (compat — do not change silently)

| Alias | Resolves to | Notes |
| :--- | :---------- | :---- |
| `group-stage-mid` | `groups:running` | Historical shorthand |
| `finished` | `groups:finished` | Means that scenario, not “any finished seed” |
| `knockout-qf` | `cup:running` | **Historical only** — single-round cup (~16 teams, ~50% played). **Not** QF→SF. Multi-stage / from-slots demo = `cup-qf-sf` (no redirect). |

## Templates (inspired competitions)

Templates are **capacity demos**, not full real multi-phase calendars:

| Id | Approximation |
| :--- | :------------ |
| `ligue-1` | Championship, 18 clubs (JSON) — **`DoubleRoundRobin`**, 34 matchdays, PairMirror (`N×(N−1)` = 306). Capacity demo, not a real L1 calendar. |
| `champions-league` | Groups 8×4 only. **Not** UEFA League Phase (future distinct track). **Not** Swiss classique (Lot 2 Kind — do not wire here). |
| `world-cup` | Groups 8×4 → Top2 qualify → R16→QF→SF → Final + Bronze (`Loser`). Stops before materialize Final/Bronze. `:progress` ignored. |
| `coupe-de-france` | Cup multi-stage R32→R16→QF→SF→Final. R32 played, R16 slots filled — **stops before** from-slots (like `cup-qf-sf`). `:progress` ignored. |

Team lists live in embedded JSON under `MyClub.PlayUp.Development/Datasets/` (display name, short name, colors, `logoAsset`).

**Seed logos (internal only):** crest/flag PNGs under `MyClub.PlayUp.Development/Assets/seed-logos/`. Datasets reference relative paths (e.g. `ligue-1/psg.png`); DevRunner imports them into Media on seed (`logo_media_id`). Not for redistribution or product DS.

## Scenarios

| Id | Progress? | Notes |
| :--- | :--- | :---- |
| `empty-workspace` | no | |
| `draft-empty` | no | |
| `registration-open` | no | |
| `championship` / `groups` / `cup` / `random` | yes | Cup = single principal round |
| `swiss-8x3` | yes | Lot 2 case 1 — Swiss 8 teams × 3 rounds; Matchdays via `GenerateNextRound` (not `MaterializeMatches`) |
| `cup-qf-sf` | no | Multi-stage QF→SF: QF played, SF slots occupied, **stops before** `materialize-from-slots` (Cockpit) |

Multi-stage **templates** `coupe-de-france` and `world-cup` also ignore `:progress` (fixed seed contracts).

## Three PostgreSQL usages (do not mix)

| Usage | Purpose | Lifetime |
| :---- | :------ | :------- |
| **Docker Compose** | Develop against a real DB; create a `*_dev` database for DevRunner | Survives `docker compose down` |
| **Testcontainers** | Integration tests (Host, Infrastructure, Development) | Destroyed after fixture |
| **Fake connection strings** | Mapping/DI unit tests without opening a connection | Never connect |

## Local stack (facts)

| Item | Value |
| :--- | :---- |
| Compose file | `compose.yml` |
| Image | `postgres:18` |
| Host port | `localhost:5432` |
| Dev Workspace DB | name **must** end with `_dev` (e.g. `myclub_dev`) |
| Host connection | User Secrets `ConnectionStrings:PlayUp` |
| Media connection (optional) | User Secrets `ConnectionStrings:Media` (defaults to PlayUp string; schema `media`) |
| DevRunner connection | User Secrets `ConnectionStrings:PlayUpDev` |

### Configuration

```bash
dotnet user-secrets set "ConnectionStrings:PlayUp" "Host=localhost;Port=5432;Database=myclub_dev;Username=myclub;Password=…" --project src/PlayUp/MyClub.PlayUp.Host
dotnet user-secrets set "ConnectionStrings:PlayUpDev" "Host=localhost;Port=5432;Database=myclub_dev;Username=myclub;Password=…" --project src/PlayUp/MyClub.PlayUp.DevRunner
```

Create the workspace DB once:

```bash
docker exec -it myclub-postgres-1 psql -U myclub -d myclub -c "CREATE DATABASE myclub_dev;"
```

## DevRunner examples

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --list
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --list-templates

dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --templates ligue-1:prepared,world-cup:finished
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --scenarios groups:running,cup:finished
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --scenarios swiss-8x3:prepared
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --scenarios cup-qf-sf
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --scenarios random:prepared --seed 7
```

Generation demos (Lot 1–3): `--templates ligue-1:prepared` (Double RR) · `--templates coupe-de-france` (multi-stage from-slots) · `--templates world-cup` (Groups→KO+Bronze) · `--scenarios cup-qf-sf` · `--scenarios swiss-8x3:running` (Swiss progressive rounds).

Reset is refused unless the DB name ends with `_dev`, the host is localhost/loopback, and the environment is not Production. Host has **no** reset/seed capability.

## Useful commands

| Command | Purpose |
| :------ | :------ |
| `docker compose up -d` | Start PostgreSQL |
| `dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios groups:running` | Reset + seed |
| `dotnet test --filter Category!=Integration` | Fast suite |
| `dotnet test` | Full suite (Docker required for Integration) |
