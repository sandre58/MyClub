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

type StructureView = {
  format: { primaryStageId?: string | null };
  stages: { stageId: string; drawExecutionBadge?: string | null }[];
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

export async function runCupSlotDrawPublishAndApply(
  request: APIRequestContext,
  stageId: string,
): Promise<void> {
  const create = await request.post(`${playUpApiBase()}/stages/${stageId}/draws`, {
    data: { kind: 'Slot' },
  });
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
    throw new Error(
      `POST .../inputs failed (${inputs.status()}): ${body}`,
    );
  }

  const generate = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws/${draw.drawId}/generate`,
  );
  if (!generate.ok()) {
    const body = await generate.text();
    throw new Error(
      `POST .../generate failed (${generate.status()}): ${body}`,
    );
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

export async function expectCupDrawAppliedOnStructure(
  request: APIRequestContext,
  competitionId: string,
): Promise<void> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `GET structure failed (${response.status()}): ${body}`,
    );
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
