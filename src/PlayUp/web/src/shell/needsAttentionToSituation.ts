import type { NeedsAttentionItem, OverviewSituation } from '../types'

function mapAttentionAction(source: string): string | null {
  switch (source) {
    case 'ProgressionPending':
    case 'ProgressionConflict':
      return 'ApplyProgression'
    case 'QualificationPending':
    case 'QualificationConflict':
      return 'ApplyQualification'
    case 'InsufficientParticipants':
      return 'AddEntry'
    default:
      return null
  }
}

function mapAttentionImpact(source: string): string | null {
  switch (source) {
    case 'DrawNoSolution':
      return 'BlocksDraw'
    case 'ProgressionPending':
    case 'ProgressionConflict':
    case 'QualificationPending':
    case 'QualificationConflict':
      return 'BlocksProgression'
    case 'InsufficientParticipants':
      return 'BlocksConstruction'
    default:
      return null
  }
}

function buildSituationParams(item: NeedsAttentionItem): Record<string, string> {
  if (item.params && Object.keys(item.params).length > 0) {
    return { ...item.params }
  }

  if (item.targetType !== 'Slot' || !item.targetId) {
    return {}
  }

  const parts = item.targetId.split(':', 2)
  if (parts.length !== 2) {
    return {}
  }

  return {
    slotKey: parts[1]!,
    destinationStageId: parts[0]!,
  }
}

/** Maps GET /attention items to OverviewSituation rows for shared shell UI. */
export function needsAttentionItemToSituation(
  item: NeedsAttentionItem,
): OverviewSituation {
  const actionCode = mapAttentionAction(item.source)

  return {
    source: item.source,
    nature: item.severity,
    targetType: item.targetType,
    targetId: item.targetId,
    matchId: null,
    actionable: actionCode != null,
    actionCode,
    impactCode: mapAttentionImpact(item.source),
    params: buildSituationParams(item),
  }
}

export function needsAttentionItemsToSituations(
  items: NeedsAttentionItem[],
): OverviewSituation[] {
  return items.map(needsAttentionItemToSituation)
}
