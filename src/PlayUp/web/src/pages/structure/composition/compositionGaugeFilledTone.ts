export function compositionGaugeFilledTone(
  current: number,
  capacity: number,
): 'complete' | 'brand' | 'over' | 'neutral' {
  if (capacity <= 0) {
    return 'neutral';
  }

  if (current >= capacity) {
    return current > capacity ? 'over' : 'complete';
  }

  return 'brand';
}
