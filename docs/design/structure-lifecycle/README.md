# Structure phase lifecycle — design artefacts

Product and technical references for **stage lifecycle** (Draft / Ready / Running), **mutability** (structure vs operational vs production), and **diagnostic policy** (Prepare, Materialize, Start, drawer).

## Documents

| Document | Purpose |
|----------|---------|
| [phase-mutability-matrix.md](./phase-mutability-matrix.md) | Exhaustive inventory (diagnostics + `UseCaseExecutor.Stages` commands), current vs target policy, reference cases, open arbitrations |
| [notion-decision-draft.md](./notion-decision-draft.md) | Short **Décision** text for Notion (copy/paste); links to Git annex |
| [implementation-plan-placeholder.md](./implementation-plan-placeholder.md) | Pre-§11B placeholder (historical) |
| [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md) | **§11B** maintainer pack (signed 2026-10-15) |
| [implementation-plan-post-11b.md](./implementation-plan-post-11b.md) | Scope **after** §11B signature |
| [notion-decision-11b-draft.md](./notion-decision-11b-draft.md) | Notion copy block §11B |
| [g-mat1-confrontation-undecidable-at-materialize.md](./g-mat1-confrontation-undecidable-at-materialize.md) | **G-MAT1** — Done (Cup gate) |
| [g-mat2-authored-award-unreachable-at-materialize.md](./g-mat2-authored-award-unreachable-at-materialize.md) | **G-MAT2** — Done (Cup gate, option B) |
| [g-ops-ready-operational-mutations-and-d1.md](./g-ops-ready-operational-mutations-and-d1.md) | **G-OPS1 + G-OPS2** — **Done** (qual/prog/affectation; R-D1a/b) |
| [g-mat3-non-root-unfed-at-materialize.md](./g-mat3-non-root-unfed-at-materialize.md) | **G-MAT3** — spec draft (R-Mat-M2) |
| [g-mat3-arbitration-session.md](./g-mat3-arbitration-session.md) | **G-MAT3** — décision signée (gate deferred, M2b) |
| [g-mat4-source-population-shortfall-at-materialize.md](./g-mat4-source-population-shortfall-at-materialize.md) | **G-MAT4** — spec draft |
| [g-mat4-arbitration-session.md](./g-mat4-arbitration-session.md) | **G-MAT4** — décision signée (gate deferred, M4b) |
| [g-ops3-draw-refuse-on-composition-change.md](./g-ops3-draw-refuse-on-composition-change.md) | **G-OPS3** — spec draft |
| [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md) | **G-OPS3** — décision signée (gate deferred, R-Draw1 neg) |

## Related Git docs

- [Structure phase fiche truth sources](../structure-phase-refonte/truth-sources.md)
- [Actions & permissions inventory](../structure-phase-refonte/actions-permissions-inventory.md)

## Status

**Signed policy (2026-10-15):** §11A in [phase-mutability-matrix.md](./phase-mutability-matrix.md); arbitrations #1–#3 closed §5.2–§8; G-UI1/G-UI2 shipped.  
**Notion:** [Décision §11A](https://app.notion.com/p/3fa75fb92d108172b028d6858d0ea45b?pvs=204) (Acceptée 2026-10-15).  
**G-MAT3 (2026-10-15):** gate deferred — [decision](./g-mat3-arbitration-session.md), **R-Mat-M2b** test.  
**G-MAT4 (2026-10-15):** gate deferred — [decision](./g-mat4-arbitration-session.md), **R-Mat-M4b** + analyzer + overlap tests.  
**G-OPS3 (2026-10-15):** gate deferred — [decision](./g-ops3-arbitration-session.md).  
**§11B (2026-10-15):** **signed** — [signoff pack](./section-11b-signoff-preparation.md) · [Notion copy](./notion-decision-11b-draft.md) · matrix §11B ticked. Post-sign work: [implementation-plan-post-11b.md](./implementation-plan-post-11b.md).
