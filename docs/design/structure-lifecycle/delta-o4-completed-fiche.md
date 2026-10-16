# O-04 — Fiche Terminée (`Completed`) — cadrage Δ-O4-1 / Δ-O4-2

**Status:** **O-04 validé** (2026-10-16) — SPA livré (**Δ-O4-2** `ec45ed60` · **Δ-O4-1** `68d0f50a`) ; recette comparative Championnat **OK** (deux passes DevRunner). **C-18** / domaine inchangés — prochain chantier distinct.

**Décision produit (gelée)** — Notion [Matrice UI Structure — § O-04 / Δ-O4](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) (2026-10-15) : phase **Terminée** = lecture seule **allégée** ; pas d’édition construction / production / sorties / awards / réglement ; **nom non modifiable en UI** ; état final tirage / places **consultable** sans signaux d’action opérationnelle.

**Ordre de livraison validé** — (1) **Δ-O4-2** strip placement / tirage → (2) **Δ-O4-1** densité mode audit. **C-18** (`RenameStage` hub Completed) **hors** ces tranches.

---

## Déjà aligné (ne pas rouvrir)

| Zone | Constat | Source indicative |
|------|---------|-------------------|
| Lifecycle header | Pas de bandeau Prepare / Start / Complete | `StructurePhaseHeaderLifecycle.tsx` · C-09 |
| Diagnostics structure hub | Bucket `hidden` en Completed | `structureDiagnosticPresentationPolicy.ts` · R-Pres-ST-1 |
| Production Matchs | Pas materialize / demat | `structureMatchsSectionUi.ts` |
| Matchs Terminée | Jauge exploitation + lien voir matchs | `StructurePhaseOperationalStrip.tsx` |
| Hub actions | `[RenameStage]` seul | `StructureViewAssembler.BuildStageActions` |
| Réglement strip | Digest sans crayons (Δ-01) | tokens Replace* absents |
| O-03 / O-03 pause / drawer | Hors périmètre Terminée | lots clôturés |

---

## Exclusions communes (toutes tranches)

- **C-18** — retrait `RenameStage` du hub Completed (écart **capacité** ; traitement ultérieur).
- **C-20** — standing rules domaine en Ready.
- **Domaine / API / Host** — pas d’assouplissement ni de nouveau garde-fou métier dans Δ-O4-*.
- **Diagnostics policy**, **compteurs**, **drawer** attention, **sévérités** wire.
- **Revert\*** / reprise lifecycle (arbitrages O* distincts).
- Refonte schéma, matchs aside (déjà partiellement conformes), Δ-03, P2 tirage.

### C-18 — règle de cadrage UI

- **Δ-O4** exige : **pas de crayon rename** sur le titre en `Completed` (affordance SPA).
- **Ne pas** retirer `RenameStage` du hub dans Δ-O4-1 / Δ-O4-2.
- Masquer le crayon **ne** fige pas le domaine : toute autre voie de rename reste hors scope de ces tranches.

---

# Tranche 1 — Δ-O4-2 (strip placement / tirage) — **Closed**

**Objectif** — Supprimer les **faux signaux d’action** sur le cluster Places / Draw en `Completed`, tout en gardant la **preuve** de l’état final (tirage appliqué ou non, occupation des places selon read model).

**Livraison** — SPA uniquement (`StructurePhaseFiche`, `StructurePhaseOperationalStrip`, tests strip). Pas de hub/API. **Renommer la phase** en UI reste visible en `Completed` → **Δ-O4-1** ; **C-18** inchangé.

### Invariants (non négociables)

1. `Completed` : **aucun** CTA qui incite à **lancer** ou **préparer** un tirage (Activer, Lancer, checklist readiness « à faire », jauges lues comme tâches en attente).
2. L’**état final reste consultable** — notamment **Voir le tirage** / workflow **lecture seule** lorsque `drawApplied` / tirage publié le justifie.
3. **Draft**, **Ready**, **Running**, **Suspended** : comportement strip **inchangé** (régression explicite en tests).
4. Diagnostics, compteurs, drawer, gardes application (`StagePhaseExploitationGuard`, etc.) : **inchangés**.

### Critères d’acceptation (livraison)

#### SPA — composition / strip

- [x] **`Completed`** + Cup/Groups avec `hasDrawRules` : pas de **Lancer le tirage**, **Activer le tirage**, Paramètres / Désactiver P1, placement manuel **actionnable**, ni checklist readiness « préparation ».
- [x] **`Completed`** + tirage appliqué / publié : consultation conservée (**Voir le tirage** secondaire).
- [x] **`Completed`** : pas de jauges pool / hints ops ni release CTA sur le strip (places : jauge ratio consultable).
- [x] **`Completed`** sans `hasDrawRules` : pas de cluster draw ops (ex. championnat terminé).
- [x] **Draft / Ready / Running / Suspended** : strip placement / draw inchangé (tests automatisés + recette spot).

#### Hub / API

- [x] Aucune modification `StructureViewAssembler` ni endpoints pour Δ-O4-2.

### Fichiers concernés (confirmés par lecture code — à revalider au go)

| Fichier | Changement attendu |
|---------|-------------------|
| `src/PlayUp/web/src/pages/structure/StructurePhaseFiche.tsx` | Gating `showPlacementBlock` / props strip pour `Completed` ; évent. `drawReadinessChecklist` / `showPoolHint` |
| `src/PlayUp/web/src/pages/structure/StructurePhaseOperationalStrip.tsx` | Branche `Completed` Places/Draw : corps lecture seule + CTA consultation |
| `src/PlayUp/web/src/pages/structure/structurePlacementOperationalProps.ts` | Props explicites si mode `completedAudit` / `readOnlyFinalState` |
| `src/PlayUp/web/src/pages/structure/StructurePhaseCompositionBand.tsx` | Pass-through props strip (si nécessaire) |

**Probablement sans changement** — `StructureDrawDialog.tsx` (sauf régression ouverture lecture) ; `structureMatchsSectionUi.ts` (Matchs déjà OK).

### Tests proposés

| Fichier | Objectif |
|---------|----------|
| `StructurePhaseOperationalStrip.test.tsx` | `status: 'Completed'` + `showDrawCta` : pas Lancer/Activer/Paramètres ; Voir tirage si `drawApplied` |
| `StructurePhaseOperationalStrip.test.tsx` | Ready + draw : régression Lancer présent |
| Optionnel | `StructurePhaseCompositionBand.test.tsx` si props band changent |

### Vérification automatisée (2026-10-16)

| Suite | Résultat |
|-------|----------|
| `StructurePhaseOperationalStrip.test.tsx` (dont Δ-O4-2 Completed + régression Δ-03 Ready) | **27/27** |

### Recette manuelle Δ-O4-2 (2026-10-16, DevRunner + Host + Vite)

Seed recommandé (un seul `--reset`) :  
`championship:finished,cup:finished,groups:running,cup-composition-complete`  
(éviter `championship:finished` + `groups:prepared` dans la même passe — collision d’ID déterministe DevRunner).

| # | Scénario | Jeu de données | Résultat |
|---|----------|----------------|----------|
| 1 | Completed, tirage **non** engagé | `championship:finished` (`d81b6474-…`) | **OK** — pas de cluster Tirage ops ; Places / équipes / Matchs consultables |
| 2 | Completed, règles engagées, tirage **non** appliqué | *Prévu fiche : `cup:finished`* | **Non rejoué en UI** — `cup:finished` applique toujours un slot draw (tirage Published/Applied) ; **lacune seed**, pas défaut fonctionnel. Branche UI : **Vitest 27/27** (`drawApplied: false`, message info, pas de CTA) |
| 3 | Completed, tirage **appliqué** | `cup:finished` (`2534918a-…`) | **OK** — message succès + **Voir le tirage** ; pas Lancer / Paramètres / checklist / placement manuel |
| 4 | Ready / Running | `groups:running` ; Ready complémentaire `groups-draw-pending` → `POST …/stages/{id}/prepare` | **OK** — strip draw exploitation inchangé (**Lancer** en Ready sans P1). Limite : `groups:prepared` seul ≠ Ready pur (souvent Running à 0 match) |
| 5 | Brouillon Cup | `cup-composition-complete` Draft | **OK** — Lancer / Paramètres / Désactiver / checklist / placement manuel présents |

**Surfaces voisines** — lifecycle Completed sans bandeau Prepare/Start ; matchs sans materialize ; diagnostics hub masqués ; crayon rename titre → **Δ-O4-1**.

**Verdict recette** — **4/5 scénarios manuels OK** ; scénario **2** couvert par tests automatisés uniquement (réserve documentée, ne bloque pas la clôture).

### Clôture tranche Δ-O4-2

- Commit dédié code + cette fiche ; Notion registre **Δ-O4-2** (Closed + SHA `ec45ed60`).

---

# Tranche 2 — Δ-O4-1 (densité mode audit) — **Closed**

**Objectif (resserré)** — Bilan **Completed** : masquer le crayon rename en SPA, ton **final** sur les jauges d’occupation, **une** réduction de densité justifiée (pas de refonte hero / liste équipes / eyebrow qualifiés).

**Prérequis** — Δ-O4-2 **clôturée**.

### Livraison SPA (2026-10-16)

| Sous-chantier | Implémentation |
|---------------|----------------|
| Rename UI | `StructurePhaseFiche.tsx` — `canRename={canRename && !isCompletedStage}` vers le header ; hub `RenameStage` inchangé (**C-18**) |
| Copy jauges Completed | `structureOperationalStripMessages.ts` + i18n `structure.json` (places `finalEmpty` / `finalPartial` ; composition population final shortfall/surplus) ; ton jauge adouci (`CompositionPopulationGauge`, pool Places strip via `isCompletedStageStatus`) |
| Densité (1 élément) | Masquer la **jauge population composition** en `Completed` quand le bloc placement est affiché (`showPlacementBlock`) — redondante avec la strip Places sur la paire running/finished |
| Hors scope | Pas de CSS dédié ; pas de masquage liste équipes / eyebrow qualifiés ; pas de changement `StructurePhaseCompositionBand` layout |

### Critères d’acceptation

- [x] **`Completed`** : pas de crayon rename titre ; autres statuts inchangés.
- [x] Jauges occupation **consultables** en Completed, formulation **finale** (distincte de Suspended `*Paused`).
- [x] Densité réduite (jauge population retirée quand strip Places présente) ; places, occupants, schéma, liens consultation conservés.
- [x] Aucun CTA opérationnel Δ-O4-2 réintroduit (strip inchangée hors copy/ton pool).
- [x] Tests ciblés OK ; recette comparative documentée ci-dessous.

### Vérification automatisée (2026-10-16)

| Suite | Résultat |
|-------|----------|
| `structureOperationalStripMessages.test.ts` (Completed place + composition copy) | inclus |
| `StructurePhaseOperationalStrip.test.tsx` (régressions Δ-O4-2 / Δ-03) | **27/27** |
| `StructurePhaseFicheHeader.test.tsx` (pas de bouton rename si `canRename={false}`) | **1/1** |
| **Total filtré Δ-O4-1** | **41/41** |

### Recette comparative Δ-O4-1

**Seed** — Comparer visuellement deux passes DevRunner (collision PK si `championship:running` + `championship:finished` dans un seul `--reset`) :

1. `--reset --scenarios championship:running` → fiche phase Running.
2. `--reset --scenarios championship:finished` → fiche phase Completed (`d81b6474-…`).

Checklist **Running** vs **Completed** (même scénario championship, progress différent) :

| Point | Running | Completed |
|-------|---------|-----------|
| Crayon rename titre | visible si hub `RenameStage` | **absent** |
| Strip Places — ton jauge | exploitation | **final** (pas « reste à placer ») |
| Jauge population sous composition (si placement block) | visible | **masquée** |
| Liste équipes / schéma / matchs | inchangé | inchangé |
| CTAs strip placement/tirage | exploitation | lecture seule (Δ-O4-2) |

**Procédure DevRunner** — Un seul `--reset` avec `championship:running,championship:finished` **échoue** (duplicate `PK_competitions`) ; utiliser **deux resets séquentiels** (même `competitionId` / `stageId` déterministes).

### Recette comparative exécutée (2026-10-16)

Environnement : DevRunner `PlayUpDev` aligné Host · Host `http://localhost:5287` · SPA Vite (port local variable). Identifiants stables (seed workspace `42`) :

| Clé | Valeur |
|-----|--------|
| Compétition | `d81b6474-eea6-5569-9b3a-9ff55dc119f5` (`Championnat généré`) |
| Phase | `045f6dd3-a0de-5a04-b3e4-85d05cc91bad` (`Championnat`) |
| URL fiche | `/competitions/{competitionId}/structure?stage={stageId}` |

| Passe | Commande | Statut API | Résultat visuel (checklist) |
|-------|----------|------------|-----------------------------|
| **Running** | `--reset --scenarios championship:running` | `Running` | **OK** — bouton **Rename phase** ; lifecycle (Complete / Suspend / Cancel start) ; **Edit standings** ; **8 équipes** listées ; strip Places + **View matches** |
| **Completed** | `--reset --scenarios championship:finished` | `Completed` | **OK** — **pas** de Rename phase ; **pas** de lifecycle ni Edit standings ; **8 équipes** inchangées ; Places + consultation matchs / réglement ; pas de CTAs exploitation strip (Δ-O4-2) |

**Verdict recette comparative** — **2/2 OK** sur Championnat. Densité Δ-O4-1 observable surtout via header/strip (pas de jauge population redondante sous composition quand le bloc placement est affiché). Complément optionnel non rejoué : `cup:finished` (jauges composition KO terminé).

### Clôture tranche Δ-O4-1

- Commit dédié code + cette fiche ; Notion registre **Δ-O4-1** (Closed + SHA).
- `README.md` lifecycle mis à jour (O-04 SPA livré).

---

## Validation globale O-04 (2026-10-16)

| Tranche | Tests | Recette manuelle | Verdict |
|---------|-------|------------------|---------|
| **Δ-O4-2** | Strip **27/27** | Matrice multi-scénarios **4/5** (réserve scénario 2 — seed) | **Closed** |
| **Δ-O4-1** | Ciblés **41/41** | Comparative Championnat **2/2** | **Closed** |

**O-04** — Pas de réouverture en l’absence d’anomalie constatée en recette. **C-18** — alignement hub/API `RenameStage` en `Completed` : audit contrat et décision serveur **hors** périmètre O-04 SPA.

---

## Registre dettes liées (hors tranches)

| ID | Sujet | Suite |
|----|--------|--------|
| **C-18** | Hub expose `RenameStage` en Completed | Dette capacité ; alignement hub **après** Δ-O4-1 UI crayon |
| **C-20** | Standing mutable domaine en Ready | Inchangé |

---

## Références

- Notion — [Matrice UI Structure](https://app.notion.com/p/3fa75fb92d10811abec9fc2c771584c4) § O-04, registre Δ, lignes Terminée matrice v0.3.
- `docs/guides/structure-phase-lifecycle-ui.md` — Completed / colonne Matchs.
- `docs/design/structure-lifecycle/structure-diagnostic-presentation-impact-rules.md` §3 — Completed `hidden`.
- Audit agent 2026-10-16 (conversation) — écarts strip vs lifecycle déjà conforme.
