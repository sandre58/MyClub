# Agent context (MyClub)

Cursor loads project guidance from **`.cursor/rules/`**. Inventory: [`docs/audit/cursor-rules-inventory.md`](docs/audit/cursor-rules-inventory.md).

| Always-on | Scoped |
| --- | --- |
| `architecture.mdc`, `workflow.mdc`, `general.mdc`, `documentation.mdc`, `no-cargo-cult.mdc` | `csharp.mdc`, `testing.mdc`, `playup-web-quality.mdc`, `design-system.mdc`, `loading-states.mdc`, `design/00`–`11` |

Pointers that are easy to miss from this page alone (existing rules and guides; no new rule files):

| Topic | Where it is documented |
| --- | --- |
| Media boundaries, `MediaService`, Host composition | `architecture.mdc`; [`docs/guides/media.md`](docs/guides/media.md) |
| DevRunner (CLI composition root; must not ship with Host) | `architecture.mdc`; [`docs/guides/local-persistence.md`](docs/guides/local-persistence.md) |
| Host project references (Development, DevRunner, TestKit) | `architecture.mdc` |
| Host network exposure | [`docs/guides/host-exposure.md`](docs/guides/host-exposure.md) |
| Development.Tests (Infrastructure allowed; Host and DevRunner forbidden) | `architecture.mdc`, `testing.mdc` |
| Design source hierarchy (Notion / Lab / rules / code) | `design/00-design-principles.mdc`, `design-system.mdc` |
| HTTP wire contract (`Request` / `Dto`) | `csharp.mdc`; [`docs/guides/http-api-contract.md`](docs/guides/http-api-contract.md) |
| MediatR-style dispatch, Platform layer | `architecture.mdc`, `no-cargo-cult.mdc` |
| Organisation hotspots (Class A/B/C) | [`docs/audit/organization-hotspots.md`](docs/audit/organization-hotspots.md) |

Media and DevRunner keep this coverage: dedicated `.mdc` files stay deferred.

Before a PR, use the **Audit before PR** command (`.cursor/commands/audit-before-pr.md`) — align local checks with CI, especially `npm run build` for Play’Up web.

Product and durable decisions: **Notion** (MyClub project). Git contributor docs: `docs/guides/`, `CONTRIBUTING.md`.

## Cursor Cloud specific instructions

Cloud Agent `start` brings up the local stack. Contributor setup stays in `CONTRIBUTING.md` and `docs/guides/local-persistence.md`.

- Docker uses the `fuse-overlayfs` storage driver. PostgreSQL 18 is the `compose.yml` service. The dev database is `myclub_dev` (the name must end with `_dev`).
- Host listens on `http://127.0.0.1:5287` (tmux session `playup-host`). Vite listens on `http://127.0.0.1:5173` (tmux session `playup-web`) and proxies API paths to the Host. Keep the Host on loopback (`docs/guides/host-exposure.md`).
- `start` writes gitignored `.env`, sets user secrets for id `MyClub.PlayUp.Host` (`ConnectionStrings:PlayUp`, `PlayUpDev`, `Media`), and applies EF migrations when the Host is not already ready. `dotnet ef` needs `--context PlayUpDbContext` or `--context MediaDbContext` because the Host references both.
- `GET /health/ready` checks Host readiness. `POST /competitions` with `{"name":"..."}` creates a competition.
- `dotnet test --filter Category!=Integration` does not need Docker or the Compose database. Tests tagged `Category=PerformanceBaseline` are also `Category=Integration` (Testcontainers). Full `dotnet test` needs Docker the same way.
- Web checks from `src/PlayUp/web`: `npm run format:check`, `npm run lint`, `npm run lint:css`, `npm run typecheck`, `npm run test:run`, `npm run build`.
- Backend checks before each push: `dotnet build MyClub.slnx -c Release`, `dotnet test`, `.github/scripts/verify-dotnet-warnings-baseline.sh`.
- Always open PRs as draft; never mark ready or merge (the owner does). Fix review nits on the same branch, no separate "Nits" PR.
