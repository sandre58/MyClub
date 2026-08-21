import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
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
import {
  actionsForDraw,
  actionsForSlot,
  actionsForStage,
  cardProminenceClass,
  closurePresentation,
  findActionByCode,
  isProminenceCondensed,
  isTeamAdminAction,
  operationalBlocks,
  orderSituationsForDisplay,
  primaryTeamActions,
  secondaryActions,
  shouldShowOperationalSection,
  sortConstructionSlots,
  stageWideOperationalActions,
  type ConstructionSlot,
} from './cockpitComposition'

/**
 * Competition Cockpit — GET /competitions/{id}/cockpit.
 * Composes Read facts (prominence, situations, actions); does not recompute métier rules.
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
  const { t } = useTranslation('cockpit')
  const situationActionCodes = useMemo(() => {
    const codes = new Set<string>()
    for (const situation of data.situations) {
      if (situation.actionCode) {
        codes.add(situation.actionCode)
      }
    }
    return codes
  }, [data.situations])

  const slotActions = (slot: Parameters<typeof actionsForSlot>[1]) => {
    const base = actionsForSlot(data.availableActions, slot).filter(
      (action) => !situationActionCodes.has(action.code),
    )
    if (slot === 'teams') {
      return primaryTeamActions(base)
    }
    if (slot === 'operational') {
      // Stage/draw row actions are attached per object — not dumped here.
      return stageWideOperationalActions(base)
    }
    return base
  }

  const actionRunner = useCockpitActionRunner(data)

  const renderedKeys = useMemo(() => {
    const keys = new Set<string>()
    const mark = (actions: CockpitAction[]) => {
      for (const action of actions) {
        keys.add(cockpitActionKey(action))
      }
    }
    mark(slotActions('teams'))
    mark(slotActions('structure'))
    mark(slotActions('regulation'))
    mark(slotActions('matches'))
    mark(slotActions('operational'))
    mark(slotActions('closure'))
    for (const stage of data.operationalFocus.stages) {
      mark(actionsForStage(data.availableActions, stage.stageId))
    }
    for (const draw of data.operationalFocus.draws) {
      mark(actionsForDraw(data.availableActions, draw.stageId, draw.drawId))
    }
    for (const action of data.availableActions) {
      if (isTeamAdminAction(action.code)) {
        keys.add(cockpitActionKey(action))
      }
    }
    const progression = data.naturalProgression?.code
      ? findActionByCode(data.availableActions, data.naturalProgression.code)
      : undefined
    if (progression) {
      keys.add(cockpitActionKey(progression))
    }
    for (const situation of data.situations) {
      if (situation.actionCode) {
        const match = findActionByCode(data.availableActions, situation.actionCode)
        if (match) {
          keys.add(cockpitActionKey(match))
        }
      }
    }
    return keys
    // eslint-disable-next-line react-hooks/exhaustive-deps -- derived from data
  }, [data, situationActionCodes])

  const leftover = secondaryActions(data.availableActions, renderedKeys)
  const slots = sortConstructionSlots(data)
  const closureMode = closurePresentation(data)

  return (
    <div className="section-stack">
      <CycleReadingSection data={data} />

      <SituationsSection
        situations={orderSituationsForDisplay(data.situations)}
        competitionId={data.competitionId}
        availableActions={data.availableActions}
        actionRunner={actionRunner}
      />

      <AttentionTriageHint count={data.attentionSummary.count} />

      <NaturalProgressionSection data={data} actionRunner={actionRunner} />

      <ConstructionDimensionsSection
        data={data}
        slots={slots}
        actionRunner={actionRunner}
        actionsFor={slotActions}
      />

      {shouldShowOperationalSection(data) && (
        <OperationalFocusSection
          data={data}
          actionRunner={actionRunner}
          opsActions={slotActions('operational')}
        />
      )}

      <ClosureHintSection
        data={data}
        mode={closureMode}
        actionRunner={actionRunner}
        closureActions={slotActions('closure')}
      />

      {leftover.length > 0 && (
        <SecondaryActionsSection
          actions={leftover}
          actionRunner={actionRunner}
          heading={t('actions.secondaryHeading')}
        />
      )}

      <SpacesNavSection competitionId={data.competitionId} />

      {actionRunner.mutation.isError && (
        <MutationError error={actionRunner.mutation.error} />
      )}
    </div>
  )
}

type ActionRunner = ReturnType<typeof useCockpitActionRunner>

function useCockpitActionRunner(data: CockpitView) {
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

  return { onActionClick, mutation, activeKey }
}

function CycleReadingSection({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const code = data.cycleReading.code

  return (
    <section className="card card--condensed" aria-labelledby="cockpit-cycle">
      <div className="card__head">
        <h2 className="card__title" id="cockpit-cycle">
          {t('cycle.heading')}
        </h2>
      </div>
      <p className="stat__value stat__value--text">
        {t(`cycle.${code}`, { defaultValue: code })}
      </p>
    </section>
  )
}

function AttentionTriageHint({ count }: { count: number }) {
  const { t } = useTranslation('cockpit')

  if (count <= 0) {
    return null
  }

  return (
    <p className="caption" role="note">
      {t('attention.triageHint', { count })}
    </p>
  )
}

function ConstructionDimensionsSection({
  data,
  slots,
  actionRunner,
  actionsFor,
}: {
  data: CockpitView
  slots: ConstructionSlot[]
  actionRunner: ActionRunner
  actionsFor: (slot: Parameters<typeof actionsForSlot>[1]) => CockpitAction[]
}) {
  const { t } = useTranslation('cockpit')
  const dims = data.constructionDimensions
  const orgHref = `/competitions/${data.competitionId}/organisation`
  const matchesHref = `/competitions/${data.competitionId}/matches`

  if (slots.length === 0) {
    return null
  }

  return (
    <section className="section-stack" aria-labelledby="cockpit-dimensions">
      <h2 className="card__title" id="cockpit-dimensions">
        {t('dimensions.heading')}
      </h2>
      <div className="card-grid">
        {slots.map((slot) => {
          if (slot === 'teams') {
            return (
              <DimensionCard
                key={slot}
                title={t('dimensions.teams.title')}
                prominence={dims.teams.prominence}
                summary={dimensionTeamsSummary(dims.teams, t)}
                href={orgHref}
                hrefLabel={t('dimensions.openOrganisation')}
                actions={actionsFor('teams')}
                actionRunner={actionRunner}
              />
            )
          }
          if (slot === 'structure') {
            return (
              <DimensionCard
                key={slot}
                title={t('dimensions.structure.title')}
                prominence={dims.structure.prominence}
                summary={dimensionStructureSummary(dims.structure, t)}
                href={orgHref}
                hrefLabel={t('dimensions.openOrganisation')}
                actions={actionsFor('structure')}
                actionRunner={actionRunner}
              />
            )
          }
          if (slot === 'regulation') {
            return (
              <RegulationDimensionCard
                key={slot}
                regulation={dims.regulation}
                href={orgHref}
                hrefLabel={t('dimensions.openOrganisation')}
                actions={actionsFor('regulation')}
                actionRunner={actionRunner}
              />
            )
          }
          return (
            <DimensionCard
              key={slot}
              title={t('dimensions.matches.title')}
              prominence={dims.matches.prominence}
              summary={dimensionMatchesSummary(dims.matches, t)}
              href={matchesHref}
              hrefLabel={t('dimensions.openMatches')}
              actions={actionsFor('matches')}
              actionRunner={actionRunner}
            />
          )
        })}
      </div>
    </section>
  )
}

function RegulationDimensionCard({
  regulation,
  href,
  hrefLabel,
  actions,
  actionRunner,
}: {
  regulation: CockpitView['constructionDimensions']['regulation']
  href: string
  hrefLabel: string
  actions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')
  const competition = regulation.competition
  const condensed = isProminenceCondensed(regulation.prominence)
  const primaryGap = regulation.transitionReadiness.find((item) => !item.ready)

  return (
    <article className={cardProminenceClass(regulation.prominence)}>
      <div className="card__head">
        <h3 className="card__title">{t('dimensions.regulation.title')}</h3>
      </div>
      <p>
        {t('dimensions.regulation.summaryCompact', {
          periods: competition.numberOfPeriods,
          duration: competition.durationPerPeriod,
          win: competition.winPoints,
          draw: competition.drawPoints,
          loss: competition.lossPoints,
          min: competition.minimumTeams,
          max: competition.maximumTeams,
        })}
      </p>
      {!condensed && primaryGap && (
        <p className="caption">
          {t('dimensions.regulation.readinessNotReady', {
            transition: t(
              `dimensions.regulation.transitions.${primaryGap.transition}`,
              { defaultValue: primaryGap.transition },
            ),
          })}
        </p>
      )}
      {!condensed && !primaryGap && regulation.transitionReadiness.length > 0 && (
        <p className="caption">{t('dimensions.regulation.allReady')}</p>
      )}
      <ActionButtons actions={actions} actionRunner={actionRunner} />
      <p>
        <Link className="btn" to={href}>
          {hrefLabel}
        </Link>
      </p>
    </article>
  )
}

function DimensionCard({
  title,
  prominence,
  summary,
  href,
  hrefLabel,
  actions,
  actionRunner,
}: {
  title: string
  prominence: string
  summary: string
  href: string
  hrefLabel: string
  actions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  return (
    <article className={cardProminenceClass(prominence)}>
      <div className="card__head">
        <h3 className="card__title">{title}</h3>
      </div>
      <p>{summary}</p>
      <ActionButtons actions={actions} actionRunner={actionRunner} />
      <p>
        <Link className="btn" to={href}>
          {hrefLabel}
        </Link>
      </p>
    </article>
  )
}

function ActionButtons({
  actions,
  actionRunner,
}: {
  actions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')
  if (actions.length === 0) {
    return null
  }

  return (
    <div className="button-row" aria-busy={actionRunner.mutation.isPending}>
      {actions.map((action) => {
        const key = cockpitActionKey(action)
        const busy =
          actionRunner.mutation.isPending && actionRunner.activeKey === key
        const label = actionLabel(action.code, {
          name: action.params?.stageName,
          ...action.params,
        })
        return (
          <button
            key={key}
            type="button"
            className="btn btn--primary"
            disabled={actionRunner.mutation.isPending}
            onClick={() => actionRunner.onActionClick(action)}
          >
            {busy ? <PendingLabel>{t('actions.busy')}</PendingLabel> : label}
          </button>
        )
      })}
    </div>
  )
}

function OperationalFocusSection({
  data,
  actionRunner,
  opsActions,
}: {
  data: CockpitView
  actionRunner: ActionRunner
  opsActions: CockpitAction[]
}) {
  const { t } = useTranslation('cockpit')
  const focus = data.operationalFocus
  const blocks = operationalBlocks(data)

  return (
    <section className="section-stack" aria-labelledby="cockpit-operational">
      <h2 className="card__title" id="cockpit-operational">
        {t('operational.heading')}
      </h2>

      {blocks.includes('stages') && (
        <section className="card" aria-labelledby="cockpit-stages">
          <h3 className="card__title" id="cockpit-stages">
            {t('operational.stagesHeading')}
          </h3>
          <ul className="plain-list">
            {focus.stages.map((stage) => (
              <li key={stage.stageId} className="row">
                <div>
                  <strong>{stage.name}</strong>{' '}
                  <StageStatusBadge status={stage.status} />
                  <span className="muted"> · {stageStatusLabel(stage.status)}</span>
                  <ActionButtons
                    actions={actionsForStage(data.availableActions, stage.stageId)}
                    actionRunner={actionRunner}
                  />
                </div>
                <Link className="btn" to={`/stages/${stage.stageId}`}>
                  {t('operational.openStage')}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}

      {blocks.includes('draws') && (
        <section className="card" aria-labelledby="cockpit-draws">
          <h3 className="card__title" id="cockpit-draws">
            {t('operational.drawsHeading')}
          </h3>
          <ul className="plain-list">
            {focus.draws.map((draw) => (
              <DrawFocusRow
                key={draw.drawId}
                draw={draw}
                actions={actionsForDraw(
                  data.availableActions,
                  draw.stageId,
                  draw.drawId,
                )}
                actionRunner={actionRunner}
              />
            ))}
          </ul>
        </section>
      )}

      {blocks.includes('counts') && (
        <section className="card" aria-labelledby="cockpit-match-counts">
          <h3 className="card__title" id="cockpit-match-counts">
            {t('operational.countsHeading')}
          </h3>
          <div className="stat-grid">
            <CountStat label={t('operational.countLive')} value={focus.matchCounts.live} />
            <CountStat
              label={t('operational.countScheduled')}
              value={focus.matchCounts.scheduled}
            />
            <CountStat
              label={t('operational.countFinished')}
              value={focus.matchCounts.finished}
            />
            <CountStat label={t('operational.countTotal')} value={focus.matchCounts.total} />
          </div>
        </section>
      )}

      {blocks.includes('upcoming') && (
        <section className="card" aria-labelledby="cockpit-upcoming">
          <h3 className="card__title" id="cockpit-upcoming">
            {t('operational.upcomingHeading')}
          </h3>
          <ul className="plain-list">
            {focus.upcomingMatches.map((match) => (
              <li key={match.matchId} className="row">
                <div>
                  <strong>
                    {match.homeDisplayName} – {match.awayDisplayName}
                  </strong>
                  {match.scheduledAt && (
                    <p className="muted">{match.scheduledAt}</p>
                  )}
                </div>
                <Link className="btn" to={`/matches/${match.matchId}`}>
                  {t('operational.openMatch')}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}

      {opsActions.length > 0 && (
        <ActionButtons actions={opsActions} actionRunner={actionRunner} />
      )}
    </section>
  )
}

function DrawFocusRow({
  draw,
  actions,
  actionRunner,
}: {
  draw: CockpitDrawFocus
  actions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')

  return (
    <li className="row">
      <div>
        <strong>{drawResolutionKindLabel(draw.kind)}</strong>
        <p className="muted">
          {drawStatusLabel(draw.status)} · {drawResolutionStateLabel(draw.resolutionState)} ·{' '}
          {draw.isApplied ? t('operational.drawApplied') : t('operational.drawNotApplied')}
        </p>
        <ActionButtons actions={actions} actionRunner={actionRunner} />
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
  situations,
  competitionId,
  availableActions,
  actionRunner,
}: {
  situations: CockpitSituation[]
  competitionId: string
  availableActions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')

  return (
    <section className="card" aria-labelledby="cockpit-situations">
      <h2 className="card__title" id="cockpit-situations">
        {t('situations.heading')}
      </h2>
      {situations.length === 0 ? (
        <p className="muted">{t('situations.empty')}</p>
      ) : (
        <ul className="plain-list">
          {situations.map((situation) => {
            const href = situationHref(situation, competitionId)
            const key = `${situation.source}:${situation.targetType}:${situation.targetId}`
            const linkedAction = situation.actionCode
              ? findActionByCode(availableActions, situation.actionCode)
              : undefined
            return (
              <li key={key} className="row">
                <div>
                  <strong>{situationTitle(situation.source, situation.params)}</strong>
                  <p className="muted">
                    {t(`nature.${situation.nature}`, {
                      defaultValue: situation.nature,
                    })}
                    {situation.impactCode && (
                      <>
                        {' '}
                        ·{' '}
                        {t(`impact.${situation.impactCode}`, {
                          defaultValue: situation.impactCode,
                        })}
                      </>
                    )}
                    {!situation.actionable && (
                      <>
                        {' '}
                        · {t('situations.notActionable')}
                      </>
                    )}
                  </p>
                  {linkedAction && (
                    <ActionButtons
                      actions={[linkedAction]}
                      actionRunner={actionRunner}
                    />
                  )}
                </div>
                {href && !linkedAction && (
                  <Link className="btn" to={href}>
                    {t('situations.open')}
                  </Link>
                )}
                {href && linkedAction && (
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

function NaturalProgressionSection({
  data,
  actionRunner,
}: {
  data: CockpitView
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')
  const code = data.naturalProgression?.code
  const matched = code ? findActionByCode(data.availableActions, code) : undefined
  const orgHref = `/competitions/${data.competitionId}/organisation`
  const matchesHref = `/competitions/${data.competitionId}/matches`
  const classementsHref = `/competitions/${data.competitionId}/classements`

  return (
    <section className="card" aria-labelledby="cockpit-progression">
      <h2 className="card__title" id="cockpit-progression">
        {t('progression.heading')}
      </h2>
      {code ? (
        <>
          <p className="stat__value stat__value--text">
            {t(`progression.codes.${code}`, {
              defaultValue: actionLabel(code),
            })}
          </p>
          {matched ? (
            <ActionButtons actions={[matched]} actionRunner={actionRunner} />
          ) : code === 'ContinueOrganisation' ? (
            <p>
              <Link className="btn" to={orgHref}>
                {t('dimensions.openOrganisation')}
              </Link>
            </p>
          ) : code === 'OpenMatches' ? (
            <p>
              <Link className="btn" to={matchesHref}>
                {t('dimensions.openMatches')}
              </Link>
            </p>
          ) : code === 'OpenConsultation' ? (
            <p>
              <Link className="btn" to={classementsHref}>
                {t('nav.classements.title')}
              </Link>
            </p>
          ) : null}
        </>
      ) : (
        <p className="muted">{t('progression.none')}</p>
      )}
    </section>
  )
}

function SecondaryActionsSection({
  actions,
  actionRunner,
  heading,
}: {
  actions: CockpitAction[]
  actionRunner: ActionRunner
  heading: string
}) {
  return (
    <section className="card card--condensed" aria-labelledby="cockpit-actions-secondary">
      <h2 className="card__title" id="cockpit-actions-secondary">
        {heading}
      </h2>
      <ActionButtons actions={actions} actionRunner={actionRunner} />
    </section>
  )
}

function ClosureHintSection({
  data,
  mode,
  actionRunner,
  closureActions,
}: {
  data: CockpitView
  mode: 'full' | 'condensed' | 'hidden'
  actionRunner: ActionRunner
  closureActions: CockpitAction[]
}) {
  const { t } = useTranslation(['cockpit', 'enums'])
  const hint = data.closureHint

  if (mode === 'hidden') {
    return null
  }

  if (mode === 'condensed') {
    return (
      <section className="card card--condensed" aria-labelledby="cockpit-closure">
        <h2 className="card__title" id="cockpit-closure">
          {t('cockpit:closure.heading')}
        </h2>
        <p className="caption">
          {hint.canCompleteNormally
            ? t('cockpit:closure.ready')
            : t('cockpit:closure.notRelevant')}
        </p>
        <ActionButtons actions={closureActions} actionRunner={actionRunner} />
      </section>
    )
  }

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
      {hint.blockerCodes.length > 0 && (
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
                  })}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
      <ActionButtons actions={closureActions} actionRunner={actionRunner} />
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
          to={`/competitions/${competitionId}/classements`}
        >
          <span className="nav-card__title">
            {t('nav.classements.title')}
            <span className="row__chevron" aria-hidden="true">
              →
            </span>
          </span>
          <span className="nav-card__desc">{t('nav.classements.desc')}</span>
        </Link>
      </div>
    </section>
  )
}

function dimensionTeamsSummary(
  dimension: CockpitDimension,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  return t('dimensions.teams.summary', {
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
    return t('dimensions.structure.none')
  }
  return t('dimensions.structure.summary', { formatKind })
}

function dimensionMatchesSummary(
  dimension: CockpitDimension,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  const total = Number(dimension.facts.total ?? '0')
  if (total === 0) {
    return t('dimensions.matches.empty')
  }
  return t('dimensions.matches.summary', {
    live: dimension.facts.live ?? '0',
    scheduled: dimension.facts.scheduled ?? '0',
    finished: dimension.facts.finished ?? '0',
    total: dimension.facts.total ?? '0',
  })
}
