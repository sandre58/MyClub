# Phase 7 — Lot P7-Perf — Bundle Vite (code-split routes)

**Go:** 2026-10-22  
**Hypothèse:** le monolithe `index-*.js` (~1,16 MB min) vient surtout des pages produit importées statiquement dans `App.tsx`. Le lazy-load par route (déjà utilisé pour `/dev/foundations` et `/design-lab`) réduit le **premier chargement** sans changer le parcours produit.

**Hors scope:** lazy i18n par namespace, `manualChunks` vendor artificiels, changement de dépendances, objectif TTI/LCP mesuré en prod.

## Changements

| Zone | Détail |
| --- | --- |
| `App.tsx` | `lazy()` sur les pages sous `AppLayout` (+ `NotFoundPage`) ; `HomePage` reste eager (hub `/`) |
| `AppShell.tsx` | `Suspense` + `WaitMark` autour de `<Outlet />` |
| Build | Plus d’avertissement Vite « chunk > 500 kB » sur le build nominal |

## Avant / après (`npm run build`, 2026-10-22)

| Métrique | Avant | Après |
| --- | ---: | ---: |
| `index-*.js` (min) | 1 164.57 kB | **283.73 kB** |
| `index-*.js` (gzip) | 301.96 kB | **86.10 kB** |
| Plus gros chunk route (min) | — (tout dans index) | `StructurePage` **315.21 kB** (gzip 73.86) |
| Warning Vite > 500 kB | Oui | **Non** |

Les assets route (Structure, Regulation, Match, etc.) se chargent à la navigation ; le poids total de l’app reste comparable, la **surface initiale** (accueil + shell) baisse.

## Vérification

```bash
cd src/PlayUp/web
npm run build
npm run test:run
```

## Statut

**Clos** — signal dette « Vite chunk » atténué pour le chemin d’entrée ; pas de clôture phase 7 tant que materialize E2E / trajectoire C restent ouverts.
