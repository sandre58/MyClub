import { describe, expect, it } from 'vitest';
import type { SchematicFormPosition, StageSchematic } from '../../types';
import {
  areProgressionPlacesLabeled,
  filterPlaceEligiblePeers,
  hasCupPlaceAddressFacts,
  isCupPlaceTargetable,
  listLabeledCupPlaces,
  listLabeledGroupPlaces,
  listLabeledPlaces,
  placeChromeLabel,
  placeChromeForDestinationSlotKey,
  placeDisplayLabel,
  placeLabelForDestinationSlotKey,
} from './structurePlaceLabel';

const t = (key: string, opts?: Record<string, unknown>) => {
  if (key === 'place.side') return `côté ${opts?.side}`;
  if (key === 'place.cup') return `${opts?.round} · ${opts?.side}`;
  if (key === 'place.cupWithOrdinal') {
    return `${opts?.round} ${opts?.ordinal} · ${opts?.side}`;
  }
  if (key === 'place.group') return `Groupe ${opts?.name}`;
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

  it('pairing-draw chrome has no Place address; not Domain-targetable (I2)', () => {
    const pairing: SchematicFormPosition = {
      kind: 'CupSlot',
      fixtureId: 'fx1',
      side: 'A',
      roundName: 'Quart de finale',
      pairOrdinal: 2,
    };
    expect(placeChromeLabel(pairing)).toBeNull();
    expect(placeDisplayLabel(pairing, t)).toBe('Quart de finale 2 · côté A');
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
      {
        apiIdentity: 'SF-1-A',
        label: 'SF-1-A',
        description: 'Demi-finale 1 · côté A',
        grain: 'slot',
      },
    ]);
    expect(placeChromeForDestinationSlotKey(schematic, 'SF-1-A')).toBe(
      'SF-1-A',
    );
    expect(placeLabelForDestinationSlotKey(schematic, 'SF-1-A', t)).toBe(
      'Demi-finale 1 · côté A',
    );
    expect(placeLabelForDestinationSlotKey(schematic, 'missing', t)).toBeNull();
  });

  it('lists Groups A1 places by stable groupId (label = localized Groupe A)', () => {
    const schematic: StageSchematic = {
      stageId: 'g1',
      competitionId: 'c1',
      name: 'Poules',
      status: 'Draft',
      formatKind: 'Groups',
      cases: [
        {
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'grp-a',
            groupName: 'A',
            index: 1,
          },
        },
        {
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'grp-a',
            groupName: 'A',
            index: 2,
          },
        },
        {
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'grp-b',
            groupName: 'B',
            index: 1,
          },
        },
      ],
      connections: [],
    };
    expect(areProgressionPlacesLabeled(schematic)).toBe(true);
    expect(listLabeledGroupPlaces(schematic, t)).toEqual([
      {
        apiIdentity: 'grp-a',
        label: 'Groupe A',
        description: null,
        grain: 'group',
      },
      {
        apiIdentity: 'grp-b',
        label: 'Groupe B',
        description: null,
        grain: 'group',
      },
    ]);
    expect(listLabeledPlaces(schematic, t)).toEqual(
      listLabeledGroupPlaces(schematic, t),
    );
  });

  it('Groups without stable groupId are Place-ineligible', () => {
    const bare: StageSchematic = {
      stageId: 'g-bare',
      competitionId: 'c1',
      name: 'Poules bare',
      status: 'Draft',
      formatKind: 'Groups',
      cases: [{ formPosition: { kind: 'GroupPlace', index: 1 } }],
      connections: [],
    };
    expect(areProgressionPlacesLabeled(bare)).toBe(false);
    expect(listLabeledPlaces(bare, t)).toEqual([]);
  });

  it('filters Place-eligible peers: Cup slots, Groups, Champ/Swiss Forme', () => {
    const labeledCup: StageSchematic = {
      stageId: 'cup',
      competitionId: 'c1',
      name: 'Cup',
      status: 'Draft',
      formatKind: 'Cup',
      cases: [{ formPosition: { kind: 'CupSlot', slotKey: 'R16-1-A' } }],
      connections: [],
    };
    const bareCup: StageSchematic = {
      stageId: 'cup-bare',
      competitionId: 'c1',
      name: 'Cup bare',
      status: 'Draft',
      formatKind: 'Cup',
      cases: [
        {
          formPosition: {
            kind: 'CupSlot',
            fixtureId: 'fx',
            side: 'A',
            roundName: 'Finale',
          },
        },
      ],
      connections: [],
    };
    const groups: StageSchematic = {
      stageId: 'groups',
      competitionId: 'c1',
      name: 'Groups',
      status: 'Draft',
      formatKind: 'Groups',
      cases: [
        {
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'g1',
            groupName: 'A',
            index: 1,
          },
        },
      ],
      connections: [],
    };
    const championship: StageSchematic = {
      stageId: 'champ',
      competitionId: 'c1',
      name: 'Champ',
      status: 'Draft',
      formatKind: 'Championship',
      cases: [{ formPosition: { kind: 'RosterPlace', index: 1 } }],
      connections: [],
    };
    const swiss: StageSchematic = {
      stageId: 'swiss',
      competitionId: 'c1',
      name: 'Swiss',
      status: 'Draft',
      formatKind: 'Swiss',
      cases: [{ formPosition: { kind: 'RosterPlace', index: 1 } }],
      connections: [],
    };
    const peers = [
      { stageId: 'cup' },
      { stageId: 'cup-bare' },
      { stageId: 'groups' },
      { stageId: 'champ' },
      { stageId: 'swiss' },
      { stageId: 'missing' },
    ];
    const byId = new Map<string, StageSchematic | undefined>([
      ['cup', labeledCup],
      ['cup-bare', bareCup],
      ['groups', groups],
      ['champ', championship],
      ['swiss', swiss],
      ['missing', undefined],
    ]);
    expect(filterPlaceEligiblePeers(peers, byId)).toEqual([
      { stageId: 'cup' },
      { stageId: 'groups' },
      { stageId: 'champ' },
      { stageId: 'swiss' },
    ]);
  });
});
