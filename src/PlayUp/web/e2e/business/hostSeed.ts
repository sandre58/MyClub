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

type StageDrawOverviewWire = StageDrawWire & {
  actions?: {
    release?: {
      releasableCount?: number;
      state?: string;
    };
  };
};

type StageOverviewWire = {
  draws?: StageDrawOverviewWire[];
  slots?: { slotKey: string; entryId: string | null }[];
  releaseCandidateDrawId?: string | null;
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
  const before = await getStageOverview(request, stageId);

  for (const draw of before.draws ?? []) {
    if (draw.status === 'Cancelled') {
      continue;
    }

    const cancel = await request.post(
      `${playUpApiBase()}/stages/${stageId}/draws/${draw.id}/cancel`,
    );
    if (!cancel.ok() && cancel.status() !== 409) {
      const body = await cancel.text();
      throw new Error(
        `POST .../draws/${draw.id}/cancel failed (${cancel.status()}): ${body}`,
      );
    }
  }

  for (let pass = 0; pass < 6; pass++) {
    const overview = await getStageOverview(request, stageId);
    const cancelledDraws = [...(overview.draws ?? [])]
      .filter((draw) => draw.status === 'Cancelled')
      .reverse();
    let released = false;
    for (const draw of cancelledDraws) {
      if ((draw.actions?.release?.releasableCount ?? 0) <= 0) {
        continue;
      }

      const release = await request.post(
        `${playUpApiBase()}/stages/${stageId}/draws/${draw.id}/release-aligned-placements`,
      );
      if (release.ok()) {
        released = true;
        continue;
      }
      if (release.status() !== 409 && release.status() !== 404) {
        const body = await release.text();
        throw new Error(
          `POST .../draws/${draw.id}/release-aligned-placements failed (${release.status()}): ${body}`,
        );
      }
    }

    const occupiedSlots =
      overview.slots?.filter((slot) => slot.entryId != null).length ?? 0;
    const pendingDraws = (overview.draws ?? []).some(
      (draw) => draw.status === 'Draft' || draw.status === 'Published',
    );
    if (occupiedSlots === 0 && !pendingDraws) {
      return;
    }

    if (!released) {
      break;
    }
  }

  let finalOverview = await getStageOverview(request, stageId);
  for (const slot of finalOverview.slots ?? []) {
    if (slot.entryId == null) {
      continue;
    }

    const cleared = await request.delete(
      `${playUpApiBase()}/stages/${stageId}/slots/${encodeURIComponent(slot.slotKey)}/assignment?expectedEntryId=${encodeURIComponent(slot.entryId)}`,
    );
    if (!cleared.ok() && cleared.status() !== 409) {
      const body = await cleared.text();
      throw new Error(
        `DELETE slot assignment ${slot.slotKey} failed (${cleared.status()}): ${body}`,
      );
    }
  }

  finalOverview = await getStageOverview(request, stageId);
  const remainingOccupied =
    finalOverview.slots?.filter((slot) => slot.entryId != null).length ?? 0;
  if (remainingOccupied > 0) {
    throw new Error(
      `Cup composition stage still has ${remainingOccupied} occupied slots after draw teardown`,
    );
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

export async function readFinishedMatchScore(
  request: APIRequestContext,
  matchId: string,
): Promise<{ homeGoals: number; awayGoals: number }> {
  const response = await request.get(`${playUpApiBase()}/matches/${matchId}`);
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `GET /matches/${matchId} failed (${response.status()}): ${body}`,
    );
  }
  const match = (await response.json()) as {
    result?: { homeGoals: number; awayGoals: number };
  };
  const homeGoals = match.result?.homeGoals;
  const awayGoals = match.result?.awayGoals;
  if (homeGoals == null || awayGoals == null) {
    throw new Error(`Match ${matchId} has no result`);
  }
  return { homeGoals, awayGoals };
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

export type DefinitiveSelectionCompetition = {
  competitionId: string;
  sourceId: string;
  destinationId: string;
  team: string;
  cupEntryIds: string[];
};

/**
 * Finished league, overall places 1–4, four teams. The fourth is withdrawn
 * afterwards, so that place is an empty selection on a completed source.
 * A group ranking does not shrink when a team withdraws, and an empty
 * selection cannot be applied, so the path is overall and the destination
 * is a cup slot — the place manual placement can fill.
 */
export async function createDefinitiveSelectionCompetition(
  request: APIRequestContext,
): Promise<DefinitiveSelectionCompetition> {
  const competitionId = await postId(request, '/competitions', {
    name: `Définitif ${crypto.randomUUID()}`,
  });
  const nord = await addNamedEntry(request, competitionId, 'Nord');
  const est = await addNamedEntry(request, competitionId, 'Est');
  const sud = await addNamedEntry(request, competitionId, 'Sud');
  const ouest = await addNamedEntry(request, competitionId, 'Ouest');
  const sourceId = await addStage(request, competitionId, {
    format: 'Championship',
    name: 'Ligue',
    matchGenerationFormat: 'SingleRoundRobin',
  });
  const destinationId = await addStage(request, competitionId, {
    format: 'Cup',
    name: 'Tableau',
    bracketSize: 4,
  });

  await sendOk(request, 'PUT', `/stages/${sourceId}/affectation`, {
    entryIds: [nord, est, sud, ouest],
  });
  await sendOk(request, 'PUT', `/stages/${sourceId}/qualification-rules`, {
    intents: [
      {
        intentId: crypto.randomUUID(),
        order: 1,
        sourceKind: 'Overall',
        positionFrom: 1,
        positionTo: 4,
        destinationStageId: destinationId,
        destinationSlotKeys: ['S1', 'S2', 'S3', 'S4'],
      },
    ],
  });
  await sendOk(request, 'POST', `/stages/${sourceId}/prepare`);
  await sendOk(request, 'POST', `/competitions/${competitionId}/prepare`);
  await sendOk(request, 'POST', `/stages/${sourceId}/matches/materialize`);
  await sendOk(request, 'POST', `/stages/${sourceId}/start`);
  await sendOk(request, 'POST', `/competitions/${competitionId}/start`);

  const strength = new Map<string, number>([
    [nord, 4],
    [est, 3],
    [sud, 2],
    [ouest, 1],
  ]);
  for (const match of await readStageMatches(request, sourceId)) {
    const home = strength.get(match.homeEntryId) ?? 0;
    const away = strength.get(match.awayEntryId) ?? 0;
    const homeWins = home > away;
    await sendOk(request, 'POST', `/matches/${match.matchId}/start`);
    await sendOk(request, 'POST', `/matches/${match.matchId}/finish`, {
      type: 'Played',
      homeGoals: homeWins ? 1 : 0,
      awayGoals: homeWins ? 0 : 1,
    });
  }

  await sendOk(request, 'POST', `/stages/${sourceId}/qualification/apply`);
  await sendOk(request, 'POST', `/stages/${sourceId}/complete`);
  await sendOk(
    request,
    'POST',
    `/competitions/${competitionId}/entries/${ouest}/withdraw`,
  );

  return {
    competitionId,
    sourceId,
    destinationId,
    team: 'Ouest',
    cupEntryIds: [nord, est, sud, ouest],
  };
}

async function readStageMatches(
  request: APIRequestContext,
  stageId: string,
): Promise<{ matchId: string; homeEntryId: string; awayEntryId: string }[]> {
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
    away?: { entryId?: string };
  }[];
  return matches.map((match) => {
    if (!match.home?.entryId || !match.away?.entryId) {
      throw new Error(`Stage ${stageId} has a match without both teams.`);
    }
    return {
      matchId: match.matchId,
      homeEntryId: match.home.entryId,
      awayEntryId: match.away.entryId,
    };
  });
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

export type CupTwoLegJourneyShell = {
  competitionId: string;
  semiStageId: string;
  finalStageId: string;
  semiRoundId: string;
};

export type CupTwoLegJourneyReady = CupTwoLegJourneyShell & {
  p1Leg1MatchId: string;
  p1Leg2MatchId: string;
  leg1HomeName: string;
  leg1AwayName: string;
  leg2HomeName: string;
  leg2AwayName: string;
};

/**
 * Coupe 4 (demi-finale) + phase finale 2 places, progression gagnant → S1.
 * Sans format de confrontation : l’E2E le pose via l’UI.
 */
export async function createCupTwoLegJourneyShell(
  request: APIRequestContext,
): Promise<CupTwoLegJourneyShell> {
  const competitionId = await postId(request, '/competitions', {
    name: `Coupe AR ${crypto.randomUUID()}`,
  });
  for (let i = 1; i <= 4; i++) {
    await addNamedEntry(request, competitionId, `Équipe ${i}`);
  }

  const structureCreate = await request.post(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
    { data: { format: 'Cup', bracketSize: 4 } },
  );
  if (!structureCreate.ok()) {
    throw new Error(
      `POST structure failed (${structureCreate.status()}): ${await structureCreate.text()}`,
    );
  }
  const configured = (await structureCreate.json()) as {
    structure: { format: { primaryStageId?: string | null } };
  };
  const semiStageId = configured.structure.format.primaryStageId?.trim();
  if (!semiStageId) {
    throw new Error(`No primary stage for competition ${competitionId}`);
  }
  const structure = await getCompetitionStructure(request, competitionId);

  const finalStageId = await addStage(request, competitionId, {
    format: 'Cup',
    name: 'Finale',
    bracketSize: 2,
  });

  const stageOverview = await readStageOverview(request, semiStageId);
  const semiRoundId = stageOverview.rounds[0]?.id;
  if (!semiRoundId) {
    throw new Error(`Semi stage ${semiStageId} has no round.`);
  }

  const entryIds = structure.participants.entries.map((entry) => entry.entryId);
  await sendOk(request, 'PUT', `/stages/${semiStageId}/progression-rules`, {
    intents: [
      {
        intentId: crypto.randomUUID(),
        order: 1,
        roundId: semiRoundId,
        outcome: 'Winner',
        destinationStageId: finalStageId,
        destinationSlotKeys: ['S1', 'S2'],
      },
    ],
  });
  await sendOk(request, 'PUT', `/stages/${semiStageId}/affectation`, {
    entryIds,
  });

  return { competitionId, semiStageId, finalStageId, semiRoundId };
}

/**
 * Complète le format 2 manches pour un départage possible (porte de préparation Coupe + progression).
 */
export async function ensureCupTwoLegDecidableTieFormat(
  request: APIRequestContext,
  semiStageId: string,
): Promise<void> {
  await sendOk(request, 'PUT', `/stages/${semiStageId}/tie-format`, {
    clear: false,
    numberOfLegs: 2,
    hasExtraTimeRule: true,
    hasPenaltyShootoutRule: true,
  });
}

/** Tirage, préparation, matérialisation (2 demies) et exploitation sportive. */
export async function prepareCupTwoLegJourneyForPlay(
  request: APIRequestContext,
  shell: CupTwoLegJourneyShell,
): Promise<CupTwoLegJourneyReady> {
  await ensureCupTwoLegDecidableTieFormat(request, shell.semiStageId);
  await sendOk(request, 'PUT', `/stages/${shell.semiStageId}/draw-rules`, {
    clear: false,
    mode: 'Random',
  });
  await runCupSlotDrawPublishAndApply(request, shell.semiStageId);
  await sendOk(request, 'POST', `/stages/${shell.semiStageId}/prepare`);
  await sendOk(request, 'POST', `/competitions/${shell.competitionId}/prepare`);
  await sendOk(
    request,
    'POST',
    `/stages/${shell.semiStageId}/matches/materialize-from-slots`,
    { pairKeys: ['P1', 'P2'] },
  );
  await openSportiveExploitation(
    request,
    shell.competitionId,
    shell.semiStageId,
  );

  const legs = await readFirstTwoLegFixture(request, shell.semiStageId);
  const leg1 = await readMatchDetail(request, legs.leg1MatchId);
  const leg2 = await readMatchDetail(request, legs.leg2MatchId);

  return {
    ...shell,
    p1Leg1MatchId: legs.leg1MatchId,
    p1Leg2MatchId: legs.leg2MatchId,
    leg1HomeName: leg1.homeDisplayName,
    leg1AwayName: leg1.awayDisplayName,
    leg2HomeName: leg2.homeDisplayName,
    leg2AwayName: leg2.awayDisplayName,
  };
}

export async function applyCupTwoLegJourneyProgression(
  request: APIRequestContext,
  semiStageId: string,
): Promise<string> {
  const legs = await readFirstTwoLegFixture(request, semiStageId);
  await sendOk(
    request,
    'POST',
    `/stages/${semiStageId}/fixtures/${legs.fixtureId}/apply-progression`,
  );
  return legs.leg1HomeEntryId;
}

export async function readFinalSlotSEntryName(
  request: APIRequestContext,
  finalStageId: string,
): Promise<string | null> {
  const overview = await readStageOverview(request, finalStageId);
  const slot = overview.slots.find((s) => s.slotKey === 'S1');
  return slot?.displayName ?? null;
}

type CompetitionStructureView = StructureView & {
  participants: { entries: { entryId: string; displayName?: string }[] };
  stages: {
    stageId: string;
    defaultTieFormat?: { numberOfLegs: number } | null;
    confrontationSegments?:
      | {
          rounds: { roundId: string; inheritsDefaultTieFormat: boolean }[];
          numberOfLegs: number;
        }[]
      | null;
  }[];
};

async function getCompetitionStructure(
  request: APIRequestContext,
  competitionId: string,
): Promise<CompetitionStructureView> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    throw new Error(
      `GET structure failed (${response.status()}): ${await response.text()}`,
    );
  }
  return (await response.json()) as CompetitionStructureView;
}

type StageOverviewResponse = {
  rounds: {
    id: string;
    fixtures: {
      id: string;
      slotAKey?: string | null;
      slotBKey?: string | null;
      attachments: { matchId: string; legIndex: number }[];
    }[];
  }[];
  bracketPairs: { pairKey: string; slotAKey: string; slotBKey: string }[];
  slots: {
    slotKey: string;
    entryId?: string | null;
    displayName?: string | null;
  }[];
};

async function readStageOverview(
  request: APIRequestContext,
  stageId: string,
): Promise<StageOverviewResponse> {
  const response = await request.get(`${playUpApiBase()}/stages/${stageId}`);
  if (!response.ok()) {
    throw new Error(
      `GET /stages/${stageId} failed (${response.status()}): ${await response.text()}`,
    );
  }
  return (await response.json()) as StageOverviewResponse;
}

async function readMatchDetail(
  request: APIRequestContext,
  matchId: string,
): Promise<{ homeDisplayName: string; awayDisplayName: string }> {
  const response = await request.get(`${playUpApiBase()}/matches/${matchId}`);
  if (!response.ok()) {
    throw new Error(
      `GET /matches/${matchId} failed (${response.status()}): ${await response.text()}`,
    );
  }
  const body = (await response.json()) as {
    home?: { displayName?: string };
    away?: { displayName?: string };
  };
  return {
    homeDisplayName: body.home?.displayName?.trim() || 'Domicile',
    awayDisplayName: body.away?.displayName?.trim() || 'Extérieur',
  };
}

async function readFirstTwoLegFixture(
  request: APIRequestContext,
  semiStageId: string,
): Promise<{
  fixtureId: string;
  leg1MatchId: string;
  leg2MatchId: string;
  leg1HomeEntryId: string;
}> {
  const overview = await readStageOverview(request, semiStageId);
  const p1 = [...overview.bracketPairs].sort((a, b) =>
    a.pairKey.localeCompare(b.pairKey),
  )[0];
  if (!p1) {
    throw new Error(`Semi stage ${semiStageId} has no bracket pairs.`);
  }
  const target = overview.rounds.flatMap((round) => round.fixtures);
  const twoLeg = target.find(
    (f) =>
      f.attachments.length === 2 &&
      f.slotAKey === p1.slotAKey &&
      f.slotBKey === p1.slotBKey,
  );
  if (!twoLeg) {
    throw new Error(
      `No two-leg fixture for pair ${p1.pairKey} on semi stage ${semiStageId}.`,
    );
  }
  const leg1 = twoLeg.attachments.find((a) => a.legIndex === 1);
  const leg2 = twoLeg.attachments.find((a) => a.legIndex === 2);
  if (!leg1 || !leg2) {
    throw new Error('Fixture attachments missing leg 1 or leg 2.');
  }
  const leg1Detail = await request.get(
    `${playUpApiBase()}/matches/${leg1.matchId}`,
  );
  const leg1Body = (await leg1Detail.json()) as {
    home?: { entryId?: string };
  };
  if (!leg1Body.home?.entryId) {
    throw new Error(`Match ${leg1.matchId} missing home entry.`);
  }
  return {
    fixtureId: twoLeg.id,
    leg1MatchId: leg1.matchId,
    leg2MatchId: leg2.matchId,
    leg1HomeEntryId: leg1Body.home.entryId,
  };
}
