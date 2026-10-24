# Phase 6 — S3-INV — Arbitrage (5 clés `regulation`)

**Date:** 2026-10-22  
**Décideur:** go explicite sur les 5 clés `investigate` de [`phase-6-s3-i18n-qualification.md`](phase-6-s3-i18n-qualification.md).  
**Lot:** suppression ciblée fr+en après preuve (pas de lot global 686).

## Tableau d’arbitrage

| Clé | Preuve d’inutilisation | Contrat produit actuel | Décision |
| --- | --- | --- | --- |
| `regulation:personalized` | Aucun `t('personalized')`. Badge phase personnalisée : `t(regulationStagePersonalizedHintKey())` → `binding.specificToPhase` + tooltip `differsFromFrame` (`RegulationPage.tsx`, test « Règles adaptées à cette phase »). | Libellé court « Personnalisé » **non** exposé. | **Supprimer** — résidu mock / ancienne variante. |
| `regulation:loadError` | Aucun `t('loadError')` dans le module regulation. Erreur chargement structure : `ErrorState` → namespace `common` (`RegulationPage.tsx` L84–88). | Message d’erreur **générique** API, pas de copie règlement dédiée. | **Supprimer** — doublon mort ; si copie dédiée un jour → nouvelle clé + arbitrage UX. |
| `regulation:phaseStructureAria` | Aucune référence. Colonnes règles phase : `aria-label={t('phaseRegulationAria')}` (`RegulationPage.tsx` L419). | Aria **règlement** de phase, pas « structure ». | **Supprimer** — drift de nom / section schéma non livrée sur hub Règlement. |
| `regulation:schematic.swissRound` | `phaseSchematic.tsx` utilise `regulation:schematic.swiss` / `swissGeneric` pour les aria-labels suisses, **pas** `swissRound` par ronde. | Pas de libellé « Ronde n » i18n dans le schéma actuel. | **Supprimer** — clé préparatoire non branchée. |
| `regulation:schematic.alimentation` | Aucune référence ; modèle schéma sans zone alimentation (commentaires `phaseSchematic.tsx`). | Alimentation = concept authoring (Structure), pas libellé schéma Règlement. | **Supprimer** — hors modèle UI actuel. |

## Non retenu

- **Rebrancher** `loadError` sur `RegulationPage` : changement produit sans demande ; `ErrorState` reste SoT.
- **Conserver** les clés « au cas où » : refus (même logique que S2-001 option 3).

## Vérification post-lot

```bash
cd src/PlayUp/web
npm run i18n:check
npm run i18n:audit:qualify   # bucket investigate → 0
```

## Suite S3

- Buckets **plural_suffix** / **namespace_root_only** : toujours **hors suppression** (faux positifs heuristiques).
- Pas d’autre clé **confirmée** sans preuve équivalente.
