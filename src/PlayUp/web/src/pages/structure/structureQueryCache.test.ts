import { QueryClient } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';

import { queryKeys } from '../../queryKeys';
import type { StructureView } from '../../types';
import {
  patchDrawRulesActivatedOptimistic,
  patchStructureViewStage,
  reconcileStructureHubPatch,
  beginStructureHubPatch,
} from './structureQueryCache';

const competitionId = '11111111-1111-1111-1111-111111111111';
const stageId = '22222222-2222-2222-2222-222222222222';

function structureView(): StructureView {
  return {
    competitionId,
    stages: [
      {
        stageId,
        name: 'Cup',
        hasDrawRules: false,
      } as StructureView['stages'][number],
    ],
  } as StructureView;
}

describe('structureQueryCache', () => {
  it('patches a stage field on the hub view', () => {
    const queryClient = new QueryClient();
    queryClient.setQueryData(
      queryKeys.competitions.structure(competitionId),
      structureView(),
    );

    patchStructureViewStage(queryClient, competitionId, stageId, {
      hasDrawRules: true,
    });

    const next = queryClient.getQueryData<StructureView>(
      queryKeys.competitions.structure(competitionId),
    );
    expect(next?.stages[0]?.hasDrawRules).toBe(true);
  });

  it('rolls back hub patch when reconciliation fails', async () => {
    const queryClient = new QueryClient();
    const view = structureView();
    queryClient.setQueryData(
      queryKeys.competitions.structure(competitionId),
      view,
    );
    const session = beginStructureHubPatch(
      queryClient,
      competitionId,
      ['hub'],
      stageId,
    );
    patchStructureViewStage(queryClient, competitionId, stageId, {
      name: 'Patched',
    });
    vi.spyOn(queryClient, 'invalidateQueries').mockRejectedValue(
      new Error('fail'),
    );

    const ok = await reconcileStructureHubPatch(queryClient, session);

    expect(ok).toBe(false);
    expect(
      queryClient.getQueryData<StructureView>(
        queryKeys.competitions.structure(competitionId),
      )?.stages[0]?.name,
    ).toBe('Cup');
  });

  it('optimistic activate draw sets hasDrawRules only', () => {
    const queryClient = new QueryClient();
    queryClient.setQueryData(
      queryKeys.competitions.structure(competitionId),
      structureView(),
    );
    vi.spyOn(queryClient, 'invalidateQueries').mockResolvedValue(undefined);

    patchDrawRulesActivatedOptimistic(queryClient, competitionId, stageId, 4);

    const next = queryClient.getQueryData<StructureView>(
      queryKeys.competitions.structure(competitionId),
    );
    expect(next?.stages[0]?.hasDrawRules).toBe(true);
    expect(next?.stages[0]?.numberOfPots).toBe(4);
  });
});
