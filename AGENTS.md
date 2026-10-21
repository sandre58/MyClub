# Agent context (MyClub)

Cursor loads project guidance from **`.cursor/rules/`**. Inventory and phase-0 snapshot: [`docs/audit/cursor-rules-inventory.md`](docs/audit/cursor-rules-inventory.md).

| Always-on | Scoped |
| --- | --- |
| `architecture.mdc`, `workflow.mdc`, `general.mdc`, `documentation.mdc`, `no-cargo-cult.mdc` | `csharp.mdc`, `testing.mdc`, `playup-web-quality.mdc`, `design-system.mdc`, `loading-states.mdc`, `design/00`–`11` |

Before a PR, use the **Audit before PR** command (`.cursor/commands/audit-before-pr.md`) — align local checks with CI, especially `npm run build` for Play’Up web.

Product and durable decisions: **Notion** (MyClub project). Git contributor docs: `docs/guides/`, `CONTRIBUTING.md`.
