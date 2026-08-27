import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { fetchCompetitionCockpit, fetchOrganisationView } from '../api'
import { TeamCrest } from '../design-system/TeamCrest'
import { Status } from '../design-system/components/Status'
import {
  AttentionMarkIcon,
  ChevronRightIcon,
  ClassementsNavIcon,
  MatchesNavIcon,
} from '../design-system/icons/shellIcons'
import { actionLabel } from '../i18n/actionLabels'
import {
  attentionTargetTypeLabel,
  structureFormatKindLabel,
} from '../i18n/enumLabels'
import { situationTitle } from '../i18n/situationCopy'
import { queryKeys } from '../queryKeys'
import {
  activeUiCyclePhaseIndex,
  UI_CYCLE_PHASES,
} from '../shell/cycleUi'
import type {
  CockpitAction,
  CockpitDimension,
  CockpitMatchLine,
  CockpitReferenceStageGameRules,
  CockpitSituation,
  CockpitSportUnit,
  CockpitStandingCompact,
  CockpitView,
  OrganisationEntry,
  StructureFormatKind,
} from '../types'
import {
  ErrorState,
  LoadingState,
  MutationError,
  PendingLabel,
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
  findActionByCode,
  isProminenceCondensed,
  isTeamAdminAction,
  orderSituationsForDisplay,
  panelProminenceClass,
  primaryTeamActions,
  secondaryActions,
  sortConstructionSlots,
  stageWideOperationalActions,
} from './cockpitComposition'
import './overview.css'
import {
  CalendarIcon,
  CheckIcon,
  CompletedIcon,
  CreateMatchesIcon,
  InProgressIcon,
  NextActionIcon,
  OverviewAttentionIcon,
  PendingCircleIcon,
  PreparationIcon,
  RegulationIcon,
  StructureIcon,
  TeamsIcon,
  WhereAreWeIcon,
} from '../design-system/icons/overviewIcons'

/**
 * Competition Cockpit — GET /competitions/{id}/cockpit.
 * Composes Read facts (prominence, situations, actions); does not recompute métier rules.
 */
export function CompetitionCockpitPage() {
  const { competitionId = '' } = useParams()

  const query = useQuery({
    queryKey: queryKeys.competitions.cockpit(competitionId),
    queryFn: () => fetchCompetitionCockpit(competitionId),
    enabled: competitionId.length > 0,
  })

  return (
    <main id="main" className="page page--overview">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && <CockpitViewBody data={query.data} />}
    </main>
  )
}

function CockpitViewBody({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const actionRunner = useCockpitActionRunner(data)

  const orgQuery = useQuery({
    queryKey: queryKeys.competitions.organisation(data.competitionId),
    queryFn: () => fetchOrganisationView(data.competitionId),
  })

  const slotActions = (slot: Parameters<typeof actionsForSlot>[1]) => {
    const base = actionsForSlot(data.availableActions, slot)
    if (slot === 'teams') {
      return primaryTeamActions(base)
    }
    if (slot === 'operational') {
      // Stage/draw row actions are attached per object — not dumped here.
      return stageWideOperationalActions(base)
    }
    return base
  }

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
    return keys
    // eslint-disable-next-line react-hooks/exhaustive-deps -- derived from data
  }, [data])

  const leftover = secondaryActions(data.availableActions, renderedKeys)
  const lifecycleActions = leftover.filter(
    (action) =>
      action.code === 'PrepareCompetition' || action.code === 'StartCompetition',
  )
  const inProgress = data.cycleReading.code === 'InProgress'
  const completedLike =
    data.cycleReading.code === 'Completed' ||
    data.cycleReading.code === 'Archived'
  const slots = sortConstructionSlots(data).filter((slot) => slot !== 'matches')
  const teamsVisible = slots.includes('teams')
  const regulationVisible = slots.includes('regulation')
  const structureVisible = slots.includes('structure')
  const gameRules = data.operationalFocus.referenceStageGameRules
  const gameRegulationVisible =
    inProgress && regulationVisible && gameRules != null
  const stageActions = data.operationalFocus.stages.flatMap((stage) =>
    actionsForStage(data.availableActions, stage.stageId),
  )
  const orgHref = `/competitions/${data.competitionId}/organisation`
  const matchesHref = `/competitions/${data.competitionId}/matches`
  const classementsHref = `/competitions/${data.competitionId}/classements`
  const focus = data.operationalFocus
  const showStanding =
    (inProgress || completedLike) && focus.standingCompact != null
  // En cours: Dernières / Prochaines always visible (empty-state override).
  const showTemporalUnits = inProgress
  const showSport = showStanding || showTemporalUnits

  return (
    <div className="overview">
      <div className="overview__pilotage">
        <WhereAreWePanel data={data} />
        <NaturalProgressionSection
          data={data}
          actionRunner={actionRunner}
          lifecycleActions={lifecycleActions}
        />
      </div>

      <div className={inProgress ? 'overview__mid overview__mid--running' : 'overview__mid'}>
        <AttentionSignalSection
          items={orderSituationsForDisplay(data.attentionSummary.items)}
          count={data.attentionSummary.count}
          competitionId={data.competitionId}
        />
        {!inProgress && teamsVisible && (
          <TeamsPanel
            variant="construction"
            dimension={data.constructionDimensions.teams}
            entries={orgQuery.data?.participants.entries ?? []}
            href={orgHref}
            hrefLabel={t('dimensions.openTeams')}
            actions={slotActions('teams')}
            actionRunner={actionRunner}
          />
        )}
        {!inProgress && regulationVisible && (
          <RegulationDimensionCard
            variant="construction"
            regulation={data.constructionDimensions.regulation}
            href={orgHref}
            hrefLabel={t('dimensions.openRegulation')}
            actions={slotActions('regulation')}
            actionRunner={actionRunner}
          />
        )}
      </div>

      {showSport && (
        <div className="overview__sport">
          {showStanding && focus.standingCompact && (
            <StandingCompactPanel
              standing={focus.standingCompact}
              href={classementsHref}
            />
          )}
          {showTemporalUnits && (
            <div className="overview__sport-stack">
              <SportUnitPanel
                kind="recent"
                unit={focus.recentUnit}
                matchesHref={matchesHref}
              />
              <SportUnitPanel
                kind="next"
                unit={focus.nextUnit}
                matchesHref={matchesHref}
              />
            </div>
          )}
        </div>
      )}

      {inProgress &&
        (structureVisible || teamsVisible || gameRegulationVisible) && (
        <div className="overview__mid overview__mid--condensed-config">
          {structureVisible && (
            <StructurePanel
              variant="condensed"
              dimension={data.constructionDimensions.structure}
              stages={data.operationalFocus.stages}
              matchTotal={data.operationalFocus.matchCounts.total}
              swissByes={data.operationalFocus.swissByes}
              href={orgHref}
              hrefLabel={t('dimensions.openStructure')}
              actions={[]}
              actionRunner={actionRunner}
            />
          )}
          {teamsVisible && (
            <TeamsPanel
              variant="identity"
              dimension={data.constructionDimensions.teams}
              entries={orgQuery.data?.participants.entries ?? []}
              href={orgHref}
              hrefLabel={t('dimensions.openTeams')}
              actions={[]}
              actionRunner={actionRunner}
            />
          )}
          {gameRegulationVisible && gameRules && (
            <RegulationDimensionCard
              variant="game"
              regulation={data.constructionDimensions.regulation}
              gameRules={gameRules}
              href={orgHref}
              hrefLabel={t('dimensions.openRegulation')}
              actions={[]}
              actionRunner={actionRunner}
            />
          )}
        </div>
      )}

      {!inProgress && structureVisible && (
        <StructurePanel
          variant="construction"
          dimension={data.constructionDimensions.structure}
          stages={data.operationalFocus.stages}
          matchTotal={data.operationalFocus.matchCounts.total}
          swissByes={data.operationalFocus.swissByes}
          href={orgHref}
          hrefLabel={t('dimensions.openStructure')}
          actions={[...slotActions('structure'), ...stageActions]}
          actionRunner={actionRunner}
        />
      )}

      {actionRunner.materializeFollowUp && (
        <MaterializeFollowUpBanner
          competitionId={data.competitionId}
          followUp={actionRunner.materializeFollowUp}
          onDismiss={actionRunner.clearMaterializeFollowUp}
        />
      )}

      {actionRunner.mutation.isError && (
        <MutationError error={actionRunner.mutation.error} />
      )}
    </div>
  )
}

type ActionRunner = ReturnType<typeof useCockpitActionRunner>

type MaterializeFollowUp = {
  createdCount: number
  attachedCount: number
  alreadyComplete: boolean
}

type MaterializeResult = {
  createdCount: number
  attachedMatchIds: string[]
  alreadyComplete: boolean
}

function useCockpitActionRunner(data: CockpitView) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [activeKey, setActiveKey] = useState<string | null>(null)
  const [materializeFollowUp, setMaterializeFollowUp] =
    useState<MaterializeFollowUp | null>(null)

  const mutation = useMutation({
    mutationFn: async (action: CockpitAction) => {
      const intent = resolveCockpitActionIntent(action, data)
      if (intent.kind !== 'execute') {
        throw new Error(`Action ${action.code} is not executable here`)
      }
      return intent.run()
    },
    onSuccess: async (result, action) => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.cockpit(data.competitionId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.attention(data.competitionId),
      })
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.workspace(data.competitionId),
      })

      if (action.code === 'MaterializeMatches') {
        const materialize = result as MaterializeResult
        await queryClient.invalidateQueries({
          queryKey: queryKeys.competitions.detail(data.competitionId),
        })
        await queryClient.invalidateQueries({
          queryKey: queryKeys.competitions.organisation(data.competitionId),
        })
        if (action.stageId) {
          await queryClient.invalidateQueries({
            queryKey: queryKeys.matches.byStage(action.stageId),
          })
        } else {
          await queryClient.invalidateQueries({
            queryKey: ['matches', 'by-stage'],
          })
        }
        setMaterializeFollowUp({
          createdCount: materialize.createdCount,
          attachedCount: materialize.attachedMatchIds.length,
          alreadyComplete: materialize.alreadyComplete,
        })
      }

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

  return {
    onActionClick,
    resolveIntent: (action: CockpitAction) =>
      resolveCockpitActionIntent(action, data),
    mutation,
    activeKey,
    materializeFollowUp,
    clearMaterializeFollowUp: () => setMaterializeFollowUp(null),
  }
}

function MaterializeFollowUpBanner({
  competitionId,
  followUp,
  onDismiss,
}: {
  competitionId: string
  followUp: MaterializeFollowUp
  onDismiss: () => void
}) {
  const { t } = useTranslation('cockpit')
  const matchesHref = `/competitions/${competitionId}/matches`
  const hasMatches = followUp.attachedCount > 0

  return (
    <section
      className="ds-panel"
      aria-labelledby="cockpit-materialize-followup"
      role="status"
    >
      <PanelHead id="cockpit-materialize-followup" icon={<NextActionIcon size="md" />}>
        {t('materializeFollowUp.heading')}
      </PanelHead>
      <p className="overview-panel__lede">
        {followUp.alreadyComplete && followUp.createdCount === 0
          ? t('materializeFollowUp.alreadyComplete', {
              count: followUp.attachedCount,
            })
          : t('materializeFollowUp.created', {
              created: followUp.createdCount,
              total: followUp.attachedCount,
            })}
      </p>
      {hasMatches && (
        <p>
          <Link className="ds-btn ds-btn--primary" to={matchesHref}>
            {t('materializeFollowUp.openMatches')}
          </Link>{' '}
          <button type="button" className="ds-btn ds-btn--secondary" onClick={onDismiss}>
            {t('materializeFollowUp.dismiss')}
          </button>
        </p>
      )}
    </section>
  )
}

function CycleLine({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const activeIndex = activeUiCyclePhaseIndex(data.cycleReading.code)

  return (
    <nav aria-label={t('cycle.lede')}>
      <ol className="overview-cycle-line">
        {UI_CYCLE_PHASES.map((phase, index) => (
          <li key={phase} className="overview-cycle-line__item">
            {index > 0 && (
              <span className="overview-cycle-line__sep" aria-hidden="true">
                →
              </span>
            )}
            <span
              className="overview-cycle-line__phase"
              data-active={index === activeIndex ? 'true' : 'false'}
              aria-current={index === activeIndex ? 'step' : undefined}
            >
              {t(`cycleUi.${phase}`, { defaultValue: phase })}
            </span>
          </li>
        ))}
      </ol>
    </nav>
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
    <h2 className="overview-panel__head" id={id}>
      <span className="overview-panel__icon" aria-hidden="true">
        {icon}
      </span>
      <span className="overview-panel__title-text">{children}</span>
    </h2>
  )
}

/** Cycle glyph — presentation of the known phase, no readiness claim. */
function cycleBadgeIcon(code: string) {
  if (code === 'Calendar') {
    return <CalendarIcon />
  }
  if (code === 'InProgress') {
    return <InProgressIcon />
  }
  if (code === 'Completed' || code === 'Archived') {
    return <CompletedIcon />
  }
  return <PreparationIcon />
}

function WhereAreWePanel({ data }: { data: CockpitView }) {
  const { t } = useTranslation('cockpit')
  const code = data.cycleReading.code

  return (
    <section className="ds-panel" aria-labelledby="cockpit-where">
      <PanelHead id="cockpit-where" icon={<WhereAreWeIcon size="md" />}>
        {t('cycle.heading')}
      </PanelHead>
      <div className="overview-hero">
        <span className="overview-badge overview-badge--tint" aria-hidden="true">
          {cycleBadgeIcon(code)}
        </span>
        <div className="overview-hero__body">
          <p className="overview-hero__title">
            {t(`cycle.${code}`, { defaultValue: code })}
          </p>
          <p className="overview-hero__sub">
            {t(`cycleDesc.${code}`, { defaultValue: t('cycle.lede') })}
          </p>
        </div>
      </div>
      <CycleLine data={data} />
    </section>
  )
}

function StandingCompactPanel({
  standing,
  href,
}: {
  standing: CockpitStandingCompact
  href: string
}) {
  const { t } = useTranslation('cockpit')
  const tables = standing.tables
  const isGroups = tables.length > 1 || tables[0]?.scope === 'Group'
  const [tableIndex, setTableIndex] = useState(0)
  const safeIndex = Math.min(tableIndex, Math.max(tables.length - 1, 0))
  const table = tables[safeIndex]
  if (!table) {
    return null
  }

  const title =
    isGroups && table.groupName
      ? t('sport.standingGroupTitle', { name: table.groupName })
      : t('sport.standingTitle')

  return (
    <section className="ds-panel" aria-labelledby="cockpit-standing-compact">
      <PanelHead id="cockpit-standing-compact" icon={<ClassementsNavIcon size="md" />}>
        {title}
      </PanelHead>
      {tables.length > 1 && (
        <div className="overview-standing-nav" role="tablist" aria-label={t('sport.groupNav')}>
          {tables.map((candidate, index) => (
            <button
              key={candidate.groupId ?? `${candidate.scope}-${index}`}
              type="button"
              role="tab"
              aria-selected={index === safeIndex}
              className={
                index === safeIndex
                  ? 'overview-standing-nav__tab overview-standing-nav__tab--active'
                  : 'overview-standing-nav__tab'
              }
              onClick={() => setTableIndex(index)}
            >
              {candidate.groupName ?? t('sport.standingTitle')}
            </button>
          ))}
        </div>
      )}
      <div className="overview-standing-wrap">
        <table className="overview-standing">
          <thead>
            <tr>
              <th scope="col" className="overview-standing__num">
                {t('sport.cols.position')}
              </th>
              <th scope="col">{t('sport.cols.team')}</th>
              <th scope="col" className="overview-standing__num">
                {t('sport.cols.played')}
              </th>
              <th scope="col" className="overview-standing__pts">
                {t('sport.cols.points')}
              </th>
            </tr>
          </thead>
          <tbody>
            {table.rows.map((row) => (
              <tr
                key={row.entryId}
                data-testid={`overview-standing-${row.entryId}`}
                className={
                  row.position === 1 ? 'overview-standing__row--leader' : undefined
                }
              >
                <td className="overview-standing__num">{row.position}</td>
                <td className="overview-standing__team">{row.displayName}</td>
                <td className="overview-standing__num">{row.played}</td>
                <td className="overview-standing__pts">{row.points}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="overview-panel__footer">
        <Link className="overview-link" to={href}>
          {t('sport.standingOpen')}
          <span className="overview-link__arrow" aria-hidden="true">
            <ChevronRightIcon size="sm" />
          </span>
        </Link>
      </p>
    </section>
  )
}

function SportUnitPanel({
  kind,
  unit,
  matchesHref,
}: {
  kind: 'recent' | 'next'
  unit: CockpitSportUnit | null
  matchesHref: string
}) {
  const { t } = useTranslation('cockpit')
  const titleId =
    kind === 'recent' ? 'cockpit-recent-unit' : 'cockpit-next-unit'
  const title =
    kind === 'recent' ? t('sport.recentTitle') : t('sport.nextTitle')
  const empty =
    kind === 'recent' ? t('sport.recentEmpty') : t('sport.nextEmpty')
  const subtitle = unit ? sportUnitSubtitle(unit, t) : null

  return (
    <section className="ds-panel" aria-labelledby={titleId}>
      <PanelHead id={titleId} icon={<MatchesNavIcon size="md" />}>
        {title}
      </PanelHead>
      {subtitle ? (
        <p className="overview-sport-unit__subtitle">{subtitle}</p>
      ) : null}
      {unit == null || unit.matches.length === 0 ? (
        <p className="overview-sport-unit__empty">{empty}</p>
      ) : (
        <div className="overview-match-scroll">
          <SportMatchLineList matches={unit.matches} />
        </div>
      )}
      <p className="overview-panel__footer">
        <Link className="overview-link" to={matchesHref}>
          {t('dimensions.openMatches')}
          <span className="overview-link__arrow" aria-hidden="true">
            <ChevronRightIcon size="sm" />
          </span>
        </Link>
      </p>
    </section>
  )
}

function sportUnitSubtitle(
  unit: CockpitSportUnit,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  const unitLabel =
    unit.unitKind === 'Matchday' && unit.matchdayNumber != null
      ? t('sport.unitMatchday', { number: unit.matchdayNumber })
      : (unit.roundName?.trim() || unit.stageName)
  const countLabel = t('sport.unitMatchCount', { count: unit.matchCount })
  return `${unitLabel} · ${countLabel}`
}

function SportMatchLineList({ matches }: { matches: CockpitMatchLine[] }) {
  const { t } = useTranslation('cockpit')
  const { i18n } = useTranslation()

  return (
    <ul className="overview-match-list">
      {matches.map((match) => {
        const score =
          match.score != null
            ? `${match.score.homeGoals}–${match.score.awayGoals}`
            : null
        const isLive = match.status === 'Live'
        const isFinished = match.status === 'Finished'
        const scheduledLabel =
          match.scheduledAt != null
            ? formatMatchSchedule(match.scheduledAt, i18n.language)
            : t('sport.scheduledUnset')

        return (
          <li key={match.matchId}>
            <Link
              className={
                isLive
                  ? 'overview-match overview-match--live'
                  : 'overview-match'
              }
              to={`/matches/${match.matchId}`}
              data-testid={`overview-match-${match.matchId}`}
            >
              <span className="overview-match__teams">
                <span className="overview-match__name">{match.homeDisplayName}</span>
                <span className="overview-match__vs" aria-hidden="true">
                  –
                </span>
                <span className="overview-match__name">{match.awayDisplayName}</span>
              </span>
              <span className="overview-match__aside">
                {isLive ? (
                  <Status density="context" tone="live" variant="soft" shape="rounded">
                    {t('sport.liveBadge')}
                  </Status>
                ) : isFinished && score ? (
                  <span className="overview-match__score">{score}</span>
                ) : (
                  <span className="overview-match__when">{scheduledLabel}</span>
                )}
                <span className="overview-match__chevron" aria-hidden="true">
                  <ChevronRightIcon size="sm" />
                </span>
              </span>
            </Link>
          </li>
        )
      })}
    </ul>
  )
}

function formatMatchSchedule(iso: string, locale: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return iso
  }
  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

function AttentionSignalSection({
  items,
  count,
  competitionId,
}: {
  items: CockpitSituation[]
  count: number
  competitionId: string
}) {
  const { t } = useTranslation('cockpit')
  const preview = items.slice(0, 2)

  return (
    <section className="overview-attention" aria-labelledby="cockpit-situations">
      <PanelHead id="cockpit-situations" icon={<OverviewAttentionIcon size="md" />}>
        {t('situations.heading')}
      </PanelHead>
      {count === 0 || preview.length === 0 ? (
        <p className="overview-panel__muted">{t('attention.empty')}</p>
      ) : (
        <>
          <ul className="shell-attention-drawer__list">
            {preview.map((situation) => (
              <OverviewAttentionItem
                key={`${situation.source}:${situation.targetType}:${situation.targetId}`}
                situation={situation}
                competitionId={competitionId}
              />
            ))}
          </ul>
          {count > preview.length && (
            <p className="overview-panel__muted">
              {t('attention.triageHint', { count })}
            </p>
          )}
        </>
      )}
    </section>
  )
}

/** Same visual recipe as AttentionDrawerItem — shared CSS classes, light preview. */
function OverviewAttentionItem({
  situation,
  competitionId,
}: {
  situation: CockpitSituation
  competitionId: string
}) {
  const { t } = useTranslation('cockpit')
  const href = situationHref(situation, competitionId)
  const isBlocking = situation.nature === 'Blocking'
  const natureTone = isBlocking ? 'error' : 'info'
  const toneClass = isBlocking
    ? 'shell-attention-drawer__item-link--blocking'
    : 'shell-attention-drawer__item-link--info'
  const staticToneClass = isBlocking
    ? 'shell-attention-drawer__item-static--blocking'
    : 'shell-attention-drawer__item-static--info'
  const targetLabel = situation.targetType
    ? attentionTargetTypeLabel(situation.targetType)
    : null

  const content = (
    <>
      <span
        className={`shell-attention-drawer__item-mark${
          isBlocking ? ' shell-attention-drawer__item-mark--blocking' : ''
        }`}
        aria-hidden="true"
      >
        <AttentionMarkIcon size="lg" />
      </span>
      <div className="shell-attention-drawer__item-main">
        <p className="shell-attention-drawer__item-title">
          {situationTitle(situation.source, situation.params)}
        </p>
        <div className="shell-attention-drawer__item-meta">
          <Status density="context" tone={natureTone} variant="soft" shape="rounded">
            {t(`nature.${situation.nature}`, { defaultValue: situation.nature })}
          </Status>
          {targetLabel && (
            <span className="shell-attention-drawer__item-target ds-meta">
              {targetLabel}
            </span>
          )}
        </div>
      </div>
      {href && (
        <ChevronRightIcon
          size="md"
          className="shell-attention-drawer__item-chevron"
          aria-hidden="true"
        />
      )}
    </>
  )

  return (
    <li className="shell-attention-drawer__item">
      {href ? (
        <Link className={`shell-attention-drawer__item-link ${toneClass}`} to={href}>
          {content}
        </Link>
      ) : (
        <div className={`shell-attention-drawer__item-static ${staticToneClass}`}>
          {content}
        </div>
      )}
    </li>
  )
}

function RegulationDimensionCard({
  variant = 'construction',
  regulation,
  gameRules,
  href,
  hrefLabel,
  actions,
  actionRunner,
}: {
  variant?: 'construction' | 'game'
  regulation: CockpitView['constructionDimensions']['regulation']
  gameRules?: CockpitReferenceStageGameRules | null
  href: string
  hrefLabel: string
  actions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')

  if (variant === 'game') {
    if (!gameRules) {
      return null
    }

    const isCup = gameRules.formatKind === 'Cup'
    const textFacts = buildGameRegulationTextFacts(gameRules, t)
    const showPoints = !isCup

    return (
      <article
        className={`${panelProminenceClass(regulation.prominence)} overview-config-card`}
        aria-labelledby="overview-regulation"
        data-testid="overview-regulation-game"
      >
        <PanelHead id="overview-regulation" icon={<RegulationIcon size="md" />}>
          {t('dimensions.regulation.title')}
        </PanelHead>
        <div className="overview-regulation-game">
          {showPoints && (
            <ul className="overview-chips overview-chips--game">
              <PointsChip
                tone="win"
                value={gameRules.winPoints}
                label={t('dimensions.regulation.pointsWin')}
              />
              <PointsChip
                tone="draw"
                value={gameRules.drawPoints}
                label={t('dimensions.regulation.pointsDraw')}
              />
              <PointsChip
                tone="loss"
                value={gameRules.lossPoints}
                label={t('dimensions.regulation.pointsLoss')}
              />
            </ul>
          )}
          {textFacts.length > 0 && (
            <ul
              className={
                isCup
                  ? 'overview-game-facts overview-game-facts--cup'
                  : 'overview-game-facts overview-game-facts--support'
              }
            >
              {textFacts.map((fact, index) => (
                <li
                  key={fact}
                  className={
                    isCup && index === 0
                      ? 'overview-game-facts__primary'
                      : undefined
                  }
                >
                  {fact}
                </li>
              ))}
            </ul>
          )}
        </div>
        <p className="overview-panel__footer">
          <OverviewLink to={href}>{hrefLabel}</OverviewLink>
        </p>
      </article>
    )
  }

  const competition = regulation.competition
  const condensed = isProminenceCondensed(regulation.prominence)
  const primaryGap = regulation.transitionReadiness.find((item) => !item.ready)

  return (
    <article
      className={panelProminenceClass(regulation.prominence)}
      aria-labelledby="overview-regulation"
    >
      <PanelHead id="overview-regulation" icon={<RegulationIcon size="md" />}>
        {t('dimensions.regulation.title')}
      </PanelHead>
      {!condensed && primaryGap && (
        <p className="overview-status overview-status--attention">
          <OverviewAttentionIcon size="sm" aria-hidden="true" />
          {t('dimensions.regulation.readinessNotReady', {
            transition: t(
              `dimensions.regulation.transitions.${primaryGap.transition}`,
              { defaultValue: primaryGap.transition },
            ),
          })}
        </p>
      )}
      {!condensed && !primaryGap && regulation.transitionReadiness.length > 0 && (
        <p className="overview-status">
          <span className="overview-status__icon" aria-hidden="true">
            <CheckIcon />
          </span>
          {t('dimensions.regulation.allReady')}
        </p>
      )}
      <ul className="overview-chips">
        <PointsChip
          tone="win"
          value={competition.winPoints}
          label={t('dimensions.regulation.pointsWin')}
        />
        <PointsChip
          tone="draw"
          value={competition.drawPoints}
          label={t('dimensions.regulation.pointsDraw')}
        />
        <PointsChip
          tone="loss"
          value={competition.lossPoints}
          label={t('dimensions.regulation.pointsLoss')}
        />
      </ul>
      <p className="overview-panel__muted">
        {t('dimensions.regulation.formatMeta', {
          periods: competition.numberOfPeriods,
          duration: competition.durationPerPeriod,
          min: competition.minimumTeams,
          max: competition.maximumTeams,
        })}
      </p>
      <ActionButtons actions={actions} actionRunner={actionRunner} />
      <p className="overview-panel__footer">
        <OverviewLink to={href}>{hrefLabel}</OverviewLink>
      </p>
    </article>
  )
}

function PointsChip({
  value,
  label,
  tone,
}: {
  value: number
  label: string
  tone: 'win' | 'draw' | 'loss'
}) {
  const { t } = useTranslation('cockpit')

  return (
    <li className={`overview-chip overview-chip--${tone}`}>
      <span className="overview-chip__value">
        {t('dimensions.regulation.pointsValue', { value })}
      </span>
      <span className="overview-chip__label">{label}</span>
    </li>
  )
}

/** Exit link toward the owning workspace — right-aligned, réf. V9. */
function OverviewLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link className="overview-link" to={to}>
      {children}
      <span className="overview-link__arrow" aria-hidden="true">
        →
      </span>
    </Link>
  )
}

function TeamsPanel({
  variant = 'construction',
  dimension,
  entries,
  href,
  hrefLabel,
  actions,
  actionRunner,
}: {
  variant?: 'construction' | 'identity'
  dimension: CockpitDimension
  entries: OrganisationEntry[]
  href: string
  hrefLabel: string
  actions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')
  const identity = variant === 'identity'
  const activeCount = Number(dimension.facts.activeCount ?? '0')
  const minimumTeams = Number(dimension.facts.minimumTeams ?? '0')
  const maximumTeams = dimension.facts.maximumTeams
  const activeEntries = entries.filter((entry) => entry.status === 'Active')
  const preview = activeEntries.slice(0, 6)
  const overflow = Math.max(0, activeCount - preview.length)
  const meetsMinimum = activeCount >= minimumTeams && minimumTeams > 0

  return (
    <article
      className={panelProminenceClass(dimension.prominence)}
      aria-labelledby="overview-teams"
    >
      <PanelHead id="overview-teams" icon={<TeamsIcon size="md" />}>
        {t('dimensions.teams.title')}
      </PanelHead>
      <p className="overview-figure">
        <span className="overview-figure__value">{activeCount}</span>
        <span className="overview-figure__label">
          {identity
            ? t('dimensions.teams.figureLabelIdentity')
            : t('dimensions.teams.figureLabel')}
        </span>
      </p>
      {preview.length > 0 && (
        <ul
          className="overview-crests"
          aria-label={
            identity
              ? t('dimensions.teams.crestsLabelIdentity')
              : t('dimensions.teams.crestsLabel')
          }
        >
          {preview.map((entry) => (
            <li
              key={entry.entryId}
              className="overview-crest"
              title={entry.displayName}
            >
              <TeamCrest
                name={entry.displayName}
                logoMediaId={entry.logoMediaId}
                primaryColor={entry.primaryColor}
                size="sm"
              />
            </li>
          ))}
          {overflow > 0 && (
            <li className="overview-crest overview-crest--more">+{overflow}</li>
          )}
        </ul>
      )}
      {!identity && (
        <div className="overview-flags">
          {meetsMinimum ? (
            <span className="overview-flag overview-flag--ok">
              <CheckIcon size="sm" aria-hidden="true" />
              {t('dimensions.teams.complete', { count: activeCount })}
            </span>
          ) : (
            <span className="overview-flag overview-flag--warn">
              <OverviewAttentionIcon size="sm" aria-hidden="true" />
              {t('dimensions.teams.minimum', { minimumTeams })}
            </span>
          )}
          {maximumTeams && (
            <span className="overview-flag">
              {t('dimensions.teams.capacity', { max: maximumTeams })}
            </span>
          )}
        </div>
      )}
      {!identity && (
        <ActionButtons actions={actions} actionRunner={actionRunner} />
      )}
      <p className="overview-panel__footer">
        <OverviewLink to={href}>{hrefLabel}</OverviewLink>
      </p>
    </article>
  )
}

/**
 * Structure — editorial rows (construction) or one-line facts (En cours condensed).
 * Journées détaillées / noms de groupes absents du Read — pas inventés ici.
 */
function StructurePanel({
  variant = 'construction',
  dimension,
  stages,
  matchTotal,
  swissByes,
  href,
  hrefLabel,
  actions,
  actionRunner,
}: {
  variant?: 'construction' | 'condensed'
  dimension: CockpitDimension
  stages: CockpitView['operationalFocus']['stages']
  matchTotal: number
  swissByes: CockpitView['operationalFocus']['swissByes']
  href: string
  hrefLabel: string
  actions: CockpitAction[]
  actionRunner: ActionRunner
}) {
  const { t } = useTranslation('cockpit')
  const formatKind = dimension.facts.formatKind
  const formatConfigured = Boolean(formatKind) && formatKind !== 'None'
  const groupCount = Number(dimension.facts.groupCount ?? '0')
  const roundCount = Number(dimension.facts.roundCount ?? '0')
  const matchdayCount = Number(dimension.facts.matchdayCount ?? '0')
  const slotCount = Number(dimension.facts.slotCount ?? '0')
  const swissRoundCount = Number(dimension.facts.swissRoundCount ?? '0')
  const swissByeCount = Number(dimension.facts.swissByeCount ?? '0')
  const stageNames = stages.map((stage) => stage.name).filter(Boolean)

  if (variant === 'condensed') {
    const formatLabel =
      formatConfigured && formatKind
        ? structureFormatKindLabel(formatKind as StructureFormatKind)
        : t('dimensions.structure.none')
    const metrics = buildStructureCondensedMetrics({
      t,
      formatKind,
      stages,
      stageNames,
      groupCount,
      roundCount,
      matchdayCount,
      swissRoundCount,
    })

    return (
      <article
        className={`${panelProminenceClass(dimension.prominence)} overview-config-card`}
        aria-labelledby="overview-structure"
        data-testid="overview-structure-condensed"
      >
        <PanelHead id="overview-structure" icon={<StructureIcon size="md" />}>
          {t('dimensions.structure.title')}
        </PanelHead>
        <div className="overview-structure-hero">
          <p
            className="overview-structure-format"
            data-testid="overview-structure-format"
          >
            {formatLabel}
          </p>
          {metrics.length > 0 && (
            <ul className="overview-structure-pills">
              {metrics.map((metric) => (
                <li key={metric} className="overview-structure-pill">
                  {metric}
                </li>
              ))}
            </ul>
          )}
        </div>
        <p className="overview-panel__footer">
          <OverviewLink to={href}>{hrefLabel}</OverviewLink>
        </p>
      </article>
    )
  }

  return (
    <article
      className={panelProminenceClass(dimension.prominence)}
      aria-labelledby="overview-structure"
    >
      <PanelHead id="overview-structure" icon={<StructureIcon size="md" />}>
        {t('dimensions.structure.title')}
      </PanelHead>
      <ul className="overview-rows">
        <StructureRow
          done={formatConfigured}
          label={
            formatConfigured
              ? structureFormatKindLabel(formatKind as StructureFormatKind)
              : t('dimensions.structure.none')
          }
          detail={
            stageNames.length > 0
              ? t('dimensions.structure.phasesDetail', {
                  count: stages.length,
                  names: stageNames.join(' · '),
                })
              : undefined
          }
        />
        {groupCount > 0 && (
          <StructureRow
            done
            label={t('dimensions.structure.groups', { count: groupCount })}
          />
        )}
        {roundCount > 0 && (
          <StructureRow
            done
            label={t('dimensions.structure.rounds', { count: roundCount })}
          />
        )}
        {formatKind === 'Swiss' && swissRoundCount > 0 && (
          <StructureRow
            done={matchdayCount > 0}
            label={t('dimensions.structure.swissRounds', {
              generated: matchdayCount,
              planned: swissRoundCount,
            })}
          />
        )}
        {formatKind !== 'Swiss' && matchdayCount > 0 && (
          <StructureRow
            done
            label={t('dimensions.structure.matchdays', { count: matchdayCount })}
          />
        )}
        {formatKind === 'Swiss' && swissByeCount > 0 && (
          <StructureRow
            done
            label={t('dimensions.structure.swissByes', { count: swissByeCount })}
            detail={t('dimensions.structure.swissByesHint')}
          />
        )}
        {formatKind === 'Swiss' && swissByes.length > 0 && (
          <li className="overview-row overview-row--stack">
            <ul className="overview-rows overview-rows--nested">
              {swissByes.map((bye) => (
                <li key={`${bye.stageId}:${bye.roundIndex}:${bye.entryId}`}>
                  {t('dimensions.structure.swissByeLine', {
                    round: bye.roundIndex,
                    name: bye.entryDisplayName,
                  })}
                </li>
              ))}
            </ul>
          </li>
        )}
        {slotCount > 0 && (
          <StructureRow
            done
            label={t('dimensions.structure.slots', { count: slotCount })}
          />
        )}
        <StructureRow
          done={matchTotal > 0}
          label={
            matchTotal > 0
              ? t('dimensions.structure.matches', { count: matchTotal })
              : t('dimensions.structure.matchesNone')
          }
          detail={
            matchTotal === 0 && slotCount > 0
              ? t('dimensions.structure.matchesPending', { count: slotCount })
              : undefined
          }
        />
      </ul>
      <ActionButtons actions={actions} actionRunner={actionRunner} />
      <p className="overview-panel__footer">
        <OverviewLink to={href}>{hrefLabel}</OverviewLink>
      </p>
    </article>
  )
}

function buildStructureCondensedMetrics({
  t,
  formatKind,
  stages,
  stageNames,
  groupCount,
  roundCount,
  matchdayCount,
  swissRoundCount,
}: {
  t: (key: string, options?: Record<string, unknown>) => string
  formatKind: string | undefined
  stages: CockpitView['operationalFocus']['stages']
  stageNames: string[]
  groupCount: number
  roundCount: number
  matchdayCount: number
  swissRoundCount: number
}): string[] {
  const metrics: string[] = []
  const push = (value: string | null | undefined) => {
    if (value && metrics.length < 3) {
      metrics.push(value)
    }
  }

  const phasesLabel =
    stages.length > 0
      ? stageNames.length > 0
        ? t('dimensions.structure.phasesDetail', {
            count: stages.length,
            names: stageNames.join(' · '),
          })
        : t('dimensions.structure.phases', { count: stages.length })
      : null

  switch (formatKind) {
    case 'Championship':
      push(phasesLabel)
      if (matchdayCount > 0) {
        push(t('dimensions.structure.matchdays', { count: matchdayCount }))
      }
      if (groupCount > 0) {
        push(t('dimensions.structure.groups', { count: groupCount }))
      }
      break
    case 'Groups':
      push(phasesLabel)
      if (groupCount > 0) {
        push(t('dimensions.structure.groups', { count: groupCount }))
      }
      if (matchdayCount > 0) {
        push(t('dimensions.structure.matchdays', { count: matchdayCount }))
      }
      break
    case 'Cup':
      push(phasesLabel)
      if (roundCount > 0) {
        push(t('dimensions.structure.rounds', { count: roundCount }))
      }
      break
    case 'Swiss':
      push(phasesLabel)
      if (swissRoundCount > 0 || matchdayCount > 0) {
        push(
          t('dimensions.structure.swissRounds', {
            generated: matchdayCount,
            planned: swissRoundCount,
          }),
        )
      }
      break
    default:
      push(phasesLabel)
      if (groupCount > 0) {
        push(t('dimensions.structure.groups', { count: groupCount }))
      }
      if (roundCount > 0) {
        push(t('dimensions.structure.rounds', { count: roundCount }))
      }
      if (matchdayCount > 0) {
        push(t('dimensions.structure.matchdays', { count: matchdayCount }))
      }
      break
  }

  return metrics
}

/** Text facts for En cours Règlement (points rendered separately as chips). */
function buildGameRegulationTextFacts(
  rules: CockpitReferenceStageGameRules,
  t: (key: string, options?: Record<string, unknown>) => string,
): string[] {
  const facts: string[] = []
  const push = (value: string | null | undefined) => {
    if (value && facts.length < 3) {
      facts.push(value)
    }
  }

  const duration = t('dimensions.regulation.formatDuration', {
    periods: rules.numberOfPeriods,
    duration: rules.durationPerPeriod,
  })

  if (rules.formatKind === 'Cup') {
    if (rules.numberOfLegs >= 2) {
      push(
        rules.aggregateScoring
          ? t('dimensions.regulation.twoLegsAggregate')
          : t('dimensions.regulation.twoLegs'),
      )
    } else {
      push(t('dimensions.regulation.elimination'))
    }
    push(duration)

    const extras: string[] = []
    if (rules.hasExtraTime || rules.hasTieExtraTime) {
      extras.push(t('dimensions.regulation.extraTime'))
    }
    if (rules.hasPenaltyShootout || rules.hasTiePenaltyShootout) {
      extras.push(t('dimensions.regulation.penalties'))
    }
    if (extras.length > 0) {
      push(extras.join(' · '))
    }
    return facts
  }

  push(duration)
  if (
    rules.formatKind === 'Swiss' &&
    rules.swissPlannedRounds != null &&
    rules.swissPlannedRounds > 0
  ) {
    push(
      t('dimensions.regulation.swissPlannedRounds', {
        count: rules.swissPlannedRounds,
      }),
    )
  }

  return facts
}

function StructureRow({
  done,
  label,
  detail,
}: {
  done: boolean
  label: string
  detail?: string
}) {
  return (
    <li className="overview-row">
      <span
        className={`overview-row__mark ${
          done ? 'overview-row__mark--done' : 'overview-row__mark--pending'
        }`}
        aria-hidden="true"
      >
        {done ? <CheckIcon /> : <PendingCircleIcon />}
      </span>
      <span className="overview-row__label">{label}</span>
      {detail && <span className="overview-row__detail">{detail}</span>}
    </li>
  )
}

function ActionButtons({
  actions,
  actionRunner,
  emphasizeFirst = false,
}: {
  actions: CockpitAction[]
  actionRunner: ActionRunner
  /** Primary only when this region owns the single CTA — Identité §11. */
  emphasizeFirst?: boolean
}) {
  const { t } = useTranslation('cockpit')
  if (actions.length === 0) {
    return null
  }

  return (
    <div className="overview-actions" aria-busy={actionRunner.mutation.isPending}>
      {actions.map((action, index) => {
        const key = cockpitActionKey(action)
        const busy =
          actionRunner.mutation.isPending && actionRunner.activeKey === key
        const label = actionLabel(action.code, {
          name: action.params?.stageName,
          ...action.params,
        })
        const intent = actionRunner.resolveIntent(action)
        const variant =
          emphasizeFirst && index === 0
            ? 'ds-btn--primary'
            : 'ds-btn--secondary'
        if (intent.kind === 'navigate') {
          return (
            <Link
              key={key}
              className={`ds-btn ${variant}`}
              to={intent.to}
              aria-label={`${label} — ${t('actions.openSpace')}`}
            >
              {label}
            </Link>
          )
        }
        return (
          <button
            key={key}
            type="button"
            className={`ds-btn ${variant}`}
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

function NaturalProgressionSection({
  data,
  actionRunner,
  lifecycleActions,
}: {
  data: CockpitView
  actionRunner: ActionRunner
  /** Prepare/Start leftovers — folded here (no separate Transitions card, réf. V9). */
  lifecycleActions: CockpitAction[]
}) {
  const { t } = useTranslation('cockpit')
  const code = data.naturalProgression?.code
  const matched = code ? findActionByCode(data.availableActions, code) : undefined
  const orgHref = `/competitions/${data.competitionId}/organisation`
  const matchesHref = `/competitions/${data.competitionId}/matches`
  const classementsHref = `/competitions/${data.competitionId}/classements`
  const hasPrimary = Boolean(code)
  const showLifecycle = lifecycleActions.length > 0

  return (
    <section
      className="ds-panel overview-panel--next"
      aria-labelledby="cockpit-progression"
    >
      <PanelHead id="cockpit-progression" icon={<NextActionIcon size="md" />}>
        {t('progression.heading')}
      </PanelHead>
      {hasPrimary ? (
        <>
          <div className="overview-hero">
            <span className="overview-badge overview-badge--brand" aria-hidden="true">
              {progressionBadgeIcon(code!)}
            </span>
            <div className="overview-hero__body">
              <p className="overview-hero__title">{actionLabel(code!)}</p>
              <p className="overview-hero__sub">
                {t(`progression.codes.${code}`, {
                  defaultValue: actionLabel(code!),
                })}
              </p>
            </div>
          </div>
          {matched ? (
            <ActionButtons
              actions={[matched]}
              actionRunner={actionRunner}
              emphasizeFirst
            />
          ) : code === 'ContinueOrganisation' ? (
            <p className="overview-actions">
              <Link className="ds-btn ds-btn--primary" to={orgHref}>
                {t('dimensions.openOrganisation')}
              </Link>
            </p>
          ) : code === 'OpenMatches' ? (
            <p className="overview-actions">
              <Link className="ds-btn ds-btn--primary" to={matchesHref}>
                {t('dimensions.openMatches')}
              </Link>
            </p>
          ) : code === 'OpenConsultation' ? (
            <p className="overview-actions">
              <Link className="ds-btn ds-btn--primary" to={classementsHref}>
                {t('nav.classements.title')}
              </Link>
            </p>
          ) : null}
        </>
      ) : showLifecycle ? (
        <>
          <div className="overview-hero">
            <span className="overview-badge overview-badge--brand" aria-hidden="true">
              {progressionBadgeIcon(lifecycleActions[0].code)}
            </span>
            <div className="overview-hero__body">
              <p className="overview-hero__title">
                {actionLabel(lifecycleActions[0].code)}
              </p>
              <p className="overview-hero__sub">
                {t(`progression.codes.${lifecycleActions[0].code}`, {
                  defaultValue: actionLabel(lifecycleActions[0].code),
                })}
              </p>
            </div>
          </div>
          <ActionButtons
            actions={lifecycleActions}
            actionRunner={actionRunner}
            emphasizeFirst
          />
        </>
      ) : (
        <p className="overview-panel__muted">{t('progression.none')}</p>
      )}
      {hasPrimary && showLifecycle && (
        <ActionButtons actions={lifecycleActions} actionRunner={actionRunner} />
      )}
    </section>
  )
}

/** Content glyph of the suggested step — flag when unmapped. */
function progressionBadgeIcon(code: string) {
  if (
    code === 'MaterializeMatches' ||
    code === 'MaterializeFromOccupiedSlots' ||
    code === 'GenerateNextRound'
  ) {
    return <CreateMatchesIcon />
  }
  if (code === 'ContinueOrganisation') {
    return <TeamsIcon />
  }
  if (code === 'OpenMatches') {
    return <CalendarIcon />
  }
  if (code === 'OpenConsultation') {
    return <CompletedIcon />
  }
  if (code === 'CompleteCompetition' || code === 'PrepareCompetition') {
    return <CheckIcon />
  }
  if (code === 'StartCompetition') {
    return <InProgressIcon />
  }
  return <NextActionIcon />
}

