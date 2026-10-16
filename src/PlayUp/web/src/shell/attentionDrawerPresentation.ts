import type { CompetitionStageSummary, OverviewSituation } from '../types';
import {
  isStructureAttentionSource,
  structureAttentionStageStatus,
} from './structureAttentionPresentation';

/**
 * Δ-drawer-1: neutral drawer lede when every presentable row is a structure
 * diagnostic on a Suspended stage (same set as the bell count).
 */
export function shouldUseNeutralAttentionDrawerLede(
  situations: readonly OverviewSituation[],
  stages: readonly CompetitionStageSummary[],
): boolean {
  if (situations.length === 0) {
    return false;
  }

  for (const item of situations) {
    if (!isStructureAttentionSource(item.source)) {
      return false;
    }
    const status = structureAttentionStageStatus(item, stages);
    if (status !== 'Suspended') {
      return false;
    }
  }

  return true;
}

export function attentionDrawerLedeKey(
  situations: readonly OverviewSituation[],
  stages: readonly CompetitionStageSummary[],
): 'attention.lede' | 'attention.ledePaused' {
  return shouldUseNeutralAttentionDrawerLede(situations, stages)
    ? 'attention.ledePaused'
    : 'attention.lede';
}
