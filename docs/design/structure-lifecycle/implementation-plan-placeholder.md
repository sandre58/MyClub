# Implementation plan (placeholder)

**Status:** **Historical (pre-§11B).** Active scope after sign-off: [implementation-plan-post-11b.md](./implementation-plan-post-11b.md). §11B preparation: [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md).

## Slices (order suggested)

1. **Domain mutability tiers** — Split `EnsureStructureMutable` into structural / game-regulation / operational (Ready, no production) without changing M-gate semantics until Open #2 is closed.
2. **Application `OperationalMutationGuard`** — Ready + `!StageAttachedProduction.HasAttachedMatches` + **D1** peer Ready check on exit mutations (new error code + i18n).
3. **Draw conflict policy** — Refuse operational mutations that invalidate published applied draw (prefer refuse over silent cancel); align Apply/Release paths.
4. **Prepare policy** — Adjust only where matrix marks Δ vs today (likely **not** Open #1 until decided).
5. **Materialize command + read** — Align `MaterializeMatches`, `MaterializeCupFromOccupiedSlots`, `StructureMaterializeAssembler` with signed C/M columns.
6. **Hub + SPA permissions** — `BuildStageActions`, structure fiche predicates, parity [actions-permissions-inventory.md](../structure-phase-refonte/actions-permissions-inventory.md).
7. **Drawer / attention single policy** — Host `StructurePresentedAttentionCollector` + web `structureDiagnosticSeverity.ts` from signed matrix (no third policy).
8. **Tests** — One integration/reference test per row in matrix §10 marked **New**.

## Swiss (slice 1b or parallel)

Only after **Open #3**: document capability row in matrix, then implement **capability-based** checks (not an ad-hoc exception on a global production guard).

## Explicit non-goals

- Refactoring `CompetitionFlowAnalyzer` fact layer
- Moving all Prepare gates to Materialize wholesale
- Changing `ReplaceStandingRules` Running behaviour without product call
