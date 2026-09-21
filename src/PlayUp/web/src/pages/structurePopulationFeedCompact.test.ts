import { describe, expect, it } from 'vitest';
import {
  compactContexts,
  compactPopulationFeedRules,
  type CompactFeedRule,
} from './structurePopulationFeedCompact';

const t = (key: string, opts?: Record<string, unknown>) => {
  if (key === 'population.feed.moreContexts') {
    return `+${opts?.count}`;
  }
  return key;
};

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

describe('compactContexts', () => {
  it('returns single context unchanged', () => {
    expect(compactContexts(['Group A'], t)).toBe('Group A');
  });

  it('joins a short list', () => {
    expect(compactContexts(['A', 'B', 'C'], t)).toBe('A · B · C');
  });

  it('truncates long context lists', () => {
    expect(
      compactContexts(['A', 'B', 'C', 'D', 'E', 'F'], t, 4),
    ).toBe('A · B · C · D +2');
  });
});

describe('compactPopulationFeedRules', () => {
  it('leaves distinct badges untouched', () => {
    const rules = [
      rule({ key: '1', badge: '1st', context: 'Group A', sortPrimary: 1 }),
      rule({ key: '2', badge: '2nd', context: 'Group A', sortPrimary: 2 }),
    ];
    const result = compactPopulationFeedRules(rules, t);
    expect(result.rules).toHaveLength(2);
    expect(result.hiddenCount).toBe(0);
  });

  it('merges EachGroup-like paths sharing badge', () => {
    const rules = ['A', 'B', 'C', 'D'].map((g, i) =>
      rule({
        key: `p-${i}`,
        badge: '1st',
        context: `Group ${g}`,
        sortPrimary: 1,
        sortSecondary: g,
      }),
    );
    const result = compactPopulationFeedRules(rules, t);
    expect(result.rules).toHaveLength(1);
    expect(result.rules[0]!.badge).toBe('1st');
    expect(result.rules[0]!.context).toBe('Group A · Group B · Group C · Group D');
    expect(result.rules[0]!.volume).toBe(4);
    expect(result.hiddenCount).toBe(0);
  });

  it('merges Winner paths across matches', () => {
    const rules = [1, 2, 3].map((n) =>
      rule({
        key: `w-${n}`,
        badge: 'Winner',
        badgeTone: 'win',
        context: `Match #${n}`,
        family: 'result',
        sortPrimary: 0,
        sortSecondary: String(n),
      }),
    );
    const result = compactPopulationFeedRules(rules, t);
    expect(result.rules).toHaveLength(1);
    expect(result.rules[0]!.context).toBe('Match #1 · Match #2 · Match #3');
    expect(result.rules[0]!.volume).toBe(3);
  });

  it('truncates after merge when too many distinct rows', () => {
    const rules = Array.from({ length: 8 }, (_, i) =>
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
    expect(result.hiddenCount).toBe(5);
  });

  it('does not merge when extra differs (e.g. points gate)', () => {
    const rules = [
      rule({
        key: 'a',
        badge: '1st',
        context: 'Group A',
        extra: '≥ 4 pts',
      }),
      rule({
        key: 'b',
        badge: '1st',
        context: 'Group B',
      }),
    ];
    const result = compactPopulationFeedRules(rules, t);
    expect(result.rules).toHaveLength(2);
  });
});
