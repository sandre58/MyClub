import { QueryClient } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';

import { queryKeys } from '../../queryKeys';
import {
  invalidateAfterMatchMutation,
  matchMutationQueryKeys,
  type MatchMutationTarget,
} from './matchInvalidation';

const target: MatchMutationTarget = {
  matchId: '11111111-1111-1111-1111-111111111111',
  stageId: '22222222-2222-2222-2222-222222222222',
  competitionId: '33333333-3333-3333-3333-333333333333',
};

describe('matchInvalidation', () => {
  it('lists every view that displays match facts', () => {
    expect(matchMutationQueryKeys(target)).toEqual([
      queryKeys.matches.detail(target.matchId),
      queryKeys.matches.byStage(target.stageId),
      queryKeys.competitions.matchHub(target.competitionId),
      queryKeys.competitions.structure(target.competitionId),
      queryKeys.stages.detail(target.stageId),
      ['stages'],
      queryKeys.competitions.overview(target.competitionId),
      queryKeys.competitions.consultation(target.competitionId),
      queryKeys.competitions.attention(target.competitionId),
    ]);
  });

  it('marks fiche, lists, standings, structure, and both overviews stale', async () => {
    const queryClient = new QueryClient();
    for (const queryKey of matchMutationQueryKeys(target)) {
      queryClient.setQueryData(queryKey, { fresh: true });
    }

    await invalidateAfterMatchMutation(queryClient, target);

    for (const queryKey of matchMutationQueryKeys(target)) {
      expect(queryClient.getQueryState(queryKey)?.isInvalidated).toBe(true);
    }
  });
});
