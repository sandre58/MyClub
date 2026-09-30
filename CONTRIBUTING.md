# Contributing to MyClub

Thank you for contributing.

## Development setup

**Prerequisites**

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0) (see `global.json`)
- Git
- Docker Desktop — local PostgreSQL via `compose.yml`, and Testcontainers for `Category=Integration` tests

```bash
dotnet restore
dotnet build
dotnet test --filter Category!=Integration
```

**Local Host database:** copy `.env.example` → `.env`, run `docker compose up -d`, set Host User Secrets `ConnectionStrings:PlayUp`, apply migrations. Step-by-step: [docs/guides/local-persistence.md](docs/guides/local-persistence.md).

Full suite, including Testcontainers PostgreSQL tests (ephemeral; not the Compose volume):

```bash
dotnet test
```

Optional coverage:

```bash
dotnet test /p:CollectCoverage=true
```

Local commit message template:

```bash
git config commit.template .gitmessage
```

## Development workflow

Product workflow (Intent → Reconciliation, Cursor Ask / Plan / Agent modes, Notion Décisions, reference business cases for important business phases) is documented in Notion — Forge *Workflow de développement* (ecosystem SoT) and MyClub *Workflow de développement* (roles / local links). Agent behaviour: `.cursor/rules/workflow.mdc`. Do not add ADR Markdown under this repo.

## Pull requests

1. Branch from `main` (`feature/…` or `bugfix/…`).
2. Keep changes focused.
3. Run build and tests locally (`dotnet test --filter Category!=Integration`; full `dotnet test` when Docker is available).
4. Fill the [pull request template](.github/PULL_REQUEST_TEMPLATE.md).

**Commit messages:** [Conventional Commits](https://www.conventionalcommits.org/).

## Coding standards

- Target **`net11.0`**.
- Nullable reference types enabled.
- Follow [.editorconfig](.editorconfig) and [StyleCop](stylecop.json).
- Document public APIs with XML comments when packing libraries.

## Dependencies

Central Package Management via [`Directory.Packages.props`](Directory.Packages.props).

- Add a `PackageVersion` entry, then reference the package in the `.csproj` **without** a version.
- Shared test packages: [`build/dependencies.props`](build/dependencies.props).
- Analyzers: [`build/code-analysis.props`](build/code-analysis.props).

### Local MyNet packages (optional)

By default (and in CI), MyNet packages are restored from **nuget.org** at the version pinned by `MyNetVersion` in `Directory.Packages.props`.

To iterate against a sibling MyNet checkout (`../MyNet`) without publishing:

1. In **MyNet**, create `Directory.Build.local.props` that imports `build/local-nuget-pack.props`, then `dotnet build` (writes `0.0.0-local` packages to `MyNet/packages`).
2. In **MyClub**, copy [`Directory.Build.local.props.example`](Directory.Build.local.props.example) to `Directory.Build.local.props` (gitignored), then `dotnet restore` / `dotnet build`.

Without `Directory.Build.local.props`, behaviour matches CI (published NuGets only).

## Testing

- Place tests in `tests/` with project names ending in `Tests`.
- Domain tests live in `tests/MyClub.PlayUp.Domain.Tests` and reference `MyClub.PlayUp.Domain` only.
- Application tests live in `tests/MyClub.PlayUp.Application.Tests` and reference Application + Domain.
- Infrastructure tests live in `tests/MyClub.PlayUp.Infrastructure.Tests` and reference Infrastructure + Application + Domain.
- Tests marked `Category=Integration` require Docker (Testcontainers PostgreSQL). Skip them locally with `--filter Category!=Integration`. Do not point those tests at the Compose `myclub` database.
- EF migrations live in `src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Migrations`. Restore `dotnet-ef` via `dotnet tool restore` (see `.config/dotnet-tools.json`). Apply with `dotnet ef database update --project src/PlayUp/MyClub.PlayUp.Infrastructure --startup-project src/PlayUp/MyClub.PlayUp.Host`.
- Never commit `.env`, User Secrets, or real connection strings. Use `.env.example` as the Compose template only.
- Prefer `docker compose down` over `docker compose down -v` unless you intentionally want to delete the local `myclub-postgres-data` volume.

## Security

Follow [SECURITY.md](SECURITY.md) — do not open public issues for vulnerabilities.

Repository: https://github.com/sandre58/MyClub
