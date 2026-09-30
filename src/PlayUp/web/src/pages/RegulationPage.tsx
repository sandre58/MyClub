import { useQuery } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { fetchStructureView } from '../api';
import { Alert } from '../design-system/components/Alert';
import { Chip, type ChipTone } from '../design-system/components/Chip';
import { FormSection } from '../design-system/components/FormSection';
import { PageHead } from '../design-system/components/PageHead';
import { Tooltip } from '../design-system/components/Tooltip';
import { TextLink } from '../design-system/components/TextLink';
import {
  AggregateIcon,
  ArrowRightIcon,
  ConfrontationIcon,
  DisciplineIcon,
  DrawConfigIcon,
  EmptySelectionIcon,
  ForfeitIcon,
  LayersIcon,
  LegsStatIcon,
  MatchRulesIcon,
  PenaltiesIcon,
  PencilIcon,
  PersonIcon,
  RandomIcon,
  StandingRulesIcon,
  TimerIcon,
  TrophyIcon,
} from '../design-system/icons/contentIcons';
import { ClockIcon, PinIcon } from '../design-system/icons/metaIcons';
import { queryKeys } from '../queryKeys';
import {
  ErrorState,
  EmptyState,
  LoadingState,
  StageStatusBadge,
  StatusBadge,
} from '../ui';
import type {
  DisciplinaryType,
  StructureRegulationSummary,
  StructureStageHubSummary,
} from '../types';
import { RegulationEditorDialog } from './RegulationEditorDialog';
import { isPartOverridden, isStagePersonalized } from './regulationImpact';
import {
  MatchRulesPanel,
  StandingRulesPanel,
  criterionLabel,
} from './regulationRulePanels';
import { structureDeepLink } from './structureNavigation';
import type { StructureSectionId } from './structureHubSections';
import './regulation.css';

type Translate = (key: string, options?: Record<string, unknown>) => string;

/**
 * Regulation — read hub + framework editing.
 * Rich mockup vocabulary, DS tokens (surface, brand, type).
 */

// —— Page ——

export function RegulationPage() {
  const { competitionId = '' } = useParams();
  const { t } = useTranslation('regulation');
  const [editorOpen, setEditorOpen] = useState(false);

  const structureQuery = useQuery({
    queryKey: queryKeys.competitions.structure(competitionId),
    queryFn: () => fetchStructureView(competitionId),
    enabled: competitionId.length > 0,
  });

  if (structureQuery.isPending) {
    return (
      <main id="main" className="page page--regulation">
        <LoadingState />
      </main>
    );
  }

  if (structureQuery.isError || !structureQuery.data) {
    return (
      <main id="main" className="page page--regulation">
        <ErrorState error={structureQuery.error} />
      </main>
    );
  }

  const data = structureQuery.data;
  const { regulation, stages } = data;
  const canReplace = data.actions.includes('ReplaceRegulation');
  const showStandingTile = stages.some(
    (stage) => stage.hasStandingRules === true,
  );

  return (
    <main id="main" className="page page--regulation">
      <div className="regulation">
        <PageHead
          title={t('title')}
          actions={
            canReplace ? (
              <button
                type="button"
                className="ds-btn ds-btn--primary"
                aria-label={t('editRegulation')}
                onClick={() => setEditorOpen(true)}
              >
                <PencilIcon size="sm" />
                <span>{t('editRegulation')}</span>
              </button>
            ) : (
              <Tooltip content={t('editRegulationDisabledHint')}>
                <button
                  type="button"
                  className="ds-btn ds-btn--primary"
                  disabled
                  aria-label={t('editRegulation')}
                >
                  <PencilIcon size="sm" />
                  <span>{t('editRegulation')}</span>
                </button>
              </Tooltip>
            )
          }
        />

        <section
          className={[
            'regulation-frame',
            showStandingTile ? 'regulation-frame--with-standing' : '',
          ]
            .filter(Boolean)
            .join(' ')}
          aria-label={t('frameHeading')}
        >
          <div className="regulation-frame__stack">
            <EntriesTile regulation={regulation} />
            <DisciplineTile regulation={regulation} />
          </div>
          <MatchTile regulation={regulation} />
          {showStandingTile ? <StandingTile regulation={regulation} /> : null}
        </section>

        <section
          className="regulation-section"
          aria-labelledby="regulation-phases-heading"
        >
          <h2
            id="regulation-phases-heading"
            className="regulation-section__label"
          >
            {t('phasesHeading')}
          </h2>
          {stages.length === 0 ? (
            <p className="regulation-empty">{t('emptyPhases')}</p>
          ) : (
            <div className="regulation-phases">
              {stages.map((stage, index) => (
                <PhaseTile
                  key={stage.stageId}
                  competitionId={competitionId}
                  stage={stage}
                  ordinal={index + 1}
                />
              ))}
            </div>
          )}
        </section>
      </div>

      <RegulationEditorDialog
        data={data}
        open={editorOpen}
        onClose={() => setEditorOpen(false)}
      />
    </main>
  );
}

// —— Framework: shared card chrome ——

function FrameCard({
  icon,
  title,
  children,
}: {
  icon: ReactNode;
  title: string;
  children: ReactNode;
}) {
  return (
    <FormSection icon={icon} title={title}>
      {children}
    </FormSection>
  );
}

// —— Framework: Teams ——

function EntriesTile({
  regulation,
}: {
  regulation: StructureRegulationSummary;
}) {
  const { t } = useTranslation('regulation');
  const min = regulation.minimumTeams;
  const max = regulation.maximumTeams;
  const exact = min === max;

  return (
    <FrameCard icon={<PersonIcon size="md" />} title={t('families.entries')}>
      <div
        className="regulation-capacity"
        aria-label={
          exact
            ? t('capacity.exactAria', { count: min })
            : t('capacity.aria', { min, max })
        }
      >
        {exact ? (
          <div
            className="regulation-capacity__scale regulation-capacity__scale--exact"
            aria-hidden="true"
          >
            <span className="regulation-capacity__pill regulation-capacity__pill--exact">
              {min}
            </span>
          </div>
        ) : (
          <>
            <div className="regulation-capacity__scale" aria-hidden="true">
              <span className="regulation-capacity__pill regulation-capacity__pill--min">
                {min}
              </span>
              <span className="regulation-capacity__rail">
                <span className="regulation-capacity__rail-fill" />
                <span className="regulation-capacity__rail-glow" />
              </span>
              <span className="regulation-capacity__pill regulation-capacity__pill--max">
                {max}
              </span>
            </div>
            <div className="regulation-capacity__caps">
              <span className="regulation-capacity__cap regulation-capacity__cap--min">
                {t('capacity.min')}
              </span>
              <span className="regulation-capacity__cap regulation-capacity__cap--max">
                {t('capacity.max')}
              </span>
            </div>
          </>
        )}
      </div>
      <Alert tone="info" role="status">
        {exact
          ? t('capacity.exactNotice', { count: min })
          : t('capacity.notice', { min, max })}
      </Alert>
    </FrameCard>
  );
}

// —— Framework: Match ——

function MatchTile({ regulation }: { regulation: StructureRegulationSummary }) {
  const { t } = useTranslation('regulation');

  return (
    <FrameCard icon={<MatchRulesIcon size="md" />} title={t('families.match')}>
      <MatchRulesPanel
        numberOfPeriods={regulation.numberOfPeriods}
        durationPerPeriod={regulation.durationPerPeriod}
        halfTimeDuration={regulation.halfTimeDuration}
        hasExtraTime={regulation.hasExtraTime}
        extraTimeNumberOfPeriods={regulation.extraTimeNumberOfPeriods}
        extraTimeDurationPerPeriod={regulation.extraTimeDurationPerPeriod}
        hasPenaltyShootout={regulation.hasPenaltyShootout}
        penaltyInitialKicksPerTeam={regulation.penaltyInitialKicksPerTeam}
      />
    </FrameCard>
  );
}

// —— Framework: Disciplinary ——

function DisciplineTile({
  regulation,
}: {
  regulation: StructureRegulationSummary;
}) {
  const { t } = useTranslation('regulation');
  const types = regulation.allowedTypes ?? [];

  return (
    <FrameCard
      icon={<DisciplineIcon size="md" />}
      title={t('families.discipline')}
    >
      {types.length === 0 ? (
        <div className="regulation-discipline">
          <EmptyState
            icon={<EmptySelectionIcon size="md" />}
            title={t('discipline.emptyTitle')}
          >
            {t('discipline.empty')}
          </EmptyState>
        </div>
      ) : (
        <div className="regulation-discipline">
          <p className="regulation-discipline__subtitle">
            {t('discipline.subtitle')}
          </p>
          <ul className="regulation-cards" aria-label={t('discipline.aria')}>
            {types.map((type) => (
              <li key={type} className="regulation-cards__item">
                <Tooltip content={disciplineLabel(type, t)}>
                  <span
                    className={`regulation-card-token regulation-card-token--${type.toLowerCase()}`}
                    aria-label={disciplineLabel(type, t)}
                    role="img"
                  >
                    <span
                      className="regulation-card-token__face"
                      aria-hidden="true"
                    />
                  </span>
                </Tooltip>
              </li>
            ))}
          </ul>
        </div>
      )}
    </FrameCard>
  );
}

// —— Framework: Standing ——

function StandingTile({
  regulation,
}: {
  regulation: StructureRegulationSummary;
}) {
  const { t } = useTranslation('regulation');

  return (
    <FrameCard
      icon={<StandingRulesIcon size="md" />}
      title={t('families.standing')}
    >
      <StandingRulesPanel
        winPoints={regulation.winPoints}
        drawPoints={regulation.drawPoints}
        lossPoints={regulation.lossPoints}
        rankingCriteria={regulation.rankingCriteria}
        forfeitWinnerGoals={regulation.forfeitWinnerGoals}
        forfeitLoserGoals={regulation.forfeitLoserGoals}
      />
    </FrameCard>
  );
}

// —— Stages: tile ——

function PersonalizedBadge() {
  const { t } = useTranslation('regulation');
  return (
    <Tooltip content={t('differsFromFrame')}>
      <StatusBadge tone="warn" density="compact">
        {t('personalized')}
      </StatusBadge>
    </Tooltip>
  );
}

function PhaseTile({
  competitionId,
  stage,
  ordinal,
}: {
  competitionId: string;
  stage: StructureStageHubSummary;
  ordinal: number;
}) {
  const { t } = useTranslation('regulation');
  const structureHref = structureDeepLink({
    competitionId,
    stageId: stage.stageId,
  });
  const ruleColumns = buildPhaseRuleColumns(stage, t, competitionId);
  const personalized = isStagePersonalized(stage);

  return (
    <article className="regulation-phase">
      <header className="regulation-phase__head">
        <div className="regulation-phase__head-main">
          <span className="regulation-phase__ordinal" aria-hidden="true">
            {ordinal}
          </span>
          <h3 className="regulation-phase__title">{stage.name}</h3>
        </div>
        <span className="regulation-phase__head-meta">
          <StageStatusBadge status={stage.status} density="compact" />
          {personalized ? <PersonalizedBadge /> : null}
        </span>
      </header>

      {ruleColumns.length > 0 ? (
        <div
          className="regulation-phase__bottom"
          aria-label={t('phaseRegulationAria')}
        >
          <div className="regulation-rule-cols" aria-label={t('tokens.aria')}>
            {ruleColumns.map((column) => (
              <section key={column.key} className="regulation-rule-col">
                <h4 className="regulation-rule-col__title">
                  <span
                    className="regulation-rule-col__icon"
                    aria-hidden="true"
                  >
                    {column.icon}
                  </span>
                  {column.title}
                </h4>
                {column.chips && column.chips.length > 0 ? (
                  <div className="regulation-rule-col__chips">
                    {column.chips.map((chip) => {
                      const node = (
                        <Chip
                          tone={chip.tone}
                          className={
                            chip.overridden
                              ? 'regulation-rule-chip--overridden'
                              : undefined
                          }
                        >
                          {chip.label}
                        </Chip>
                      );
                      return chip.title ? (
                        <Tooltip key={chip.key} content={chip.title}>
                          {node}
                        </Tooltip>
                      ) : (
                        <span key={chip.key}>{node}</span>
                      );
                    })}
                  </div>
                ) : null}
                {column.sections && column.sections.length > 0 ? (
                  <div className="regulation-rule-col__sections">
                    {column.sections.map((section) => (
                      <div
                        key={section.key}
                        className="regulation-rule-section"
                      >
                        <h5 className="regulation-rule-section__title">
                          {section.title}
                        </h5>
                        <PhaseRuleItemList items={section.items} />
                      </div>
                    ))}
                  </div>
                ) : column.items.length > 0 ? (
                  <PhaseRuleItemList items={column.items} />
                ) : null}
              </section>
            ))}
          </div>
        </div>
      ) : null}

      <div className="regulation-phase__footer">
        <TextLink to={structureHref}>{t('openInStructure')}</TextLink>
      </div>
    </article>
  );
}

type PhaseRuleChip = {
  key: string;
  label: ReactNode;
  tone?: ChipTone;
  title?: string;
  /** Unbound heritable part — accent label on the phase tile. */
  overridden?: boolean;
};

type PhaseRuleItem = {
  key: string;
  label: ReactNode;
  title?: string;
  /** Numbered ranking-criteria pill when set. */
  index?: number;
  icon?: ReactNode;
  /** Unbound heritable part — accent label on the phase tile. */
  overridden?: boolean;
};

type PhaseRuleSection = {
  key: string;
  title: string;
  items: PhaseRuleItem[];
};

type PhaseRuleColumn = {
  key: string;
  title: string;
  icon: ReactNode;
  chips?: PhaseRuleChip[];
  items: PhaseRuleItem[];
  /** Optional subsections (e.g. Confrontation multi-format). */
  sections?: PhaseRuleSection[];
};

function PhaseRuleItemList({ items }: { items: PhaseRuleItem[] }) {
  return (
    <ul className="regulation-rule-list">
      {items.map((item) => {
        const body = (
          <>
            {item.index != null ? (
              <span className="regulation-criteria__n" aria-hidden="true">
                {item.index}
              </span>
            ) : item.icon ? (
              <span className="regulation-rule-list__mark" aria-hidden="true">
                {item.icon}
              </span>
            ) : null}
            <span className="regulation-rule-list__label">{item.label}</span>
          </>
        );
        return (
          <li
            key={item.key}
            className={[
              'regulation-rule-list__item',
              item.overridden ? 'regulation-rule-list__item--overridden' : null,
            ]
              .filter(Boolean)
              .join(' ')}
          >
            {item.title ? (
              <Tooltip content={item.title}>
                <span className="regulation-rule-list__hit">{body}</span>
              </Tooltip>
            ) : (
              body
            )}
          </li>
        );
      })}
    </ul>
  );
}

// —— Stages: rule builders ——

type TieFormatTokens = {
  numberOfLegs: number;
  aggregateScoring?: boolean | null;
  hasAwayGoalsRule?: boolean;
  hasTieExtraTime?: boolean;
  hasTiePenaltyShootout?: boolean;
};

function buildTiePropertyItems(
  tie: TieFormatTokens,
  t: Translate,
  keyPrefix = '',
): PhaseRuleItem[] {
  const twoLegs = tie.numberOfLegs > 1;
  const prefix = keyPrefix ? `${keyPrefix}-` : '';
  const items: PhaseRuleItem[] = [
    {
      key: `${prefix}legs`,
      label: twoLegs ? t('tokens.tieTwoLegs') : t('tokens.tieOneLeg'),
      icon: twoLegs ? <LegsStatIcon size="sm" /> : <ArrowRightIcon size="sm" />,
      title: twoLegs ? t('tokens.tieTwoLegsTip') : t('tokens.tieOneLegTip'),
    },
  ];
  if (tie.aggregateScoring) {
    items.push({
      key: `${prefix}aggregate`,
      label: t('tokens.tieAggregate'),
      icon: <AggregateIcon size="sm" />,
      title: t('tokens.tieAggregateTip'),
    });
  }
  if (tie.hasAwayGoalsRule) {
    items.push({
      key: `${prefix}away-goals`,
      label: t('tokens.tieAwayGoals'),
      icon: <PinIcon size="sm" />,
      title: t('tokens.tieAwayGoalsTip'),
    });
  }
  if (tie.hasTieExtraTime) {
    items.push({
      key: `${prefix}tie-extra-time`,
      label: t('tokens.tieExtraTime'),
      icon: <TimerIcon size="sm" />,
      title: t('tokens.tieExtraTimeTip'),
    });
  }
  if (tie.hasTiePenaltyShootout) {
    items.push({
      key: `${prefix}tie-penalties`,
      label: t('tokens.tiePenalties'),
      icon: <PenaltiesIcon size="sm" />,
      title: t('tokens.tiePenaltiesTip'),
    });
  }
  return items;
}

function buildPhaseRuleColumns(
  stage: StructureStageHubSummary,
  t: Translate,
  competitionId: string,
): PhaseRuleColumn[] {
  const columns: PhaseRuleColumn[] = [];
  const sectionLink = (section: StructureSectionId) =>
    structureDeepLink({
      competitionId,
      stageId: stage.stageId,
      section,
    });
  const matchDurationOverridden = isPartOverridden(stage, 'matchDuration');
  const extraTimeOverridden = isPartOverridden(stage, 'extraTime');
  const penaltiesOverridden = isPartOverridden(stage, 'penaltyShootout');
  const forfeitOverridden = isPartOverridden(stage, 'administrativeResult');
  const pointsOverridden = isPartOverridden(stage, 'points');
  const rankingOverridden = isPartOverridden(stage, 'rankingCriteria');

  const matchItems: PhaseRuleItem[] = [
    {
      key: 'duration',
      label: t('tokens.matchSummary', {
        periods: stage.numberOfPeriods,
        minutes: stage.durationPerPeriod,
      }),
      icon: <ClockIcon size="sm" />,
      overridden: matchDurationOverridden,
    },
  ];
  if (stage.hasExtraTime) {
    const periods = stage.extraTimeNumberOfPeriods;
    const minutes = stage.extraTimeDurationPerPeriod;
    const hasDetail = periods != null && minutes != null;
    matchItems.push({
      key: 'extra-time',
      label: (
        <LabelWithDetail
          text={t('tokens.extraTime')}
          detail={
            hasDetail
              ? t('tokens.extraTimeDetail', { periods, minutes })
              : undefined
          }
        />
      ),
      icon: <TimerIcon size="sm" />,
      title: hasDetail
        ? t('tokens.extraTimeTip', { periods, minutes })
        : t('tokens.extraTime'),
      overridden: extraTimeOverridden,
    });
  }
  if (stage.hasPenaltyShootout) {
    const kicks = stage.penaltyInitialKicksPerTeam;
    matchItems.push({
      key: 'penalties',
      label: (
        <LabelWithDetail
          text={t('tokens.penalties')}
          detail={
            kicks != null
              ? t('tokens.penaltiesDetail', { count: kicks })
              : undefined
          }
        />
      ),
      icon: <PenaltiesIcon size="sm" />,
      title:
        kicks != null
          ? t('tokens.penaltiesTip', { count: kicks })
          : t('tokens.penalties'),
      overridden: penaltiesOverridden,
    });
  }
  columns.push({
    key: 'match',
    title: t('columns.match'),
    icon: <MatchRulesIcon size="sm" />,
    items: matchItems,
  });

  if (stage.hasTieFormat) {
    const segments = [...(stage.confrontationSegments ?? [])].sort(
      (left, right) => {
        const leftOrder = Math.min(
          ...left.rounds.map(
            (round) => round.sortOrder ?? Number.MAX_SAFE_INTEGER,
          ),
        );
        const rightOrder = Math.min(
          ...right.rounds.map(
            (round) => round.sortOrder ?? Number.MAX_SAFE_INTEGER,
          ),
        );
        return leftOrder - rightOrder;
      },
    );
    if (segments.length > 1) {
      columns.push({
        key: 'tie',
        title: t('columns.tie'),
        icon: <ConfrontationIcon size="sm" />,
        items: [],
        sections: segments.map((segment, index) => ({
          key: `tie-section-${index}`,
          title: segment.rounds.map((round) => round.name).join('/'),
          items: buildTiePropertyItems(segment, t, `seg-${index}`),
        })),
      });
    } else if (stage.numberOfLegs != null) {
      columns.push({
        key: 'tie',
        title: t('columns.tie'),
        icon: <ConfrontationIcon size="sm" />,
        items: buildTiePropertyItems(
          {
            numberOfLegs: stage.numberOfLegs,
            aggregateScoring: stage.aggregateScoring,
            hasAwayGoalsRule: stage.hasAwayGoalsRule,
            hasTieExtraTime: stage.hasTieExtraTime,
            hasTiePenaltyShootout: stage.hasTiePenaltyShootout,
          },
          t,
        ),
      });
    }
  }

  if (stage.hasDrawRules) {
    const pots = stage.numberOfPots;
    const seeds = stage.numberOfSeeds;
    const constraints = stage.drawConstraints ?? [];
    const items: PhaseRuleItem[] = [
      {
        key: 'mode',
        label:
          stage.drawMode === 'Random' || stage.drawMode == null
            ? t('tokens.drawRandom')
            : t(`tokens.drawMode.${stage.drawMode}`, {
                defaultValue: stage.drawMode,
              }),
        icon: <RandomIcon size="sm" />,
        title: t('tokens.drawRandomTip'),
      },
    ];
    if (pots != null && pots > 0) {
      items.push({
        key: 'pots',
        label: (
          <span className="regulation-rule-list__label-row">
            <Chip tone="soft">{pots}</Chip>
            <span>{t('tokens.drawPotsUnit', { count: pots })}</span>
          </span>
        ),
        icon: <LayersIcon size="sm" />,
        title: t('tokens.drawPotsTip', { count: pots }),
      });
    }
    if (seeds != null && seeds > 0) {
      items.push({
        key: 'seeds',
        label: (
          <span className="regulation-rule-list__label-row">
            <Chip tone="soft">{seeds}</Chip>
            <span>{t('tokens.drawSeedsUnit', { count: seeds })}</span>
          </span>
        ),
        title: t('tokens.drawSeedsTip', { count: seeds }),
      });
    }
    for (const constraint of constraints) {
      const typeLabel = t(`tokens.drawConstraint.${constraint.type}`, {
        defaultValue: constraint.type,
      });
      const enforcementLabel = t(
        `tokens.drawEnforcement.${constraint.enforcement}`,
        { defaultValue: constraint.enforcement },
      );
      const max =
        constraint.maxPerGroup != null
          ? t('tokens.drawConstraintMax', { count: constraint.maxPerGroup })
          : null;
      items.push({
        key: `constraint-${constraint.type}-${constraint.enforcement}-${constraint.maxPerGroup ?? 'x'}`,
        label: (
          <span className="regulation-rule-list__label-row">
            <span>{typeLabel}</span>
            <span className="regulation-detail">
              · {enforcementLabel}
              {max ? ` · ${max}` : ''}
            </span>
          </span>
        ),
        title: `${typeLabel} · ${enforcementLabel}${max ? ` · ${max}` : ''}`,
      });
    }
    columns.push({
      key: 'draw',
      title: t('columns.draw'),
      icon: <DrawConfigIcon size="sm" />,
      items,
    });
  }

  const classifies = stage.hasStandingRules === true;
  const forfeitWinner = stage.forfeitWinnerGoals;
  const forfeitLoser = stage.forfeitLoserGoals;
  const showForfeit =
    classifies && forfeitWinner != null && forfeitLoser != null;
  const showStandingPoints =
    classifies &&
    stage.winPoints != null &&
    stage.drawPoints != null &&
    stage.lossPoints != null;

  if (showStandingPoints || showForfeit) {
    const criteria = showStandingPoints ? (stage.rankingCriteria ?? []) : [];
    const items: PhaseRuleItem[] = criteria.map((criterion, index) => ({
      key: criterion,
      label: criterionLabel(criterion, t),
      index: index + 1,
      overridden: rankingOverridden,
    }));
    if (showForfeit) {
      items.push({
        key: 'forfeit',
        label: (
          <span className="regulation-rule-list__label-row">
            <span>&nbsp;{t('forfeit.heading')}</span>
            <span className="regulation-detail">
              {forfeitWinner}–{forfeitLoser}
            </span>
          </span>
        ),
        icon: <ForfeitIcon size="sm" />,
        title: t('forfeit.aria', {
          winner: forfeitWinner,
          loser: forfeitLoser,
        }),
        overridden: forfeitOverridden,
      });
    }
    columns.push({
      key: 'standing',
      title: t('columns.standing'),
      icon: <TrophyIcon size="sm" />,
      chips: showStandingPoints
        ? [
            {
              key: 'win',
              label: stage.winPoints,
              tone: 'win',
              title: t('tokens.standingWinTip', { value: stage.winPoints }),
              overridden: pointsOverridden,
            },
            {
              key: 'draw',
              label: stage.drawPoints,
              tone: 'draw',
              title: t('tokens.standingDrawTip', { value: stage.drawPoints }),
              overridden: pointsOverridden,
            },
            {
              key: 'loss',
              label: stage.lossPoints,
              tone: 'loss',
              title: t('tokens.standingLossTip', { value: stage.lossPoints }),
              overridden: pointsOverridden,
            },
          ]
        : undefined,
      items,
    });
  }

  return columns;
}

// —— Shared labels ——

function LabelWithDetail({ text, detail }: { text: string; detail?: string }) {
  return (
    <>
      {text}
      {detail ? <span className="regulation-detail">{detail}</span> : null}
    </>
  );
}

function disciplineLabel(type: DisciplinaryType, t: Translate) {
  return t(`discipline.${type}`, { defaultValue: type });
}
