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
  icon: ReactNode | null;
  content: ReactNode;
  variant?: 'default' | 'confrontation-row';
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

type ConfrontationFactChip = {
  key: string;
  icon: ReactNode;
  content: ReactNode;
};

function withoutRuleLabel(rule: string, tStructure: Translate): string {
  return tStructure('fiche.confrontationWithout', { rule });
}

export function confrontationFactsFromForm(
  form: TieFormState,
  keyPrefix: string,
  tReg: Translate,
): ConfrontationFactChip[] {
  const twoLegs = form.numberOfLegs === 2;
  const chips: ConfrontationFactChip[] = [
    {
      key: `${keyPrefix}-legs`,
      icon: twoLegs ? (
        <LegsStatIcon size="sm" aria-hidden="true" />
      ) : (
        <ArrowRightIcon size="sm" aria-hidden="true" />
      ),
      content: twoLegs ? tReg('tokens.tieTwoLegs') : tReg('tokens.tieOneLeg'),
    },
  ];
  if (twoLegs && form.aggregateScoring) {
    chips.push({
      key: `${keyPrefix}-agg`,
      icon: <AggregateIcon size="sm" aria-hidden="true" />,
      content: tReg('tokens.tieAggregate'),
    });
  }
  if (twoLegs && form.hasAwayGoalsRule) {
    chips.push({
      key: `${keyPrefix}-away`,
      icon: <PinIcon size="sm" aria-hidden="true" />,
      content: tReg('tokens.tieAwayGoals'),
    });
  }
  if (form.hasExtraTimeRule) {
    chips.push({
      key: `${keyPrefix}-et`,
      icon: <ExtraTimeIcon size="sm" aria-hidden="true" />,
      content: tReg('tokens.tieExtraTime'),
    });
  }
  if (form.hasPenaltyShootoutRule) {
    chips.push({
      key: `${keyPrefix}-tab`,
      icon: <PenaltiesIcon size="sm" aria-hidden="true" />,
      content: tReg('tokens.tiePenalties'),
    });
  }
  return chips;
}

export function confrontationDeltaFactsFromForms(
  round: TieFormState,
  phase: TieFormState,
  keyPrefix: string,
  tStructure: Translate,
  tReg: Translate,
): ConfrontationFactChip[] {
  const chips: ConfrontationFactChip[] = [];

  if (round.numberOfLegs !== phase.numberOfLegs) {
    const twoLegs = round.numberOfLegs === 2;
    chips.push({
      key: `${keyPrefix}-legs`,
      icon: twoLegs ? (
        <LegsStatIcon size="sm" aria-hidden="true" />
      ) : (
        <ArrowRightIcon size="sm" aria-hidden="true" />
      ),
      content: twoLegs ? tReg('tokens.tieTwoLegs') : tReg('tokens.tieOneLeg'),
    });
  }

  if (round.numberOfLegs === 2) {
    if (round.aggregateScoring !== phase.aggregateScoring) {
      chips.push({
        key: `${keyPrefix}-agg`,
        icon: <AggregateIcon size="sm" aria-hidden="true" />,
        content: round.aggregateScoring
          ? tReg('tokens.tieAggregate')
          : withoutRuleLabel(tReg('tokens.tieAggregate'), tStructure),
      });
    }
    if (round.hasAwayGoalsRule !== phase.hasAwayGoalsRule) {
      chips.push({
        key: `${keyPrefix}-away`,
        icon: <PinIcon size="sm" aria-hidden="true" />,
        content: round.hasAwayGoalsRule
          ? tReg('tokens.tieAwayGoals')
          : withoutRuleLabel(tReg('tokens.tieAwayGoals'), tStructure),
      });
    }
  }

  if (round.hasExtraTimeRule !== phase.hasExtraTimeRule) {
    chips.push({
      key: `${keyPrefix}-et`,
      icon: <ExtraTimeIcon size="sm" aria-hidden="true" />,
      content: round.hasExtraTimeRule
        ? tReg('tokens.tieExtraTime')
        : withoutRuleLabel(tReg('tokens.tieExtraTime'), tStructure),
    });
  }
  if (round.hasPenaltyShootoutRule !== phase.hasPenaltyShootoutRule) {
    chips.push({
      key: `${keyPrefix}-tab`,
      icon: <PenaltiesIcon size="sm" aria-hidden="true" />,
      content: round.hasPenaltyShootoutRule
        ? tReg('tokens.tiePenalties')
        : withoutRuleLabel(tReg('tokens.tiePenalties'), tStructure),
    });
  }

  return chips;
}

function confrontationRowContent(
  leading: ReactNode | null,
  chips: ConfrontationFactChip[],
): ReactNode {
  return (
    <span className="structure-regulation-digest__confrontation-row">
      {leading}
      {chips.map((chip) => (
        <span
          key={chip.key}
          className="structure-regulation-digest__confrontation-chip"
        >
          <span className="structure-regulation-digest__fact-icon" aria-hidden>
            {chip.icon}
          </span>
          <span>{chip.content}</span>
        </span>
      ))}
    </span>
  );
}

function roundNameLead(roundName: string): ReactNode {
  return (
    <span className="structure-regulation-digest__confrontation-chip">
      <span className="structure-regulation-digest__fact-icon" aria-hidden>
        <RoundsStatIcon size="sm" aria-hidden="true" />
      </span>
      <span className="structure-regulation-digest__segment">{roundName}</span>
    </span>
  );
}

function confrontationRowToken(
  key: string,
  content: ReactNode,
): RegulationDigestToken {
  return {
    key,
    icon: null,
    content,
    variant: 'confrontation-row',
  };
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
  const tokens: RegulationDigestToken[] = [
    confrontationRowToken(
      'phase-row',
      confrontationRowContent(
        null,
        confrontationFactsFromForm(phaseForm, 'phase', tReg),
      ),
    ),
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
    const deltaChips = confrontationDeltaFactsFromForms(
      round.form,
      phaseForm,
      `round-${round.roundId}`,
      tStructure,
      tReg,
    );
    tokens.push(
      confrontationRowToken(
        `round-${round.roundId}`,
        confrontationRowContent(roundNameLead(round.name), deltaChips),
      ),
    );
  }

  const hiddenCount = differingRounds.length - visible.length;
  if (hiddenCount > 0) {
    tokens.push(
      confrontationRowToken(
        'rounds-overflow',
        confrontationRowContent(null, [
          {
            key: 'rounds-overflow',
            icon: <RoundsStatIcon size="sm" aria-hidden="true" />,
            content: tStructure('fiche.confrontationMoreRounds', {
              count: hiddenCount,
            }),
          },
        ]),
      ),
    );
  }

  return tokens;
}
