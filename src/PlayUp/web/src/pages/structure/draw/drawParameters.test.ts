import { describe, expect, it } from 'vitest';
import { resolveDrawParameterFacts } from './drawParameters';

describe('resolveDrawParameterFacts', () => {
  it('keeps pots × groups and marks seeds and every constraint not applied', () => {
    expect(
      resolveDrawParameterFacts({
        formatKind: 'Groups',
        numberOfPots: 4,
        groupCount: 2,
        numberOfSeeds: 2,
        constraints: [
          { type: 'MaxSameAssociationPerGroup', maxPerGroup: 1 },
          { type: 'SameAssociationAvoidance' },
        ],
      }),
    ).toEqual([
      { id: 'potsGroups', kind: 'applied', pots: 4, groups: 2 },
      { id: 'seeds', kind: 'notApplied', seeds: 2 },
      {
        id: 'constraint-MaxSameAssociationPerGroup-0',
        kind: 'notApplied',
        constraintType: 'MaxSameAssociationPerGroup',
        maxPerGroup: 1,
      },
      {
        id: 'constraint-SameAssociationAvoidance-1',
        kind: 'notApplied',
        constraintType: 'SameAssociationAvoidance',
        maxPerGroup: undefined,
      },
    ]);
  });

  it('omits pots × groups when the phase is not a groups draw', () => {
    expect(
      resolveDrawParameterFacts({
        formatKind: 'Cup',
        numberOfPots: null,
        groupCount: 0,
        numberOfSeeds: null,
        constraints: [],
      }),
    ).toEqual([]);
  });
});
