import type { APIRequestContext } from '@playwright/test';

const DEFAULT_API_BASE = 'http://127.0.0.1:5287';

export function playUpApiBase(): string {
  return process.env.PLAYUP_API_URL?.replace(/\/$/, '') ?? DEFAULT_API_BASE;
}

type CompetitionListItem = {
  id: string;
  name: string;
  status: string;
};

type MatchHubView = {
  stages: {
    stageId: string;
    matches: { matchId: string; status: string }[];
  }[];
};

const CUP_COMPOSITION_COMPLETE_NAME = 'Coupe — composition complète';
const RECETTE_G1_GROUPS_NAME = 'RECETTE G1 — Groups 0/6 actionable';

type StructureView = {
  format: { primaryStageId?: string | null };
  readiness: { attachedMatchCount: number };
  stages: {
    stageId: string;
    name?: string;
    drawExecutionBadge?: string | null;
  }[];
};

type MaterializeMatchesResponse = {
  createdCount: number;
  alreadyComplete: boolean;
  attachedMatchIds: string[];
};

type DrawSummary = {
  drawId: string;
};

type DrawGeneration = {
  isResolved: boolean;
};

export async function findChampionshipReadyCompetition(
  request: APIRequestContext,
): Promise<CompetitionListItem> {
  const response = await request.get(`${playUpApiBase()}/competitions`);
  if (!response.ok()) {
    throw new Error(
      `GET /competitions failed (${response.status()}). Is Host running at ${playUpApiBase()}?`,
    );
  }

  const list = (await response.json()) as CompetitionListItem[];
  const hit = list.find((c) => c.name === 'Championnat Ready');
  if (!hit) {
    throw new Error(
      'Competition "Championnat Ready" not found. Seed with DevRunner: --reset --scenarios championship-ready',
    );
  }
  return hit;
}

export async function findRecetteG1GroupsCompetition(
  request: APIRequestContext,
): Promise<CompetitionListItem> {
  const response = await request.get(`${playUpApiBase()}/competitions`);
  if (!response.ok()) {
    throw new Error(
      `GET /competitions failed (${response.status()}). Is Host running at ${playUpApiBase()}?`,
    );
  }

  const list = (await response.json()) as CompetitionListItem[];
  const hit = list.find((c) => c.name === RECETTE_G1_GROUPS_NAME);
  if (!hit) {
    throw new Error(
      `Competition "${RECETTE_G1_GROUPS_NAME}" not found. Seed with DevRunner: --reset --scenarios structure-materialize-recette`,
    );
  }
  return hit;
}

export async function findCupCompositionCompleteCompetition(
  request: APIRequestContext,
): Promise<CompetitionListItem> {
  const response = await request.get(`${playUpApiBase()}/competitions`);
  if (!response.ok()) {
    throw new Error(
      `GET /competitions failed (${response.status()}). Is Host running at ${playUpApiBase()}?`,
    );
  }

  const list = (await response.json()) as CompetitionListItem[];
  const hit = list.find((c) => c.name === CUP_COMPOSITION_COMPLETE_NAME);
  if (!hit) {
    throw new Error(
      `Competition "${CUP_COMPOSITION_COMPLETE_NAME}" not found. Seed with DevRunner: --reset --scenarios cup-composition-complete`,
    );
  }
  return hit;
}

export async function getPrimaryStageId(
  request: APIRequestContext,
  competitionId: string,
): Promise<string> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `GET /competitions/${competitionId}/structure failed (${response.status()}): ${body}`,
    );
  }

  const view = (await response.json()) as StructureView;
  const stageId = view.format.primaryStageId?.trim();
  if (!stageId) {
    throw new Error(
      `Structure view has no primaryStageId for competition ${competitionId}`,
    );
  }
  return stageId;
}

export async function getStructureMaterializationState(
  request: APIRequestContext,
  competitionId: string,
): Promise<{ stageId: string; attachedMatchCount: number }> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `GET /competitions/${competitionId}/structure failed (${response.status()}): ${body}`,
    );
  }

  const view = (await response.json()) as StructureView;
  const stageId = view.format.primaryStageId?.trim();
  if (!stageId) {
    throw new Error(
      `Structure view has no primaryStageId for competition ${competitionId}`,
    );
  }

  return {
    stageId,
    attachedMatchCount: view.readiness.attachedMatchCount,
  };
}

export async function runGroupsMaterializeMatches(
  request: APIRequestContext,
  stageId: string,
): Promise<MaterializeMatchesResponse> {
  const response = await request.post(
    `${playUpApiBase()}/stages/${stageId}/matches/materialize`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `POST /stages/${stageId}/matches/materialize failed (${response.status()}): ${body}`,
    );
  }

  return (await response.json()) as MaterializeMatchesResponse;
}

export async function runCupSlotDrawPublishAndApply(
  request: APIRequestContext,
  stageId: string,
): Promise<void> {
  const create = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws`,
    {
      data: { kind: 'Slot' },
    },
  );
  if (!create.ok()) {
    const body = await create.text();
    throw new Error(
      `POST /stages/${stageId}/draws failed (${create.status()}): ${body}`,
    );
  }
  const draw = (await create.json()) as DrawSummary;

  const inputs = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws/${draw.drawId}/inputs`,
  );
  if (!inputs.ok()) {
    const body = await inputs.text();
    throw new Error(`POST .../inputs failed (${inputs.status()}): ${body}`);
  }

  const generate = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws/${draw.drawId}/generate`,
  );
  if (!generate.ok()) {
    const body = await generate.text();
    throw new Error(`POST .../generate failed (${generate.status()}): ${body}`);
  }
  const generation = (await generate.json()) as DrawGeneration;
  if (!generation.isResolved) {
    throw new Error('Draw generate did not resolve (isResolved=false)');
  }

  const publishAndApply = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws/${draw.drawId}/publish-and-apply`,
  );
  if (!publishAndApply.ok()) {
    const body = await publishAndApply.text();
    throw new Error(
      `POST .../publish-and-apply failed (${publishAndApply.status()}): ${body}`,
    );
  }
}

type StageDrawWire = {
  id: string;
  status: string;
};

type StageOverviewWire = {
  draws?: StageDrawWire[];
};

/**
 * API and UI specs share one seeded cup. Reset draw state before each test.
 *
 * Cancels every non-cancelled draw on the stage, including draws already
 * applied (`Applied`), then releases aligned placements. UI specs apply a draw;
 * without this teardown the next spec (API or UI, in either order) would not
 * start from the seeded pending-draw scenario.
 */
export async function ensureCupCompositionDrawPending(
  request: APIRequestContext,
): Promise<void> {
  const competition = await findCupCompositionCompleteCompetition(request);
  const stageId = await getPrimaryStageId(request, competition.id);
  const overview = await getStageOverview(request, stageId);

  for (const draw of overview.draws ?? []) {
    if (draw.status === 'Cancelled') {
      continue;
    }

    const cancel = await request.post(
      `${playUpApiBase()}/stages/${stageId}/draws/${draw.id}/cancel`,
    );
    if (!cancel.ok()) {
      const body = await cancel.text();
      throw new Error(
        `POST .../draws/${draw.id}/cancel failed (${cancel.status()}): ${body}`,
      );
    }

    const release = await request.post(
      `${playUpApiBase()}/stages/${stageId}/draws/${draw.id}/release-aligned-placements`,
    );
    if (!release.ok()) {
      const body = await release.text();
      throw new Error(
        `POST .../draws/${draw.id}/release-aligned-placements failed (${release.status()}): ${body}`,
      );
    }
  }
}

/**
 * API and UI specs share RECETTE G1. Drop attached matches so materialize
 * starts from 0/6 again.
 */
export async function ensureGroupsMaterializePending(
  request: APIRequestContext,
): Promise<void> {
  const competition = await findRecetteG1GroupsCompetition(request);
  const before = await getStructureMaterializationState(
    request,
    competition.id,
  );
  if (before.attachedMatchCount === 0) {
    return;
  }

  const response = await request.post(
    `${playUpApiBase()}/stages/${before.stageId}/matches/dematerialize`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `POST /stages/${before.stageId}/matches/dematerialize failed (${response.status()}): ${body}`,
    );
  }

  const after = await getStructureMaterializationState(request, competition.id);
  if (after.attachedMatchCount !== 0) {
    throw new Error(
      `Expected 0 attached matches after dematerialize, got ${after.attachedMatchCount}`,
    );
  }
}

async function getStageOverview(
  request: APIRequestContext,
  stageId: string,
): Promise<StageOverviewWire> {
  const response = await request.get(`${playUpApiBase()}/stages/${stageId}`);
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `GET /stages/${stageId} failed (${response.status()}): ${body}`,
    );
  }

  return (await response.json()) as StageOverviewWire;
}

export async function expectCupDrawAppliedOnStructure(
  request: APIRequestContext,
  competitionId: string,
): Promise<void> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`GET structure failed (${response.status()}): ${body}`);
  }

  const view = (await response.json()) as StructureView;
  const applied = view.stages.some(
    (stage) => stage.drawExecutionBadge === 'Applied',
  );
  if (!applied) {
    throw new Error(
      `Expected drawExecutionBadge Applied on structure for competition ${competitionId}`,
    );
  }
}

const RECETTE_FRESHNESS_ACTIONS_NAME = 'RECETTE — Actions de fraîcheur';

const RECETTE_T3_QUAL_SLOT_NAME = 'RECETTE T3 — Qual slot fraîcheur';

const RECETTE_T3_SCORE_UPSTREAM_NAME = 'RECETTE T3 — Score upstream fraîcheur';

export async function findFreshnessActionsRecetteCompetition(
  request: APIRequestContext,
): Promise<CompetitionListItem> {
  const response = await request.get(`${playUpApiBase()}/competitions`);
  if (!response.ok()) {
    throw new Error(
      `GET /competitions failed (${response.status()}). Is Host running at ${playUpApiBase()}?`,
    );
  }

  const list = (await response.json()) as CompetitionListItem[];
  const hit = list.find((c) => c.name === RECETTE_FRESHNESS_ACTIONS_NAME);
  if (!hit) {
    throw new Error(
      `Competition "${RECETTE_FRESHNESS_ACTIONS_NAME}" not found. Seed with DevRunner: --reset --scenarios freshness-actions-recette`,
    );
  }
  return hit;
}

export async function findResolutionFreshnessScoreUpstreamCompetition(
  request: APIRequestContext,
): Promise<CompetitionListItem> {
  const response = await request.get(`${playUpApiBase()}/competitions`);
  if (!response.ok()) {
    throw new Error(
      `GET /competitions failed (${response.status()}). Is Host running at ${playUpApiBase()}?`,
    );
  }

  const list = (await response.json()) as CompetitionListItem[];
  const hit = list.find((c) => c.name === RECETTE_T3_SCORE_UPSTREAM_NAME);
  if (!hit) {
    throw new Error(
      `Competition "${RECETTE_T3_SCORE_UPSTREAM_NAME}" not found. Seed with DevRunner: --reset --scenarios resolution-freshness-qual-slot`,
    );
  }
  return hit;
}

export async function findResolutionFreshnessQualSlotCompetition(
  request: APIRequestContext,
): Promise<CompetitionListItem> {
  const response = await request.get(`${playUpApiBase()}/competitions`);
  if (!response.ok()) {
    throw new Error(
      `GET /competitions failed (${response.status()}). Is Host running at ${playUpApiBase()}?`,
    );
  }

  const list = (await response.json()) as CompetitionListItem[];
  const hit = list.find((c) => c.name === RECETTE_T3_QUAL_SLOT_NAME);
  if (!hit) {
    throw new Error(
      `Competition "${RECETTE_T3_QUAL_SLOT_NAME}" not found. Seed with DevRunner: --reset --scenarios resolution-freshness-qual-slot`,
    );
  }
  return hit;
}

export async function getStageIdByName(
  request: APIRequestContext,
  competitionId: string,
  stageName: string,
): Promise<string> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `GET /competitions/${competitionId}/structure failed (${response.status()}): ${body}`,
    );
  }

  const view = (await response.json()) as StructureView;
  const stage = view.stages.find((s) => s.name === stageName);
  if (!stage?.stageId) {
    throw new Error(
      `Stage "${stageName}" not found on structure for competition ${competitionId}`,
    );
  }
  return stage.stageId;
}

export async function findFirstFinishedMatchOnStage(
  request: APIRequestContext,
  stageId: string,
): Promise<string> {
  const response = await request.get(
    `${playUpApiBase()}/stages/${stageId}/matches`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `GET /stages/${stageId}/matches failed (${response.status()}): ${body}`,
    );
  }

  const matches = (await response.json()) as {
    matchId: string;
    status: string;
  }[];
  const finished = matches.find((m) => m.status === 'Finished');
  if (!finished) {
    throw new Error(`No Finished match on stage ${stageId}`);
  }
  return finished.matchId;
}

export async function correctFinishedMatchResult(
  request: APIRequestContext,
  matchId: string,
  homeGoals: number,
  awayGoals: number,
): Promise<void> {
  const response = await request.put(
    `${playUpApiBase()}/matches/${matchId}/result`,
    {
      data: {
        type: 'Played',
        homeGoals,
        awayGoals,
      },
    },
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `PUT /matches/${matchId}/result failed (${response.status()}): ${body}`,
    );
  }
}

export type IsolatedReleaseCompetition = {
  competitionId: string;
  stageId: string;
  team: string;
  matchLabel: string | null;
};

/**
 * A competition created for one release walkthrough. A retry builds another
 * competition, so a previous release cannot empty the place under test.
 * Nord is qualified while a win meets the points condition, then the score
 * becomes a draw and the place is unconfirmed again.
 */
export async function createIsolatedReleaseCompetition(
  request: APIRequestContext,
  withMatch: boolean,
): Promise<IsolatedReleaseCompetition> {
  const competitionId = await postId(request, '/competitions', {
    name: `Libérer ${crypto.randomUUID()}`,
  });
  const nord = await addNamedEntry(request, competitionId, 'Nord');
  const est = await addNamedEntry(request, competitionId, 'Est');
  const sourceId = await addStage(request, competitionId, {
    format: 'Championship',
    name: 'Source',
    matchGenerationFormat: 'SingleRoundRobin',
  });
  const stageId = await addStage(request, competitionId, {
    format: 'Cup',
    name: withMatch ? 'Généré' : 'Vide',
    bracketSize: 2,
  });

  await sendOk(request, 'PUT', `/stages/${sourceId}/affectation`, {
    entryIds: [nord, est],
  });
  await sendOk(request, 'PUT', `/stages/${sourceId}/qualification-rules`, {
    intents: [
      {
        ...intent(1, 1, stageId, 'S1'),
        minimumPoints: 3,
      },
      ...(withMatch ? [intent(2, 2, stageId, 'S2')] : []),
    ],
  });
  await sendOk(request, 'POST', `/stages/${sourceId}/prepare`);
  await sendOk(request, 'POST', `/competitions/${competitionId}/prepare`);
  await sendOk(request, 'POST', `/stages/${sourceId}/matches/materialize`);
  await sendOk(request, 'POST', `/stages/${sourceId}/start`);
  await sendOk(request, 'POST', `/competitions/${competitionId}/start`);

  const sourceMatch = await readStageMatch(request, sourceId);
  const nordAtHome = sourceMatch.homeEntryId === nord;
  await sendOk(request, 'POST', `/matches/${sourceMatch.matchId}/start`);
  await sendOk(request, 'POST', `/matches/${sourceMatch.matchId}/finish`, {
    type: 'Played',
    homeGoals: nordAtHome ? 2 : 0,
    awayGoals: nordAtHome ? 0 : 2,
  });
  await sendOk(request, 'POST', `/stages/${sourceId}/qualification/apply`);
  if (withMatch) {
    await sendOk(request, 'POST', `/stages/${stageId}/prepare`);
    await sendOk(
      request,
      'POST',
      `/stages/${stageId}/matches/materialize-from-slots`,
      { pairKeys: ['P1'] },
    );
  }
  await sendOk(request, 'PUT', `/matches/${sourceMatch.matchId}/result`, {
    type: 'Played',
    homeGoals: 1,
    awayGoals: 1,
  });

  return {
    competitionId,
    stageId,
    team: 'Nord',
    matchLabel: withMatch ? 'Nord – Est' : null,
  };
}

async function postId(
  request: APIRequestContext,
  path: string,
  data: unknown,
): Promise<string> {
  const response = await request.post(`${playUpApiBase()}${path}`, { data });
  if (!response.ok()) {
    throw new Error(
      `POST ${path} failed (${response.status()}): ${await response.text()}`,
    );
  }
  const body = (await response.json()) as {
    id?: string;
    stageId?: string;
  };
  const id = body.stageId ?? body.id;
  if (!id) {
    throw new Error(`POST ${path} returned no id.`);
  }
  return id;
}

async function addNamedEntry(
  request: APIRequestContext,
  competitionId: string,
  displayName: string,
): Promise<string> {
  const response = await request.post(
    `${playUpApiBase()}/competitions/${competitionId}/entries`,
    { data: { displayName } },
  );
  if (!response.ok()) {
    throw new Error(
      `POST entry failed (${response.status()}): ${await response.text()}`,
    );
  }
  const view = (await response.json()) as {
    participants?: { entries?: { entryId: string; displayName?: string }[] };
    entries?: { entryId: string; displayName?: string }[];
  };
  const entries = view.participants?.entries ?? view.entries ?? [];
  const created = entries.find((entry) => entry.displayName === displayName);
  if (!created) {
    throw new Error(`Entry "${displayName}" was not returned.`);
  }
  return created.entryId;
}

async function addStage(
  request: APIRequestContext,
  competitionId: string,
  data: {
    format: string;
    name: string;
    matchGenerationFormat?: string;
    bracketSize?: number;
  },
): Promise<string> {
  return postId(request, `/competitions/${competitionId}/stages`, data);
}

function intent(
  order: number,
  position: number,
  destinationStageId: string,
  slotKey: string,
) {
  return {
    intentId: crypto.randomUUID(),
    order,
    sourceKind: 'Overall',
    positionFrom: position,
    positionTo: position,
    destinationStageId,
    destinationSlotKeys: [slotKey],
  };
}

async function sendOk(
  request: APIRequestContext,
  method: 'POST' | 'PUT',
  path: string,
  data?: unknown,
): Promise<void> {
  const response = await request.fetch(`${playUpApiBase()}${path}`, {
    method,
    data,
  });
  if (!response.ok()) {
    throw new Error(
      `${method} ${path} failed (${response.status()}): ${await response.text()}`,
    );
  }
}

async function readStageMatch(
  request: APIRequestContext,
  stageId: string,
): Promise<{ matchId: string; homeEntryId: string }> {
  const response = await request.get(
    `${playUpApiBase()}/stages/${stageId}/matches`,
  );
  if (!response.ok()) {
    throw new Error(
      `GET matches failed (${response.status()}): ${await response.text()}`,
    );
  }
  const matches = (await response.json()) as {
    matchId: string;
    home?: { entryId?: string };
  }[];
  const match = matches[0];
  if (!match?.home?.entryId) {
    throw new Error(`Stage ${stageId} has no match with a home team.`);
  }
  return { matchId: match.matchId, homeEntryId: match.home.entryId };
}

export async function replaceQualificationRulesForStaleBadge(
  request: APIRequestContext,
  leagueStageId: string,
  terminalStageId: string,
): Promise<void> {
  const response = await request.put(
    `${playUpApiBase()}/stages/${leagueStageId}/qualification-rules`,
    {
      data: {
        intents: [
          {
            intentId: crypto.randomUUID(),
            order: 1,
            sourceKind: 'Overall',
            positionFrom: 2,
            positionTo: 2,
            destinationStageId: terminalStageId,
            destinationSlotKeys: ['Champ'],
          },
        ],
      },
    },
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `PUT qualification-rules failed (${response.status()}): ${body}`,
    );
  }
}

export type ScheduledMatchRef = {
  matchId: string;
  stageId: string;
};

export async function findFirstScheduledMatch(
  request: APIRequestContext,
  competitionId: string,
): Promise<ScheduledMatchRef> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/matches-hub`,
  );
  if (!response.ok()) {
    throw new Error(
      `GET matches-hub failed (${response.status()}) for competition ${competitionId}`,
    );
  }

  const hub = (await response.json()) as MatchHubView;
  for (const stage of hub.stages) {
    const scheduled = stage.matches.find((m) => m.status === 'Scheduled');
    if (scheduled) {
      return { matchId: scheduled.matchId, stageId: stage.stageId };
    }
  }

  throw new Error(
    `No Scheduled match in matches-hub for competition ${competitionId}`,
  );
}

/**
 * championship-ready leaves competition/stage Ready; match Start requires Running (StageExploitationGuard).
 */
export async function openSportiveExploitation(
  request: APIRequestContext,
  competitionId: string,
  stageId: string,
): Promise<void> {
  const stageStart = await request.post(
    `${playUpApiBase()}/stages/${stageId}/start`,
  );
  if (!stageStart.ok()) {
    const body = await stageStart.text();
    throw new Error(
      `POST /stages/${stageId}/start failed (${stageStart.status()}): ${body}`,
    );
  }

  const competitionStart = await request.post(
    `${playUpApiBase()}/competitions/${competitionId}/start`,
  );
  if (!competitionStart.ok()) {
    const body = await competitionStart.text();
    throw new Error(
      `POST /competitions/${competitionId}/start failed (${competitionStart.status()}): ${body}`,
    );
  }
}
