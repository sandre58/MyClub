# Phase 8 — Register reconciliation (AUD-0001 … AUD-0017)

**Date:** 2026-10-22  
**Mode:** read-only reconciliation — Git closures + [`phase-7-debts-consolidated.md`](phase-7-debts-consolidated.md).  
**Notion registre:** [Registre des findings — Audit technique MyClub](https://www.notion.com/p/ec992f0e07134ac2a11a837e5539e45c) — alignement manuel optionnel pour CA1859 (signal gate, pas finding registre).

---

## Summary

| Bucket | Count | IDs |
| --- | ---: | --- |
| **Vérifié** (correctif ou IC clos) | 14 | AUD-0001–0008 (partiel 0008), 0011, 0013–0017 — voir phase 3 closure |
| **Dette acceptée** | 3+ | AUD-0009, 0010, 0012 |
| **Reporté produit** | 2 | AUD-0004, F5 (`presentedIssueCodes`) |
| **Dette / signal phase 5–6** | plusieurs | S4-INV, i18n heuristique, CSS DRY, S6, chunk Vite (atténué P7) |

**Nouveau signal technique (hors registre):** **CA1859** après lot AUD-0012 — menace gate `verify-dotnet-warnings-baseline.sh` — traiter en lot **P8-W1-A** ([wave 1](phase-8-wave-1-global-replay-baseline.md) §6).

---

## Per-finding status (pertinent au chantier)

| Réf | Statut correction (pilotage) | Preuve / suivi |
| --- | --- | --- |
| AUD-0001 | Vérifié | Phase 3 C7 |
| AUD-0002 | Vérifié | Phase 3 C2 |
| AUD-0003 | Vérifié | Phase 3 C7 |
| AUD-0004 | Reporté produit | Phase 3–4 D3 ; checklist A3 / assign |
| AUD-0005 | Vérifié (politique) + gate | Phase 4 D1/D1-B — **gate 0 warnings** à rétablir (CA1859) |
| AUD-0006 | Vérifié | Phase 3 C7 |
| AUD-0007 | Vérifié | Phase 3 C7 |
| AUD-0008 | Partiel + dette | C1 phase 3 ; C+C2 phase 7 ; auth/TLS/S6 reportés |
| AUD-0009 | Dette acceptée | Phase 3 |
| AUD-0010 | Dette acceptée | Phase 3 |
| AUD-0011 | Vérifié | Phase 3 C8 (Cursor governance) |
| AUD-0012 | Dette acceptée (+ mitigations) | Phase 4 D2 + P7 perf lots |
| AUD-0013 | Vérifié | Phase 3 IC |
| AUD-0014 | Vérifié | Phase 3 IC |
| AUD-0015 | Vérifié (hybride) | Phase 3 C5 + P7 Host E2E slices |
| AUD-0016 | Dette acceptée (dev advisories) | Phase 3 C6 |
| AUD-0017 | Vérifié | Phase 3 C6 (CI npm) |

---

## Explicit non-findings (do not reopen as AUD-0018 without arbitration)

| Sujet | Traitement |
| --- | --- |
| E2E UI draw / materialize | Signal couverture — phase 7 closure |
| Heuristiques `frontend:audit` / i18n repo | Report-only S1 |
| Auth/TLS Internet | AUD-0008 dette — produit/infra |

---

## Reconciliation outcome

**Cohérent** pour clôture chantier **sous réserve** de rétablir les **contrôles retenus** (warnings .NET + Prettier) documentés en wave 1.
