import type { StructureStageHubSummary } from '../../types';

import type { CompositionTileMode } from './compositionTilePresentation';

/** Single footer gauge under the Équipes section (prep → Entrées). */
export type CompositionTeamsGaugeVariant = 'entries' | 'resolved';

export function resolveCompositionTeamsGaugeVariant(
  tileMode: CompositionTileMode,
  resolvedStats: { resolved: number; total: number } | null,
): CompositionTeamsGaugeVariant | null {
  if (tileMode === 'prep') {
    return 'entries';
  }

  if (resolvedStats != null && resolvedStats.total > 0) {
    return 'resolved';
  }

  return null;
}

export function resolveCompositionBandResolvedGaugeStats(
  stage: Pick<
    StructureStageHubSummary,
    | 'compositionBandResolvedCount'
    | 'compositionCapacity'
    | 'inboundComposition'
    | 'compositionEntryCount'
  >,
  placesN: number | null | undefined,
): { resolved: number; total: number } | null {
  const capacity = placesN ?? stage.compositionCapacity ?? null;
  if (capacity == null || capacity <= 0) {
    return null;
  }

  const resolved = resolveCompositionResolvedSeatCount(stage);
  if (resolved <= 0) {
    return null;
  }

  const cappedResolved = capacity > 0 ? Math.min(resolved, capacity) : resolved;

  return { resolved: cappedResolved, total: capacity };
}

export function resolveCompositionResolvedSeatCount(
  stage: Pick<
    StructureStageHubSummary,
    | 'compositionBandResolvedCount'
    | 'inboundComposition'
    | 'compositionEntryCount'
  >,
  inboundFallbackResolved?: number,
): number {
  if (stage.inboundComposition != null) {
    return stage.inboundComposition.resolvedCount;
  }

  if (stage.compositionBandResolvedCount != null) {
    return stage.compositionBandResolvedCount;
  }

  return inboundFallbackResolved ?? stage.compositionEntryCount ?? 0;
}
