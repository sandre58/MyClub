import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { StructurePhaseTeamLine } from './StructurePhaseTeamLine';
import { ResolutionReleaseContext } from '../resolution/structureResolutionReleaseContext';

function occupant(
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

describe('StructurePhaseTeamLine', () => {
  it('renders a normal team row without action', () => {
    renderWithI18n(
      <MemoryRouter>
        <ul>
          <li className="structure-entries__team structure-entries__team--line">
            <StructurePhaseTeamLine displayName="Sud" />
          </li>
        </ul>
      </MemoryRouter>,
    );
    expect(screen.getByText('Sud')).toBeInTheDocument();
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('shows forfait icon and freshness state with a single action button', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <ResolutionReleaseContext.Provider value={{ requestRelease }}>
          <ul>
            <li className="structure-entries__team structure-entries__team--line">
              <StructurePhaseTeamLine
                displayName="Centre"
                withdrawn
                ineligible
                provenanceMeta="1er · Poules"
                resolutionOccupant={occupant()}
                onRelease={requestRelease}
              />
            </li>
          </ul>
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );
    expect(screen.getByLabelText('Forfait')).toBeInTheDocument();
    expect(screen.getByText('Non déterminé')).toBeInTheDocument();
    expect(screen.getByText('1er · Poules')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Libérer/ }));
    expect(requestRelease).toHaveBeenCalled();
  });

  it('renders unresolved seat with hourglass and meta provenance', () => {
    renderWithI18n(
      <MemoryRouter>
        <ul>
          <li className="structure-entries__team structure-entries__team--line">
            <StructurePhaseTeamLine
              displayName=""
              pending
              pendingLabel="Non résolue"
              provenanceMeta="1er · Poules"
            />
          </li>
        </ul>
      </MemoryRouter>,
    );
    expect(screen.getByText('Non résolue')).toBeInTheDocument();
    expect(screen.getByText('1er · Poules')).toBeInTheDocument();
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('shows placed hint on the secondary line', () => {
    renderWithI18n(
      <MemoryRouter>
        <ul>
          <li className="structure-entries__team structure-entries__team--line">
            <StructurePhaseTeamLine
              displayName="Nord"
              placed
              placedPlaceLabel="Tête"
            />
          </li>
        </ul>
      </MemoryRouter>,
    );
    expect(screen.getByText('Tête')).toBeInTheDocument();
  });
});
