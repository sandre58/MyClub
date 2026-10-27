import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { joinCompositionPlaceAssignmentSections } from './structureOperationalStripLayout';

describe('joinCompositionPlaceAssignmentSections', () => {
  it('inserts a horizontal GradientSeparator between places and draw', () => {
    const sections = joinCompositionPlaceAssignmentSections(
      <div data-testid="places">Places</div>,
      <div data-testid="draw">Draw</div>,
      { showPlaces: true, showDraw: true },
    );
    const { getByTestId, container } = render(<>{sections}</>);
    expect(getByTestId('places')).toBeTruthy();
    expect(getByTestId('draw')).toBeTruthy();
    expect(
      container.querySelector(
        '.ds-gradient-separator--horizontal.structure-operational-strip__composition-tirage-separator',
      ),
    ).toBeTruthy();
  });

  it('omits separator when only draw is shown', () => {
    const sections = joinCompositionPlaceAssignmentSections(
      null,
      <div data-testid="draw">Draw</div>,
      { showPlaces: false, showDraw: true },
    );
    const { getByTestId, container } = render(<>{sections}</>);
    expect(getByTestId('draw')).toBeTruthy();
    expect(container.querySelector('.ds-gradient-separator')).toBeNull();
  });
});
