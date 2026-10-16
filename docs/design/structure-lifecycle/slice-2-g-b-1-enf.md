# SLICE-2 — G-B-1-ENF

| Field | Value |
|-------|--------|
| **Status** | **Authorized** (DM arbitration 2026-10-16) — **not Closed** until Release tests + dedicated commit + review |
| **Slice ID** | `SLICE-2-G-B-1-ENF` |
| **Allowed backlog** | `G-B-1-ENF` only |
| **Contract** | B-Q3a — at most **one** non-cancelled draw per phase |

## Périmètre

| In scope | Out of scope |
|----------|----------------|
| Refuse `Stage.CreateDraw` when any draw `Status != Cancelled` exists | C-ENF, G-OPS3 (autres slices) |
| Wire `Stage.NonCancelledDrawAlreadyExists` | SPA-only enforcement |
| Tests domaine + application | Concurrency globale (limitation G-B-3) |

## Preuve (working tree — non commité)

- Domaine : [`Stage.CreateDraw`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs)
- Tests : [`StageDrawTests`](../../../tests/MyClub.PlayUp.Domain.Tests/Stages/StageDrawTests.cs), [`CreateDrawTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/CreateDrawTests.cs)

## Enregistrement

[programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4
