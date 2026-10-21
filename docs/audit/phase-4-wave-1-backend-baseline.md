# Phase 4 — Wave 1 — Backend baseline (@ `dbea98d4`)

**Date:** 2026-10-21 · **Branch:** `main` · **Purpose:** reference state before phase 4 corrective / arbitration work.

## Commands

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
```

## Build (`dotnet build -c Release`)

| Metric | Value |
| --- | --- |
| Errors | 0 |
| Warnings | **83** |
| Dominant rule | **SA1143** (conditional expression formatting — StyleCop) across Domain, Application, Infrastructure, Host, Media |

**NU1608:** not observed on restore/build @ this SHA (C4 delivered).

**Note:** Warnings are **not** treated as errors in CI today (`build/code-analysis.props` — no global `TreatWarningsAsErrors`). This gap is the subject of **AUD-0005-C** (policy), not a silent defect.

## Tests

Run full `dotnet test -c Release` before closing wave 1 on the integration machine; record pass/fail counts here when validated.

| Suite | Result @ wave 1 |
| --- | --- |
| Solution (`dotnet test -c Release`) | **2198** passed, **0** failed (2026-10-21, local `main`) |

## Open register links (phase 4)

| Réf. | Statut | Phase 4 stream |
| --- | --- | --- |
| AUD-0005 | Vérifié (NU1608) ; **-C open** | D1 |
| AUD-0012 | Dette acceptée | D2 |
| AUD-0004 | Vérifié (C3) ; **réserve produit** | D3 |
| AUD-0009, 0010 | Dette acceptée | Hors scope |

## Next

1. **AUD-0005-C** arbitrated — see `phase-4-lot-d1-plan.md` + `dotnet-warnings-baseline.json` + CI script.
2. D2 — see `phase-4-d2-stage-repository-investigation.md` + `Category=PerformanceBaseline` tests.
3. D3 — product call on Cup C2 A3 / cas assign (see `phase-3-lot-c3-plan.md` reserves).
