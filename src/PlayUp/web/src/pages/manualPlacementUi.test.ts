import { describe, expect, it } from 'vitest';
import type { SchematicCase } from '../types';
import {
  listCupManualPlaces,
  occupiedEntryIds,
  resolveManualPlaceMode,
} from './manualPlacementUi';

function cupCase(
  slotKey: string,
  overrides: Partial<SchematicCase> = {},
): SchematicCase {
  return {
    formPosition: { kind: 'CupSlot', slotKey },
    ...overrides,
  };
}

describe('resolveManualPlaceMode', () => {
  it('marks empty Cup place editable', () => {
    expect(resolveManualPlaceMode(cupCase('S1'))).toBe('editable');
  });

  it('marks Direct place editable', () => {
    expect(
      resolveManualPlaceMode(
        cupCase('S1', {
          feedOrigin: { kind: 'Direct', configuredEntryId: 'e1' },
          entry: { entryId: 'e1', displayName: 'A' },
        }),
      ),
    ).toBe('editable');
  });

  it('blocks Qual/Prog feeds', () => {
    expect(
      resolveManualPlaceMode(
        cupCase('S1', {
          feedOrigin: { kind: 'Qualification', sourceStageId: 'g' },
        }),
      ),
    ).toBe('qualProg');
    expect(
      resolveManualPlaceMode(
        cupCase('S1', {
          feedOrigin: { kind: 'Progression', sourceStageId: 'g' },
        }),
      ),
    ).toBe('qualProg');
  });

  it('blocks Draw feeds', () => {
    expect(
      resolveManualPlaceMode(
        cupCase('S1', {
          feedOrigin: { kind: 'Draw', drawId: 'd1' },
          entry: { entryId: 'e1' },
        }),
      ),
    ).toBe('draw');
  });

  it('blocks occupied places without Direct', () => {
    expect(
      resolveManualPlaceMode(cupCase('S1', { entry: { entryId: 'e1' } })),
    ).toBe('unavailable');
  });
});

describe('listCupManualPlaces / occupiedEntryIds', () => {
  it('lists Cup slots and skips other kinds', () => {
    const places = listCupManualPlaces([
      cupCase('S1'),
      {
        formPosition: { kind: 'GroupPlace', groupId: 'g', index: 0 },
      },
    ]);
    expect(places).toHaveLength(1);
    expect(places[0]?.slotKey).toBe('S1');
  });

  it('excludes current slot from occupied set', () => {
    const places = listCupManualPlaces([
      cupCase('S1', {
        feedOrigin: { kind: 'Direct' },
        entry: { entryId: 'e1' },
      }),
      cupCase('S2', {
        feedOrigin: { kind: 'Direct' },
        entry: { entryId: 'e2' },
      }),
    ]);
    expect([...occupiedEntryIds(places, 'S1')]).toEqual(['e2']);
  });
});
