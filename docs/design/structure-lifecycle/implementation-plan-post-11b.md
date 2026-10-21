# Implementation plan — post §11B sign-off

**Prerequisite:** Human signature on [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md) / Notion Décision §11B.

**Supersedes for active work:** [implementation-plan-placeholder.md](./implementation-plan-placeholder.md) (pre-11B historical).

---

## In scope after §11B (without new product decision)

| Track | Work | Trigger |
|-------|------|---------|
| **Regression** | Keep §12 **Done** rows green in CI | Any change touching stages/materialize/hub |
| **Doc sync** | Matrix §5 vs code drift | Review finding only |
| **OPS extension** | Additional `UseCaseExecutor.Stages` commands per §9 | **Notion Task** per command — not bulk guard |

---

## Explicitly deferred (reopen arbitration first)

| ID | Reopen when | Doc |
|----|-------------|-----|
| **G-MAT3** | M2a attainable or revised predicate | [g-mat3-arbitration-session.md](./g-mat3-arbitration-session.md) |
| **G-MAT4** | D3-1 equivalent M4-1 + M4a | [g-mat4-arbitration-session.md](./g-mat4-arbitration-session.md) |
| **G-OPS3** | **D3-1** + R-Draw1 positive test | [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md) |

---

## Out of scope until product call

- Domain-wide `EnsureStructureMutable` tier split (structural / game-reg / operational)
- Global `OperationalMutationGuard` replacing per-command guards
- Moving Prepare gates wholesale to Materialize
- Championship/Groups G-MAT1/G-MAT2 parity (Cup-only today)
- Hub persistent remediation for D1 consumers (beyond mutation footer shipped)

---

## Suggested priority if reopening one deferred item

1. **G-OPS3** — user-visible integrity (draw applied) once D3-1 signed  
2. **G-MAT3 / G-MAT4** — only if product revives positive materialize predicates  

Swiss: continue **constraint** model §8 — no new global guard.
