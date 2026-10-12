# Organizer problems — pilot alias registry (D1)

**Status:** Pilot trio (2026-10-12). **O1 DECIDED:** layered model (Option B). **O2 DECIDED:** Attention + remediation hint (Option A).

| Canon (VERIFY unless stated) | Semantic level | blocksAction | Transport / alias | Equivalence |
| :--------------------------- | :------------- | :----------- | :---------------- | :---------- |
| `OpenMatches` | Gate | `CompleteStage` | Lifecycle `disabledReasonCode`; POST `Application.StageCompletionBlocked` + `reasons[]` | **same rule** (V1) |
| `CycleDetected` | Gate | `PrepareStage` | **Canon:** hub `structureIssues[]`. **Lifecycle transport:** `StructureGraphInvalid` (aggregate disabled). **POST transport:** `Application.StructureCycleDetected` | **O1 DECIDED (layered):** one prepare gate fact; three wire layers — do not merge strings in SPA |
| `QualificationPending` | Attention | *(empty — normative)* | Attention `source`; **`suggestedActionCode` `ApplyQualification`** (remediation, not blocksAction) | **O2 DECIDED (Option A):** intervention expected; no lifecycle-style gate list |

**Wire (O3 DECIDED):** Optional `problem` on lifecycle transition and attention item; `structureIssueSemantics[]` on structure hub. Shape: `canonCode`, `semanticLevel`, `blockedActions`, optional `transportCode`, optional `suggestedActionCode`. Resolver populates pilot trio only today.
