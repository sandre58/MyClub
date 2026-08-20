import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchMatchesByStage, fetchStageOverview } from '../api'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MatchStatusBadge,
  PageHeader,
} from '../ui'
import { formatScore, sideLabel, type MatchSummary } from '../types'

export function StageMatchesPage() {
  const { stageId = '' } = useParams()

  // Stage overview shares queryKeys.stages.detail with StagePage (cache when coming from there).
  const stageQuery = useQuery({
    queryKey: queryKeys.stages.detail(stageId),
    queryFn: () => fetchStageOverview(stageId),
    enabled: stageId.length > 0,
  })

  const matchesQuery = useQuery({
    queryKey: queryKeys.matches.byStage(stageId),
    queryFn: () => fetchMatchesByStage(stageId),
    enabled: stageId.length > 0,
  })

  const pending = stageQuery.isPending || matchesQuery.isPending
  const error = stageQuery.error ?? matchesQuery.error

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow="Matches"
        title={
          stageQuery.data
            ? `${stageQuery.data.name} · matches`
            : 'Stage matches'
        }
        back={
          stageId
            ? { to: `/stages/${stageId}`, label: 'Back to stage' }
            : undefined
        }
      />

      {pending && <LoadingState />}
      {error && <ErrorState error={error} />}
      {matchesQuery.data && <MatchList matches={matchesQuery.data} />}
    </main>
  )
}

function MatchList({ matches }: { matches: MatchSummary[] }) {
  if (matches.length === 0) {
    return (
      <EmptyState title="No matches for this stage yet">
        Structure may exist without attached matches. Prepare the draw on the
        stage to materialize them.
      </EmptyState>
    )
  }

  return (
    <ul className="row-list">
      {matches.map((match) => (
        <li key={match.matchId}>
          <Link to={`/matches/${match.matchId}`} className="match-row">
            <span className="row__main">
              <span className="match-row__sides">
                {sideLabel(match.home)} vs {sideLabel(match.away)}
              </span>
            </span>
            <span className="match-row__aside">
              {match.score ? (
                <span className="match-row__score">
                  {formatScore(match.score)}
                </span>
              ) : null}
              <MatchStatusBadge status={match.status} />
              <span className="row__chevron" aria-hidden="true">
                →
              </span>
            </span>
          </Link>
        </li>
      ))}
    </ul>
  )
}
