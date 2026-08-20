import type { CockpitView } from '../types'

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const drawId = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc'

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
        facts: {
          minimumTeams: 2,
          maximumTeams: 64,
          durationPerPeriod: 45,
          numberOfPeriods: 2,
          winPoints: 3,
          drawPoints: 1,
          lossPoints: 0,
        },
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
      upcomingMatches: [],
    },
    situations: [],
    attentionSummary: { count: 0, items: [] },
    availableActions: [],
    naturalProgression: { code: 'ContinueOrganisation' },
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
