import type { CockpitNavigationHint, CockpitSituation } from '../types'

/**
 * Resolve a Cockpit navigationHint / situation target to an existing SPA route.
 * Uses Host-provided matchId when present — no Fixture → Match join in React.
 */
export function cockpitTargetHref(target: {
  targetType: string | null | undefined
  targetId: string | null | undefined
  matchId?: string | null
  stageId?: string | null
  competitionId?: string | null
}): string | null {
  const { targetType, targetId, matchId, stageId, competitionId } = target

  if (matchId) {
    return `/matches/${matchId}`
  }

  if (!targetType || !targetId) {
    return null
  }

  switch (targetType) {
    case 'Stage':
      return `/stages/${targetId}`
    case 'Slot': {
      const stageFromSlot = targetId.split(':')[0]
      return stageFromSlot ? `/stages/${stageFromSlot}` : null
    }
    case 'Fixture':
      // Without resolved matchId, fall back to stage matches or competition hub.
      if (stageId) {
        return `/stages/${stageId}/matches`
      }
      return competitionId
        ? `/competitions/${competitionId}/matches`
        : null
    case 'Match':
      return `/matches/${targetId}`
    case 'Competition':
      return `/competitions/${targetId}`
    case 'Organisation':
      return `/competitions/${targetId}/organisation`
    case 'Draw':
      return stageId ? `/stages/${stageId}` : null
    default:
      return null
  }
}

export function situationHref(
  situation: CockpitSituation,
  competitionId: string,
): string | null {
  return (
    cockpitTargetHref({
      targetType: situation.targetType,
      targetId: situation.targetId,
      matchId: situation.matchId,
      competitionId,
    }) ??
    (situation.targetType === 'Fixture'
      ? `/competitions/${competitionId}/matches`
      : null)
  )
}

export function navigationHintHref(hint: CockpitNavigationHint): string | null {
  return cockpitTargetHref(hint)
}
