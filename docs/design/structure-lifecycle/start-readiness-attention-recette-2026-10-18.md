# Recette — Start bloqué vs drawer « Matchs non créés » (lot C)

**Status:** **Closed** (2026-10-18) — see [start-readiness-production-gap-arbitration-2026-10-18.md](./start-readiness-production-gap-arbitration-2026-10-18.md).

**Goal:** `TryGetDisabledReasonCode` / attention drawer expose the **real** Start blocker; Structure fiche stays **consistent** with À traiter (without reintroducing suppressed noise on Matchs tile).

**Related code:** `StageStartReadinessGuard.cs` · `StructurePresentedAttentionCollector.cs` · SPA `shouldShowPrimaryBlockMessage` · [structure-phase-lifecycle-ui.md](../../guides/structure-phase-lifecycle-ui.md) (Unmaterialized suppression on Matchs paragraph).

---

## Hypothesis

`HasMaterializationGapBeforeStart` returns true for **roster incomplete** (Championship) or **groups not ready**, but `TryGetDisabledReasonCode` still returns **`UnmaterializedConfrontations`** when the unified predicate is true — misleading copy when `materializedCount === determinedCount` (e.g. 28/28).

---

## Scenarios

| Id | Arrange | Inspect (API / hub) | Pass criteria |
|----|---------|---------------------|---------------|
| **C-A** | Championship **Ready**, full materialization, roster complete | `lifecycle.transition.disabledReasonCode`, Start enabled | No `UnmaterializedConfrontations` in attention; Start OK |
| **C-B** | **Ready**, 28/28 materialized, **1 composition entry Withdrawn** (roster &lt; Places N) | Same + `compositionIneligibleCount` | Blocker code ≠ « unmaterialized » **or** drawer copy names **effectif / éligibilité**; fiche surfaces blocker on **Équipes** or bandeau (not silent) |
| **C-C** | **Ready**, eligible confrontation not materialized | `materialize.materializedCount` &lt; target | `UnmaterializedConfrontations` in drawer **and** remediation path clear (Matchs production CTA); acceptable fiche suppression of **paragraph** only if ratio &lt; full |
| **C-D** | Groups **Ready**, empty group or &lt;2 per group | Groups on stage | Distinct reason from cup/championship materialization gap |

---

## Investigation checklist

1. Record `disabledReasonCode` vs internal branch (materialization count vs roster vs groups).
2. Compare `StructurePresentedAttentionCollector.CollectStartTransitionBlockers` item `Source` with fiche `stage.lifecycle.transition`.
3. If C-B confirms mislabel: spec new read codes (or evaluation order) before SPA copy changes.

---

## Done (lot C)

- Repro documented (scenario id + competition seed or DevRunner command).
- Decision recorded in Git (addendum to [start-readiness-exploitable-arbitration.md](./start-readiness-exploitable-arbitration.md) or new short arbitration) **or** closed as non-issue with evidence.
