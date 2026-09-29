# Local persistence environment

This guide describes how Play'Up developers run **PostgreSQL locally** for day-to-day Host work, and how that differs from automated tests and the **Development Workspace** (DevRunner).

Product vision and architecture decisions live in **Notion**. This document is the Git-side bootstrap for the local database.

## Why Docker

Play'Up persists with **EF Core + PostgreSQL 18**. Local development uses **Docker Desktop** and **Docker Compose** so every developer gets the same PostgreSQL version, port layout, and volume strategy without installing PostgreSQL natively.

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

Fixed UX / Structure / Règlement scenarios (`empty-workspace`, `draft-empty`, `registration-open`, `registration-withdrawn`, `championship-ready`, `championship-archived`, `championship-structure-draft`, `structure-graph-invalid`, `groups-suspended`, `groups-draw-pending`, `groups-to-ko-mid`, `qual-auto-place-mid`, `qual-hybrid-auto-draw-mid`, `prog-auto-place-mid`, `qual-form-to-champ-mid`, `flux-qualif-draft`, `flux-qual-form-draft`, `flux-prog-placement-draft`, `flux-prog-group-draft`, `flux-empty-relations-draft`, `flux-full-graph-draft`, `regulation-demo`, `regulation-tie-homogeneous`, `confrontation-multi-round`, `cup-draw-pending`, `cup-composition-partial`, `cup-composition-complete`, `cup-qf-sf`, `cup-sf-running`, `swiss-ready`) do **not** accept `:progress`.

### Aliases (compat — do not change silently)

| Alias | Resolves to | Notes |
| :--- | :---------- | :---- |
| `group-stage-mid` | `groups:running` | Historical shorthand |
| `finished` | `groups:finished` | Means that scenario, not “any finished seed” |
| `knockout-qf` | `cup:running` | **Historical only** — single-round cup (~16 teams, ~50% played). **Not** QF→SF. Multi-stage / from-slots demo = `cup-qf-sf` (no redirect). |

## Catalog taxonomy

| Catalog | CLI | Intention |
| :--- | :--- | :-------- |
| **Templates** | `--templates` | Inspired competitions (real-world names / logos). Approximation modulo Domain — not UX mid-states. |
| **Scenarios** | `--scenarios` | UX / métier / QA situations (lifecycle, Structure, Règlement, Confrontation, Flux). |

No third catalog. Règlement / Confrontation / Flux seeds are **scenarios**.

## Templates (inspired competitions)

Five templates today. Capacity demos — not full real calendars:

| Id | Approximation |
| :--- | :------------ |
| `ligue-1` | Championship, 18 clubs (JSON) — **`DoubleRoundRobin`**, 34 matchdays, PairMirror (`N×(N−1)` = 306). Capacity demo, not a real L1 calendar. Use `:finished` for Standing → `CompetitionOutcome`. |
| `champions-league` | Groups 8×4 · 32 clubs — **Groups capacity** demo (classic CL group-stage shape). **Not** UEFA League Phase; **not** Swiss; no KO. No competition Outcome (aligned Domain). |
| `world-cup` | Groups 8×4 → Top2 → R16→QF→SF → Final + Bronze played · PlacementAwards ranks 1–4 · **Completed** + `CompetitionOutcome`. `:progress` ignored. |
| `coupe-de-france` | Cup 32 · R32 **pairing draw** · Winner→Population **intents** · **Slot draws** R16→…→Final · PlacementAwards 1–2 · **Completed** + Outcome. `:progress` ignored. |
| `euro-across-groups` | Groups 6×4 → Top2 + **best 4 thirds** (`AcrossGroups` P=3) → R16 population → Slot Draw → QF→SF→Final · PlacementAwards 1–2 · **Completed** + Outcome. `:progress` ignored. |

Team lists live in embedded JSON under `MyClub.PlayUp.Development/Datasets/` (display name, short name, colors, `logoAsset`). Inspired templates also seed:

- **Rosters** — real-inspired squads from `Generators/squads.json` (players + staff) on every dataset club
- **Competition dates** — `scheduledStart` / `scheduledEnd` are applied **randomly** (sometimes both, sometimes start only, sometimes neither), using the dataset window when present
- **Match kickoffs** — a subset of matches get a `MatchPlacement` (date + dummy resource); others stay undated
- **Match sheets, goals, assists, cards, substitutions** — filled on played matches (`:running` / `:finished` / fixed completed templates). Yellow + red are authorized on seeded competitions.

**Seed logos (internal only):** crest/flag PNGs under `MyClub.PlayUp.Development/Assets/seed-logos/`. Datasets reference relative paths (e.g. `ligue-1/psg.png`); DevRunner imports them into Media on seed (`logo_media_id`). Not for redistribution or product DS.

## Scenarios

| Id | Progress? | Notes |
| :--- | :--- | :---- |
| `empty-workspace` | no | Empty Accueil list |
| `draft-empty` | no | Draft, 0 entries |
| `registration-open` | no | Partial registration (3/16) — construction problem |
| `registration-withdrawn` | no | Championship Running then 1 forfait (Withdraw) — operational problem |
| `championship-structure-draft` | no | Championship materialized + full composition, stays Draft (Structure E4 + Règlement championnat schematic) |
| `structure-graph-invalid` | no | Poules → Barrages → Finale/Bronze Draft with missing qual slots + multi-dest progression — Structure Topology / anomaly QA |
| `championship-ready` | no | Championship Ready (not started) |
| `championship-archived` | no | Championship Completed then Archived |
| `championship` / `groups` / `cup` / `random` | yes | Mono progressive. Cup = pairing draw applied + single principal round. Progressive seeds assign full root composition. `random` = meta-picker over championship\|groups\|cup (prefer explicit ids for QA) |
| `groups-suspended` | no | Groups mid-results then Suspended |
| `groups-draw-pending` | no | Groups + pot DrawRules + full composition, empty groups — Draft awaiting draw (E4) |
| `groups-to-ko-mid` | no | Groups 2×4 finished → Top2 in QF slots — KO Draft (healthy multi-phase mid) |
| `qual-auto-place-mid` | no | Qual Auto Place into QF mid — WhoFeeds = Qual |
| `qual-hybrid-auto-draw-mid` | no | Qual Top1 Auto + Top2 population + Draw mid |
| `prog-auto-place-mid` | no | Prog Auto Place into SF mid |
| `qual-form-to-champ-mid` | no | Qual ForForm → Championship FormPathResolutions mid |
| `flux-qualif-draft` | no | Structure flux: Groups Affectation + Qualif → QF Places — Draft |
| `flux-qual-form-draft` | no | Structure flux: Groups + directs → Champ ForForm — Draft |
| `flux-prog-placement-draft` | no | Structure flux: Demi Affectation → Finale/Bronze Prog + Attribution 1–4 — Draft |
| `flux-prog-group-draft` | no | Structure flux: Semi Winner → Groups ForGroup — Draft |
| `flux-empty-relations-draft` | no | Structure flux: Groups + QF sans arêtes — Draft |
| `flux-full-graph-draft` | no | Structure flux: Groups → Demis (Qualif) → Finale/Bronze (Prog + Attribution) — Draft |
| `regulation-demo` | no | Règlement hub: Groupes → Qual → KO A/R · Finale · ET+TAB · Draft |
| `regulation-tie-homogeneous` | no | Confrontation tokens: Finale A/R riche · Draft |
| `confrontation-multi-round` | no | Confrontation multi-tours + phase aval vide · Draft |
| `cup-draw-pending` | no | Cup bracket + entries, **composition empty (E0)**, no pairing draw. Contrast: `cup-composition-complete` / `cup:prepared` |
| `cup-composition-partial` | no | Cup 16, composition **10/16 (E1)** — Completer les entrées |
| `cup-composition-complete` | no | Cup 16, composition **16/16 (E2)** — Modifier + tirage pending |
| `cup-qf-sf` | no | Multi-stage QF→SF: QF played, SF slots occupied, **stops before** `materialize-from-slots` |
| `cup-sf-running` | no | QF done + SF materialized ~50% played — multi-phase Running |
| `swiss-8x3` | yes | Swiss 8×3; Matchdays via `GenerateNextRound` |
| `swiss-ready` | no | Swiss Ready (0 rounds yet) — also Règlement Swiss schematic |

Multi-stage **templates** `coupe-de-france` and `world-cup` also ignore `:progress` (fixed seed contracts).

### Structure QA quick start

Each scenario runs in its own seed scope (deterministic ids). Seed the full Structure matrix:

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios championship-structure-draft,structure-graph-invalid,championship-ready,swiss-ready,groups-draw-pending,cup-draw-pending,cup-composition-partial,cup-composition-complete,registration-open,registration-withdrawn,groups-suspended,championship-archived,groups-to-ko-mid,flux-qualif-draft,flux-qual-form-draft,flux-prog-placement-draft,flux-prog-group-draft,flux-empty-relations-draft,flux-full-graph-draft,cup-qf-sf,cup-sf-running,championship:running,groups:running,cup:prepared,swiss-8x3:running
```

**Composition / Entrées QA (minimal):**

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios cup-draw-pending,cup-composition-partial,cup-composition-complete,championship-structure-draft,groups-draw-pending
```

**Entrées / Sorties / Attribution QA (Structure flux):**

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios flux-empty-relations-draft,flux-qualif-draft,flux-qual-form-draft,flux-prog-placement-draft,flux-prog-group-draft,flux-full-graph-draft,groups-to-ko-mid,cup-qf-sf
```

**Règlement / Confrontation QA:**

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios regulation-demo,regulation-tie-homogeneous,confrontation-multi-round,championship-structure-draft,swiss-ready
```
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
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --scenarios cup-qf-sf,cup-sf-running,groups-to-ko-mid
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --scenarios championship-ready,groups-suspended,championship-archived
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --scenarios random:prepared --seed 7
```

Generation demos: `--templates ligue-1:finished` (Championship Outcome) · `--templates coupe-de-france` (cup + intents + draws + Outcome) · `--templates world-cup` (Groups→KO+Bronze + Outcome) · `--templates euro-across-groups` (AcrossGroups best thirds + Outcome) · `--scenarios cup-qf-sf` (mid-bracket from-slots) · `--scenarios swiss-8x3:running` (Swiss progressive rounds). See **Structure QA quick start** above for the full Structure matrix.

Reset is refused unless the DB name ends with `_dev`, the host is localhost/loopback, and the environment is not Production. Host has **no** reset/seed capability.

## Unsupported legacy regulation JSON (Progression / PlacementAward)

Cup V1 Paths are **PairKey-only** (`SourcePairKey` = `BracketPair.PairKey`). There is **no** application migration, dual-read, or fallback from the former FixtureId-based path identity (`SourceFixtureId`, Guid `"N"` as PairKey).

Data produced under that old model is **outside the supported contract**. Loading or applying it fails explicitly.

For a local `*_dev` database that still holds such rows: **purge and reseed** with DevRunner (`--reset` + templates/scenarios). Do not add compatibility code in Host or Domain.

## Useful commands

| Command | Purpose |
| :------ | :------ |
| `docker compose up -d` | Start PostgreSQL |
| `dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios groups:running` | Reset + seed |
| `dotnet test --filter Category!=Integration` | Fast suite |
| `dotnet test` | Full suite (Docker required for Integration) |
