import { describe, expect, it } from 'vitest';
import {
  areProgressionPlacesLabeled,
  emptyProgPath,
  incompletePathReason,
  isPathComplete,
  pathFromApi,
  toApiPath,
} from './structureProgressionDraft';

describe('structureProgressionDraft', () => {
  it('gates Place labels until schematic U4', () => {
    expect(areProgressionPlacesLabeled()).toBe(false);
  });

  it('maps population API paths', () => {
    const draft = pathFromApi(
      {
        sourceFixtureId: 'f1',
        outcome: 'Winner',
        destinationStageId: 'peer',
        destinationSlotKey: null,
        sourceLabel: 'QF · #1',
      },
      'source',
    );
    expect(draft.targetKind).toBe('population');
    expect(draft.legacyInterPhasePlace).toBe(false);
    expect(toApiPath(draft).destinationSlotKey).toBeNull();
  });

  it('flags legacy inter-phase Place without exposing SlotKey in summary helpers', () => {
    const draft = pathFromApi(
      {
        sourceFixtureId: 'f1',
        outcome: 'Loser',
        destinationStageId: 'other',
        destinationSlotKey: 'slot-7',
      },
      'source',
    );
    expect(draft.targetKind).toBe('place');
    expect(draft.legacyInterPhasePlace).toBe(true);
    expect(incompletePathReason(draft, [draft], false)).toBe('LegacyPlace');
  });

  it('blocks Place paths while labels are unavailable', () => {
    const draft = emptyProgPath('source', 'place');
    draft.sourceFixtureId = 'f1';
    draft.destinationStageId = 'source';
    draft.destinationSlotKey = 'A';
    expect(incompletePathReason(draft, [draft], false)).toBe(
      'PlaceUnavailable',
    );
    expect(isPathComplete(draft, [draft], false)).toBe(false);
  });

  it('detects duplicate fixture+outcome', () => {
    const a = emptyProgPath('peer');
    a.sourceFixtureId = 'f1';
    a.outcome = 'Winner';
    const b = emptyProgPath('peer');
    b.sourceFixtureId = 'f1';
    b.outcome = 'Winner';
    expect(incompletePathReason(a, [a, b], false)).toBe('DuplicateSource');
  });

  it('allows multiple paths to the same Population', () => {
    const a = emptyProgPath('peer');
    a.sourceFixtureId = 'f1';
    a.outcome = 'Winner';
    const b = emptyProgPath('peer');
    b.sourceFixtureId = 'f2';
    b.outcome = 'Winner';
    expect(isPathComplete(a, [a, b], false)).toBe(true);
    expect(isPathComplete(b, [a, b], false)).toBe(true);
  });

  it('detects duplicate Place destinations when labeled', () => {
    const a = emptyProgPath('source', 'place');
    a.sourceFixtureId = 'f1';
    a.destinationSlotKey = 'SF-A';
    const b = emptyProgPath('source', 'place');
    b.sourceFixtureId = 'f2';
    b.destinationSlotKey = 'SF-A';
    expect(incompletePathReason(a, [a, b], true)).toBe('DuplicatePlace');
  });
});
