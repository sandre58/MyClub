import type { NeedsAttentionItem, OverviewSituation } from '../types';

export type AttentionSeverityTone = 'blocking' | 'warning';

/** Worst-case tone for chrome badge: blocking wins over warnings-only. */
export function attentionSeverityToneFromItems(
  items: readonly Pick<NeedsAttentionItem, 'severity'>[],
): AttentionSeverityTone {
  if (items.length === 0) {
    return 'warning';
  }

  const hasBlocking = items.some(
    (item) => (item.severity?.trim() || 'Blocking') === 'Blocking',
  );
  return hasBlocking ? 'blocking' : 'warning';
}

export function attentionSeverityToneFromSituations(
  items: readonly Pick<OverviewSituation, 'nature'>[],
): AttentionSeverityTone {
  if (items.length === 0) {
    return 'warning';
  }

  const hasBlocking = items.some((item) => item.nature === 'Blocking');
  return hasBlocking ? 'blocking' : 'warning';
}
