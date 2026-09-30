import type { Page, Route } from '@playwright/test';

/** Stable opaque ids for smoke fixtures (not business data). */
export const IDS = {
  competitionId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  stageId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  matchId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  fixtureId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
  homeEntryId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
  awayEntryId: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
} as const;

const COMPETITION_NAME = 'Spring Cup';
const STAGE_NAME = 'League';

type MatchStatus = 'Scheduled' | 'Live' | 'Finished';

function json(route: Route, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  });
}

function noContent(route: Route) {
  return route.fulfill({ status: 204, body: '' });
}

/** SPA deep-links share API path prefixes — never stub document navigations. */
function isApiFetch(route: Route) {
  const type = route.request().resourceType();
  return type === 'fetch' || type === 'xhr';
}

async function fulfillApi(
  route: Route,
  handler: (route: Route) => Promise<unknown> | unknown,
) {
  if (!isApiFetch(route)) {
    return route.fallback();
  }
  return handler(route);
}

function competitionListItem() {
  return {
    id: IDS.competitionId,
    name: COMPETITION_NAME,
    status: 'Ready',
  };
}

function competitionDetail() {
  return {
    id: IDS.competitionId,
    name: COMPETITION_NAME,
    status: 'Ready',
    entries: [],
    stages: [
      {
        stageId: IDS.stageId,
        name: STAGE_NAME,
        status: 'Ready',
      },
    ],
  };
}

function needsAttention() {
  return { competitionId: IDS.competitionId, items: [], count: 0 };
}

/** Minimal OverviewView — enough for shell + Overview region smoke. */
function overviewView() {
  return {
    competitionId: IDS.competitionId,
    name: COMPETITION_NAME,
    status: 'Ready',
    completionMode: null,
    cycleReading: { code: 'Construction' },
    preparationFocus: 'Setup',
    calendarSummary: null,
    competitionOutcome: null,
    constructionDimensions: {
      teams: {
        prominence: 'Present',
        facts: { activeCount: '2', minimumTeams: '2' },
      },
      structure: {
        prominence: 'Present',
        facts: {
          formatKind: 'Championship',
          groupCount: '0',
          roundCount: '0',
          matchdayCount: '1',
          slotCount: '0',
        },
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
          stageId: IDS.stageId,
          stageName: STAGE_NAME,
          hasDrawRules: false,
          numberOfPots: null,
          hasQualificationRules: false,
          qualificationPathCount: 0,
          hasProgressionRules: false,
          progressionPathCount: 0,
          hasTieFormat: false,
        },
        competitionRegulationMutable: true,
        transitionReadiness: [],
      },
      matches: {
        prominence: 'Condensed',
        facts: { live: '0', scheduled: '1', finished: '0', total: '1' },
      },
    },
    operationalFocus: {
      stages: [{ stageId: IDS.stageId, name: STAGE_NAME, status: 'Ready' }],
      draws: [],
      matchCounts: {
        live: 0,
        scheduled: 1,
        finished: 0,
        postponed: 0,
        cancelled: 0,
        total: 1,
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
  };
}

function structureView() {
  return {
    competitionId: IDS.competitionId,
    name: COMPETITION_NAME,
    status: 'Ready',
    participants: {
      activeCount: 2,
      occupyingCount: 2,
      entries: [
        {
          entryId: IDS.homeEntryId,
          displayName: 'Alpha',
          status: 'Active',
        },
        {
          entryId: IDS.awayEntryId,
          displayName: 'Beta',
          status: 'Active',
        },
      ],
    },
    format: {
      kind: 'Championship',
      primaryStageId: IDS.stageId,
      primaryStageName: STAGE_NAME,
      primaryStageStatus: 'Ready',
    },
    regulation: {
      minimumTeams: 2,
      maximumTeams: 64,
      durationPerPeriod: 45,
      numberOfPeriods: 2,
      winPoints: 3,
      drawPoints: 1,
      lossPoints: 0,
    },
    structure: {
      groupCount: 0,
      roundCount: 0,
      matchdayCount: 1,
      slotCount: 0,
      hasDrawRules: false,
      numberOfPots: null,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    actions: [],
    readiness: {
      readyForNextSlice: true,
      readyForDraw: false,
      readyForMaterialization: true,
      readyForSchedule: true,
      readyForMatchOperation: true,
      readyForSchedulePath: true,
      attachedMatchCount: 1,
      blockers: [],
    },
    stages: [
      {
        stageId: IDS.stageId,
        name: STAGE_NAME,
        status: 'Ready',
        teamCount: 2,
        matchCount: 1,
        groupCount: 0,
        roundCount: 0,
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
        formatKind: 'Championship',
        defaultsBinding: {
          matchDuration: { isBound: true },
          extraTime: { isBound: true },
          penaltyShootout: { isBound: true },
          administrativeResult: { isBound: true },
          points: { isBound: true },
          rankingCriteria: { isBound: true },
        },
      },
    ],
  };
}

function stageOverview() {
  return {
    id: IDS.stageId,
    competitionId: IDS.competitionId,
    name: STAGE_NAME,
    status: 'Ready',
    rounds: [],
    slots: [],
    draws: [],
  };
}

function stageSchematic() {
  return {
    stageId: IDS.stageId,
    competitionId: IDS.competitionId,
    name: STAGE_NAME,
    status: 'Ready',
    formatKind: 'Championship',
    cases: [],
    connections: [],
  };
}

function matchDetail(status: MatchStatus) {
  return {
    matchId: IDS.matchId,
    competitionId: IDS.competitionId,
    stageId: IDS.stageId,
    status,
    home: { entryId: IDS.homeEntryId, displayName: 'Alpha' },
    away: { entryId: IDS.awayEntryId, displayName: 'Beta' },
    result:
      status === 'Finished'
        ? {
            type: 'Played',
            homeGoals: 2,
            awayGoals: 1,
            extraTimePlayed: false,
          }
        : null,
    fixtureId: IDS.fixtureId,
    legIndex: 1,
    hasObservedLive: status === 'Live' || status === 'Finished',
    runningScore:
      status === 'Live' ? { homeGoals: 0, awayGoals: 0 } : undefined,
    declaredParticipations: [],
    recordedGoals: [],
    recordedSubstitutions: [],
    recordedDisciplinaryEvents: [],
  };
}

/**
 * Intercept Host JSON endpoints with fixed fixtures so smokes do not need
 * Postgres / DevRunner. Playwright runs handlers in reverse registration
 * order — register general routes first, specific last.
 */
export async function stubPlayUpApi(page: Page) {
  let matchStatus: MatchStatus = 'Scheduled';

  await page.route('**/competitions', (route) =>
    fulfillApi(route, (r) => {
      if (r.request().method() === 'GET') {
        return json(r, [competitionListItem()]);
      }
      return r.fallback();
    }),
  );

  await page.route(`**/competitions/${IDS.competitionId}`, (route) =>
    fulfillApi(route, (r) => {
      if (r.request().method() === 'GET') {
        return json(r, competitionDetail());
      }
      return r.fallback();
    }),
  );

  await page.route(`**/matches/${IDS.matchId}`, (route) =>
    fulfillApi(route, (r) => {
      if (r.request().method() === 'GET') {
        return json(r, matchDetail(matchStatus));
      }
      return noContent(r);
    }),
  );

  await page.route(`**/stages/${IDS.stageId}`, (route) =>
    fulfillApi(route, (r) => {
      if (r.request().method() === 'GET') {
        return json(r, stageOverview());
      }
      return noContent(r);
    }),
  );

  // Specific paths — registered last so they win over the general ones above.
  await page.route('**/competitions/*/overview', (route) =>
    fulfillApi(route, (r) => json(r, overviewView())),
  );
  await page.route('**/competitions/*/structure', (route) =>
    fulfillApi(route, (r) => json(r, structureView())),
  );
  await page.route('**/competitions/*/attention', (route) =>
    fulfillApi(route, (r) => json(r, needsAttention())),
  );
  await page.route('**/competitions/*/matches-hub', (route) =>
    fulfillApi(route, (r) =>
      json(r, {
        detail: competitionDetail(),
        stages: [
          {
            stageId: IDS.stageId,
            name: STAGE_NAME,
            status: 'Ready',
            matches: [],
          },
        ],
      }),
    ),
  );
  await page.route('**/stages/*/schematic', (route) =>
    fulfillApi(route, (r) => json(r, stageSchematic())),
  );
  await page.route('**/stages/*/matches', (route) =>
    fulfillApi(route, (r) => json(r, [])),
  );
  // Catch-all mutations before start/finish so those specific routes win (reverse order).
  await page.route(`**/matches/${IDS.matchId}/**`, (route) =>
    fulfillApi(route, (r) => {
      if (r.request().method() !== 'GET') {
        return noContent(r);
      }
      return r.fallback();
    }),
  );
  await page.route(`**/matches/${IDS.matchId}/start`, (route) =>
    fulfillApi(route, (r) => {
      matchStatus = 'Live';
      return noContent(r);
    }),
  );
  await page.route(`**/matches/${IDS.matchId}/finish`, (route) =>
    fulfillApi(route, (r) => {
      matchStatus = 'Finished';
      return noContent(r);
    }),
  );
}
