import type { StructureStageHubSummary } from '../types';

/**
 * Sorties destinations = later peers in Structure order only
 * (`Competition.StageIds` / `data.stages` index).
 *
 * - Self excluded (intra-stage Place is form, not Sorties).
 * - Earlier stages excluded (index ≤ source).
 * - Inter-stage only; destination must be downstream.
 */
export function sortiesAvalPeerStages(
  stages: readonly StructureStageHubSummary[],
  sourceStageId: string,
): StructureStageHubSummary[] {
  const sourceIndex = stages.findIndex((s) => s.stageId === sourceStageId);
  if (sourceIndex < 0) {
    return [];
  }
  return stages.filter((_, index) => index > sourceIndex);
}
