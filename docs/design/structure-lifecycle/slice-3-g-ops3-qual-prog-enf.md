# SLICE-3 — G-OPS3-QUAL-PROG-ENF

| Field | Value |
|-------|--------|
| **Status** | **Authorized** (DM arbitration 2026-10-16) — **not Closed** per slice protocol; shipped `472546bb` |
| **Slice ID** | `SLICE-3-G-OPS3-QUAL-PROG-ENF` |
| **Allowed backlog** | Reliquat **G-OPS3** — `ReplaceStageQualificationRules` / `ReplaceStageProgressionRules` only |
| **Contract** | Refuse **change** to qual/prog rules when a non-cancelled draw is **Applied** on the stage (`DrawAppliedState`); **no-op** replace when rules equal |

---

## Périmètre

| In scope | Out of scope |
|----------|----------------|
| `StructureAppliedDrawFeedRulesChangeGate` on qual/prog replace | `ReplaceStageAffectationAuthoring` (**G-B-3** / SLICE-1) |
| Wire `Application.AppliedDrawFeedRulesChangeBlocked` + i18n mutation | **C-ENF-*** |
| Test `ReplaceStageQualificationRulesGOps3Tests` | Promotion **G-OPS3** §12 → **Done** |
| | `ReplaceStageDrawRules`, autres commandes composition |

**§12 G-OPS3** reste **Partial** après livraison de ce slice (draw-rules et invalidation hors set `EntryId` non couverts).

---

## Preuve (commit `472546bb`)

- [`StructureAppliedDrawFeedRulesChangeGate`](../../../src/PlayUp/MyClub.PlayUp.Application/Stages/StructureAppliedDrawFeedRulesChangeGate.cs)
- [`ReplaceStageQualificationRulesGOps3Tests`](../../../tests/MyClub.PlayUp.Application.Tests/Stages/ReplaceStageQualificationRulesGOps3Tests.cs)
- [`ReplaceStageQualificationRulesGOps3EndpointTests`](../../../tests/MyClub.PlayUp.Host.Tests/ReplaceStageQualificationRulesGOps3EndpointTests.cs) (HTTP 409)
