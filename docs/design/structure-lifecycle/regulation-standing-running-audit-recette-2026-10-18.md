# Recette — Règlement classement vs phase En cours (lot D)

**Status:** **Closed** (2026-10-18). UI audit — **no domain change**.

**SoT métier:** [phase-mutability-matrix.md](./phase-mutability-matrix.md) §9 — `ReplaceStageStandingRules` denied in **Ready** (`Stage.StandingRulesLocked`); allowed **Running+** on classifying stages. [c-20-standing-rules-ready-audit.md](./c-20-standing-rules-ready-audit.md).

---

## Scenarios

| Id | Phase status | Phase type | Action | Expected | Audit (2026-10-18) |
|----|--------------|------------|--------|----------|---------------------|
| **D-A** | Draft | Championship | Edit standing rules (points / criteria) | Allowed | **OK** — hub `ReplaceStandingRules` in `BuildDraftStageActions`; Structure fiche strip when `canEditStanding`. |
| **D-B** | Ready | Championship | Same | **Blocked** — `StandingRulesLocked` / UI disabled | **OK** — `BuildReadyStageActions` omits `ReplaceStandingRules`; `StructureView_exposes_operational_actions_when_stage_is_Ready_without_production`; strip test hides standing pencil when `canEditStanding: false`. |
| **D-C** | Running | Championship | Same | **Allowed** — save succeeds; standings read refresh | **OK** — `BuildExploitationStageActions` + `StructureView_exposes_rename_and_standing_when_stage_is_Running_classifying`; Host `ReplaceStandingRulesEndpointTests`. |
| **D-D** | Running | Cup (non-classifying) | Edit standing | Blocked or tile hidden per product rules | **OK** — `hasStandingRules === false` → no standing column on phase tile (`RegulationPage` test Finale); no standing strip on Structure when non-classifying. |
| **D-E** | Completed | Championship | Edit standing | Blocked | **OK** — `BuildStageActions` → `[]` for Completed; domain `ReplaceStandingRules_on_Completed_is_rejected`. |

---

## UI surfaces checked

| Surface | Role | Result |
|---------|------|--------|
| `RegulationPage.tsx` | Read hub + competition-level `ReplaceRegulation` only | **OK** — phase tiles display standing tokens; **no** per-phase standing mutation entry point. |
| Structure fiche `StructurePhaseRegulationStrip` | Standing edit gated on `ReplaceStandingRules` | **OK** — `StructurePhaseRegulationStrip.test.tsx` (lot D). |
| Host `PUT` standing rules | HTTP 409 + `Stage.StandingRulesLocked` on Ready | **OK** — `ReplaceStandingRulesEndpointTests`. |

---

## Gaps

None — no Notion task required for lot D.
