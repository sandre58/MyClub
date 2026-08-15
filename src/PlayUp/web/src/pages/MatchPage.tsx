import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { fetchMatchDetail, fetchStageOverview } from '../api'
import { BackLink, ErrorState, LoadingState } from '../queryUi'
import {
  formatScore,
  matchStatusLabel,
  resultTypeLabel,
  sideLabel,
  type MatchDetail,
} from '../types'

export function MatchPage() {
  const { matchId = '' } = useParams()

  const matchQuery = useQuery({
    queryKey: ['matches', matchId],
    queryFn: () => fetchMatchDetail(matchId),
    enabled: matchId.length > 0,
  })

  const stageId = matchQuery.data?.stageId
  const stageQuery = useQuery({
    queryKey: ['stages', stageId ?? ''],
    queryFn: () => fetchStageOverview(stageId!),
    enabled: Boolean(stageId),
  })

  return (
    <main className="page">
      <header className="page__header">
        <p className="eyebrow">Match</p>
        <h1>
          {matchQuery.data
            ? `${sideLabel(matchQuery.data.home)} vs ${sideLabel(matchQuery.data.away)}`
            : 'Detail'}
        </h1>
        {stageId && (
          <BackLink to={`/stages/${stageId}/matches`}>
            ← Back to{' '}
            {stageQuery.data?.name
              ? `${stageQuery.data.name} matches`
              : 'match list'}
          </BackLink>
        )}
      </header>

      {matchQuery.isPending && <LoadingState />}
      {matchQuery.isError && <ErrorState error={matchQuery.error} />}
      {matchQuery.data && (
        <MatchDetailView
          data={matchQuery.data}
          stageName={stageQuery.data?.name}
        />
      )}
    </main>
  )
}

function MatchDetailView({
  data,
  stageName,
}: {
  data: MatchDetail
  stageName?: string
}) {
  return (
    <article className="panel">
      <header className="panel__header">
        <p className="match-headline">
          {sideLabel(data.home)}{' '}
          <span className="muted">vs</span> {sideLabel(data.away)}
        </p>
        <p>
          Status: <strong>{matchStatusLabel[data.status]}</strong>
        </p>
        <p className="mono">{data.matchId}</p>
      </header>

      <section>
        <h3>Result</h3>
        {data.result ? (
          <ul className="plain-list">
            <li>
              Score:{' '}
              <strong>
                {formatScore({
                  homeGoals: data.result.homeGoals,
                  awayGoals: data.result.awayGoals,
                })}
              </strong>
            </li>
            <li>Type: {resultTypeLabel[data.result.type]}</li>
            <li>
              Extra time:{' '}
              {data.result.extraTimePlayed ? 'Yes' : 'No'}
            </li>
            {data.result.shootout && (
              <li>Shootout: {formatScore(data.result.shootout)}</li>
            )}
          </ul>
        ) : (
          <p className="hint">No result yet (match not finished).</p>
        )}
      </section>

      <section>
        <h3>Context</h3>
        <ul className="plain-list">
          <li>
            Stage:{' '}
            {stageName ?? (
              <span className="mono">{data.stageId}</span>
            )}
          </li>
          <li>
            Competition: <span className="mono">{data.competitionId}</span>
          </li>
          {data.fixtureId && (
            <li>
              Fixture: <span className="mono">{data.fixtureId}</span>
              {data.legIndex != null ? ` · leg ${data.legIndex}` : ''}
            </li>
          )}
        </ul>
      </section>
    </article>
  )
}
