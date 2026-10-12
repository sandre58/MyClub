# Organizer problems — pilot alias registry (D1)

**Status:** Pilot trio (2026-10-12). **O1 DECIDED:** layered model for cycle prepare (Option B). **O2** (qualification) still open.

| Canon (VERIFY unless stated) | Semantic level | blocksAction | Transport / alias | Equivalence |
| :--------------------------- | :------------- | :----------- | :---------------- | :---------- |
| `OpenMatches` | Gate | `CompleteStage` | Lifecycle `disabledReasonCode`; POST `Application.StageCompletionBlocked` + `reasons[]` | **same rule** (V1) |
| `CycleDetected` | Gate | `PrepareStage` | **Canon:** hub `structureIssues[]`. **Lifecycle transport:** `StructureGraphInvalid` (aggregate disabled). **POST transport:** `Application.StructureCycleDetected` | **O1 DECIDED (layered):** one prepare gate fact; three wire layers — do not merge strings in SPA |
| `QualificationPending` | Attention | *(empty)* | Attention `source`; `suggestedActionCode` `ApplyQualification` | runtime attention (V3) |

Wire field: `problem` on lifecycle transition and attention item; `structureIssueSemantics[]` on structure hub (pilot codes only in resolver today).
