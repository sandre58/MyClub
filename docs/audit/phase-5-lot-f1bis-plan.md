# Phase 5 — Lot F1bis (AUD-0001 global audit)

**Go décideur:** 2026-10-22  
**Complète F1:** repoint + scoped **et** restauration des outils d’audit **repo-wide** (contributor / investigation — **pas** gate CI).

## Livré

| npm script | Script | Périmètre |
| --- | --- | --- |
| `frontend:audit` | `scripts/frontend-audit.mjs` | Graphe CSS, classes orphelines (heuristique), composants DS, clés i18n FR (échantillon), blocs décl. dupliqués |
| `i18n:audit:repo` | `scripts/i18n-key-audit.mjs` | Clés feuilles fr+en suspectes (heuristique + préfixes dynamiques) |
| `css:audit` | `audit-structure-fiche-css.mjs` | Inchangé — fiche Structure |
| `i18n:audit` | `audit-structure-i18n.mjs` | Inchangé — `structure.json` (en) |
| `tile:check` | `tile-family-check.mjs` | Inchangé |

**CI:** inchangée (`lint:css` Stylelint uniquement pour le CSS normatif).

## IDE (Rider)

Les alertes CSS Rider ≠ automatiquement Stylelint. **Référence CI / npm:** `npm run lint:css`. Voir `src/PlayUp/web/docs/conventions.md` (section CSS tooling).

## Hors scope F1bis

- Corriger les ~100 classes / 60+ groupes dupliqués signalés par `frontend:audit` (lot hygiène séparé, preuve par finding).
- Ajouter `frontend:audit` en CI.
- Lot correctif F2–F4 (toujours en attente de go registre).
