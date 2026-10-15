import type { StructureView } from '../../types';

/** True when any qual/prog path targets `destinationStageId` (analyzer HasInboundFeed). */
export function stageHasInboundQualProgFeed(
  data: StructureView,
  destinationStageId: string,
): boolean {
  for (const source of data.stages) {
    if (
      source.qualificationPaths?.some(
        (path) => path.destinationStageId === destinationStageId,
      )
    ) {
      return true;
    }
    if (
      source.progressionPaths?.some(
        (path) => path.destinationStageId === destinationStageId,
      )
    ) {
      return true;
    }
  }
  return false;
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
