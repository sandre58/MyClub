# Structure phase lifecycle — design artefacts

Product and technical references for **stage lifecycle** (Draft / Ready / Running), **mutability** (structure vs operational vs production), and **diagnostic policy** (Prepare, Materialize, Start, drawer).

## Documents

| Document | Purpose |
|----------|---------|
| [phase-mutability-matrix.md](./phase-mutability-matrix.md) | Exhaustive inventory (diagnostics + `UseCaseExecutor.Stages` commands), current vs target policy, reference cases, open arbitrations |
| [ctrl-o3-mat-devrunner-recette.md](./ctrl-o3-mat-devrunner-recette.md) | **CTRL-O3-MAT** gate — DevRunner `ctrl-o3-mat-recette` + automated read/SPA assertions (Suspended / production UI); includes **Δ-drawer-1** drawer recette |
| [delta-03-hub-p1-fiche.md](./delta-03-hub-p1-fiche.md) | **Δ-03** closed — hub P1 draw rules Draft-only in Ready (C-19 / B-Q1b); manual recette + tests |
| [delta-o4-completed-fiche.md](./delta-o4-completed-fiche.md) | **O-04** validé — **Δ-O4-2** `ec45ed60` + **Δ-O4-1** `68d0f50a` ; recette comparative Championnat OK |
| [c-18-rename-stage-completed-audit.md](./c-18-rename-stage-completed-audit.md) | **C-18** — audit lecture seule hub/API/domaine `RenameStage` en `Completed` (arbitrage métier avant impl.) |
| **Notion — Matrice UI Structure (présentation par statut)** | [Gel produit 2026-10-15](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) — N2 presentation by phase status; **Δ-O3-1/2** + **Δ-drawer-1** + **Δ-03** + **Δ-O4-2** + **Δ-O4-1** closed (`d7460d0b` · `353b991d` · `440eabb6` · `5f7a49b9` · `ec45ed60` · `68d0f50a`). Post-gel backlog: capacity **C-18** / **C-20**. |
| [structure-diagnostic-presentation-contract.md](./structure-diagnostic-presentation-contract.md) | **Signed** — UI severity/tone/copy for structure diagnostics (presentation only) |
| [structure-diagnostic-presentation-technical-audit.md](./structure-diagnostic-presentation-technical-audit.md) | Pre-implementation audit — codes, gaps, parity plan |
| [structure-diagnostic-presentation-impact-rules.md](./structure-diagnostic-presentation-impact-rules.md) | Impact ², `R-Pres-*`, mutation inventory — **steps 3–5 implemented** (policy, surfaces, i18n short + mutation copy) |
| [notion-decision-draft.md](./notion-decision-draft.md) | Short **Décision** text for Notion (copy/paste); links to Git annex |
| [implementation-plan-placeholder.md](./implementation-plan-placeholder.md) | Pre-§11B placeholder (historical) |
| [section-11b-signoff-preparation.md](./section-11b-signoff-preparation.md) | **§11B** maintainer pack (signed 2026-10-15) |
| [section-11c-addendum-clarification-arbitration.md](./section-11c-addendum-clarification-arbitration.md) | **§11C signed (Git)** 2026-10-15 — workbook + matrix; **SLICE-0-none** (no ENF code) |
| [section-11c-e5-matrix-diff-proposal.md](./section-11c-e5-matrix-diff-proposal.md) | **E5** — applied matrix diff record (§9–§11) |
| [notion-decision-11c-draft.md](./notion-decision-11c-draft.md) | Notion copy block §11C |
| [implementation-plan-post-11c.md](./implementation-plan-post-11c.md) | Post–§11C sign — slices & stop line (**SLICE-0-none**) |
| [slice-1-g-b-3-enf.md](./slice-1-g-b-3-enf.md) | **Authorized** `SLICE-1-G-B-3-ENF` — `G-B-3-ENF` only (§11C.4) |
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
**§11C (2026-10-15):** [addendum](./section-11c-addendum-clarification-arbitration.md) + [matrix §11](./phase-mutability-matrix.md#11-sign-off-scope-three-layers) — full Git sign; implementation **SLICE-0-none** until new §11C.4 decision.
