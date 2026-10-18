# G-MAT4 — Session d’arbitrage

## Décision — G-MAT4 : différer le gate, signer la garantie A5 (M4b)

| | |
|--|--|
| **Date** | 2026-10-15 |
| **Périmètre** | G-MAT4 / `SourcePopulationShortfall` / materialization Championship·Groups |
| **Statut** | **Décision signée** — **R-Mat-M4b** validé ; gate G-MAT4 **différé** ; spec **Spec draft** (pas §12 Ready) |
| **Impact** | Moyen — contrat analyzer vs materialize |

### Décision

1. **R-Mat-M4b** est retenu comme garantie de non-régression **A5** : un diagnostic `SourcePopulationShortfall` sur une phase **T** sans lien d’extraction requis pour le batch de **S** ne doit pas, à lui seul, bloquer `MaterializeMatches` sur **S**.
2. **R-Mat-M4a** (refus positif gate dédié) **n’est pas atteignable** sous le runtime actuel sans redondance avec le refus existant — voir motif.
3. Aucun gate materialize G-MAT4 n’est introduit ; pas de wire code `Application.SourcePopulationShortfallAtMaterialize` ni mapping HTTP dédié.
4. **G-MAT4** reste **Spec draft** dans §12. Pas de promotion **Ready**, pas d’implémentation gate.
5. Le **Δ** documenté reste ouvert : [`AnalyzeCapacity`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs) (extraction qual sur source) vs [`ChampionshipSwissPhasePlaces.IsRosterComplete`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/ChampionshipSwissPhasePlaces.cs) sur la **cible** — fermeture produit seulement si une **réouverture** justifie un gate distinct.

### Motif

- `SourcePopulationShortfall` : `outboundQual(U) > availablePopulation(U)` alors que l’outbound ne dépasse pas les **places** structurelles de `U` (distinct de `OverExtraction`).
- `MaterializeMatches` (Ch/Groups) ne lit **pas** ce prédicat : il exige un roster **complet sur la phase cible** (`IsRosterComplete`) ou échoue en **`MaterializationFailure`**.
- Cas qual → championnat incomplet : Prepare peut passer (D3), materialize refuse déjà — [`PrepareQualFeedPendingMaterializeBlocksTests`](../../../tests/MyClub.PlayUp.Application.Tests/Reads/PrepareQualFeedPendingMaterializeBlocksTests.cs) ; le refus effectif est **roster cible**, pas shortfall source nommé.
- Cas championnat **S** indépendant avec poules **T** en shortfall : materialize **réussit** — [`MaterializeMatchesRMatM4bTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeMatchesRMatM4bTests.cs).
- **Cup** : hors périmètre (slots) ; pas de gate G-MAT4 Cup en v1.

Un gate G-MAT4 aujourd’hui **renommerait** `MaterializationFailure` ou exigerait un scénario artificiel sans gain métier distinct.

### Contrat signé — R-Mat-M4b

**Étant donné :** phase `T` avec `SourcePopulationShortfall` ; phase **S** (Championship) avec batch éligible et roster complet ; aucune dépendance du batch de **S** à l’extraction promettue sur **T**.

**Quand :** `MaterializeMatches` sur **S**.

**Alors :** succès sur **S** ; diagnostic sur **T** ne veto pas ; vérifier **résultat effectif** (matchs créés).

### Contrat documenté — chevauchement M4a / roster (pas un gate G-MAT4)

**Étant donné :** championnat **S** alimenté par qual depuis **T**, composition **incomplète** vs volume inbound attendu.

**Quand :** `MaterializeMatches` sur **S**.

**Alors :** `Application.MaterializationFailure` (roster incomplet sur **S**) — [`MaterializeMatches_overlap_documents_roster_refusal`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeMatchesRMatM4bTests.cs).

### Arbitrages M4-1..4 (statut)

| ID | Proposition initiale | Décision 2026-10-15 |
|----|----------------------|---------------------|
| **M4-1** | Code dédié 409 | **Non retenu** (gate différé) |
| **M4-2** | Ch d’abord | **Confirmé** pour toute réouverture future ; Groups en second |
| **M4-3** | vs `IsRosterComplete` | **Δ maintenu** — gate futur seulement si **strictement** plus fort que roster cible |
| **M4-4** | `OverExtraction` séparé | **Confirmé** — ne pas mapper à G-MAT4 |

### Hors périmètre

Wire code / HTTP G-MAT4 ; gate `MaterializeMatches` / Cup ; modification Prepare ; promotion §12 **Ready** ; G-OPS3 ; §11B.

### Conditions de réouverture

1. Scénario **M4a** réaliste où le refus est **distinct** de `MaterializationFailure` sur la cible **et** prouvé par test commande ; ou  
2. Décision produit de **fusion** ou **spécialisation** des messages (`MaterializationFailure` vs extraction source) avec contrat et scénarios +/- explicites.

### Vigilance

Signer M4b n’annule pas le diagnostic hub sur **T** (warning) ni les politiques Prepare / qual / OPS — seulement qu’il ne constitue **pas à lui seul** un veto materialize sur une phase **S** indépendante.

### Checklist d’exécution

- [x] Test émission diagnostic `SourcePopulationShortfall` (analyzer)
- [x] Test contractuel **R-Mat-M4b**
- [x] Test documentation chevauchement roster / `MaterializationFailure`
- [x] Décision consignée (cette fiche)
- [x] G-MAT4 **Spec draft** §12 — pas **Ready**
- [x] Aucun gate ni wire code hors périmètre

---

## Contexte (préparation)

Spec : [g-mat4-source-population-shortfall-at-materialize.md](./g-mat4-source-population-shortfall-at-materialize.md)
