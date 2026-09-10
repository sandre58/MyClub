import type { QueryClient } from '@tanstack/react-query';
import { queryKeys } from '../queryKeys';

export async function invalidateAfterStructureMutation(
  queryClient: QueryClient,
  competitionId: string,
  options?: { stageId?: string },
) {
  await Promise.all([
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.structure(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.detail(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.workspace(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.overview(competitionId),
    }),
    queryClient.invalidateQueries({
      queryKey: queryKeys.competitions.consultation(competitionId),
    }),
    ...(options?.stageId
      ? [
          queryClient.invalidateQueries({
            queryKey: queryKeys.stages.detail(options.stageId),
          }),
        ]
      : []),
  ]);
}
