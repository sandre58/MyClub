<div align="center">

# MyClub

**Software suite for amateur sports clubs** — competitions, teams, and training as focused products, starting with Play'up.

[![License](https://img.shields.io/github/license/sandre58/MyClub?style=for-the-badge)](https://github.com/sandre58/MyClub/blob/main/LICENSE)
[![GitHub issues](https://img.shields.io/github/issues/sandre58/MyClub?style=for-the-badge)](https://github.com/sandre58/MyClub/issues)
[![Contributors](https://img.shields.io/github/contributors/sandre58/MyClub?style=for-the-badge)](https://github.com/sandre58/MyClub/graphs/contributors)
[![Last commit](https://img.shields.io/github/last-commit/sandre58/MyClub/main?style=for-the-badge)](https://github.com/sandre58/MyClub/commits/main/)
[![Repo size](https://img.shields.io/github/repo-size/sandre58/MyClub?style=for-the-badge)](https://github.com/sandre58/MyClub)

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Language](https://img.shields.io/github/languages/top/sandre58/MyClub?style=for-the-badge)](https://github.com/sandre58/MyClub/search?l=c%23)
[![Architecture](https://img.shields.io/badge/Architecture-DDD%20%7C%20Modular%20Monolith-2EA44F?style=for-the-badge)](docs/README.md)

[![CI](https://github.com/sandre58/MyClub/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/sandre58/MyClub/actions/workflows/ci.yml)

[![Semantic Versioning](https://img.shields.io/badge/SemVer-2.0.0-3C1E70?style=for-the-badge)](https://semver.org/)
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-FE5196?style=for-the-badge)](https://www.conventionalcommits.org/)

[![GitHub stars](https://img.shields.io/github/stars/sandre58/MyClub?style=social)](https://github.com/sandre58/MyClub/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/sandre58/MyClub?style=social)](https://github.com/sandre58/MyClub/network/members)

[Documentation](docs/README.md) · [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Report a bug](https://github.com/sandre58/MyClub/issues)

</div>

---

## Overview

**MyClub** is a suite of specialized applications that help amateur sports clubs (starting with football) modernize day-to-day organization: competitions, teams, and training — without replacing volunteers with enterprise software they do not need.

| Highlight | Description |
| :-------- | :---------- |
| **Product suite** | Independent apps that stay valuable alone and stronger together. |
| **Domain-first** | Rich domain model, clean architecture, long-term maintainability. |
| **Play'up first** | Customizable amateur competitions are the current implementation focus. |
| **Quality-minded** | Automated tests, analyzers, Conventional Commits, and GitHub Actions CI. |

Vision, product roadmap, and architectural decisions are maintained in **Notion**. This repository is the source of truth for **code**, issues, pull requests, and releases.

---

## Vision

Amateur clubs often run on spreadsheets, messaging apps, and tools that do not match sports workflows. MyClub aims to become a practical digital toolbox for volunteers, coaches, and organizers — simple, specialized, and adapted to amateur constraints.

Initial domain: **amateur football**.

---

## Products

| Product | Role | Status |
| :------ | :--- | :----- |
| **Play'up** | Create and run customizable competitions (formats, rules, fixtures, results, standings, stats). | **In development** (Host: PrepareStage, ApplyProgressionOutcome, StartMatch, FinishMatch) |
| **Team'up** | Day-to-day team life (rosters, convocations, attendance). | Planned |
| **Train'in** | Training session design and follow-up. | Future |

**Play'up** is designed to work **autonomously**: participants can be managed without requiring personal accounts. Catalog/templates are an application capability for bootstrapping competitions — not a separate bounded context.

---

## Architecture

**Target style:** Modular Monolith with Domain / Application / Infrastructure / Host for each product (Play'up first). No shared “kitchen-sink” library and no Platform layer until a real trigger exists.

**Current codebase:** Domain + Application use cases + Infrastructure (EF Core / PostgreSQL) + Host Minimal APIs (`PrepareStage`, `ApplyProgressionOutcome`, `StartMatch`, `FinishMatch`). Application also orchestrates `PublishDraw` (not yet HTTP).

```text
src/
└── PlayUp/
    ├── MyClub.PlayUp.Domain/           # Competition · Stage · Match · Common · Rules · Scheduling
    ├── MyClub.PlayUp.Application/      # Use cases + ports + UseCaseExecutor (named methods)
    ├── MyClub.PlayUp.Infrastructure/   # PlayUpDbContext, AR mappings, migrations, PostgreSQL DI
    └── MyClub.PlayUp.Host/             # Composition root · Minimal APIs
tests/
    ├── MyClub.PlayUp.Domain.Tests/
    ├── MyClub.PlayUp.Application.Tests/
    ├── MyClub.PlayUp.Infrastructure.Tests/ # Unit + Testcontainers PostgreSQL (Category=Integration)
    └── MyClub.PlayUp.Host.Tests/           # WebApplicationFactory + Testcontainers (Category=Integration)
```

**Dependency flow:**

```text
Domain  ←  Application  ←  Infrastructure
                ↑
               Host (composition root + Minimal APIs)
```

Details and naming conventions live in the Notion project *MyClub* (architecture pages and **Décisions** database) — not as ADR Markdown in this repository.

This repository is an **application suite**: it does **not** publish NuGet packages today (`IsPackable=false` by default).

---

## Tech stack

| Area | Choice |
| :--- | :----- |
| Runtime | .NET 10 LTS |
| Style | DDD, Modular Monolith |
| API | ASP.NET Core Minimal APIs (Host; PrepareStage, ApplyProgressionOutcome, StartMatch, FinishMatch) |
| Persistence | EF Core 10 + PostgreSQL (Competition, Stage, Match; Testcontainers integration tests) |
| Tests | xUnit, FluentAssertions, Moq (via Central Package Management) |
| Quality | Nullable, StyleCop / Roslynator / NetAnalyzers, Coverlet |
| Versioning | GitVersion + SemVer + Conventional Commits |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (see [`global.json`](global.json))
- Git
- Docker (optional; required for PostgreSQL integration tests)

---

## Getting started

```bash
dotnet restore
dotnet build
dotnet test --filter Category!=Integration
```

PostgreSQL integration tests (`Category=Integration`) use Testcontainers and need Docker:

```bash
dotnet test
```

Optional coverage:

```bash
dotnet test /p:CollectCoverage=true
```

---

## Repository layout

| Path | Purpose |
| :--- | :------ |
| `src/PlayUp/` | Play'up product projects |
| `tests/` | Test projects (`*Tests`) |
| `build/` | Shared MSBuild props |
| `docs/` | Contributor docs (pointers to Notion; guides) |
| `.config/` | Local .NET tools (`dotnet-ef`) |
| `.cursor/rules/` | AI assistant conventions for this repo |

---

## Conventions

- **Commits:** [Conventional Commits](.gitmessage)
- **Branches:** `main` / `feature/*`
- **Code:** `net10.0`, nullable enabled, [`.editorconfig`](.editorconfig), [`stylecop.json`](stylecop.json)
- **Packages:** Central Package Management — [`Directory.Packages.props`](Directory.Packages.props)
- **Public docs:** English (`README`, `CONTRIBUTING`, `SECURITY`, contributor `docs/**`)
- **Decisions:** Notion **Décisions** database — never ADR Markdown in Git

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Please run `dotnet build` and `dotnet test --filter Category!=Integration` before opening a pull request. Run the full `dotnet test` suite when Docker is available.

Security reports: [SECURITY.md](SECURITY.md).

---

## License

[MIT](LICENSE) © Stéphane ANDRE
