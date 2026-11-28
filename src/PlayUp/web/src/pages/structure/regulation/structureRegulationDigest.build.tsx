import type { ReactNode } from 'react';
import {
  AggregateIcon,
  ArrowRightIcon,
  ExtraTimeIcon,
  ForfeitIcon,
  LegsStatIcon,
  PenaltiesIcon,
  PointsRulesIcon,
  RoundsStatIcon,
  SortingRulesIcon,
  TimerIcon,
} from '../../../design-system/icons/contentIcons';
import { PinIcon } from '../../../design-system/icons/metaIcons';
import { Chip } from '../../../design-system/components/Chip';
import {
  sameTie,
  tieFormFromSegment,
  tieFormFromSummary,
  type TieFormState,
} from '../../stage/tieFormatForm';
import type {
  RankingCriterion,
  StructureConfrontationSegment,
  StructureStageHubSummary,
} from '../../../types';

type Translate = (key: string, options?: Record<string, unknown>) => string;

export type RegulationDigestToken = {
  key: string;
  icon: ReactNode;
  content: ReactNode;
};

const MAX_VISIBLE_ROUND_LINES = 6;

function rankingCriterionLabel(criterion: RankingCriterion, t: Translate) {
  return t(`criteria.${criterion}`, { defaultValue: criterion });
}

export function buildMatchDigestTokens(
  stage: StructureStageHubSummary,
  t: Translate,
): RegulationDigestToken[] {
  const tokens: RegulationDigestToken[] = [];
  const periods = stage.numberOfPeriods;
  const mins = stage.durationPerPeriod;
  const half = stage.halfTimeDuration ?? 0;

  tokens.push({
    key: 'regulation',
    icon: <TimerIcon size="sm" aria-hidden="true" />,
    content: (
      <span>
        {periods}×{mins}′
        {half > 0 ? (
          <>
            {' · '}
            {t('matchTimeline.break')} {half}′
          </>
        ) : null}
      </span>
    ),
  });

  const etCount = stage.extraTimeNumberOfPeriods ?? 0;
  const etMins = stage.extraTimeDurationPerPeriod ?? 0;
  if (stage.hasExtraTime && etCount > 0 && etMins > 0) {
    tokens.push({
      key: 'extra',
      icon: <ExtraTimeIcon size="sm" aria-hidden="true" />,
      content: (
        <span>
          {etCount}×{etMins}′
        </span>
      ),
    });
  }

  if (stage.hasPenaltyShootout) {
    const kicks = stage.penaltyInitialKicksPerTeam;
    tokens.push({
      key: 'penalties',
      icon: <PenaltiesIcon size="sm" aria-hidden="true" />,
      content: (
        <span>
          {kicks != null
            ? `${t('matchTimeline.tabAria', { count: kicks })}`
            : null}
        </span>
      ),
    });
  }

  return tokens;
}

export function buildStandingDigestTokens(
  stage: StructureStageHubSummary,
  t: Translate,
): RegulationDigestToken[] {
  const tokens: RegulationDigestToken[] = [];
  const win = stage.winPoints ?? 0;
  const draw = stage.drawPoints ?? 0;
  const loss = stage.lossPoints ?? 0;
  const criteria = stage.rankingCriteria ?? [];
  const forfeitWinner = stage.forfeitWinnerGoals;
  const forfeitLoser = stage.forfeitLoserGoals;
  const showForfeit = forfeitWinner != null && forfeitLoser != null;

  tokens.push({
    key: 'points',
    icon: <PointsRulesIcon size="sm" aria-hidden="true" />,
    content: (
      <span className="structure-regulation-digest__chips">
        <Chip tone="win">{win}</Chip>
        <Chip tone="draw">{draw}</Chip>
        <Chip tone="loss">{loss}</Chip>
      </span>
    ),
  });

  if (criteria.length > 0) {
    tokens.push({
      key: 'criteria',
      icon: <SortingRulesIcon size="sm" aria-hidden="true" />,
      content: (
        <span>
          {criteria.map((criterion, index) => (
            <span key={criterion}>
              {index > 0 ? ' → ' : null}
              {rankingCriterionLabel(criterion, t)}
            </span>
          ))}
        </span>
      ),
    });
  }

  if (showForfeit) {
    tokens.push({
      key: 'forfeit',
      icon: <ForfeitIcon size="sm" aria-hidden="true" />,
      content: (
        <span>
          {t('forfeit.heading')} {forfeitWinner}–{forfeitLoser}
        </span>
      ),
    });
  }

  return tokens;
}

function confrontationItems(
  seg: StructureConfrontationSegment,
  prefix: string,
  t: Translate,
): RegulationDigestToken[] {
  const twoLegs = seg.numberOfLegs > 1;
  const items: RegulationDigestToken[] = [
    {
      key: `${prefix}-legs`,
      icon: twoLegs ? (
        <LegsStatIcon size="sm" aria-hidden="true" />
      ) : (
        <ArrowRightIcon size="sm" aria-hidden="true" />
      ),
      content: twoLegs ? t('tokens.tieTwoLegs') : t('tokens.tieOneLeg'),
    },
  ];
  if (seg.aggregateScoring) {
    items.push({
      key: `${prefix}-agg`,
      icon: <AggregateIcon size="sm" aria-hidden="true" />,
      content: t('tokens.tieAggregate'),
    });
  }
  if (seg.hasAwayGoalsRule) {
    items.push({
      key: `${prefix}-away`,
      icon: <PinIcon size="sm" aria-hidden="true" />,
      content: t('tokens.tieAwayGoals'),
    });
  }
  if (seg.hasTieExtraTime) {
    items.push({
      key: `${prefix}-et`,
      icon: <ExtraTimeIcon size="sm" aria-hidden="true" />,
      content: t('tokens.tieExtraTime'),
    });
  }
  if (seg.hasTiePenaltyShootout) {
    items.push({
      key: `${prefix}-tab`,
      icon: <PenaltiesIcon size="sm" aria-hidden="true" />,
      content: t('tokens.tiePenalties'),
    });
  }
  return items;
}

export function confrontationPhaseForm(
  stage: Pick<StructureStageHubSummary, 'defaultTieFormat'>,
): TieFormState {
  return tieFormFromSummary(stage.defaultTieFormat);
}

export function confrontationFormatDiffersFromPhase(
  stage: Pick<
    StructureStageHubSummary,
    'confrontationSegments' | 'defaultTieFormat'
  >,
): boolean {
  const phaseForm = confrontationPhaseForm(stage);
  const segments = stage.confrontationSegments ?? [];
  if (segments.length === 0) {
    return false;
  }
  return segments.some(
    (segment) => !sameTie(tieFormFromSegment(segment), phaseForm),
  );
}

export function confrontationFormatScopeHintKey(
  stage: Pick<
    StructureStageHubSummary,
    'confrontationSegments' | 'defaultTieFormat'
  >,
): string {
  return confrontationFormatDiffersFromPhase(stage)
    ? 'fiche.confrontationFormatHint.perRound'
    : 'fiche.confrontationFormatHint.uniform';
}

export function confrontationFormatScopeHintAttention(
  stage: Pick<
    StructureStageHubSummary,
    'confrontationSegments' | 'defaultTieFormat'
  >,
): boolean {
  return confrontationFormatDiffersFromPhase(stage);
}

function formatCompactTieFormat(
  form: TieFormState,
  tStructure: Translate,
  tReg: Translate,
): string {
  const parts: string[] = [];
  parts.push(
    form.numberOfLegs === 2
      ? tStructure('regulation.legsTwo')
      : tStructure('regulation.legsOne'),
  );
  if (form.numberOfLegs === 2 && form.aggregateScoring) {
    parts.push(tReg('tokens.tieAggregateShort'));
  }
  if (form.numberOfLegs === 2 && form.hasAwayGoalsRule) {
    parts.push(tReg('tokens.tieAwayGoalsShort'));
  }
  if (form.hasExtraTimeRule) {
    parts.push(tReg('tokens.tieExtraTime'));
  }
  if (form.hasPenaltyShootoutRule) {
    parts.push(tReg('tokens.tiePenalties'));
  }
  return parts.join(' · ');
}

export function confrontationRoundDigestDeltaParts(
  round: TieFormState,
  phase: TieFormState,
  tStructure: Translate,
  tReg: Translate,
): string[] {
  const parts: string[] = [];

  if (round.numberOfLegs !== phase.numberOfLegs) {
    parts.push(
      tStructure(
        round.numberOfLegs === 2 ? 'regulation.legsTwo' : 'regulation.legsOne',
      ),
    );
  }

  if (round.numberOfLegs === 2) {
    if (round.aggregateScoring !== phase.aggregateScoring) {
      parts.push(
        round.aggregateScoring
          ? tStructure('fiche.confrontationAdded', {
              rule: tReg('tokens.tieAggregateShort'),
            })
          : tStructure('fiche.confrontationWithout', {
              rule: tReg('tokens.tieAggregateShort'),
            }),
      );
    }
    if (round.hasAwayGoalsRule !== phase.hasAwayGoalsRule) {
      parts.push(
        round.hasAwayGoalsRule
          ? tStructure('fiche.confrontationAdded', {
              rule: tReg('tokens.tieAwayGoalsShort'),
            })
          : tStructure('fiche.confrontationWithout', {
              rule: tReg('tokens.tieAwayGoalsShort'),
            }),
      );
    }
  }

  if (round.hasExtraTimeRule !== phase.hasExtraTimeRule) {
    parts.push(
      round.hasExtraTimeRule
        ? tStructure('fiche.confrontationAdded', {
            rule: tReg('tokens.tieExtraTime'),
          })
        : tStructure('fiche.confrontationWithout', {
            rule: tReg('tokens.tieExtraTime'),
          }),
    );
  }
  if (round.hasPenaltyShootoutRule !== phase.hasPenaltyShootoutRule) {
    parts.push(
      round.hasPenaltyShootoutRule
        ? tStructure('fiche.confrontationAdded', {
            rule: tReg('tokens.tiePenalties'),
          })
        : tStructure('fiche.confrontationWithout', {
            rule: tReg('tokens.tiePenalties'),
          }),
    );
  }

  return parts;
}

function confrontationDigestLabeledRow(
  label: string,
  value: string,
  separator: ':' | '·' = ':',
): ReactNode {
  const sep = separator === ':' ? ' : ' : ' · ';
  return (
    <>
      <span className="structure-regulation-digest__segment">{label}</span>
      <span aria-hidden="true">{sep}</span>
      <span>{value}</span>
    </>
  );
}

function formatRoundDigestDeltaText(
  roundForm: TieFormState,
  phaseForm: TieFormState,
  tStructure: Translate,
  tReg: Translate,
): string {
  return confrontationRoundDigestDeltaParts(
    roundForm,
    phaseForm,
    tStructure,
    tReg,
  ).join(' · ');
}

export function buildConfrontationDigestTokens(
  stage: StructureStageHubSummary,
  tStructure: Translate,
  tReg: Translate,
): RegulationDigestToken[] {
  const segments = [...(stage.confrontationSegments ?? [])].sort((a, b) => {
    const ao = a.rounds[0]?.sortOrder ?? 0;
    const bo = b.rounds[0]?.sortOrder ?? 0;
    return ao - bo;
  });

  if (segments.length === 0) {
    const fallback: StructureConfrontationSegment = {
      rounds: [],
      numberOfLegs: stage.numberOfLegs ?? 1,
      aggregateScoring: stage.aggregateScoring ?? false,
      hasAwayGoalsRule: stage.hasAwayGoalsRule ?? false,
      hasTieExtraTime: stage.hasTieExtraTime ?? false,
      hasTiePenaltyShootout: stage.hasTiePenaltyShootout ?? false,
    };
    return confrontationItems(fallback, 'default', tReg);
  }

  const phaseForm = confrontationPhaseForm(stage);
  const phaseCompact = formatCompactTieFormat(phaseForm, tStructure, tReg);
  const tokens: RegulationDigestToken[] = [
    {
      key: 'phase-format',
      icon:
        phaseForm.numberOfLegs === 2 ? (
          <LegsStatIcon size="sm" aria-hidden="true" />
        ) : (
          <ArrowRightIcon size="sm" aria-hidden="true" />
        ),
      content: confrontationDigestLabeledRow(
        tStructure('fiche.confrontationPhasePrefix'),
        phaseCompact,
        ':',
      ),
    },
  ];

  type RoundRow = {
    roundId: string;
    name: string;
    sortOrder: number;
    form: TieFormState;
  };
  const differingRounds: RoundRow[] = [];

  for (const segment of segments) {
    const segmentForm = tieFormFromSegment(segment);
    if (sameTie(segmentForm, phaseForm)) {
      continue;
    }
    for (const round of [...segment.rounds].sort(
      (a, b) => a.sortOrder - b.sortOrder,
    )) {
      differingRounds.push({
        roundId: round.roundId,
        name: round.name || round.roundId,
        sortOrder: round.sortOrder,
        form: segmentForm,
      });
    }
  }

  differingRounds.sort((a, b) => a.sortOrder - b.sortOrder);

  const visible =
    differingRounds.length > MAX_VISIBLE_ROUND_LINES
      ? differingRounds.slice(0, MAX_VISIBLE_ROUND_LINES - 1)
      : differingRounds;

  for (const round of visible) {
    tokens.push({
      key: `round-${round.roundId}`,
      icon: <RoundsStatIcon size="sm" aria-hidden="true" />,
      content: confrontationDigestLabeledRow(
        round.name,
        formatRoundDigestDeltaText(round.form, phaseForm, tStructure, tReg),
        '·',
      ),
    });
  }

  const hiddenCount = differingRounds.length - visible.length;
  if (hiddenCount > 0) {
    tokens.push({
      key: 'rounds-overflow',
      icon: <RoundsStatIcon size="sm" aria-hidden="true" />,
      content: (
        <span>
          {tStructure('fiche.confrontationMoreRounds', { count: hiddenCount })}
        </span>
      ),
    });
  }

  return tokens;
}
