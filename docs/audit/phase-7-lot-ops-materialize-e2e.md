# Phase 7 — Lot P7-Ops — Materialize Host E2E (path #2)

**Go:** 2026-10-22  
**Scope:** signal régression sur `POST /stages/{id}/matches/materialize` (Groups), aligné sur la matrice `critical-paths.md` — **API Playwright**, pas le flux UI Structure.

## Livrables

| Item | Détail |
| --- | --- |
| Seed | `structure-materialize-recette` — compétition **RECETTE G1 — Groups 0/6 actionable** |
| Spec | `e2e/business/groups-materialize.spec.ts` |
| Helpers | `hostSeed.ts` — `findRecetteG1GroupsCompetition`, `getStructureMaterializationState`, `runGroupsMaterializeMatches` |
| CI | `e2e-integration` seed étendu avec `structure-materialize-recette` |

## Critères

- Avant materialize : `readiness.attachedMatchCount === 0`
- `POST .../materialize` → `createdCount > 0`
- Après : `attachedMatchCount === 6` (2×3 groupes, calendrier complet G1)

## Hors scope

- Cup `materialize-from-slots` (C1 recette — lot ultérieur si besoin)
- Navigation Structure / boutons materialize en UI

## Statut

**Clos** lorsque CI `e2e-integration` vert.
