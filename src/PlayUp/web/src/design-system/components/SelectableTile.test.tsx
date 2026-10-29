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

  it('sets data-tone on error tiles and keeps the disclosure chevron hidden from assistive tech', () => {
    render(
      <SelectableTile tone="error" selected aria-label="Tile">
        Body
      </SelectableTile>,
    );
    const tile = screen.getByLabelText('Tile');
    expect(tile).toHaveAttribute('data-tone', 'error');
    expect(tile.querySelector('.ds-selectable-tile__chevron')).toHaveAttribute(
      'aria-hidden',
      'true',
    );
  });

  it('leaves the default tile without data-tone', () => {
    render(<SelectableTile>Plain</SelectableTile>);
    expect(screen.getByText('Plain')).not.toHaveAttribute('data-tone');
  });
});
