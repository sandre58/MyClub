import type { RankingCriterion } from '../types';

type Translate = (key: string, options?: Record<string, unknown>) => string;

export function criterionLabel(criterion: RankingCriterion, t: Translate) {
  return t(`criteria.${criterion}`, { defaultValue: criterion });
}
