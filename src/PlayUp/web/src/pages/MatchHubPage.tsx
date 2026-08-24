import { useQueries, useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { fetchCompetitionOverview, fetchMatchesByStage } from '../api'
import {
  ClassementsNavIcon,
  MatchesNavIcon,
} from '../design-system/icons/shellIcons'
import { competitionStatusLabel } from '../i18n/enumLabels'
import { queryKeys } from '../queryKeys'
import {
  EmptyState,
  ErrorState,
  LoadingState,
} from '../ui'
import {
  formatScore,
  sideLabel,
  type CompetitionOverview,
  type CompetitionStageSummary,
  type MatchStatus,
  type MatchSummary,
} from '../types'
import {
  matchResultTypeLabel,
  matchScheduledLabel,
  matchSportingContext,
} from './matchListMeta'
import './matches.css'

/**
 * Matchs workspace — overview stages + stage match lists.
 * Presents Read facts by journée (V3). No page-level « À traiter » (Shell drawer).
 */
export function MatchHubPage() {
  const { competitionId = '' } = useParams()

  const overviewQuery = useQuery({
    queryKey: queryKeys.competitions.detail(competitionId),
    queryFn: () => fetchCompetitionOverview(competitionId),
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
  const pending = overviewQuery.isPending
  const error = overviewQuery.error

  const rows = buildMatchRows(stages, matchQueries.map((query) => query.data))

  return (
    <main id="main" className="page page--matches">
      {pending && !overviewQuery.data && <LoadingState />}
      {error && !overviewQuery.data && <ErrorState error={error} />}
      {overviewQuery.data && (
        <MatchesView
          data={overviewQuery.data}
          rows={rows}
          stages={stages}
          matchesPending={matchesPending}
          matchesError={matchesError}
        />
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

function MatchesView({
  data,
  rows,
  stages,
  matchesPending,
  matchesError,
}: {
  data: CompetitionOverview
  rows: MatchHubRow[]
  stages: CompetitionStageSummary[]
  matchesPending: boolean
  matchesError: unknown
}) {
  const { t } = useTranslation('matches')
  const overviewHref = `/competitions/${data.id}`
  const classementsHref = `/competitions/${data.id}/classements`
  const buckets = groupMatchesBySportingBucket(rows, t)
  const needsResult = rows.filter(
    (row) => row.match.status === 'Finished' && row.match.score == null,
  )

  return (
    <div className="matches">
      <header className="matches__page-head">
        <Link className="matches__back" to={overviewHref}>
          <span aria-hidden="true">←</span>
          {t('back')}
        </Link>
        <h1 className="matches__title">{t('title')}</h1>
      </header>

      <ContextBand data={data} matchCount={rows.length} />

      {matchesPending ? <LoadingState label={t('loading')} /> : null}
      {matchesError ? <ErrorState error={matchesError} /> : null}
      {!matchesPending && !matchesError ? (
        <CalendarPanel
          buckets={buckets}
          stages={stages}
          matchCount={rows.length}
        />
      ) : null}

      {!matchesPending && !matchesError ? (
        <div className="matches__bottom">
          <NeedsResultPanel items={needsResult} />
          <ClassementsCrossLink href={classementsHref} rows={rows} />
        </div>
      ) : null}
    </div>
  )
}

function ContextBand({
  data,
  matchCount,
}: {
  data: CompetitionOverview
  matchCount: number
}) {
  const { t } = useTranslation('matches')
  const primaryStage = data.stages[0]

  return (
    <ul className="matches-band" aria-label={data.name}>
      <li className="matches-band__chip">{data.name}</li>
      <li className="matches-band__chip matches-band__chip--status">
        {competitionStatusLabel(data.status)}
      </li>
      {primaryStage ? (
        <li className="matches-band__chip matches-band__chip--muted">
          {primaryStage.name}
        </li>
      ) : null}
      <li className="matches-band__chip matches-band__chip--muted">
        {t('band.matches', { count: matchCount })}
      </li>
    </ul>
  )
}

function CalendarPanel({
  buckets,
  stages,
  matchCount,
}: {
  buckets: SportingBucket[]
  stages: CompetitionStageSummary[]
  matchCount: number
}) {
  const { t } = useTranslation('matches')

  return (
    <section className="ds-panel" aria-labelledby="matches-calendar">
      <PanelHead id="matches-calendar" icon={<MatchesNavIcon size="md" />}>
        {t('calendar.heading')}
      </PanelHead>
      <p className="matches-panel__meta">
        {t('calendar.meta', {
          count: matchCount,
          matches: matchCount,
          days: buckets.length,
          stages: stages.length,
        })}
      </p>

      {stages.length === 0 ? (
        <EmptyState title={t('list.noStagesTitle')}>
          {t('list.noStagesBody')}
        </EmptyState>
      ) : matchCount === 0 ? (
        <EmptyState title={t('list.noMatchesTitle')}>
          {t('list.noMatchesBody')}
        </EmptyState>
      ) : (
        buckets.map((bucket) => (
          <MatchdaySection key={bucket.key} bucket={bucket} />
        ))
      )}
    </section>
  )
}

function MatchdaySection({ bucket }: { bucket: SportingBucket }) {
  const { t } = useTranslation('matches')
  const status = journéeStatus(bucket.rows.map((row) => row.match.status))
  const breakdown = journéeBreakdown(bucket.rows.map((row) => row.match.status), t)

  return (
    <section className="matches-day" aria-labelledby={`day-${bucket.key}`}>
      <div className="matches-day__head">
        <h3 id={`day-${bucket.key}`} className="matches-day__title">
          {bucket.label}
        </h3>
        <span className="matches-day__status">{t(`calendar.dayStatus.${status}`)}</span>
      </div>
      {breakdown ? (
        <p className="matches-day__breakdown">{breakdown}</p>
      ) : null}

      <div className="matches-day__list">
        <div className="matches-day__list-head" aria-hidden="true">
          <span>{t('calendar.columns.match')}</span>
          <span>{t('calendar.columns.score')}</span>
        </div>
        <ul className="matches-day__rows">
          {bucket.rows.map(({ match, stageName }) => (
            <li key={match.matchId}>
              <MatchResultRow match={match} stageName={stageName} />
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}

function MatchResultRow({
  match,
  stageName,
}: {
  match: MatchSummary
  stageName: string
}) {
  const { t } = useTranslation('matches')
  const homeName = sideLabel(match.home)
  const awayName = sideLabel(match.away)
  const when = matchScheduledLabel(match)
  const resultKind = matchResultTypeLabel(match)
  const sporting = matchSportingContext(match, t)
  const needsResult = match.status === 'Finished' && match.score == null

  const asideLabel =
    match.score != null
      ? formatScore(match.score)
      : needsResult
        ? t('calendar.needsResult')
        : (when ?? t('calendar.pending'))

  return (
    <Link
      className="matches-result"
      to={`/matches/${match.matchId}`}
      aria-label={`${homeName} – ${awayName}, ${asideLabel}`}
    >
      <span className="matches-result__match">
        <span className="matches-result__team">
          <TeamCrest name={homeName} />
          <span className="matches-result__name">{homeName}</span>
        </span>
        <span className="matches-result__vs" aria-hidden="true">
          –
        </span>
        <span className="matches-result__team">
          <TeamCrest name={awayName} />
          <span className="matches-result__name">{awayName}</span>
        </span>
      </span>

      <span className="matches-result__aside">
        {match.score != null ? (
          <span className="matches-result__score">{formatScore(match.score)}</span>
        ) : needsResult ? (
          <span className="matches-result__pill">{t('calendar.needsResult')}</span>
        ) : match.status === 'Live' ? (
          <>
            <span className="matches-result__pill matches-result__pill--live">
              {t('calendar.live')}
            </span>
            {when ? <span className="matches-result__meta">{when}</span> : null}
          </>
        ) : match.status === 'Scheduled' ? (
          <>
            {when ? (
              <span className="matches-result__meta">{when}</span>
            ) : (
              <span className="matches-result__score matches-result__score--pending">
                {t('calendar.pending')}
              </span>
            )}
            <span className="matches-result__pill matches-result__pill--muted">
              {t('calendar.upcoming')}
            </span>
          </>
        ) : match.status === 'Postponed' || match.status === 'Cancelled' ? (
          <span className="matches-result__pill matches-result__pill--muted">
            {match.status === 'Postponed'
              ? t('calendar.postponed')
              : t('calendar.cancelled')}
          </span>
        ) : (
          <span className="matches-result__score matches-result__score--pending">
            {t('calendar.pending')}
          </span>
        )}
        {resultKind && match.status === 'Finished' ? (
          <span className="matches-result__meta">{resultKind}</span>
        ) : null}
        {sporting && match.roundName == null && match.matchdayNumber == null ? (
          <span className="matches-result__meta">{stageName}</span>
        ) : null}
        <span className="matches-result__chevron" aria-hidden="true">
          ›
        </span>
      </span>
    </Link>
  )
}

function NeedsResultPanel({ items }: { items: MatchHubRow[] }) {
  const { t } = useTranslation('matches')

  return (
    <section className="ds-panel" aria-labelledby="matches-needs-result">
      <PanelHead id="matches-needs-result" icon={<MatchesNavIcon size="md" />}>
        {t('needsResult.heading')}
      </PanelHead>
      {items.length === 0 ? (
        <p className="matches-panel__meta">{t('needsResult.clear')}</p>
      ) : (
        <>
          <p className="matches-panel__meta">
            {t('needsResult.count', { count: items.length })}
          </p>
          <ul className="matches-need__list">
            {items.slice(0, 5).map(({ match }) => (
              <li key={match.matchId} className="matches-need__item">
                <Link className="matches-link" to={`/matches/${match.matchId}`}>
                  {sideLabel(match.home)} – {sideLabel(match.away)}
                  <span aria-hidden="true">→</span>
                </Link>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  )
}

function ClassementsCrossLink({
  href,
  rows,
}: {
  href: string
  rows: MatchHubRow[]
}) {
  const { t } = useTranslation('matches')
  const hasFinished = rows.some((row) => row.match.status === 'Finished')

  if (!hasFinished) {
    return (
      <section className="ds-panel" aria-labelledby="matches-classements">
        <PanelHead
          id="matches-classements"
          icon={<ClassementsNavIcon size="md" />}
        >
          {t('classements.heading')}
        </PanelHead>
        <p className="matches-panel__meta">{t('classements.empty')}</p>
      </section>
    )
  }

  return (
    <section className="ds-panel" aria-labelledby="matches-classements">
      <PanelHead
        id="matches-classements"
        icon={<ClassementsNavIcon size="md" />}
      >
        {t('classements.heading')}
      </PanelHead>
      <p className="matches-panel__meta">{t('classements.body')}</p>
      <div className="matches-panel__footer">
        <Link className="matches-link" to={href}>
          {t('classements.open')}
          <span aria-hidden="true">→</span>
        </Link>
      </div>
    </section>
  )
}

function PanelHead({
  id,
  icon,
  children,
}: {
  id: string
  icon: ReactNode
  children: ReactNode
}) {
  return (
    <h2 id={id} className="matches-panel__head">
      <span className="matches-panel__icon" aria-hidden="true">
        {icon}
      </span>
      {children}
    </h2>
  )
}

function TeamCrest({ name }: { name: string }) {
  const initial = teamInitial(name)
  const tone = crestTone(name)

  return (
    <span
      className={`matches-crest matches-crest--${tone}`}
      aria-hidden="true"
    >
      <svg
        className="matches-crest__shield"
        viewBox="0 0 24 28"
        focusable="false"
      >
        <path d="M12 1.5 21 5.2v8.4c0 6.1-3.9 10.6-9 12.4-5.1-1.8-9-6.3-9-12.4V5.2L12 1.5Z" />
      </svg>
      <span className="matches-crest__initial">{initial}</span>
    </span>
  )
}

function teamInitial(name: string): string {
  const trimmed = name.trim()
  if (!trimmed || trimmed === '—') {
    return '?'
  }
  return trimmed.charAt(0).toLocaleUpperCase()
}

function crestTone(name: string): 'a' | 'b' | 'c' | 'd' | 'e' {
  let hash = 0
  for (let i = 0; i < name.length; i += 1) {
    hash = (hash + name.charCodeAt(i) * (i + 1)) % 5
  }
  return (['a', 'b', 'c', 'd', 'e'] as const)[hash]
}

interface SportingBucket {
  key: string
  label: string
  sort: number
  rows: MatchHubRow[]
}

/**
 * Groups matches for calendar display — presentation only.
 * Prefer Read roundName, then matchdayNumber, else a residual bucket.
 */
function groupMatchesBySportingBucket(
  rows: MatchHubRow[],
  t: ReturnType<typeof useTranslation<'matches'>>['t'],
): SportingBucket[] {
  const map = new Map<string, SportingBucket>()

  for (const row of rows) {
    const round = row.match.roundName?.trim()
    let key: string
    let label: string
    let sort: number

    if (round) {
      key = `round:${round}`
      label = round
      sort = 10_000
    } else if (row.match.matchdayNumber != null) {
      key = `day:${row.match.matchdayNumber}`
      label = t('list.matchday', { number: row.match.matchdayNumber })
      sort = row.match.matchdayNumber
    } else {
      key = `stage:${row.match.stageId}`
      label = row.stageName
      sort = 20_000
    }

    const bucket = map.get(key)
    if (bucket) {
      bucket.rows.push(row)
    } else {
      map.set(key, { key, label, sort, rows: [row] })
    }
  }

  return [...map.values()].sort((a, b) => a.sort - b.sort || a.label.localeCompare(b.label))
}

type DayStatus = 'upcoming' | 'live' | 'finished' | 'partial' | 'other'

function journéeStatus(statuses: MatchStatus[]): DayStatus {
  if (statuses.length === 0) {
    return 'other'
  }
  if (statuses.every((status) => status === 'Finished')) {
    return 'finished'
  }
  if (statuses.some((status) => status === 'Live')) {
    return 'live'
  }
  if (statuses.every((status) => status === 'Scheduled')) {
    return 'upcoming'
  }
  if (
    statuses.some((status) => status === 'Finished') &&
    statuses.some((status) => status === 'Scheduled' || status === 'Live')
  ) {
    return 'partial'
  }
  return 'other'
}

function journéeBreakdown(
  statuses: MatchStatus[],
  t: ReturnType<typeof useTranslation<'matches'>>['t'],
): string | null {
  const finished = statuses.filter((status) => status === 'Finished').length
  const live = statuses.filter((status) => status === 'Live').length
  const upcoming = statuses.filter((status) => status === 'Scheduled').length
  const parts: string[] = []
  if (finished > 0) {
    parts.push(t('calendar.breakdown.finished', { count: finished }))
  }
  if (live > 0) {
    parts.push(t('calendar.breakdown.live', { count: live }))
  }
  if (upcoming > 0) {
    parts.push(t('calendar.breakdown.upcoming', { count: upcoming }))
  }
  return parts.length > 0 ? parts.join(' · ') : null
}
