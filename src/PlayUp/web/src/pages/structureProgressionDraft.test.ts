import { describe, expect, it } from 'vitest';
import type { StageSchematic } from '../types';
import {
  areProgressionPlacesLabeled,
  emptyProgIntent,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
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
    expect(draft.expandedPathCount).toBe(4);
    expect(toApiIntent(draft, 1).destinationSlotKey).toBeNull();
  });

  it('coerces cross-stage Place to Population (V3 purge)', () => {
    const draft = intentFromApi(
      {
        intentId: 'i1',
        order: 1,
        roundId: 'r1',
        outcome: 'Loser',
        destinationStageId: 'other',
        destinationSlotKey: 'slot-7',
        expandedPathCount: 1,
      },
      'source',
    );
    expect(draft.targetKind).toBe('population');
    expect(draft.destinationSlotKey).toBe('');
    expect(incompleteIntentReason(draft, [draft], false)).toBeNull();
  });

  it('blocks Place intents while labels are unavailable', () => {
    const draft = emptyProgIntent('source', 'place');
    draft.roundId = 'r1';
    draft.destinationStageId = 'source';
    draft.destinationSlotKey = 'A';
    expect(incompleteIntentReason(draft, [draft], false)).toBe(
      'PlaceUnavailable',
    );
    expect(isIntentComplete(draft, [draft], false)).toBe(false);
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

  it('detects duplicate Place destinations when labeled', () => {
    const a = emptyProgIntent('source', 'place');
    a.roundId = 'r1';
    a.destinationSlotKey = 'SF-A';
    const b = emptyProgIntent('source', 'place');
    b.roundId = 'r2';
    b.destinationSlotKey = 'SF-A';
    expect(incompleteIntentReason(a, [a, b], true)).toBe('DuplicatePlace');
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
