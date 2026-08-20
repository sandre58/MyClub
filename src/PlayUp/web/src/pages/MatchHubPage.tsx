import { useQueries, useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import {
  fetchCompetitionOverview,
  fetchMatchesByStage,
  fetchNeedsAttention,
} from '../api'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  ErrorState,
  LoadingState,
  MatchStatusBadge,
  PageHeader,
  StatusBadge,
  type StatusTone,
} from '../ui'
import {
  formatScore,
  sideLabel,
  type CompetitionStageSummary,
  type MatchStatus,
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
    queryKey: queryKeys.competitions.detail(competitionId),
    queryFn: () => fetchCompetitionOverview(competitionId),
    enabled: competitionId.length > 0,
  })

  const attentionQuery = useQuery({
    queryKey: queryKeys.competitions.attention(competitionId),
    queryFn: () => fetchNeedsAttention(competitionId),
    enabled: competitionId.length > 0,
  })

  const stages = overviewQuery.data?.stages ?? []

  const matchQueries = useQueries({
    queries: stages.map((stage) => ({
      queryKey: queryKeys.matches.byStage(stage.stageId),
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
      <PageHeader
        eyebrow="Match hub"
        title="Matches"
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}`,
                label: 'Back to workspace',
              }
            : undefined
        }
      />

      {pending && <LoadingState />}
      {error && <ErrorState error={error} />}
      {!pending && !error && overviewQuery.data && (
        <div className="section-stack">
          <AttentionSection
            items={attentionQuery.data?.items ?? []}
            matches={rows}
          />
          <MatchHubList rows={rows} stages={stages} />
        </div>
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
      <section className="card" aria-labelledby="attention-heading">
        <div className="card__head">
          <h2 className="card__title" id="attention-heading">
            Attention
          </h2>
          <span className="status-badge status-badge--ok">
            <span className="status-badge__dot" aria-hidden="true" />
            Clear
          </span>
        </div>
        <EmptyState title="Nothing needs attention right now">
          Items appear here when the Host reports a blocked or pending step.
        </EmptyState>
      </section>
    )
  }

  return (
    <section className="card" aria-labelledby="attention-heading">
      <div className="card__head">
        <h2 className="card__title" id="attention-heading">
          Attention
        </h2>
        <span className="status-badge status-badge--warn">
          <span className="status-badge__dot" aria-hidden="true" />
          {items.length} to review
        </span>
      </div>
      <ul className="row-list">
        {items.map((item) => {
          const href = attentionHref(item, matches)
          const body = (
            <>
              <span className="row__main">
                <span className="row__title">{item.reason}</span>
                <span className="row__meta">
                  {item.source}
                  {item.targetType ? ` · ${item.targetType}` : ''}
                </span>
              </span>
              <span className="row__aside">
                <StatusBadge tone={severityTone(item.severity)}>
                  {item.severity}
                </StatusBadge>
                {href && (
                  <span className="row__chevron" aria-hidden="true">
                    →
                  </span>
                )}
              </span>
            </>
          )

          return (
            <li
              key={`${item.source}:${item.targetType}:${item.targetId}:${item.reason}`}
            >
              {href ? (
                <Link className="row" to={href}>
                  {body}
                </Link>
              ) : (
                <div className="row">{body}</div>
              )}
            </li>
          )
        })}
      </ul>
    </section>
  )
}

/** Host severity strings are free-form; map the known ones, stay neutral otherwise. */
function severityTone(severity: string): StatusTone {
  switch (severity.toLowerCase()) {
    case 'blocking':
    case 'error':
      return 'danger'
    case 'warning':
      return 'warn'
    default:
      return 'info'
  }
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

/** Reading order for the organizer: what is running, then what is next. */
const matchGroups: { title: string; statuses: MatchStatus[] }[] = [
  { title: 'Live now', statuses: ['Live'] },
  { title: 'Upcoming', statuses: ['Scheduled'] },
  { title: 'Finished', statuses: ['Finished'] },
  { title: 'Postponed or cancelled', statuses: ['Postponed', 'Cancelled'] },
]

function MatchHubList({
  rows,
  stages,
}: {
  rows: MatchHubRow[]
  stages: CompetitionStageSummary[]
}) {
  return (
    <section className="card" aria-labelledby="matches-heading">
      <div className="card__head">
        <h2 className="card__title" id="matches-heading">
          Matches
        </h2>
        {rows.length > 0 && (
          <p className="card__subtitle">
            {rows.length} match{rows.length === 1 ? '' : 'es'} across{' '}
            {stages.length} stage{stages.length === 1 ? '' : 's'}
          </p>
        )}
      </div>

      {stages.length === 0 ? (
        <EmptyState title="No stages yet">
          No stages yet. Configure organisation structure first.
        </EmptyState>
      ) : rows.length === 0 ? (
        <EmptyState title="No matches yet">
          No matches attached yet. Open a stage to prepare draws and materialize
          matches.
        </EmptyState>
      ) : (
        <div className="section-stack">
          {matchGroups.map((group) => {
            const groupRows = rows.filter((row) =>
              group.statuses.includes(row.match.status),
            )
            if (groupRows.length === 0) {
              return null
            }

            return (
              <div className="match-group" key={group.title}>
                <h3 className="match-group__title">
                  {group.title}
                  <span className="match-group__count">
                    {groupRows.length}
                  </span>
                </h3>
                <ul className="row-list">
                  {groupRows.map(({ match, stageName }) => (
                    <li key={match.matchId}>
                      <Link
                        to={`/matches/${match.matchId}`}
                        className="match-row"
                      >
                        <span className="row__main">
                          <span className="match-row__sides">
                            {sideLabel(match.home)} vs {sideLabel(match.away)}
                          </span>
                          <span className="match-row__meta">
                            <span>{stageName}</span>
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
              </div>
            )
          })}
        </div>
      )}
    </section>
  )
}
