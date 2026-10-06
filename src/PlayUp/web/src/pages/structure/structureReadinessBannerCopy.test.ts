import { describe, expect, it } from 'vitest';
import { structureReadinessBannerHeadline } from './structureReadinessBannerCopy';

describe('structureReadinessBannerHeadline', () => {
  const t = (key: string, options?: Record<string, unknown>) => {
    if (key === 'readiness.bannerProblemsIdentified') {
      const count = options?.count as number;
      return count === 1 ? '1 problème identifié' : `${count} problèmes identifiés`;
    }
    if (key === 'readiness.bannerPriorityActions') {
      const count = options?.count as number;
      return count === 1 ? '1 action prioritaire' : `${count} actions prioritaires`;
    }
    return key;
  };

  it('combines problems and shown priority actions', () => {
    expect(
      structureReadinessBannerHeadline({
        presentedProblemCount: 5,
        priorityActionsShown: 3,
        t,
      }),
    ).toBe('5 problèmes identifiés · 3 actions prioritaires');
  });

  it('uses singular forms when counts are one', () => {
    expect(
      structureReadinessBannerHeadline({
        presentedProblemCount: 1,
        priorityActionsShown: 1,
        t,
      }),
    ).toBe('1 problème identifié · 1 action prioritaire');
  });

  it('shows problems only when no actions are listed', () => {
    expect(
      structureReadinessBannerHeadline({
        presentedProblemCount: 2,
        priorityActionsShown: 0,
        t,
      }),
    ).toBe('2 problèmes identifiés');
  });
});
