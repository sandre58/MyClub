# G-MAT3 — Session d’arbitrage (périmètre fermé)

## Décision — G-MAT3 : différer le contrat de refus, signer la garantie A5

| | |
|--|--|
| **Date** | 2026-10-15 |
| **Périmètre** | G-MAT3 / materialization Cup |
| **Statut** | **Décision signée** — M2b validé ; G-MAT3 reste **Spec draft** |
| **Impact** | Moyen — contrat de materialization |

### Décision

1. **M2b** est retenu comme garantie de non-régression **A5**. Un diagnostic `NonRootUnfed` sur une phase **étrangère** au batch de materialization ne doit pas, à lui seul, bloquer la commande.
2. Le choix sémantique de **G-MAT3** est **différé**. Aucun gate supplémentaire n’est introduit à ce stade.
3. **G-MAT3** reste en **Spec draft** dans §12. Pas de promotion **Ready**, pas de nouveau code applicatif ni mapping HTTP dédiés.
4. Le contrat actuel de [`MaterializeCupFromOccupiedSlots`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs) reste inchangé : éligibilité = slots occupés ; absence de paire éligible → `MaterializationFailure`.
5. Un éventuel gate « feed slot impossible sur paire éligible » fera l’objet d’une **décision distincte** — non assimilé au diagnostic `NonRootUnfed`.

### Motif

Le diagnostic `NonRootUnfed` décrit la topologie / authoring d’une phase. L’éligibilité Cup repose sur deux `EntryId` sur les slots ; la commande **n’utilise pas** `SlotFeedResolver`. Aucun scénario positif réaliste ne démontre une paire éligible refusée pour une cause `NonRootUnfed` distincte des refus existants. Un gate maintenant renommerait un refus générique ou inventerait une sémantique sans contrat produit.

### Contrat signé — R-Mat-M2b

**Étant donné :** phase `T` avec `NonRootUnfed` ; phase `S` distincte cible ; batch Cup éligible sur `S` ; aucune dépendance d’alimentation du batch vers `T` ; pas d’autre blocage sur le batch de `S`.

**Quand :** materialization de `S`.

**Alors :** pas de refus pour le diagnostic de `T` ; comportement normal sur `S` ; le test vérifie le **résultat effectif** (pas seulement l’absence d’un code).

**Test dépôt :** [`MaterializeCupFromOccupiedSlotsRMatM2bTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeCupFromOccupiedSlotsRMatM2bTests.cs).

### Hors périmètre

Nouveau code/HTTP G-MAT3 ; modification materialize Cup / Prepare / Ch-Groups ; gate `SlotFeedResolver` ; promotion §12 **Ready** ; G-MAT4 / G-OPS3.

### Conditions de réouverture

1. Scénario positif réaliste avec garantie métier distincte des refus existants ; **ou**
2. Proposition explicite « feed slot impossible » (périmètre, prédicat, Prepare / `MaterializationFailure`, scénarios +/-).

### Vigilance

Signer M2b ne signifie pas que `NonRootUnfed` est sans conséquence partout — seulement qu’il ne constitue **pas à lui seul** un veto materialize quand il concerne une phase étrangère sans dépendance au batch courant.

### Checklist d’exécution

- [x] Test M2b avec préconditions explicites `T` / `S`
- [x] Vérification du résultat effectif de materialization sur `S`
- [x] Décision consignée dans cette fiche
- [x] G-MAT3 maintenu **Spec draft** §12 (pas **Ready**)
- [x] Aucun changement hors périmètre (pas de gate, pas de wire code)

---

## Contexte session (historique)

**Choix A (exigence produit, non satisfait pour M2a) :** G-MAT3 n’est justifié que si une confrontation Cup est **réellement éligible** avec alimentation **impossible** pour une cause **distincte** des refus existants.

---

## Ordre de traitement validé (produit)

| # | Sujet | Action |
|---|--------|--------|
| 1 | **G-MAT3** | Arbitrage prédicat + chevauchements (cette session) |
| 2 | **G-MAT4** | Rédiger spec + scénarios **avant** toute impl (session séparée) |
| 3 | **G-OPS3** | Impl différée ; contrat témoin + UX d’abord |
| 4 | **§11B** | Quand §12 est **Ready / Deferred / Spec signée** sans `Litigious` orphelin |

**Extensions G-OPS** (autres commandes §9) et **remédiation hub D1** : **Deferred** explicite — hors §11B minimal.

---

## Vocabulaire §12 (garde-fou)

| Statut | Signification |
|--------|----------------|
| **Ready** | Contrat + critères d’acceptation suffisants pour coder |
| **Deferred** | Décision de ne pas traiter maintenant ; portée assumée |
| **Spec signée** | Contrat validé ; **≠** livré |
| **Done** | Comportement + tests alignés spec |

Une ligne **Litigious** sans réserve écrite bloque **§11B**.

---

## Trois états (ne pas confondre) — A5

| État | Question | Veto materialize ? |
|------|----------|-------------------|
| **1 — Diagnostic présent** | Le hub voit `NonRootUnfed` sur une phase ? | **Non** (signal) |
| **2 — Confrontation éligible au batch** | La commande va matérialiser cette confrontation sur **ce** `stage` ? | N/A seul |
| **3 — Impossible à alimenter** | Les entrées requisent ne se résolvent pas selon le contrat du **format + commande** ? | **Oui**, si (2) vrai |

**Confrontation éligible** = définition **de la commande** (batch réel), pas l’ensemble des anomalies structure.

---

## Propositions de contrat M3-1..3 (à valider — **pas** promotion implicite §12 Ready)

### M3-1 — Code métier et HTTP

| Élément | Proposition |
|---------|-------------|
| HTTP | **409 Conflict** (mapping explicite host — ne pas déduire du nom du code) |
| Wire code candidat | `Application.NonRootUnfedAtMaterialize` |
| Payload | Phase + confrontations bloquantes ; message orienté action |
| Condition | Prouver une situation **distincte** de `MaterializationFailure`, `ConfrontationUndecidable`, attribution (G-MAT2) — sinon le code dédié ne fait que renommer un refus générique |

**Verrou décisionnel :** ne pas signer M3-1 tant que R-Mat-M2a n’est pas atteignable.

### M3-2 — Périmètre format

- Gate **v1** : [`MaterializeCupFromOccupiedSlots`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs) uniquement.
- Batch = paires **éligibles** de la commande (`IsEligible` : deux `Slot.EntryId` distincts, pas de fixture existante).
- Ch/Groups : inchangés (`IsRosterComplete` / `ResolveCompositionRoster` → spec **G-MAT4** ou autre).
- Pas de prédicat partagé multi-format sans équivalence démontrée des contrats d’éligibilité.

### M3-3 — Chevauchement et priorité

| Situation | Comportement proposé |
|-----------|---------------------|
| Aucune paire éligible | Conserver `MaterializationFailure` (« No eligible bracket pairs ») |
| Paire éligible + cause G-MAT3 **démontrée** | Code dédié M3-1 |
| `NonRootUnfed` sur phase **hors** batch de la commande | Pas de refus G-MAT3 |
| `SlotFeedMissing` (Prepare / hub) | Ne devient **pas** automatiquement un refus materialize |
| Plusieurs gates sur la même paire | Priorité **déterministe** = ordre réel d’exécution dans `EnsureMutable` puis corps de la commande |

**Ordre gates Cup aujourd’hui** (avant résolution du batch) :

1. `StageMaterializeGuard`
2. [`StructureMaterializeExecutionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureMaterializeExecutionGate.cs) (G-MAT1)
3. [`StructureMaterializeAttributionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureMaterializeAttributionGate.cs) (G-MAT2)
4. *(G-MAT3 candidat — à insérer ici si signé)*
5. `ResolveTargetPairs` / production

---

## Prédicat produit (formulation session)

> G-MAT3 refuse la materialization Cup uniquement lorsqu’au moins une confrontation appartient au **batch éligible** de la commande et qu’une **preuve métier** établit que l’alimentation requise par cette confrontation est impossible en lien avec un état **NonRootUnfed** (selon définition signée). La présence du diagnostic dans l’analyse globale ne suffit pas. L’absence de confrontation éligible conserve le refus existant.

**Résolveur population :** réutiliser un contrat existant (`SlotFeedResolver`, composition, etc.) si compatible — pas une 2ᵉ définition divergente de l’éligibilité.

---

## Ancrage dépôt (vérifié — à lire avant de fixer le wire code)

### Commandes materialize

| Commande | Batch / éligibilité | Gates application aujourd’hui |
|----------|---------------------|-------------------------------|
| [`MaterializeCupFromOccupiedSlots`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs) | `IsEligible` = deux slots occupés, entrées distinctes, pas de fixture existante | `StageMaterializeGuard`, [`StructureMaterializeExecutionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureMaterializeExecutionGate.cs) (G-MAT1), [`StructureMaterializeAttributionGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureMaterializeAttributionGate.cs) (G-MAT2) |
| [`MaterializeMatches`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeMatches/MaterializeMatches.cs) | Ch/Groups : paires déterminées + [`ChampionshipSwissPhasePlaces.IsRosterComplete`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/ChampionshipSwissPhasePlaces.cs) / `ResolveCompositionRoster` | Pas de gate `NonRootUnfed` ; Cup/Swiss refusés explicitement |

### `SlotFeedMissing` vs G-MAT3

- **Prepare** : bloque via [`StructurePreparePlacesGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructurePreparePlacesGate.cs) + analyse feed.
- **Materialize (commandes)** : **aucun** refus `SlotFeedMissing` aujourd’hui — échecs plutôt `MaterializationFailure` / roster incomplet / pas de paires éligibles.
- **Hub** : `SlotFeedMissing` (souvent blocking en UX) + scénario dev [`InvariantsFeedMissingScenario`](../../../src/PlayUp/MyClub.PlayUp.Development/Scenarios/InvariantsFeedMissingScenario.cs) coémet `SlotFeedMissing`, `PopulationShortfall`, **`NonRootUnfed`**.

**Conséquence pour M3-3 :** le chevauchement n’est pas « deux codes à materialize sur le même cas » aujourd’hui, mais **choix de sémantique** : quand Cup refuse déjà sans paires éligibles, un code G-MAT3 **apporte-t-il** une cause métier distincte de `MaterializationFailure` ? À trancher en session.

### Cup-first vs cohérence multi-format

- Sur **Cup**, une phase `NonRootUnfed` sans affectation implique souvent **slots vides** → batch vide ou `MaterializationFailure` **avant** un prédicat topology dédié.
- Sur **Ch/Groups**, l’alimentation passe par **composition de phase** (`CompositionEntries`) et `IsRosterComplete` — proche de **G-MAT4** (`SourcePopulationShortfall`) plus que de `NonRootUnfed` pur.

## Atteignabilité R-Mat-M2a — analyse dépôt (verrou principal)

| Couche | Contrat actuel |
|--------|----------------|
| Diagnostic `NonRootUnfed` | Non-root, pas de qual/prog **entrant vers la phase**, `AffectationAuthoring` vide ([`AppendNonRootUnfed`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/CompetitionFlowAnalyzer.cs)) — **ne regarde pas** `Slot.EntryId` |
| Éligibilité batch Cup | [`IsEligible`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/MaterializeCupFromOccupiedSlots/MaterializeCupFromOccupiedSlots.cs) : deux slots **occupés** (`EntryId` non null), entrées distinctes |
| Materialize Cup | **N’appelle pas** [`SlotFeedResolver`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Feed/SlotFeedResolver.cs) ; copie les `EntryId` des slots dans les matchs |
| Domaine slot | `EntryId` = résolution **seulement** — « never a feed » ([`Slot.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Slot.cs)) |
| Placement manuel | `AssignEntryToSlot` exige l’entrée dans **composition** de phase et crée souvent un `DirectAssignment` — occupation compatible materialize |

**Conséquence :** avec le modèle **actuel**, la conjonction « `NonRootUnfed` sur la phase materialisée + au moins une paire `IsEligible` + alimentation impossible **au sens du diagnostic** » est **vide ou non démontrée** : slots vides ⇒ pas éligible ⇒ `MaterializationFailure` ; slots occupés (composition / direct / tirage / placement) ⇒ materialize **réussit** sans gate topology.

**Pistes si le produit maintient le choix A :**

1. **Élargir le prédicat** (ex. slot du batch avec feed `Missing` / non résolu **et** critère métier explicite — peut chevaucher `SlotFeedMissing`, pas le code hub `NonRootUnfed` tel quel).
2. **Reporter / scinder** G-MAT3 : livrer d’abord **R-Mat-M2b** (A5) + spec signée « gate conditionnel à preuve M2a ».
3. **Ne pas fabriquer** un état de test incohérent (occupants sans population / sans feed) uniquement pour faire passer M2a.

`InvariantsFeedMissingScenario` aide **M2b**, pas M2a.

---

## R-Mat-M2 — contrats de test

### R-Mat-M2a — Refus positif (preuve d’atteignabilité **obligatoire**)

**Given :** phase Cup non root ; ≥1 paire **éligible** ; preuve d’impossibilité d’alimentation **selon contrat signé** ; cas **non** réductible à « aucune paire éligible » ni G-MAT1/G-MAT2.

**When :** `MaterializeCupFromOccupiedSlots`.

**Then :** code dédié ; phase + paire bloquante ; **aucune** fixture/match créée ; refus **≠** simple `IsEligible == false`.

**Sortie session :** fixture nommé **ou** constat « prédicat à revoir ».

### R-Mat-M2b — Diagnostic hors batch (A5)

**Given :** phase `T` avec `NonRootUnfed` ; materialize sur `S` ≠ `T` ; batch éligible sur `S` ; aucune confrontation du batch ne dépend de l’alimentation impossible de `T` ; pas d’autre blocage.

**When :** materialize `S`.

**Then :** pas de refus G-MAT3 ; comportement normal sur `S` ; vérifier **résultat effectif** (pas seulement l’absence du code).

---

## Ordre de travail (promotion §12)

1. Atteignabilité **R-Mat-M2a** (batch + résolution slots / feeds).
2. Valider M3-1 + mapping HTTP (si et seulement si 1 OK).
3. Figer priorité refus (ordre gates réel).
4. Écrire contrats M2a + M2b.
5. **Signer spec** → promouvoir §12 **Ready**.
6. Impl gate Cup + tests — **sans** Prepare / Ch / Groups.

---

## Décisions à cocher en fin de session

- [ ] **R-Mat-M2a** — atteignable **ou** prédicat révisé (documenté)
- [ ] **M3-1** — wire code + 409 (seulement si M2a atteignable)
- [ ] **M3-2** — Cup v1 confirmé
- [ ] **M3-3** — unicité + ordre gates G-MAT1 → G-MAT2 → G-MAT3
- [ ] **R-Mat-M2b** — récit + fixture (`InvariantsFeedMissingScenario` ou équivalent)
- [ ] Promotion §12 : **Spec signée** → **Ready** (impl après §11B) — **pas** si M2a non atteignable sans révision

**Annexe technique :** [g-mat3-non-root-unfed-at-materialize.md](./g-mat3-non-root-unfed-at-materialize.md)
