import { describe, expect, it } from 'vitest';
import type { SchematicCase, StructureEntry } from '../../../types';
import {
  groupSeatCounter,
  listAvailableGroupEntries,
  resolveGroupSeatInteraction,
} from './manualGroupPlacementUi';

const actions = ['AssignEntryToGroup', 'ClearEntryFromGroup'];

function place(partial: Partial<SchematicCase> = {}): SchematicCase {
  return {
    formPosition: {
      kind: 'GroupPlace',
      groupId: 'g-a',
      groupName: 'A',
      index: 1,
    },
    entry: null,
    assignment: null,
    seatMode: 'Editable',
    canClear: false,
    ...partial,
  };
}

describe('resolveGroupSeatInteraction', () => {
  it('places an empty editable seat when assign is allowed', () => {
    expect(resolveGroupSeatInteraction(place(), { actions })).toEqual({
      kind: 'place',
      reasonKey: 'placeInvite',
    });
  });

  it('locks an empty seat that has a match, a reservation, or a pending draw', () => {
    expect(
      resolveGroupSeatInteraction(place({ seatMode: 'MatchLocked' }), {
        actions,
      }),
    ).toEqual({ kind: 'locked', reasonKey: 'matchLocked' });
    expect(
      resolveGroupSeatInteraction(place({ seatMode: 'Reserved' }), { actions }),
    ).toEqual({ kind: 'locked', reasonKey: 'reserved' });
    expect(
      resolveGroupSeatInteraction(place({ seatMode: 'DrawPending' }), {
        actions,
      }),
    ).toEqual({ kind: 'locked', reasonKey: 'drawPending' });
  });

  it('removes an occupied editable seat that can be cleared, including excess and outside composition', () => {
    const occupied = place({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    expect(resolveGroupSeatInteraction(occupied, { actions })).toEqual({
      kind: 'remove',
      reasonKey: 'removeInvite',
    });
    expect(
      resolveGroupSeatInteraction(
        place({ ...occupied, excess: true, outsideComposition: true }),
        { actions },
      ),
    ).toEqual({ kind: 'remove', reasonKey: 'removeInvite' });
  });

  it('locks occupied seats that cannot be cleared', () => {
    const occupied = {
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: false as const,
    };
    expect(
      resolveGroupSeatInteraction(
        place({ ...occupied, seatMode: 'Editable' }),
        {
          actions,
        },
      ),
    ).toEqual({ kind: 'locked', reasonKey: 'clearLocked' });
    expect(
      resolveGroupSeatInteraction(
        place({ ...occupied, seatMode: 'QualProg' }),
        {
          actions,
        },
      ),
    ).toEqual({ kind: 'locked', reasonKey: 'qualProg' });
    expect(
      resolveGroupSeatInteraction(place({ ...occupied, seatMode: 'Draw' }), {
        actions,
      }),
    ).toEqual({ kind: 'locked', reasonKey: 'draw' });
    expect(
      resolveGroupSeatInteraction(
        place({ ...occupied, seatMode: 'DrawPending' }),
        { actions },
      ),
    ).toEqual({ kind: 'locked', reasonKey: 'drawPending' });
  });

  it('offers nothing without the group actions or without a seat mode', () => {
    const editable = place({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    expect(resolveGroupSeatInteraction(editable, { actions: [] })).toEqual({
      kind: 'none',
    });
    expect(
      resolveGroupSeatInteraction(place(), { actions: undefined }),
    ).toEqual({ kind: 'none' });
    expect(
      resolveGroupSeatInteraction(place({ seatMode: null }), { actions }),
    ).toEqual({ kind: 'none' });
    expect(
      resolveGroupSeatInteraction(
        place({
          formPosition: { kind: 'CupSlot', slotKey: 'A' },
          seatMode: 'Editable',
        }),
        { actions },
      ),
    ).toEqual({ kind: 'none' });
  });
});

describe('listAvailableGroupEntries', () => {
  const entries: StructureEntry[] = [
    { entryId: 'e1', displayName: 'Alpha', status: 'Active' },
    { entryId: 'e2', displayName: 'Bravo', status: 'Active' },
    { entryId: 'e3', displayName: 'Charlie', status: 'Active' },
  ];

  it('excludes teams already placed in any group and keeps composition order without pots', () => {
    const cases = [
      place({
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g-b',
          groupName: 'B',
          index: 1,
        },
        entry: { entryId: 'e2', displayName: 'Bravo' },
      }),
    ];
    expect(
      listAvailableGroupEntries(
        { compositionEntryIds: ['e1', 'e2', 'e3'], compositionEntryPots: null },
        cases,
        entries,
      ).map((row) => row.entryId),
    ).toEqual(['e1', 'e3']);
    expect(
      listAvailableGroupEntries(
        { compositionEntryIds: ['e1', 'e3'] },
        [],
        entries,
      ).every((row) => row.pot == null),
    ).toBe(true);
  });

  it('sorts by pot and then composition order', () => {
    const listed = listAvailableGroupEntries(
      {
        compositionEntryIds: ['e1', 'e2', 'e3'],
        compositionEntryPots: [
          { entryId: 'e1', pot: 2 },
          { entryId: 'e2', pot: 1 },
          { entryId: 'e3', pot: 1 },
        ],
      },
      [],
      entries,
    );
    expect(listed.map((row) => row.entryId)).toEqual(['e2', 'e3', 'e1']);
    expect(listed.map((row) => row.pot)).toEqual([1, 1, 2]);
  });
});

describe('groupSeatCounter', () => {
  it('counts every projected member against PlacesPerGroup', () => {
    const cases = [
      place({
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g-a',
          groupName: 'A',
          index: 1,
        },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      }),
      place({
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g-a',
          groupName: 'A',
          index: 2,
        },
        entry: { entryId: 'e2', displayName: 'Bravo' },
      }),
      place({
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g-a',
          groupName: 'A',
          index: 3,
        },
        entry: { entryId: 'e3', displayName: 'Charlie' },
      }),
    ];
    expect(groupSeatCounter({ id: 'g-a' }, cases, 4)).toEqual({
      occupied: 3,
      capacity: 4,
    });

    const excess = [
      ...cases,
      place({
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g-a',
          groupName: 'A',
          index: 4,
        },
        entry: { entryId: 'e4', displayName: 'Delta' },
      }),
      place({
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g-a',
          groupName: 'A',
          index: 5,
        },
        entry: { entryId: 'e5', displayName: 'Echo' },
        excess: true,
        outsideComposition: true,
      }),
    ];
    expect(groupSeatCounter({ id: 'g-a' }, excess, 4)).toEqual({
      occupied: 5,
      capacity: 4,
    });
  });
});
