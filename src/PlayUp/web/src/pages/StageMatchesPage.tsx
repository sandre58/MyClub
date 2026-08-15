import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { fetchMatchesByStage, fetchStageOverview } from '../api'
import { BackLink, EmptyState, ErrorState, LoadingState } from '../queryUi'
import {
  formatScore,
  matchStatusLabel,
  sideLabel,
  type MatchSummary,
} from '../types'

export function StageMatchesPage() {
  const { stageId = '' } = useParams()

  // Stage overview shares ['stages', stageId] with StagePage (cache when coming from there).
  const stageQuery = useQuery({
    queryKey: ['stages', stageId],
    queryFn: () => fetchStageOverview(stageId),
    enabled: stageId.length > 0,
  })

  const matchesQuery = useQuery({
    queryKey: ['matches', 'by-stage', stageId],
    queryFn: () => fetchMatchesByStage(stageId),
    enabled: stageId.length > 0,
  })

  const pending = stageQuery.isPending || matchesQuery.isPending
  const error = stageQuery.error ?? matchesQuery.error

  return (
    <main className="page">
      <header className="page__header">
        <p className="eyebrow">Matches</p>
        <h1>
          {stageQuery.data
            ? `${stageQuery.data.name} · matches`
            : 'Stage matches'}
        </h1>
        {stageId && (
          <BackLink to={`/stages/${stageId}`}>← Back to stage</BackLink>
        )}
      </header>

      {pending && <LoadingState />}
      {error && <ErrorState error={error} />}
      {matchesQuery.data && <MatchList matches={matchesQuery.data} />}
    </main>
  )
}

function MatchList({ matches }: { matches: MatchSummary[] }) {
  if (matches.length === 0) {
    return (
      <EmptyState>
        No matches for this stage yet. Structure may exist without attached
        matches.
      </EmptyState>
    )
  }

  return (
    <ul className="match-list">
      {matches.map((match) => (
        <li key={match.matchId} className="match-list__item">
          <Link to={`/matches/${match.matchId}`} className="match-list__link">
            <span className="match-list__sides">
              {sideLabel(match.home)} vs {sideLabel(match.away)}
            </span>
            <span className="match-list__meta">
              {matchStatusLabel[match.status]}
              {match.score ? ` · ${formatScore(match.score)}` : ''}
            </span>
          </Link>
        </li>
      ))}
    </ul>
  )
}
