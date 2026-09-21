import { describe, expect, it } from 'vitest';
import {
  emptyQualIntent,
  hasDuplicateSourceOccurrence,
  incompleteIntentReason,
  isIntentComplete,
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
});
