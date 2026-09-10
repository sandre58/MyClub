import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { fetchCompetitionOverview, fetchStructureView } from '../api';
import { TeamCrest } from '../design-system/TeamCrest';
import { AttentionGroup } from '../design-system/components/AttentionGroup';
import { OverviewNextAction } from '../design-system/components/OverviewNextAction';
import { OverviewPodium } from '../design-system/components/OverviewPodium';
import { PanelHead } from '../design-system/components/PanelHead';
import { Status } from '../design-system/components/Status';
import { Tooltip } from '../design-system/components/Tooltip';
import {
  ChevronRightIcon,
  ClassementsNavIcon,
  MatchesNavIcon,
} from '../design-system/icons/shellIcons';
import { AttentionSituationRow } from '../shell/AttentionSituationRow';
import { TextLink } from '../design-system/components/TextLink';
import { CalendarIcon } from '../design-system/icons/overviewIcons';
import { actionLabel } from '../i18n/actionLabels';
import { structureFormatKindLabel } from '../i18n/enumLabels';
import { toIntlLocale } from '../i18n/intlLocale';
import { queryKeys } from '../queryKeys';
import type {
  OverviewAction,
  OverviewCalendarSummary,
  OverviewDimension,
  OverviewMatchLine,
  OverviewReferenceStageGameRules,
  OverviewSituation,
  OverviewSportUnit,
  OverviewStandingCompact,
  CompetitionOutcome,
  OverviewView,
  StructureEntry,
  StructureFormatKind,
} from '../types';
import { ErrorState, LoadingState, MutationError, PendingLabel } from '../ui';
import {
  overviewActionKey,
  resolveOverviewActionIntent,
} from './overviewActions';
import {
  actionsForDraw,
  actionsForSlot,
  actionsForStage,
  findActionByCode,
  isTeamAdminAction,
  orderSituationsForDisplay,
  panelProminenceClass,
  primaryTeamActions,
  secondaryActions,
  sortConstructionSlots,
  stageWideOperationalActions,
} from './overviewComposition';
import './overview.css';
import {
  NextActionIcon,
  OverviewAttentionIcon,
  RegulationIcon,
  StructureIcon,
  TeamsIcon,
} from '../design-system/icons/overviewIcons';

/**
 * Competition Overview — GET /competitions/{id}/overview.
 * Composes Read facts (prominence, situations, actions); does not recompute métier rules.
 */
export function CompetitionOverviewPage() {
  const { competitionId = '' } = useParams();

  const query = useQuery({
    queryKey: queryKeys.competitions.overview(competitionId),
    queryFn: () => fetchCompetitionOverview(competitionId),
    enabled: competitionId.length > 0,
  });

  return (
    <main id="main" className="page page--overview">
      {query.isPending && !query.data && <LoadingState />}
      {query.isError && !query.data && <ErrorState error={query.error} />}
      {query.data && <OverviewViewBody data={query.data} />}
    </main>
  );
}

function OverviewViewBody({ data }: { data: OverviewView }) {
  const { t } = useTranslation('overview');
  const actionRunner = useOverviewActionRunner(data);

  const orgQuery = useQuery({
    queryKey: queryKeys.competitions.structure(data.competitionId),
    queryFn: () => fetchStructureView(data.competitionId),
  });

  const slotActions = (slot: Parameters<typeof actionsForSlot>[1]) => {
    const base = actionsForSlot(data.availableActions, slot);
    if (slot === 'teams') {
      return primaryTeamActions(base);
    }
    if (slot === 'operational') {
      // Stage/draw row actions are attached per object — not dumped here.
      return stageWideOperationalActions(base);
    }
    return base;
  };

  const renderedKeys = useMemo(() => {
    const keys = new Set<string>();
    const mark = (actions: OverviewAction[]) => {
      for (const action of actions) {
        keys.add(overviewActionKey(action));
      }
    };
    mark(slotActions('teams'));
    mark(slotActions('structure'));
    mark(slotActions('regulation'));
    mark(slotActions('matches'));
    mark(slotActions('operational'));
    mark(slotActions('closure'));
    for (const stage of data.operationalFocus.stages) {
      mark(actionsForStage(data.availableActions, stage.stageId));
    }
    for (const draw of data.operationalFocus.draws) {
      mark(actionsForDraw(data.availableActions, draw.stageId, draw.drawId));
    }
    for (const action of data.availableActions) {
      if (isTeamAdminAction(action.code)) {
        keys.add(overviewActionKey(action));
      }
    }
    const progression = data.naturalProgression?.code
      ? findActionByCode(data.availableActions, data.naturalProgression.code)
      : undefined;
    if (progression) {
      keys.add(overviewActionKey(progression));
    }
    return keys;
    // eslint-disable-next-line react-hooks/exhaustive-deps -- derived from data
  }, [data]);

  const leftover = secondaryActions(data.availableActions, renderedKeys);
  const lifecycleActions = leftover.filter(
    (action) =>
      action.code === 'PrepareCompetition' ||
      action.code === 'StartCompetition',
  );
  const inProgress = data.cycleReading.code === 'InProgress';
  const completedLike =
    data.cycleReading.code === 'Completed' ||
    data.cycleReading.code === 'Archived';
  /** En cours + Terminée share Config + Sport composition (not Préparation). */
  const operationalOverview = inProgress || completedLike;
  const generatedCalendar =
    !operationalOverview && data.preparationFocus === 'GeneratedCalendar';
  const slots = sortConstructionSlots(data).filter(
    (slot) => slot !== 'matches',
  );
  const teamsVisible = slots.includes('teams');
  const regulationVisible = slots.includes('regulation');
  const structureVisible = slots.includes('structure') && !generatedCalendar;
  const gameRules = data.operationalFocus.referenceStageGameRules;
  const gameRegulationVisible =
    operationalOverview && regulationVisible && gameRules != null;
  const structureHref = `/competitions/${data.competitionId}/structure`;
  const teamsHref = `/competitions/${data.competitionId}/teams`;
  const matchesHref = `/competitions/${data.competitionId}/matches`;
  const classementsHref = `/competitions/${data.competitionId}/classements`;
  const showProgression =
    Boolean(data.naturalProgression?.code) || lifecycleActions.length > 0;
  const focus = data.operationalFocus;
  // En cours + Terminée: standingCompact when Host projects it (independent of Outcome).
  // Terminée Résultat: Host presentation Winner|Podium — silence when Outcome null.
  const showStanding = operationalOverview && focus.standingCompact != null;
  const showOutcome =
    completedLike &&
    data.competitionOutcome != null &&
    data.competitionOutcome.places.length > 0;
  // En cours: always Dernières + Prochaines (empty-state). Terminée: Dernières always; Prochaines only if nextUnit.
  const showRecentUnit = operationalOverview;
  const showNextUnit = inProgress || (completedLike && focus.nextUnit != null);
  const showTemporalUnits = showRecentUnit || showNextUnit;
  const showSport = showStanding || showOutcome || showTemporalUnits;
  const showCalendar = generatedCalendar && data.calendarSummary != null;

  const attentionItems = orderSituationsForDisplay(data.attentionSummary.items);
  const attentionCount = data.attentionSummary.count;
  const showAttention = attentionCount > 0 && attentionItems.length > 0;
  const showPrepConfig =
    !operationalOverview && (teamsVisible || regulationVisible);
  const showOperationalConfig =
    structureVisible || teamsVisible || gameRegulationVisible;

  const operationalConfigBand =
    operationalOverview && showOperationalConfig ? (
      <div
        className="overview__config overview__mid overview__mid--condensed-config"
        data-testid="overview-region-config"
      >
        {structureVisible && (
          <StructurePanel
            variant="condensed"
            dimension={data.constructionDimensions.structure}
            stages={data.operationalFocus.stages}
            matchTotal={data.operationalFocus.matchCounts.total}
            href={structureHref}
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
            href={teamsHref}
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
            href={structureHref}
            hrefLabel={t('dimensions.openRegulation')}
            actions={[]}
            actionRunner={actionRunner}
          />
        )}
      </div>
    ) : null;

  const showResultColumn = showStanding || showOutcome;
  const sportBand = showSport ? (
    <div
      className={
        showResultColumn && showTemporalUnits
          ? 'overview__sport'
          : 'overview__sport overview__sport--solo'
      }
      data-testid="overview-region-sport"
    >
      {showResultColumn && (
        <div
          className="overview__sport-stack"
          data-testid="overview-sport-result-column"
        >
          {showOutcome && data.competitionOutcome && (
            <OutcomePodiumPanel
              outcome={data.competitionOutcome}
              href={classementsHref}
            />
          )}
          {showStanding && focus.standingCompact && (
            <StandingCompactPanel
              standing={focus.standingCompact}
              href={classementsHref}
            />
          )}
        </div>
      )}
      {showTemporalUnits && (
        <div
          className="overview__sport-stack"
          data-testid="overview-sport-temporal-column"
        >
          {showRecentUnit && (
            <SportUnitPanel
              kind="recent"
              unit={focus.recentUnit}
              matchesHref={matchesHref}
            />
          )}
          {showNextUnit && (
            <SportUnitPanel
              kind="next"
              unit={focus.nextUnit}
              matchesHref={matchesHref}
            />
          )}
        </div>
      )}
    </div>
  ) : null;

  return (
    <div className="overview ds-page">
      {operationalOverview ? (
        <>
          {showAttention && (
            <div
              className="overview__signals"
              data-testid="overview-region-attention"
            >
              <AttentionSignalSection
                items={attentionItems}
                count={attentionCount}
                competitionId={data.competitionId}
              />
            </div>
          )}
          {operationalConfigBand}
          {showProgression && (
            <div
              className="overview__signals"
              data-testid="overview-region-progression"
            >
              <NaturalProgressionSection
                data={data}
                actionRunner={actionRunner}
                lifecycleActions={lifecycleActions}
              />
            </div>
          )}
          {sportBand}
        </>
      ) : (
        <>
          {showProgression && (
            <div
              className="overview__signals"
              data-testid="overview-region-progression"
            >
              <NaturalProgressionSection
                data={data}
                actionRunner={actionRunner}
                lifecycleActions={lifecycleActions}
              />
            </div>
          )}

          {showAttention && (
            <div
              className="overview__signals"
              data-testid="overview-region-attention"
            >
              <AttentionSignalSection
                items={attentionItems}
                count={attentionCount}
                competitionId={data.competitionId}
              />
            </div>
          )}

          {showCalendar && data.calendarSummary && (
            <div
              className="overview__sport overview__sport--solo"
              data-testid="overview-region-calendar"
            >
              <CalendarSummaryPanel
                summary={data.calendarSummary}
                matchesHref={matchesHref}
              />
            </div>
          )}

          {showPrepConfig && (
            <div
              className={
                generatedCalendar
                  ? 'overview__config overview__mid overview__mid--condensed-config'
                  : 'overview__config overview__mid'
              }
              data-testid="overview-region-config"
            >
              {teamsVisible && (
                <TeamsPanel
                  variant={generatedCalendar ? 'identity' : 'construction'}
                  dimension={data.constructionDimensions.teams}
                  entries={orgQuery.data?.participants.entries ?? []}
                  href={teamsHref}
                  hrefLabel={t('dimensions.openTeams')}
                  actions={generatedCalendar ? [] : slotActions('teams')}
                  actionRunner={actionRunner}
                />
              )}
              {regulationVisible && (
                <RegulationDimensionCard
                  variant="construction"
                  regulation={data.constructionDimensions.regulation}
                  href={structureHref}
                  hrefLabel={t('dimensions.openRegulation')}
                  actions={generatedCalendar ? [] : slotActions('regulation')}
                  actionRunner={actionRunner}
                />
              )}
            </div>
          )}

          {structureVisible && (
            <div
              className="overview__structure"
              data-testid="overview-region-structure"
            >
              <StructurePanel
                variant="construction"
                dimension={data.constructionDimensions.structure}
                stages={data.operationalFocus.stages}
                matchTotal={data.operationalFocus.matchCounts.total}
                href={structureHref}
                hrefLabel={t('dimensions.openStructure')}
                actions={slotActions('structure')}
                actionRunner={actionRunner}
              />
            </div>
          )}
        </>
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
  );
}

type ActionRunner = ReturnType<typeof useOverviewActionRunner>;

type MaterializeFollowUp = {
  createdCount: number;
  attachedCount: number;
  alreadyComplete: boolean;
};

type MaterializeResult = {
  createdCount: number;
  attachedMatchIds: string[];
  alreadyComplete: boolean;
};

function useOverviewActionRunner(data: OverviewView) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [activeKey, setActiveKey] = useState<string | null>(null);
  const [materializeFollowUp, setMaterializeFollowUp] =
    useState<MaterializeFollowUp | null>(null);

  const mutation = useMutation({
    mutationFn: async (action: OverviewAction) => {
      const intent = resolveOverviewActionIntent(action, data);
      if (intent.kind !== 'execute') {
        throw new Error(`Action ${action.code} is not executable here`);
      }
      return intent.run();
    },
    onSuccess: async (result, action) => {
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.overview(data.competitionId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.attention(data.competitionId),
      });
      await queryClient.invalidateQueries({
        queryKey: queryKeys.competitions.workspace(data.competitionId),
      });

      if (action.code === 'MaterializeMatches') {
        const materialize = result as MaterializeResult;
        await queryClient.invalidateQueries({
          queryKey: queryKeys.competitions.detail(data.competitionId),
        });
        await queryClient.invalidateQueries({
          queryKey: queryKeys.competitions.structure(data.competitionId),
        });
        if (action.stageId) {
          await queryClient.invalidateQueries({
            queryKey: queryKeys.matches.byStage(action.stageId),
          });
        } else {
          await queryClient.invalidateQueries({
            queryKey: ['matches', 'by-stage'],
          });
        }
        setMaterializeFollowUp({
          createdCount: materialize.createdCount,
          attachedCount: materialize.attachedMatchIds.length,
          alreadyComplete: materialize.alreadyComplete,
        });
      }

      setActiveKey(null);
    },
    onError: () => {
      setActiveKey(null);
    },
  });

  function onActionClick(action: OverviewAction) {
    const intent = resolveOverviewActionIntent(action, data);
    if (intent.kind === 'navigate') {
      void navigate(intent.to);
      return;
    }
    if (intent.kind === 'execute') {
      setActiveKey(overviewActionKey(action));
      mutation.mutate(action);
    }
  }

  return {
    onActionClick,
    resolveIntent: (action: OverviewAction) =>
      resolveOverviewActionIntent(action, data),
    mutation,
    activeKey,
    materializeFollowUp,
    clearMaterializeFollowUp: () => setMaterializeFollowUp(null),
  };
}

function MaterializeFollowUpBanner({
  competitionId,
  followUp,
  onDismiss,
}: {
  competitionId: string;
  followUp: MaterializeFollowUp;
  onDismiss: () => void;
}) {
  const { t } = useTranslation('overview');
  const matchesHref = `/competitions/${competitionId}/matches`;
  const hasMatches = followUp.attachedCount > 0;

  return (
    <section
      className="ds-panel"
      aria-labelledby="overview-materialize-followup"
      role="status"
    >
      <PanelHead
        id="overview-materialize-followup"
        title={t('materializeFollowUp.heading')}
        icon={<NextActionIcon size="md" />}
      />
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
          <button
            type="button"
            className="ds-btn ds-btn--secondary"
            onClick={onDismiss}
          >
            {t('materializeFollowUp.dismiss')}
          </button>
        </p>
      )}
    </section>
  );
}

function OutcomePodiumPanel({
  outcome,
  href,
}: {
  outcome: CompetitionOutcome;
  href: string;
}) {
  const { t } = useTranslation('overview');
  const byRank = (rank: number) =>
    outcome.places.find((place) => place.rank === rank);

  const first = byRank(1);
  if (!first) {
    return null;
  }

  const isPodium = outcome.presentation === 'Podium';
  const second = isPodium ? byRank(2) : undefined;
  const third = isPodium ? byRank(3) : undefined;
  const winnerLabel = isPodium
    ? t('sport.outcomeChampionLabel')
    : t('sport.outcomeWinnerLabel');

  return (
    <section className="ds-panel" aria-labelledby="overview-outcome-podium">
      <PanelHead
        id="overview-outcome-podium"
        title={t('sport.outcomeTitle')}
        icon={<ClassementsNavIcon size="md" />}
      />
      {isPodium ? (
        <OverviewPodium
          testId="overview-outcome-podium"
          presentation={outcome.presentation}
          steps={[
            {
              rank: 1,
              name: first.displayName,
              subtitle: winnerLabel,
              testId: `overview-outcome-${first.entryId}`,
            },
            ...(second
              ? [
                  {
                    rank: 2 as const,
                    name: second.displayName,
                    testId: `overview-outcome-${second.entryId}`,
                  },
                ]
              : []),
            ...(third
              ? [
                  {
                    rank: 3 as const,
                    name: third.displayName,
                    testId: `overview-outcome-${third.entryId}`,
                  },
                ]
              : []),
          ]}
        />
      ) : (
        <div
          className="overview-outcome overview-outcome--winner"
          data-testid="overview-outcome-podium"
          data-presentation={outcome.presentation}
        >
          <div
            className="overview-outcome__hero"
            data-testid={`overview-outcome-${first.entryId}`}
          >
            <span
              className="overview-outcome__hero-rank ds-tabular"
              aria-hidden="true"
            >
              1
            </span>
            <p className="overview-outcome__hero-name">{first.displayName}</p>
            <p className="overview-outcome__hero-label">{winnerLabel}</p>
          </div>
        </div>
      )}
      <p className="overview-panel__footer">
        <TextLink to={href}>{t('sport.outcomeOpenFull')}</TextLink>
      </p>
    </section>
  );
}

function StandingCompactPanel({
  standing,
  href,
}: {
  standing: OverviewStandingCompact;
  href: string;
}) {
  const { t } = useTranslation('overview');
  const tables = standing.tables;
  const isGroups = tables.length > 1 || tables[0]?.scope === 'Group';
  const [tableIndex, setTableIndex] = useState(0);
  const safeIndex = Math.min(tableIndex, Math.max(tables.length - 1, 0));
  const table = tables[safeIndex];
  if (!table) {
    return null;
  }

  const title =
    isGroups && table.groupName
      ? t('sport.standingGroupTitle', { name: table.groupName })
      : t('sport.standingTitle');

  return (
    <section className="ds-panel" aria-labelledby="overview-standing-compact">
      <PanelHead
        id="overview-standing-compact"
        title={title}
        icon={<ClassementsNavIcon size="md" />}
      />
      {tables.length > 1 && (
        <div
          className="overview-standing-nav"
          role="tablist"
          aria-label={t('sport.groupNav')}
        >
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
                  row.position === 1
                    ? 'overview-standing__row--leader'
                    : undefined
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
        <TextLink to={href}>{t('sport.standingOpen')}</TextLink>
      </p>
    </section>
  );
}

function CalendarSummaryPanel({
  summary,
  matchesHref,
}: {
  summary: OverviewCalendarSummary;
  matchesHref: string;
}) {
  const { t } = useTranslation('overview');
  const titleId = 'overview-calendar-summary';
  const next = summary.nextMatch;

  return (
    <section
      className="ds-panel overview-calendar"
      aria-labelledby={titleId}
      data-testid="overview-calendar-panel"
    >
      <PanelHead
        id={titleId}
        title={t('dimensions.calendar.title')}
        icon={<CalendarIcon size="md" />}
      />
      <p className="overview-calendar__totals">
        {t('dimensions.calendar.matchdays', { count: summary.matchdayCount })}
        {' · '}
        {t('dimensions.calendar.matches', { count: summary.matchCount })}
      </p>
      {summary.matchdays.length > 0 ? (
        <ul className="overview-calendar__matchdays">
          {summary.matchdays.map((matchday) => (
            <li key={matchday.matchdayNumber}>
              {t('dimensions.calendar.matchdayLine', {
                number: matchday.matchdayNumber,
                count: matchday.matchCount,
              })}
            </li>
          ))}
        </ul>
      ) : null}
      {next ? (
        <div className="overview-calendar__next">
          <p className="overview-calendar__next-heading">
            {t('dimensions.calendar.nextHeading')}
            {next.matchdayNumber != null
              ? ` · ${t('dimensions.calendar.nextMatchday', { number: next.matchdayNumber })}`
              : null}
          </p>
          <p className="overview-calendar__next-line">
            {t('dimensions.calendar.nextLine', {
              home: next.homeDisplayName,
              away: next.awayDisplayName,
            })}
          </p>
        </div>
      ) : null}
      <p className="overview-panel__footer">
        <TextLink to={matchesHref}>{t('dimensions.openMatches')}</TextLink>
      </p>
    </section>
  );
}

function SportUnitPanel({
  kind,
  unit,
  matchesHref,
}: {
  kind: 'recent' | 'next';
  unit: OverviewSportUnit | null;
  matchesHref: string;
}) {
  const { t } = useTranslation('overview');
  const titleId =
    kind === 'recent' ? 'overview-recent-unit' : 'overview-next-unit';
  const title =
    kind === 'recent' ? t('sport.recentTitle') : t('sport.nextTitle');
  const empty =
    kind === 'recent' ? t('sport.recentEmpty') : t('sport.nextEmpty');
  const subtitle = unit ? sportUnitSubtitle(unit, t) : null;

  return (
    <section className="ds-panel" aria-labelledby={titleId}>
      <PanelHead
        id={titleId}
        title={title}
        icon={<MatchesNavIcon size="md" />}
      />
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
        <TextLink to={matchesHref}>{t('dimensions.openMatches')}</TextLink>
      </p>
    </section>
  );
}

function sportUnitSubtitle(
  unit: OverviewSportUnit,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  const unitLabel =
    unit.unitKind === 'Matchday' && unit.matchdayNumber != null
      ? t('sport.unitMatchday', { number: unit.matchdayNumber })
      : unit.roundName?.trim() || unit.stageName;
  const countLabel = t('sport.unitMatchCount', { count: unit.matchCount });
  return `${unitLabel} · ${countLabel}`;
}

function SportMatchLineList({ matches }: { matches: OverviewMatchLine[] }) {
  const { t } = useTranslation('overview');
  const { i18n } = useTranslation();

  return (
    <ul className="overview-match-list">
      {matches.map((match) => {
        const score =
          match.score != null
            ? `${match.score.homeGoals}–${match.score.awayGoals}`
            : null;
        const isLive = match.status === 'Live';
        const isFinished = match.status === 'Finished';
        const scheduledLabel =
          match.scheduledAt != null
            ? formatMatchSchedule(match.scheduledAt, toIntlLocale(i18n.language))
            : t('sport.scheduledUnset');

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
                <span className="overview-match__name">
                  {match.homeDisplayName}
                </span>
                <span className="overview-match__vs" aria-hidden="true">
                  –
                </span>
                <span className="overview-match__name">
                  {match.awayDisplayName}
                </span>
              </span>
              <span className="overview-match__aside">
                {isLive ? (
                  <Status
                    density="context"
                    tone="live"
                    variant="soft"
                    shape="rounded"
                  >
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
        );
      })}
    </ul>
  );
}

function formatMatchSchedule(iso: string, locale: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date);
}

function AttentionSignalSection({
  items,
  count,
  competitionId,
}: {
  items: OverviewSituation[];
  count: number;
  competitionId: string;
}) {
  const { t } = useTranslation('overview');
  const preview = items.slice(0, 2);

  if (count === 0 || preview.length === 0) {
    return null;
  }

  return (
    <AttentionGroup
      headingId="overview-situations"
      heading={t('situations.heading')}
      icon={<OverviewAttentionIcon size="md" />}
    >
      <ul className="shell-attention-drawer__list">
        {preview.map((situation) => (
          <AttentionSituationRow
            key={`${situation.source}:${situation.targetType}:${situation.targetId}`}
            item={situation}
            competitionId={competitionId}
          />
        ))}
      </ul>
      {count > 2 && (
        <p className="overview-panel__muted">
          {t('attention.triageHint', { count })}
        </p>
      )}
    </AttentionGroup>
  );
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
  variant?: 'construction' | 'game';
  regulation: OverviewView['constructionDimensions']['regulation'];
  gameRules?: OverviewReferenceStageGameRules | null;
  href: string;
  hrefLabel: string;
  actions: OverviewAction[];
  actionRunner: ActionRunner;
}) {
  const { t } = useTranslation('overview');

  if (variant === 'game') {
    if (!gameRules) {
      return null;
    }

    const isCup = gameRules.formatKind === 'Cup';
    const textFacts = buildGameRegulationTextFacts(gameRules, t);
    const showPoints = !isCup;

    return (
      <article
        className={`${panelProminenceClass(regulation.prominence)} overview-config-card`}
        aria-labelledby="overview-regulation"
        data-testid="overview-regulation-game"
      >
        <PanelHead
          id="overview-regulation"
          title={t('dimensions.regulation.title')}
          icon={<RegulationIcon size="md" />}
        />
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
    );
  }

  const competition = regulation.competition;

  return (
    <article
      className={panelProminenceClass(regulation.prominence)}
      aria-labelledby="overview-regulation"
      data-testid="overview-regulation-construction"
    >
      <PanelHead
        id="overview-regulation"
        title={t('dimensions.regulation.title')}
        icon={<RegulationIcon size="md" />}
      />
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
        {t('dimensions.regulation.formatDuration', {
          periods: competition.numberOfPeriods,
          duration: competition.durationPerPeriod,
        })}
      </p>
      <ActionButtons actions={actions} actionRunner={actionRunner} />
      <p className="overview-panel__footer">
        <OverviewLink to={href}>{hrefLabel}</OverviewLink>
      </p>
    </article>
  );
}

function PointsChip({
  value,
  label,
  tone,
}: {
  value: number;
  label: string;
  tone: 'win' | 'draw' | 'loss';
}) {
  const { t } = useTranslation('overview');

  return (
    <li className={`overview-chip overview-chip--${tone}`}>
      <span className="overview-chip__value">
        {t('dimensions.regulation.pointsValue', { value })}
      </span>
      <span className="overview-chip__label">{label}</span>
    </li>
  );
}

/** Exit link toward the owning workspace — right-aligned, réf. V9. */
function OverviewLink({ to, children }: { to: string; children: ReactNode }) {
  return <TextLink to={to}>{children}</TextLink>;
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
  variant?: 'construction' | 'identity';
  dimension: OverviewDimension;
  entries: StructureEntry[];
  href: string;
  hrefLabel: string;
  actions: OverviewAction[];
  actionRunner: ActionRunner;
}) {
  const { t } = useTranslation('overview');
  const identity = variant === 'identity';
  const activeCount = Number(dimension.facts.activeCount ?? '0');
  const minimumTeams = Number(dimension.facts.minimumTeams ?? '0');
  const activeEntries = entries.filter((entry) => entry.status === 'Active');
  const preview = activeEntries.slice(0, 6);
  const overflow = Math.max(0, activeCount - preview.length);
  const belowMinimum = minimumTeams > 0 && activeCount < minimumTeams;

  return (
    <article
      className={panelProminenceClass(dimension.prominence)}
      aria-labelledby="overview-teams"
    >
      <PanelHead
        id="overview-teams"
        title={t('dimensions.teams.title')}
        icon={<TeamsIcon size="md" />}
      />
      <p className="overview-figure">
        <span className="ds-overview-situation__num ds-num">{activeCount}</span>
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
            <li key={entry.entryId} className="overview-crest">
              <Tooltip content={entry.displayName}>
                <TeamCrest
                  name={entry.displayName}
                  logoMediaId={entry.logoMediaId}
                  primaryColor={entry.primaryColor}
                  size="sm"
                />
              </Tooltip>
            </li>
          ))}
          {overflow > 0 && (
            <li className="overview-crest overview-crest--more">+{overflow}</li>
          )}
        </ul>
      )}
      {!identity && belowMinimum && (
        <p className="overview-teams-minimum">
          <OverviewAttentionIcon size="sm" aria-hidden="true" />
          {t('dimensions.teams.minimumRequired', { minimumTeams })}
        </p>
      )}
      {!identity && (
        <ActionButtons actions={actions} actionRunner={actionRunner} />
      )}
      <p className="overview-panel__footer">
        <OverviewLink to={href}>{hrefLabel}</OverviewLink>
      </p>
    </article>
  );
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
  href,
  hrefLabel,
  actions,
  actionRunner,
}: {
  variant?: 'construction' | 'condensed';
  dimension: OverviewDimension;
  stages: OverviewView['operationalFocus']['stages'];
  matchTotal: number;
  href: string;
  hrefLabel: string;
  actions: OverviewAction[];
  actionRunner: ActionRunner;
}) {
  const { t } = useTranslation('overview');
  const formatKind = dimension.facts.formatKind;
  const formatConfigured = Boolean(formatKind) && formatKind !== 'None';
  const groupCount = Number(dimension.facts.groupCount ?? '0');
  const roundCount = Number(dimension.facts.roundCount ?? '0');
  const matchdayCount = Number(dimension.facts.matchdayCount ?? '0');
  const slotCount = Number(dimension.facts.slotCount ?? '0');
  const swissRoundCount = Number(dimension.facts.swissRoundCount ?? '0');
  const swissByeCount = Number(dimension.facts.swissByeCount ?? '0');
  const stageNames = stages.map((stage) => stage.name).filter(Boolean);

  if (variant === 'condensed') {
    const formatLabel =
      formatConfigured && formatKind
        ? structureFormatKindLabel(formatKind as StructureFormatKind)
        : t('dimensions.structure.none');
    const metrics = buildStructureCondensedMetrics({
      t,
      formatKind,
      stages,
      stageNames,
      groupCount,
      roundCount,
      matchdayCount,
      swissRoundCount,
    });

    return (
      <article
        className={`${panelProminenceClass(dimension.prominence)} overview-config-card`}
        aria-labelledby="overview-structure"
        data-testid="overview-structure-condensed"
      >
        <PanelHead
          id="overview-structure"
          title={t('dimensions.structure.title')}
          icon={<StructureIcon size="md" />}
        />
        <div className="overview-structure-hero">
          <p
            className="ds-overview-situation__num ds-num"
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
    );
  }

  return (
    <article
      className={panelProminenceClass(dimension.prominence)}
      aria-labelledby="overview-structure"
      data-testid="overview-structure-construction"
    >
      <PanelHead
        id="overview-structure"
        title={t('dimensions.structure.title')}
        icon={<StructureIcon size="md" />}
      />
      <ul className="overview-rows">
        <StructureFactRow
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
          <StructureFactRow
            label={t('dimensions.structure.groups', { count: groupCount })}
          />
        )}
        {roundCount > 0 && (
          <StructureFactRow
            label={t('dimensions.structure.rounds', { count: roundCount })}
          />
        )}
        {formatKind === 'Swiss' && swissRoundCount > 0 && (
          <StructureFactRow
            label={t('dimensions.structure.swissRounds', {
              generated: matchdayCount,
              planned: swissRoundCount,
            })}
          />
        )}
        {formatKind !== 'Swiss' && matchdayCount > 0 && (
          <StructureFactRow
            label={t('dimensions.structure.matchdays', {
              count: matchdayCount,
            })}
          />
        )}
        {formatKind === 'Swiss' && swissByeCount > 0 && (
          <StructureFactRow
            label={t('dimensions.structure.swissByes', {
              count: swissByeCount,
            })}
            detail={t('dimensions.structure.swissByesHint')}
          />
        )}
        {slotCount > 0 && (
          <StructureFactRow
            label={t('dimensions.structure.slots', { count: slotCount })}
          />
        )}
        <StructureFactRow
          label={
            matchTotal > 0
              ? t('dimensions.structure.matches', { count: matchTotal })
              : t('dimensions.structure.matchesNone')
          }
        />
      </ul>
      <ActionButtons actions={actions} actionRunner={actionRunner} />
      <p className="overview-panel__footer">
        <OverviewLink to={href}>{hrefLabel}</OverviewLink>
      </p>
    </article>
  );
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
  t: (key: string, options?: Record<string, unknown>) => string;
  formatKind: string | undefined;
  stages: OverviewView['operationalFocus']['stages'];
  stageNames: string[];
  groupCount: number;
  roundCount: number;
  matchdayCount: number;
  swissRoundCount: number;
}): string[] {
  const metrics: string[] = [];
  const push = (value: string | null | undefined) => {
    if (value && metrics.length < 3) {
      metrics.push(value);
    }
  };

  const phasesLabel =
    stages.length > 0
      ? stageNames.length > 0
        ? t('dimensions.structure.phasesDetail', {
            count: stages.length,
            names: stageNames.join(' · '),
          })
        : t('dimensions.structure.phases', { count: stages.length })
      : null;

  switch (formatKind) {
    case 'Championship':
      push(phasesLabel);
      if (matchdayCount > 0) {
        push(t('dimensions.structure.matchdays', { count: matchdayCount }));
      }
      if (groupCount > 0) {
        push(t('dimensions.structure.groups', { count: groupCount }));
      }
      break;
    case 'Groups':
      push(phasesLabel);
      if (groupCount > 0) {
        push(t('dimensions.structure.groups', { count: groupCount }));
      }
      if (matchdayCount > 0) {
        push(t('dimensions.structure.matchdays', { count: matchdayCount }));
      }
      break;
    case 'Cup':
      push(phasesLabel);
      if (roundCount > 0) {
        push(t('dimensions.structure.rounds', { count: roundCount }));
      }
      break;
    case 'Swiss':
      push(phasesLabel);
      if (swissRoundCount > 0 || matchdayCount > 0) {
        push(
          t('dimensions.structure.swissRounds', {
            generated: matchdayCount,
            planned: swissRoundCount,
          }),
        );
      }
      break;
    default:
      push(phasesLabel);
      if (groupCount > 0) {
        push(t('dimensions.structure.groups', { count: groupCount }));
      }
      if (roundCount > 0) {
        push(t('dimensions.structure.rounds', { count: roundCount }));
      }
      if (matchdayCount > 0) {
        push(t('dimensions.structure.matchdays', { count: matchdayCount }));
      }
      break;
  }

  return metrics;
}

/** Text facts for En cours Règlement (points rendered separately as chips). */
function buildGameRegulationTextFacts(
  rules: OverviewReferenceStageGameRules,
  t: (key: string, options?: Record<string, unknown>) => string,
): string[] {
  const facts: string[] = [];
  const push = (value: string | null | undefined) => {
    if (value && facts.length < 3) {
      facts.push(value);
    }
  };

  const duration = t('dimensions.regulation.formatDuration', {
    periods: rules.numberOfPeriods,
    duration: rules.durationPerPeriod,
  });

  if (rules.formatKind === 'Cup') {
    if (rules.numberOfLegs >= 2) {
      push(
        rules.aggregateScoring
          ? t('dimensions.regulation.twoLegsAggregate')
          : t('dimensions.regulation.twoLegs'),
      );
    } else {
      push(t('dimensions.regulation.elimination'));
    }
    push(duration);

    const extras: string[] = [];
    if (rules.hasExtraTime || rules.hasTieExtraTime) {
      extras.push(t('dimensions.regulation.extraTime'));
    }
    if (rules.hasPenaltyShootout || rules.hasTiePenaltyShootout) {
      extras.push(t('dimensions.regulation.penalties'));
    }
    if (extras.length > 0) {
      push(extras.join(' · '));
    }
    return facts;
  }

  push(duration);
  if (
    rules.formatKind === 'Swiss' &&
    rules.swissPlannedRounds != null &&
    rules.swissPlannedRounds > 0
  ) {
    push(
      t('dimensions.regulation.swissPlannedRounds', {
        count: rules.swissPlannedRounds,
      }),
    );
  }

  return facts;
}

function StructureFactRow({
  label,
  detail,
}: {
  label: string;
  detail?: string;
}) {
  return (
    <li className="overview-row">
      <span className="overview-row__label">{label}</span>
      {detail && <span className="overview-row__detail">{detail}</span>}
    </li>
  );
}

function ActionButtons({
  actions,
  actionRunner,
  emphasizeFirst = false,
}: {
  actions: OverviewAction[];
  actionRunner: ActionRunner;
  /** Primary only when this region owns the single CTA — Identité §11. */
  emphasizeFirst?: boolean;
}) {
  const { t } = useTranslation('overview');
  if (actions.length === 0) {
    return null;
  }

  return (
    <div
      className="overview-actions"
      aria-busy={actionRunner.mutation.isPending}
    >
      {actions.map((action, index) => {
        const key = overviewActionKey(action);
        const busy =
          actionRunner.mutation.isPending && actionRunner.activeKey === key;
        const label = actionLabel(action.code, {
          name: action.params?.stageName,
          ...action.params,
        });
        const intent = actionRunner.resolveIntent(action);
        const variant =
          emphasizeFirst && index === 0
            ? 'ds-btn--primary'
            : 'ds-btn--secondary';
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
          );
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
        );
      })}
    </div>
  );
}

function NaturalProgressionSection({
  data,
  actionRunner,
  lifecycleActions,
}: {
  data: OverviewView;
  actionRunner: ActionRunner;
  /**
   * PrepareCompetition / StartCompetition leftovers — occupy Prochaine action only when
   * naturalProgression is null (never stacked with a structural tip).
   */
  lifecycleActions: OverviewAction[];
}) {
  const { t } = useTranslation('overview');
  const code = data.naturalProgression?.code;
  const matched = code
    ? findActionByCode(data.availableActions, code)
    : undefined;
  const lifecycle = lifecycleActions[0];
  const hasPrimary = Boolean(code);

  // Structural tip XOR lifecycle — never both (Préparation V1 P5).
  if (hasPrimary) {
    return (
      <OverviewNextAction
        headingId="overview-progression"
        heading={t('progression.heading')}
        icon={<NextActionIcon size="md" />}
        title={actionLabel(code!)}
        why={t(`progression.codes.${code}`, {
          defaultValue: actionLabel(code!),
        })}
        action={
          matched ? (
            <ActionButtons
              actions={[matched]}
              actionRunner={actionRunner}
              emphasizeFirst
            />
          ) : null
        }
      />
    );
  }

  if (!lifecycle) {
    return null;
  }

  return (
    <OverviewNextAction
      headingId="overview-progression"
      heading={t('progression.heading')}
      icon={<NextActionIcon size="md" />}
      title={actionLabel(lifecycle.code)}
      why={t(`progression.codes.${lifecycle.code}`, {
        defaultValue: actionLabel(lifecycle.code),
      })}
      action={
        <ActionButtons
          actions={[lifecycle]}
          actionRunner={actionRunner}
          emphasizeFirst
        />
      }
    />
  );
}
