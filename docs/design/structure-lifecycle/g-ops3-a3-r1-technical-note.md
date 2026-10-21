# A-3-R1 / TECH-1 — Note technique : attribution du rejeu comparatif (population)

| Field | Value |
|-------|--------|
| **Parent** | [g-ops3-progression-impact-arbitration.md](./g-ops3-progression-impact-arbitration.md) § [A-3-R1](./g-ops3-progression-impact-arbitration.md#a-3-r1--réserve-technique-attribution-du-rejeu) |
| **Slice** | [slice-4-g-ops3-prog-impact-enf.md](./slice-4-g-ops3-prog-impact-enf.md) — **TECH-1** |
| **Date** | 2026-10-16 |
| **Mode** | Analyse / caractérisation — **aucun** changement runtime ; gate SLICE-3 progression **inchangé** |

---

## 1. Objectif

Démontrer que le rejeu comparatif **before / after** peut attribuer une **perte de satisfaction** au chemin `ProgressionPath` retiré ou modifié (diff P-4), sans confondre avec une satisfaction maintenue par un **autre** chemin (autre `(SourcePairKey, Outcome)` ou autre siège form/slot).

---

## 2. Symboles examinés

| Rôle | Symbole | Fichier |
|------|---------|---------|
| Instruction pure | `ProgressionApplier.Apply` | [`ProgressionApplier.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Progression/ProgressionApplier.cs) |
| Satisfaction par chemin | `DestinationSatisfaction.EvaluateProgression` | [`DestinationSatisfaction.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/DestinationSatisfaction.cs) |
| Rejeu population (fixture décidé) | `InboundFeedSeatResolver.ResolveProgressionEntry` → `ProjectProgressionFromSourceFixture` | [`InboundFeedSeatResolver.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/InboundFeedSeatResolver.cs) |
| Placement / siège | `InboundFeedSeatResolver.IsInboundSeatPlaced` | idem |
| Modèle attention (boucle **par chemin**) | `NeedsAttentionAssembler.CollectProgressionAttentions` | [`NeedsAttentionAssembler.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/NeedsAttentionAssembler.cs) |
| Apply runtime | `ApplyProgressionOutcome.Execute` | [`ApplyProgressionOutcome.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ApplyProgressionOutcome/ApplyProgressionOutcome.cs) |
| Form persisté | `FormPathResolutionKey.FromProgression`, `Stage.FormPathResolutions` | [`FormPathResolutionKey.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/FormPathResolutionKey.cs), [`Stage.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Stages/Stage.cs) |
| Hub inbound (1 règle = 1 chemin) | `StructureInboundCompositionAssembler.ResolveSeat` | [`StructureInboundCompositionAssembler.cs`](../../../src/PlayUp/MyClub.PlayUp.Application/Reads/StructureInboundCompositionAssembler.cs) |
| Identité chemin | `ProgressionPath` + invariant `(SourcePairKey, Outcome)` unique | [`ProgressionRules.cs`](../../../src/PlayUp/MyClub.PlayUp.Domain/Rules/Progression/ProgressionRules.cs) |

**Tests de caractérisation (read-only produit) :** [`ProgressionPathConsumptionWitnessA3R1Tests.cs`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ProgressionPathConsumptionWitnessA3R1Tests.cs).

---

## 3. Vérification 1 — Reconstruction du `before`

**État de référence cohérent** (snapshot pour le garde, sans persister `after`) :

| Donnée | Source |
|--------|--------|
| Règles amont | `beforeRules = stage.Regulation.ProgressionRules` (paths materialisés) |
| Stages concours | Liste déjà chargée par `UseCaseExecutor` (`competitionStages`) |
| Matchs amont | `matches.ListByStageForUpdateAsync(sourceStageId)` |
| Projections match | `MatchSummaryRow` ou `MatchAttentionSlice` (même sémantique que `NeedsAttentionAssembler` / `InboundFeedSeatResolver`) |
| Destination | `competitionStages` — composition, slots, `FormPathResolutions` **tels qu’en base** |

**Entrée attendue pour un chemin `p` (population / fixture décidé) :**

1. `fixture = source.FindFixtureByBracketPairKey(p.SourcePairKey)` (ou résolution équivalente).
2. `outcome = FixtureOutcomeResolver.Resolve(...)` sur matchs du fixture.
3. `instruction = ProgressionApplier.Apply(p, p.SourcePairKey, outcome)`.
4. `expectedEntry = instruction.EntryId`.

**Form :** `expectedEntry` via `FormPathResolutions` + empreinte `FromProgression(source.Id, p)` (déjà utilisé dans `ResolveProgressionEntry`).

**Slot :** occupant `FindSlot(p.Destination.SlotKey)` + validation feed via `SlotFeedMatchesProgression`.

**Constat :** le produit **évalue déjà** la satisfaction **chemin par chemin** dans `CollectProgressionAttentions` (groupement par `SourcePairKey`, puis `foreach (var path in pathGroup)` + `EvaluateProgression(..., path, instruction.EntryId)`). Le garde SLICE-4 peut **réutiliser le même découpage**, pas un agrégat « destination population ».

---

## 4. Vérification 2 — Simulation du `after` (sans persistance)

1. Calculer `afterRules` comme `ReplaceStageProgressionRules` ( `FromIntents` / paths ) **sans** appeler `ReplaceProgressionRulesOperationally`.
2. Calculer le diff P-4 : ensemble `Touched` = chemins retirés ou `ProgressionPath` modifié (même clé `(SourcePairKey, Outcome)`).
3. Pour chaque `p` dans `Touched` :
   - **SatBefore** : `EvaluateProgression(dest, source.Id, p, expectedEntry)` avec règles et état actuels (équivalent `before`).
   - **SatAfter** : si `after` contient un chemin `p'` de même clé source et destination compatible, évaluer `p'` ; sinon considérer que l’obligation du chemin `p` **n’existe plus** → perte si `SatBefore == Satisfied`.

**Refus (consommation)** pour le chemin `p` :

```text
SatBefore == Satisfied
AND (p absent de after OU ProgressionPath(p) ≠ ProgressionPath dans after)
```

**Pas de persistance** : `afterRules` est un objet en mémoire ; `stage.Regulation` reste `before` jusqu’à validation complète du garde.

---

## 5. Vérification 3 — Attribution multi-chemins

### 5.1 Modèle d’identité

- Un chemin atomique = `(SourcePairKey, Outcome, ProgressionDestination)`.
- Deux chemins vers la **même population** mais paires sources différentes = **deux obligations distinctes** (deux sièges inbound dans le hub).

### 5.2 Scénarios

| ID | Scénario | Sat P1 | Sat P2 | Retrait P1 seul | Attendu garde |
|----|----------|--------|--------|-----------------|---------------|
| **M1** | P1→pop, P2→pop (paires différentes), les deux appliqués | Satisfied | Satisfied | Oui | **Refus** (P1 consommé) — P2 ne « couvre » pas P1 |
| **M2** | P1→slot A, P2→slot B même phase | Satisfied | Satisfied | Retrait P1 | **Refus** si A occupé via P1 ; **Allow** si P1 non Satisfied |
| **M3** | Extension **A** : ajout P3, P1/P2 inchangés | — | — | N/A | **Allow** si P1/P2 non dans `Touched` |
| **M4** | Form : deux empreintes différentes | Satisfied | Satisfied | Retrait d’une empreinte | **Refus** seulement pour l’empreinte dont la résolution form est Satisfied |

**Interprétation A-3-R1 :** « un autre chemin maintient la satisfaction » s’entend **par siège / chemin**, pas par « la phase aval a encore des entrées en composition ». Une autre entrée en population **ne annule pas** la consommation de P1 (voir M1). Cela évite de confondre avec G-OPS2 (volume global Ready).

**Preuve caractérisation :** test **M1** (`Multi_path_same_population_sibling_does_not_clear_consumption_of_removed_path`) ; **M2**–**M4** déduits de WC-1 + `EvaluateProgression` (slot/form) — pas de test slot dédié dans ce spike.

---

## 6. Vérification 4 — Limites et refus prudent

| État | Comportement garde proposé |
|------|---------------------------|
| Fixture amont **non décidé** | `ProjectProgressionFromSourceFixture` → `null` → **refus** si `p ∈ Touched` |
| Matchs / summaries **manquants** pour l’amont | **refus** si `p ∈ Touched` |
| Population : entrée en composition **sans** fixture décidé (saisie manuelle, qualif, draw aval) | `expectedEntry` indéterminé ou `Evaluate` Pending → **refus** si modification de `p` |
| Population : entrée présente **égale** à `expectedEntry` rejeu mais Apply jamais passé | `SatBefore == Satisfied` → **refus** (prudent, aligné « satisfaction acquise ») |
| Feed slot **Multiple** / **Invalid** | `SlotFeedMatchesProgression` false → ne pas inférer consommation depuis occupation seule ; si modification de `p` et ambiguïté → **refus** |
| Qualification + progression sur même destination | Hors slice progression ; si ambiguïté sur siège → **refus** |
| Group destination | `EvaluateProgression` / group membership — même schéma per-path |
| `FormPathResolutions` orphelines après ancienne règle | Évaluer avec empreinte du chemin **`before`** uniquement |

---

## 7. Règle de précision complémentaire (WC-1) — formalisée pour implémentation

**Witness contract `WC-1` (par chemin touché `p`) :**

1. Calculer `expectedEntry` (ou form/slot) via rejeu §3 ; si indéterminé → **ConsumedAmbiguous** → refus.
2. `satBefore = DestinationSatisfaction.EvaluateProgression(dest, sourceId, p, expectedEntry)`.
3. Si `satBefore != Satisfied` → **NotConsumed** → pas de refus pour `p`.
4. Résoudre `p'` dans `afterRules` à la même clé `(SourcePairKey, Outcome)`.
5. Si `p'` absent ou `!p'.Equals(p)` (destination ou discriminant) → **ConsumedLoss** → refus (`ProgressionFeedConsumed`).
6. Si `p'` présent et identique à `p` → pas de refus pour `p` (no-op sur ce chemin).

**Non-objectif WC-1 :** ne pas agréger « la destination a encore N entrées » ; ne pas utiliser `CompositionEntries.Count` seul.

### 7.1 Confirmations DM (clôture TECH-1, 2026-10-16)

**1. Identité du chemin (before / after)**  
- Clé source : `(SourcePairKey, Outcome)` — résolution de `p'` dans `afterRules` à cette clé.  
- **Suppression** : `p'` absent → perte possible si `SatBefore == Satisfied`.  
- **Remplacement de destination** : `p'` présent mais `!p.Equals(p')` (discriminant `ProgressionDestination` : population / form / group / slot et paramètres) → même traitement que suppression sur l’obligation `before` (WC-1 étape 5).  
- Une entrée toujours en composition **ne** prouve pas que le **même** chemin reste satisfait après changement de destination.

**2. Preuve de consommation (`SatBefore`)**  
- `expectedEntry` (form / slot / population) provient du **rejeu** amont (§3) : fixture décidé → `FixtureOutcome` → `ProgressionApplier.Apply` ; form utilise l’empreinte `FromProgression(sourceId, p)` **et** la résolution persistée **uniquement** si elle correspond à cet `expectedEntry` (`EvaluateForm` : mismatch → `Conflict`, pas `Satisfied`).  
- **Interdit** pour WC-1 : inférer consommation depuis la seule présence d’entrées en `CompositionEntries` sans rejeu du chemin `p`.  
- **`Pending`** ou rejeu **indéterminé** (fixture non décidé, summaries manquants, §6) sur un chemin **touché** → **refus prudent** (`ConsumedAmbiguous`), pas de consommation supposée.  
- Une `FormPathResolution` historique orpheline ou incohérente avec le rejeu courant **ne** compte pas comme satisfaction acquise pour bloquer une mutation sans rejeu aligné production.

**3. Pas de surinterprétation (WC-2 / 2c)**  
- WC-1 couvre **uniquement** l’attribution rejeu population / form / slot / group pour W-OPS3-2b (et équivalents siège).  
- La preuve **chemin ↔ production aval** (témoin **2c**, A-4) reste **hors WC-1** ; contrat séparé **WC-2**, obligatoire dans le garde SLICE-4 lorsque le chemin touché participe à une production aval liée.

**Validation DM :** **WC-1** validé ; **TECH-1** / **A-3-R1** clos sur ce périmètre ([slice-4](./slice-4-g-ops3-prog-impact-enf.md)). Ne clôt **pas** la slice SLICE-4 ni n’autorise le retrait du gate SLICE-3 progression.

---

## 8. Conclusion TECH-1

| Question | Réponse |
|----------|---------|
| Attribution par chemin faisable avec APIs existantes ? | **Oui** — même pattern que `NeedsAttentionAssembler` + `DestinationSatisfaction` / `InboundFeedSeatResolver` |
| Rejeu before/after sans persister ? | **Oui** — `afterRules` en mémoire + diff P-4 |
| Multi-chemins même population ? | **Oui** — obligations indépendantes ; retrait d’un chemin consommé **ne** est pas excusé par un autre chemin |
| Lacunes nécessitant refus prudent ? | **Oui** — §6 ; pas de nouveau modèle persisté requis pour slice 1 |

**Statut (DM 2026-10-16) :** **TECH-1 = Closed** — **WC-1** + tests [`ProgressionPathConsumptionWitnessA3R1Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ProgressionPathConsumptionWitnessA3R1Tests.cs) (contrat garde). Implémentation autorisée ; gate SLICE-3 progression **inchangé** jusqu’à garde de remplacement livré et testé.

**Hors TECH-1 :** **WC-2** / **2c** (arbitrage A-4) — note ou section dédiée avant de considérer la protection aval complète.

---

## 9. Gouvernance

| Artefact | Statut |
|----------|--------|
| **A-3-R1** | **Closed** (faisabilité attribution par chemin) |
| **TECH-1** | **Closed** — périmètre WC-1 uniquement |
| **SLICE-4** | **Closed** (DM 2026-10-16) — [slice-4](./slice-4-g-ops3-prog-impact-enf.md) §5 |
| **SLICE-3 progression** | **Remplacé** par garde SLICE-4 (qualification : gate Applied inchangé) |
