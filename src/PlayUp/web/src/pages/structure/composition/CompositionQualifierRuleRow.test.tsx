import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { CompositionQualifierRuleRow } from './CompositionQualifierRuleRow';
import { ResolutionReleaseContext } from '../resolution/structureResolutionReleaseContext';

function slotOccupant(
  overrides: Partial<StructureResolutionOccupant> = {},
): StructureResolutionOccupant {
  return {
    target: 'Slot',
    entryId: 'e1',
    provenanceKind: 'Qualification',
    sourcePathId: 'Q|p|1',
    state: 'Indetermine',
    slotKey: 'Tete',
    indeterminacyCause: 'SelectionVide',
    releasable: true,
    cascadeReplaceable: false,
    ...overrides,
  };
}

describe('CompositionQualifierRuleRow layout (iter6 option A)', () => {
  it('places freshness action in the rule grid, outside the outcome hint', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    const { container } = renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <ResolutionReleaseContext.Provider value={{ requestRelease }}>
          <CompositionQualifierRuleRow
            rule={{
              key: 'q1',
              volume: 1,
              badge: '1er',
              badgeTone: 'accent',
              context: 'Poule A',
              family: 'place',
              sortPrimary: 1,
              sortSecondary: '1',
              qualPathOrder: 1,
            }}
            seat={{
              resolved: true,
              entryId: 'e1',
              teamName: 'Nord',
              placed: true,
            }}
            entry={{ entryId: 'e1', displayName: 'Nord', status: 'Active' }}
            inboundRule={undefined}
            schematic={undefined}
            entryPlaceProjections={
              new Map([['e1', { labels: ['Tête'], details: [null] }]])
            }
            listedResolution={{
              occupant: slotOccupant(),
              multiplePlacements: false,
            }}
            onRelease={requestRelease}
          />
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );

    const grid = container.querySelector(
      '.structure-composition-qualifiers__rule-grid',
    );
    const outcome = container.querySelector(
      '.structure-composition-qualifiers__outcome',
    );
    const gridAction = grid?.querySelector(
      ':scope > .structure-entries__action',
    );
    expect(grid).not.toBeNull();
    expect(outcome).not.toBeNull();
    expect(outcome!.querySelector('.structure-entries__action')).toBeNull();
    expect(gridAction).not.toBeNull();
    expect(screen.getByText('Nord')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Libérer/ }));
    expect(requestRelease).toHaveBeenCalled();
  });
});
