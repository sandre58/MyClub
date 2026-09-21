// -----------------------------------------------------------------------
// Population rail — soft-truncate Expand paths (no Intent grain, no merge).
// Decision: Population = chemins ; Sorties = intentions.
// Merge-by-badge was rejected (e.g. "1st · A · B" / "2nd · A · B" for
// "1st & 2nd of each group" reads as neither path nor intent).
// -----------------------------------------------------------------------

export const POPULATION_FEED_RULE_VISIBLE_MAX = 8;

export type CompactFeedFamily = 'place' | 'result';

export type CompactFeedRule = {
  key: string;
  badge: string;
  badgeTone: 'win' | 'loss' | 'neutral' | 'accent';
  context: string;
  extra?: string;
  volume: number;
  family: CompactFeedFamily;
  sortPrimary: number;
  sortSecondary: string;
};

export type CompactFeedResult = {
  rules: CompactFeedRule[];
  /** Paths not shown after soft truncation. */
  hiddenCount: number;
};

type Translate = (key: string, opts?: Record<string, unknown>) => string;

/**
 * Soft-truncate only — keep one row per Expand path.
 * Volume on the group head already totals expected teams.
 */
export function compactPopulationFeedRules(
  rules: CompactFeedRule[],
  _t: Translate,
  options?: {
    ruleVisibleMax?: number;
  },
): CompactFeedResult {
  const ruleVisibleMax =
    options?.ruleVisibleMax ?? POPULATION_FEED_RULE_VISIBLE_MAX;

  if (rules.length === 0) {
    return { rules: [], hiddenCount: 0 };
  }

  if (rules.length <= ruleVisibleMax) {
    return { rules, hiddenCount: 0 };
  }

  const visible = rules.slice(0, ruleVisibleMax);
  const hidden = rules.slice(ruleVisibleMax);
  const hiddenCount = hidden.reduce(
    (sum, r) => sum + Math.max(1, r.volume),
    0,
  );
  return {
    rules: visible,
    hiddenCount: hiddenCount > 0 ? hiddenCount : hidden.length,
  };
}
