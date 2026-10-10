import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SelectableTile } from './SelectableTile';

describe('SelectableTile', () => {
  it('marks selection on the foundation class', () => {
    render(
      <SelectableTile selected className="teams-tile">
        Alpha
      </SelectableTile>,
    );

    const tile = screen.getByText('Alpha');
    expect(tile).toHaveClass('ds-selectable-tile', 'teams-tile');
    expect(tile).toHaveAttribute('data-selected', 'true');
  });

  it('renders as a custom element', () => {
    render(
      <SelectableTile as="article" selected={false} aria-label="Beta">
        Body
      </SelectableTile>,
    );

    const tile = screen.getByLabelText('Beta');
    expect(tile.tagName).toBe('ARTICLE');
    expect(tile).toHaveAttribute('data-selected', 'false');
  });
});
