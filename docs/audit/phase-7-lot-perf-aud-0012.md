# Phase 7 — Lot P7-Perf (AUD-0012) — D2-B lot 2

**Go:** 2026-10-22  
**Registre:** AUD-0012 reste **dette acceptée** (mitigation partielle cumulée avec D2-B lot 1).

## Contexte

- Wave 1 a rejoué `Category=PerformanceBaseline` sans delta vs phase 4.
- D2-B **lot 1** (batch `FixtureAttachmentRef` par stage) déjà sur `main` — voir `phase-4-lot-d2-b-attachment-batch.md`.

## Hypothèse (lot 2)

**`GetByIdsReadOnlyAsync` / `GetByIdsForUpdateAsync`** appelaient `HydrateOrderedCollectionsAsync` **une fois par stage**, dupliquant les requêtes SQL de structure (`GroupEntryRef`, tri `Round`, pièces jointes). Une hydratation **groupée** sur la liste chargée fusionne ces allers-retours sans toucher aux graphes `Include`.

**Hors scope:** réduction des `Include`, changement de profils côté produit, bundle Vite.

## Critères d’acceptation

| Critère | Résultat |
| --- | --- |
| Même filtre `PerformanceBaseline` | ✅ 6/6 verts |
| SQL ↓ sur multi-stage | ✅ voir tableau |
| Comportement (ordre rounds / entries / attachments) | Inchangé (même logique, requêtes fusionnées) |
| Lot 1 inchangé | ✅ list matches toujours **18** SQL |

## Avant / après (Testcontainers, 2026-10-22)

| Scénario | Métrique | Avant lot 2 | Après lot 2 |
| --- | --- | ---: | ---: |
| cup-qf-sf | `GetByIdReadOnly(Full)` | 17 | 17 |
| cup-qf-sf | `GetByIdsReadOnly(Full)` N=2 | 18 | **17** |
| cup-qf-sf | `GetOverviewViewAsync` (et bundles alignés) | 21 | **20** |
| cup-qf-sf | `GetMatchHubViewAsync` | 19 | **18** |
| groups:running | `GET /stages/{id}/matches` | 18 | 18 |

## Fichiers

- `StageRepository.cs` — `HydrateOrderedCollectionsAsync(IReadOnlyList<Stage>)` + appels groupés depuis `GetByIds*`.
- Plafonds régression : `StageRepositoryLoadBaselineTests`, `ReadPerformanceBaselineTests`.

## Références

- `docs/audit/phase-4-d2-stage-repository-investigation.md` §6–§7
- `docs/audit/phase-7-wave-1-security-perf-ops-baseline.md` §2–§3

## Statut lot

**Clos** (implémentation + mesures) — pas de clôture registre AUD-0012 sans décision explicite.
