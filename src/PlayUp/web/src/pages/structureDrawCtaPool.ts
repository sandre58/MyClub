/** Ratio color = pool resolution only — never Draw readiness. */
export type DrawCtaPoolTone = 'neutral' | 'partial' | 'complete';

export function resolveDrawCtaPoolTone(
  filled: number,
  capacity: number,
): DrawCtaPoolTone {
  if (capacity <= 0 || filled <= 0) return 'neutral';
  if (filled < capacity) return 'partial';
  return 'complete';
}
