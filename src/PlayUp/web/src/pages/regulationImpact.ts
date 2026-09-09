import type {
  OrganisationHeritablePartBinding,
  OrganisationRegulationSummary,
  OrganisationStageDefaultsBinding,
  OrganisationStageHubSummary,
  RankingCriterion,
  ReplaceRegulationRequest,
} from '../types'

/** Heritable Match/Standing parts — Domain grain (not scalar fields). */
export type HeritablePartKey =
  | 'matchDuration'
  | 'extraTime'
  | 'penaltyShootout'
  | 'administrativeResult'
  | 'points'
  | 'rankingCriteria'

const MATCH_PARTS: HeritablePartKey[] = [
  'matchDuration',
  'extraTime',
  'penaltyShootout',
  'administrativeResult',
]

const STANDING_PARTS: HeritablePartKey[] = ['points', 'rankingCriteria']

export function applicableHeritableParts(
  binding: OrganisationStageDefaultsBinding | undefined,
): HeritablePartKey[] {
  if (!binding) {
    return []
  }
  const parts = [...MATCH_PARTS]
  if (binding.points != null) {
    parts.push('points')
  }
  if (binding.rankingCriteria != null) {
    parts.push('rankingCriteria')
  }
  return parts
}

export function partBinding(
  binding: OrganisationStageDefaultsBinding,
  part: HeritablePartKey,
): OrganisationHeritablePartBinding | null {
  switch (part) {
    case 'matchDuration':
      return binding.matchDuration
    case 'extraTime':
      return binding.extraTime
    case 'penaltyShootout':
      return binding.penaltyShootout
    case 'administrativeResult':
      return binding.administrativeResult
    case 'points':
      return binding.points
    case 'rankingCriteria':
      return binding.rankingCriteria
  }
}

export function isPartOverridden(
  stage: OrganisationStageHubSummary,
  part: HeritablePartKey,
): boolean {
  const binding = stage.defaultsBinding
  if (!binding) {
    return false
  }
  const entry = partBinding(binding, part)
  return entry != null && !entry.isBound
}

export function isStagePersonalized(
  stage: OrganisationStageHubSummary,
): boolean {
  const binding = stage.defaultsBinding
  if (!binding) {
    return false
  }
  return applicableHeritableParts(binding).some((part) =>
    isPartOverridden(stage, part),
  )
}

export function overriddenParts(
  stage: OrganisationStageHubSummary,
): HeritablePartKey[] {
  const binding = stage.defaultsBinding
  if (!binding) {
    return []
  }
  return applicableHeritableParts(binding).filter((part) =>
    isPartOverridden(stage, part),
  )
}

export function boundParts(
  stage: OrganisationStageHubSummary,
): HeritablePartKey[] {
  const binding = stage.defaultsBinding
  if (!binding) {
    return []
  }
  return applicableHeritableParts(binding).filter((part) => {
    const entry = partBinding(binding, part)
    return entry != null && entry.isBound
  })
}

function sortedAllowedTypes(types: string[] | null | undefined): string {
  return [...(types ?? [])].sort().join(',')
}

function criteriaKey(criteria: RankingCriterion[] | null | undefined): string {
  return (criteria ?? []).join('|')
}

/** Canonical MatchDuration identity (periods + minutes + half-time). */
function matchDurationKey(source: {
  numberOfPeriods: number
  durationPerPeriod: number
  halfTimeDuration?: number | null
}): string {
  return [
    source.numberOfPeriods,
    source.durationPerPeriod,
    source.halfTimeDuration ?? 0,
  ].join(':')
}

function extraTimeKey(source: {
  hasExtraTime?: boolean
  extraTimeNumberOfPeriods?: number | null
  extraTimeDurationPerPeriod?: number | null
}): string {
  if (!source.hasExtraTime) {
    return 'off'
  }
  return `on:${source.extraTimeNumberOfPeriods ?? 0}:${source.extraTimeDurationPerPeriod ?? 0}`
}

function shootoutKey(source: {
  hasPenaltyShootout?: boolean
  penaltyInitialKicksPerTeam?: number | null
}): string {
  if (!source.hasPenaltyShootout) {
    return 'off'
  }
  return `on:${source.penaltyInitialKicksPerTeam ?? 0}`
}

function administrativeKey(source: {
  forfeitWinnerGoals?: number | null
  forfeitLoserGoals?: number | null
}): string {
  return `${source.forfeitWinnerGoals ?? 0}:${source.forfeitLoserGoals ?? 0}`
}

function pointsKey(source: {
  winPoints: number
  drawPoints: number
  lossPoints: number
}): string {
  return `${source.winPoints}:${source.drawPoints}:${source.lossPoints}`
}

/**
 * Which heritable Match/Standing sub-VOs differ between form and competition seed
 * using canonical representations (projection only — Domain remains authority).
 */
export function detectChangedHeritableParts(
  form: ReplaceRegulationRequest,
  seed: OrganisationRegulationSummary,
): HeritablePartKey[] {
  const changed: HeritablePartKey[] = []
  if (matchDurationKey(form) !== matchDurationKey(seed)) {
    changed.push('matchDuration')
  }
  if (extraTimeKey(form) !== extraTimeKey(seed)) {
    changed.push('extraTime')
  }
  if (shootoutKey(form) !== shootoutKey(seed)) {
    changed.push('penaltyShootout')
  }
  if (administrativeKey(form) !== administrativeKey(seed)) {
    changed.push('administrativeResult')
  }
  if (pointsKey(form) !== pointsKey(seed)) {
    changed.push('points')
  }
  if (criteriaKey(form.rankingCriteria) !== criteriaKey(seed.rankingCriteria)) {
    changed.push('rankingCriteria')
  }
  return changed
}

export function detectCompetitionOnlyChanges(
  form: ReplaceRegulationRequest,
  seed: OrganisationRegulationSummary,
): { entry: boolean; discipline: boolean } {
  const entry =
    form.minimumTeams !== seed.minimumTeams ||
    form.maximumTeams !== seed.maximumTeams
  const discipline =
    sortedAllowedTypes(form.allowedTypes) !==
    sortedAllowedTypes(seed.allowedTypes)
  return { entry, discipline }
}

export type PartImpactCounts = {
  inherit: number
  keepOverride: number
}

export type RegulationImpactPreview = {
  eligibleStageCount: number
  /** Stages that will receive at least one bound-part update. */
  stagesUpdatedCount: number
  byPart: Partial<Record<HeritablePartKey, PartImpactCounts>>
  competitionOnly: { entry: boolean; discipline: boolean }
  changedParts: HeritablePartKey[]
}

function isEligibleForPropagation(status: string): boolean {
  return status === 'Draft' || status === 'Ready'
}

/**
 * UI projection of ReplaceRegulation + PropagateBoundDefaults.
 * Does not implement Domain rules — anticipates from DefaultsBinding + changed parts.
 */
export function buildRegulationImpactPreview(
  form: ReplaceRegulationRequest,
  data: {
    regulation: OrganisationRegulationSummary
    stages: OrganisationStageHubSummary[]
  },
): RegulationImpactPreview {
  const changedParts = detectChangedHeritableParts(form, data.regulation)
  const competitionOnly = detectCompetitionOnlyChanges(form, data.regulation)
  const eligible = data.stages.filter((stage) =>
    isEligibleForPropagation(stage.status),
  )

  const byPart: Partial<Record<HeritablePartKey, PartImpactCounts>> = {}
  for (const part of changedParts) {
    byPart[part] = { inherit: 0, keepOverride: 0 }
  }

  let stagesUpdatedCount = 0
  for (const stage of eligible) {
    const binding = stage.defaultsBinding
    if (!binding) {
      continue
    }
    let stageTouched = false
    for (const part of changedParts) {
      if (STANDING_PARTS.includes(part) && stage.hasStandingRules !== true) {
        continue
      }
      const entry = partBinding(binding, part)
      if (entry == null) {
        continue
      }
      const counts = byPart[part]
      if (!counts) {
        continue
      }
      if (entry.isBound) {
        counts.inherit += 1
        stageTouched = true
      } else {
        counts.keepOverride += 1
      }
    }
    if (stageTouched) {
      stagesUpdatedCount += 1
    }
  }

  return {
    eligibleStageCount: eligible.length,
    stagesUpdatedCount,
    byPart,
    competitionOnly,
    changedParts,
  }
}
