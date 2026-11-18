import { describe, expect, it } from 'vitest';

import type { SchematicCase } from '../../../types';
import {
  entryPlaceProjectionFromSchematic,
  isEntryPlacedInPhase,
  placedEntryIdsFromSchematic,
  placedHintShortPlace,
  primaryPlaceLabel,
  showPerEntryPlacement,
} from './structureEntryPlacementProjection';

const t = (key: string, opts?: Record<string, unknown>) => {
  if (key === 'place.group' && opts?.name) {
    return `Group ${opts.name}`;
  }
  if (key === 'place.groupWhere' && opts?.name) {
    return `Groupe ${opts.name}`;
  }
  if (
    key === 'place.groupWhereIndexed' &&
    opts?.name != null &&
    opts?.index != null
  ) {
    return `Groupe ${opts.name}, place ${opts.index}`;
  }
  if (key === 'place.side' && opts?.side) {
    return `side ${opts.side}`;
  }
  if (key === 'place.cup' && opts?.round && opts?.side) {
    return `${opts.round} · ${opts.side}`;
  }
  return key;
};

describe('structureEntryPlacementProjection', () => {
  it('enables per-entry placement for Cup and Groups only', () => {
    expect(showPerEntryPlacement('Cup')).toBe(true);
    expect(showPerEntryPlacement('Groups')).toBe(true);
    expect(showPerEntryPlacement('Championship')).toBe(false);
    expect(showPerEntryPlacement('Swiss')).toBe(false);
  });

  it('collects Cup entry-round slots with resolved teams', () => {
    const cases: SchematicCase[] = [
      {
        formPosition: { kind: 'CupSlot', slotKey: 'A', roundOrder: 0 },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      },
      {
        formPosition: { kind: 'CupSlot', slotKey: 'B', roundOrder: 0 },
        feedOrigin: { kind: 'Qualification', sourceStageId: 'src-q' },
      },
      {
        formPosition: { kind: 'CupSlot', slotKey: 'C', roundOrder: 1 },
        entry: { entryId: 'e2', displayName: 'Beta' },
      },
    ];
    const placed = placedEntryIdsFromSchematic(cases, 'Cup');
    expect([...placed]).toEqual(['e1']);
  });

  it('collects Groups group places with assignments', () => {
    const cases: SchematicCase[] = [
      {
        formPosition: { kind: 'GroupPlace', groupId: 'g1', index: 1 },
        assignment: { entryId: 'e3', displayName: 'Gamma' },
      },
    ];
    expect([...placedEntryIdsFromSchematic(cases, 'Groups')]).toEqual(['e3']);
  });

  it('returns not placed when schematic is not ready', () => {
    const placed = new Set(['e1']);
    expect(isEntryPlacedInPhase('e1', placed, 'Cup', false)).toBe(false);
    expect(isEntryPlacedInPhase('e1', placed, 'Cup', true)).toBe(true);
  });

  it('maps cup entry-round places to chrome labels', () => {
    const cases: SchematicCase[] = [
      {
        formPosition: { kind: 'CupSlot', slotKey: 'SF-1-A', roundOrder: 0 },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      },
    ];
    const map = entryPlaceProjectionFromSchematic(cases, 'Cup', t);
    expect(primaryPlaceLabel(map.get('e1'))).toBe('SF-1-A');
  });

  it('uses group shortPlace without index', () => {
    const cases: SchematicCase[] = [
      {
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g1',
          groupName: 'A',
        },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      },
    ];
    const map = entryPlaceProjectionFromSchematic(cases, 'Groups', t);
    expect(placedHintShortPlace(map.get('e1'))).toBe('Groupe A');
  });

  it('uses group shortPlace with index for accessible hint', () => {
    const cases: SchematicCase[] = [
      {
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g1',
          groupName: 'A',
          index: 3,
        },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      },
    ];
    const map = entryPlaceProjectionFromSchematic(cases, 'Groups', t);
    expect(placedHintShortPlace(map.get('e1'))).toBe('Groupe A, place 3');
  });

  it('keeps cup shortPlace as slot chrome only', () => {
    expect(
      placedHintShortPlace({
        labels: ['Tête'],
        details: ['Demi-finale 1 · côté A'],
        kinds: ['cup'],
      }),
    ).toBe('Tête');
  });

  it('emits short place for cup slot chrome', () => {
    expect(
      placedHintShortPlace({
        labels: ['SF-1-A'],
        details: [null],
        kinds: ['cup'],
      }),
    ).toBe('SF-1-A');
  });

  it('shortPlace uses the first group when several places match', () => {
    expect(
      placedHintShortPlace({
        labels: ['Group A', 'Group B'],
        details: ['Groupe A, place 1', 'Groupe B, place 2'],
        kinds: ['group', 'group'],
      }),
    ).toBe('Groupe A, place 1');
  });
});
