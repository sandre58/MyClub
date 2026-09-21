import type { StructureStageHubSummary } from '../types';

/**
 * Sorties destinations = peers **aval** in Structure order only
 * (Competition.StageIds / `data.stages` index).
 *
 * - Self excluded (intra-Stage Place = forme, not Sorties).
 * - Amont excluded (index ≤ source).
 *
 * @see Notion: Sorties = inter-Stage only ; destination aval
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
