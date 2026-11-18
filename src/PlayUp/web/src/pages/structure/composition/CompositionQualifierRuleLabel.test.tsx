import { fireEvent, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

function mockFinePointer() {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches:
        query.includes('hover: hover') && query.includes('pointer: fine'),
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    }),
  });
}

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

function ruleLabelVisibleText(container: HTMLElement): string {
  const label = container.querySelector('.structure-feed-group__rule-label');
  if (!label) {
    return '';
  }
  const chunks: string[] = [];
  label.childNodes.forEach((node) => {
    if (
      node instanceof HTMLElement &&
      node.getAttribute('aria-hidden') === 'true'
    ) {
      return;
    }
    const text = node.textContent?.trim();
    if (text) {
      chunks.push(text);
    }
  });
  return chunks.join(' · ');
}

describe('CompositionQualifierRuleLabel', () => {
  beforeEach(() => {
    mockFinePointer();
  });

  it('prefixes équipes-band qualification rules with source phase glyph before the badge', () => {
    const { container } = renderWithI18n(
      <CompositionQualifierRuleLabel
        rule={{
          ...placeRule,
          badge: '1er',
          context: 'Groupe A',
        }}
        sourcePhaseName="Phase de groupes"
        sourcePhaseFormatKind="Groups"
      />,
    );
    expect(screen.queryByText('Phase de groupes')).toBeNull();
    const trigger = screen.getByLabelText('Phase de groupes');
    expect(trigger).toHaveClass('structure-feed-group__rule-source-trigger');
    expect(trigger).toHaveAttribute('role', 'img');
    expect(container.querySelector('.ds-icon--xs')).not.toBeNull();
    const source = container.querySelector(
      '.structure-feed-group__rule-source',
    );
    expect(
      source?.nextElementSibling?.classList.contains(
        'structure-feed-group__rule-sep',
      ),
    ).toBe(false);
    expect(
      container.querySelector('.structure-feed-group__rule-source'),
    ).not.toBeNull();
    expect(ruleLabelVisibleText(container)).toBe('1er · Groupe A');
  });

  it('exposes the source phase name for assistive tech and in the tooltip', async () => {
    const { container } = renderWithI18n(
      <CompositionQualifierRuleLabel
        rule={progressionRule}
        sourcePhaseName="Barrage"
        sourcePhaseFormatKind="Cup"
      />,
    );
    const trigger = screen.getByLabelText('Barrage');
    expect(trigger).toHaveAttribute('tabIndex', '0');
    expect(ruleLabelVisibleText(container)).toBe('Perdant · P1');
    expect(screen.queryByText('Barrage')).toBeNull();
    fireEvent.focus(trigger);
    expect(await screen.findByRole('tooltip')).toHaveTextContent('Barrage');
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
    expect(ruleLabelVisibleText(container)).toBe('Perdant · P1');
  });
});
