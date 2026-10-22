# Phase 6 — S3 — i18n repo (qualification)

**Mode:** audit only — **aucune** suppression ; `_one` / `_other` **exclus** de tout nettoyage automatique.  
**SHA:** post S2-001 (`main` après commit courant).  
**Outils:** `npm run i18n:audit:repo` · `npm run i18n:audit:qualify` (buckets) · `npm run i18n:check` (parité fr/en, gate sémantique distincte).

## Synthèse

| Métrique | Valeur |
| --- | ---: |
| Lignes « suspicious » (fr **+** en) | 686 |
| Clés logiques uniques (`ns:path`, toute locale) | **343** |
| `i18n:check` (parity) | **16/16** vert @ wave 1 |

Les **686** entrées ne sont **pas** 686 défauts : doublon fr/en + heuristique textuelle naïve.

---

## Buckets (heuristique `i18n-key-audit-qualify.mjs`)

| Bucket | Count (fr+en rows) | Qualification | Suppression auto |
| --- | ---: | --- | --- |
| **plural_suffix** | 404 | **Faux positif** — formes i18next (`_one`, `_other`, …) ; usage via `t('band.matches', { count })` sans littéral `matches_one` dans le TS. Preuve : `MatchHubPage.tsx` → `t('band.matches', { count })`. | **Non** |
| **namespace_root_only** | 272 | **Faux positif probable** — clés référencées par chemin relatif dans le namespace (`useTranslation('structure')` + `t('readiness.actionsAria')`) ; l’audit ne voit pas le préfixe complet. Ex. `structure:readiness.*`, `regulation:tokens.*` (tooltips `t('tokens.standingWinTip')`). | **Non** |
| **investigate** | 10 (5 clés × 2 locales) | **À investiguer** — aucune occurrence textuelle repérée ; lot manuel si confirmé mort. | **Non** sans preuve |

Aucune clé classée **confirmé inutilisé** en S3 (preuve suppression insuffisante).

---

## S3-INV — regulation (échantillon « investigate »)

| Clé | Observation rapide |
| --- | --- |
| `regulation:personalized` | `PersonalizedPhaseHint` utilise `binding.specificToPhase`, pas `personalized`. |
| `regulation:loadError` | Pas de `t('loadError')` dans le module regulation (erreurs ailleurs / clés shell). |
| `regulation:phaseStructureAria` | Aucun `t('phaseStructureAria')` trouvé. |
| `regulation:schematic.swissRound` | `phaseSchematic.tsx` lit `schematic.swissRoundCount` (domaine), pas cette clé i18n. |
| `regulation:schematic.alimentation` | Idem — pas de référence TS repérée. |

**Arbitrage proposé :** lot S3-bis **optionnel** — pour chaque clé INV, grep + test UI ou décision « dette acceptée » ; pas de lot global sur les 343 clés.

---

## Préfixes dynamiques déjà exclus de l’audit (`i18n-key-audit.mjs`)

`errors:*`, `enums:attention*`, `enums:completionBlocker.*`, certains préfixes `structure:Qual*` / `Placement*` / `Prog*` — usage runtime / Host.

---

## Critère de fin S3 (audit)

- [x] Buckets documentés et reproductibles.
- [x] Pluriels et chemins relatifs **hors** périmètre suppression.
- [ ] Arbitrage sur S3-INV (5 clés) ou dette acceptée explicite.
- [ ] Pas de lot « 686 suppressions ».

---

## Repro

```bash
cd src/PlayUp/web
npm run i18n:audit:repo
npm run i18n:audit:qualify
npm run i18n:check
```
