# G-OPS3 — Arbitrage progression : impact structurel vs tirage Applied

| Field | Value |
|-------|--------|
| **Backlog ID** | G-OPS3 (reliquat qual/prog après **SLICE-3**) |
| **Statut** | **Arbitrage métier validé** (DM 2026-10-16) — [SLICE-4](./slice-4-g-ops3-prog-impact-enf.md) **Closed** (garde progression livré P5–P6) · qualification : gate Applied SLICE-3 |
| **Date rédaction** | 2026-10-16 |
| **Validation DM** | 2026-10-16 — A-3 · A-4 · A-3-R1 / TECH-1 **Closed** (WC-1) |
| **Commande cible (slice 1)** | `ReplaceStageProgressionRules` uniquement |
| **§12 G-OPS3** | Reste **Partial** (reliquats draw-rules / composition hors SLICE-4) |

**Contexte livré :** [slice-3](./slice-3-g-ops3-qual-prog-enf.md) (gate Applied **qualification**) · [slice-4](./slice-4-g-ops3-prog-impact-enf.md) (`StructureProgressionImpactGate` sur progression). *Historique SLICE-3 :* blocage qual **et** prog avant assouplissement progression.

**Gouvernance :** [g-ops3-arbitration-session.md](./g-ops3-arbitration-session.md) (composition / R-Draw1) · [g-ops-ready-operational-mutations-and-d1.md](./g-ops-ready-operational-mutations-and-d1.md) (G-OPS2 / D1) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §12.

**Process :** audit A-3…A-6 **clos** ([annexe](#annexe--résultats-audit-2026-10-16)). Impl garde [SLICE-4 §7](./slice-4-g-ops3-prog-impact-enf.md) **Closed** (P5 garde + P6 nettoyage).

**Principe transversal :** l’absence de provenance persistée **n’autorise pas** implicitement ; la présence de production aval **ne bloque pas** un changement sans lien avec le chemin touché.

---

## Décision de gouvernance (DM 2026-10-16)

| Arbitrage | Statut |
|-----------|--------|
| **A-3** — population / rejeu comparatif | **Validé** · [A-3-R1](#a-3-r1--réserve-technique-attribution-du-rejeu) **Closed** (WC-1, DM 2026-10-16) |
| **A-4** — témoin 2c production aval | **Validé** (slice 1, scope diff P-4, lien chemin–production, ambiguïté → refus) |

**Enregistrement :** slice `SLICE-4-G-OPS3-PROG-IMPACT-ENF` en [§11C.4](./slice-4-g-ops3-prog-impact-enf.md) — **Closed** (DM 2026-10-16).

---

## Problème produit

Un organisateur configure une **sortie progression** depuis une phase amont dont le **tirage est déjà appliqué**, vers une phase aval dont le **tableau / la composition sont encore vides**.

- **Attendu métier :** la mutation ne remet pas en cause les placements figés par le tirage sur l’amont ; elle ne fait que déclarer un flux futur vers l’aval.
- **Comportement actuel :** refus **409** `Application.AppliedDrawFeedRulesChangeBlocked` dès que `before ≠ after` sur les règles de progression, **sans** analyse d’impact.

**Erreur de modèle :** utiliser la **présence** d’un tirage `Applied` sur l’amont comme proxy unique du danger. Le risque réel est l’**impact sur la structure déjà consommée** (aval et, le cas échéant, intégrité du tirage sur la même phase).

---

## Décision produit (cadre — validé DM 2026-10-16)

1. **Critère de refus progression** — Refuser seulement lorsqu’un **témoin d’impact** démontre une **perte de satisfaction structurelle** ou un contrat aval rompu — pas parce qu’un tirage est Applied sur l’amont.
2. **Filet par défaut** — Tant que les témoins ne sont pas implémentés et testés : garder SLICE-3 sur **progression** ; **qualification** reste sous SLICE-3 **sans** assouplissement.
3. **Séparation** — Ne pas affaiblir **G-B-3**, **B1**, **D2**, **G-OPS2**.
4. **Ambiguïté** — Preuve d’impact insuffisante ou ambiguë → **refus** (ne pas supposer l’absence de consommation).

---

## Décisions métier validées (A-3 / A-4)

### A-3 — Preuve de consommation **population** (et règle d’impact) — **validé**

**Décision DM :**

- **Jamais** traiter la seule présence d’entrées dans `CompositionEntries` comme preuve suffisante qu’un chemin **retiré ou modifié** est consommé.
- **Trois situations :**
  1. **Form / slot** — provenance identifiable → refuser si le chemin `before` retiré/modifié est **référencé et consommé** (2a-form / 2a-slot ; voir W-OPS3-2).
  2. **Population** (pas de provenance persistée) → **rejeu comparatif** via mécanismes existants [`InboundFeedSeatResolver`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/InboundFeedSeatResolver.cs) / [`DestinationSatisfaction`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DestinationSatisfaction.cs) sur l’état métier **courant**, de façon **déterministe**.
  3. **Preuve impossible ou ambiguë** → **refus** (`Application.ProgressionFeedConsumed` ou filet SLICE-3 jusqu’à slice).

**Seuil d’impact (population et rejeu) :** ne pas bloquer parce que le chemin **contribuerait** positivement à une satisfaction future ; bloquer seulement si la mutation ferait **perdre une satisfaction actuellement acquise** pour ce chemin `before` — c’est-à-dire : satisfaction **Satisfied** (ou équivalent métier) sous règles **before**, qui ne le serait plus sous règles **after** (ou chemin absent de `after`), pour la même entrée / le même siège inbound.

**Outcome déjà appliqué :** lorsqu’un fixture amont lié au `SourcePairKey` est **décidé** et que le rejeu établit que le chemin `before` alimentait la destination → traiter comme consommé (aligné **B2** / matrice ci-dessous).

#### A-3-R1 — Réserve technique (attribution du rejeu)

**Condition avant implémentation :** le rejeu comparatif doit opposer la satisfaction **before** vs **after** à partir d’un **état de référence cohérent** (même snapshot métier : stages, matchs, composition, résolutions form, slots).

Il **ne doit pas** attribuer à un chemin `before` retiré/modifié une satisfaction acquise **via un autre chemin** ou un autre mécanisme d’alimentation.

**Livrable de clôture A-3-R1 :** note technique courte (ou section tests) démontrant que [`InboundFeedSeatResolver`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/InboundFeedSeatResolver.cs) / [`DestinationSatisfaction`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DestinationSatisfaction.cs) — ou un extracteur dédié du garde — permet cette attribution **par chemin** de façon fiable ; sinon **réouverture** du détail A-3 (sans remettre en cause le principe validé) avant toute ligne de code produit.

**Clôture (DM 2026-10-16) :** [g-ops3-a3-r1-technical-note.md](./g-ops3-a3-r1-technical-note.md) — **WC-1** validé ; [slice-4](./slice-4-g-ops3-prog-impact-enf.md) **Closed** (garde livré).

### A-4 — Production aval et témoin **2c** — **validé**

**Décision DM :**

- **2c en slice 1**, destinations = chemins **retirés ou modifiés** (diff P-4) uniquement.
- **Extension additive pure (famille A)** : **non** bloquée par une production aval sans rapport avec les chemins existants.
- **Refus 2c** lorsque, pour un chemin touché :
  1. la destination est dans le diff ;
  2. des matchs sont attachés sur cette destination ;
  3. le **lien** entre le chemin touché et cette production est **établi** ;
  4. sinon → **refus prudent** (ambiguïté).

Les matchs attachés seuls **ne suffisent pas** (audit A-4) — la condition (3) est obligatoire.

---

## Matrice de décision (validée DM 2026-10-16)

| Situation | Décision |
|-----------|----------|
| Extension **A** vers destination vierge (aucun témoin) | **Autoriser** |
| Population aval sans provenance persistée | Rejeu comparatif before/after ; **refus** si **perte** de satisfaction acquise démontrée |
| Form ou slot alimenté par le chemin modifié | **Refuser** |
| Outcome déjà appliqué sur le chemin modifié (fixture décidé + rejeu) | **Refuser** |
| Matchs attachés sur la **destination du chemin touché** | **Refuser** si le chemin touché **participe** à cette production (2c + lien, pas production seule) |
| Preuve d’impact insuffisante ou ambiguë | **Refus** (défaut prudent) |

---

## Arbitrages — état

| ID | Sujet | État |
|----|--------|------|
| **P-4** | Diff `ProgressionPath` | **Confirmé** (audit A-1) — voir [§ P-4](#p-4--diff-des-chemins-préalable-technique) |
| **A-3** | Population / impact | **Validé** · A-3-R1 **Closed** (WC-1) |
| **A-4** | 2c production aval | **Validé** |
| **P-1** | `Application.ProgressionFeedConsumed` | **Confirmé** faisable (audit A-5) |
| **P-2** | Qualification | Hors slice ; SLICE-3 inchangé |

---

### P-4 — Diff des chemins (préalable technique)

**Règle :** le diff et le no-op doivent s’appuyer sur le **modèle persisté** `ProgressionRules.Paths`, pas sur une projection approximative des intents API seuls.

**Constats dépôt (à confirmer en audit formel) :**

- `ProgressionRules.Equals` compare **uniquement** la séquence ordonnée de `ProgressionPath` ([`ProgressionRules.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Rules/Progression/ProgressionRules.cs)) — les intents ne participent pas à l’égalité réglementaire utilisée par le gate SLICE-3.
- Identité d’un chemin atomique = record `ProgressionPath` : `SourcePairKey` (normalisé), `Outcome`, `ProgressionDestination` (stage, slot/group/form/population).
- Invariant domaine : au plus **un** chemin par couple `(SourcePairKey, Outcome)` — un changement de destination pour la même source est un **remplacement** (famille **B**), pas un ajout (famille **A**).

**Algorithme de diff cible (proposition) :**

1. Calculer `afterPaths` comme aujourd’hui la commande ( `FromIntents` / liste de paths ) **avant** toute persistance.
2. Indexer `before` et `after` par clé **`(SourcePairKey, Outcome)`** (identité source métier).
3. **Retirés** : clés présentes dans `before`, absentes dans `after`, ou présentes dans les deux avec `ProgressionPath` non égal (destination ou discriminant destination modifié).
4. **Ajoutés** : clés dans `after` absentes de `before`.
5. **Famille A** : ensemble des clés source inchangées (même `ProgressionPath` pour chaque `(SourcePairKey, Outcome)` présent dans les deux) **et** uniquement des **ajouts** de nouvelles clés source.
6. **Famille C** : tout retrait ou réduction de volume promis (y compris clear `after == null`).

**Hors scope P-4 :** comparer les intents pour le no-op métier — le no-op wire reste `Equals(beforeRules, afterRules)` sur `ProgressionRules` tel que persisté (paths).

---

### W-OPS3-2 — Progression consommée (critères signables)

**Périmètre :** chemins **retirés ou modifiés** uniquement (diff P-4). Extensions **A** indépendantes : pas de blocage par consommation d’un **autre** lien.

**Code wire (P-1) :** `Application.ProgressionFeedConsumed` → Host **409**, i18n `problems.lifecycle.ProgressionFeedConsumed`, `lifecycleMutationMessageKey.ts`.

#### 2a — Slot (et place Cup)

- WhoFeeds **Unique** `FeedKind.Progression` avec `ProgressionFeedRef` = chemin `before` ([`SlotFeedMatchesProgression`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/InboundFeedSeatResolver.cs)).
- Slot occupé **et** ref concordant → consommé ; occupation seule → **non**.
- Refuser si satisfaction acquise pour ce siège serait **perdue** par la mutation (rejeu before/after sur le slot cible).

#### 2b — Form

- `FormPathResolutions` + empreinte [`FormPathResolutionKey.FromProgression`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/FormPathResolutionKey.cs) du chemin `before`.
- Refuser si entrée encore liée à l’empreinte `before` et que la mutation rompt cette satisfaction ([`DestinationSatisfaction.EvaluateProgression`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DestinationSatisfaction.cs)).

#### 2b-pop — Population (sans provenance persistée)

- **Pas** de refus sur `CompositionEntries.Count` seul.
- Rejeu **comparatif** déterministe : pour chaque chemin `before` touché, évaluer satisfaction sous règles **before** vs **after** (même entrées / fixture amont décidé si applicable via `ProjectProgressionFromSourceFixture`).
- Refuser seulement si **perte** d’une satisfaction **Satisfied** actuelle attribuable à ce chemin.
- Fixture amont **non décidé** ou rejeu **ambigu** → **refus** (défaut prudent).

#### 2c — Production aval (slice 1)

Voir [A-4](#a-4--production-aval-et-témoin-2c) : destination du chemin touché + `HasAttachedMatches` + participation du chemin à la production (pas extension A sans chemin existant modifié).

---

## Familles de modification (contrat produit)

Comparaison **sans persister** : `before = stage.Regulation.ProgressionRules`, `after` = règles proposées matérialisées en **paths** (cf. P-4).

| ID | Famille | Description | Politique cible |
|----|---------|-------------|-----------------|
| **A** | **Extension** | Nouvelles clés `(SourcePairKey, Outcome)` uniquement ; chemins existants inchangés (égalité `ProgressionPath`) | **Autoriser** si aucun témoin d’impact |
| **B** | **Remplacement** | Au moins une clé source existante avec `ProgressionPath` différent (dont changement de destination) | **Refuser** si chemin **before** consommé (W-OPS3-2) |
| **C** | **Retrait / réduction** | Suppression de clés, clear, baisse volume promis | **Refuser** si W-OPS3-2 et/ou **G-OPS2** |

**No-op :** `Equals(before, after)` sur `ProgressionRules` → **autoriser** (inchangé).

**Vigilance :** destination Ready avec **composition 0** ne prouve pas l’absence d’impact (slots, population post-`ApplyProgressionOutcome`, production aval).

---

## Témoins d’impact

### W-OPS3-1 — Contrat consommateur Ready (G-OPS2, existant)

**Garde :** [`StructureReadyConsumerFeedGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureReadyConsumerFeedGate.cs).

**Règle :** inchangée — volume promis progression vs `CompositionEntries.Count` sur consommateur **Ready** avec entrées placées.

**Code wire :** `Application.ReadyConsumerWouldBreak`.

**Tests :** [`ReplaceStageProgressionRulesGOpsTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageProgressionRulesGOpsTests.cs).

---

### W-OPS3-3 — Intégrité tirage Applied sur l’amont

Une progression **purement extension A** ne modifie pas les placements comparés par `DrawAppliedState`. **Ne pas** refuser uniquement pour Applied une fois W-OPS3-1/2 en place.

---

## Garde-fous — responsabilités et ordre d’exécution

### Responsabilités

| Garde | ID | Rôle | Slice 1 |
|-------|-----|------|---------|
| `StructureOperationalExitEditGuard` | D2 | Matchs attachés **amont** | Inchangé |
| `StructureReadyConsumerFeedGate` | G-OPS2 | Consommateur Ready | Inchangé (W-OPS3-1) |
| `StructureProgressionImpactGate` | W-OPS3-2 | WC-1 + WC-2 / **2c** | **Livré** (SLICE-4) |
| `StructureAppliedDrawFeedRulesChangeGate` | SLICE-3 | Blocage Applied **qualification** | **Actif** (progression : SLICE-4) |
| G-B-3 / B1 | — | Pool / placement manuel | Hors scope |

### Ordre **réel** aujourd’hui (ne pas inverser sans décision explicite)

Dans [`ReplaceStageProgressionRules.EnsureAndApply`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageProgressionRules/ReplaceStageProgressionRules.cs) :

1. **No-op** — `Equals` règles (`StructureProgressionImpactGate` + use case)  
2. **D2** — `StructureOperationalExitEditGuard`  
3. **G-OPS2** — `StructureReadyConsumerFeedGate` (si `competitionStages` fourni)  
4. **W-OPS3-2** — `StructureProgressionImpactGate` (WC-1 + WC-2)  
5. Persistance — `ReplaceProgressionRulesOperationally`

**Livré SLICE-4 :** proxy SLICE-3 **progression** retiré ; qualification inchangée.

---

## Cas de référence — matrice de tests

| ID | Scénario | Attendu |
|----|----------|---------|
| **R-Prog-OPS3-A1** | Amont Applied, aval Draft vide, **ajout** premier chemin | **Allow** |
| **R-Prog-OPS3-A2** | Amont Applied, aval **Ready**, composition **0**, ajout chemin | **Allow** — **obligatoire** : prouver G-OPS2 neutre **et** absence de déclenchement W-OPS3-2 / autres gardes structurels |
| **R-Prog-OPS3-C1** | Réduction chemins ; aval Ready, 2 entrées | **Refuse** `ReadyConsumerWouldBreak` |
| **R-Prog-OPS3-C2** | Clear ; aval Ready peuplé | **Refuse** G-OPS2 |
| **R-Prog-OPS3-B1** | Aval : slot occupé **avec** WhoFeeds progression = chemin `before` ; modifier chemin | **Refuse** `ProgressionFeedConsumed` |
| **R-Prog-OPS3-B1neg** | Slot occupé mais feed **non** progression (ex. direct/draw) ; extension indépendante | **Allow** (si audit OK) |
| **R-Prog-OPS3-B2** | Après `ApplyProgressionOutcome` population ; retirer chemin correspondant | **Refuse** W-OPS3-2 (si preuve 2b validée) |
| **R-Prog-OPS3-D2** | Amont matchs attachés ; ajout progression | **Refuse** `StructureNotMutable` (**avant** autres codes) |
| **R-Prog-OPS3-POOL** | Shrink affectation amont Applied | **Refuse** `AppliedDrawPoolChangeBlocked` |
| **R-Prog-OPS3-NOOP** | Règles identiques, Applied | **Allow** |
| **R-Prog-OPS3-2c** | *(si requis post-audit P-3)* | Matchs matérialisés aval liés au chemin modifié → refus |

**Preuve Host :** A1 ou A2 **200** + B1 ou C1 **409** avec code attendu.

---

## Audit dépôt (prérequis signature)

| # | Question | Statut post-audit | Référence |
|---|----------|-------------------|-----------|
| **A-1** | P-4 : diff / `Equals` sur paths | **Confirmé** | [Annexe § A-1](#a-1--p-4-diff-chemins) |
| **A-2** | W-OPS3-2a slot | **Confirmé** | [Annexe § A-2](#a-2--w-ops3-2a-slot) |
| **A-3** | W-OPS3-2b population | **Validé DM** · A-3-R1 **Closed** | [§ A-3](#a-3--preuve-de-consommation-population-et-règle-dimpact--validé) · [note WC-1](./g-ops3-a3-r1-technical-note.md) |
| **A-4** | P-3 production aval / 2c | **Validé DM** | [§ A-4](#a-4--production-aval-et-témoin-2c--validé) · [Annexe](#a-4--production-aval) |
| **A-5** | P-1 wire API | **Confirmé** (checklist livrable) | [Annexe § A-5](#a-5--contrat-api-progressionfeedconsumed) |
| **A-6** | Cas A2 | **Confirmé** (G-OPS2) · test **manquant** | [Annexe § A-6](#a-6--cas-a2-ready-vide) |
| **A-7** | Ordre D2 → G-OPS2 | **Confirmé** | [Annexe § A-7](#a-7--ordre-des-garde-fous) |

**Garde-fou audit :** aucun code modifié ; gate SLICE-3 inchangé ; §12 G-OPS3 non promu **Done**.

---

## Annexe — Résultats audit (2026-10-16)

Audit ciblé dépôt MyClub — lecture et traces d’exécution uniquement.

### A-3 — Population après progression

**Fichiers / symboles inspectés**

| Symbole | Fichier |
|---------|---------|
| `ApplyProgressionOutcome.Execute` | [`ApplyProgressionOutcome.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ApplyProgressionOutcome/ApplyProgressionOutcome.cs) |
| `AddResolvedPopulationEntry`, `RecordFormPathResolution`, `ApplyResolvedEntry` | [`Stage.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) |
| `CompositionEntry` | [`CompositionEntry.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/CompositionEntry.cs) |
| `FormPathResolutionKey.FromProgression` | [`FormPathResolutionKey.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/FormPathResolutionKey.cs) |
| `InboundFeedSeatResolver.ResolveProgressionEntry`, `ProjectProgressionFromSourceFixture` | [`InboundFeedSeatResolver.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/InboundFeedSeatResolver.cs) |
| `DestinationSatisfaction.EvaluateProgression` | [`DestinationSatisfaction.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DestinationSatisfaction.cs) |
| `ReplaceProgressionRulesOperationally` / `ApplyProgressionRulesReplacement` | [`Stage.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) |

**Constats vérifiés**

1. **Population (`TargetsPopulation`)** — `ApplyProgressionOutcome` appelle `AddResolvedPopulationEntry` uniquement ; `CompositionEntry` ne stocke que `EntryId` (**aucune provenance feed persistée**).
2. **Form (`TargetsForm`)** — `RecordFormPathResolution(FormPathResolutionKey.FromProgression(source, path), entryId)` ; empreinte stable `P|{sourceStageN}|{pairKey}|{Outcome}` ; persistée (`StageConfiguration` owns `FormPathResolutions`).
3. **Slot** — dual-write population + `ApplyResolvedEntry` ; preuve slot = occupant + WhoFeeds (hors scope A-3 strict, couvert A-2).
4. **`ReplaceProgressionRules` ne supprime pas** les `FormPathResolutions` existantes sur la destination — les empreintes « anciennes » peuvent subsister après mutation des règles amont (**pas** une preuve fiable de consommation actuelle sans réévaluer avec le chemin **before**).
5. **Rejeu population (lecture)** — `InboundFeedSeatResolver.ProjectProgressionFromSourceFixture` + `ProgressionApplier.Apply` + `DestinationSatisfaction` permettent de tester, **au moment du garde**, si une entrée en composition correspond au résultat d’un fixture **terminé** amont pour le chemin **before** (même logique que le hub Composition).

**Hypothèses (non vérifiées comme suffisantes seules)**

- « Toute entrée en composition population après un `ApplyProgressionOutcome` » sans rejeu fixture → **faux positif** si entrée ajoutée par qualif, direct, draw aval, ou `AddResolvedPopulationEntry` hors progression.
- « `FormPathResolutions` orphelines » → ne doivent **pas** déclencher W-OPS3-2 sans concordance `before` path + `EntryId` encore en composition / satisfaction.

**Tests existants / manquants**

| Existant | Manquant |
|----------|----------|
| [`MaterializeCupFromOccupiedSlotsTests.Composition_progression_then_materialize_from_slots`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/MaterializeCupFromOccupiedSlotsTests.cs) (population + slots) | Garde W-OPS3-2 population sur chemin retiré après `ApplyProgressionOutcome` |
| [`ApplyProgressionOutcomeTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ApplyProgressionOutcomeTests.cs) | Cas form : `FormPathResolution` + replace progression amont |
| [`QualificationForFormDestinationTests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/QualificationForFormDestinationTests.cs) (qual form, analogue) | **R-Prog-OPS3-B2** |

**Conséquence W-OPS3-2 / slice (si DM valide A-3)**

| Destination kind | Signal slice 1 |
|------------------|----------------|
| **Slot** | **2a** — ref progression + occupation ; perte de satisfaction |
| **Form** | **2b** — `FormPathResolutions` + empreinte `before` |
| **Population** | **2b-pop** — rejeu comparatif before/after ; **perte** de satisfaction acquise ; ambiguïté → refus |

**Statut :** métier **validé DM** ; garde SLICE-4 **Closed** ([§ A-3-R1](#a-3-r1--réserve-technique-attribution-du-rejeu)).

---

### A-4 — Production aval

**Fichiers / symboles inspectés**

| Symbole | Fichier |
|---------|---------|
| `StructureOperationalExitEditGuard.Ensure` | [`StructureOperationalExitEditGuard.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureOperationalExitEditGuard.cs) |
| `StageAttachedProduction.HasAttachedMatches` | [`StageAttachedProduction.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StageAttachedProduction.cs) |
| `ReplaceStageProgressionRules.EnsureAndApply` | [`ReplaceStageProgressionRules.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageProgressionRules/ReplaceStageProgressionRules.cs) |
| `UseCaseExecutor.ReplaceStageProgressionRulesAsync` | [`UseCaseExecutor.Stages.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Pipeline/UseCaseExecutor.Stages.cs) (`ListByStageForUpdateAsync` **amont uniquement**) |
| `DematerializeCup` / `DematerializeChampionshipOrGroups` | [`DematerializeCup.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/Dematerialization/DematerializeCup.cs) (démat amont possible) |

**Constats vérifiés**

1. **D2 ne porte que sur l’amont** muté : `upstreamMatches` = matchs de la phase source ; **aucune** lecture des matchs de la phase destination dans `ReplaceStageProgressionRules`.
2. **Scénario nominal** — après `ApplyProgressionOutcome`, l’amont a typiquement des matchs attachés au fixture → `ReplaceProgressionRules_refuses_when_ready_upstream_has_attached_matches_D2` ; une **extension** sur amont déjà en production est déjà bloquée par D2.
3. **Asymétrie** — dématérialisation **amont** (ex. `DematerializeCup`) peut retirer les matchs amont tout en laissant composition / production **aval** ; D2 ne voit plus de matchs amont → `ReplaceStageProgressionRules` peut à nouveau passer (hors SLICE-3) **sans** garde aval.
4. **Cas A2** (aval Ready, composition 0, pas de production) — production aval absente → **2c non pertinent**.

**Hypothèses**

- « D2 suffit pour protéger la progression consommée » → **infirmé** pour l’aval et pour l’amont dématérialisé.

**Tests existants / manquants**

| Existant | Manquant |
|----------|----------|
| D2 amont : `ReplaceProgressionRules_refuses_when_ready_upstream_has_attached_matches_D2` | Amont dématérialisé + aval avec matchs + modification chemin progression |
| | **R-Prog-OPS3-2c** (définition selon arbitrage ci-dessous) |

**Conséquence W-OPS3-2 / slice (si DM valide A-4)**

- **2c en slice 1** — scope = destinations des chemins **retirés/modifiés** uniquement.
- Matchs attachés sur destination **nécessaires mais pas suffisants** : preuve de **participation** du chemin touché requise en implémentation (sinon ambiguïté → refus).
- Extension **A** : production aval existante sur une **autre** relation de flux ne bloque pas.

**Statut :** métier **validé DM** ([§ A-4](#a-4--production-aval-et-témoin-2c--validé)).

---

### A-5 — Contrat API `Application.ProgressionFeedConsumed`

**Fichiers / symboles inspectés**

| Couche | Fichier |
|--------|---------|
| Constante | [`ApplicationErrorCodes.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Common/ApplicationErrorCodes.cs) |
| HTTP 409 | [`PlayUpExceptionHandler.cs`](../../../src/PlayUp/MyClub.PlayUp.Host/PlayUpExceptionHandler.cs) (`AppliedDrawFeedRulesChangeBlocked` → 409) |
| SPA map | [`lifecycleMutationMessageKey.ts`](../../../src/PlayUp/web/src/pages/structure/problems/lifecycleMutationMessageKey.ts) |
| Copy | [`structure.json` (en/fr)](../../../src/PlayUp/web/src/i18n/locales/en/structure.json) `problems.lifecycle.*` |
| Inventaire impact | [`structure-diagnostic-presentation-impact-rules.md`](./structure-diagnostic-presentation-impact-rules.md) §5.1 |
| Footer mutation | [`formatStructureMutationMessage.ts`](../../../src/PlayUp/web/src/pages/structure/problems/formatStructureMutationMessage.ts) (payload spécial : `ReadyConsumerWouldBreak` + `consumerStageIds`) |
| Preuve Host | [`ReplaceStageQualificationRulesGOps3EndpointTests.cs`](../../../tests/MyClub.PlayUp.Host.Tests/ReplaceStageQualificationRulesGOps3EndpointTests.cs) |

**Constats vérifiés**

1. Convention codes lifecycle mutation : `Application.{Name}` dans `ApplicationErrorCodes`, mapping **409** explicite dans `PlayUpExceptionHandler` pour les refus structure récents (pool, feed rules, slot placement).
2. SPA : codes structure mutation passent par `MUTATION_WIRE_TO_LIFECYCLE` ou `problems.mutation.*` ; les codes G-OPS3 récents sont mappés en `problems.lifecycle.*` (pas `errors.Application` — même pattern que `AppliedDrawFeedRulesChangeBlocked`).
3. **`ProgressionFeedConsumed` n’existe pas encore** — ajout sans conflit de nom ; pas de consommateur frontend aujourd’hui.
4. Pas d’obligation de payload `Reasons` pour ce code (contrairement à `ReadyConsumerWouldBreak`) sauf décision produit ultérieure.

**Checklist slice (livrable implémentation, non exécutée)**

- [ ] `ApplicationErrorCodes.ProgressionFeedConsumed`
- [ ] `PlayUpExceptionHandler` → 409
- [ ] `lifecycleMutationMessageKey.ts`
- [ ] `structure.json` EN/FR `problems.lifecycle.ProgressionFeedConsumed`
- [ ] Mise à jour [`structure-diagnostic-presentation-impact-rules.md`](./structure-diagnostic-presentation-impact-rules.md)
- [ ] Test Host 409 (mirroring qual G-OPS3)
- [ ] `structureProblems.test.ts` (optionnel, parité `AppliedDrawFeedRulesChangeBlocked`)

**Statut :** **Confirmé** — le contrat actuel **peut accueillir** `Application.ProgressionFeedConsumed` (orientation P-1 maintenue).

---

### A-6 — Cas A2 (Ready vide, extension)

**Fichiers / symboles inspectés**

| Étape | Garde | Fichier |
|-------|-------|---------|
| 1 | D2 | `StructureOperationalExitEditGuard` |
| 2 | G-OPS2 | `StructureReadyConsumerFeedGate.CollectBrokenReadyConsumers` |
| 3 | SLICE-3 | `StructureAppliedDrawFeedRulesChangeGate` |
| 4 | Domaine | `EnsureOperationalExitRulesMutable` |
| Executor | Pas de `EnsureCompetitionAllowsLifecycleMutation` sur `ReplaceStageProgressionRulesAsync` | `UseCaseExecutor.Stages.cs` L615–647 |

**Constats vérifiés**

1. **G-OPS2 neutre si `placedCount == 0`** — `CollectBrokenReadyConsumers` : consommateur Ready avec `CompositionEntries.Count == 0` → `continue` (pas de refus), y compris quand le volume promis augmente (extension).
2. **Prepare / materialize** — non invoqués sur `ReplaceStageProgressionRules` (pas de gate Prepare sur cette commande).
3. **Domaine** — `ReplaceProgressionRulesOperationally` exige amont `Draft` ou `Ready` uniquement (`EnsureOperationalExitRulesMutable`).
4. **Blocage actuel cas A2 + Applied draw** — uniquement **étape 3** SLICE-3 (pas G-OPS2).
5. **`ReplaceStageProgressionRules_allows_narrowing_when_downstream_draft`** — prouve extension avec aval **Draft**, **sans** tirage Applied.

**Tests existants / manquants**

| Réutilisable | Manquant |
|--------------|----------|
| G-OPS2 refuse : `ReplaceProgressionRules_refuses_when_ready_consumer_has_more_entries_than_inbound_volume` | **R-Prog-OPS3-A2** : amont Applied (draw), aval Ready composition 0, ajout chemin |
| D2 : test dédié amont matchs | Host HTTP pour A2 post-slice |

**Statut :** **Confirmé** (chemin d’exécution + G-OPS2) · **test A2 manquant** à exiger avant clôture slice (pas bloquant signature arbitrage si accepté comme critère slice).

---

### A-7 — Ordre des garde-fous

**Constat vérifié** — ordre dans `EnsureAndApply` : **D2 → G-OPS2 → SLICE-3 → persist**. Test D2 (`StructureNotMutable`) ne passe pas par G-OPS2.

**Statut :** **Confirmé** — la slice doit insérer W-OPS3-2 **entre** G-OPS2 et persist, **sans** permuter D2 et G-OPS2.

---

### A-1 — (P-4) Diff chemins

**Statut :** **Confirmé** — `ProgressionRules.Equals` = égalité des `Paths` ordonnés ; invariant un chemin par `(SourcePairKey, Outcome)` ; diff métier = clés retirées / ajoutées / `ProgressionPath` modifié.

---

### A-2 — (W-OPS3-2a) Slot

**Statut :** **Confirmé** — pattern production [`SlotFeedMatchesProgression`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/InboundFeedSeatResolver.cs) + `ProgressionFeedRef` ; aligné fiche (occupation seule insuffisante).

---

### Synthèse — conséquences signature / slice

| Sujet | État |
|-------|------|
| **P-4** | **Confirmé** (audit) |
| **A-3** | **Validé** · **A-3-R1 Closed** (WC-1) |
| **A-4** | **Validé** — 2c slice 1 |
| **P-1** | **Confirmé** faisable |
| **A6 / A2** | G-OPS2 neutre **confirmé** ; **R-Prog-OPS3-A2** requis à la slice |
| **SLICE-3** | Actif jusqu’à garde impact progression livré ; **qual** inchangé |

---

## Suite (état au 2026-10-16)

| Étape | Statut |
|-------|--------|
| 1. Arbitrage métier A-3 / A-4 | **Fait** (cette fiche) |
| 2. Fiche slice §11C.4 | **Préparée** — [slice-4-g-ops3-prog-impact-enf.md](./slice-4-g-ops3-prog-impact-enf.md) |
| 3. Clôture **A-3-R1** / **TECH-1** (WC-1) | **Fait** (DM 2026-10-16) |
| 4. Impl garde (WC-1 + WC-2 / 2c) | **Done** — [slice-4 §7](./slice-4-g-ops3-prog-impact-enf.md) |
| 5. Livraison : tests R-Prog-OPS3-*, Host ; retrait SLICE-3 **progression** ; P6 nettoyage ; **qual** inchangé | **Done** (DM 2026-10-16) |

**Hors slice :** promotion §12 G-OPS3 **Done** global (reliquats draw-rules, etc.).

---

## Périmètre slice implémentation (contrat SLICE-4)

| In scope | Out of scope |
|----------|----------------|
| `ReplaceStageProgressionRules` + W-OPS3-2 (2a, 2b, 2b-pop, **2c**) | `ReplaceStageQualificationRules` |
| Tests `R-Prog-OPS3-*` | G-B-3, B1, assouplissement D2 |
| `Application.ProgressionFeedConsumed` + i18n | Promotion §12 G-OPS3 → **Done** |
| Retrait gate Applied **progression seulement** | Qual sous SLICE-3 |

**Slice ID :** `SLICE-4-G-OPS3-PROG-IMPACT-ENF` — voir [fiche slice](./slice-4-g-ops3-prog-impact-enf.md).

---

## Critères de clôture

### Arbitrage métier

1. **Fait** — A-3 / A-4 + matrice ([§ validées](#décisions-métier-validées-a-3--a-4)).  
2. Audit **A-1..A-7** clos ; W-OPS3-2 (§ 2a–2c) aligné sur décisions DM.  
3. **A-3-R1** / **TECH-1** **Closed** (WC-1) — première PR garde autorisée ; WC-2 dans le même livrable garde.  
4. Notion **Décision** (optionnel) — bloc [ci-dessous](#notion--bloc-décision-dm-2026-10-16).

### Slice **livrée**

1. Garde impact en place ; SLICE-3 désactivé pour progression uniquement.  
2. Tests Release + preuve Host minimale.  
3. Qual : inchangée (SLICE-3).

---

## Références code

| Artefact | Chemin |
|----------|--------|
| Gate Applied (actuel) | [`StructureAppliedDrawFeedRulesChangeGate.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawFeedRulesChangeGate.cs) |
| Commande | [`ReplaceStageProgressionRules.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageProgressionRules/ReplaceStageProgressionRules.cs) |
| Paths / égalité | [`ProgressionRules.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Rules/Progression/ProgressionRules.cs), [`ProgressionPath.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Rules/Progression/ProgressionPath.cs) |
| G-OPS2 | [`StructureReadyConsumerFeedGate.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureReadyConsumerFeedGate.cs) |
| Apply progression | [`ApplyProgressionOutcome.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ApplyProgressionOutcome/ApplyProgressionOutcome.cs) |

---

## Notion — bloc Décision (DM 2026-10-16)

**Titre :** G-OPS3 — Progression : refus par impact structurel (chemins consommés)

**Décision :** Valider A-3 (rejeu comparatif population, perte de satisfaction acquise, ambiguïté → refus, réserve A-3-R1 attribution par chemin) et A-4 (2c slice 1, scope diff, lien chemin–production). Remplacer le proxy tirage Applied sur `ReplaceStageProgressionRules` par diff `ProgressionPath` et témoins W-OPS3-1/2. Code `Application.ProgressionFeedConsumed`. SLICE-3 maintenu sur qualification ; progression assouplie seulement après garde SLICE-4 livré.

**Motif :** Impact structurel réel, pas proxy Applied ; pas d’autorisation implicite ; extension A non bloquée par production aval non liée.

**Hors périmètre :** qualification (SLICE-3), G-B-3, B1.

**Liens Git SoT :** [g-ops3-progression-impact-arbitration.md](./g-ops3-progression-impact-arbitration.md) · [slice-4-g-ops3-prog-impact-enf.md](./slice-4-g-ops3-prog-impact-enf.md)
