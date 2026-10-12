import { QueryClient } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';

import { queryKeys } from '../../queryKeys';
import { invalidateAfterStructureMutation } from './structureInvalidation';

describe('invalidateAfterStructureMutation', () => {
  const competitionId = '11111111-1111-1111-1111-111111111111';
  const stageId = '22222222-2222-2222-2222-222222222222';

  it('invalidates attention alongside overview after structure mutations', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    await invalidateAfterStructureMutation(queryClient, competitionId, {
      stageId,
    });

    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.competitions.attention(competitionId),
    });
    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.competitions.overview(competitionId),
    });
  });
});
