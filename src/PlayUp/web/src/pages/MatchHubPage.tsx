import { useQueries, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
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
import { situationMeta, situationTitle } from '../i18n/situationCopy'
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
  const { t } = useTranslation('matches')

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
        eyebrow={t('eyebrow')}
        title={t('title')}
        back={
          competitionId
            ? {
                to: `/competitions/${competitionId}`,
                label: t('back'),
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
  const { t } = useTranslation(['matches', 'enums'])

  if (items.length === 0) {
    return (
      <section className="card" aria-labelledby="attention-heading">
        <div className="card__head">
          <h2 className="card__title" id="attention-heading">
            {t('attention.heading')}
          </h2>
          <span className="status-badge status-badge--ok">
            <span className="status-badge__dot" aria-hidden="true" />
            {t('attention.clear')}
          </span>
        </div>
        <EmptyState title={t('attention.emptyTitle')}>
          {t('attention.emptyBody')}
        </EmptyState>
      </section>
    )
  }

  return (
    <section className="card" aria-labelledby="attention-heading">
      <div className="card__head">
        <h2 className="card__title" id="attention-heading">
          {t('attention.heading')}
        </h2>
        <span className="status-badge status-badge--warn">
          <span className="status-badge__dot" aria-hidden="true" />
          {t('attention.toReview', { count: items.length })}
        </span>
      </div>
      <ul className="row-list">
        {items.map((item) => {
          const href = attentionHref(item, matches)
          const title = situationTitle(item.source)
          const body = (
            <>
              <span className="row__main">
                <span className="row__title">{title}</span>
                <span className="row__meta">
                  {situationMeta(item.source, item.targetType)}
                </span>
              </span>
              <span className="row__aside">
                <StatusBadge tone={severityTone(item.severity)}>
                  {t(`attentionSeverity.${item.severity}`, {
                    ns: 'enums',
                    defaultValue: item.severity,
                  })}
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
              key={`${item.source}:${item.targetType}:${item.targetId}`}
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

function MatchHubList({
  rows,
  stages,
}: {
  rows: MatchHubRow[]
  stages: CompetitionStageSummary[]
}) {
  const { t } = useTranslation('matches')

  const matchGroups: { titleKey: string; statuses: MatchStatus[] }[] = [
    { titleKey: 'list.groupLive', statuses: ['Live'] },
    { titleKey: 'list.groupUpcoming', statuses: ['Scheduled'] },
    { titleKey: 'list.groupFinished', statuses: ['Finished'] },
    { titleKey: 'list.groupOther', statuses: ['Postponed', 'Cancelled'] },
  ]

  return (
    <section className="card" aria-labelledby="matches-heading">
      <div className="card__head">
        <h2 className="card__title" id="matches-heading">
          {t('list.heading')}
        </h2>
        {rows.length > 0 && (
          <p className="card__subtitle">
            {t('list.subtitle', { count: rows.length, stages: stages.length })}
          </p>
        )}
      </div>

      {stages.length === 0 ? (
        <EmptyState title={t('list.noStagesTitle')}>
          {t('list.noStagesBody')}
        </EmptyState>
      ) : rows.length === 0 ? (
        <EmptyState title={t('list.noMatchesTitle')}>
          {t('list.noMatchesBody')}
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
              <div className="match-group" key={group.titleKey}>
                <h3 className="match-group__title">
                  {t(group.titleKey)}
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
