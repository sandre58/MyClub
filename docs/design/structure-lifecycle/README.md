# Structure phase lifecycle — design artefacts

Product and technical references for **stage lifecycle** (Draft / Ready / Running), **mutability** (structure vs operational vs production), and **diagnostic policy** (Prepare, Materialize, Start, drawer).

## Documents

| Document | Purpose |
|----------|---------|
| [phase-mutability-matrix.md](./phase-mutability-matrix.md) | Exhaustive inventory (diagnostics + `UseCaseExecutor.Stages` commands), current vs target policy, reference cases, open arbitrations |
| [notion-decision-draft.md](./notion-decision-draft.md) | Short **Décision** text for Notion (copy/paste); links to Git annex |
| [implementation-plan-placeholder.md](./implementation-plan-placeholder.md) | Post-sign-off implementation slices — **do not execute** until matrix + arbitrations are closed |
| [g-mat1-confrontation-undecidable-at-materialize.md](./g-mat1-confrontation-undecidable-at-materialize.md) | **G-MAT1** proposed materialize contract (`ConfrontationUndecidable`) — litigious, not §11B |

## Related Git docs

- [Structure phase fiche truth sources](../structure-phase-refonte/truth-sources.md)
- [Actions & permissions inventory](../structure-phase-refonte/actions-permissions-inventory.md)

## Status

**Signed policy (2026-10-15):** §11A in [phase-mutability-matrix.md](./phase-mutability-matrix.md); arbitrations #1–#3 closed §5.2–§8; G-UI1/G-UI2 shipped.  
**Next:** implement **G-MAT1** (Cup gate + P+/N1–N3 tests per [spec](./g-mat1-confrontation-undecidable-at-materialize.md)); G-MAT2–4 still litigious before broader **§11B**.
