# SLICE-2 — G-B-1-ENF

| Field | Value |
|-------|--------|
| **Status** | **Closed** (2026-10-16) — delivery [`c1a12ec4`](https://github.com/sandre58/MyClub/commit/c1a12ec4) ; governance doc `docs(structure-lifecycle): close SLICE-2-G-B-1-ENF with SPA reserve` (B-Q3a ; réserve SPA) |
| **Slice ID** | `SLICE-2-G-B-1-ENF` |
| **Allowed backlog** | `G-B-1-ENF` only |
| **Contract** | B-Q3a — at most **one** non-cancelled draw per phase |

**Governance:** [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4 · §12 **G-B-1** (remains **Deferred**).

---

## Périmètre

| In scope | Out of scope |
|----------|----------------|
| Refuse `Stage.CreateDraw` when any draw `Status != Cancelled` exists | C-ENF, G-OPS3 (autres slices) |
| Wire `Stage.NonCancelledDrawAlreadyExists` (domain code; Application path) | **SPA-only enforcement** (UX disable / hub parity) |
| Tests domaine + application | Concurrency globale (limitation G-B-3) |

---

## Réserve produit — enforcement SPA (DM 2026-10-16 — décision **A**)

SLICE-2 couvre l’enforcement domaine et Application de la règle B-Q3a : un stage ne peut pas créer un second tirage non annulé. L’enforcement SPA reste partiel et hors périmètre de cette slice. Cette réserve ne bloque pas sa clôture, conformément au périmètre signé et à la décision DM A.

**G-B-1** reste **Deferred** dans §12. La clôture de SLICE-2 ne signifie ni que le parcours SPA est entièrement couvert, ni que l’objectif produit G-B-1 est atteint. Le reliquat SPA nécessitera un arbitrage et un périmètre explicites avant toute implémentation complémentaire.

---

## Delivery proof (`c1a12ec4`)

- Domain : [`Stage.CreateDraw`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) · [`StageErrorCodes.NonCancelledDrawAlreadyExists`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/StageErrorCodes.cs)
- Application : [`CreateDraw.Execute`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/CreateDraw/CreateDraw.cs) (domain failure surfaces on use case path)
- Tests : [`StageDrawTests`](../../../tests/MyClub.PlayUp.Domain.Tests/Stages/StageDrawTests.cs) · [`CreateDrawTests.Create_second_non_cancelled_draw_is_rejected`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/CreateDrawTests.cs)

**Not claimed in this slice:** dedicated Host HTTP integration test; SPA create-button parity (see reserve above).

---

## Closure criteria

**Closed** when:

1. Diff contains **G-B-1-ENF / B-Q3a** scope only. — **Done** (`c1a12ec4`).
2. Release tests for domain + Application refuse path green. — **Done** (tests above).
3. Wire code `Stage.NonCancelledDrawAlreadyExists` defined and exercised via Application. — **Done**.
4. SPA reliquat explicitly recorded; §12 **G-B-1** stays **Deferred**. — **Done** (this closure + matrix §12 note).
5. §11C.4 + index sync (governance doc commit). — **Done** (DM décision **A** 2026-10-16).

**§12:** **G-B-1** remains **Deferred** — slice **Closed** ≠ backlog **Done**.

---

## Sequencing

**SLICE-2** **Closed**. **SLICE-3** **Closed** (G-OPS3 partial reserve). **C-ENF** trilogy **Closed** — do not reopen without anomaly.
