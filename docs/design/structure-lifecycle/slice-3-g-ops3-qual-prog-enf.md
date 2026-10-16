# SLICE-3 — G-OPS3-QUAL-PROG-ENF

| Field | Value |
|-------|--------|
| **Status** | **Closed** (2026-10-16) — delivery [`472546bb`](https://github.com/sandre58/MyClub/commit/472546bb) ; governance doc `docs(structure-lifecycle): close SLICE-3-G-OPS3-QUAL-PROG-ENF with G-OPS3 partial reserve` (qual/prog Applied refuse ; réserves preuve) |
| **Slice ID** | `SLICE-3-G-OPS3-QUAL-PROG-ENF` |
| **Allowed backlog** | Reliquat **G-OPS3** — `ReplaceStageQualificationRules` / `ReplaceStageProgressionRules` only |
| **Contract** | Refuse **change** to qual/prog rules when a non-cancelled draw is **Applied** on the stage (`DrawAppliedState`); **no-op** replace when rules equal |

**Governance:** [programme-b-arbitration-2026-10-16.md](./programme-b-arbitration-2026-10-16.md) · [phase-mutability-matrix.md](./phase-mutability-matrix.md) §11C.4 · §12 **G-OPS3** (remains **Partial**).

---

## Périmètre

| In scope | Out of scope |
|----------|----------------|
| `StructureAppliedDrawFeedRulesChangeGate` on qual/prog replace | `ReplaceStageAffectationAuthoring` (**G-B-3** / SLICE-1) |
| Wire `Application.AppliedDrawFeedRulesChangeBlocked` + i18n mutation | **C-ENF-*** |
| Tests `ReplaceStageQualificationRulesGOps3Tests` + Host qual endpoint | Promotion **G-OPS3** §12 → **Done** |
| | `ReplaceStageDrawRules`, autres commandes composition, invalidation hors N2 |

**§12 G-OPS3** reste **Partial** après clôture de cette slice (draw-rules et reliquats composition non couverts).

---

## Réserves de clôture (DM 2026-10-16)

La livraison couvre le refus des modifications des règles de qualification et de progression lorsqu’un tirage appliqué rend le changement interdit. Les preuves d’exécution rapportées comprennent le test Application et le test Host de qualification.

La branche de progression ne dispose pas d’un test dédié Application/Host ; le comportement no-op lorsque les règles sont inchangées n’est pas couvert par un test dédié. Ces limites de preuve sont acceptées pour la clôture de cette slice et ne constituent pas une affirmation de couverture symétrique complète.

**G-OPS3** reste **Partial** dans §12. La clôture de SLICE-3 ne vaut pas complétude du backlog G-OPS3 et n’autorise aucune extension aux commandes draw-rules ou aux autres reliquats.

---

## Delivery proof (`472546bb`)

- Gate : [`StructureAppliedDrawFeedRulesChangeGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawFeedRulesChangeGate.cs)
- Commands : [`ReplaceStageQualificationRules`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageQualificationRules/ReplaceStageQualificationRules.cs) · [`ReplaceStageProgressionRules`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/ReplaceStageProgressionRules/ReplaceStageProgressionRules.cs)
- Wire : `Application.AppliedDrawFeedRulesChangeBlocked` · Host **409** · i18n EN/FR · `lifecycleMutationMessageKey.ts`
- Tests (Release) :
  - Application : [`ReplaceStageQualificationRulesGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageQualificationRulesGOps3Tests.cs) — **1/1**
  - Host : [`ReplaceStageQualificationRulesGOps3EndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/ReplaceStageQualificationRulesGOps3EndpointTests.cs) — **1/1**

**Not claimed in this slice:** progression-dedicated Application/Host tests; no-op path test; draw-rules or other G-OPS3 reliquat (see reserves above).

---

## Closure criteria

**Closed** when:

1. Diff contains **SLICE-3** qual/prog gate scope only. — **Done** (`472546bb`).
2. Both qual and prog commands call the shared Applied-draw gate. — **Done**.
3. Release tests cited in fiche green (qual Application + qual Host). — **Done** (1+1 Release).
4. Proof limits and §12 **G-OPS3 Partial** explicitly recorded. — **Done** (reserves above).
5. §11C.4 + index sync (governance doc commit). — **Done** (DM 2026-10-16).

**§12:** **G-OPS3** remains **Partial** — slice **Closed** ≠ backlog **Done**.

---

## Sequencing

**SLICE-3** **Closed**. Reliquat **G-OPS3** (draw-rules, autres commandes) : **arbitrage distinct** avant toute implémentation. **C-ENF** trilogy **Closed** — do not reopen without anomaly. **SLICE-2** **Closed** (SPA reserve).
