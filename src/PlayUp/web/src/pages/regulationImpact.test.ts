import { describe, expect, it } from 'vitest'
import {
  buildRegulationImpactPreview,
  detectChangedHeritableParts,
  isStagePersonalized,
} from './regulationImpact'
import type {
  OrganisationRegulationSummary,
  OrganisationStageHubSummary,
  ReplaceRegulationRequest,
} from '../types'

const seed: OrganisationRegulationSummary = {
  minimumTeams: 8,
  maximumTeams: 16,
  durationPerPeriod: 45,
  numberOfPeriods: 2,
  halfTimeDuration: 15,
  winPoints: 3,
  drawPoints: 1,
  lossPoints: 0,
  rankingCriteria: ['Points', 'GoalDifference', 'GoalsFor', 'HeadToHead'],
  hasExtraTime: false,
  hasPenaltyShootout: false,
  forfeitWinnerGoals: 3,
  forfeitLoserGoals: 0,
  allowedTypes: ['Yellow', 'Red'],
}

function form(overrides: Partial<ReplaceRegulationRequest> = {}): ReplaceRegulationRequest {
  return {
    minimumTeams: 8,
    maximumTeams: 16,
    durationPerPeriod: 45,
    numberOfPeriods: 2,
    halfTimeDuration: 15,
    winPoints: 3,
    drawPoints: 1,
    lossPoints: 0,
    rankingCriteria: ['Points', 'GoalDifference', 'GoalsFor', 'HeadToHead'],
    hasExtraTime: false,
    hasPenaltyShootout: false,
    forfeitWinnerGoals: 3,
    forfeitLoserGoals: 0,
    allowedTypes: ['Yellow', 'Red'],
    ...overrides,
  }
}

describe('regulationImpact', () => {
  it('treats reordered ranking criteria as a canonical change', () => {
    const changed = detectChangedHeritableParts(
      form({
        rankingCriteria: ['Points', 'GoalsFor', 'GoalDifference', 'HeadToHead'],
      }),
      seed,
    )
    expect(changed).toEqual(['rankingCriteria'])
  })

  it('does not flag unchanged ordered criteria', () => {
    expect(detectChangedHeritableParts(form(), seed)).toEqual([])
  })

  it('aggregates inherit vs keep-override from DefaultsBinding', () => {
    const stages: OrganisationStageHubSummary[] = [
      {
        stageId: '1',
        name: 'Groupes',
        status: 'Draft',
        teamCount: 8,
        matchCount: 0,
        numberOfPeriods: 2,
        durationPerPeriod: 45,
        hasExtraTime: false,
        hasPenaltyShootout: false,
        hasStandingRules: true,
        hasDrawRules: false,
        hasQualificationRules: false,
        qualificationPathCount: 0,
        hasProgressionRules: false,
        progressionPathCount: 0,
        hasTieFormat: false,
        defaultsBinding: {
          matchDuration: { isBound: true },
          extraTime: { isBound: true },
          penaltyShootout: { isBound: true },
          administrativeResult: { isBound: true },
          points: { isBound: true },
          rankingCriteria: { isBound: true },
        },
      },
      {
        stageId: '2',
        name: 'Finale',
        status: 'Draft',
        teamCount: 2,
        matchCount: 0,
        numberOfPeriods: 2,
        durationPerPeriod: 45,
        hasExtraTime: true,
        hasPenaltyShootout: true,
        hasStandingRules: false,
        hasDrawRules: false,
        hasQualificationRules: false,
        qualificationPathCount: 0,
        hasProgressionRules: false,
        progressionPathCount: 0,
        hasTieFormat: true,
        defaultsBinding: {
          matchDuration: { isBound: true },
          extraTime: { isBound: false },
          penaltyShootout: { isBound: false },
          administrativeResult: { isBound: true },
          points: null,
          rankingCriteria: null,
        },
      },
    ]

    const preview = buildRegulationImpactPreview(form({ durationPerPeriod: 40 }), {
      regulation: seed,
      stages,
    })

    expect(preview.changedParts).toEqual(['matchDuration'])
    expect(preview.byPart.matchDuration).toEqual({ inherit: 2, keepOverride: 0 })
    expect(preview.stagesUpdatedCount).toBe(2)
  })

  it('marks a stage personalized only from unbound parts', () => {
    expect(
      isStagePersonalized({
        stageId: '1',
        name: 'X',
        status: 'Draft',
        teamCount: 0,
        matchCount: 0,
        numberOfPeriods: 2,
        durationPerPeriod: 45,
        hasExtraTime: false,
        hasPenaltyShootout: false,
        hasDrawRules: false,
        hasQualificationRules: false,
        qualificationPathCount: 0,
        hasProgressionRules: false,
        progressionPathCount: 0,
        hasTieFormat: false,
        defaultsBinding: {
          matchDuration: { isBound: true },
          extraTime: { isBound: true },
          penaltyShootout: { isBound: true },
          administrativeResult: { isBound: true },
          points: null,
          rankingCriteria: null,
        },
      }),
    ).toBe(false)

    expect(
      isStagePersonalized({
        stageId: '2',
        name: 'Y',
        status: 'Draft',
        teamCount: 0,
        matchCount: 0,
        numberOfPeriods: 2,
        durationPerPeriod: 45,
        hasExtraTime: true,
        hasPenaltyShootout: true,
        hasDrawRules: false,
        hasQualificationRules: false,
        qualificationPathCount: 0,
        hasProgressionRules: false,
        progressionPathCount: 0,
        hasTieFormat: false,
        defaultsBinding: {
          matchDuration: { isBound: true },
          extraTime: { isBound: false },
          penaltyShootout: { isBound: false },
          administrativeResult: { isBound: true },
          points: null,
          rankingCriteria: null,
        },
      }),
    ).toBe(true)
  })
})
