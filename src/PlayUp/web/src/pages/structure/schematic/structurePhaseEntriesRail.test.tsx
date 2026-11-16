import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { RootEntriesRail } from './structurePhaseEntriesRail';
import { ResolutionReleaseContext } from '../resolution/structureResolutionReleaseContext';
import { CascadeReplaceContext } from '../resolution/structureCascadeReplaceContext';

const sourcePathId = 'Q|poules|1';

const baseStage = {
  stageId: 'stage-1',
  name: 'Tableau',
  status: 'Running',
  teamCount: 4,
  matchCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: true,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
  formatKind: 'Cup',
  affectationEntryCount: 1,
  compositionEntryCount: 1,
  compositionEntryIds: ['e1'],
} as StructureStageHubSummary;

function slotOccupant(
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

function groupOccupant(
  overrides: Partial<StructureResolutionOccupant> = {},
): StructureResolutionOccupant {
  return {
    target: 'Group',
    entryId: 'e1',
    provenanceKind: 'Progression',
    sourcePathId: 'P|barrage|winner',
    groupId: 'g1',
    state: 'Indetermine',
    indeterminacyCause: 'SelectionVide',
    releasable: true,
    cascadeReplaceable: false,
    ...overrides,
  };
}

function populationFor(sourcePathIdValue: string): StructureResolutionOccupant {
  return {
    target: 'Population',
    entryId: 'e1',
    provenanceKind: sourcePathIdValue.startsWith('P')
      ? 'Progression'
      : 'Qualification',
    sourcePathId: sourcePathIdValue,
    state: 'Indetermine',
    releasable: false,
    cascadeReplaceable: false,
  };
}

function renderRail(
  occupants: StructureResolutionOccupant[],
  requestRelease = vi.fn(),
) {
  const stage = {
    ...baseStage,
    resolutionOccupants: occupants,
  } as StructureStageHubSummary;
  return renderWithI18n(
    <MemoryRouter initialEntries={['/competitions/comp/structure']}>
      <ResolutionReleaseContext.Provider value={{ requestRelease }}>
        <RootEntriesRail
          stage={stage}
          entries={[{ entryId: 'e1', displayName: 'Nord', status: 'Active' }]}
          placedEntryIds={new Set(['e1'])}
          formatKind="Cup"
          schematicReady={true}
          compositionTileMode="exploitation"
          variant="composition-band"
        />
      </ResolutionReleaseContext.Provider>
    </MemoryRouter>,
  );
}

describe('RootEntriesRail freshness badge', () => {
  it('releases via Slot when the team is placed on a cup slot', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    renderRail([populationFor(sourcePathId), slotOccupant()], requestRelease);

    await user.click(
      screen.getByRole('button', {
        name: /Non déterminé.*Libérer la place/,
      }),
    );
    expect(requestRelease).toHaveBeenCalledWith(
      expect.objectContaining({ target: 'Slot', slotKey: 'Tete' }),
    );
  });

  it('replaces via Slot when the team is placed and replayable on a cup slot', async () => {
    const requestReplace = vi.fn();
    const user = userEvent.setup();
    const stage = {
      ...baseStage,
      resolutionOccupants: [
        populationFor(sourcePathId),
        {
          ...slotOccupant(),
          state: 'Perime',
          stalenessCause: 'ResultatAmont',
          cascadeReplaceable: true,
          releasable: false,
        },
      ],
    } as StructureStageHubSummary;
    renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <CascadeReplaceContext.Provider value={{ requestReplace }}>
          <RootEntriesRail
            stage={stage}
            entries={[{ entryId: 'e1', displayName: 'Nord', status: 'Active' }]}
            placedEntryIds={new Set(['e1'])}
            formatKind="Cup"
            schematicReady={true}
            compositionTileMode="exploitation"
            variant="composition-band"
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

  it('renders freshness action on a direct team line', () => {
    const { container } = renderRail(
      [populationFor(sourcePathId), slotOccupant()],
      vi.fn(),
    );

    expect(screen.getByText('Nord')).toBeInTheDocument();
    expect(
      container.querySelector('.structure-freshness-badge__action'),
    ).toBeTruthy();
  });

  it('releases via Group when the team is placed in poules', async () => {
    const requestRelease = vi.fn();
    const user = userEvent.setup();
    const progressionPath = 'P|barrage|winner';
    renderRail(
      [
        populationFor(progressionPath),
        groupOccupant({ sourcePathId: progressionPath }),
      ],
      requestRelease,
    );

    await user.click(
      screen.getByRole('button', {
        name: /Non déterminé.*Libérer la place/,
      }),
    );
    expect(requestRelease).toHaveBeenCalledWith(
      expect.objectContaining({ target: 'Group', groupId: 'g1' }),
    );
  });
});

describe('RootEntriesRail équipes band inbound qual format', () => {
  it('renders direct team caption meta on the second line', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <RootEntriesRail
          variant="composition-band"
          stage={baseStage}
          entries={[{ entryId: 'e2', displayName: 'Sud', status: 'Active' }]}
          formatKind="Groups"
          schematicReady
          compositionTileMode="exploitation"
          placedEntryIds={new Set(['e2'])}
          entryPlaceProjections={
            new Map([
              [
                'e2',
                {
                  labels: ['Groupe A'],
                  details: ['Groupe A, place 2'],
                  kinds: ['group'],
                },
              ],
            ])
          }
          compositionTeamRows={[
            {
              key: 'direct-e2',
              kind: 'direct',
              entry: {
                entryId: 'e2',
                displayName: 'Sud',
                status: 'Active',
              },
              provenance: {
                full: '1er de Poule A',
                badge: '1er',
                chipTone: 'accent',
              },
            },
          ]}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Sud')).toBeInTheDocument();
    expect(
      container.querySelector('.structure-entries__meta--caption'),
    ).not.toBeNull();
    expect(screen.getByText('1er')).toBeInTheDocument();
    expect(screen.getByText('Groupe A')).toBeInTheDocument();
    const hint = container.querySelector('.structure-entries__placed-hint');
    expect(hint).toHaveAttribute(
      'aria-label',
      'Cette équipe est déjà placée : Groupe A, place 2',
    );
    expect(hint).not.toHaveAttribute('title');
    expect(hint).toHaveTextContent('Groupe A');
  });

  it('renders group place with Placée en tooltip on équipes direct team caption', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <RootEntriesRail
          variant="composition-band"
          stage={{ ...baseStage, formatKind: 'Groups' }}
          entries={[{ entryId: 'e2', displayName: 'Sud', status: 'Active' }]}
          formatKind="Groups"
          schematicReady
          compositionTileMode="exploitation"
          placedEntryIds={new Set(['e2'])}
          entryPlaceProjections={
            new Map([
              [
                'e2',
                {
                  labels: ['Groupe B'],
                  details: ['Groupe B, place 1'],
                  kinds: ['group'],
                },
              ],
            ])
          }
          compositionTeamRows={[
            {
              key: 'direct-e2',
              kind: 'direct',
              entry: {
                entryId: 'e2',
                displayName: 'Sud',
                status: 'Active',
              },
              provenance: null,
            },
          ]}
        />
      </MemoryRouter>,
    );
    const hint = container.querySelector('.structure-entries__placed-hint');
    expect(hint).toHaveTextContent('Groupe B');
    expect(hint).toHaveAttribute(
      'aria-label',
      'Cette équipe est déjà placée : Groupe B, place 1',
    );
    expect(hint).not.toHaveAttribute('title');
  });

  it('renders resolved inbound qual line with crest, place, freshness and action', () => {
    const requestRelease = vi.fn();
    const stage = {
      ...baseStage,
      formatKind: 'Cup',
      resolutionOccupants: [
        populationFor('Q|poules|1'),
        slotOccupant({ entryId: 'e1', slotKey: 'Tete' }),
      ],
    } as StructureStageHubSummary;
    const { container } = renderWithI18n(
      <MemoryRouter initialEntries={['/competitions/comp/structure']}>
        <ResolutionReleaseContext.Provider value={{ requestRelease }}>
          <RootEntriesRail
            variant="composition-band"
            stage={stage}
            entries={[{ entryId: 'e1', displayName: 'Nord', status: 'Active' }]}
            formatKind="Cup"
            schematicReady
            compositionTileMode="exploitation"
            placedEntryIds={new Set(['e1'])}
            entryPlaceProjections={
              new Map([
                [
                  'e1',
                  {
                    labels: ['Tête'],
                    details: ['Tête de tableau'],
                    kinds: ['cup'],
                  },
                ],
              ])
            }
            compositionTeamRows={[
              {
                key: 'inbound-1',
                kind: 'inbound',
                rule: {
                  key: 'q-src-1',
                  volume: 1,
                  badge: '1er',
                  badgeTone: 'accent',
                  context: 'Poule A',
                  family: 'place',
                  sortPrimary: 1,
                  sortSecondary: '1',
                  qualPathOrder: 1,
                },
                seat: {
                  resolved: true,
                  entryId: 'e1',
                  teamName: 'Nord',
                  placed: true,
                },
                entry: {
                  entryId: 'e1',
                  displayName: 'Nord',
                  status: 'Active',
                },
              },
            ]}
          />
        </ResolutionReleaseContext.Provider>
      </MemoryRouter>,
    );
    expect(screen.getByText('1er')).toBeInTheDocument();
    expect(screen.getByText('Nord')).toBeInTheDocument();
    expect(screen.getByText('Tête')).toBeInTheDocument();
    expect(screen.getByText('Non déterminé')).toBeInTheDocument();
    expect(
      container.querySelector('.structure-freshness-badge__action'),
    ).toBeTruthy();
    expect(container.querySelector('.team-crest')).toBeTruthy();
  });

  it('renders qual rule line and unresolved caption hint like Qualifiés', () => {
    const { container } = renderWithI18n(
      <MemoryRouter>
        <RootEntriesRail
          variant="composition-band"
          stage={baseStage}
          entries={[]}
          compositionTeamRows={[
            {
              key: 'unresolved-1',
              kind: 'inbound',
              rule: {
                key: 'q-src-1',
                volume: 1,
                badge: '1er',
                badgeTone: 'accent',
                context: 'Poule A',
                family: 'place',
                sortPrimary: 1,
                sortSecondary: '1',
                qualPathOrder: 1,
              },
              seat: {
                resolved: false,
                entryId: null,
                teamName: null,
                placed: false,
              },
            },
          ]}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Non résolue')).toBeInTheDocument();
    expect(screen.getByText('1er')).toBeInTheDocument();
    expect(
      container.querySelector('.structure-entries__team--inbound'),
    ).not.toBeNull();
    expect(
      container.querySelectorAll('.structure-composition-qualifiers__hint-sep'),
    ).toHaveLength(0);
  });
});
