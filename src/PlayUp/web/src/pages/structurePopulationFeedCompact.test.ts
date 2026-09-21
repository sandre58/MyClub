import { describe, expect, it } from 'vitest';
import {
  compactPopulationFeedRules,
  type CompactFeedRule,
} from './structurePopulationFeedCompact';

const t = (key: string) => key;

function rule(
  partial: Partial<CompactFeedRule> & Pick<CompactFeedRule, 'key' | 'badge'>,
): CompactFeedRule {
  return {
    badgeTone: 'accent',
    context: 'Standings',
    volume: 1,
    family: 'place',
    sortPrimary: 1,
    sortSecondary: '',
    ...partial,
  };
}

describe('compactPopulationFeedRules', () => {
  it('keeps EachGroup paths atomic (no merge by badge)', () => {
    const rules = [
      rule({
        key: '1a',
        badge: '1st',
        context: 'Group A',
        sortPrimary: 1,
        sortSecondary: 'A',
      }),
      rule({
        key: '1b',
        badge: '1st',
        context: 'Group B',
        sortPrimary: 1,
        sortSecondary: 'B',
      }),
      rule({
        key: '2a',
        badge: '2nd',
        context: 'Group A',
        sortPrimary: 2,
        sortSecondary: 'A',
      }),
      rule({
        key: '2b',
        badge: '2nd',
        context: 'Group B',
        sortPrimary: 2,
        sortSecondary: 'B',
      }),
    ];
    const result = compactPopulationFeedRules(rules, t);
    expect(result.rules).toHaveLength(4);
    expect(result.rules.map((r) => `${r.badge}|${r.context}`)).toEqual([
      '1st|Group A',
      '1st|Group B',
      '2nd|Group A',
      '2nd|Group B',
    ]);
    expect(result.hiddenCount).toBe(0);
  });

  it('soft-truncates long lists', () => {
    const rules = Array.from({ length: 10 }, (_, i) =>
      rule({
        key: `r-${i}`,
        badge: `${i + 1}th`,
        context: 'Standings',
        sortPrimary: i + 1,
      }),
    );
    const result = compactPopulationFeedRules(rules, t, {
      ruleVisibleMax: 3,
    });
    expect(result.rules).toHaveLength(3);
    expect(result.hiddenCount).toBe(7);
  });
});
