export type OccupancyMeterState = 'exact' | 'short' | 'over';

export function occupancyMeterState(
  current: number,
  capacity: number,
): OccupancyMeterState {
  if (current === capacity) return 'exact';
  if (current < capacity) return 'short';
  return 'over';
}

export function occupancyMeterRatio(current: number, capacity: number): number {
  if (capacity > 0) return Math.min(1, Math.max(0, current / capacity));
  return current > 0 ? 1 : 0;
}
