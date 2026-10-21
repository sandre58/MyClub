import i18n from './index';
import { attentionSourceLabel } from './enumLabels';

/**
 * Situation / attention copy from wire `source` (+ optional params).
 * Prefer this over deprecated API `reason` prose.
 */
export function situationTitle(
  source: string,
  params?: Record<string, string | number | undefined>,
): string {
  const fromEnums = attentionSourceLabel(source);
  if (fromEnums !== source) {
    return fromEnums;
  }

  return i18n.t(`attentionSource.${source}`, {
    ns: 'enums',
    defaultValue: source,
    ...params,
  });
}

/**
 * Optional secondary line under the title for attention echoes.
 * Returns null when there is no echo-safe description — long déficit
 * narration stays on the Teams SoT, not Overview / AttentionDrawer (anti-triple).
 */
export function situationDescription(
  _source: string,
  _params?: Record<string, string | number | undefined>,
): string | null {
  return null;
}
