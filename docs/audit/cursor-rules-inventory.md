# Cursor rules inventory

Current list of `.cursor/rules/**/*.mdc` at tip `33feae5142a59addef50061c877f561d8c8c5371` (2026-10-27).  
**No content rewrite** of the rules — file list, scope, and one-line purpose only. Phase 0 / C8 / phase 8 notes stay at the bottom.

Entry point for agents: [`AGENTS.md`](../../AGENTS.md).

## Always-on (`alwaysApply: true`)

| File | Purpose |
| --- | --- |
| [`architecture.mdc`](../../.cursor/rules/architecture.mdc) | Modular monolith boundaries, Host flow (`UseCaseExecutor`, Media via `MediaService`), tooling isolation, anti-patterns (no MediatR-style bus, no Platform layer without a trigger) |
| [`workflow.mdc`](../../.cursor/rules/workflow.mdc) | Ask / Plan / Agent modes, Notion decisions, reference business cases |
| [`general.mdc`](../../.cursor/rules/general.mdc) | Minimal diffs, no unjustified abstractions, Central Package Management |
| [`documentation.mdc`](../../.cursor/rules/documentation.mdc) | Notion vs Git source of truth, English source comments |
| [`no-cargo-cult.mdc`](../../.cursor/rules/no-cargo-cult.mdc) | Require a real problem, alternatives, and a trade-off before a new pattern or library |

## Scoped product / tech

| File | Globs | Purpose |
| --- | --- | --- |
| [`csharp.mdc`](../../.cursor/rules/csharp.mdc) | `**/*.{cs,csproj,props,targets}` | C# / .NET naming, analyzers, Domain conventions, Host `Request` / `Dto` suffixes |
| [`testing.mdc`](../../.cursor/rules/testing.mdc) | `**/*Tests*/**/*.{cs,csproj}` | Behaviour-first tests, TestKit, Integration category, Development.Tests recette boundary |
| [`design-system.mdc`](../../.cursor/rules/design-system.mdc) | `src/PlayUp/web/**/*.{css,tsx,ts,md}` | Design-system evolution gate (foundations, Lab, page CSS); links `design/00` |
| [`playup-web-quality.mdc`](../../.cursor/rules/playup-web-quality.mdc) | `src/PlayUp/web/**/*.{ts,tsx,css,json}` | Play’Up web quality stack, data path, CI gates including `npm run build` |
| [`loading-states.mdc`](../../.cursor/rules/loading-states.mdc) | `src/PlayUp/web/**/*.{tsx,ts,css}` | Regional loader on first load vs in-place marker on background refresh |

## Design (`src/PlayUp/web/**`)

| File | Purpose |
| --- | --- |
| [`design/00-design-principles.mdc`](../../.cursor/rules/design/00-design-principles.mdc) | Source hierarchy (Notion / Lab / rules / code) and the workflow before UI work |
| [`design/01-brand.mdc`](../../.cursor/rules/design/01-brand.mdc) | Brand elements — lockup, wordmark, arrow, angle, gradient |
| [`design/02-tokens.mdc`](../../.cursor/rules/design/02-tokens.mdc) | Design tokens — colors, spacing, radius, borders, shadows |
| [`design/03-typography.mdc`](../../.cursor/rules/design/03-typography.mdc) | Typography roles, including numeric treatments |
| [`design/04-layout.mdc`](../../.cursor/rules/design/04-layout.mdc) | Width, gutters, asymmetric composition, density |
| [`design/05-surfaces.mdc`](../../.cursor/rules/design/05-surfaces.mdc) | Surface hierarchy — Canvas, Groupe, Panneau, Overlay |
| [`design/06-components.mdc`](../../.cursor/rules/design/06-components.mdc) | Canonical components and reuse rules |
| [`design/07-navigation.mdc`](../../.cursor/rules/design/07-navigation.mdc) | Navigation and shell rules |
| [`design/08-data-display.mdc`](../../.cursor/rules/design/08-data-display.mdc) | Scores, standings, trends, matchdays, counters |
| [`design/09-icons.mdc`](../../.cursor/rules/design/09-icons.mdc) | Icon style and usage |
| [`design/10-motion.mdc`](../../.cursor/rules/design/10-motion.mdc) | Utility transitions, live pulse, reduced motion |
| [`design/11-patterns.mdc`](../../.cursor/rules/design/11-patterns.mdc) | Recurring UI patterns — overview, next action, match hero, empty / loading / error |

## Covered without a dedicated rule (this lot)

Media and DevRunner stay on **`architecture.mdc`** plus the contributor guides that rule already names. This lot does **not** add `media.mdc` or `devrunner.mdc`.

| Topic | Rule | Guide |
| --- | --- | --- |
| Media (no Media Host; HTTP via Play’Up Host and `MediaService`) | `architecture.mdc` | [`docs/guides/media.md`](../guides/media.md) |
| DevRunner (Development + Infrastructure CLI; must not ship with Host) | `architecture.mdc` | [`docs/guides/local-persistence.md`](../guides/local-persistence.md) |
| Host network exposure (loopback, anonymous API in this phase) | Not duplicated in `.mdc` (phase 8) | [`docs/guides/host-exposure.md`](../guides/host-exposure.md) |
| Development.Tests may reference Infrastructure; not Host or DevRunner | `architecture.mdc`, `testing.mdc` | — |

## Counts

5 always-on · 5 scoped product/tech · 12 design rules (`00`–`11`) · **22** `.mdc` files.

`.cursor/commands/` (PR helpers, not rules): [`audit-before-pr.md`](../../.cursor/commands/audit-before-pr.md), [`cross-design-review.md`](../../.cursor/commands/cross-design-review.md).

## History

**Phase 0** snapshot at `93b09f161739f47b768caad63aed90d0d3c1469f` — file list and scope only; no content rewrite. The same 22 files are still present.

**Wave 5 (governance, no rewrites):** see [`phase-1-wave-5.md`](phase-1-wave-5.md) §4 — dual design entry (`design/00` vs `design-system.mdc`); Media/DevRunner not scoped by dedicated rules (still true).

**Lot C8 (2026-10-21):** targeted closure — `AGENTS.md` at repo root; `audit-before-pr` aligned with CI `web` (`lint:css`, `build`); `architecture.mdc` / `testing.mdc` document **Development.Tests** + Media/DevRunner pointers; `design-system.mdc` links `design/00-design-principles.mdc`. Dedicated Media/DevRunner rule files still **deferred**; see [`phase-3-lot-c8-plan.md`](phase-3-lot-c8-plan.md).

**Phase 8 reconciliation (2026-10-22):** [`phase-8-cursor-rules-reconciliation.md`](phase-8-cursor-rules-reconciliation.md) — no known conflict rules ↔ SoT; `playup-web-quality.mdc` example commands include `npm run build` (aligned with `audit-before-pr`).

**F-01 / F-09 partial (2026-10-27, tip `33feae51`):** docs and pointers only. `AGENTS.md` now links the Media, DevRunner, and host-exposure homes above. Re-check note: [`phase-8-cursor-rules-reconciliation.md`](phase-8-cursor-rules-reconciliation.md) §8. Still no dedicated Media or DevRunner `.mdc`. Stages / `OverviewAssembler` / `StructureViewAssembler`, MediatR, and a Platform layer stay out of this lot.
