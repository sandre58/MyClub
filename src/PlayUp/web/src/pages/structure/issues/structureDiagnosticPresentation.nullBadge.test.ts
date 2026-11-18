import { describe, expect, it } from 'vitest';
import {
  deferGroupsNotReadyPresentation,
  isSlotFeedAwaitingDraw,
} from './structureDiagnosticPresentation';

describe('full Groups grid badge', () => {
  it('does not treat a null badge as a draw that is still required', () => {
    const full = { hasDrawRules: true, drawExecutionBadge: null };
    expect(isSlotFeedAwaitingDraw(full)).toBe(false);
    expect(deferGroupsNotReadyPresentation(full)).toBe(false);
    expect(
      isSlotFeedAwaitingDraw({
        hasDrawRules: true,
        drawExecutionBadge: 'ToLaunch',
      }),
    ).toBe(true);
    expect(
      deferGroupsNotReadyPresentation({
        hasDrawRules: true,
        drawExecutionBadge: 'ToLaunch',
      }),
    ).toBe(true);
  });
});
