# Composition Équipes — résolution vs éligibilité (arbitration)

**Status:** Product principle **signed** (2026-10-18). **UI (2026-10-18):** no separate **Éligibles** counter — withdrawn/forfeit teams stay in **Entrées** / composition count and list chrome only; they must **not** drive PopulationShortfall or a second gauge.

**Scope:** Structure fiche — bande / tuile **Équipes** et en-tête **Qualifiés**. Hors scope : Règlement, Équipes workspace, forfait match sans retrait.

**Related:** [composition-tile-lifecycle-presentation-2026-10-16.md](./composition-tile-lifecycle-presentation-2026-10-16.md) · [truth-sources](../structure-phase-refonte/truth-sources.md) · [composition-teams-withdrawal-presentation-recette-2026-10-18.md](./composition-teams-withdrawal-presentation-recette-2026-10-18.md).

---

## 1. Problem (historical)

A single ratio **7/7** could mean « seats resolved » while an entry was **Withdrawn** — misleading if read as « seven teams can still play ».

## 2. Decision (validated)

1. **Withdrawal UX** — icon + tooltip on the team row (recette linked); not a dedicated eligibility ratio.
2. **Population / shortfall** — based on **occupancy** (`compositionEntryCount`, Places N, inbound configured volume), **not** Active-only counts. A forfait compétition still occupies its phase entry slot for meter purposes.
3. **Qualifiés** eyebrow — **Résolues** vs `configuredVolume` only.
4. **Équipes** footer — one counter: **Entrées** (prep) or **Résolues** (exploitation/historical when band stats apply).

## 3. Hub (Git SoT)

| Field | Role |
|--------|------|
| `compositionEntryCount` | Pool size (includes withdrawn in composition) |
| `compositionIneligibleCount` | Row chrome / diagnostics only |
| `compositionBandResolvedCount` | Résolues gauge (inbound or roster, capped at Places N) |
| `inboundComposition.resolvedCount` / `configuredVolume` | Qualifiés eyebrow |

Removed: `compositionBandEligibleCount`, `compositionBandShowEligibleGauge`, `compositionActiveCount`, `compositionEligibleCount`, `inboundComposition.eligibleResolvedCount`.

## 4. Recette (acceptance)

| Id | Setup | Expected |
|----|--------|----------|
| **T-ELIG-1** | 7 composition entries, 1 `Withdrawn` | List shows forfait; meter **7/7 Entrées** or **7/7 Résolues** — **no** Éligibles counter; **no** PopulationShortfall solely due to withdraw |
| **T-ELIG-2** | All Active, 6/7 inbound resolved | Qualifiés **6/2** or **6/7** per inbound vs configured — not conflated with Active count |
| **T-ELIG-3** | Match forfeit only, entry Active | No withdrawal row chrome |

---

## 5. Notion

Optional Décision row: « Structure Équipes — pas de compteur Éligibles » → link this file.
