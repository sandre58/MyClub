# G-OPS3 — Session d’arbitrage

## Décision — G-OPS3 : différer le gate draw-refuse, signer les contrats négatifs R-Draw1

| | |
|--|--|
| **Date** | 2026-10-15 |
| **Périmètre** | G-OPS3 / `ReplaceStageAffectationAuthoring` / tirage **Applied** |
| **Statut** | **Décision signée** — contrats **R-Draw1 négatifs** validés ; gate **différé** ; spec **Spec draft** (pas §12 Ready) |
| **Impact** | Moyen — intégrité tirage vs composition |

### Décision

1. **R-Draw1 (refus positif)** — refuser une modification de composition qui **invalide** un tirage **Applied** — reste l’**objectif produit** §11A, mais le **gate commande** n’est **pas** implémenté tant que **D3-1** (témoin d’invalidation) n’est pas signé.
2. **Contrats négatifs signés** (non-régression pour un futur gate) :
   - tirage **publié non appliqué** → affectation modifiable ;
   - tirage **appliqué** + **même** set d’entrées → replace autorisé ;
   - tirage **annulé** → affectation modifiable.
3. Pas de wire code (`Application.DrawCompositionConflict` ou autre) ni guard application à ce stade.
4. **G-OPS3** reste **Spec draft (impl deferred)** dans §12 — pas de promotion **Ready**.
5. **D3-2** : première implémentation future limitée à **`ReplaceStageAffectationAuthoring`** (pas qual/prog dans le même slice).
6. **D3-3** : à la réouverture, privilégier **erreur mutation** (footer dialog) en slice 1 — pas hub persistant obligatoire.

### Motif

- **Écart confirmé** : la SPA verrouille partiellement les règles de tirage ([`areDrawRulesLockedByExecution`](../../../src/PlayUp/web/src/pages/structure/drawUi.ts)) ; la commande passe encore par G-OPS1/2 uniquement ([`ReplaceStageAffectationAuthoring`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageAffectationAuthoring/ReplaceStageAffectationAuthoring.cs)).
- **Applied** est dérivé ([`DrawAppliedState`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DrawAppliedState.cs)) — le gate doit s’appuyer sur ce contrat, pas sur le badge UI seul.
- **D3-1 ouvert** : réduire l’entrée du pool vs égalité stricte du set vs diff slots/pots — sans choix, un gate risquerait des faux positifs (ex. shrink avec Applied) ou des faux négatifs.
- **R-Draw1 positif** (retirer une entrée du pool avec tirage Applied) **échoue encore** côté commande — comportement connu, **non** figé en test de succès (contrairement aux négatifs).

### Contrats signés — R-Draw1 négatifs

| ID | Scénario | Test |
|----|----------|------|
| **R-Draw1-N1** | Published, pas Applied → shrink affectation OK (phase **Draft**, poules sans entrées en groupes) | `ReplaceAffectationAuthoring_allows_shrink_when_group_draw_published_not_applied` |
| **R-Draw1-N2** | Applied, même entrées → replace OK | `ReplaceAffectationAuthoring_allows_replace_with_same_entries_when_group_draw_applied` |
| **R-Draw1-N3** | Draw cancelled → change OK | `ReplaceAffectationAuthoring_allows_change_when_draw_cancelled` |

Fichier : [`ReplaceStageAffectationAuthoringGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageAffectationAuthoringGOps3Tests.cs).

### Arbitrages D3-1..3 (statut)

| ID | Statut 2026-10-15 |
|----|-------------------|
| **D3-1** | **Ouvert** — bloque gate |
| **D3-2** | **Orienté** — affectation seule en v1 |
| **D3-3** | **Orienté** — erreur mutation slice 1 |

### Hors périmètre

Guard application / HTTP ; modification domaine draw ; SPA parity obligatoire dans ce slice ; §11B ; G-MAT3/4 (déjà traités).

### Conditions de réouverture

1. **D3-1** signé avec scénario **R-Draw1 positif** (test commande refuse avant persist) ; ou  
2. Décision produit explicite d’un témoin **minimal** (ex. « toute entrée présente dans la résolution Applied doit rester dans l’affectation ») + tests +/-.

**Mise à jour 2026-10-15 (§11C / Phase A doc, pas implémentation) :**

- **D3-1 borné** pour le slice proposé : **égalité stricte du set** `EntryId` (allow si identique — **R-Draw1-N2** ; refuse si différent avec tirage **Applied**).
- **Implémentation autorisée** uniquement après enregistrement **§11C.4** `SLICE-1-G-B-3-ENF` + Notion — fiche [slice-1-g-b-3-enf.md](./slice-1-g-b-3-enf.md).
- Tant que **SLICE-0-none** : gate **non** livré ; G-OPS3 reste **Deferred** §12.

### Vigilance

Les contrats négatifs **n’autorisent pas** le shrink avec tirage Applied — ils documentent seulement ce qui doit **rester** permis quand le gate existera. Le gap R-Draw1 positif reste un **risque produit** jusqu’à implémentation.

### Checklist d’exécution

- [x] Tests R-Draw1-N1..N3
- [x] Décision consignée (cette fiche)
- [x] G-OPS3 **Spec draft** §12 — pas **Ready**
- [x] Aucun guard / wire code
- [x] Spec [g-ops3](./g-ops3-draw-refuse-on-composition-change.md) synchronisée

---

Spec : [g-ops3-draw-refuse-on-composition-change.md](./g-ops3-draw-refuse-on-composition-change.md)
