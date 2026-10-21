import type {
  ReplaceStageDefaultTieFormatRequest,
  StructureConfrontationSegment,
  StructureStageHubSummary,
  StructureTieFormatSummary,
} from '../../types';

export type TieFormState = {
  numberOfLegs: 1 | 2;
  aggregateScoring: boolean;
  hasAwayGoalsRule: boolean;
  hasExtraTimeRule: boolean;
  hasPenaltyShootoutRule: boolean;
};

export function defaultOneLegForm(): TieFormState {
  return {
    numberOfLegs: 1,
    aggregateScoring: false,
    hasAwayGoalsRule: false,
    hasExtraTimeRule: false,
    hasPenaltyShootoutRule: false,
  };
}

export function stableTie(form: TieFormState): TieFormState {
  const legs = form.numberOfLegs === 2 ? 2 : 1;
  const aggregate = legs === 2;
  return {
    numberOfLegs: legs,
    aggregateScoring: aggregate,
    hasAwayGoalsRule: aggregate && form.hasAwayGoalsRule,
    hasExtraTimeRule: form.hasExtraTimeRule,
    hasPenaltyShootoutRule: form.hasPenaltyShootoutRule,
  };
}

export function sameTie(a: TieFormState, b: TieFormState): boolean {
  return JSON.stringify(stableTie(a)) === JSON.stringify(stableTie(b));
}

export function cloneTie(form: TieFormState): TieFormState {
  return { ...stableTie(form) };
}

export function toRequest(
  form: TieFormState,
): ReplaceStageDefaultTieFormatRequest {
  const stable = stableTie(form);
  return {
    clear: false,
    numberOfLegs: stable.numberOfLegs,
    hasAwayGoalsRule: stable.hasAwayGoalsRule,
    hasExtraTimeRule: stable.hasExtraTimeRule,
    hasPenaltyShootoutRule: stable.hasPenaltyShootoutRule,
  };
}

export function tieFormFromSummary(
  summary: StructureTieFormatSummary | null | undefined,
): TieFormState {
  if (!summary) {
    return defaultOneLegForm();
  }
  const legs = summary.numberOfLegs === 2 ? 2 : 1;
  const aggregate = legs === 2;
  return {
    numberOfLegs: legs,
    aggregateScoring: aggregate,
    hasAwayGoalsRule: aggregate && summary.hasAwayGoalsRule === true,
    hasExtraTimeRule: summary.hasTieExtraTime === true,
    hasPenaltyShootoutRule: summary.hasTiePenaltyShootout === true,
  };
}

export function tieFormFromSegment(
  segment: StructureConfrontationSegment,
): TieFormState {
  const legs = segment.numberOfLegs === 2 ? 2 : 1;
  const aggregate = legs === 2;
  return {
    numberOfLegs: legs,
    aggregateScoring: aggregate,
    hasAwayGoalsRule: aggregate && segment.hasAwayGoalsRule === true,
    hasExtraTimeRule: segment.hasTieExtraTime === true,
    hasPenaltyShootoutRule: segment.hasTiePenaltyShootout === true,
  };
}

export function roundFormsFromStage(
  stage: StructureStageHubSummary,
): Record<string, TieFormState> {
  const next: Record<string, TieFormState> = {};
  for (const segment of stage.confrontationSegments ?? []) {
    const form = tieFormFromSegment(segment);
    for (const round of segment.rounds) {
      next[round.roundId] = form;
    }
  }
  return next;
}

export function withLegs(form: TieFormState, legs: 1 | 2): TieFormState {
  const twoLegs = legs === 2;
  return {
    ...form,
    numberOfLegs: legs,
    aggregateScoring: twoLegs,
    hasAwayGoalsRule: twoLegs ? form.hasAwayGoalsRule : false,
  };
}
