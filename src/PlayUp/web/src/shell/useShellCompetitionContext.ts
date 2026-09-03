import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import {
  fetchCompetitionDetail,
  fetchCompetitions,
  fetchMatchDetail,
  fetchStageOverview,
} from '../api'
import { queryKeys } from '../queryKeys'

export type ShellCompetitionContextState =
  | 'selected'
  | 'choose'
  | 'empty'
  | 'loading'
  | 'unavailable'

/**
 * Resolves the active competition for shell chrome only.
 * Reuses TanStack Query keys shared with business pages to avoid duplicate fetches.
 */
export function useShellCompetitionContext() {
  const { competitionId: routeCompetitionId, stageId, matchId } = useParams()

  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId ?? ''),
    queryFn: () => fetchStageOverview(stageId!),
    enabled: Boolean(stageId) && !routeCompetitionId,
  })

  const matchQuery = useQuery({
    queryKey: queryKeys.matches.detail(matchId ?? ''),
    queryFn: () => fetchMatchDetail(matchId!),
    enabled: Boolean(matchId) && !routeCompetitionId,
  })

  const resolvedCompetitionId =
    routeCompetitionId ??
    stageQuery.data?.competitionId ??
    matchQuery.data?.competitionId

  const overviewQuery = useQuery({
    queryKey: queryKeys.competitions.detail(resolvedCompetitionId ?? ''),
    queryFn: () => fetchCompetitionDetail(resolvedCompetitionId!),
    enabled: Boolean(resolvedCompetitionId),
  })

  const isResolvingDeepLink =
    (Boolean(stageId) && !routeCompetitionId && stageQuery.isPending) ||
    (Boolean(matchId) && !routeCompetitionId && matchQuery.isPending)

  const competitionsListQuery = useQuery({
    queryKey: queryKeys.competitions.all,
    queryFn: fetchCompetitions,
    enabled: !resolvedCompetitionId && !isResolvingDeepLink,
  })

  let state: ShellCompetitionContextState
  if (isResolvingDeepLink && !resolvedCompetitionId) {
    state = 'loading'
  } else if (resolvedCompetitionId) {
    if (isResolvingDeepLink || overviewQuery.isPending) {
      state = 'loading'
    } else if (overviewQuery.isError || !overviewQuery.data?.name) {
      state = 'unavailable'
    } else {
      state = 'selected'
    }
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
    logoMediaId: overviewQuery.data?.logoMediaId ?? null,
    status: overviewQuery.data?.status,
    scheduledStart: overviewQuery.data?.scheduledStart ?? null,
    scheduledEnd: overviewQuery.data?.scheduledEnd ?? null,
    state,
  }
}
