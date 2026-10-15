import type { ReactNode } from 'react';
import {
  AggregateIcon,
  ArrowRightIcon, ExtraTimeIcon, ForfeitIcon,
  LegsStatIcon,
  PenaltiesIcon, PointsRulesIcon, SortingRulesIcon,
  TimerIcon,
} from '../../design-system/icons/contentIcons';
import { PinIcon } from '../../design-system/icons/metaIcons';
import { Chip } from '../../design-system/components/Chip';
import type {
  RankingCriterion,
  StructureConfrontationSegment,
  StructureStageHubSummary,
} from '../../types';

type Translate = (key: string, options?: Record<string, unknown>) => string;

export type RegulationDigestToken = {
  key: string;
  icon: ReactNode;
  content: ReactNode;
};

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
          {kicks != null ? `${t('matchTimeline.tabAria', { count: kicks })}` : null}
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

export function buildConfrontationDigestTokens(
  stage: StructureStageHubSummary,
  t: Translate,
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
    return confrontationItems(fallback, 'default', t);
  }

  if (segments.length === 1) {
    return confrontationItems(segments[0]!, 'seg-0', t);
  }

  const tokens: RegulationDigestToken[] = [];
  segments.forEach((seg, index) => {
    const title =
      seg.rounds
        .map((r) => r.name)
        .filter(Boolean)
        .join(' / ') || `§${index + 1}`;
    tokens.push({
      key: `seg-${index}-title`,
      icon: <ArrowRightIcon size="sm" aria-hidden="true" />,
      content: <span className="structure-regulation-digest__segment">{title}</span>,
    });
    tokens.push(...confrontationItems(seg, `seg-${index}`, t));
  });
  return tokens;
}

export function RegulationDigestFacts({
  tokens,
  ariaLabel,
}: {
  tokens: RegulationDigestToken[];
  ariaLabel?: string;
}) {
  if (tokens.length === 0) {
    return null;
  }

  return (
    <ul
      className="structure-regulation-digest__facts"
      aria-label={ariaLabel}
    >
      {tokens.map((token) => (
        <li key={token.key} className="structure-regulation-digest__fact">
          <span className="structure-regulation-digest__fact-icon" aria-hidden="true">
            {token.icon}
          </span>
          <span className="structure-regulation-digest__fact-content">
            {token.content}
          </span>
        </li>
      ))}
    </ul>
  );
}
