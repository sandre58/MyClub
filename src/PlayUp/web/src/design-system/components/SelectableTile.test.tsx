import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SelectableTile } from './SelectableTile';

const foundationsDir = join(
  dirname(fileURLToPath(import.meta.url)),
  '../foundations',
);

const selectableTileCss = readFileSync(
  join(foundationsDir, 'selectable-tile.css'),
  'utf8',
);

const focusCss = readFileSync(join(foundationsDir, 'focus.css'), 'utf8');

const designSystemIndexCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), '../index.css'),
  'utf8',
);

const GLOBAL_FOCUS_SELECTOR =
  '.ds-root :is(a, button, input, select, textarea, summary, [tabindex]):focus-visible';

/** Measured with @bramus/specificity (same as focus.css global rule). */
const GLOBAL_FOCUS_SPECIFICITY: readonly [number, number, number] = [0, 3, 0];

/** Minimum expected for `.ds-root .ds-selectable-tile .ds-selectable-tile__trigger:focus-visible`. */
const TILE_TRIGGER_FOCUS_SUPPRESS_SPECIFICITY: readonly [
  number,
  number,
  number,
] = [0, 4, 0];

function selectorForBoxShadowNone(css: string): string {
  const match = css.match(/([^{}\n]+)\{[^{}]*box-shadow:\s*none[^}]*\}/);
  if (!match) {
    throw new Error('Expected a rule setting box-shadow: none');
  }
  return match[1].trim();
}

function compareSpecificity(
  left: readonly [number, number, number],
  right: readonly [number, number, number],
): number {
  for (let i = 0; i < 3; i++) {
    if (left[i] !== right[i]) {
      return left[i] - right[i];
    }
  }
  return 0;
}

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

  it('wins the focus halo cascade over the global focus rule for tile triggers', () => {
    const triggerSuppressSelector = selectorForBoxShadowNone(selectableTileCss);
    expect(triggerSuppressSelector).toMatch(
      /\.ds-root \.ds-selectable-tile \.ds-selectable-tile__trigger:focus-visible$/,
    );
    expect(selectableTileCss).not.toMatch(
      /\.ds-root \.ds-selectable-tile__trigger:focus-visible\s*\{/,
    );

    expect(
      compareSpecificity(
        TILE_TRIGGER_FOCUS_SUPPRESS_SPECIFICITY,
        GLOBAL_FOCUS_SPECIFICITY,
      ),
    ).toBeGreaterThan(0);

    const selectableTileImport = designSystemIndexCss.indexOf(
      './foundations/selectable-tile.css',
    );
    const focusImport = designSystemIndexCss.indexOf('./foundations/focus.css');
    expect(selectableTileImport).toBeGreaterThan(-1);
    expect(focusImport).toBeGreaterThan(selectableTileImport);
    expect(focusCss).toMatch(
      new RegExp(GLOBAL_FOCUS_SELECTOR.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
    );
  });

  it('documents error-tone rest chrome, hover at rest, and destructive chrome when selected', () => {
    expect(selectableTileCss).toMatch(
      /\.ds-selectable-tile\[data-tone='error'\]\s*\{[\s\S]*color-mix\(in srgb, var\(--color-error\) 4%/,
    );
    expect(selectableTileCss).toMatch(
      /\.ds-selectable-tile\[data-tone='error'\]\[data-selected='true'\]\s*\{[\s\S]*border-color:\s*var\(--color-error\)/,
    );
    expect(selectableTileCss).not.toMatch(
      /\[data-tone='error'\]\[data-selected='true'\][\s\S]*inset 0 0 0 1px var\(--color-error\)/,
    );
    expect(selectableTileCss).toMatch(
      /\.ds-root \.ds-selectable-tile\[data-tone='error'\]\[data-selected='true'\]:has\(\.ds-selectable-tile__trigger:focus-visible\)\s*\{[\s\S]*box-shadow:\s*var\(--focus-halo\)/,
    );
    expect(selectableTileCss).toMatch(
      /\.ds-selectable-tile\[data-tone='error'\]:hover:not\(:disabled\):not\(\[aria-disabled='true'\]\)\s*\{[\s\S]*color-mix\(in srgb, var\(--color-error\) 6%/,
    );
    expect(selectableTileCss).not.toMatch(
      /\.ds-selectable-tile\[data-tone='error'\]\[data-selected='true'\]:hover/,
    );
    expect(selectableTileCss).not.toMatch(
      /\.ds-selectable-tile[\s\S]*opacity:\s*[\d.]/,
    );
    expect(selectableTileCss).not.toMatch(/\.ds-selectable-tile__body/);
    expect(selectableTileCss).toMatch(
      /\.ds-root \.ds-selectable-tile:has\(\.ds-selectable-tile__trigger:focus-visible\)/,
    );
  });
});
