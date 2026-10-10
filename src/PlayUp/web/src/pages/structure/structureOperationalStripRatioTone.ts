import type { StripOperationalMessageKind } from './structureOperationalStripUi';

export type OperationalStripRatioTone = 'neutral' | 'partial' | 'complete';

export function ratioToneForOperationalMeter(
  filled: number,
  capacity: number,
  messageKind: StripOperationalMessageKind,
): OperationalStripRatioTone {
  if (capacity <= 0) {
    return 'neutral';
  }
  if (filled >= capacity) {
    return 'complete';
  }
  if (messageKind === 'attention' || messageKind === 'danger') {
    return 'partial';
  }
  if (filled <= 0) {
    return 'neutral';
  }
  return 'partial';
}
