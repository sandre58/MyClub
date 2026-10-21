import type { StructureView } from '../../types';
import { stageHasInboundQualProgTo } from './structureStageQualProgProjection';

/** True when any qual/prog edge targets `destinationStageId` (analyzer HasInboundFeed). */
export function stageHasInboundQualProgFeed(
  data: StructureView,
  destinationStageId: string,
): boolean {
  return stageHasInboundQualProgTo(data.stages, destinationStageId);
}

export function isEntryBaselineStage(
  data: StructureView,
  stageId: string,
): boolean {
  const primary = data.format.primaryStageId;
  if (primary) {
    return primary === stageId;
  }
  return data.stages[0]?.stageId === stageId;
}
