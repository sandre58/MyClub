import { describe, expect, it } from 'vitest';
import {
  emptyQualIntent,
  hasDuplicateSourceOccurrence,
  incompleteIntentReason,
  intentFromApi,
  isIntentComplete,
  countUnmappedPlaceSlots,
  fillEmptyPlaceSlotKeys,
  resizeDestinationSlotKeys,
  syncPlaceSlotKeys,
  toApiIntent,
} from './structureQualificationDraft';

const groups = [
  { id: 'g1', name: 'Groupe A' },
  { id: 'g2', name: 'Groupe B' },
];

describe('qualification duplicate source detection', () => {
  it('flags overlapping source occurrences across destinations (soft)', () => {
    const a = emptyQualIntent('sf');
    a.sourceKind = 'EachGroup';
    a.positionFrom = '1';
    a.positionTo = '1';

    const b = emptyQualIntent('final');
    b.sourceKind = 'EachGroup';
    b.positionFrom = '1';
    b.positionTo = '1';

    expect(hasDuplicateSourceOccurrence(a, [a, b], groups)).toBe(true);
    expect(hasDuplicateSourceOccurrence(b, [a, b], groups)).toBe(true);
    // Soft: does not block completeness
    expect(incompleteIntentReason(a, groups)).toBeNull();
    expect(isIntentComplete(a, groups)).toBe(true);
  });

  it('flags range overlap on the same source place', () => {
    const a = emptyQualIntent('sf');
    a.sourceKind = 'EachGroup';
    a.positionFrom = '1';
    a.positionTo = '1';

    const b = emptyQualIntent('sf');
    b.sourceKind = 'EachGroup';
    b.positionFrom = '1';
    b.positionTo = '2';

    expect(hasDuplicateSourceOccurrence(a, [a, b], groups)).toBe(true);
  });

  it('flags SingleGroup overlapping an EachGroup occurrence', () => {
    const each = emptyQualIntent('sf');
    each.sourceKind = 'EachGroup';
    each.positionFrom = '1';
    each.positionTo = '1';

    const single = emptyQualIntent('final');
    single.sourceKind = 'SingleGroup';
    single.groupId = 'g1';
    single.groupName = 'Groupe A';
    single.positionFrom = '1';
    single.positionTo = '1';

    expect(hasDuplicateSourceOccurrence(single, [each, single], groups)).toBe(
      true,
    );
  });

  it('ignores points condition when comparing sources', () => {
    const plain = emptyQualIntent('sf');
    plain.sourceKind = 'SingleGroup';
    plain.groupId = 'g1';
    plain.positionFrom = '1';
    plain.positionTo = '1';

    const gated = emptyQualIntent('final');
    gated.sourceKind = 'SingleGroup';
    gated.groupId = 'g1';
    gated.positionFrom = '1';
    gated.positionTo = '1';
    gated.conditionKind = 'points';
    gated.minimumPoints = '3';

    expect(hasDuplicateSourceOccurrence(plain, [plain, gated], groups)).toBe(
      true,
    );
  });

  it('does not flag distinct source places', () => {
    const a = emptyQualIntent('sf');
    a.sourceKind = 'SingleGroup';
    a.groupId = 'g1';
    a.positionFrom = '1';
    a.positionTo = '1';

    const b = emptyQualIntent('sf');
    b.sourceKind = 'SingleGroup';
    b.groupId = 'g1';
    b.positionFrom = '2';
    b.positionTo = '2';

    expect(hasDuplicateSourceOccurrence(a, [a, b], groups)).toBe(false);
  });

  it('ignores destination / Place when detecting duplicates', () => {
    const a = emptyQualIntent('sf', 'place');
    a.sourceKind = 'EachGroup';
    a.destinationSlotKeys = ['SF1-A', 'SF1-B'];
    a.positionFrom = '1';
    a.positionTo = '1';

    const b = emptyQualIntent('final', 'population');
    b.sourceKind = 'EachGroup';
    b.positionFrom = '1';
    b.positionTo = '1';

    expect(hasDuplicateSourceOccurrence(a, [a, b], groups)).toBe(true);
  });
});

describe('qualification Place destinationSlotKeys', () => {
  it('maps Place API intents with destinationSlotKeys', () => {
    const draft = intentFromApi({
      intentId: 'i1',
      order: 1,
      sourceKind: 'Overall',
      positionFrom: 1,
      positionTo: 2,
      destinationStageId: 'peer',
      destinationSlotKeys: ['SF1-A', 'SF1-B'],
    });
    expect(draft.targetKind).toBe('place');
    expect(draft.destinationSlotKeys).toEqual(['SF1-A', 'SF1-B']);
    expect(toApiIntent(draft, 1).destinationSlotKeys).toEqual([
      'SF1-A',
      'SF1-B',
    ]);
  });

  it('coerces legacy singular destinationSlotKey from API', () => {
    const draft = intentFromApi({
      intentId: 'i1',
      order: 1,
      sourceKind: 'Overall',
      positionFrom: 1,
      positionTo: 1,
      destinationStageId: 'peer',
      destinationSlotKey: 'SF1-A',
    });
    expect(draft.targetKind).toBe('place');
    expect(draft.destinationSlotKeys).toEqual(['SF1-A']);
  });

  it('maps Population API intents to null slot keys', () => {
    const draft = intentFromApi({
      intentId: 'i1',
      order: 1,
      sourceKind: 'Overall',
      positionFrom: 1,
      positionTo: 1,
      destinationStageId: 'peer',
      destinationSlotKeys: null,
    });
    expect(draft.targetKind).toBe('population');
    expect(toApiIntent(draft, 1).destinationSlotKeys).toBeNull();
  });

  it('requires all Place slots filled and matching Expand count', () => {
    const draft = emptyQualIntent('peer', 'place');
    draft.sourceKind = 'Overall';
    draft.positionFrom = '1';
    draft.positionTo = '2';
    expect(incompleteIntentReason(draft, groups, true)).toBe('MultiSlot');
    draft.destinationSlotKeys = ['SF1-A'];
    expect(incompleteIntentReason(draft, groups, true)).toBe('MultiSlot');
    draft.destinationSlotKeys = ['SF1-A', ''];
    expect(incompleteIntentReason(draft, groups, true)).toBe('MultiSlot');
    draft.destinationSlotKeys = ['SF1-A', 'SF1-B'];
    expect(incompleteIntentReason(draft, groups, true)).toBeNull();
  });

  it('flags duplicate Place slot keys within an intent', () => {
    const draft = emptyQualIntent('peer', 'place');
    draft.sourceKind = 'Overall';
    draft.positionFrom = '1';
    draft.positionTo = '2';
    draft.destinationSlotKeys = ['SF1-A', 'SF1-A'];
    expect(incompleteIntentReason(draft, groups, true)).toBe('DuplicateSlot');
  });

  it('flags duplicate Place destinations across intents', () => {
    const a = emptyQualIntent('peer', 'place');
    a.sourceKind = 'Overall';
    a.positionFrom = '1';
    a.positionTo = '1';
    a.destinationSlotKeys = ['SF1-A'];
    const b = emptyQualIntent('peer', 'place');
    b.sourceKind = 'Overall';
    b.positionFrom = '2';
    b.positionTo = '2';
    b.destinationSlotKeys = ['SF1-A'];
    expect(incompleteIntentReason(a, groups, true, [a, b])).toBe(
      'DuplicatePlace',
    );
    expect(isIntentComplete(a, groups, true, [a, b])).toBe(false);
  });

  it('allows two intents to the same Population destination', () => {
    const a = emptyQualIntent('peer', 'population');
    a.sourceKind = 'Overall';
    a.positionFrom = '1';
    a.positionTo = '1';
    const b = emptyQualIntent('peer', 'population');
    b.sourceKind = 'Overall';
    b.positionFrom = '2';
    b.positionTo = '2';
    expect(incompleteIntentReason(a, groups, true, [a, b])).toBeNull();
  });

  it('blocks Place while destination places are unlabeled', () => {
    const draft = emptyQualIntent('peer', 'place');
    draft.sourceKind = 'Overall';
    draft.destinationSlotKeys = ['SF1-A'];
    expect(incompleteIntentReason(draft, groups, false)).toBe(
      'PlaceUnavailable',
    );
    expect(isIntentComplete(draft, groups, false)).toBe(false);
  });

  it('resizes destinationSlotKeys when Expand count changes', () => {
    expect(resizeDestinationSlotKeys(['A', 'B', 'C'], 2)).toEqual(['A', 'B']);
    expect(resizeDestinationSlotKeys(['A'], 3)).toEqual(['A', '', '']);
    expect(resizeDestinationSlotKeys(['A', 'B'], 2)).toEqual(['A', 'B']);

    const draft = emptyQualIntent('peer', 'place');
    draft.sourceKind = 'Overall';
    draft.positionFrom = '1';
    draft.positionTo = '2';
    draft.destinationSlotKeys = ['SF1-A', 'SF1-B'];
    draft.positionTo = '3';
    const grown = syncPlaceSlotKeys(draft, groups);
    expect(grown.destinationSlotKeys).toEqual(['SF1-A', 'SF1-B', '']);
    draft.positionTo = '1';
    draft.destinationSlotKeys = ['SF1-A', 'SF1-B', 'SF1-C'];
    const shrunk = syncPlaceSlotKeys(draft, groups);
    expect(shrunk.destinationSlotKeys).toEqual(['SF1-A']);
  });

  it('clears slot keys when switching to Population via sync', () => {
    const draft = emptyQualIntent('peer', 'place');
    draft.destinationSlotKeys = ['SF1-A'];
    draft.targetKind = 'population';
    expect(syncPlaceSlotKeys(draft, groups).destinationSlotKeys).toEqual([]);
  });

  it('fillEmptyPlaceSlotKeys fills only empties in available order', () => {
    expect(
      fillEmptyPlaceSlotKeys(
        ['R16-3', '', 'R16-1', ''],
        ['R16-1', 'R16-2', 'R16-3', 'R16-4'],
      ),
    ).toEqual(['R16-3', 'R16-2', 'R16-1', 'R16-4']);
  });

  it('countUnmappedPlaceSlots sums empty Place rows', () => {
    const a = emptyQualIntent('peer', 'place');
    a.sourceKind = 'Overall';
    a.positionFrom = '1';
    a.positionTo = '2';
    a.destinationSlotKeys = ['SF1-A', ''];
    expect(countUnmappedPlaceSlots([a], groups)).toBe(1);
  });
});
