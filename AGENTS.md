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

Media and DevRunner keep this coverage: dedicated `.mdc` files stay deferred.

Before a PR, use the **Audit before PR** command (`.cursor/commands/audit-before-pr.md`) — align local checks with CI, especially `npm run build` for Play’Up web.

Product and durable decisions: **Notion** (MyClub project). Git contributor docs: `docs/guides/`, `CONTRIBUTING.md`.
