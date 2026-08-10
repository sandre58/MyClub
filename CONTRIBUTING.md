# Contributing to MyClub

Thank you for contributing.

## Development setup

**Prerequisites**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (see `global.json`)
- Git

```bash
dotnet restore
dotnet build
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

Product workflow (Intent → Reconciliation, Cursor Ask / Plan / Agent modes, Notion Décisions) is documented in Notion — Forge *Workflow de développement* (ecosystem SoT) and MyClub *Workflow de développement* (roles / local links). Agent behaviour: `.cursor/rules/workflow.mdc`. Do not add ADR Markdown under this repo.

## Pull requests

1. Branch from `main` (`feature/…` or `bugfix/…`).
2. Keep changes focused.
3. Run build and tests locally.
4. Fill the [pull request template](.github/PULL_REQUEST_TEMPLATE.md).

**Commit messages:** [Conventional Commits](https://www.conventionalcommits.org/).

## Coding standards

- Target **`net10.0`**.
- Nullable reference types enabled.
- Follow [.editorconfig](.editorconfig) and [StyleCop](stylecop.json).
- Document public APIs with XML comments when packing libraries.

## Dependencies

Central Package Management via [`Directory.Packages.props`](Directory.Packages.props).

- Add a `PackageVersion` entry, then reference the package in the `.csproj` **without** a version.
- Shared test packages: [`build/dependencies.props`](build/dependencies.props).
- Analyzers: [`build/code-analysis.props`](build/code-analysis.props).

## Testing

- Place tests in `tests/` with project names ending in `Tests`.
- Domain tests live in `tests/MyClub.PlayUp.Domain.Tests` and reference `MyClub.PlayUp.Domain` only.
- Application tests live in `tests/MyClub.PlayUp.Application.Tests` and reference Application + Domain.

## Security

Follow [SECURITY.md](SECURITY.md) — do not open public issues for vulnerabilities.

Repository: https://github.com/sandre58/MyClub
