// -----------------------------------------------------------------------
// Population rail — compact Expand paths without reintroducing Intent grain.
// Decision: rails Population/Sorties — chemins vs intentions ; R1 no Place chip.
// -----------------------------------------------------------------------

export const POPULATION_FEED_RULE_VISIBLE_MAX = 6;
export const POPULATION_FEED_CONTEXT_VISIBLE_MAX = 4;

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
  /** Paths not shown as rows after merge + truncation. */
  hiddenCount: number;
};

type Translate = (key: string, opts?: Record<string, unknown>) => string;

function mergeKey(rule: CompactFeedRule): string {
  return [
    rule.family,
    rule.badge,
    rule.badgeTone,
    String(rule.sortPrimary),
    rule.extra ?? '',
  ].join('\0');
}

/**
 * Join distinct contexts; when too many, keep the first N and a +count suffix.
 * Still path-level reading — not an Intent summary.
 */
export function compactContexts(
  contexts: string[],
  t: Translate,
  maxVisible: number = POPULATION_FEED_CONTEXT_VISIBLE_MAX,
): string {
  const unique = [...new Set(contexts.map((c) => c.trim()).filter(Boolean))];
  if (unique.length === 0) return '';
  if (unique.length === 1) return unique[0]!;
  if (unique.length <= maxVisible) return unique.join(' · ');
  const head = unique.slice(0, maxVisible);
  const rest = unique.length - maxVisible;
  return `${head.join(' · ')} ${t('population.feed.moreContexts', { count: rest })}`;
}

/**
 * 1) Merge rows that share the same operational "who" (badge / family / place
 *    rank / extra) but differ only by context (group / match #).
 * 2) Soft-truncate remaining rows to `ruleVisibleMax`.
 */
export function compactPopulationFeedRules(
  rules: CompactFeedRule[],
  t: Translate,
  options?: {
    ruleVisibleMax?: number;
    contextVisibleMax?: number;
  },
): CompactFeedResult {
  const ruleVisibleMax =
    options?.ruleVisibleMax ?? POPULATION_FEED_RULE_VISIBLE_MAX;
  const contextVisibleMax =
    options?.contextVisibleMax ?? POPULATION_FEED_CONTEXT_VISIBLE_MAX;

  if (rules.length === 0) {
    return { rules: [], hiddenCount: 0 };
  }

  const order: string[] = [];
  const buckets = new Map<
    string,
    {
      prototype: CompactFeedRule;
      contexts: string[];
      volume: number;
      pathCount: number;
    }
  >();

  for (const rule of rules) {
    const key = mergeKey(rule);
    const existing = buckets.get(key);
    if (existing) {
      existing.contexts.push(rule.context);
      existing.volume += rule.volume;
      existing.pathCount += 1;
    } else {
      order.push(key);
      buckets.set(key, {
        prototype: rule,
        contexts: [rule.context],
        volume: rule.volume,
        pathCount: 1,
      });
    }
  }

  const merged: CompactFeedRule[] = order.map((key) => {
    const bucket = buckets.get(key)!;
    const { prototype, contexts, volume, pathCount } = bucket;
    if (pathCount === 1) {
      return { ...prototype, volume };
    }
    return {
      ...prototype,
      key: `merged-${key}`,
      context: compactContexts(contexts, t, contextVisibleMax),
      volume,
      sortSecondary: contexts[0] ?? prototype.sortSecondary,
    };
  });

  if (merged.length <= ruleVisibleMax) {
    return { rules: merged, hiddenCount: 0 };
  }

  const visible = merged.slice(0, ruleVisibleMax);
  const hidden = merged.slice(ruleVisibleMax);
  const hiddenCount = hidden.reduce((sum, r) => sum + Math.max(1, r.volume), 0);
  // Prefer path-ish count: each truncated merged row contributes its volume
  // (already summed Expand volume). Fallback 1 per row if volume 0.
  return {
    rules: visible,
    hiddenCount:
      hiddenCount > 0
        ? hiddenCount
        : hidden.length,
  };
}
