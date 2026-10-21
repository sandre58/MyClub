# AUDIT (Draft) — G-B-1 — couverture SPA et preuves associées

> **DRAFT — NON APPROUVÉ.** Document d’audit uniquement : **aucune** autorisation d’implémentation, **aucune** modification de §11C.4, **aucun** changement de statut §12. Ne pas confondre avec une slice ni avec la clôture G-OPS3 B1 ([propositions B1](./slice-g-ops3-b1-cup-slot-placement-closure-proposal.md) restent non commitées).

| Field | Value |
|-------|--------|
| **Status** | **Draft** (2026-10-16) |
| **Backlog** | **G-B-1** — §12 **Deferred** |
| **Slice liée** | **SLICE-2-G-B-1-ENF** — **Closed** (`c1a12ec4`) ; réserve SPA explicite |
| **G-OPS3** | **Partial** (inchangé par cet audit) |
| **Méthode** | Inspection dépôt Git (lecture code + tests) ; **pas** de modification applicative |

**Références :** [slice-2-g-b-1-enf.md](./slice-2-g-b-1-enf.md) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §12 **G-B-1** · [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md).

**Périmètre de l’audit :** qualifier le reliquat **SPA / preuve** derrière **G-B-1 Deferred**, sans rouvrir SLICE-2 ni étendre le contrat métier B-Q3a. Le sujet **`Ready → PUT /draw-rules`** est traité ici car il alimente la lecture « SPA partielle » et le reliquat **G-OPS3** (B-Q1b), **pas** parce qu’il fait partie du backlog G-B-1 au sens §12.

---

## Décision de programme (proposée — DM)

| Élément | Décision |
|---------|----------|
| Propositions clôture B1 | Draft, non approuvées, non commitées |
| **G-B-1** | **Deferred** |
| **SLICE-2** | **Closed** |
| **G-OPS3** | **Partial** |
| Nouveau code SPA | **Non autorisé** (tant que cet audit n’a pas conduit à un arbitrage DM explicite) |
| Nouvelle slice §11C.4 | **Non autorisée** |
| Prochaine action après cet audit | Arbitrage DM sur **Cas 2** (§3) ; puis éventuellement preuve Host optionnelle ou doc — **pas** d’implémentation par défaut |

---

## 1. `Ready` → `PUT /draw-rules` (B-Q1b · reliquat G-OPS3)

### 1.1 Faits vérifiés (dépôt)

| # | Fait | Source |
|---|------|--------|
| F1 | En **Ready**, `ReplaceStageDrawRules` est refusé côté domaine/application avec **`Stage.StructureLocked`**. | [`ReplaceStageDrawRulesTests.Replace_rejected_when_stage_is_Ready`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageDrawRulesTests.cs) |
| F2 | Preuve **Host HTTP** : `PUT /stages/{id}/draw-rules` sur phase **Ready** → **409** + code **`Stage.StructureLocked`**. | [`ReplaceStageDrawRulesEndpointTests.Put_draw_rules_when_ready_returns_409_StructureLockedAsync`](../../../tests/MyClub.PlayUp.Host.Tests/ReplaceStageDrawRulesEndpointTests.cs) (commentaire **B-Q1b**) |
| F3 | **Prévention SPA principale (fiche Structure)** : édition P1 des draw-rules (`canAuthorDrawRulesP1`) = action hub `ReplaceDrawRules` **et** `stage.status === 'Draft'` — le bandeau outil « params tirage » n’est rendu que si `canEditDraw` (même condition). | [`StructurePhaseFiche.tsx`](../../../src/PlayUp/web/src/pages/structure/StructurePhaseFiche.tsx) · [`StructurePhaseOperationalStrip.tsx`](../../../src/PlayUp/web/src/pages/structure/StructurePhaseOperationalStrip.tsx) |
| F4 | Verrou **L2** (après génération) : `areDrawRulesLockedByExecution` → dialogue params en **lecture seule** (`rulesLocked`) ; distinct du verrou **Ready** (B-Q1b). | [`drawUi.ts`](../../../src/PlayUp/web/src/pages/structure/drawUi.ts) · [`DrawRulesDialog`](../../../src/PlayUp/web/src/pages/structure/StructureRegulationDialogs.tsx) |
| F5 | Le hub peut encore exposer l’action **`ReplaceDrawRules`** en Ready (si format Cup/Groups ou draw-rules déjà présentes) — la SPA **ne s’appuie pas uniquement** sur l’absence d’action pour bloquer l’intention produit. | [`StructureViewAssembler`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureViewAssembler.cs) (`StageNeedsDrawRulesAction`) |
| F6 | Appels API SPA à `replaceStageDrawRules` : **`DrawRulesDialog`** (PUT params / activate) et **désactivation** (`clear: true`) sur la fiche. | [`StructureRegulationDialogs.tsx`](../../../src/PlayUp/web/src/pages/structure/StructureRegulationDialogs.tsx) · [`StructurePhaseFiche.tsx`](../../../src/PlayUp/web/src/pages/structure/StructurePhaseFiche.tsx) |
| F7 | Si une mutation draw-rules échoue avec `Stage.StructureLocked`, la copy peut retomber sur **`errors:Stage.StructureLocked`** (*« La structure est verrouillée. »*) via `apiErrorLabel` — **pas** de clé dédiée dans `problems.lifecycle.*`. | [`resolveStructureProblemMessage.ts`](../../../src/PlayUp/web/src/pages/structure/problems/resolveStructureProblemMessage.ts) · [`errors.json`](../../../src/PlayUp/web/src/i18n/locales/fr/errors.json) |
| F8 | Le dialogue `DrawRulesDialog` **n’a pas** de `onError` sur `saveMutation` ; la logique `demoted = stage.status === 'Ready'` suppose encore un succès HTTP en Ready (toast « déclassé ») — **incohérent** avec B-Q1b actuel. | [`StructureRegulationDialogs.tsx`](../../../src/PlayUp/web/src/pages/structure/StructureRegulationDialogs.tsx) |

**Lecture :** l’absence de test HTTP dédié **n’est pas** un signal de défaut serveur — la preuve Host **existe** (F2). Le reliquat est plutôt **documentaire**, **points d’entrée alternatifs**, et **présentation d’erreur** si un PUT est quand même tenté.

### 1.2 Points restant à vérifier (hors code — recette / DM)

| # | Point | Pourquoi |
|---|--------|----------|
| V1 | Deep link Structure `section=tirage` sur phase **Ready** ouvre-t-il `DrawRulesDialog` avec bouton **Enregistrer** actif ? | `initialEdit === 'tirage'` ouvre le dialogue **sans** re-vérifier `canAuthorDrawRulesP1` (F3 vs F8). |
| V2 | Parcours **readiness** / navigation qui cible `section: 'tirage'` en Ready. | [`structureReadinessActions.ts`](../../../src/PlayUp/web/src/pages/structure/structureReadinessActions.ts) référence la section tirage. |
| V3 | Écran **réglement** hors fiche phase : aucun autre appelant `replaceStageDrawRules` dans `src/PlayUp/web` (hors fiche + dialogue). | Confirmé par recherche symbole — pas d’autre page ; V1 reste le risque principal. |
| V4 | Désactivation draw (`clear: true`) en présence d’un tirage non annulé : UI désactive le bouton ; comportement si API forcée. | UI : `deactivateBlocked = hasNonCancelledDraw` ; footer utilise `MutationError` (meilleur chemin que F8). |

### 1.3 Décisions DM possibles (sans préjuger)

| # | Sujet | Options (non exclusives) |
|---|--------|---------------------------|
| D1 | Statut du reliquat draw-rules Ready | **A.** Documenter F2 + F3 comme couverture suffisante pour B-Q1b · **B.** Exiger recette V1–V2 avant de classer « SPA OK » · **C.** Traiter draw-rules dans un futur périmètre **G-OPS3** (pas G-B-1) |
| D2 | Message utilisateur | Garder `Stage.StructureLocked` générique · ou copy produit spécifique « params tirage interdits en phase prête » (implémentation **non** autorisée par cet audit) |
| D3 | F8 (toast `demoted` / pas d’`onError`) | Qualifier dette UX · corriger seulement si V1 confirme un parcours réel (arbitrage séparé) |

---

## 2. B-Q3a — second `CreateDraw` (cœur métier G-B-1)

### 2.1 Faits vérifiés (dépôt)

| # | Fait | Source |
|---|------|--------|
| F9 | Règle **≤ 1 tirage non annulé** : refus domaine **`Stage.NonCancelledDrawAlreadyExists`** sur second `CreateDraw`. | [`Stage.CreateDraw`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) · [`CreateDrawTests.Create_second_non_cancelled_draw_is_rejected`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/CreateDrawTests.cs) |
| F10 | **SLICE-2** a livré l’enforcement domaine/Application ; réserve SPA consignée dans la fiche slice. | [slice-2-g-b-1-enf.md](./slice-2-g-b-1-enf.md) |
| F11 | **Garde SPA** avant création : `resolveStageDrawCreateGate` → `hasActiveDraw` → `reason: 'active'` ; CTA désactivé ; **re-validation** dans `createMutation` avant `createAndGenerateDraw`. | [`drawUi.ts`](../../../src/PlayUp/web/src/pages/structure/drawUi.ts) · [`StructureDrawDialog.tsx`](../../../src/PlayUp/web/src/pages/structure/StructureDrawDialog.tsx) |
| F12 | Copy FR préventive : *« Annulez d’abord l’exécution active. »* (`createBlocked.short.active`). Présentation **`quiet`** (tooltip), pas bandeau inline. | [`structure.json`](../../../src/PlayUp/web/src/i18n/locales/fr/structure.json) · `resolveDrawCreateBlockPresentation('active')` |
| F13 | **Pas** de test Host HTTP dédié « `POST /stages/{id}/draws` deuxième fois → 409 + `NonCancelledDrawAlreadyExists` ». D’autres tests Host appellent `CreateDraw` une fois dans des scénarios plus larges. | Recherche `tests/MyClub.PlayUp.Host.Tests` ; [slice-2](./slice-2-g-b-1-enf.md) § Delivery proof |
| F14 | **Pas** d’entrée i18n `NonCancelledDrawAlreadyExists` dans le web ; **pas** de mapping dans `lifecycleMutationMessageKey.ts`. | Recherche `src/PlayUp/web` |
| F15 | En cas d’échec API sur création, le dialogue tirage affiche **`toastActionFailed`** (générique), pas `formatStructureMutationMessage`. | [`StructureDrawDialog.tsx`](../../../src/PlayUp/web/src/pages/structure/StructureDrawDialog.tsx) `createMutation.onError` |

**Lecture :** F13 documente une **lacune de preuve HTTP ciblée**, pas une preuve que le Host accepterait un second tirage. Le contrat serveur est couvert en **Domain + Application** (F9). Le reliquat G-B-1 SPA porte sur **course / refus serveur visible** (F14–F15) et éventuellement **preuve Host optionnelle** (F13).

### 2.2 Points restant à vérifier

| # | Point | Pourquoi |
|---|--------|----------|
| V5 | Parcours manuel : tirage actif → « Nouveau tirage » / CTA create — tooltip vs bandeau. | F12 : présentation `quiet`. |
| V6 | Course : création simultanée ou overview tirage périmé entre gate client et `POST /draws`. | F11 re-valide côté client mais ne remplace pas F9 serveur. |
| V7 | Hub / actions : existe-t-il une action `CreateDraw` exposée indépendamment du dialogue ? | Endpoint `POST .../draws` ; création UI semble centralisée dans `StructureDrawDialog` + `createAndGenerateDraw`. |

### 2.3 Décisions DM possibles

| # | Sujet | Options |
|---|--------|---------|
| D4 | Preuve Host B-Q3a | **A.** Considérer Domain+Application suffisants (comme SLICE-2) · **B.** Ajouter un test HTTP dédié (**hors** autorisation actuelle — nécessiterait arbitrage « preuve seule » sans slice fonctionnelle) |
| D5 | Niveau de garde SPA | **A.** Garder gate + tooltip (état actuel) · **B.** Renforcer présentation (inline) — **implémentation non autorisée** ici |

---

## 3. Arbitrage Cas 2 — message métier explicite vs documentation

### 3.1 Question

Lorsque le **serveur** refuse une action que la SPA a (partiellement) autorisée ou n’a pas empêchée — typiquement :

- **Course** sur second `CreateDraw` (`NonCancelledDrawAlreadyExists`) — F14–F15 ;
- **PUT draw-rules** en Ready via point d’entrée alternatif — F8, V1 ;

faut-il un **message métier explicite** dans la SPA, ou la **documentation de la couverture actuelle** (SLICE-2 réserve + cet audit) suffit ?

### 3.2 Faits utiles à l’arbitrage

| Scénario | Prévention SPA | Si serveur refuse | Copy métier dédiée SPA |
|----------|----------------|-------------------|-------------------------|
| Second tirage (B-Q3a) | Oui (F11–F12) | Domain refuse (F9) | **Non** (F14) ; toast générique (F15) |
| Draw-rules Ready (B-Q1b) | Oui en parcours nominal (F3) | Host 409 (F2) | Générique `StructureLocked` (F7) ; dialogue sans `onError` (F8) |
| Draw-rules après génération (L2) | Vue seule (F4) | Codes `DrawRulesReplaceBlockedAfterGenerate` / clear blocked | **Oui** dans `errors.json` (domaine) |

### 3.3 Décisions DM requises (explicitement)

| ID | Décision | Notes |
|----|----------|-------|
| **DM-G-B-1-A** | Le reliquat **G-B-1** est-il **documentaire + preuves existantes** (F9–F12, F2–F3), ou y a-t-il un **défaut UX produit** à traiter ? | Ne pas inférer un défaut serveur depuis F13. |
| **DM-G-B-1-B** | En cas d’échec `NonCancelledDrawAlreadyExists`, exiger une copy alignée B-Q3a dans la SPA ? | Impact : i18n + branche erreur create (hors scope sans « go »). |
| **DM-G-B-1-C** | Les sujets draw-rules Ready (§1) sont-ils **rattachés à G-OPS3 Partial** plutôt qu’à la clôture **G-B-1** ? | Recommandation audit : **oui** pour D1/D2 — évite de mélanger backlogs. |
| **DM-G-B-1-D** | Autoriser une future **slice** ou **correctif SPA** ? | **Non** par défaut ; nécessite §11C.4 ou tâche DM explicite. |

### 3.4 Recommandation rédactionnelle (audit, pas décision)

1. **Classer Cas 1** pour B-Q3a nominal : garde SPA + serveur alignés ; réserve = course + message + preuve Host optionnelle.
2. **Classer §1** comme **G-OPS3 / B-Q1b** : preuve Host déjà là ; vérifier V1–V2 avant tout correctif SPA.
3. **Ne pas** lancer d’implémentation tant que **DM-G-B-1-A** et **DM-G-B-1-C** ne sont pas tranchés.

---

## 4. Synthèse

| Zone | Verdict audit (Draft) | Bloquant pour « G-B-1 Done » ? |
|------|------------------------|--------------------------------|
| Contrat B-Q3a Domain/Application | **Couvert** (SLICE-2) | Non — déjà livré |
| Preuve Host B-Q3a ciblée | **Absente** — **pas** équivalent à bug serveur | DM (D4) |
| Prévention SPA second tirage | **Substantielle** (F11–F12) | Réserve SLA-2 |
| `Ready` PUT draw-rules | **Serveur OK** (F2) ; SPA nominal OK (F3) | Reliquat **G-OPS3** / recette V1 |
| Implémentation SPA | **Non autorisée** | — |

---

## 5. Historique

| Date | Événement |
|------|-----------|
| 2026-10-16 | Rédaction Draft — inspection dépôt + cadrage DM (sans commit, sans code) |

**Liens B1 (non commités) :** [closure proposal](./slice-g-ops3-b1-cup-slot-placement-closure-proposal.md) · [§11C.4 row proposal](./slice-g-ops3-b1-11c4-row-proposal.md).
