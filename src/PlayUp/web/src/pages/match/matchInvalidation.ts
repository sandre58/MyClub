import type { QueryClient, QueryKey } from '@tanstack/react-query';
import { queryKeys } from '../../queryKeys';

/**
 * Identifiers of the match that just changed.
 * Every match command (score, status, sheet, recorded events) uses this shape.
 */
export type MatchMutationTarget = {
  matchId: string;
  stageId: string;
  competitionId: string;
};

const INVALIDATE_OPTIONS = {
  refetchType: 'active' as const,
};

/**
 * Reads that display facts derived from a match.
 * One list so a command cannot refresh the fiche and leave lists,
 * standings, or overviews fresh for the global staleTime.
 */
export function matchMutationQueryKeys(
  target: MatchMutationTarget,
): QueryKey[] {
  const { matchId, stageId, competitionId } = target;
  return [
    queryKeys.matches.detail(matchId),
    queryKeys.matches.byStage(stageId),
    queryKeys.competitions.matchHub(competitionId),
    queryKeys.competitions.structure(competitionId),
    queryKeys.stages.detail(stageId),
    ['stages'],
    queryKeys.competitions.overview(competitionId),
    queryKeys.competitions.consultation(competitionId),
    queryKeys.competitions.attention(competitionId),
  ];
}

export async function invalidateAfterMatchMutation(
  queryClient: QueryClient,
  target: MatchMutationTarget,
): Promise<void> {
  await Promise.all(
    matchMutationQueryKeys(target).map((queryKey) =>
      queryClient.invalidateQueries({
        queryKey,
        ...INVALIDATE_OPTIONS,
      }),
    ),
  );
}
