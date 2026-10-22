# Phase 5 — Réconciliation F1–F4 (post wave 1)

**Mode:** arbitrage documentaire — **pas** de correctif applicatif ni de lot F1–F4 implémenté.

**Références mesurées:** `main` @ `d14391dd46170243056408d42df6d22d1627213bd` (`phase-5-wave-1-frontend-baseline.md`).

**Constats historiques:** phase 1 vague 3 @ `93b09f16…` (`phase-1-wave-3.md`) — branche `feature/new_version`, **antérieure** aux lots C7 / IC13–14 mergés sur `main`.

---

## 1. Décision F1 — AUD-0001 (**validée 2026-10-22**)

**Décision décideur:** clôturer **« repoint + scoped »** — **ne pas** restaurer `frontend-audit.mjs` global sans besoin distinct démontré.

**F1-doc (2026-10-22):** `baseline.md` (section politique AUD-0001), renvois `phase-1-wave-3.md` §1.

| Condition | État @ `d14391dd` | Preuve |
| --- | --- | --- |
| Scripts npm exécutables | ✅ | `css:audit`, `i18n:audit`, `tile:check` → fichiers `scripts/*.mjs` existants ; wave 1 exit 0 |
| Couverture actuelle | Structure fiche CSS + `structure.json` (en) + tuiles B | Scripts `audit-structure-*`, `tile-family-check.mjs` |
| Audit global absent | ✅ documenté | Pas de `frontend-audit.mjs` / `i18n-key-audit.mjs` dans l’arbre |
| Besoin global non démontré | — | Phase 2 / C7 : « structure-scoped audits only » (`phase-3-lot-c7-plan.md` §2) |
| Docs contributeur web | ✅ alignés | Pas de référence `frontend-audit` dans `src/PlayUp/web/docs/` @ `d14391dd` |
| **Docs audit Git** | ✅ synchronisés (F1-doc) | Politique courante + bannière historique wave 3 |

**F1-doc:** documentation audit Git synchronisée — **pas** de changement SPA/CSS/i18n.

**F1bis (go 2026-10-22):** `frontend-audit.mjs` et `i18n-key-audit.mjs` restaurés — `npm run frontend:audit`, `npm run i18n:audit:repo` (voir `phase-5-lot-f1bis-plan.md`). Scoped scripts inchangés.

---

## 2. Matrice F2–F4 (findings non reproduits)

| ID | Finding historique (wave 3) | Fichier / comportement | Protocole historique | Résultat @ `d14391dd` | Statut proposé | Preuve de décision |
| --- | --- | --- | --- | --- | --- | --- |
| **AUD-0003** | Duplicate selector `.structure-fiche__header-lifecycle-toolbar` → `lint:css` fail | `structure-fiche.css` ~L124 vs ~L238 | `npm run lint:css` | **Pass** (0 violations) | **Clos (go F2, 2026-10-22)** — corrigé depuis C7 | Lot **C7** `2bd16efc` ; pas de lot phase 5 |
| **AUD-0013** | 4 classes orphelines structure-fiche (4/122 tokens) | `structure-fiche.css` | `npm run css:audit` (heuristique grep `src/`) | **0 unused / 118** tokens | **Clos (go F3, 2026-10-22)** | Commit `f6ab500f` ; pas de lot phase 5 |
| **AUD-0014** | 6 clés feuilles suspectes (6/989) | `locales/en/structure.json` | `npm run i18n:audit` | **0 suspect / 983** leaves | **Clos (go F4, 2026-10-22)** | Même commit `f6ab500f` ; pas de lot phase 5 |
| **F5** (optionnel) | Présentation BE/FE post D3-B | `presentedIssueCodes` / attention | Recette C2 + protocole impasse | D3-B livré PR #71 | **Hors F2–F4** | `phase-4-lot-d3-plan.md` — pas de finding wave 3 ouvert |

**Lecture:** la non-reproduction sur `d14391dd` s’explique principalement par **correctifs déjà mergés** (C7, IC13–14), pas par un protocole incompatible seul. Les heuristiques wave 1 utilisent les **mêmes scripts** que wave 3 ; le dénominateur (118 vs 122 classes, 983 vs 989 clés) reflète les suppressions `f6ab500f`.

**Lots F2–F4 correctifs:** **non retenus** — F2–F4 clos par go décideur (2026-10-22). Registre Notion à aligner (AUD-0003, 0013, 0014).

---

## 3. Build — alerte chunk (hors F2–F4)

| Signal | Wave 1 | Lot correctif ? |
| --- | --- | --- |
| Vite chunk `index-*.js` > 500 kB (warning) | Présent, build **réussi** | **Non** par défaut — suivi perf / phase 7 si arbitrage ; pas de finding registre AUD-xxxx ouvert ici |

---

## 4. Prochaines étapes (gouvernance)

| # | Action | Go requis |
| --- | --- | --- |
| 1 | Décision **F1** (repoint + scoped) | ✅ **Go 2026-10-22** |
| 2 | Lot **F1-doc** (baseline + wave 3) | ✅ **2026-10-22** |
| 3 | **F2** — AUD-0003 clos sans lot | ✅ **Go 2026-10-22** |
| 4 | **F3–F4** — AUD-0013/0014 clos sans lot | ✅ **Go 2026-10-22** |
| 5 | Registre Notion AUD-0003, 0013, 0014 | À aligner (décideur / registre) |

**Interdit sans go:** implémentation frontend, restauration `frontend-audit.mjs`, lots F2–F4 « nettoyage » sur findings déjà corrigés.

---

## 5. Liens

- Wave 1 : `phase-5-wave-1-frontend-baseline.md`
- C7 : `phase-3-lot-c7-plan.md`
- IC13–14 : `phase-3-lot-ic13-14-plan.md`
- Plan phase 5 : `phase-5-plan.md`
