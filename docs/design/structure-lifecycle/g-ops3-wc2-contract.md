# SLICE-4 — Contrat WC-2 (2c / production aval)

| Field | Value |
|-------|--------|
| **Inventaire P3** | [g-ops3-wc2-production-link-inventory.md](./g-ops3-wc2-production-link-inventory.md) |
| **Périmètre** | Chemins `Touched` uniquement ; branché au garde depuis **P5 Done** (DM 2026-10-16) |

## Matrice DM

| Situation | Verdict WC-2 |
|-----------|----------------|
| Production aval + chemin touché **participant** démontré | `Participating` |
| Matchs aval, **aucun** lien démontré avec le chemin touché | `NotParticipating` (ne pas bloquer comme consommation) |
| Extension additive, aucun chemin existant touché | N/A (`Touched` vide) |
| Attribution impossible ou contradictoire | `ParticipationAmbiguous` |

## Politique d’ambiguïté

- `ParticipationAmbiguous` ≠ `NotConsumed` (WC-1) : défaut de données / rejeu impossible **avec production présente** → refus prudent **sans** prétendre avoir prouvé la consommation.
- Au garde (P5) : code d’erreur et message à distinguer WC-1 `ConsumedAmbiguous` vs WC-2 `ParticipationAmbiguous` si le produit expose deux causes (sinon regrouper sous refus prudent documenté).

## Preuve form / slot (réserve DM — levée P4 ; garde P5)

| Cas DM | Règle WC-2 | Test |
|--------|------------|------|
| Form consommé, fingerprint **aligné** rejeu + entrée sur production | `FormPathResolution` + `expectedEntry` rejeu + `EntryAppearsOnAttachedProduction` | `Wc2_form_resolution_aligned_with_replay_and_attached_match_is_participating` |
| Fingerprint présent mais **≠** rejeu | `NotParticipating` | `Wc2_form_resolution_not_aligned_with_replay_is_not_participating` |
| Production sans résolution form | `ParticipationAmbiguous` | `Wc2_form_without_resolution_with_attached_production_is_ambiguous` |
| Slot feed progression **Unique** + rejeu + entrée sur match attaché | `Participating` | `Wc2_slot_explicit_feed_and_entry_on_attached_match_is_participating` |
| Feed OK mais entrée absente de la production | `NotParticipating` | `Wc2_slot_feed_matches_but_entry_not_on_attached_production_is_not_participating` |
| `MultipleFeeds` sur le slot | `ParticipationAmbiguous` | `Wc2_slot_multiple_feeds_with_attached_production_is_ambiguous` |
| Population (déjà couvert) | idem inventaire | tests `Wc2_progressed_entry_*`, `Wc2_attached_production_*` |

Un fingerprint ou un slot occupé **seul** ne suffit pas : rejeu + cohérence feed (slot) + présence sur match attaché (population / form / slot).

## Tests P4 (caractérisation)

Voir tableau ci-dessus + `ProgressionPathProductionParticipationWitnessTests` (11 tests Release).

## Conditions d’acceptation P5 (DM)

1. Ordre garde : D2 → G-OPS2 → impact progression (WC-1 + WC-2).
2. G-OPS2 inchangé sur réduction flux Ready peuplé.
3. Refus remplacement/suppression si consommation WC-1 prouvée.
4. Refus prudent distinct : `FeedConsumedAmbiguous` vs `DownstreamProductionAmbiguous` (`ProgressionImpactWitnessComposition`).
5. Extensions pures (draw Applied amont) autorisées si `Touched` vide / pas de témoin.
6. Host : ≥1 HTTP 200 allow + ≥1 HTTP 409 avec code/message vérifiés.

**Statut :** **Done** (DM 2026-10-16) — preuves dans [slice-4 §7](./slice-4-g-ops3-prog-impact-enf.md#preuves-p5-dm-2026-10-16) (Host 204 A1, 409 `ProgressionFeedConsumed` B2, 409 `ProgressionDownstreamProductionBlocked` 2c).

**P6** : **Done** — suppression `StructureAppliedDrawFeedRulesChangeGate.EnsureProgressionReplaceAllowed` ; qualification inchangée.
