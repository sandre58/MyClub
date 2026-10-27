import type { StructureStageHubSummary } from '../../../types';

/**
 * Swiss generated round count for Structure UI.
 * Uses hub matchdayCount (matchdays with attached matches) — aligned with progressive GenerateNextRound.
 */
export function swissRoundCounts(stage: StructureStageHubSummary): {
  generated: number;
  planned: number | null;
} {
  return {
    generated: stage.matchdayCount ?? 0,
    planned: stage.swissRoundCount ?? null,
  };
}

export function formatSwissRoundsProgress(
  t: (key: string, options?: Record<string, unknown>) => string,
  generated: number,
  planned: number | null,
): string {
  if (planned != null && planned > 0) {
    return t('hub.topologyStatSwissProgress', { generated, planned });
  }
  return t('hub.topologyStatSwissGeneratedOnly', { count: generated });
}
