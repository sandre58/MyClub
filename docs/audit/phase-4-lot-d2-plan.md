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

## Next (only if arbitrated)

**D2-B** — targeted optimization with before/after SQL counts on the same scenarios; one change per PR.
