import { describe, expect, it } from 'vitest';
import type { SchematicFormPosition, StageSchematic } from '../types';
import {
  areProgressionPlacesLabeled,
  hasCupPlaceAddressFacts,
  isCupPlaceTargetable,
  listLabeledCupPlaces,
  placeChromeLabel,
  placeDisplayLabel,
  placeLabelForDestinationSlotKey,
} from './structurePlaceLabel';

const t = (key: string, opts?: Record<string, unknown>) => {
  if (key === 'place.side') return `côté ${opts?.side}`;
  if (key === 'place.cup') return `${opts?.round} · ${opts?.side}`;
  if (key === 'place.cupWithOrdinal') {
    return `${opts?.round} ${opts?.ordinal} · ${opts?.side}`;
  }
  return key;
};

describe('structurePlaceLabel', () => {
  it('C2 chrome uses SlotKey; long label uses topology', () => {
    const sf: SchematicFormPosition = {
      kind: 'CupSlot',
      slotKey: 'SF-1-A',
      roundName: 'Demi-finale',
      pairOrdinal: 1,
      side: 'A',
    };
    expect(placeChromeLabel(sf)).toBe('SF-1-A');
    expect(placeDisplayLabel(sf, t)).toBe('Demi-finale 1 · côté A');

    const finale: SchematicFormPosition = {
      kind: 'CupSlot',
      slotKey: 'F-A',
      roundName: 'Finale',
      pairOrdinal: null,
      side: 'B',
    };
    expect(placeChromeLabel(finale)).toBe('F-A');
    expect(placeDisplayLabel(finale, t)).toBe('Finale · côté B');
  });

  it('SlotKey alone is chrome + targetable without inventing tour codes', () => {
    const raw: SchematicFormPosition = {
      kind: 'CupSlot',
      slotKey: 'SF-1-A',
    };
    expect(placeChromeLabel(raw)).toBe('SF-1-A');
    expect(hasCupPlaceAddressFacts(raw)).toBe(false);
    expect(isCupPlaceTargetable(raw)).toBe(true);
    expect(placeDisplayLabel(raw, t)).toBe('SF-1-A');
  });

  it('pairing-draw chrome falls back to ordinal·side; not Domain-targetable', () => {
    const pairing: SchematicFormPosition = {
      kind: 'CupSlot',
      fixtureId: 'fx1',
      side: 'A',
      roundName: 'Quart de finale',
      pairOrdinal: 2,
    };
    expect(placeChromeLabel(pairing)).toBe('2·A');
    expect(placeDisplayLabel(pairing, t)).toBe(
      'Quart de finale 2 · côté A',
    );
    expect(isCupPlaceTargetable(pairing)).toBe(false);
  });

  it('lists targetable Cup places with long labels for dialog/rail', () => {
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
            slotKey: 'SF-1-A',
            roundName: 'Demi-finale',
            pairOrdinal: 1,
            side: 'A',
          },
        },
        {
          formPosition: {
            kind: 'CupSlot',
            fixtureId: 'fx',
            side: 'B',
            roundName: 'Demi-finale',
            pairOrdinal: 1,
          },
        },
      ],
      connections: [],
    };
    expect(areProgressionPlacesLabeled(schematic)).toBe(true);
    expect(listLabeledCupPlaces(schematic, t)).toEqual([
      { apiIdentity: 'SF-1-A', label: 'Demi-finale 1 · côté A' },
    ]);
    expect(placeLabelForDestinationSlotKey(schematic, 'SF-1-A', t)).toBe(
      'Demi-finale 1 · côté A',
    );
    expect(placeLabelForDestinationSlotKey(schematic, 'missing', t)).toBeNull();
  });
});
