# Cursor rules inventory (phase 0)

Snapshot at execution SHA `93b09f161739f47b768caad63aed90d0d3c1469f`.  
**No content rewrite** in phase 0 — file list and scope only.

| File | Scope | SoT / notes |
| --- | --- | --- |
| `architecture.mdc` | alwaysApply | Modular monolith, Play’Up/Media boundaries, UseCaseExecutor, tooling isolation |
| `workflow.mdc` | alwaysApply | Ask / Plan / Agent, Notion decisions, reference cases |
| `general.mdc` | alwaysApply | Simplicity, dependencies, CPM |
| `documentation.mdc` | alwaysApply | Notion vs Git SoT, comment style |
| `no-cargo-cult.mdc` | alwaysApply | Frontend/backend anti-patterns |
| `csharp.mdc` | `**/*.cs` | Naming, analyzers, Domain conventions |
| `testing.mdc` | tests | Test conventions |
| `design-system.mdc` | web DS | DS evolution gate, Lab vs Foundations |
| `playup-web-quality.mdc` | web | Oxlint, Prettier, TanStack Query, `api.ts` |
| `loading-states.mdc` | web loading UX | Regional vs background refresh |
| `design/00-design-principles.mdc` | design | Notion vs Lab vs rules hierarchy |
| `design/01-brand.mdc` … `11-patterns.mdc` | design | Tokens, components, shell, patterns |

**Counts:** 5 global alwaysApply · 4 product/tech scoped · 12 design numbered rules · 2 `.cursor/commands` (PR helpers, not rules).

**Wave 5 (governance, no rewrites):** see `phase-1-wave-5.md` §4 — dual design entry (`design/00` vs `design-system.mdc`), `audit-before-pr` suggests `typecheck` without `build` (masks **AUD-0002**), no `AGENTS.md`, Media/DevRunner not scoped by dedicated rules.

**Lot C8 (2026-10-21):** targeted closure — `AGENTS.md` at repo root; `audit-before-pr` aligned with CI `web` (`lint:css`, `build`); `architecture.mdc` / `testing.mdc` document **Development.Tests** + Media/DevRunner pointers; `design-system.mdc` links `design/00-design-principles.mdc`. Dedicated Media/DevRunner rule files still **deferred**; see `phase-3-lot-c8-plan.md`.
