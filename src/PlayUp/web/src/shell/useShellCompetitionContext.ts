import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import {
  fetchCompetitionOverview,
  fetchCompetitions,
  fetchMatchDetail,
  fetchStageOverview,
} from '../api'

export type ShellCompetitionContextState =
  | 'selected'
  | 'choose'
  | 'empty'
  | 'loading'

/**
 * Resolves the active competition for shell chrome only.
 * Reuses TanStack Query keys shared with business pages to avoid duplicate fetches.
 */
export function useShellCompetitionContext() {
  const { competitionId: routeCompetitionId, stageId, matchId } = useParams()

  const stageQuery = useQuery({
    queryKey: ['stages', stageId ?? ''],
    queryFn: () => fetchStageOverview(stageId!),
    enabled: Boolean(stageId) && !routeCompetitionId,
  })

  const matchQuery = useQuery({
    queryKey: ['matches', matchId ?? ''],
    queryFn: () => fetchMatchDetail(matchId!),
    enabled: Boolean(matchId) && !routeCompetitionId,
  })

  const resolvedCompetitionId =
    routeCompetitionId ??
    stageQuery.data?.competitionId ??
    matchQuery.data?.competitionId

  const overviewQuery = useQuery({
    queryKey: ['competitions', resolvedCompetitionId ?? ''],
    queryFn: () => fetchCompetitionOverview(resolvedCompetitionId!),
    enabled: Boolean(resolvedCompetitionId),
  })

  const competitionsListQuery = useQuery({
    queryKey: ['competitions'],
    queryFn: fetchCompetitions,
    enabled: !resolvedCompetitionId,
  })

  const isResolvingDeepLink =
    (Boolean(stageId) && !routeCompetitionId && stageQuery.isPending) ||
    (Boolean(matchId) && !routeCompetitionId && matchQuery.isPending)

  const isLoadingSelected =
    Boolean(resolvedCompetitionId) &&
    (isResolvingDeepLink || overviewQuery.isPending)

  let state: ShellCompetitionContextState
  if (resolvedCompetitionId) {
    state = isLoadingSelected ? 'loading' : 'selected'
  } else if (competitionsListQuery.isPending) {
    state = 'loading'
  } else if (competitionsListQuery.data?.length === 0) {
    state = 'empty'
  } else {
    state = 'choose'
  }

  return {
    competitionId: resolvedCompetitionId,
    competitionName: overviewQuery.data?.name,
    state,
  }
}
