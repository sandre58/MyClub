import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SelectableTile } from './SelectableTile';

const selectableTileCss = readFileSync(
  join(
    dirname(fileURLToPath(import.meta.url)),
    '../foundations/selectable-tile.css',
  ),
  'utf8',
);

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

  it('sets data-tone on error tiles without injecting body or chevron', () => {
    render(
      <SelectableTile as="div" tone="error" selected aria-label="Tile">
        <button type="button" className="ds-selectable-tile__trigger">
          Trigger
        </button>
      </SelectableTile>,
    );
    const tile = screen.getByLabelText('Tile');
    expect(tile).toHaveAttribute('data-tone', 'error');
    expect(tile.querySelector('.ds-selectable-tile__body')).toBeNull();
    expect(tile.querySelector('.ds-selectable-tile__chevron')).toBeNull();
  });

  it('leaves the default tile without data-tone', () => {
    render(<SelectableTile>Plain</SelectableTile>);
    expect(screen.getByText('Plain')).not.toHaveAttribute('data-tone');
  });

  it('documents error-tone hover and focus-visible rules in the foundation CSS', () => {
    expect(selectableTileCss).toMatch(
      /\[data-tone='error'\]\[data-selected='true'\][\s\S]*--color-error/,
    );
    expect(selectableTileCss).toMatch(
      /\[data-tone='error'\]\[data-selected='true'\]:hover[\s\S]*--color-error/,
    );
    expect(selectableTileCss).toMatch(
      /\.ds-root \.ds-selectable-tile__trigger:focus-visible[\s\S]*box-shadow:\s*none/,
    );
    expect(selectableTileCss).toMatch(
      /\.ds-root \.ds-selectable-tile:has\(\.ds-selectable-tile__trigger:focus-visible\)/,
    );
    expect(selectableTileCss).not.toMatch(
      /\.ds-selectable-tile[\s\S]*opacity:/,
    );
    expect(selectableTileCss).not.toMatch(/\.ds-selectable-tile__body/);
  });
});
