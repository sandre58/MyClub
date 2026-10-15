# Δ-03 — Hub P1 draw rules in Ready (C-19)

**Status:** **Closed** (2026-10-16) — commit `d371baf6` on `feature/new_version_alt`.

**Décision produit (gelée)** — Notion [Matrice UI Structure — § Δ-03](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) · **B-Q1b** : `ReplaceStageDrawRules` / P1 = **Brouillon uniquement** ; en **Prêt** = digest + **P3** (workflow), pas Activer / Paramètres / Désactiver P1.

**C-19 résolu** — Hub Ready n’expose plus `ReplaceDrawRules` ; SPA borne P1 au Brouillon (`canAuthorDrawRulesP1`).

---

## Vérification technique — hub vs P1 vs P3 (SPA)

| Signal SPA | Définition actuelle | Dépend du token hub `ReplaceDrawRules` ? | Impact retrait hub Ready |
|------------|---------------------|------------------------------------------|---------------------------|
| **`showDrawCta`** | `stage.hasDrawRules` | **Non** | **P3 conservé** (Lancer / Voir le tirage) |
| **`showActivateDrawCta`** | `!hasDrawRules && canEditDraw && (Cup\|Groups)` | **Oui** | **Activer** disparaît en Prêt (voulu) |
| **Toolbar P1** (icônes params / désactiver, colonne non-composition) | `drawEngaged && canEditDraw` | **Oui** | Masqué si `canEditDraw` false |
| **Bandeau composition P1** (Paramètres / Désactiver) | `canEditDraw && !drawApplied` | **Oui** | Masqué si `canEditDraw` false |
| **`showPlacementBlock`** | `showDrawCta \|\| showActivate \|\| manualPlace \|\| tracksOperationalPlaces` | Partiel | Prêt Cup/Groups **sans** draw : jauges / places / placement manuel **restent** via `tracksOperationalPlaces` / `canManualPlace` — pas de fiche « vide » |

**Conclusion :** retirer `ReplaceDrawRules` du hub **Ready** ne doit **pas** couper P3, car `showDrawCta` est indépendant du token.

**Renfort SPA recommandé (ceinture + bretelles)** — ne pas faire reposer P1 uniquement sur le hub :

- Introduire une capacité explicite du type `canAuthorDrawRulesP1` = `actions.includes('ReplaceDrawRules') && stage.status === 'Draft'`.
- Dériver `showActivateDrawCta` et les props P1 du strip de **`canAuthorDrawRulesP1`**, pas de `canEditDraw` seul.
- Conserver `canEditDraw` ou un alias **uniquement** si un autre écran en a encore besoin en Prêt (audit : **non** pour P1 — réglement strip n’expose pas le crayon tirage P1 en Prêt).

**Prêt sans règles engagées — message produit**

- Pas de CTA « Activer le tirage ».
- Chemin attendu : **Revenir en brouillon** (`RevertToDraft` / lifecycle header) pour engager P1 — conforme B-Q1b.
- Recette : vérifier que la section Composition / Places reste lisible (jauges, éventuel placement manuel Cup) et que le lifecycle Prêt propose le retour brouillon quand Start est bloqué.

---

## Critères d’acceptation

### Hub (Application)

- [ ] Pour toute phase **`Ready`** sans match attaché (`attachedMatches == 0`), `stage.actions` **ne contient pas** `ReplaceDrawRules`, y compris si `StageNeedsDrawRulesAction(stage)` est vrai (Cup / Groups).
- [ ] **Brouillon** : `ReplaceDrawRules` inchangé quand `StageNeedsDrawRulesAction` (régression).
- [ ] **Running / Suspended** : pas de changement sur `BuildExploitationStageActions` (pas de `ReplaceDrawRules` aujourd’hui).

### SPA

- [ ] **Prêt** : aucun bouton **Activer le tirage**, **Paramètres de tirage**, **Désactiver le tirage** (P1) sur le strip composition / toolbar tirage.
- [ ] **Prêt** + `hasDrawRules` : CTA **P3** présent selon règles existantes (`showDrawCta`, gates create, `drawApplied` → libellé secondaire « voir »).
- [ ] **Prêt** + `!hasDrawRules` (Cup/Groups) : pas d’état « activer » ; section placement / lifecycle toujours cohérente.
- [ ] **Brouillon** : P1 inchangé (Activer / params / désactiver selon guards existants).

### API / domaine

- [ ] `PUT /stages/{id}/draw-rules` sur phase **Ready** : toujours **refusé** (test d’intégration ou use case existant — pas de assouplissement).

---

## Fichiers concernés (confirmés par lecture code)

| Fichier | Nature du changement |
|---------|----------------------|
| `src/PlayUp/MyClub.PlayUp.Application/Reads/StructureViewAssembler.cs` | Retirer le bloc `ActionReplaceDrawRules` dans `BuildReadyStageActions` (L850–853) |
| `src/PlayUp/web/src/pages/structure/StructurePhaseFiche.tsx` | Séparer P1 vs P3 : `canAuthorDrawRulesP1` + `showActivateDrawCta` ; props placement |
| `src/PlayUp/web/src/pages/structure/structurePlacementOperationalProps.ts` | Renommer / documenter prop P1 si nouveau nom (`canAuthorDrawRulesP1`) |
| `src/PlayUp/web/src/pages/structure/StructurePhaseOperationalStrip.tsx` | Utiliser la prop P1 pour toolbar / `drawSetupEditable` (si pas déjà couvert par fiche) |

**Probablement sans changement fonctionnel**

- `StructureRegulationDialogs.tsx` — dialogs P1 ouverts depuis fiche uniquement.
- `StageEndpoints.cs` / `ReplaceStageDrawRules` — domaine inchangé.
- Drawer, diagnostics, attention, policy présentation.

**Tests à ajouter / ajuster**

| Fichier | Objectif |
|---------|----------|
| `tests/MyClub.PlayUp.Application.Tests/Reads/StructureViewAssemblerDrawRulesTests.cs` (**nouveau**) ou extension proche | Ready Cup/Groups : pas `ReplaceDrawRules` ; Draft : oui |
| `src/PlayUp/web/src/pages/structure/StructurePhaseOperationalStrip.test.tsx` | Prêt + `showDrawCta` + `canAuthorDrawRulesP1: false` : pas Paramètres/Désactiver ; P3 visible |
| `src/PlayUp/web/src/pages/structure/StructurePhaseOperationalStrip.test.tsx` | Prêt + `showActivateDrawCta: false` : pas Activer |
| `tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageDrawRulesTests.cs` | Conserver / mentionner refus Ready (si absent, ajouter un cas) |
| Optionnel Host | `draw-rules` sur stage Ready → 4xx (si scénario seed stable) |

---

## Recette manuelle (5 scénarios)

Seed / compétitions : privilégier **Cup** et **Groups** **Prêt** sans production (`attachedMatches == 0`), plus un cas **Brouillon** pour régression P1.

1. **Prêt, sans règles engagées** (`hasDrawRules === false`, Cup ou Groups)  
   - Pas « Activer le tirage ».  
   - Jauges / places / manual place Cup si applicables.  
   - Lifecycle : chemin **Revenir en brouillon** compréhensible si l’organisateur doit engager le tirage.

2. **Prêt, règles engagées, tirage non appliqué**  
   - Pas Paramètres / Désactiver P1.  
   - **Lancer le tirage** (ou équivalent P3) visible si gates OK.

3. **Prêt, tirage appliqué**  
   - Consultation workflow (**Voir le tirage** / secondaire).  
   - Pas édition P1.

4. **Brouillon** (même format)  
   - Activer / Paramètres / Désactiver inchangés vs avant la tranche.

5. **API** — `ReplaceStageDrawRules` sur stage **Ready** (DevTools ou script) : refus métier inchangé.

---

## Exclusions explicites

- **P2** — cancel Published, copy parcours correction draw entity.
- **P3** — refonte workflow, gates create, draw dialog (sauf régression).
- **O-03** pause / diagnostics / drawer / compteurs / sévérités.
- **C-20** — `ReplaceStandingRules` domaine en Ready.
- **Δ-O4-*** / **O-04** — Terminée.
- **Hub** — autres tokens Ready (qual/prog/affectation/slots) : hors Δ-03.
- **Domaine** — pas d’amendement B-Q1b.

---

## Vérification automatisée (2026-10-16)

| Suite | Résultat |
|-------|----------|
| `StructureViewAssemblerDrawRulesTests` + `ReplaceStageDrawRulesTests` (filtre Δ-03) | 9/9 |
| `StructurePhaseOperationalStrip.test.tsx` | 25/25 |

## Recette manuelle (2026-10-16, DevRunner + Host rebuild + Vite)

| # | Scénario | Jeu de données | Résultat |
|---|----------|----------------|----------|
| 1 | Prêt, sans règles | `cup-composition-partial` → Prepare | Pas P1 ; places/manuel ; **Back to draft** |
| 2 | Prêt, règles, tirage non appliqué | `groups-draw-pending` → Prepare | Pas P1 ; **Run the draw** |
| 3 | Prêt, tirage appliqué | `RECETTE C1` → Prepare | **View draw** ; pas P1 |
| 4 | Brouillon P1 | `cup-composition-complete` | Settings / Deactivate / Run the draw |
| 5 | API | `PUT …/draw-rules` en Prêt | **409** ; hub sans `ReplaceDrawRules` |

## Clôture de tranche

- Commit dédié sur `feature/new_version_alt` (SHA ci-dessus après push).
- Registre **C-19** / **Δ-03** Notion : Closed + lien commit.
- `docs/design/structure-lifecycle/README.md` — ligne Δ-03.

---

## Références Git

- `docs/design/structure-lifecycle/phase-mutability-matrix.md` — §9.1.1, `ReplaceStageDrawRules` row.
- `docs/design/structure-lifecycle/section-11c-addendum-clarification-arbitration.md` — §4.6.1 B-Q1b.
- Notion — [Matrice UI Structure](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) (Δ-03 fermée).
