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
 * Optional secondary line under the title (e.g. missing teams).
 * Returns null when the source has no dedicated description template.
 */
export function situationDescription(
  source: string,
  params?: Record<string, string | number | undefined>,
): string | null {
  if (source !== 'InsufficientParticipants') {
    return null;
  }

  const activeCount = Number(params?.activeCount);
  const minimumTeams = Number(params?.minimumTeams);
  if (!Number.isFinite(activeCount) || !Number.isFinite(minimumTeams)) {
    return null;
  }

  const missingFromParams = Number(params?.missingCount);
  const count = Number.isFinite(missingFromParams)
    ? Math.max(0, missingFromParams)
    : Math.max(0, minimumTeams - activeCount);

  return i18n.t('attentionDescription.InsufficientParticipants', {
    ns: 'enums',
    count,
    activeCount,
    minimumTeams,
  });
}
