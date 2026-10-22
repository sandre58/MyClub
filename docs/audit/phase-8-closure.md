# Phase 8 — Verrouillage et clôture chantier (closure)

**Status:** closed (pilotage) — 2026-10-22  
**Go phase 8 cadrage:** 2026-10-22  
**Go vague 1:** 2026-10-22  
**Go P8-W1 / P8-Cursor:** 2026-10-22  
**Go clôture chantier:** 2026-10-22  

| Field | Value |
| --- | --- |
| **Plan** | [`phase-8-plan.md`](phase-8-plan.md) |
| **Closure SHA** | `main` @ `36c9f5b2` (doc clôture ; correctifs gate `8d4a8dfa`) |
| **Notion phase 8** | [Phase 8 — Verrouillage et clôture](https://www.notion.com/p/40175fb92d10819d9f19ff1952bff0e6) |
| **Notion chantier** | [MyClub — Audit technique global et assainissement](https://www.notion.com/p/3fd75fb92d1081b2b076efd33da8d03f) → **Terminé** (pilotage) |

---

## Phase 8 streams — bilan

| Stream | Résultat | Livrable |
| --- | --- | --- |
| **Cadrage** | Périmètre P8 + exclusions | [`phase-8-plan.md`](phase-8-plan.md) |
| **P8-Ctrl** | Rejeu contrôles ; écarts CA1859 + Prettier identifiés | [`phase-8-wave-1-global-replay-baseline.md`](phase-8-wave-1-global-replay-baseline.md) |
| **P8-Reg** | Registre AUD-0001…0017 réconcilié (Git) | [`phase-8-register-reconciliation.md`](phase-8-register-reconciliation.md) |
| **P8-Path** | Gaps UI draw/materialize **acceptés** (documentés) | [`critical-paths.md`](critical-paths.md) |
| **P8-Cursor** | Aucun conflit connu rules ↔ SoT | [`phase-8-cursor-rules-reconciliation.md`](phase-8-cursor-rules-reconciliation.md) |
| **P8-W1** | Gate CI rétabli | `StageRepository` CA1859 + Prettier e2e @ `8d4a8dfa` |
| **P8-Doc** | Hub audit à jour via closures phase 8 (pas de refonte README) | `docs/audit/phase-8-*.md` |

---

## Critères clôture plan directeur — statut @ `36c9f5b2`

| Critère | Statut |
| --- | --- |
| Builds backend + frontend | ✅ local Release build **0** warnings ; web `build` OK post P8-W1 |
| Tests obligatoires | ✅ `dotnet test` + Vitest rejeu vague 1 / P8-W1 |
| P0/P1 arbitrés | ✅ [`phase-8-register-reconciliation.md`](phase-8-register-reconciliation.md) |
| Zéro warning contrôles retenus | ✅ post **P8-W1** (.NET baseline + web format/lint) |
| Parcours critiques | ✅ matrice à jour ; gaps UI **signal accepté** |
| Garde-fous CI | ✅ publish isolation, warnings script, arch tests (inchangés) |
| Dette résiduelle documentée | ✅ [`phase-7-debts-consolidated.md`](phase-7-debts-consolidated.md) + registre Notion |
| Règles Cursor | ✅ inventaire + réconciliation P8 |
| CI distante sur SHA clôture | ⚠️ **À confirmer** sur GitHub Actions (`36c9f5b2`) |

---

## Chantier global — synthèse (phases 0 → 8)

| Phase | Intention | Clôture Git / Notion |
| --- | --- | --- |
| **0** | Baseline, inventaire | [`baseline.md`](baseline.md) |
| **1** | Cartographie, registre | Synthèse phase 1 Notion |
| **2** | Arbitrages 1–7 | Phase 2 Notion |
| **3** | Correctifs C1–C8 | [`phase-3-closure.md`](phase-3-closure.md) @ `bbb76630` |
| **4** | Backend D1–D3 | [`phase-4-plan.md`](phase-4-plan.md) (streams livrés) |
| **5** | Frontend | [`phase-5-closure.md`](phase-5-closure.md) |
| **6** | Qualité statique | [`phase-6-closure.md`](phase-6-closure.md) |
| **7** | Sécu / perf / ops | [`phase-7-closure.md`](phase-7-closure.md) |
| **8** | Rejeu + verrouillage | **this file** |

**Résultat:** chantier d’assainissement technique **clôturé au pilotage** — correctifs livrés par lots validés ; dettes **explicitement acceptées** ou reportées produit (pas de promesse de zéro dette).

---

## Dettes & suites hors chantier (maintenues)

| Sujet | Suivi |
| --- | --- |
| **AUD-0008** auth / TLS / S6 | Arbitrage produit / infra avant Internet |
| **AUD-0012** EF Include / hydration | Dette + mesures ; lots futurs sur preuve |
| **AUD-0009**, **0010** | Dette acceptée |
| **F5**, **AUD-0004** | Report produit |
| **S4-INV**, i18n/CSS heuristique, gros TSX | Dette phase 5–6 |
| **E2E UI** draw / materialize | Signal QA optionnel |
| **Media / DevRunner** rules dédiées | Dette gouvernance Cursor C8 |

Nouveau travail = **tâches produit** ou **findings** avec go explicite — pas continuation implicite du chantier audit.

---

## Commits phase 8 (référence)

| Lot | SHA |
| --- | --- |
| Cadrage | `e6b3b63b` |
| Vague 1 docs | `51c7cb77` |
| P8-W1 | `8d4a8dfa` |
| P8-Cursor | `36c9f5b2` |
