# O-04 — Fiche Terminée (`Completed`) — cadrage Δ-O4-1 / Δ-O4-2

**Status:** **Δ-O4-2 Closed** (2026-10-16) — commit `ec45ed60` on `feature/new_version_alt`. **Δ-O4-1** : Open (go distinct). Cadrage O-04 global inchangé.

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

**Surfaces voisines** — lifecycle Completed sans bandeau Prepare/Start ; matchs sans materialize ; diagnostics hub masqués ; **Renommer la phase** encore visible (hors Δ-O4-2).

**Verdict recette** — **4/5 scénarios manuels OK** ; scénario **2** couvert par tests automatisés uniquement (réserve documentée, ne bloque pas la clôture).

### Clôture tranche Δ-O4-2

- Commit dédié code + cette fiche ; Notion registre **Δ-O4-2** (Closed + SHA).
- **Δ-O4-1** reste **Open** jusqu’au go suivant.

---

# Tranche 2 — Δ-O4-1 (densité mode audit)

**Objectif** — Faire lire la fiche **Completed** comme un **bilan**, pas comme une phase en exploitation : réduire densité sans **supprimer** les preuves d’état final.

**Prérequis** — Δ-O4-2 **clôturée** (plus de faux CTAs strip).

### Principes de réduction (à respecter à l’implémentation)

| Catégorie | Traitement cible |
|-----------|------------------|
| **Masquer** | Empty states « constituer », toolbars compose, CTAs secondaires suggestifs, checklist ops déjà couverts par Δ-O4-2 |
| **Condenser** | Hero stats, rails qualifiés / pool (listes → synthèse ou sections repliées selon matrice « réduite ») |
| **Conserver visible** | Occupation finale schéma, résumé tirage/places, sorties/awards en lecture, réglement digest, lien matchs |

« Réduire la densité » **≠** retirer l’information utile à l’audit (places occupées, tirage final, résultat matchs).

### Critères d’acceptation

- [ ] **`Completed`** : composition **visuellement moins dense** qu’une phase **Running** comparable (même compétition, progress `running` vs `finished`) — recette comparative obligatoire.
- [ ] Sections matrice **réduites** : composition, placement opérationnel (post Δ-O4-2), sorties, awards, schéma, réglement — **lecture** ; pas de crayons / compose.
- [ ] **Titre phase** : pas de crayon rename (**O-04 UI** ; hub C-18 inchangé).
- [ ] **Running / Ready / Draft** : pas de régression layout (tests spot ou recette).

### Fichiers concernés (indicatif)

| Fichier | Nature |
|---------|--------|
| `StructurePhaseFiche.tsx` | Flags `Completed` pour hero / sections / `showCompositionBandEmpty` |
| `StructurePhaseFicheHeader.tsx` | `canRename` → false en UI si `Completed` (sans toucher hub) |
| `StructurePhaseCompositionBand.tsx` | Layout réduit / sections conditionnelles |
| CSS modules structure phase | Classes « audit » / density si nécessaire |

### Tests / recette Δ-O4-1

- Tests unitaires **légers** sur gates de sections (éviter tests pixel-perfect).
- Recette **paire** : même seed `championship:running` vs `championship:finished` (ou groups/cup) — checklist densité + contenu final toujours présent.

### Clôture tranche Δ-O4-1

- Commit dédié + Notion **Δ-O4-1** Closed.
- Mettre à jour `README.md` lifecycle + backlog post-gel (O-04 impl. livrée).

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
