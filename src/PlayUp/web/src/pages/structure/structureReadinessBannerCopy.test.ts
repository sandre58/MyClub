import { describe, expect, it } from 'vitest';
import { structureReadinessBannerHeadline } from './structureReadinessBannerCopy';

describe('structureReadinessBannerHeadline', () => {
  const t = (key: string, options?: Record<string, unknown>) => {
    if (key === 'readiness.bannerProblemsIdentified') {
      const count = options?.count as number;
      return count === 1 ? '1 problème identifié' : `${count} problèmes identifiés`;
    }
    return key;
  };

  it('uses plural problems label', () => {
    expect(
      structureReadinessBannerHeadline({
        presentedProblemCount: 5,
        t,
      }),
    ).toBe('5 problèmes identifiés');
  });

  it('uses singular problems label', () => {
    expect(
      structureReadinessBannerHeadline({
        presentedProblemCount: 1,
        t,
      }),
    ).toBe('1 problème identifié');
  });
});
