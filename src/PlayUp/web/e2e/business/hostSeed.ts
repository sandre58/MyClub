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
