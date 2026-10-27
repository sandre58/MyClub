import type { StructureStageHubSummary, StructureView } from '../../../types';
import type { StructureDiagnosticSeverityContext } from './structureDiagnosticSeverity';
import {
  isEntryBaselineStage,
  stageHasInboundQualProgFeed,
} from '../intents/structureInboundQualProgFeed';

export function structureDiagnosticContextFromStage(
  data: StructureView,
  stage: StructureStageHubSummary,
): StructureDiagnosticSeverityContext {
  return {
    formatKind: stage.formatKind,
    stageStatus: stage.status,
    isEntryBaselineStage: isEntryBaselineStage(data, stage.stageId),
    hasInboundQualProgFeed: stageHasInboundQualProgFeed(data, stage.stageId),
    affectationEntryCount: stage.affectationEntryCount ?? 0,
    compositionEntryCount: stage.compositionEntryCount,
    compositionCapacity: stage.compositionCapacity,
  };
}
