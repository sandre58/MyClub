import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { renderWithI18n } from '../../../test/renderWithI18n';
import { CompositionQualifierRuleLabel } from './CompositionQualifierRuleLabel';

const placeRule = {
  key: 'q1',
  volume: 1,
  badge: '9e',
  badgeTone: 'accent' as const,
  context: 'Classement',
  family: 'place' as const,
  sortPrimary: 1,
  sortSecondary: '1',
  qualPathOrder: 1,
};

const progressionRule = {
  key: 'p1',
  volume: 1,
  badge: 'Perdant',
  badgeTone: 'loss' as const,
  context: 'P1',
  family: 'result' as const,
  sortPrimary: 1,
  sortSecondary: '1',
  progressionPairKey: 'P1',
  progressionOutcome: 'Loser' as const,
};

function ruleLabelAccessibleText(container: HTMLElement): string {
  const label = container.querySelector('.structure-feed-group__rule-label');
  return (
    label?.textContent
      ?.replace(/\s*·\s*/g, ' · ')
      .replace(/\s+/g, ' ')
      .trim() ?? ''
  );
}

describe('CompositionQualifierRuleLabel', () => {
  it('prefixes équipes-band qualification rules with source phase first in reading order', () => {
    const { container } = renderWithI18n(
      <CompositionQualifierRuleLabel
        rule={{
          ...placeRule,
          badge: '1er',
          context: 'Groupe A',
        }}
        sourcePhaseName="Phase de groupes"
      />,
    );
    expect(screen.getByText('Phase de groupes')).toHaveClass(
      'structure-feed-group__rule-source',
    );
    const label = container.querySelector('.structure-feed-group__rule-label');
    expect(label).not.toHaveAttribute('aria-label');
    expect(ruleLabelAccessibleText(container)).toBe(
      'Phase de groupes · 1er · Groupe A',
    );
  });

  it('prefixes équipes-band progression rules with phase before outcome and pair key', () => {
    const { container } = renderWithI18n(
      <CompositionQualifierRuleLabel
        rule={progressionRule}
        sourcePhaseName="Barrage"
      />,
    );
    const label = container.querySelector('.structure-feed-group__rule-label');
    expect(label).not.toHaveAttribute('aria-label');
    expect(ruleLabelAccessibleText(container)).toBe('Barrage · Perdant · P1');
    expect(screen.getByText('Perdant')).toHaveClass(
      'structure-feed-group__rule-lead--loss',
    );
  });

  it('does not prefix when source phase is omitted (Qualifiés)', () => {
    const { container } = renderWithI18n(
      <CompositionQualifierRuleLabel rule={progressionRule} />,
    );
    expect(
      container.querySelector('.structure-feed-group__rule-source'),
    ).toBeNull();
    expect(ruleLabelAccessibleText(container)).toBe('Perdant · P1');
  });
});
