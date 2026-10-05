import { describe, expect, it } from 'vitest';
import { countSchematicOccupiedPlaces } from './structurePlaceOccupancy';
import type { SchematicCase } from '../../types';

function cupCase(
  slotKey: string,
  roundOrder: number,
  entryId?: string,
): SchematicCase {
  return {
    formPosition: {
      kind: 'CupSlot',
      slotKey,
      roundOrder,
    },
    entry: entryId ? { entryId, displayName: 'Team' } : null,
  };
}

describe('countSchematicOccupiedPlaces', () => {
  it('counts entry-round cup occupants only', () => {
    const cases = [
      cupCase('S1', 0, 'e1'),
      cupCase('S2', 0),
      cupCase('S3', 1, 'e3'),
    ];
    expect(countSchematicOccupiedPlaces(cases, 'Cup', 8)).toBe(1);
  });

  it('returns null when places N is unknown', () => {
    expect(countSchematicOccupiedPlaces([], 'Cup', null)).toBeNull();
  });
});
