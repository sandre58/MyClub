# Phase 1 — Wave 2: Architecture and backend

**Launched:** 2026-10-21  
**Branch:** `feature/new_version`  
**SHA vague:** `93b09f161739f47b768caad63aed90d0d3c1469f`  
**Comparison to `main`:** not performed (per SHA arbitration).

No code changes. Findings registered in Notion (`AUD-0008`…).

## 1. Architecture and dependencies

### Observed graph (Play’Up + Media)

```text
Host → Domain, Application, Infrastructure, Media.Infrastructure
Application → Domain
Infrastructure → Application, Domain
Media.Infrastructure → Media.Application, Media.Domain
Media.Application → Media.Domain
Development → Domain, Application, TestKit, Media.Application  (no Infrastructure/Host)
DevRunner → Development, Infrastructure, Media.Infrastructure
TestKit → Domain, Application only
```

Automated checks (passing on execution SHA):

- `AssemblyDependencyTests` — Domain/Application do not reference EF/Npgsql/AspNet assemblies.
- `HostArchitectureIsolationTests` — Host csproj and sources exclude Development/TestKit/DevRunner.
- `TestKitArchitectureIsolationTests` — TestKit → Domain + Application only.
- `DevelopmentArchitectureIsolationTests` — Development csproj excludes Infrastructure/Host/DevRunner.
- CI publish step — Development/DevRunner/TestKit DLLs absent from Host publish output (phase 0).

**Gap:** No automated test forbids Application→Infrastructure, Domain→Application, or circular project references beyond the checks above. Dependency discipline relies on `.csproj` hygiene and review.

**Host → Domain (explicit):** Documented in `MyClub.PlayUp.Host.csproj` (H-02) for HTTP contracts and mappers (`StructureRequestMapper`, `*Request` records). Not a layer violation by project rules; increases coupling at the HTTP edge.

## 2. DDD and domain

- Business mutations live on Domain aggregates (Stage, Match, Competition, etc.); Application exposes **static** use cases (`*Execute` / `*.cs` per feature folder).
- Lifecycle / Start guards emit domain/application error codes (e.g. `StartBlockedUnmaterialized`, `StartBlockedIncompleteEntryRound`) — see **AUD-0004** (wave 1) for recette vs ordering.
- Read-side presentation rules also live in Application (`StructureViewAssembler`, `StructureStageLifecycleAssembler`, attention collectors). Boundary between **domain truth** and **organizer read model** is a phase 2 arbitration topic, not a defect by itself.

## 3. Application and orchestration

- **`UseCaseExecutor`** (partials ~2.9k lines total; **`UseCaseExecutor.Stages.cs` ~1130 lines**) is the sole command/read HTTP orchestrator for Play’Up: load ports → static use case → **one** `SaveChangesAsync` per command; reads never save (documented in type XML).
- **Not** MediatR/generic bus — aligned with `.cursor/rules/architecture.mdc`.
- **Development** scenarios call `IUnitOfWork.SaveChangesAsync` outside the executor (allowed tooling path per architecture rules).

## 4. Persistence

- Play’Up: `PlayUpDbContext` + repositories in Infrastructure; **no** `ExecuteSql` / `FromSql` in `src/` (raw SQL only in Infrastructure **tests** for constraint probes).
- Media: separate `MediaDbContext`, PostgreSQL schema **`media`**, migrations under `MyClub.Media.Infrastructure`; binaries via `LocalFileMediaStorage`.
- Host `Program.cs`: `ConnectionStrings:Media` defaults to Play’Up connection when unset — **same server, separate schema** (by design).
- `StageRepository` uses deep `Include` / `AsSplitQuery` graphs for load capabilities — legitimate for aggregate hydration; **volume/latency risk** on hot paths (investigate under performance axis, wave 5).

## 5. Host and HTTP

- Minimal APIs grouped by `*Endpoints.cs`; Play’Up commands/read via **`UseCaseExecutor`**; Media via **`MediaService`** (intentional per architecture rules).
- **No** `AddAuthentication` / `UseAuthorization` / `[Authorize]` in Host — see **AUD-0008**.
- Errors: `PlayUpExceptionHandler`, `MediaExceptionHandler`, ProblemDetails + `correlationId` extension.
- Wire contract documented in `docs/guides/http-api-contract.md` (enums as strings, camelCase).

## 6. Satellite projects

| Project | Role | Isolation |
| --- | --- | --- |
| **Media** | Upload/serve logos; schema `media`; composed by Host | No Media Host; HTTP only through Host |
| **TestKit** | Situations for tests + Development | Enforced |
| **Development** | Scenarios, seeds, recette helpers | No Infrastructure in csproj |
| **DevRunner** | CLI composition root | Development + Infrastructure |
| **Development.Tests** | Integration/recette (Testcontainers) | **References Infrastructure** while Development does not — see **AUD-0011** |

## Alignment summary (no register entry)

- Modular monolith boundaries largely match documented rules and enforced tests.
- `UseCaseExecutor` + static use cases match the chosen Host surface (no mediator).
- Media module integration matches “MediaService, not executor” rule.
