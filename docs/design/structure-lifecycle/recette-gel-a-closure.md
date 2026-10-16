# Recette gel A — clôture (2026-10-16)

**Statut** : **Terminée** pour le périmètre exécuté (automatisation + passes UI ciblées). Aucune **anomalie** contractuelle constatée.

**Références** — [delta-o4-completed-fiche.md](./delta-o4-completed-fiche.md) · [ctrl-o3-mat-devrunner-recette.md](./ctrl-o3-mat-devrunner-recette.md) · [wave-1-capacity-audit-ready-d4.md](./wave-1-capacity-audit-ready-d4.md) · [wave-2-g-b-3-g-ops3-closure.md](./wave-2-g-b-3-g-ops3-closure.md).

---

## Automatisation

| Suite | Résultat |
|-------|----------|
| `CtrlO3MatRecetteIntegrationTests` | **8/8** |
| Vitest strip / matchs UI / drawer (gel A) | **44 + 36** |
| `StructureUseCaseTests` + `ReplaceStageAffectationAuthoringGOps3Tests` | **30/30** |

---

## Visuel (passe ciblée)

| Scénario | Résultat | Catégorie |
|----------|----------|-----------|
| **O-04 `Completed`** (`championship:finished`, `d81b6474-…`) | Pas de rename ; équipes visibles ; digest réglement sans crayons ; pas de CTA lifecycle ops ; hub `actions` vides | **Conforme** |
| **O-04 `Running`** (session antérieure, même seed) | Fiche rendue ; comparatif documenté Δ-O4-1 | **Conforme** (preuve partielle même session) |
| **CTRL-O3-MAT S2** | Reprendre ; pas de Matérialiser primaire (région Matchs) | **Conforme** |
| **CTRL-O3-MAT S4** | Reprendre ; copy Swiss ; pas de batch materialize | **Conforme** |
| **S2 aside Matchs** | Pas de primaire Matérialiser actif | **Conforme** |
| **S1, S3, S5–S7** (UI) | Non parcourus | **Non testé** (hors critère d’échec) |

---

## Exclusions (différés recette)

G-MAT3/4, G-B-1, C-ENF, reliquat G-OPS3 (qual/prog / obsolete) — **écart assumé**, pas bug gel A.

---

## Suite

Programme **B** : [wave-b-programme-after-gel-a.md](./wave-b-programme-after-gel-a.md). Renfort tests **W1-T** : voir commit tests dédié.
