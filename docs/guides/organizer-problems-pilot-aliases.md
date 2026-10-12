# Organizer problems — pilot alias registry (D1)

**Status:** Engineering draft aligned with V1–V5 VERIFY (2026-10-12). Product alias **DECIDED** only where noted.

| Canon (VERIFY unless stated) | Semantic level | blocksAction | Transport / alias | Equivalence |
| :--------------------------- | :------------- | :----------- | :---------------- | :---------- |
| `OpenMatches` | Gate | `CompleteStage` | Lifecycle `disabledReasonCode`; POST `Application.StageCompletionBlocked` + `reasons[]` | **same rule** (V1) |
| `CycleDetected` | Gate | `PrepareStage` | Hub `structureIssues[]`; lifecycle often `StructureGraphInvalid`; POST `Application.StructureCycleDetected` | **presentation aggregate** (V2) — canon on hub; lifecycle transport differs |
| `QualificationPending` | Attention | *(empty)* | Attention `source`; `suggestedActionCode` `ApplyQualification` | runtime attention (V3) |

Wire field: `problem` on lifecycle transition and attention item; `structureIssueSemantics[]` on structure hub (pilot codes only in resolver today).
