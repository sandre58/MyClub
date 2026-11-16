import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { CompositionQualifiersPanel } from './CompositionQualifiersPanel';
import type { CompositionQualifierFeedGroup } from './structureCompositionQualifierFeeds';
import { ResolutionReleaseContext } from '../resolution/structureResolutionReleaseContext';
import { CascadeReplaceContext } from '../resolution/structureCascadeReplaceContext';

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
  entryPlaceProjections: ReadonlyMap<
    string,
    { labels: readonly string[]; details: readonly (string | null)[] }
  > = new Map(),
) {
  return renderWithI18n(
    <MemoryRouter initialEntries={['/competitions/comp/structure']}>
      <ResolutionReleaseContext.Provider value={{ requestRelease }}>
        <CompositionQualifiersPanel
          groups={[qualifierGroup(true)]}
          inboundComposition={undefined}
          entries={[{ entryId: 'e1', displayName: 'Nord', status: 'Active' }]}
          formatKind="Cup"
          entryPlaceProjections={entryPlaceProjections}
          schematic={undefined}
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
        name: /Non déterminé.*Libérer la place/,
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
            entries={[{ entryId: 'e1', displayName: 'Nord', status: 'Active' }]}
            formatKind="Cup"
            entryPlaceProjections={new Map()}
            schematic={undefined}
            resolutionOccupants={[populationOccupant({ releasable: true })]}
          />
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );

    await user.click(
      screen.getByRole('button', {
        name: /Non déterminé.*Libérer la place/,
      }),
    );
    expect(requestRelease).toHaveBeenCalledWith(
      expect.objectContaining({ target: 'Population' }),
    );
  });

  it('uses the place occupant for replace on Slot when the team is placed', async () => {
    const requestReplace = vi.fn();
    const user = userEvent.setup();
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <CascadeReplaceContext.Provider value={{ requestReplace }}>
          <CompositionQualifiersPanel
            groups={[qualifierGroup(true)]}
            inboundComposition={undefined}
            entries={[{ entryId: 'e1', displayName: 'Nord', status: 'Active' }]}
            formatKind="Cup"
            entryPlaceProjections={new Map()}
            schematic={undefined}
            resolutionOccupants={[
              populationOccupant(),
              {
                ...releasableSlotOccupant(),
                state: 'Perime',
                stalenessCause: 'ResultatAmont',
                cascadeReplaceable: true,
                releasable: false,
              },
            ]}
          />
        </CascadeReplaceContext.Provider>
      </MemoryRouter>,
    );

    await user.click(
      screen.getByRole('button', {
        name: /Périmé.*remplacer/i,
      }),
    );
    expect(requestReplace).toHaveBeenCalledWith(
      expect.objectContaining({ target: 'Slot', slotKey: 'Tete' }),
    );
  });

  it('stacks the rule label before the team hint and spans the full width', () => {
    const { container } = renderPanel([
      populationOccupant(),
      releasableSlotOccupant(),
    ]);

    const line = container.querySelector(
      '.structure-composition-qualifiers__rule-line--after',
    );
    expect(line).not.toBeNull();
    const rulePrimary = line!.querySelector(
      '.structure-composition-qualifiers__rule-primary',
    );
    const outcome = line!.querySelector(
      '.structure-composition-qualifiers__outcome',
    );
    expect(rulePrimary).not.toBeNull();
    expect(outcome).not.toBeNull();
    expect(
      rulePrimary!.compareDocumentPosition(outcome!) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(
      line!.querySelector('.structure-composition-qualifiers__hint-name'),
    ).toHaveTextContent('Nord');
    expect(line!.querySelector('.team-crest--xs')).not.toBeNull();
  });

  it('exposes group place index in tooltip and aria on the placed hint', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <CompositionQualifiersPanel
          groups={[
            {
              ...qualifierGroup(true),
              rules: [
                {
                  ...qualifierGroup(true).rules[0],
                  hubSeats: [
                    {
                      entryId: 'e1',
                      teamName: 'Nord',
                      placed: true,
                      resolved: true,
                    },
                  ],
                },
              ],
            },
          ]}
          inboundComposition={undefined}
          entries={[{ entryId: 'e1', displayName: 'Nord', status: 'Active' }]}
          formatKind="Groups"
          entryPlaceProjections={
            new Map([
              [
                'e1',
                {
                  labels: ['Groupe A'],
                  details: ['Groupe A, place 2'],
                },
              ],
            ])
          }
          schematic={undefined}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Groupe A')).toBeInTheDocument();
    const hint = container.querySelector('.structure-entries__placed-hint');
    expect(hint).toHaveAttribute('title', 'Groupe A, place 2');
    expect(hint).toHaveAttribute('aria-label', 'Groupe A, place 2');
  });

  it('does not show place on unresolved unplaced qualifier seats', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <CompositionQualifiersPanel
          groups={[
            {
              peerId: 'src',
              peerName: 'Barrage',
              peerOrder: 0,
              volume: 1,
              rules: [
                {
                  key: 'p1',
                  volume: 1,
                  badge: 'Vainqueur',
                  badgeTone: 'win',
                  context: 'P1',
                  family: 'place',
                  sortPrimary: 1,
                  sortSecondary: '1',
                  hubSeats: [
                    {
                      resolved: false,
                      placed: false,
                      teamName: null,
                      entryId: null,
                    },
                  ],
                },
              ],
            },
          ]}
          inboundComposition={undefined}
          entries={[]}
          formatKind="Cup"
          entryPlaceProjections={new Map()}
          schematic={undefined}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Non résolue')).toBeInTheDocument();
    expect(
      container.querySelector('.structure-entries__placed-hint'),
    ).toBeNull();
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-sep'),
    ).toHaveLength(0);
  });

  it('shows unresolved seats as hourglass hint under the rule', () => {
    renderWithI18n(
      <MemoryRouter>
        <CompositionQualifiersPanel
          groups={[
            {
              peerId: 'src',
              peerName: 'Barrage',
              peerOrder: 0,
              volume: 1,
              rules: [
                {
                  key: 'p1',
                  volume: 1,
                  badge: 'Vainqueur',
                  badgeTone: 'win',
                  context: 'P1',
                  family: 'place',
                  sortPrimary: 1,
                  sortSecondary: '1',
                  hubSeats: [
                    {
                      resolved: false,
                      placed: false,
                      teamName: null,
                      entryId: null,
                    },
                  ],
                },
              ],
            },
          ]}
          inboundComposition={undefined}
          entries={[]}
          formatKind="Cup"
          entryPlaceProjections={new Map()}
          schematic={undefined}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Non résolue')).toBeInTheDocument();
    expect(screen.getByText('Vainqueur')).toBeInTheDocument();
  });

  it('shows placed chrome on unresolved seats when the hub marks them placed', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <CompositionQualifiersPanel
          groups={[
            {
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
                      resolved: false,
                      placed: true,
                      teamName: null,
                      entryId: null,
                    },
                  ],
                },
              ],
            },
          ]}
          inboundComposition={{
            configuredVolume: 1,
            resolvedCount: 0,
            rules: [
              {
                feedKey: 'q-src-1',
                sourceStageId: 'src',
                seats: [{ entryId: null, displayName: null, placed: true }],
                qualificationPath: {
                  order: 1,
                  selectionMode: 'Position' as const,
                  selectionValue: 1,
                  destinationStageId: 'dest',
                  destinationSlotKey: 'Tete',
                },
              },
            ],
          }}
          entries={[]}
          formatKind="Cup"
          entryPlaceProjections={new Map()}
          schematic={{
            stageId: 'dest',
            competitionId: 'c1',
            name: 'Tableau',
            status: 'Ready',
            formatKind: 'Cup',
            cases: [
              {
                formPosition: {
                  kind: 'CupSlot',
                  slotKey: 'Tete',
                  roundName: 'Finale',
                  pairOrdinal: null,
                  side: 'A',
                },
                assignment: null,
                entry: null,
              },
            ],
            connections: [],
          }}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Non résolue')).toBeInTheDocument();
    expect(screen.getByText('Tete')).toBeInTheDocument();
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-sep'),
    ).toHaveLength(0);
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-unit')
        .length,
    ).toBeGreaterThanOrEqual(2);
  });

  it('renders the placed reminder in the qualifier hint meta', () => {
    renderPanel(
      [populationOccupant(), releasableSlotOccupant()],
      vi.fn(),
      new Map([['e1', { labels: ['Tête'], details: [null] }]]),
    );
    expect(screen.getByText('Tête')).toBeInTheDocument();
    expect(screen.getByText('Non déterminé')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Non déterminé.*Libérer la place/ }),
    ).toBeInTheDocument();
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
    expect(screen.getByText('Non déterminé')).toBeInTheDocument();
  });
});
