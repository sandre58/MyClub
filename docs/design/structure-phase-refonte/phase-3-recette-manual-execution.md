# Structure phase fiche — Phase 3 : feuille de recette (navigateur)

**Statut :** closed (2026-10-18)  
**Date de référence :** 2026-10-18  
**Périmètre :** header, aside Matchs, aside Flux, digest règlement, schematic, intégration fiche.

**Autorité :** [actions-permissions-inventory](./actions-permissions-inventory.md), [truth-sources](./truth-sources.md), O-MAT-VIS-1, `structure-phase-lifecycle-ui.md`.

**Automated evidence (Vitest / .NET):** see [phase-3-recette.md](./phase-3-recette.md) § 2026-10-18 — historical replay, **not** a substitute for browser checks below.

---

## 1. Fiche d’exécution

| Champ | Valeur |
|:------|:-------|
| Date de recette | 2026-10-18 |
| Exécutant | Cursor Agent (session recette) |
| Branche / commit | `feature/new_version` · `8873a491` |
| Environnement | Local — Host `5287` / Vite `5173` (confirmé) |
| Compétition / seed | DevRunner `--reset` (voir §2) — 18 compétitions |
| URL de la fiche | `/competitions/{id}/structure?stage={stageId}` (par contrôle) |
| Locale | `fr` (UI observée) |
| Navigateur / viewport | Cursor IDE Browser (desktop ~1280px ; D6 : 390×844 mobile) |
| Dossier de preuves | Captures session : `%LOCALAPPDATA%\\Temp\\cursor\\screenshots\\P3-C2-*.png`, `P3-D6-*.png` |
| Verdict global | **Phase 3 clôturée** (typecheck/E2E hors périmètre) |

### Statuts autorisés

| Statut | Signification |
|:-------|:--------------|
| À tester | Aucune observation manuelle |
| Conforme | Comportement conforme à l’attendu |
| Écart | Comportement différent de l’attendu |
| Bloqué | Contrôle impossible (motif obligatoire) |
| N/A justifié | Non applicable (justification obligatoire) |

**Règle :** ne pas marquer « Conforme » sur la seule base d’un test Vitest. Automate et navigateur = deux preuves distinctes.

---

## 2. Préparation

- Vérifier branche et commit.
- Démarrer Host et Vite (workflow local).
- Préparer les données de recette ; identifier la compétition par scénario.
- Ouvrir la fiche Structure ; consigner URL et locale.
- Captures : `P3-[ID]-[scenario]-[viewport].png`.
- **Suspended :** suivre [ctrl-o3-mat-devrunner-recette.md](../structure-lifecycle/ctrl-o3-mat-devrunner-recette.md), seed `ctrl-o3-mat-recette`.

```bash
dotnet run --project src/PlayUp/MyClub.PlayUp.DevRunner -- --reset --scenarios ctrl-o3-mat-recette,flux-qual-form-draft,qual-form-champ-ready-partial,qual-form-champ-ready-full,qual-form-champ-running,swiss-ready,championship-ready,championship:finished,regulation-demo,regulation-tie-homogeneous,draft-empty,flux-qualif-draft
```

Après reset, confirmer les compétitions listées (préfixes `CTRL-O3-MAT`, `Flux —`, `Qual Forme →`, etc.) avant de conclure.

---

## 3. Checklist manuelle — 24 contrôles (+ B-SUS)

Pour chaque ligne : statut après observation ; preuve ou écart au § 5.

### A — Header (5)

| ID | Scénario | Attendu | Statut |
|:---|:---------|:--------|:-------|
| A1 | Phase Draft | Prepare dans la toolbar du header | Conforme |
| A2 | Aside Matchs en Draft | Aucune tuile Matchs | Conforme |
| A3 | Phase Ready | Actions de cycle selon permissions | Conforme |
| A4 | Phase Running (et états applicables) | Labels, disabled, tooltips pré-refonte | Conforme |
| A5 | Cas avec blocage + remédiation | Messages et liens sous la ligne de titre | Conforme |

**A3–A4 :** consigner état de cycle, action affichée / absente / disabled, tooltip. Vérifier Complete, Suspend, Resume quand proposés — ne pas déduire depuis un seul état.

### B — Aside Matchs (6)

Autorité O-MAT-VIS-1 : tuile seulement si contenu utile rendu ; cycle de vie dans le header uniquement.

| ID | Scénario | Attendu | Statut |
|:---|:---------|:--------|:-------|
| B1 | Draft | Tuile absente ; Prepare au header | Conforme |
| B2 | Production / progression utile | Contenu visible ; pas de CTA cycle dupliquée | Conforme |
| B3 | Champ Ready, signal placement, sans `materialize` | Pas de coquille vide Matchs | Conforme |
| B4 | Production / progression (cas recette) | Contenu visible ; règles strip inchangées | Conforme |
| B5 | Swiss Ready (`swiss-ready`) | Tuile masquée si pas de contenu utile | Conforme |
| B6 | Ready, signal cycle seul, sans production | Tuile masquée ; pas de shell vide | Conforme |

**B-SUS (obligatoire, hors B1–B6) :** seed `ctrl-o3-mat-recette` ; fiche complète ; Resume selon contrat — journaliser comme contrôle additionnel.

| B-SUS | `CTRL-O3-MAT S3` · stage `4a09d808-9988-5ea9-90c7-1b25b7156455` | `Reprendre la phase` header ; aside Matchs consultatif ; **aucun** bouton Matérialiser actif | **Conforme** |

**Completed :** lien « Voir les matchs » dans la toolbar — observé sur `Championnat généré` (Completed) et phases Running/Suspended concernées.

### Scénarios navigateur (référence rapide)

| Contrôle | Compétition | URL fiche |
|:---------|:------------|:----------|
| A1–A2, B1, C3 | Flux — Qual Forme Championnat (Champ Draft) | `/competitions/69e6946c-7da5-5b06-83a9-530149fbd089/structure?stage=4e7e7450-c230-5966-be53-369b3f73f876` |
| A3 | Championnat Ready | `/competitions/8c6c0bd6-9765-5f1d-a545-401dc5c0bcf8/structure?stage=2da84d69-ba81-5e9c-b578-6517c6cd77b5` |
| A4–B2–B4 | Qual Forme → Championnat (Running) | `/competitions/9b3a006f-d0a2-5cbd-9c9f-27fc73d2d74d/structure?stage=424f5859-b771-5ea5-bcf0-2162d20ba295` |
| A5, B3 | Qual Forme → Championnat (Ready) | `/competitions/1f39a744-8d00-5534-8b04-f6cad4358051/structure?stage=0e0e8eb7-5b5d-52b6-b0c2-36116a7c2a75` |
| B5 | Suisse Ready | `/competitions/515fa976-4f04-5ba2-9c2f-58afdf81d6bf/structure?stage=1aa61770-3212-5e83-9a71-0e43f71e3843` |
| B6 (cas 6) | **Suisse Ready** · seed `swiss-ready` | `/competitions/515fa976-4f04-5ba2-9c2f-58afdf81d6bf/structure?stage=1aa61770-3212-5e83-9a71-0e43f71e3843` |
| Cas 2 (hors B6) | Qual Forme → Championnat roster partiel | `/competitions/aaa5a397-acaf-588d-a451-59918381a3f9/structure?stage=37660c78-0a86-569e-838e-2b820940b785` |
| B-SUS | CTRL-O3-MAT S3 | `/competitions/2c4ae578-e4ef-5662-8e1a-98ed26fb4266/structure?stage=4a09d808-9988-5ea9-90c7-1b25b7156455` |
| Completed | Championnat généré | `/competitions/d81b6474-eea6-5569-9b3a-9ff55dc119f5/structure?stage=045f6dd3-a0de-5a04-b3e4-85d05cc91bad` |
| C1 | Flux — Qualification Draft (groupes) | `/competitions/0a6fca25-8e57-5952-b362-05ed7df45ba1/structure?stage=090f16e4-df07-5de3-840e-337ac19b5bcd` |
| D1–D5 | Digest sur fiches ci-dessus + liaison « hérité » | — |
| C2 | Démo Confrontation homogène · Finale Draft | `/competitions/9fb6a9b4-f65c-5360-a411-e52441441657/structure?stage=7dc45558-4f66-5b50-8d94-f749b7bc9e5d` |
| D6 | Démo Règlement · Groupes · mobile 390px | `/competitions/6f302053-0f0b-5dc8-8eee-2bcc5fde63c1/structure?stage=47785483-d9f0-5623-89b5-003df627e5e6` |

### C — Aside Flux (4)

| ID | Scénario | Attendu | Statut |
|:---|:---------|:--------|:-------|
| C1 | Sorties présentes | En-têtes + libellés alignés Qualifiés ; pas de chip volume | Conforme |
| C2 | Attribution présente | Médaille rang + ligne règle style Qualifiés ; pas de chip résultat | Conforme |
| C3 | Sorties vides | EmptyState compact, petite icône, faible hauteur | Conforme |
| C4 | Attribution vide | Idem C3 | N/A justifié |

Si Sorties et Attribution ne sont pas isolables sur une compétition, noter la limite et utiliser une autre seed. Ne pas conclure Conforme sans avoir vu l’état vide.

### D — Digest règlement (6)

| ID | Scénario | Attendu | Statut |
|:---|:---------|:--------|:-------|
| D1 | Digest visible | Sections Match / Classement / Confrontation si pertinentes | Conforme |
| D2 | Section digest | Icône marque à gauche ; titre, badge liaison, crayon toolbar | Conforme |
| D3 | Ligne des faits | Icône secondaire, valeur lisible, espacement large | Conforme |
| D4 | Règlement sans prolongation / TAB | Faits non applicables omis | Conforme |
| D5 | Modes liaison + toolbar | Badges général / personnalisé ; édition cohérente | Conforme |
| D6 | Footband + viewport étroit | Lien « Ouvrir le règlement » ; reflow ; séparateur dégradé | Conforme |

**D5 :** consigner le mode observé ; deux compétitions si les deux modes ne coexistent pas.

### E — Schematic et intégration (3)

| ID | Scénario | Attendu | Statut |
|:---|:---------|:--------|:-------|
| E1 | Tuile schematic | Pas de cadre extérieur en pointillés | Conforme |
| E2 | Parcours fiche complet | Header + Matchs + Flux + règlement ; pas de doublon cycle ; pas de coquille vide | Conforme |
| E3 | Rechargement + parcours | Rendu stable ; liens et actions cohérents | Conforme |

E2/E3 ne remplacent pas A–D ; noter le scénario exact pour tout écart.

---

## 4. Preuves automatisées — référence (18/10/2026)

Ne pas utiliser ce tableau pour cocher la checklist navigateur.

| Suite | Résultat | Portée |
|:------|:---------|:-------|
| `phase3MatchsAsideRecette.test.tsx` | 8/8 | Aside SPA + header couvert |
| Bundle Matchs (3 fichiers) | 50/50 | Tuile + strip |
| Lifecycle + Draft + Regulation + `StructurePage` | 54/54 | Header, digest, intégration |
| Sept fichiers Vitest (périmètre Phase 3) | 106/106 (clôture 18/10) | Couverture ciblée |
| .NET `Qual_form_champ_*` | 3/3 | Hub seeds couverts (`--no-build` si verrou build) |
| Playwright `test:e2e` | Non exécuté | `typecheck` app en échec sur working tree |

48/48 (17/10) → 50/50 (18/10) = décompte tests ajoutés, pas échec.

Vitest (`tsconfig.vitest`) a passé ; `typecheck` app (`tsconfig.app`) bloque build/smoke — décision technique séparée.

---

## 4bis. Arbitrage B6 (GAP-01) — investigation 18/10/2026

**Ne pas corriger le rendu ni assouplir `shouldShowAsideMatchsTile` avant décision explicite.**

Ordre de vérification (comme convenu) :

| Étape | Constats |
|:------|:---------|
| **1. Donnée scénario** | URL B6 = `aaa5a397` / stage `37660c78` (Champ **Ready**, composition **0/4**). Hub : `materialize` **présent** (`labelVariant: idle`, `disabledReason: PhaseRosterIncomplete`, compteurs à 0) ; `lifecycle.transition` = `StartStage` désactivé (`PhaseRosterIncomplete`). Ce n’est **pas** « cycle seul sans production » au sens hub : c’est le seed **`qual-form-champ-ready-partial`**. |
| **2. Présentation (`resolveAsideMatchsStripContent`)** | Ready + `materialize != null` → `shouldShowProductionInMatchsSection` = **true** → `showProductionBody` = **true** → `hasUsefulAsideMatchsContent` = **true** → tuile **affichée**. Aligné avec Vitest **`phase3MatchsAsideRecette` cas 2** (message composition incomplète, pas de CTA Materialize). |
| **3. Rendu fiche** | Tuile **MATCHS** + copie composition incomplète ; pas de chemin « cycle seul » isolé — le strip reçoit le bloc production vide roster. |

**Divergence recette ↔ auto :** la ligne checklist **B6** décrit le **cas 6** Vitest (`materialize` / `dematerialize` absents, seul signal lifecycle). Le seed utilisé en navigateur correspond au **cas 2** (roster partiel), où la tuile **doit** rester visible selon les tests actuels.

**Décision décisionnaire (a) — 18/10/2026 :** re-scénariser B6 avant de conclure à un bug.

### Replay B6 (cas 6) — 18/10/2026

| Précondition hub | Valeur observée (`swiss-ready`) |
|:-----------------|:--------------------------------|
| `materialize` / `dematerialize` | **absents** (`null`) |
| Cycle (header) | `StartStage` **enabled** — bouton « Démarrer la phase » |
| Corps production aside | **aucun** — pas de région `Matchs` / `#operational-matchs` |
| Capture | `P3-B6-swiss-ready-case6-desktop-fr.png` |

**Verdict B6 :** **Conforme** — tuile Matchs masquée ; signal de cycle au header uniquement.

**Note seed :** c’est le seul scénario DevRunner documenté qui combine Ready + absence de bloc `materialize` (`StructureMaterializeAssembler` retourne `null` pour Swiss). Le **cas 6 Vitest sur format Championnat** (`materialize` absent) reste **synthétique** : un championnat Ready projette toujours un DTO `materialize` côté hub. L’ancien URL `aaa5a397` / `37660c78` reste valide pour le **cas 2** (roster partiel), pas pour B6.

---

## 5. Journal des écarts

Une entrée par écart. Capture seule insuffisante : relier scénario et commit.

| ID | Contrôle | Compétition / seed | URL | Locale / viewport | Attendu | Observé | Preuve | Qualification | Décision / ticket |
|:---|:---------|:-------------------|:----|:------------------|:--------|:--------|:-------|:--------------|:------------------|
| GAP-01 | B6 | `swiss-ready` / `515fa976` / `1aa61770` | voir §4bis replay | fr · desktop | Tuile masquée (cas 6) | **Résolu** — pas de tuile Matchs ; `Démarrer la phase` header | `P3-B6-swiss-ready-case6-desktop-fr.png` | — | Clôturé — scénario initial incorrect (cas 2) |
| GAP-02 | C2 | `9fb6a9b4` / `7dc45558` | voir tableau C2 | fr · desktop | Attribution : rang + ligne règle | **Résolu** — section « Attribution des places » : 1 Vainqueur / 2 Perdant · P1 · Match #1 ; digest confrontation ET+TAB | `P3-C2-regulation-tie-homogeneous-desktop-fr.png` | — | Clôturé 18/10 |
| GAP-03 | D6 | `6f302053` / `47785483` | voir tableau D6 | fr · **390×844** | Footband + lien règlement + séparateur | **Résolu** — scroll `main` ; tuiles Match/Classement empilées ; `GradientSeparator` + lien footband **« Règlement »** (i18n structure, pas « Ouvrir… ») | `P3-D6-regulation-demo-mobile-390-footband-fr.png` | — | Clôturé 18/10 |

**Qualification :** Bug · Contrat / décision produit · Report accepté · Environnement / bloqué.

Ne pas corriger sur impression visuelle seule : bug prouvé, décision acceptée, ou déblocage technique convenu.

---

## 6. Table de parité

| Surface / cas | Réf. auto | Preuve navigateur | Scénario | URL / locale | Capture | Verdict |
|:--------------|:----------|:------------------|:---------|:-------------|:--------|:--------|
| Draft / Prepare | Draft + phase3 + StructurePage | Header ; pas tuile Matchs | Flux Qual Forme Champ Draft | `69e6946c`…`4e7e7450` | fr | Conforme |
| Ready / Start | Lifecycle block + StructurePage | Label, tooltip, état réel | Championnat Ready | `8c6c0bd6`…`2da84d69` | fr | Conforme |
| Running / Complete, Suspend | Lifecycle + strip | Actions cycle + header | Qual Running | `9b3a006f`…`424f5859` | fr | Conforme |
| Suspended / Resume | Strip + matchsSectionUi | Seed ctrl-o3-mat ; fiche complète | CTRL-O3 S3 | `2c4ae578`…`4a09d808` | fr | Conforme |
| Completed / Voir les matchs | structureMatchsSectionUi | Lien toolbar + destination | Championnat généré | `d81b6474`…`045f6dd3` | fr | Conforme |
| Matchs masqué cas 3 | phase3 + shouldShowAsideMatchsTile | Champ Ready placement, pas coquille | Qual Ready | `1f39a744`…`0e0e8eb7` | fr | Conforme |
| Matchs masqué cas 5 | phase3 + shouldShowAsideMatchsTile | Swiss Ready sans contenu utile | Suisse Ready | `515fa976`…`1aa61770` | fr | Conforme |
| Matchs masqué cas 6 | phase3 cas 6 + `swiss-ready` | Cycle seul, pas tuile | Suisse Ready | `515fa976`…`1aa61770` | fr | Conforme |
| Matchs roster partiel (Vitest cas 2) | phase3 cas 2 | Tuile + message roster | Qual partial | `aaa5a397`…`37660c78` | fr | Conforme |
| Matchs production utile | phase3 + OperationalStrip | Corps visible ; pas CTA cycle aside | Qual Running | `9b3a006f`…`424f5859` | fr | Conforme |
| Flux rempli | StructurePage (partiel) | Sorties sans chips interdits | Flux Qualif Draft | `0a6fca25`…`090f16e4` | fr | Conforme |
| Flux vide | À confirmer auto | EmptyState Sorties compact | Flux Qual Forme Champ Draft | `69e6946c`…`4e7e7450` | fr | Conforme |
| Digest règlement | RegulationStrip | Présentation, liaison, faits conditionnels | Multi-fiches | — | fr | Conforme |
| Digest viewport étroit | Responsive auto à confirmer | Reflow + séparateur | Démo Règlement · Groupes · 390px | `6f302053`…`47785483` | fr | Conforme |
| Schematic | Pas de test dédié listé | Pas de cadre pointillés | Observé desktop | — | fr | Conforme |
| Intégration fiche | StructurePage | Cohérence cross-surface réelle | Parcours seeds | — | fr | Conforme |

Template inventaire : [actions-permissions-inventory](./actions-permissions-inventory.md#phase-3-parity-table-template).

---

## 7. Conditions de clôture

**Règle :** les suites auto vertes **ne suffisent pas** à clôturer si un écart navigateur ou une preuve manquante subsiste.

1. **C2** — rejoué après chargement complet ; compétition, URL, locale, capture consignées.
2. **D6** — rejoué en viewport mobile ; reflow sections, hiérarchie icônes/valeurs, séparateur footband vérifiés.
3. **B6 / GAP-01** — arbitrage explicite **avant** tout correctif (re-seed cas 6, revue libellé B6, ou bug prouvé sur seed cas 6 validé).
4. **Typecheck / E2E** — hors périmètre de clôture fonctionnelle B6 ; PR technique séparée.

| Champ | Valeur |
|:------|:-------|
| Verdict | **Phase 3 clôturée** |
| Écarts bloquants | Aucun (fonctionnel navigateur) |
| Reports acceptés | C4 ; libellé footband D6 « Règlement » vs recette « Ouvrir le règlement » ; cas 6 **Championnat** non seedable DevRunner — suivi seed QA optionnel |
| Décision de clôture | Recette navigateur + replay auto (106/106 Vitest, 3/3 .NET @ `8873a491`) ; pas de changement produit |
| Date / validateur | 2026-10-18 · **Stéphane ANDRE** (sign-off clôture) |

---

## Ordre d’exécution conseillé

1. Environnement + seed Suspended.
2. A puis B (Draft → Suspended / Completed).
3. C et D (états remplis, vides, modes liaison).
4. E ; parité + journal.
5. Clôture seulement après qualification de tous les écarts.

Recette exécutée le 2026-10-18 ; statuts A–E + B-SUS consignés dans ce document. Réouverture uniquement si nouveau périmètre produit ou régression qualifiée.
