# SLICE-4 — P3 inventaire WC-2 (lien chemin → production aval)

| Field | Value |
|-------|--------|
| **Parent** | [A-4](./g-ops3-progression-impact-arbitration.md#a-4--production-aval-et-témoin-2c--validé) · [slice-4 §7](./slice-4-g-ops3-prog-impact-enf.md) |
| **Scope** | Chemins **touchés** (P-4 `Touched`) uniquement — pas d’extension A |

## Prérequis 2c

1. Phase **destination** = `pathBefore.Destination.StageId`.
2. **Production attachée** sur cette phase : `StageAttachedProduction.HasAttachedMatches(destination, matches)`.
3. **Participation** du chemin `before` à cette production (preuve ci-dessous) — les matchs seuls **ne suffisent pas**.

## Données persistées exploitables

| Destination kind | Lien établi (participation) | Symboles |
|------------------|----------------------------|----------|
| **Slot** | WhoFeeds **Unique** progression concordant **et** rejeu **et** entrée sur match attaché | `SlotFeedMatchesProgression`, `MaterializedParticipantSnapshots` |
| **Form** | Empreinte **et** `resolution.EntryId` = rejeu **et** entrée sur match attaché | `FormPathResolutions`, `FormPathResolutionKey` |
| **Population** | Rejeu amont → `expectedEntry` ; entrée **home ou away** d’un match **attaché** sur la destination | `ProjectProgressionEntryFromSourceFixture`, `MaterializedParticipantSnapshots`, fixtures `Stage.HasMatch` |
| **Group** | Même schéma population + appartenance au `GroupId` du chemin (si groupe vide / absent → ambiguïté) | `Stage.FindGroup`, composition |

## Verdicts WC-2 (read-only)

| Verdict | Signification |
|---------|----------------|
| **NotApplicable** | Pas de match attaché sur la destination (2c hors scope). |
| **NotParticipating** | Production présente ; preuve que le chemin **ne** participe pas (ex. rejeu OK, entrée absente des adversaires attachés ; slot feed autre règle **Unique**). |
| **Participating** | Lien démontré → refus attendu au garde (P5). |
| **ParticipationAmbiguous** | Production présente ; lien **non** démontré ni réfuté (rejeu impossible, feed non Unique, form sans résolution) — **refus prudent** ; message distinct de « consommation WC-1 prouvée ». |

## Composition WC-1 + WC-2 (revue avant P5)

| WC-1 | WC-2 | Garde (P5) |
|------|------|------------|
| `ConsumedLoss` | * | Refus consommation |
| `ConsumedAmbiguous` | * | Refus prudent (rejeu) |
| `NotConsumed` | `Participating` | Refus production |
| `NotConsumed` | `ParticipationAmbiguous` | Refus prudent (lien production) |
| `NotConsumed` | `NotApplicable` / `NotParticipating` | Pas de blocage 2c |

**Code :** `ProgressionPathProductionParticipationWitness` · contrat [g-ops3-wc2-contract.md](./g-ops3-wc2-contract.md).
