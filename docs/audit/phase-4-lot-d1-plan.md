# Phase 4 — Lot D1 (AUD-0005-C) — .NET warnings policy

**Decision (2026-10-21):** three-tier policy — qualify baseline, reduce debt deliberately, block growth in CI without global `TreatWarningsAsErrors`.

## Decision table (decision maker)

| Topic | Decision |
| --- | --- |
| SA1143 | Examine rule and occurrences; fix where readability/conformance genuinely improves. |
| Other warnings | Fix confirmed defects; document intentionally tolerated ones. |
| CI | Progressive gate: block **new** warnings vs recorded baseline (no immediate zero-warning mandate). |
| Gate scope | **Compiler and analyzer** diagnostics only (`SA*`, `CA*`, `IDE*`, `RCS*`, …). **NuGet `NU*` warnings are excluded** (version skew between packages, e.g. EF Core vs Npgsql EF on different RC trains). Align package versions in `Directory.Packages.props`; do not use the gate to tolerate permanent `NU1608`. |
| `NoWarn` | No global blanket; only targeted, justified, traceable exceptions. |
| `TreatWarningsAsErrors` | **Not** global at this stage; consider phased activation after baseline qualification. |

## Rationale

Avoid permanent tolerance (83+ warnings drift) and avoid premature global hard-fail on legacy noise unrelated to PR changes.

## Baseline @ qualification (`main`, Release, `--no-incremental`)

| Code | Count | Notes |
| --- | ---: | --- |
| SA1143 | 0 | Cleared D1-B (2026-10-21) — solution Release build |
| SA1137 | 0 | Fixed 2026-10-21 (`DevelopmentTestsArchitectureIsolationTests.cs` — collection expression indentation) |
| **Total** | **0** | See `docs/audit/dotnet-warnings-baseline.json` (was 78 after Host lot) |

**SA1143:** rule enforces consistent parenthesis/line breaks for nested conditional expressions. `dotnet format analyzers` was tried and **rejected** (warning count increased); fixes will be intentional per site.

## Deliverables

- [x] Decision recorded (this file + Notion phase 4 fiche).
- [x] Baseline JSON + CI verification script.
- [x] SA1143 remediation pass (baseline **0** @ D1-B close).
- [x] SA1137 fix (single site).
- [ ] Registre / Notion: AUD-0005-C row or phase-4 note when policy fully operational.

## CI

After `dotnet build` (Release): `.github/scripts/verify-dotnet-warnings-baseline.sh` — fails if **gated** total or per-code counts exceed baseline. The script ignores `NU*` lines in the build log (logged as excluded count only). Totals in `dotnet-warnings-baseline.json` are gated totals, not MSBuild’s full warning count.

## Out of scope (D1)

- Global `TreatWarningsAsErrors`
- New global `NoWarn` for SA1143
- Frontend warnings (separate controls)
