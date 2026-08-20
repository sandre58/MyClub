import i18n from './index'
import { attentionSourceLabel, attentionTargetTypeLabel } from './enumLabels'

/**
 * Situation / attention copy from wire `source` (+ optional params).
 * Prefer this over deprecated API `reason` prose.
 */
export function situationTitle(
  source: string,
  params?: Record<string, string | number | undefined>,
): string {
  const fromEnums = attentionSourceLabel(source)
  if (fromEnums !== source) {
    return fromEnums
  }

  return i18n.t(`attentionSource.${source}`, {
    ns: 'enums',
    defaultValue: source,
    ...params,
  })
}

export function situationMeta(source: string, targetType?: string | null): string {
  const parts = [attentionSourceLabel(source)]
  if (targetType) {
    parts.push(attentionTargetTypeLabel(targetType))
  }
  return parts.join(' · ')
}
