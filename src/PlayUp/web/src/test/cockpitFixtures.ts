import type {
  CockpitReferenceStageGameRules,
  CockpitView,
} from '../types'

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const drawId = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc'

/** Sample ReferenceStage game rules for En cours Règlement tests. */
export function referenceStageGameRules(
  overrides: Partial<CockpitReferenceStageGameRules> = {},
): CockpitReferenceStageGameRules {
  return {
    stageId,
    stageName: 'Phase 1',
    formatKind: 'Championship',
    winPoints: 3,
    drawPoints: 1,
    lossPoints: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    numberOfLegs: 1,
    aggregateScoring: false,
    hasTieExtraTime: false,
    hasTiePenaltyShootout: false,
    swissPlannedRounds: null,
    ...overrides,
  }
}

/** Minimal valid CockpitView for SPA tests (mirrors Host CockpitViewDto). */
export function cockpitView(
  overrides: Partial<CockpitView> = {},
): CockpitView {
  return {
    competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    completionMode: null,
    cycleReading: { code: 'Construction' },
    preparationFocus: 'Setup',
    calendarSummary: null,
    competitionOutcome: null,
    constructionDimensions: {
      teams: {
        prominence: 'Present',
        facts: { activeCount: '1', minimumTeams: '2' },
      },
      structure: {
        prominence: 'Present',
        facts: { formatKind: 'None' },
      },
      regulation: {
        prominence: 'Present',
        competition: {
          minimumTeams: 2,
          maximumTeams: 64,
          durationPerPeriod: 45,
          numberOfPeriods: 2,
          winPoints: 3,
          drawPoints: 1,
          lossPoints: 0,
        },
        stage: {
          stageId,
          stageName: 'Phase 1',
          hasDrawRules: false,
          numberOfPots: null,
          hasQualificationRules: false,
          qualificationPathCount: 0,
          hasProgressionRules: false,
          progressionPathCount: 0,
          hasTieFormat: false,
        },
        competitionRegulationMutable: true,
        transitionReadiness: [
          {
            transition: 'Draw',
            ready: false,
            blockerCodes: ['InsufficientParticipants'],
          },
          {
            transition: 'MaterializeMatches',
            ready: false,
            blockerCodes: ['InsufficientParticipants'],
          },
        ],
      },
      matches: {
        prominence: 'Absent',
        facts: { total: '0' },
      },
    },
    operationalFocus: {
      stages: [{ stageId, name: 'Phase 1', status: 'Draft' }],
      draws: [
        {
          stageId,
          drawId,
          kind: 'Slot',
          status: 'Published',
          resolutionState: 'Resolved',
          isApplied: false,
        },
      ],
      matchCounts: {
        live: 0,
        scheduled: 0,
        finished: 0,
        postponed: 0,
        cancelled: 0,
        total: 0,
      },
      swissByes: [],
      recentUnit: null,
      nextUnit: null,
      standingCompact: null,
      referenceStageGameRules: null,
    },
    situations: [],
    attentionSummary: { count: 0, items: [] },
    availableActions: [],
    naturalProgression: null,
    closureHint: { canCompleteNormally: false, blockerCodes: [] },
    navigationHints: [],
    ...overrides,
  }
}

export function cockpitSituation(
  overrides: Partial<CockpitView['situations'][number]> = {},
): CockpitView['situations'][number] {
  return {
    source: 'InsufficientParticipants',
    nature: 'Blocking',
    targetType: 'Organisation',
    targetId: competitionId,
    matchId: null,
    actionable: true,
    actionCode: 'AddEntry',
    impactCode: 'BlocksConstruction',
    params: { minimumTeams: '2', activeCount: '0' },
    ...overrides,
  }
}

export const cockpitIds = {
  competitionId,
  stageId,
  drawId,
  matchId,
} as const
