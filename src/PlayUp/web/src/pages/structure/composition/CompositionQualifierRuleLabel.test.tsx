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

describe('CompositionQualifierRuleLabel', () => {
  it('prefixes équipes-band rules with source phase and accessible label', () => {
    renderWithI18n(
      <CompositionQualifierRuleLabel
        rule={placeRule}
        sourcePhaseName="Poules"
      />,
    );
    expect(screen.getByText('Poules')).toHaveClass(
      'structure-feed-group__rule-source',
    );
    expect(screen.getByText('9e')).toBeInTheDocument();
    expect(screen.getByText('Classement')).toBeInTheDocument();
    const label = screen
      .getByText('9e')
      .closest('.structure-feed-group__rule-label');
    expect(label).toHaveAttribute('aria-label', 'Poules · 9e · Classement');
  });

  it('does not prefix when source phase is omitted (Qualifiés)', () => {
    const { container } = renderWithI18n(
      <CompositionQualifierRuleLabel rule={progressionRule} />,
    );
    expect(
      container.querySelector('.structure-feed-group__rule-source'),
    ).toBeNull();
    expect(screen.getByText('Perdant')).toHaveClass(
      'structure-feed-group__rule-lead--loss',
    );
  });
});
