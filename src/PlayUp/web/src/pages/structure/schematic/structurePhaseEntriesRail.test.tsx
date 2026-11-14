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
  entryProvenanceByEntryId?: Map<
    string,
    { full: string; badge: string; chipTone?: 'win' | 'loss' | 'soft' }
  >,
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
          entryProvenanceByEntryId={entryProvenanceByEntryId}
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
        name: /Non confirmée.*Libérer la place/,
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

  it('keeps gap between provenance chip and freshness badge on the rail', () => {
    const { container } = renderRail(
      [populationFor(sourcePathId), slotOccupant()],
      vi.fn(),
      new Map([
        [
          'e1',
          {
            full: 'Barrage — Vainqueur P1',
            badge: 'Vainqueur P1',
            chipTone: 'win',
          },
        ],
      ]),
    );

    const action = container.querySelector(
      '.structure-freshness-badge__action',
    );
    expect(action).toBeTruthy();
    expect(getComputedStyle(action!).marginInlineStart).toMatch(/^0(px)?$/);

    const trailing = container.querySelector(
      '.structure-entries__cell-trailing',
    );
    expect(
      trailing?.querySelector('.structure-entries__team-provenance-badge'),
    ).toBeTruthy();
    expect(trailing?.querySelector('.structure-freshness-badge')).toBeTruthy();
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
        name: /Non confirmée.*Libérer la place/,
      }),
    );
    expect(requestRelease).toHaveBeenCalledWith(
      expect.objectContaining({ target: 'Group', groupId: 'g1' }),
    );
  });
});
