# Phase 4 — Architecture backend & domaine (launch)

**Roadmap (plan directeur):** corrections architecturales / métier, persistance, SQL, transactions, concurrence, erreurs — optimisations **mesurées** uniquement.

**Reprise:** `main` @ `dbea98d46170243056408d42df6d22d1627213bd` (post phase 3 closure doc).

**Pilotage Notion:** fiche *Phase 4 — Architecture backend & domaine (en cours)*.

---

## 1. Scope

### In scope (phase 4)

| Stream | Registre / sujet | Mode |
| --- | --- | --- |
| **Vague 1** | Baseline .NET post-merge (warnings, tests, NU1608) | Investigation — **no app code** |
| **D1** | AUD-0005-C — politique warnings .NET (arbitrage 4 reporté) | Arbitrage **puis** lot correctif éventuel |
| **D2** | AUD-0012 — `StageRepository` / graphes EF Include | Investigation mesure **avant** toute optimisation |
| **D3** | AUD-0004 — réserve produit (A3 checklist vs backend ; cas assign) | **Arbitrage produit** avant changement domaine/tests |

### Out of scope (unless new arbitration)

| Item | Reason |
| --- | --- |
| AUD-0009 | Dette acceptée — tests archi étendus optionnels |
| AUD-0010 | Dette acceptée — pas de découpage `UseCaseExecutor.Stages` sur taille seule |
| AUD-0008 trajectoire C | Auth/TLS — hors phase 4 |
| Frontend / CSS / i18n | Roadmap **phase 5** |
| Chantier global phase 8 | Rejeu baseline & clôture chantier |

---

## 2. Lots (non autorisés tant que non listés sur fiche Notion)

| Lot | Prérequis | Livrable attendu |
| --- | --- | --- |
| **Vague 1** | Go phase 4 | `phase-4-wave-1-backend-baseline.md` |
| **D1** | Arbitrage AUD-0005-C | Politique documentée + changements CPM/CI/csproj si validés |
| **D2** | Protocole mesure validé | Trace / scénario repro + recommandation (pas de refactor aveugle) |
| **D3** | Décision produit Cup C2 | Alignement checklist / read-model / tests protocole |

---

## 3. Critères de fin phase 4 (pilotage)

- Vague 1 baseline publiée sur `main`.
- Chaque stream D1–D3 : **arbitré**, **livré**, ou **explicitement reporté** avec référence registre / Notion.
- Aucun P1 backend ouvert sans statut registre (Vérifié / Dette acceptée / arbitrage).
- CI `build` + tests obligatoires verts sur SHA de clôture phase 4.

---

## 4. Progress

- [x] Lancement phase 4 + plan Git (this file).
- [x] Vague 1 baseline (`phase-4-wave-1-backend-baseline.md`).
- [x] D1 — arbitrage AUD-0005-C (decision 2026-10-21, `phase-4-lot-d1-plan.md`).
- [x] D1 — garde-fou CI + baseline JSON (`verify-dotnet-warnings-baseline.sh`).
- [x] D1 — SA1137 corrigé ; baseline **82** warnings (SA1143 seul).
- [x] D1-B — SA1143 solution-wide ; baseline **0** (garde-fou CI inchangé).
- [x] D2 — investigation + D2-B lot 1 mergés @ `9faf3f16` (AUD-0012 **dette acceptée** maintenue).
- [ ] D2 — clôture registre AUD-0012 (optionnel : accepter mitigation documentée).
- [x] D3 — lancement arbitrage (`phase-4-lot-d3-plan.md`).
- [x] D3 — arbitrage validé Q1–Q2 + Q3-A ([contrat Notion](https://www.notion.com/p/40075fb92d1081979ed7c15846a62c0b)).
- [x] D3 — preuve backend A3/A5 (`CupC2Impasse`).
- [ ] D3 — rejouage SPA + **go D3-B** + lot correctif écarts.
