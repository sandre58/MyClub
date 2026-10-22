# Phase 8 — Verrouillage et clôture chantier (cadrage)

**Status:** **vague 1 exécutée** (pilotage) — 2026-10-22 · correctifs gate = go lot **P8-W1** séparé  
**Roadmap (plan directeur):** contrôles finaux · rejeu assainissement · cohérence règles Cursor vs décisions SoT · non-régression · garde-fous CI · documentation · bilan dettes · **clôture tâche Notion**.

**Reprise technique:** `main` @ `e6b3b63b` (cadrage phase 8 ; post clôture phase 7 @ `d0548ad2`).  
**Bilan dettes entrée:** [`phase-7-debts-consolidated.md`](phase-7-debts-consolidated.md).  
**Phase 7 close:** [`phase-7-closure.md`](phase-7-closure.md).  
**Baseline historique (phase 0):** [`baseline.md`](baseline.md) — mesures @ `93b09f16` (référence, pas SHA cible).  
**Pilotage Notion:** [Phase 8 — Verrouillage et clôture (cadrage)](https://www.notion.com/p/40175fb92d10819d9f19ff1952bff0e6) · parent [chantier audit](https://www.notion.com/p/3fd75fb92d1081b2b076efd33da8d03f).

---

## 0. Séparation cadrage / exécution

| Étape | Autorisé maintenant | Interdit sans go explicite |
| --- | --- | --- |
| **Cadrage** (ce document + Notion) | Plan, priorisation, critères de fin chantier | Lots correctifs code, nouvelles gates CI |
| **Vague 1** (post go phase 8) | Rejeu contrôles, inventaire écarts vs baseline / registre | Correctifs métier ou refactors opportunistes |
| **Lots P8-x** | — | Un lot = périmètre + preuves + critères d’arrêt **validés** |

**Objectif phase 8:** vérifier que le chantier **« MyClub — Audit technique global et assainissement »** peut être **clôturé** au sens du plan directeur (critères § ci-dessous), pas « tout résoudre » (dettes acceptées restent valides si documentées).

---

## 1. Périmètre indicatif (streams)

### In scope (candidats — **pas** engagés)

| Stream | Intention | Entrées |
| --- | --- | --- |
| **P8-Ctrl** | Rejeu des **contrôles retenus** (CI + commandes locales documentées) vs état actuel | `baseline.md`, `phase-6-wave-1-static-baseline.md`, workflows CI |
| **P8-Reg** | Relecture **registre** AUD-0001… : statut (Vérifié / Dette acceptée / Reporté) aligné Git + Notion | [Registre findings](https://www.notion.com/p/ec992f0e07134ac2a11a837e5539e45c), closures phases 3–7 |
| **P8-Path** | Matrice **parcours critiques** — écarts documentés vs couverture actuelle | [`critical-paths.md`](critical-paths.md) |
| **P8-Cursor** | Cohérence **`.cursor/rules/`** + `AGENTS.md` vs décisions arbitrées (phase 2, lot C8) | [`cursor-rules-inventory.md`](cursor-rules-inventory.md), arbitrages Notion |
| **P8-Doc** | README / `docs/**` contributeur : statut Play’Up, liens Notion ↔ Git, plus de « préparation phase N » obsolète | Hub audit `docs/audit/` |
| **P8-Close** | Document **clôture chantier** + proposition statut Notion tâche parente **Terminé** | Critères plan directeur (page chantier) |

### Out of scope (sauf nouvel arbitrage)

| Exclusion | Raison |
| --- | --- |
| **AUD-0008** auth / TLS / S6 (hébergement Internet) | Dette acceptée — lot **produit/infra**, pas condition de clôture audit assainissement |
| **AUD-0012** optimisations EF supplémentaires | Dette acceptée — lots futurs **mesure + PerformanceBaseline** uniquement |
| **F5**, **AUD-0004** réserves métier | Report produit — hors clôture technique |
| **S4-INV**, i18n mass-delete, découpage TSX, refactor CSS DRY | Dettes phase 5–6 — pas rejeu correctif P8 par défaut |
| Nouvelles gates heuristiques (`frontend:audit`, etc.) | Politique S1 phase 6 inchangée |

---

## 2. Critères de clôture du chantier (rappel plan directeur)

Checklist **à remplir en vague 1** (preuves Git / CI, pas supposition) :

| Critère | Mode de vérification P8 |
| --- | --- |
| Builds backend + frontend OK | CI `build` + `web` sur SHA clôture |
| Tests obligatoires OK | CI `dotnet test`, Vitest, E2E stub + Host selon CI |
| P0/P1 sans statut explicite | Revue registre + `phase-7-debts-consolidated.md` |
| Zéro warning sur contrôles **retenus** | .NET baseline CI ; oxlint/stylelint/build web — comparer phase 6 S1 |
| Parcours critiques | `critical-paths.md` + gaps listés (UI draw/materialize = signal accepté ou lot) |
| Garde-fous CI actifs | Host isolation publish, warnings baseline, arch tests |
| Dette résiduelle documentée | Registre + synthèse `phase-8-closure.md` (à produire) |
| Règles Cursor | Inventaire à jour ; pas de conflit **connu** avec SoT Notion / `architecture.mdc` |

**Non exigé pour clôture:** résolution de toutes les dettes acceptées ; preuve d’absence totale d’usage dynamique (code mort / i18n / CSS).

---

## 3. Priorisation suggérée (à arbitrer — ordre vague 1)

| Ordre | Stream | Risque si ignoré | Effort estimé |
| --- | --- | --- | --- |
| 1 | **P8-Ctrl** | Fausse clôture si CI rouge | 1 session mesure |
| 2 | **P8-Reg** | Dettes orphelines / statuts contradictoires | 1 session revue |
| 3 | **P8-Path** | Régression non signalée sur chemins 2–3 | Lecture + mise à jour matrice |
| 4 | **P8-Cursor** | Dérive assistée IA post-audit | Revue ciblée (C8 + phase 2) |
| 5 | **P8-Doc** | Onboarding / pilotage faux | Diff docs audit |
| 6 | **P8-Close** | Tâche Notion bloquée en « En cours » | Doc + décideur |

**Décision attendue:** valider cet ordre ou ouvrir **un seul** lot correctif P8 (ex. doc uniquement) avant vague 1.

---

## 4. Livrables Git prévus

| Livrable | Chemin | État |
| --- | --- | --- |
| Plan cadrage | `phase-8-plan.md` (this file) | ✅ |
| Vague 1 rejeu | `phase-8-wave-1-global-replay-baseline.md` | ✅ @ `422aba33` |
| Revue registre | `phase-8-register-reconciliation.md` | ✅ (wave 1) |
| Revue Cursor | `phase-8-cursor-rules-reconciliation.md` | ✅ @ `8d4a8dfa` + micro-align `playup-web-quality.mdc` |
| Clôture chantier | `phase-8-closure.md` | ⬜ fin phase 8 |

---

## 5. Critères de fin phase 8 (pilotage)

- Vague 1 publiée : état contrôles + écarts vs phase 0/6 **documentés**.
- Registre : chaque finding encore pertinent a un statut cohérent (ou ligne archivée avec preuve).
- `critical-paths.md` à jour ou écarts **explicitement acceptés** en clôture.
- Règles Cursor : réconciliation documentée (écarts = tâche Notion ou dette, pas silence).
- `phase-8-closure.md` + mise à jour tâche Notion **Terminé** (si décideur valide).
- CI verte sur SHA de clôture phase 8.

---

## 6. Prochaines étapes

1. **Décideur:** go **lot P8-W1** (CA1859 + Prettier e2e) — voir [`phase-8-wave-1-global-replay-baseline.md`](phase-8-wave-1-global-replay-baseline.md) §6.
2. **Clôture phase 8** — `phase-8-closure.md` + Notion parent **Terminé** (go décideur).
3. Arbitrer si un **lot correctif** est requis avant clôture (ex. doc README, ligne registre) ou si dettes acceptées suffisent.
4. Clôturer tâche [MyClub — Audit technique global et assainissement](https://www.notion.com/p/3fd75fb92d1081b2b076efd33da8d03f) après `phase-8-closure.md`.
