import { describe, expect, it } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import {
  buildStructureResolutionOccupantIndex,
  hasStaleTirageResolutionOccupant,
  lookupGroupResolutionOccupant,
  lookupListedResolutionOccupant,
  lookupPopulationResolutionOccupant,
  lookupSlotResolutionOccupant,
  isDefinitivelyUnconfirmed,
  resolutionOccupantNeedsBadge,
} from './structureResolutionOccupants';

const slotOccupant: StructureResolutionOccupant = {
  target: 'Slot',
  entryId: 'e1',
  provenanceKind: 'Qualification',
  sourcePathId: 'p1',
  state: 'Perime',
  slotKey: 'A',
  groupId: null,
  stalenessCause: 'ResultatAmont',
  cascadeReplaceable: false,
};

describe('structureResolutionOccupants', () => {
  it('returns null index when occupants are absent', () => {
    expect(buildStructureResolutionOccupantIndex(null)).toBeNull();
    expect(buildStructureResolutionOccupantIndex([])).toBeNull();
  });

  it('indexes slot, group, and population occupants', () => {
    const index = buildStructureResolutionOccupantIndex([
      slotOccupant,
      {
        ...slotOccupant,
        target: 'Group',
        groupId: 'g1',
        slotKey: null,
        entryId: 'e2',
        state: 'Indetermine',
        stalenessCause: null,
      },
      {
        ...slotOccupant,
        target: 'Population',
        entryId: 'e3',
        slotKey: null,
        groupId: null,
        state: 'NonVerifiee',
        stalenessCause: null,
      },
    ]);
    expect(lookupSlotResolutionOccupant(index, 'A')?.state).toBe('Perime');
    expect(lookupGroupResolutionOccupant(index, 'g1', 'e2')?.state).toBe(
      'Indetermine',
    );
    expect(lookupPopulationResolutionOccupant(index, 'e3')?.state).toBe(
      'NonVerifiee',
    );
  });

  it('flags badge-worthy states only', () => {
    expect(
      resolutionOccupantNeedsBadge({ ...slotOccupant, state: 'AJour' }),
    ).toBe(false);
    expect(resolutionOccupantNeedsBadge(slotOccupant)).toBe(true);
  });

  it('hides the badge for an unverified occupant', () => {
    expect(
      resolutionOccupantNeedsBadge({
        ...slotOccupant,
        state: 'NonVerifiee',
        provenanceKind: 'NonVerifiee',
      }),
    ).toBe(false);
    expect(
      resolutionOccupantNeedsBadge({ ...slotOccupant, state: 'Indetermine' }),
    ).toBe(true);
    expect(isDefinitivelyUnconfirmed(slotOccupant)).toBe(false);
    expect(
      isDefinitivelyUnconfirmed({
        ...slotOccupant,
        state: 'Indetermine',
        definitive: true,
      }),
    ).toBe(true);
    expect(
      isDefinitivelyUnconfirmed({
        ...slotOccupant,
        state: 'Perime',
        definitive: true,
      }),
    ).toBe(false);
  });

  it('lookupListedResolutionOccupant picks Population when not placed', () => {
    const index = buildStructureResolutionOccupantIndex([
      {
        ...slotOccupant,
        target: 'Population',
        entryId: 'e-pop',
        slotKey: null,
        groupId: null,
        state: 'Indetermine',
        releasable: true,
      },
    ]);
    const result = lookupListedResolutionOccupant(index, 'e-pop');
    expect(result.multiplePlacements).toBe(false);
    expect(result.occupant?.target).toBe('Population');
  });

  it('lookupListedResolutionOccupant picks Slot when placed with matching provenance', () => {
    const sourcePathId = 'path-qual';
    const index = buildStructureResolutionOccupantIndex([
      {
        ...slotOccupant,
        target: 'Population',
        entryId: 'e1',
        sourcePathId,
        slotKey: null,
        groupId: null,
        state: 'Indetermine',
        releasable: false,
      },
      {
        ...slotOccupant,
        target: 'Slot',
        entryId: 'e1',
        sourcePathId,
        slotKey: 'A',
        state: 'Indetermine',
        releasable: true,
      },
    ]);
    const result = lookupListedResolutionOccupant(index, 'e1');
    expect(result.multiplePlacements).toBe(false);
    expect(result.occupant?.target).toBe('Slot');
    expect(result.occupant?.slotKey).toBe('A');
  });

  it('lookupListedResolutionOccupant picks Group when placed in poules', () => {
    const sourcePathId = 'path-prog';
    const index = buildStructureResolutionOccupantIndex([
      {
        ...slotOccupant,
        target: 'Population',
        entryId: 'e1',
        provenanceKind: 'Progression',
        sourcePathId,
        slotKey: null,
        groupId: null,
        state: 'Indetermine',
      },
      {
        ...slotOccupant,
        target: 'Group',
        entryId: 'e1',
        provenanceKind: 'Progression',
        sourcePathId,
        groupId: 'g1',
        slotKey: null,
        state: 'Indetermine',
        releasable: true,
      },
    ]);
    const result = lookupListedResolutionOccupant(index, 'e1');
    expect(result.occupant?.target).toBe('Group');
  });

  it('lookupListedResolutionOccupant keeps Population for Tirage placement', () => {
    const sourcePathId = 'path-qual';
    const index = buildStructureResolutionOccupantIndex([
      {
        ...slotOccupant,
        target: 'Population',
        entryId: 'e1',
        sourcePathId,
        slotKey: null,
        groupId: null,
        state: 'Indetermine',
      },
      {
        ...slotOccupant,
        target: 'Slot',
        entryId: 'e1',
        provenanceKind: 'Tirage',
        sourcePathId: 'draw-path',
        slotKey: 'A',
        state: 'AJour',
      },
    ]);
    const result = lookupListedResolutionOccupant(index, 'e1');
    expect(result.occupant?.target).toBe('Population');
    expect(result.multiplePlacements).toBe(false);
  });

  it('lookupListedResolutionOccupant keeps Population when sourcePathId differs', () => {
    const index = buildStructureResolutionOccupantIndex([
      {
        ...slotOccupant,
        target: 'Population',
        entryId: 'e1',
        sourcePathId: 'pop-path',
        slotKey: null,
        groupId: null,
        state: 'Indetermine',
      },
      {
        ...slotOccupant,
        target: 'Slot',
        entryId: 'e1',
        sourcePathId: 'other-path',
        slotKey: 'A',
        state: 'Indetermine',
        releasable: true,
      },
    ]);
    const result = lookupListedResolutionOccupant(index, 'e1');
    expect(result.occupant?.target).toBe('Population');
  });

  it('lookupListedResolutionOccupant flags multiple placements', () => {
    const sourcePathId = 'path-qual';
    const index = buildStructureResolutionOccupantIndex([
      {
        ...slotOccupant,
        target: 'Population',
        entryId: 'e1',
        sourcePathId,
        slotKey: null,
        groupId: null,
        state: 'Indetermine',
      },
      {
        ...slotOccupant,
        target: 'Slot',
        entryId: 'e1',
        sourcePathId,
        slotKey: 'A',
        state: 'Indetermine',
      },
      {
        ...slotOccupant,
        target: 'Slot',
        entryId: 'e1',
        sourcePathId,
        slotKey: 'B',
        state: 'Indetermine',
      },
    ]);
    const result = lookupListedResolutionOccupant(index, 'e1');
    expect(result.multiplePlacements).toBe(true);
    expect(result.occupant?.target).toBe('Population');
  });

  it('detects stale tirage occupants', () => {
    expect(
      hasStaleTirageResolutionOccupant([
        { ...slotOccupant, provenanceKind: 'Tirage', state: 'Perime' },
      ]),
    ).toBe(true);
    expect(
      hasStaleTirageResolutionOccupant([
        { ...slotOccupant, provenanceKind: 'Tirage', state: 'AJour' },
      ]),
    ).toBe(false);
  });
});
