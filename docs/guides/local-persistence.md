# Local persistence environment

This guide describes how Play'up developers run **PostgreSQL locally** for day-to-day Host work, and how that differs from automated tests.

Product vision and architecture decisions live in **Notion**. This document is the Git-side bootstrap for the local database.

## Why Docker

Play'up persists with **EF Core + PostgreSQL 18**. Local development uses **Docker Desktop** and **Docker Compose** so every developer gets the same PostgreSQL version, port layout, and volume strategy without installing PostgreSQL natively.

Docker is also required for **Testcontainers** integration tests. That usage is separate from the persistent Compose database (see below).

## Architecture (runtime)

```text
Host (configuration / User Secrets)
  |
  | ConnectionStrings:PlayUp
  v
AddPlayUpInfrastructure(connectionString)
  |
  v
PlayUpDbContext
  |
  | UseNpgsql
  v
PostgreSQL 18 (Docker Compose)
```

| Layer | Responsibility |
| :---- | :------------- |
| **Host** | Owns runtime configuration. Reads `ConnectionStrings:PlayUp` and calls `AddPlayUpInfrastructure`. |
| **Infrastructure** | Consumes the connection string. Configures `PlayUpDbContext` with `UseNpgsql`. Does **not** read Host User Secrets. |
| **EF design-time** | Uses the Host as startup project (service provider). No `IDesignTimeDbContextFactory` is required. |
| **Docker Compose** | Persistent local PostgreSQL for developers (`compose.yml`). |
| **Testcontainers** | Ephemeral PostgreSQL for automated integration tests only. |

## Three PostgreSQL usages (do not mix)

| Usage | Purpose | Lifetime |
| :---- | :------ | :------- |
| **Docker Compose** | Develop against a real, persistent database (`myclub`) | Survives `docker compose down` (volume kept) |
| **Testcontainers** | Prove mapping / constraints / Host E2E in CI and local full test runs | Destroyed after the test fixture |
| **Fake connection strings** | Unit tests of model mapping or DI registration without opening a connection | Never connect |

## Local stack (facts)

| Item | Value |
| :--- | :---- |
| Compose file | `compose.yml` (repository root) |
| Image | `postgres:18` |
| Host port | `localhost:5432` |
| Database | `myclub` (from `.env`) |
| User | `myclub` (from `.env`) |
| Password | Local only — set in `.env`, never committed |
| Volume | `myclub-postgres-data` → `/var/lib/postgresql` (PostgreSQL 18 data layout) |
| App connection | Host User Secrets key `ConnectionStrings:PlayUp` |

### Configuration split

| Source | Role |
| :----- | :--- |
| `.env` / `.env.example` | Variables for **Docker Compose** (`POSTGRES_*`). `.env` is gitignored. |
| Host User Secrets | Runtime **`ConnectionStrings:PlayUp`** for the ASP.NET Host and `dotnet ef`. |
| `appsettings.json` | No connection string (no secrets in Git). |
| `appsettings.Development.json` | Gitignored; local logging overrides only — not the SoT for the connection string. |

Keep the password in `.env` and in User Secrets aligned so the Host can connect to the Compose database.

---

# Bootstrap for a new developer

## 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (see [`global.json`](../../global.json))
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Linux engine / WSL2 backend on Windows)
- Git
- JetBrains Rider (or any PostgreSQL client) — **optional**, not a project dependency

## 2. Clone and restore

```bash
git clone https://github.com/sandre58/MyClub.git
cd MyClub
dotnet restore
dotnet tool restore
```

## 3. Create `.env`

```bash
cp .env.example .env
```

Edit `.env` and set a local `POSTGRES_PASSWORD` (replace `change-me`). Do not commit `.env`.

## 4. Start PostgreSQL

```bash
docker compose up -d
docker compose ps
```

Expect service `postgres` using image `postgres:18` and port `5432`.

Optional validation of the resolved Compose file:

```bash
docker compose config
```

## 5. Configure Host User Secrets

```bash
dotnet user-secrets set "ConnectionStrings:PlayUp" "Host=localhost;Port=5432;Database=myclub;Username=myclub;Password=YOUR_LOCAL_PASSWORD" --project src/PlayUp/MyClub.PlayUp.Host
```

Verify:

```bash
dotnet user-secrets list --project src/PlayUp/MyClub.PlayUp.Host
```

You should see `ConnectionStrings:PlayUp` (password never commit this value).

## 6. Apply EF Core migrations

Migrations live in `src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Migrations`.

```bash
dotnet ef database update --project src/PlayUp/MyClub.PlayUp.Infrastructure --startup-project src/PlayUp/MyClub.PlayUp.Host
```

Design-time uses the Host configuration (User Secrets). Do not add an `IDesignTimeDbContextFactory` for this workflow.

## 7. Run the Host

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.Host
```

Confirm the process starts without a missing-connection-string exception.

## 8. Connect with a client (optional)

Use Rider’s Database tool, `psql`, or another PostgreSQL client:

- Host: `localhost`
- Port: `5432`
- Database: `myclub`
- User: `myclub`
- Password: the value from `.env`

Example via Docker:

```bash
docker exec -it myclub-postgres-1 psql -U myclub -d myclub
```

(Container name may vary; use `docker compose ps`.)

## 9. Stop PostgreSQL

```bash
docker compose down
```

This **stops and removes the container** but **keeps** the named volume `myclub-postgres-data`. Data survives restarts.

### Do not destroy the volume by accident

```bash
docker compose down -v
```

removes volumes declared in Compose, including `myclub-postgres-data`, and **deletes local database data**. Only use it when you intentionally want a blank database.

List volumes:

```bash
docker volume ls
```

---

## Useful commands

| Command | Purpose |
| :------ | :------ |
| `docker compose config` | Validate / print resolved Compose config |
| `docker compose up -d` | Start PostgreSQL in the background |
| `docker compose ps` | Show service status |
| `docker compose down` | Stop containers; **keep** volumes |
| `docker compose down -v` | Stop containers and **delete** volumes (data loss) |
| `docker volume ls` | List volumes (look for `myclub-postgres-data`) |
| `dotnet ef database update --project …Infrastructure --startup-project …Host` | Apply migrations |
| `dotnet test --filter Category!=Integration` | Fast suite without Testcontainers |
| `dotnet test` | Full suite (needs Docker for Testcontainers) |

## Troubleshooting (observed)

| Symptom | Likely cause | What to do |
| :------ | :----------- | :--------- |
| Compose / Testcontainers fail with daemon errors | Docker Desktop not running | Start Docker Desktop; wait until the engine is ready |
| `Bind for 0.0.0.0:5432 failed` | Another PostgreSQL (or process) already uses 5432 | Stop the other service, or temporarily change the host port mapping (local only) |
| Unexpected auth / empty data after recreate | Existing volume `myclub-postgres-data` keeps first-boot credentials | Align `.env` / User Secrets with the volume, or intentionally recreate the volume (data loss) |
| PostgreSQL 18 data directory | Official image expects data under `/var/lib/postgresql` | Keep the Compose mount as in `compose.yml`; do not mount the older `/var/lib/postgresql/data` path for PG 18 |

## Related docs

- [README — Getting started](../../README.md#getting-started)
- [CONTRIBUTING.md](../../CONTRIBUTING.md)
- Notion: Technologies *Docker* (hub général + exemple MyClub) · *PostgreSQL* · *Testcontainers* · *EF Core* ; Play'up *Architecture technique*
