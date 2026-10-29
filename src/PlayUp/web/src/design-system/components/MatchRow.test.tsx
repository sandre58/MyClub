import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MatchRow } from './MatchRow';

describe('MatchRow', () => {
  it('keeps the default row when density is omitted', () => {
    const { container } = render(
      <MatchRow
        home={{ name: 'Belgium', crest: <span /> }}
        away={{ name: 'Poland', crest: <span /> }}
        score="P1"
      />,
    );
    const row = container.querySelector('.ds-match-row');
    expect(row).not.toHaveAttribute('data-density');
    expect(screen.getByText('Belgium')).toBeInTheDocument();
  });

  it('sets data-density when compact', () => {
    const { container } = render(
      <MatchRow
        home={{ name: 'Belgium', crest: <span /> }}
        away={{ name: 'Poland', crest: <span /> }}
        score="P1"
        density="compact"
      />,
    );
    expect(container.querySelector('.ds-match-row')).toHaveAttribute(
      'data-density',
      'compact',
    );
  });
});
