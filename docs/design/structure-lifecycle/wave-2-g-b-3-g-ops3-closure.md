# Wave 2 — Clôture documentaire G-B-3 / G-OPS3 (preuves, sans élargir le périmètre)

**Status:** Revue (2026-10-16) — **lecture seule** ; pas de promotion **G-OPS3 → Done** sans preuve du périmètre exact.

---

## G-B-3 / SLICE-1-G-B-3-ENF

| Preuve | État |
|--------|------|
| Garde application | [`StructureAppliedDrawPoolChangeGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawPoolChangeGate.cs) dans `ReplaceStageAffectationAuthoring.Execute` |
| Code wire | `Application.AppliedDrawPoolChangeBlocked` → Host **409** ([`PlayUpExceptionHandler`](../../../src/PlayUp/MyClub.PlayUp.Host/PlayUpExceptionHandler.cs)) |
| **R-Draw1-N1, N2, N3** | [`ReplaceStageAffectationAuthoringGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs) |
| **R-Draw1+** (shrink + swap) | `ReplaceAffectationAuthoring_refuses_shrink_when_group_draw_applied`, `…_refuses_swap_with_same_count_when_group_draw_applied` |
| SPA copy | `lifecycleMutationMessageKey` + i18n en/fr + [`structureProblems.test.ts`](../../../src/PlayUp/web/src/pages/structure/problems/structureProblems.test.ts) |
| Matrice §12 | **G-B-3** = **Done** (ligne cohérente avec le code) |

### Lacunes acceptées (non bloquantes gel A)

| Lacune | Note |
|--------|------|
| **Pas de test Host** HTTP affectation + Applied draw | Couverture **application** directe ; slice demandait 409 — chemin executor non prouvé en intégration Postgres |
| **Concurrence inter-requêtes** | Limitation acceptée slice §4 — Notion task suivi |
| Fiche slice statut « Authorized » | Mise à jour **Closed** ci-dessous dans [slice-1-g-b-3-enf.md](./slice-1-g-b-3-enf.md) |

**Verdict G-B-3** — **Livré** pour le contrat signé (pool `EntryId` set + Applied draw sur la phase).

---

## G-OPS3 — statut **Partial** (maintenu)

**Périmètre signé** ([g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md)) — objectif large « refuser composition qui invalide tirage Applied », mais gate **différé** sauf D3-1 + slice.

| Sous-périmètre | Statut | Preuve |
|----------------|--------|--------|
| **R-Draw1+ / N*** sur **`ReplaceStageAffectationAuthoring`** (égalité stricte set `EntryId`, D3-1 borné) | **Livré** via **G-B-3** (ci-dessus) | Tests GOps3 |
| **Qual / prog** replace avec draw Applied | **Non livré** | Hors D3-2 v1 |
| **Obsolete-first (C-ENF)** | **Non livré** | Contrat C séparé |
| **Draw rules** / autres commandes composition | **Non livré** | G-OPS3 spec draft |
| **SPA disable-button parity** obligatoire slice | **Non exigé** SLICE-1 | Optionnel futur |

**Verdict G-OPS3** — Rester **Partial** dans §12. **Ne pas** passer à **Done**.

### Reliquat explicite (recette gel : comportement connu, pas bug)

1. Modifier qual/prog en Prêt avec tirage Applied — **non** couvert par G-B-3 (seulement affectation pool).
2. Toute invalidation draw **hors** changement de set `EntryId` — **non** gate.
3. Consommation tirage obsolète — **C-ENF-*** (interdit sans slice).

### Mise à jour doc arbitration

La checklist historique « Aucun guard / wire code » dans [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md) est **obsolète** pour la branche livrée : le guard **G-B-3** existe. La fiche arbitration est corrigée par note de bas de page (sans réécrire la décision 2026-10-15).

---

## Recette gel — périmètre vérifié

- D4 réglement jeu en **Prêt** : UI + domaine (+ standing API C-20).
- Affectation pool + tirage **Applied** : refus **409** `AppliedDrawPoolChangeBlocked` (mutation footer SPA).
- **Ne pas** attendre en recette gel : gates MAT3/4, ENF, G-B-1, G-OPS3 intégral.
