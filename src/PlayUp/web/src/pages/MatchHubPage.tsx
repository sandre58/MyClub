import { useQueries, useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import {
  fetchCompetitionOverview,
  fetchMatchesByStage,
  fetchNeedsAttention,
} from '../api'
import {
  BackLink,
  EmptyState,
  ErrorState,
  LoadingState,
  MatchStatusBadge,
} from '../queryUi'
import {
  formatScore,
  sideLabel,
  type CompetitionStageSummary,
  type MatchSummary,
  type NeedsAttentionItem,
} from '../types'

/**
 * Competition Match Hub — remaining 13.4.
 * Host has no competition-wide match list (V1 = stage-scoped).
 * Hub composes: overview stages + GET /stages/{id}/matches + GET …/attention.
 */
export function MatchHubPage() {
  const { competitionId = '' } = useParams()

  const overviewQuery = useQuery({
    queryKey: ['competitions', competitionId],
    queryFn: () => fetchCompetitionOverview(competitionId),
    enabled: competitionId.length > 0,
  })

  const attentionQuery = useQuery({
    queryKey: ['competitions', competitionId, 'attention'],
    queryFn: () => fetchNeedsAttention(competitionId),
    enabled: competitionId.length > 0,
  })

  const stages = overviewQuery.data?.stages ?? []

  const matchQueries = useQueries({
    queries: stages.map((stage) => ({
      queryKey: ['matches', 'by-stage', stage.stageId],
      queryFn: () => fetchMatchesByStage(stage.stageId),
      enabled: overviewQuery.isSuccess && stages.length > 0,
    })),
  })

  const matchesPending =
    overviewQuery.isSuccess &&
    stages.length > 0 &&
    matchQueries.some((query) => query.isPending)
  const matchesError = matchQueries.find((query) => query.error)?.error
  const pending =
    overviewQuery.isPending || attentionQuery.isPending || matchesPending
  const error = overviewQuery.error ?? attentionQuery.error ?? matchesError

  const rows = buildMatchRows(stages, matchQueries.map((query) => query.data))

  return (
    <main id="main" className="page">
      <header className="page__header">
        <p className="eyebrow">Match hub</p>
        <h1>
          {overviewQuery.data
            ? `${overviewQuery.data.name} · matches`
            : 'Matches'}
        </h1>
        {competitionId && (
          <BackLink to={`/competitions/${competitionId}`}>
            ← Back to workspace
          </BackLink>
        )}
      </header>

      {pending && <LoadingState />}
      {error && <ErrorState error={error} />}
      {!pending && !error && overviewQuery.data && (
        <article className="panel">
          <AttentionSection
            items={attentionQuery.data?.items ?? []}
            matches={rows}
          />
          <MatchHubList rows={rows} stages={stages} />
        </article>
      )}
    </main>
  )
}

interface MatchHubRow {
  match: MatchSummary
  stageName: string
}

function buildMatchRows(
  stages: CompetitionStageSummary[],
  matchLists: (MatchSummary[] | undefined)[],
): MatchHubRow[] {
  const rows: MatchHubRow[] = []
  stages.forEach((stage, index) => {
    const matches = matchLists[index]
    if (!matches) {
      return
    }
    for (const match of matches) {
      rows.push({ match, stageName: stage.name })
    }
  })
  return rows
}

function AttentionSection({
  items,
  matches,
}: {
  items: NeedsAttentionItem[]
  matches: MatchHubRow[]
}) {
  if (items.length === 0) {
    return (
      <section>
        <h2 className="section-title">Attention</h2>
        <EmptyState>Nothing needs attention right now.</EmptyState>
      </section>
    )
  }

  return (
    <section>
      <h2 className="section-title">Attention ({items.length})</h2>
      <ul className="entity-list">
        {items.map((item) => {
          const href = attentionHref(item, matches)
          const meta = (
            <span className="muted">
              {item.severity} · {item.source}
              {item.targetType ? ` · ${item.targetType}` : ''}
            </span>
          )
          return (
            <li
              key={`${item.source}:${item.targetType}:${item.targetId}:${item.reason}`}
              className="entity-list__item"
            >
              {href ? (
                <Link className="entity-list__link" to={href}>
                  <span className="entity-list__title">{item.reason}</span>
                  {meta}
                </Link>
              ) : (
                <div className="entity-list__link">
                  <span className="entity-list__title">{item.reason}</span>
                  {meta}
                </div>
              )}
            </li>
          )
        })}
      </ul>
    </section>
  )
}

function attentionHref(
  item: NeedsAttentionItem,
  matches: MatchHubRow[],
): string | null {
  if (!item.targetId) {
    return null
  }

  if (item.targetType === 'Stage') {
    return `/stages/${item.targetId}`
  }

  if (item.targetType === 'Slot') {
    const stageId = item.targetId.split(':')[0]
    return stageId ? `/stages/${stageId}` : null
  }

  if (item.targetType === 'Fixture') {
    const row = matches.find(
      (candidate) => candidate.match.fixtureId === item.targetId,
    )
    return row ? `/matches/${row.match.matchId}` : null
  }

  return null
}

function MatchHubList({
  rows,
  stages,
}: {
  rows: MatchHubRow[]
  stages: CompetitionStageSummary[]
}) {
  return (
    <section>
      <h2 className="section-title">All matches</h2>
      {stages.length === 0 ? (
        <EmptyState>
          No stages yet. Configure organisation structure first.
        </EmptyState>
      ) : rows.length === 0 ? (
        <EmptyState>
          No matches attached yet. Open a stage to prepare draws and materialize
          matches.
        </EmptyState>
      ) : (
        <ul className="match-list">
          {rows.map(({ match, stageName }) => (
            <li key={match.matchId} className="match-list__item">
              <Link
                to={`/matches/${match.matchId}`}
                className="match-list__link"
              >
                <span className="match-list__sides">
                  {sideLabel(match.home)} vs {sideLabel(match.away)}
                </span>
                <span className="match-list__meta">
                  <span className="muted">{stageName}</span>
                  <MatchStatusBadge status={match.status} />
                  {match.score ? (
                    <span className="match-list__score">
                      {formatScore(match.score)}
                    </span>
                  ) : null}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
