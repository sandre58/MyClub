import { describe, expect, it } from 'vitest';
import { closureBlockerLabel, closureHintHasContent } from './closureHintCopy';

describe('closureHintCopy', () => {
  it('treats an empty hint as silence', () => {
    expect(
      closureHintHasContent({
        canCompleteNormally: false,
        blockerCodes: [],
      }),
    ).toBe(false);
    expect(
      closureHintHasContent({
        canCompleteNormally: false,
        blockerCodes: ['  '],
      }),
    ).toBe(false);
  });

  it('treats normal completion or blocker codes as content', () => {
    expect(
      closureHintHasContent({
        canCompleteNormally: true,
        blockerCodes: [],
      }),
    ).toBe(true);
    expect(
      closureHintHasContent({
        canCompleteNormally: false,
        blockerCodes: ['ScheduledMatches'],
      }),
    ).toBe(true);
  });

  it('reuses completion and attention labels', () => {
    expect(closureBlockerLabel('ScheduledMatches')).toBe(
      'Des matchs sont encore planifiés',
    );
    expect(closureBlockerLabel('DrawNoSolution')).toBe('Tirage sans solution');
    expect(closureBlockerLabel('NotARealBlocker')).toBe('NotARealBlocker');
  });
});
