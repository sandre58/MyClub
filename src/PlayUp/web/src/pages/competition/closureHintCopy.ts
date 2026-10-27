import i18n from '../../i18n';
import type { OverviewClosureHint } from '../../types';

/**
 * Closure hint is always on the Overview wire.
 * Silence when Host has neither a normal-completion signal nor blocker codes.
 */
export function closureHintHasContent(hint: OverviewClosureHint): boolean {
  return (
    hint.canCompleteNormally ||
    hint.blockerCodes.some((code) => code.trim().length > 0)
  );
}

/**
 * Reuse existing completion / attention vocabulary.
 * Unknown codes stay as the wire code — no second diagnostic catalogue.
 */
export function closureBlockerLabel(code: string): string {
  const completionKey = `completionBlocker.${code}`;
  if (i18n.exists(completionKey, { ns: 'enums' })) {
    return i18n.t(completionKey, { ns: 'enums' });
  }

  const attentionKey = `attentionSource.${code}`;
  if (i18n.exists(attentionKey, { ns: 'enums' })) {
    return i18n.t(attentionKey, { ns: 'enums' });
  }

  return code;
}
