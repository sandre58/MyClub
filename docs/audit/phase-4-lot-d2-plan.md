# Phase 4 — Lot D2 (AUD-0012) — StageRepository / EF Include

**Mode:** investigation and measurement **before** any Include-graph refactor.

## Registre context

AUD-0012 is **dette acceptée** (phase 3 closure): deep `Include` / `AsSplitQuery` on `StageRepository` is legitimate for aggregate hydration; **volume/latency risk** on hot paths must be **measured**, not assumed.

## Objectives

| Goal | Out of scope (D2) |
| --- | --- |
| Catalog repository entry points and load shapes | Changing `ApplyProfile` / `Include` without data |
| Reproducible SQL command baselines (Postgres + Testcontainers) | Production APM / load testing |
| Recommendations ranked by evidence | D3 product arbitration |
| | D1 warnings (closed) |

## Deliverables

- [x] Investigation write-up: `phase-4-d2-stage-repository-investigation.md`
- [x] Isolated baselines: `StageRepositoryLoadBaselineTests` (`Category=PerformanceBaseline`)
- [x] Reuse existing read baselines: `ReadPerformanceBaselineTests`
- [ ] Decision: accept debt, schedule D2-B optimization lot, or new registre row — **after** review of captured numbers

## How to run

```bash
dotnet test tests/MyClub.PlayUp.Development.Tests/MyClub.PlayUp.Development.Tests.csproj \
  -c Release \
  --filter "Category=PerformanceBaseline" \
  --logger "console;verbosity=detailed"
```

Requires Docker (Testcontainers PostgreSQL 18). Copy SQL counts from test output into the investigation doc when refreshing baselines.

## Pilotage (decision maker, 2026-10-22)

| Stream | Statut phase 4 | Registre AUD-0012 |
| --- | --- | --- |
| **D2 investigation** | **Livré** (`0c0657c0` on `feature/audit-d2-stage-repository`) | Finding **not** closed — remains **dette acceptée** until optimization is arbitrated and closure criteria met |
| **D2-B** | Lot 1 livré (batch attachments — `phase-4-lot-d2-b-attachment-batch.md`) | Finding still **dette acceptée** |

**D1** (warnings / AUD-0005-C policy) is **closed on `main`** (baseline 0, CI gate). **D3** (AUD-0004) stays **pending**.

Gap **Summary → Full** (e.g. cup-qf-sf: 8 → 17 SQL commands in Testcontainers) signals **cost to investigate**, not proof that `Full` is wrong for command paths.

## D2-B lot 1 (delivered)

See `phase-4-lot-d2-b-attachment-batch.md` — single batched `FixtureAttachmentRef` load per stage hydration.

## D2-B preparation (further lots)

| Prerequisite | Content |
| --- | --- |
| Scenarios | Same seeds as D2 (`cup-qf-sf`, `groups:running`, `champions-league:running`) + isolated `StageRepository` profiles |
| Before/after | One hypothesis per PR; re-run full PerformanceBaseline filter |
| Acceptance criteria (define per hypothesis) | SQL command count, optional wall-clock / allocations, hydration-specific cost when relevant — not SQL count alone |
| Out of scope until arbitrated | `ApplyProfile` / `Include` changes, profile narrowing on command paths without product sign-off |

## Next (only if arbitrated)

**D2-B** — targeted optimization with before/after measurements; one change per PR.
