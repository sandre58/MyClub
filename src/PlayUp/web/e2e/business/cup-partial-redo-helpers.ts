import type { APIRequestContext } from '@playwright/test';

import { getPrimaryStageId, playUpApiBase } from './hostSeed';

type StageOverview = {
  slots: { slotKey: string; entryId: string | null }[];
};

type DrawSummary = { drawId: string };

type RosterEntry = { entryId: string };

async function readRoster(
  request: APIRequestContext,
  competitionId: string,
): Promise<RosterEntry[]> {
  const response = await request.get(
    `${playUpApiBase()}/competitions/${competitionId}/structure`,
  );
  if (!response.ok()) {
    throw new Error(`GET structure failed (${response.status()})`);
  }
  const view = (await response.json()) as {
    participants?: { entries?: RosterEntry[] };
  };
  return view.participants?.entries ?? [];
}

export async function applyCupPartialDrawAfterDirectAssignment(
  request: APIRequestContext,
  competitionId: string,
): Promise<{ stageId: string; manualSlotKey: string; manualEntryId: string }> {
  const stageId = await getPrimaryStageId(request, competitionId);
  const overviewResponse = await request.get(
    `${playUpApiBase()}/stages/${stageId}`,
  );
  if (!overviewResponse.ok()) {
    throw new Error(
      `GET /stages/${stageId} failed (${overviewResponse.status()})`,
    );
  }

  const overview = (await overviewResponse.json()) as StageOverview;
  const manualSlot = overview.slots.find((slot) => slot.entryId == null);
  const placedEntryIds = new Set(
    overview.slots
      .map((slot) => slot.entryId)
      .filter((entryId): entryId is string => entryId != null),
  );
  const roster = await readRoster(request, competitionId);
  const manualEntry = roster.find(
    (entry) => entry.entryId && !placedEntryIds.has(entry.entryId),
  )?.entryId;
  if (!manualSlot || !manualEntry) {
    throw new Error('Cup stage needs a vacant slot and a composition entry');
  }

  const assignment = await request.put(
    `${playUpApiBase()}/stages/${stageId}/slots/${encodeURIComponent(manualSlot.slotKey)}/assignment`,
    { data: { entryId: manualEntry, expectedEntryId: null } },
  );
  if (!assignment.ok()) {
    throw new Error(
      `PUT slot assignment failed (${assignment.status()}): ${await assignment.text()}`,
    );
  }

  const create = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws`,
    { data: { kind: 'Slot', intent: 'Partial' } },
  );
  if (!create.ok()) {
    throw new Error(`POST draw failed (${create.status()})`);
  }
  const draw = (await create.json()) as DrawSummary;

  const inputs = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws/${draw.drawId}/inputs`,
    { data: { intent: 'Partial' } },
  );
  if (!inputs.ok()) {
    throw new Error(`POST inputs failed (${inputs.status()})`);
  }

  const generate = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws/${draw.drawId}/generate`,
  );
  if (!generate.ok()) {
    throw new Error(`POST generate failed (${generate.status()})`);
  }

  const publishAndApply = await request.post(
    `${playUpApiBase()}/stages/${stageId}/draws/${draw.drawId}/publish-and-apply`,
  );
  if (!publishAndApply.ok()) {
    throw new Error(
      `POST publish-and-apply failed (${publishAndApply.status()})`,
    );
  }

  return {
    stageId,
    manualSlotKey: manualSlot.slotKey,
    manualEntryId: manualEntry,
  };
}
