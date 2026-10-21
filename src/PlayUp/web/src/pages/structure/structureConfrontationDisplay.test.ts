import { describe, expect, it } from 'vitest';
import {
  structureMatchNumberInContainerVisible,
  structureProgressionSourceContext,
  structureProgressionSortKey,
} from './structureConfrontationDisplay';

describe('structureConfrontationDisplay', () => {
  it('hides match number when PairKey is present', () => {
    expect(structureMatchNumberInContainerVisible('P1', 3)).toBeNull();
    expect(
      structureProgressionSourceContext(
        'P1',
        3,
        (n) => `Match #${n}`,
        () => '?',
      ),
    ).toBe('P1');
    expect(structureProgressionSortKey('P1', 3)).toBe('P1');
  });

  it('shows match number only without PairKey', () => {
    expect(structureMatchNumberInContainerVisible(null, 2)).toBe(2);
    expect(
      structureProgressionSourceContext(
        null,
        2,
        (n) => `Match #${n}`,
        () => '?',
      ),
    ).toBe('Match #2');
    expect(structureProgressionSortKey(null, 2)).toBe('00000002');
  });
});
