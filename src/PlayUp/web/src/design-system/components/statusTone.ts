import type { StatusTone } from './Status';

/** Maps StatusBadge tone names to StatusTone. */
export function statusToneFromBadgeTone(
  tone: 'neutral' | 'info' | 'ok' | 'live' | 'done' | 'warn' | 'danger',
): StatusTone {
  switch (tone) {
    case 'ok':
      return 'success';
    case 'warn':
      return 'attention';
    case 'danger':
      return 'error';
    default:
      return tone;
  }
}
