import { describe, expect, it } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import {
  buildStructureResolutionOccupantIndex,
  hasStaleTirageResolutionOccupant,
  lookupGroupResolutionOccupant,
  lookupPopulationResolutionOccupant,
  lookupSlotResolutionOccupant,
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
