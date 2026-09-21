import { describe, expect, it } from 'vitest';
import type { StageSchematic } from '../types';
import {
  areProgressionPlacesLabeled,
  countUnmappedPlaceSlots,
  emptyProgIntent,
  fillEmptyPlaceSlotKeys,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
  resizeDestinationSlotKeys,
  syncPlaceSlotKeys,
  toApiIntent,
} from './structureProgressionDraft';

describe('structureProgressionDraft', () => {
  it('gates Place labels without a Cup schematic', () => {
    expect(areProgressionPlacesLabeled(null)).toBe(false);
    expect(areProgressionPlacesLabeled(undefined)).toBe(false);
  });

  it('maps population API intents', () => {
    const draft = intentFromApi(
      {
        intentId: 'i1',
        order: 1,
        roundId: 'r1',
        roundName: 'QF',
        outcome: 'Winner',
        destinationStageId: 'peer',
        destinationSlotKey: null,
        expandedPathCount: 4,
      },
      'source',
    );
    expect(draft.targetKind).toBe('population');
    expect(draft.destinationSlotKeys).toEqual([]);
    expect(draft.expandedPathCount).toBe(4);
    expect(toApiIntent(draft, 1).destinationSlotKeys).toBeNull();
  });

  it('maps Place API intents with destinationSlotKeys (D1)', () => {
    const draft = intentFromApi(
      {
        intentId: 'i1',
        order: 1,
        roundId: 'r1',
        outcome: 'Loser',
        destinationStageId: 'other',
        destinationSlotKeys: ['slot-a', 'slot-b'],
        expandedPathCount: 2,
      },
      'source',
    );
    expect(draft.targetKind).toBe('place');
    expect(draft.destinationStageId).toBe('other');
    expect(draft.destinationSlotKeys).toEqual(['slot-a', 'slot-b']);
    expect(toApiIntent(draft, 1).destinationSlotKeys).toEqual([
      'slot-a',
      'slot-b',
    ]);
  });

  it('coerces legacy singular destinationSlotKey from API', () => {
    const draft = intentFromApi(
      {
        intentId: 'i1',
        order: 1,
        roundId: 'r1',
        outcome: 'Winner',
        destinationStageId: 'other',
        destinationSlotKey: 'slot-7',
        expandedPathCount: 1,
      },
      'source',
    );
    expect(draft.targetKind).toBe('place');
    expect(draft.destinationSlotKeys).toEqual(['slot-7']);
    expect(toApiIntent(draft, 1).destinationSlotKeys).toEqual(['slot-7']);
  });

  it('resizes destinationSlotKeys when Expand count changes', () => {
    const draft = emptyProgIntent('peer', 'place');
    draft.roundId = 'r1';
    draft.expandedPathCount = 3;
    draft.destinationSlotKeys = ['SF1-A', 'SF1-B'];
    const grown = syncPlaceSlotKeys(draft);
    expect(grown.destinationSlotKeys).toEqual(['SF1-A', 'SF1-B', '']);

    draft.expandedPathCount = 1;
    draft.destinationSlotKeys = ['SF1-A', 'SF1-B', 'SF1-C'];
    const shrunk = syncPlaceSlotKeys(draft);
    expect(shrunk.destinationSlotKeys).toEqual(['SF1-A']);
  });

  it('clears slot keys when switching to Population via sync', () => {
    const draft = emptyProgIntent('peer', 'place');
    draft.destinationSlotKeys = ['SF1-A'];
    draft.targetKind = 'population';
    expect(syncPlaceSlotKeys(draft).destinationSlotKeys).toEqual([]);
  });

  it('fillEmptyPlaceSlotKeys fills only empties in available order', () => {
    expect(
      fillEmptyPlaceSlotKeys(
        ['SF1-A', '', ''],
        ['SF1-A', 'SF1-B', 'SF1-C', 'SF1-D'],
      ),
    ).toEqual(['SF1-A', 'SF1-B', 'SF1-C']);
  });

  it('countUnmappedPlaceSlots counts empty Expand slots', () => {
    const a = emptyProgIntent('peer', 'place');
    a.roundId = 'r1';
    a.expandedPathCount = 2;
    a.destinationSlotKeys = ['SF1-A', ''];
    expect(countUnmappedPlaceSlots([a])).toBe(1);
  });

  it('blocks Place intents while labels are unavailable', () => {
    const draft = emptyProgIntent('source', 'place');
    draft.roundId = 'r1';
    draft.destinationStageId = 'source';
    draft.expandedPathCount = 1;
    draft.destinationSlotKeys = ['A'];
    expect(incompleteIntentReason(draft, [draft], false)).toBe(
      'PlaceUnavailable',
    );
    expect(isIntentComplete(draft, [draft], false)).toBe(false);
  });

  it('requires full Place mapping (MultiSlot)', () => {
    const draft = emptyProgIntent('peer', 'place');
    draft.roundId = 'r1';
    draft.expandedPathCount = 2;
    draft.destinationSlotKeys = ['SF1-A'];
    expect(incompleteIntentReason(syncPlaceSlotKeys(draft), [draft], true)).toBe(
      'MultiSlot',
    );
  });

  it('detects duplicate slots within one intent', () => {
    const draft = emptyProgIntent('peer', 'place');
    draft.roundId = 'r1';
    draft.expandedPathCount = 2;
    draft.destinationSlotKeys = ['SF1-A', 'SF1-A'];
    expect(incompleteIntentReason(draft, [draft], true)).toBe('DuplicateSlot');
  });

  it('detects duplicate round+outcome', () => {
    const a = emptyProgIntent('peer');
    a.roundId = 'r1';
    a.outcome = 'Winner';
    const b = emptyProgIntent('peer');
    b.roundId = 'r1';
    b.outcome = 'Winner';
    expect(incompleteIntentReason(a, [a, b], false)).toBe(
      'DuplicateRoundOutcome',
    );
  });

  it('allows multiple intents to the same Population', () => {
    const a = emptyProgIntent('peer');
    a.roundId = 'r1';
    a.outcome = 'Winner';
    const b = emptyProgIntent('peer');
    b.roundId = 'r2';
    b.outcome = 'Winner';
    expect(isIntentComplete(a, [a, b], false)).toBe(true);
    expect(isIntentComplete(b, [a, b], false)).toBe(true);
  });

  it('detects duplicate Place destinations across intents when labeled', () => {
    const a = emptyProgIntent('source', 'place');
    a.roundId = 'r1';
    a.expandedPathCount = 1;
    a.destinationSlotKeys = ['SF-A'];
    const b = emptyProgIntent('source', 'place');
    b.roundId = 'r2';
    b.expandedPathCount = 1;
    b.destinationSlotKeys = ['SF-A'];
    expect(incompleteIntentReason(a, [a, b], true)).toBe('DuplicatePlace');
  });

  it('resizeDestinationSlotKeys pads and truncates', () => {
    expect(resizeDestinationSlotKeys(['a'], 3)).toEqual(['a', '', '']);
    expect(resizeDestinationSlotKeys(['a', 'b', 'c'], 1)).toEqual(['a']);
  });

  it('unlocks Place when Cup schematic exposes targetable addresses', () => {
    const schematic: StageSchematic = {
      stageId: 's1',
      competitionId: 'c1',
      name: 'KO',
      status: 'Draft',
      formatKind: 'Cup',
      cases: [
        {
          formPosition: {
            kind: 'CupSlot',
            slotKey: 'SF1-A',
            roundName: 'Demi-finale',
            pairOrdinal: 1,
            side: 'A',
          },
        },
      ],
      connections: [],
    };
    expect(areProgressionPlacesLabeled(schematic)).toBe(true);
  });
});
