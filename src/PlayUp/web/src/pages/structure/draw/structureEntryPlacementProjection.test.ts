import { describe, expect, it } from 'vitest';

import type { SchematicCase } from '../../../types';
import {
  entryPlaceAccessibilityLabel,
  entryPlaceProjectionFromSchematic,
  isEntryPlacedInPhase,
  placedEntryIdsFromSchematic,
  placedTooltipLines,
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
  if (key === 'entries.teamAlreadyPlaced' && opts?.where != null) {
    return `Cette équipe est déjà placée : ${opts.where}`;
  }
  if (key === 'entries.teamAlreadyPlacedCup' && opts?.where != null) {
    return `Cette équipe est déjà placée : Coupe — ${opts.where}`;
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

  it('uses group where label without index', () => {
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
    expect(primaryPlaceLabel(map.get('e1'))).toBe('Group A');
    expect(map.get('e1')?.details[0]).toBe('Groupe A');
    expect(placedTooltipLines(map.get('e1'), t)).toEqual([
      'Cette équipe est déjà placée : Groupe A',
    ]);
    expect(entryPlaceAccessibilityLabel(map.get('e1'), t)).toBe(
      'Cette équipe est déjà placée : Groupe A',
    );
  });

  it('shows group place as Groupe name only with index in details', () => {
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
    expect(primaryPlaceLabel(map.get('e1'))).toBe('Group A');
    expect(map.get('e1')?.kinds[0]).toBe('group');
    expect(map.get('e1')?.details[0]).toBe('Groupe A, place 3');
    expect(entryPlaceAccessibilityLabel(map.get('e1'), t)).toBe(
      'Cette équipe est déjà placée : Groupe A, place 3',
    );
  });

  it('uses cup sentences with long topology in where', () => {
    expect(
      placedTooltipLines(
        {
          labels: ['Tête'],
          details: ['Demi-finale 1 · côté A'],
          kinds: ['cup'],
        },
        t,
      ),
    ).toEqual([
      'Cette équipe est déjà placée : Coupe — Demi-finale 1 · côté A',
    ]);
    expect(
      placedTooltipLines(
        { labels: ['SF-1-A'], details: [null], kinds: ['cup'] },
        t,
      ),
    ).toEqual(['Cette équipe est déjà placée : Coupe — SF-1-A']);
  });

  it('emits one sentence per cup placement', () => {
    expect(
      placedTooltipLines(
        {
          labels: ['Tête', 'Demi-finale 1'],
          details: [null, 'côté A'],
          kinds: ['cup', 'cup'],
        },
        t,
      ),
    ).toEqual([
      'Cette équipe est déjà placée : Coupe — Tête',
      'Cette équipe est déjà placée : Coupe — côté A',
    ]);
    expect(
      entryPlaceAccessibilityLabel(
        {
          labels: ['Tête', 'Demi-finale 1'],
          details: [null, 'côté A'],
          kinds: ['cup', 'cup'],
        },
        t,
      ),
    ).toBe(
      'Cette équipe est déjà placée : Coupe — Tête ; Cette équipe est déjà placée : Coupe — côté A',
    );
  });

  it('separates multiple group placements with semicolons in aria text', () => {
    expect(
      entryPlaceAccessibilityLabel(
        {
          labels: ['Group A', 'Group B'],
          details: ['Groupe A, place 1', 'Groupe B, place 2'],
          kinds: ['group', 'group'],
        },
        t,
      ),
    ).toBe(
      'Cette équipe est déjà placée : Groupe A, place 1 ; Cette équipe est déjà placée : Groupe B, place 2',
    );
  });

  it('keeps multiple place labels for the same entry', () => {
    const cases: SchematicCase[] = [
      {
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g1',
          groupName: 'A',
          index: 1,
        },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      },
      {
        formPosition: {
          kind: 'GroupPlace',
          groupId: 'g2',
          groupName: 'B',
          index: 2,
        },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      },
    ];
    const map = entryPlaceProjectionFromSchematic(cases, 'Groups', t);
    expect(map.get('e1')?.labels).toEqual(['Group A', 'Group B']);
    expect(placedTooltipLines(map.get('e1'), t)).toHaveLength(2);
    expect(entryPlaceAccessibilityLabel(map.get('e1'), t)).toBe(
      'Cette équipe est déjà placée : Groupe A, place 1 ; Cette équipe est déjà placée : Groupe B, place 2',
    );
  });
});
