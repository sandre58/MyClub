# Documentation

Contributor-facing documentation for **MyClub**. Product vision, detailed architecture, and decisions live in **Notion**. This folder keeps Git-side pointers and developer guides.

| Document | Description |
| :------- | :---------- |
| [Guides](guides/README.md) | Developer guides (growing with the codebase) |
| [Local persistence](guides/local-persistence.md) | PostgreSQL 18 via Docker Compose, secrets, migrations, Compose vs Testcontainers |
| [HTTP API contract](guides/http-api-contract.md) | Phase 12.8 Host JSON conventions (string enums, named DTOs, ProblemDetails) |

## Canonical sources

- **Code / CI / releases:** this GitHub repository
- **Vision / products / architecture / decisions:** Notion project *MyClub*
- **Play'up technical reference:** Notion *Architecture technique*
- **Development workflow:** Notion Forge *Workflow de développement* (ecosystem SoT) and MyClub *Workflow de développement* (project application); includes reference business cases / Phase Contract for important business phases; Cursor summary in `.cursor/rules/workflow.mdc`
