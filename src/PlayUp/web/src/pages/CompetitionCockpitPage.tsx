import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { fetchCompetitionCockpit } from '../api'
import { actionLabel } from '../i18n/actionLabels'
import {
  drawResolutionKindLabel,
  drawResolutionStateLabel,
  drawStatusLabel,
  stageStatusLabel,
} from '../i18n/enumLabels'
import { situationTitle } from '../i18n/situationCopy'
import { queryKeys } from '../queryKeys'
import type {
  CockpitAction,
  CockpitDimension,
  CockpitDrawFocus,
  CockpitSituation,
  CockpitView,
} from '../types'
import {
  CompetitionStatusBadge,
  ErrorState,
  LoadingState,
  MutationError,
  PageHeader,
  PendingLabel,
  StageStatusBadge,
  StatusBadge,
} from '../ui'
import {
  cockpitActionKey,
  resolveCockpitActionIntent,
} from './cockpitActions'
import { situationHref } from './cockpitNavigation'

/**
 * Competition Cockpit — GET /competitions/{id}/cockpit.
 * Presents Read Surface facts; does not recompute readiness, blockers, or draw applied.
 */
export function CompetitionCockpitPage() {
  const { competitionId = '' } = useParams()
  const { t } = useTranslation('cockpit')

  const query = useQuery({
    queryKey: queryKeys.competitions.cockpit(competitionId),
    queryFn: () => fetchCompetitionCockpit(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page">
      <PageHeader
        eyebrow={t('eyebrow')}
        title={query.data?.name ?? t('titleFallback')}
        back={{ to: '/competitions', label: t('back') }}
        badges={
          query.data && (
            <>
              <CompetitionStatusBadge status={query.data.status} />
              <StatusBadge tone="info">
                {t(`cycle.${query.data.cycleReading.code}`, {
                  defaultValue: query.data.cycleReading.code,
                })}
              </StatusBadge>
            </>
          )
        }
      />

      {query.isPending && <LoadingState />}
      {query.isError && <ErrorState error={query.error} />}
      {query.data && <CockpitViewBody data={query.data} />}
    </main>
  )
}

function CockpitViewBody({ data }: { data: CockpitView }) {
  const { t } = useTranslation(['cockpit', 'enums', 'actions'])

  return (
    <div className="section-stack">
      <CycleReadingSection data={data} />
      <ConstructionDimensionsSection data={data} />
      <OperationalFocusSection data={data} />
      <SituationsSection
        heading={t('cockpit:situations.heading')}
        empty={t('cockpit:situations.empty')}
        situations={data.situations}
        competitionId={data.competitionId}
      />
      <AttentionSummarySection data={data} />
      <NaturalProgressionSection data={data} />
      <AvailableActionsSection data={data} />
      <ClosureHintSection data={data} />
      <SpacesNavSection competitionId={data.competitionId} />
    </div>
  )
}

function CycleReadingSection({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const code = data.cycleReading.code

  return (
    <section className="card" aria-labelledby="cockpit-cycle">
      <div className="card__head">
        <h2 className="card__title" id="cockpit-cycle">
          {t('cycle.heading')}
        </h2>
        <span className="id-chip">{data.competitionId}</span>
      </div>
      <p className="lede">{t('cycle.lede')}</p>
      <p className="stat__value stat__value--text">
        {t(`cycle.${code}`, { defaultValue: code })}
      </p>
    </section>
  )
}

function ConstructionDimensionsSection({ data }: { data: CockpitView }) {
  const { t } = useTranslation(['cockpit', 'enums'])
  const dims = data.constructionDimensions
  const orgHref = `/competitions/${data.competitionId}/organisation`
  const matchesHref = `/competitions/${data.competitionId}/matches`

  return (
    <section className="section-stack" aria-labelledby="cockpit-dimensions">
      <h2 className="card__title" id="cockpit-dimensions">
        {t('cockpit:dimensions.heading')}
      </h2>
      <div className="card-grid">
        <DimensionCard
          title={t('cockpit:dimensions.teams.title')}
          prominence={dims.teams.prominence}
          summary={dimensionTeamsSummary(dims.teams, t)}
          href={orgHref}
          hrefLabel={t('cockpit:dimensions.openOrganisation')}
        />
        <DimensionCard
          title={t('cockpit:dimensions.structure.title')}
          prominence={dims.structure.prominence}
          summary={dimensionStructureSummary(dims.structure, t)}
          href={orgHref}
          hrefLabel={t('cockpit:dimensions.openOrganisation')}
        />
        <DimensionCard
          title={t('cockpit:dimensions.regulation.title')}
          prominence={dims.regulation.prominence}
          summary={t('cockpit:dimensions.regulation.summary', {
            periods: dims.regulation.facts.numberOfPeriods,
            duration: dims.regulation.facts.durationPerPeriod,
            win: dims.regulation.facts.winPoints,
            draw: dims.regulation.facts.drawPoints,
            loss: dims.regulation.facts.lossPoints,
            min: dims.regulation.facts.minimumTeams,
            max: dims.regulation.facts.maximumTeams,
          })}
          href={orgHref}
          hrefLabel={t('cockpit:dimensions.openOrganisation')}
        />
        <DimensionCard
          title={t('cockpit:dimensions.matches.title')}
          prominence={dims.matches.prominence}
          summary={dimensionMatchesSummary(dims.matches, t)}
          href={matchesHref}
          hrefLabel={t('cockpit:dimensions.openMatches')}
        />
      </div>
    </section>
  )
}

function DimensionCard({
  title,
  prominence,
  summary,
  href,
  hrefLabel,
}: {
  title: string
  prominence: string
  summary: string
  href: string
  hrefLabel: string
}) {
  const { t } = useTranslation('cockpit')

  return (
    <article className="card">
      <div className="card__head">
        <h3 className="card__title">{title}</h3>
        <span className="muted">
          {t(`prominence.${prominence}`, { defaultValue: prominence })}
        </span>
      </div>
      <p>{summary}</p>
      <p>
        <Link className="btn" to={href}>
          {hrefLabel}
        </Link>
      </p>
    </article>
  )
}

function OperationalFocusSection({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const focus = data.operationalFocus

  return (
    <section className="section-stack" aria-labelledby="cockpit-operational">
      <h2 className="card__title" id="cockpit-operational">
        {t('cockpit:operational.heading')}
      </h2>

      <section className="card" aria-labelledby="cockpit-stages">
        <h3 className="card__title" id="cockpit-stages">
          {t('cockpit:operational.stagesHeading')}
        </h3>
        {focus.stages.length === 0 ? (
          <p className="muted">{t('cockpit:operational.stagesEmpty')}</p>
        ) : (
          <ul className="plain-list">
            {focus.stages.map((stage) => (
              <li key={stage.stageId} className="row">
                <div>
                  <strong>{stage.name}</strong>{' '}
                  <StageStatusBadge status={stage.status} />
                  <span className="muted"> · {stageStatusLabel(stage.status)}</span>
                </div>
                <Link className="btn" to={`/stages/${stage.stageId}`}>
                  {t('cockpit:operational.openStage')}
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="card" aria-labelledby="cockpit-draws">
        <h3 className="card__title" id="cockpit-draws">
          {t('cockpit:operational.drawsHeading')}
        </h3>
        {focus.draws.length === 0 ? (
          <p className="muted">{t('cockpit:operational.drawsEmpty')}</p>
        ) : (
          <ul className="plain-list">
            {focus.draws.map((draw) => (
              <DrawFocusRow key={draw.drawId} draw={draw} />
            ))}
          </ul>
        )}
      </section>

      <section className="card" aria-labelledby="cockpit-match-counts">
        <h3 className="card__title" id="cockpit-match-counts">
          {t('cockpit:operational.countsHeading')}
        </h3>
        <div className="stat-grid">
          <CountStat
            label={t('cockpit:operational.countLive')}
            value={focus.matchCounts.live}
          />
          <CountStat
            label={t('cockpit:operational.countScheduled')}
            value={focus.matchCounts.scheduled}
          />
          <CountStat
            label={t('cockpit:operational.countFinished')}
            value={focus.matchCounts.finished}
          />
          <CountStat
            label={t('cockpit:operational.countTotal')}
            value={focus.matchCounts.total}
          />
        </div>
      </section>

      <section className="card" aria-labelledby="cockpit-upcoming">
        <h3 className="card__title" id="cockpit-upcoming">
          {t('cockpit:operational.upcomingHeading')}
        </h3>
        {focus.upcomingMatches.length === 0 ? (
          <p className="muted">{t('cockpit:operational.upcomingEmpty')}</p>
        ) : (
          <ul className="plain-list">
            {focus.upcomingMatches.map((match) => (
              <li key={match.matchId} className="row">
                <div>
                  <strong>
                    {match.homeDisplayName} – {match.awayDisplayName}
                  </strong>
                  {match.scheduledAt && (
                    <p className="muted mono">{match.scheduledAt}</p>
                  )}
                </div>
                <Link className="btn" to={`/matches/${match.matchId}`}>
                  {t('cockpit:operational.openMatch')}
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </section>
  )
}

function DrawFocusRow({ draw }: { draw: CockpitDrawFocus }) {
  const { t } = useTranslation('cockpit')

  return (
    <li className="row">
      <div>
        <strong>{drawResolutionKindLabel(draw.kind)}</strong>
        <span className="muted">
          {' '}
          · {drawStatusLabel(draw.status)} ·{' '}
          {drawResolutionStateLabel(draw.resolutionState)} ·{' '}
          {draw.isApplied
            ? t('operational.drawApplied')
            : t('operational.drawNotApplied')}
        </span>
      </div>
      <Link className="btn" to={`/stages/${draw.stageId}`}>
        {t('operational.openStage')}
      </Link>
    </li>
  )
}

function CountStat({ label, value }: { label: string; value: number }) {
  return (
    <div className="stat">
      <p className="stat__label">{label}</p>
      <p className="stat__value">{value}</p>
    </div>
  )
}

function SituationsSection({
  heading,
  empty,
  situations,
  competitionId,
}: {
  heading: string
  empty: string
  situations: CockpitSituation[]
  competitionId: string
}) {
  const { t } = useTranslation('cockpit')
  const headingId = 'cockpit-situations'

  return (
    <section className="card" aria-labelledby={headingId}>
      <h2 className="card__title" id={headingId}>
        {heading}
      </h2>
      {situations.length === 0 ? (
        <p className="muted">{empty}</p>
      ) : (
        <ul className="plain-list">
          {situations.map((situation) => {
            const href = situationHref(situation, competitionId)
            const key = `${situation.source}:${situation.targetType}:${situation.targetId}:${situation.matchId}`
            return (
              <li key={key} className="row">
                <div>
                  <strong>{situationTitle(situation.source, situation.params)}</strong>
                  <p className="muted">
                    {t(`nature.${situation.nature}`, {
                      defaultValue: situation.nature,
                    })}
                    {situation.actionCode && (
                      <>
                        {' '}
                        · {actionLabel(situation.actionCode, situation.params)}
                      </>
                    )}
                  </p>
                </div>
                {href && (
                  <Link className="btn" to={href}>
                    {t('situations.open')}
                  </Link>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}

function AttentionSummarySection({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const summary = data.attentionSummary

  return (
    <section className="card" aria-labelledby="cockpit-attention">
      <div className="card__head">
        <h2 className="card__title" id="cockpit-attention">
          {t('attention.heading')}
        </h2>
        <span className="stat__value">{summary.count}</span>
      </div>
      <p className="muted">{t('attention.hint')}</p>
      {summary.items.length === 0 ? (
        <p className="muted">{t('attention.empty')}</p>
      ) : (
        <ul className="plain-list">
          {summary.items.map((situation) => {
            const href = situationHref(situation, data.competitionId)
            const key = `attention:${situation.source}:${situation.targetType}:${situation.targetId}`
            return (
              <li key={key} className="row">
                <div>
                  <strong>
                    {situationTitle(situation.source, situation.params)}
                  </strong>
                  <p className="muted">
                    {t(`nature.${situation.nature}`, {
                      defaultValue: situation.nature,
                    })}
                  </p>
                </div>
                {href && (
                  <Link className="btn" to={href}>
                    {t('situations.open')}
                  </Link>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}

function NaturalProgressionSection({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const code = data.naturalProgression?.code

  return (
    <section className="card" aria-labelledby="cockpit-progression">
      <h2 className="card__title" id="cockpit-progression">
        {t('progression.heading')}
      </h2>
      {code ? (
        <p className="stat__value stat__value--text">{actionLabel(code)}</p>
      ) : (
        <p className="muted">{t('progression.none')}</p>
      )}
    </section>
  )
}

function AvailableActionsSection({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [activeKey, setActiveKey] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: async (action: CockpitAction) => {
      const intent = resolveCockpitActionIntent(action, data)
      if (intent.kind !== 'execute') {
        throw new Error(`Action ${action.code} is not executable here`)
      }
      return intent.run()
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.cockpit(data.competitionId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.attention(data.competitionId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.workspace(data.competitionId),
      })
      setActiveKey(null)
    },
    onError: () => {
      setActiveKey(null)
    },
  })

  function onActionClick(action: CockpitAction) {
    const intent = resolveCockpitActionIntent(action, data)
    if (intent.kind === 'navigate') {
      void navigate(intent.to)
      return
    }
    if (intent.kind === 'execute') {
      setActiveKey(cockpitActionKey(action))
      mutation.mutate(action)
    }
  }

  return (
    <section className="card" aria-labelledby="cockpit-actions">
      <h2 className="card__title" id="cockpit-actions">
        {t('actions.heading')}
      </h2>
      {data.availableActions.length === 0 ? (
        <p className="muted">{t('actions.empty')}</p>
      ) : (
        <div className="button-row" aria-busy={mutation.isPending}>
          {data.availableActions.map((action) => {
            const key = cockpitActionKey(action)
            const intent = resolveCockpitActionIntent(action, data)
            const busy = mutation.isPending && activeKey === key
            const label = actionLabel(action.code, {
              name: action.params?.stageName,
              ...action.params,
            })

            if (intent.kind === 'unsupported') {
              return (
                <span key={key} className="muted">
                  {label}
                </span>
              )
            }

            return (
              <button
                key={key}
                type="button"
                className="btn btn--primary"
                disabled={mutation.isPending}
                onClick={() => onActionClick(action)}
              >
                {busy ? <PendingLabel>{t('actions.busy')}</PendingLabel> : label}
              </button>
            )
          })}
        </div>
      )}
      {mutation.isError && <MutationError error={mutation.error} />}
    </section>
  )
}

function ClosureHintSection({ data }: { data: CockpitView }) {
  const { t } = useTranslation(['cockpit', 'enums'])
  const hint = data.closureHint

  return (
    <section className="card" aria-labelledby="cockpit-closure">
      <h2 className="card__title" id="cockpit-closure">
        {t('cockpit:closure.heading')}
      </h2>
      <p
        className={`stat__value stat__value--text${hint.canCompleteNormally ? ' stat--ok' : ''}`}
      >
        {hint.canCompleteNormally
          ? t('cockpit:closure.ready')
          : t('cockpit:closure.notReady')}
      </p>
      {hint.blockerCodes.length > 0 ? (
        <div className="stack stack--tight">
          <h3 className="stat__label">{t('cockpit:closure.blockersHeading')}</h3>
          <ul className="check-list">
            {hint.blockerCodes.map((code) => (
              <li key={code} className="check check--no">
                <span className="check__mark" aria-hidden="true">
                  !
                </span>
                <span>
                  {t(`completionBlocker.${code}`, {
                    ns: 'enums',
                    defaultValue: code,
                  })}{' '}
                  <span className="mono">({code})</span>
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : (
        <p className="muted">{t('cockpit:closure.noBlockers')}</p>
      )}
    </section>
  )
}

function SpacesNavSection({ competitionId }: { competitionId: string }) {
  const { t } = useTranslation('cockpit')

  return (
    <section className="section-stack" aria-labelledby="cockpit-spaces">
      <h2 className="card__title" id="cockpit-spaces">
        {t('nav.heading')}
      </h2>
      <div className="card-grid">
        <Link
          className="nav-card"
          to={`/competitions/${competitionId}/organisation`}
        >
          <span className="nav-card__title">
            {t('nav.organisation.title')}
            <span className="row__chevron" aria-hidden="true">
              →
            </span>
          </span>
          <span className="nav-card__desc">{t('nav.organisation.desc')}</span>
        </Link>
        <Link
          className="nav-card"
          to={`/competitions/${competitionId}/matches`}
        >
          <span className="nav-card__title">
            {t('nav.matches.title')}
            <span className="row__chevron" aria-hidden="true">
              →
            </span>
          </span>
          <span className="nav-card__desc">{t('nav.matches.desc')}</span>
        </Link>
        <Link
          className="nav-card"
          to={`/competitions/${competitionId}/overview`}
        >
          <span className="nav-card__title">
            {t('nav.overview.title')}
            <span className="row__chevron" aria-hidden="true">
              →
            </span>
          </span>
          <span className="nav-card__desc">{t('nav.overview.desc')}</span>
        </Link>
      </div>
    </section>
  )
}

function dimensionTeamsSummary(
  dimension: CockpitDimension,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  return t('cockpit:dimensions.teams.summary', {
    activeCount: dimension.facts.activeCount ?? '0',
    minimumTeams: dimension.facts.minimumTeams ?? '—',
  })
}

function dimensionStructureSummary(
  dimension: CockpitDimension,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  const formatKind = dimension.facts.formatKind
  if (!formatKind || formatKind === 'None') {
    return t('cockpit:dimensions.structure.none')
  }
  return t('cockpit:dimensions.structure.summary', { formatKind })
}

function dimensionMatchesSummary(
  dimension: CockpitDimension,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  const total = Number(dimension.facts.total ?? '0')
  if (total === 0) {
    return t('cockpit:dimensions.matches.empty')
  }
  return t('cockpit:dimensions.matches.summary', {
    live: dimension.facts.live ?? '0',
    scheduled: dimension.facts.scheduled ?? '0',
    finished: dimension.facts.finished ?? '0',
    total: dimension.facts.total ?? '0',
  })
}
