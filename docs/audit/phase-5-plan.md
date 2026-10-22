# Phase 5 — Frontend et cohérence produit (launch)

**Roadmap (plan directeur):** composants / hooks / services inutilisés · frontière BE/FE · contrats API et typage · **i18n** · glossaire · **CSS** orphelin et assainissement styles · cohérence Design System.

**Reprise:** `main` @ `d14391dd46170243056408d42df6d22d1627213bd` (post phase 4 D3, merge PR #71).

**Pilotage Notion:** [Phase 5 — Frontend et cohérence produit](https://www.notion.com/p/40075fb92d1081f7ac8fcbb7b4618a7e).

**Go décideur:** 2026-10-22.

**Arbitrage F1–F4 (post wave 1):** [`phase-5-reconciliation-f1-f4.md`](phase-5-reconciliation-f1-f4.md) — F1–F4 clos sans lot correctif (go 2026-10-22).

---

## 1. Scope

### In scope (phase 5)

| Stream | Registre / sujet | Mode |
| --- | --- | --- |
| **Vague 1** | Baseline web post–phase 4 (`npm run build`, `i18n:check`, lint, état scripts audit) | Investigation — **no app code** |
| **F1** | AUD-0001 — scripts scoped (`css:audit` / `i18n:audit` / `tile:check`) | **Clos** (repoint + scoped, 2026-10-22) |
| **F1bis** | AUD-0001 — audits repo-wide (`frontend:audit`, `i18n:audit:repo`) | **Livré** (2026-10-22) — voir `phase-5-lot-f1bis-plan.md` |
| **F2** | AUD-0003 — sélecteur CSS dupliqué `structure-fiche` | **Clos** — corrigé depuis C7 ; pas de lot (go 2026-10-22) |
| **F3** | AUD-0013 — classes orphelines `structure-fiche.css` (heuristique) | **Clos** — corrigé depuis IC13–14 ; pas de lot (go 2026-10-22) |
| **F4** | AUD-0014 — clés feuilles orphelines `structure.json` | **Clos** — corrigé depuis IC13–14 ; pas de lot (go 2026-10-22) |
| **F5** | Frontière BE/FE / `presentedIssueCodes` (suites post–D3-B) | Investigation seulement tant que non arbitré |

### Out of scope (unless new arbitration)

| Item | Reason |
| --- | --- |
| Backend domaine / guard / persistance | Phase 4 close |
| AUD-0008 trajectoire C | Auth/TLS Host |
| AUD-0009, AUD-0010 | Dette acceptée backend |
| Refonte Structure / lifecycle métier | Produit hors audit correctif |
| Chantier global phase 8 | Rejeu final assainissement |
| `frontend-audit.mjs` global recréation | Sauf arbitrage F1 explicite |

---

## 2. Lots (non autorisés tant que non listés sur fiche Notion)

| Lot | Prérequis | Livrable attendu |
| --- | --- | --- |
| **Vague 1** | Go phase 5 | `phase-5-wave-1-frontend-baseline.md` |
| **F1** | Vague 1 + arbitrage AUD-0001 | Scripts npm alignés + docs + éventuel CI |
| **F2** | AUD-0003 @ `d14391dd` — `lint:css` propre | **Aucun lot** — déjà corrigé (C7 `2bd16efc`) |
| **F3** | AUD-0013 @ `d14391dd` — `css:audit` 0 unused | **Aucun lot** — déjà corrigé (`f6ab500f`) |
| **F4** | AUD-0014 @ `d14391dd` — `i18n:audit` 0 suspect | **Aucun lot** — déjà corrigé (`f6ab500f`) |
| **F5** | Arbitrage produit si écart | Doc / tests contrat SPA |

**Preuves phase 1 (référence):** `docs/audit/phase-1-wave-3.md` · arbitrage 7.

---

## 3. Critères de fin phase 5 (pilotage)

- Vague 1 baseline publiée sur `main`.
- Chaque stream F1–F4 : **livré**, **dette acceptée**, ou **reporté** avec registre / Notion.
- `npm run build` (Play’Up web) vert sur SHA de clôture.
- Aucune suppression CSS/i18n sans preuve d’inutilité (vigilance clés dynamiques / classes conditionnelles).
- Pas de duplication de règles métier C# → TypeScript introduite par les lots.

---

## 4. Progress

- [x] Go phase 5 (décideur, 2026-10-22).
- [x] Plan Git (this file).
- [x] Vague 1 — baseline frontend @ `d14391dd` (`phase-5-wave-1-frontend-baseline.md`, 2026-10-22).
- [x] Réconciliation arbitrage F1–F4 (`phase-5-reconciliation-f1-f4.md`, 2026-10-22) — **sans** correctif code.
- [x] **Go décideur F1** — repoint + scoped (2026-10-22).
- [x] F1-doc — sync docs audit Git (`baseline.md`, `phase-1-wave-3.md` renvois).
- [x] **Go F1bis** — `frontend:audit` + `i18n:audit:repo` restaurés (`phase-5-lot-f1bis-plan.md`).
- [x] **Go F2** — AUD-0003 clos « corrigé depuis » (pas de lot CSS) — 2026-10-22.
- [x] **Go F3–F4** — AUD-0013/0014 clos « corrigé depuis » (pas de lot) — 2026-10-22.
- [ ] Registre Notion — AUD-0003, 0013, 0014 → vérifié/clos (alignement décideur).
- [ ] F5 — si déclenché.
- [ ] Clôture phase 5 (pilotage Notion + registre).

---

## 5. Vérification rapide (référence)

```bash
cd src/PlayUp/web
npm run build
npm run i18n:check
npm run lint:css
# Scripts audit (état attendu documenté en vague 1):
npm run css:audit
```
