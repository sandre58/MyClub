import type { MatchSummary, NeedsAttentionItem } from '../types'

export interface AttentionMatchRow {
  match: MatchSummary
  stageName: string
}

/**
 * Maps Host attention items to existing organizer routes.
 * Mirrors MatchHubPage.attentionHref — Fixture needs stage match lists.
 */
export function attentionItemHref(
  item: NeedsAttentionItem,
  matches: AttentionMatchRow[] = [],
): string | null {
  if (!item.targetId) {
    return null
  }

  if (item.targetType === 'Stage') {
    return `/stages/${item.targetId}`
  }

  if (item.targetType === 'Slot') {
    const stageId = item.targetId.split(':')[0]
    return stageId ? `/stages/${stageId}` : null
  }

  if (item.targetType === 'Fixture') {
    const row = matches.find(
      (candidate) => candidate.match.fixtureId === item.targetId,
    )
    return row ? `/matches/${row.match.matchId}` : null
  }

  if (item.targetType === 'Match') {
    return `/matches/${item.targetId}`
  }

  if (item.targetType === 'Competition') {
    return `/competitions/${item.targetId}`
  }

  if (item.targetType === 'Organisation') {
    return `/competitions/${item.targetId}/organisation`
  }

  return null
}

export function attentionItemContextLabel(item: NeedsAttentionItem): string {
  const parts = [item.source]
  if (item.targetType) {
    parts.push(item.targetType)
  }
  return parts.join(' · ')
}

export function attentionSeverityStateClass(severity: string): string {
  switch (severity.toLowerCase()) {
    case 'blocking':
    case 'error':
      return 'ds-state--error'
    case 'warning':
      return 'ds-state--attention'
    default:
      return 'ds-state--info'
  }
}
