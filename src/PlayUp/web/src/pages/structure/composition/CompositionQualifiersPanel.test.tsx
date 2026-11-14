import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { CompositionQualifiersPanel } from './CompositionQualifiersPanel';
import type { CompositionQualifierFeedGroup } from './structureCompositionQualifierFeeds';
import { ResolutionReleaseContext } from '../resolution/structureResolutionReleaseContext';

const sourcePathId = 'Q|poules|1';

function releasableSlotOccupant(
  overrides: Partial<StructureResolutionOccupant> = {},
): StructureResolutionOccupant {
  return {
    target: 'Slot',
    entryId: 'e1',
    provenanceKind: 'Qualification',
    sourcePathId,
    state: 'Indetermine',
    slotKey: 'Tete',
    indeterminacyCause: 'SelectionVide',
    releasable: true,
    cascadeReplaceable: false,
    ...overrides,
  };
}

function populationOccupant(
  overrides: Partial<StructureResolutionOccupant> = {},
): StructureResolutionOccupant {
  return {
    target: 'Population',
    entryId: 'e1',
    provenanceKind: 'Qualification',
    sourcePathId,
    state: 'Indetermine',
    indeterminacyCause: 'SelectionVide',
    releasable: false,
    cascadeReplaceable: false,
    ...overrides,
  };
}

function qualifierGroup(placed: boolean): CompositionQualifierFeedGroup {
  return {
    peerId: 'src',
    peerName: 'Poules',
    peerOrder: 0,
    volume: 1,
    rules: [
      {
        key: 'q-src-1',
        volume: 1,
        badge: '1er',
        badgeTone: 'accent',
        context: 'Poule A',
        family: 'place',
        sortPrimary: 1,
        sortSecondary: '1',
        qualPathOrder: 1,
        hubSeats: [
          {
            entryId: 'e1',
            teamName: 'Nord',
            placed,
            resolved: true,
          },
        ],
      },
    ],
  };
}

function renderPanel(
  resolutionOccupants: StructureResolutionOccupant[],
  requestRelease = vi.fn(),
) {
  return renderWithI18n(
    <MemoryRouter initialEntries={['/competitions/comp/structure']}>
      <ResolutionReleaseContext.Provider value={{ requestRelease }}>
        <CompositionQualifiersPanel
          groups={[qualifierGroup(true)]}
          inboundComposition={undefined}
          entries={[
            { entryId: 'e1', displayName: 'Nord', status: 'Active' },
          ]}
          formatKind="Cup"
          entryPlaceProjections={new Map()}
          resolutionOccupants={resolutionOccupants}
        />
      </ResolutionReleaseContext.Provider>
    </MemoryRouter>,
  );
}

describe('CompositionQualifiersPanel freshness badge', () => {
  it('uses the place occupant when the team is placed and releasable on Slot', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    renderPanel(
      [populationOccupant(), releasableSlotOccupant()],
      requestRelease,
    );

    await user.click(
      screen.getByRole('button', {
        name: /Non confirmée.*Libérer la place/,
      }),
    );
    expect(requestRelease).toHaveBeenCalledWith(
      expect.objectContaining({ target: 'Slot', slotKey: 'Tete' }),
    );
  });

  it('keeps Population when the team is not placed', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <ResolutionReleaseContext.Provider value={{ requestRelease }}>
          <CompositionQualifiersPanel
            groups={[qualifierGroup(false)]}
            inboundComposition={undefined}
            entries={[
              { entryId: 'e1', displayName: 'Nord', status: 'Active' },
            ]}
            formatKind="Cup"
            entryPlaceProjections={new Map()}
            resolutionOccupants={[populationOccupant({ releasable: true })]}
          />
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );

    await user.click(
      screen.getByRole('button', {
        name: /Non confirmée.*Libérer la place/,
      }),
    );
    expect(requestRelease).toHaveBeenCalledWith(
      expect.objectContaining({ target: 'Population' }),
    );
  });

  it('shows an inert badge without release when several places match', () => {
    const requestRelease = vi.fn();
    renderPanel(
      [
        populationOccupant(),
        releasableSlotOccupant({ slotKey: 'A' }),
        releasableSlotOccupant({ slotKey: 'B' }),
      ],
      requestRelease,
    );

    expect(screen.queryByRole('button', { name: /Libérer/ })).toBeNull();
    expect(
      screen.getByLabelText(/agir depuis la schématique/i),
    ).toBeInTheDocument();
  });
});