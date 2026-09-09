import type { RankingCriterion } from '../types';

export const ALL_RANKING_CRITERIA: RankingCriterion[] = [
  'Points',
  'GoalDifference',
  'GoalsFor',
  'GoalsAgainst',
  'Wins',
  'HeadToHead',
];

export const DEFAULT_RANKING_CRITERIA: RankingCriterion[] = [
  'Points',
  'GoalDifference',
  'GoalsFor',
  'HeadToHead',
];

/** Points always first — T3 arbitration. */
export function normalizeCriteria(
  criteria: RankingCriterion[] | null | undefined,
): RankingCriterion[] {
  const rest = (criteria ?? DEFAULT_RANKING_CRITERIA).filter(
    (item) => item !== 'Points',
  );
  return ['Points', ...rest];
}
