import { describe, expect, it } from 'vitest';

import {
  inboundRuleDestinationSlotKey,
  qualifierSeatPlaceLabel,
} from './compositionQualifierSeatPlaceLabel';
import type { SelectionMode, StageSchematic } from '../../../types';

const schematic: StageSchematic = {
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
};

describe('qualifierSeatPlaceLabel', () => {
  it('uses entry projection when the seat is resolved', () => {
    expect(
      qualifierSeatPlaceLabel({
        seat: {
          resolved: true,
          entryId: 'e1',
          teamName: 'Nord',
          placed: true,
        },
        inboundRule: undefined,
        schematic,
        entryPlaceProjections: new Map([
          ['e1', { labels: ['Tête'], details: [null] }],
        ]),
      }),
    ).toBe('Tête');
  });

  it('uses destination slot chrome when unresolved but structurally placed', () => {
    const inboundRule = {
      feedKey: 'q-src-1',
      sourceStageId: 'src',
      seats: [],
      qualificationPath: {
        order: 1,
        selectionMode: 'Position' as SelectionMode,
        selectionValue: 1,
        destinationStageId: 'dest',
        destinationSlotKey: 'Tete',
      },
    };

    expect(inboundRuleDestinationSlotKey(inboundRule)).toBe('Tete');

    expect(
      qualifierSeatPlaceLabel({
        seat: {
          resolved: false,
          entryId: null,
          teamName: null,
          placed: true,
        },
        inboundRule,
        schematic,
        entryPlaceProjections: new Map(),
      }),
    ).toBe('Tete');
  });
});
