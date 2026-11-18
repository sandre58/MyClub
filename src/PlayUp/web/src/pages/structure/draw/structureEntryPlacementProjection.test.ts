import { describe, expect, it } from 'vitest';

import type { SchematicCase } from '../../../types';
import {
  buildPlacedTooltipModel,
  entryPlaceProjectionFromSchematic,
  isEntryPlacedInPhase,
  placedEntryIdsFromSchematic,
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
  if (key === 'entries.teamAlreadyPlacedIntro') {
    return 'Cette équipe est déjà placée :';
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

  it('uses group shortPlace without index and where line without index', () => {
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
    const model = buildPlacedTooltipModel(map.get('e1'), 'Poules', t);
    expect(model?.shortPlace).toBe('Groupe A');
    expect(model?.whereLines).toEqual(['Groupe A']);
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
    expect(
      buildPlacedTooltipModel(map.get('e1'), 'Poules', t)?.shortPlace,
    ).toBe('Groupe A, place 3');
  });

  it('keeps cup shortPlace as slot chrome only', () => {
    expect(
      buildPlacedTooltipModel(
        {
          labels: ['Tête'],
          details: ['Demi-finale 1 · côté A'],
          kinds: ['cup'],
        },
        'Généré',
        t,
      ),
    ).toMatchObject({
      shortPlace: 'Tête',
      whereLines: ['Généré — Tête ; Demi-finale 1 · côté A'],
    });
  });

  it('uses cup where lines with phase name, slot chrome and topology detail', () => {
    const phase = 'Généré';
    expect(
      buildPlacedTooltipModel(
        { labels: ['SF-1-A'], details: [null], kinds: ['cup'] },
        phase,
        t,
      )?.whereLines,
    ).toEqual(['Généré — SF-1-A']);
  });

  it('keeps slot chrome in cup tooltip when destination phase name is missing', () => {
    expect(
      buildPlacedTooltipModel(
        {
          labels: ['Tête'],
          details: ['Tour 1 · côté A'],
          kinds: ['cup'],
        },
        null,
        t,
      ),
    ).toMatchObject({
      whereLines: ['Tête ; Tour 1 · côté A'],
      shortPlace: 'Tête',
    });
  });

  it('emits one where line per cup placement without repeating the intro', () => {
    const model = buildPlacedTooltipModel(
      {
        labels: ['Tête', 'Demi-finale 1'],
        details: [null, 'côté A'],
        kinds: ['cup', 'cup'],
      },
      'Généré',
      t,
    );
    expect(model?.whereLines).toEqual([
      'Généré — Tête',
      'Généré — Demi-finale 1 ; côté A',
    ]);
    expect(model?.shortPlace).toBe('Tête');
  });

  it('shortPlace uses the first group when several places match', () => {
    expect(
      buildPlacedTooltipModel(
        {
          labels: ['Group A', 'Group B'],
          details: ['Groupe A, place 1', 'Groupe B, place 2'],
          kinds: ['group', 'group'],
        },
        'Poules',
        t,
      )?.shortPlace,
    ).toBe('Groupe A, place 1');
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
    expect(
      buildPlacedTooltipModel(map.get('e1'), 'Poules', t)?.whereLines,
    ).toHaveLength(2);
  });
});
