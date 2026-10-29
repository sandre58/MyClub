import { describe, expect, it } from 'vitest';
import type { StageDraw } from '../../types';
import {
  drawActionsOf,
  isDrawActionAvailable,
  resolveServerReleaseCta,
} from '../structure/draw/drawActions';

function draw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: 'draw-1',
    kind: 'Group',
    status: 'Cancelled',
    resolutionState: 'Resolved',
    slotPlacements: [],
    ...overrides,
  };
}

describe('draw action projection', () => {
  it('treats a missing actions payload as Hidden', () => {
    const actions = drawActionsOf(draw());
    expect(actions.cancel.state).toBe('Hidden');
    expect(actions.release.state).toBe('Hidden');
    expect(actions.release.releasableCount).toBe(0);
    expect(isDrawActionAvailable(actions.apply)).toBe(false);
  });

  it('reads Available and Blocked from the server payload', () => {
    const actions = drawActionsOf(
      draw({
        actions: {
          cancel: { state: 'Available' },
          release: {
            state: 'Blocked',
            blockCode: 'Application.DrawReleaseBlockedByAttachedMatches',
            releasableCount: 2,
          },
          apply: { state: 'Hidden' },
          publishAndApply: { state: 'Hidden' },
          redo: { state: 'Hidden' },
        },
      }),
    );
    expect(isDrawActionAvailable(actions.cancel)).toBe(true);
    expect(actions.release.state).toBe('Blocked');
    expect(actions.release.blockCode).toBe(
      'Application.DrawReleaseBlockedByAttachedMatches',
    );
    expect(actions.release.releasableCount).toBe(2);
  });

  it('offers a disabled release CTA from the release candidate', () => {
    const candidate = draw({
      actions: {
        cancel: { state: 'Hidden' },
        release: {
          state: 'Blocked',
          blockCode: 'Application.DrawReleaseBlockedByAttachedMatches',
          releasableCount: 1,
        },
        apply: { state: 'Hidden' },
        publishAndApply: { state: 'Hidden' },
        redo: { state: 'Hidden' },
      },
    });
    const cta = resolveServerReleaseCta({
      draws: [candidate],
      releaseCandidateDrawId: candidate.id,
    });
    expect(cta.draw?.id).toBe(candidate.id);
    expect(cta.aligned).toBe(1);
    expect(cta.disabled).toBe(true);
  });
});
