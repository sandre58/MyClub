import { describe, expect, it } from 'vitest';
import type { StageDraw, StageOverview } from '../../../types';
import { releaseFooterMode, resolveServerReleaseCta } from './drawActions';

type ReleaseAction = NonNullable<StageDraw['actions']>['release'];

function drawWithRelease(
  id: string,
  release: ReleaseAction,
  kind: StageDraw['kind'] = 'Slot',
): StageDraw {
  return {
    id,
    kind,
    status: 'Cancelled',
    resolutionState: 'Resolved',
    slotPlacements:
      kind === 'Slot'
        ? [{ slotKey: 'S1', entryId: 'e1', displayName: 'A' }]
        : [],
    groupPlacements:
      kind === 'Group'
        ? [{ groupId: 'g1', entryId: 'e1', displayName: 'A' }]
        : [],
    actions: {
      cancel: { state: 'Hidden' },
      release,
      apply: { state: 'Hidden' },
      publishAndApply: { state: 'Hidden' },
      redo: { state: 'Hidden' },
    },
  };
}

describe('releaseFooterMode', () => {
  it('hides when count is 0 even if Blocked', () => {
    const draw = drawWithRelease('d1', {
      state: 'Blocked',
      blockCode: 'Application.DrawReleaseFailure',
      releasableCount: 0,
    });
    expect(releaseFooterMode(draw)).toBe('hidden');
  });

  it('disables only when Blocked with count > 0', () => {
    const draw = drawWithRelease('d1', {
      state: 'Blocked',
      blockCode: 'Stage.MaterializedOpponentConflict',
      releasableCount: 1,
    });
    expect(releaseFooterMode(draw)).toBe('disabled');
  });

  it('enables when Available with count > 0', () => {
    const draw = drawWithRelease('d1', {
      state: 'Available',
      releasableCount: 2,
    });
    expect(releaseFooterMode(draw)).toBe('enabled');
  });
});

describe('resolveServerReleaseCta', () => {
  it('returns empty when candidate release is not Available', () => {
    const overview = {
      releaseCandidateDrawId: 'd-blocked',
      draws: [
        drawWithRelease('d-blocked', {
          state: 'Blocked',
          blockCode: 'Application.DrawReleaseFailure',
          releasableCount: 1,
        }),
      ],
    } satisfies Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>;
    expect(resolveServerReleaseCta(overview).draw).toBeNull();
  });

  it('returns CTA when candidate is Available', () => {
    const overview = {
      releaseCandidateDrawId: 'd-ok',
      draws: [
        drawWithRelease('d-ok', {
          state: 'Available',
          releasableCount: 1,
        }),
      ],
    } satisfies Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>;
    const cta = resolveServerReleaseCta(overview);
    expect(cta.draw?.id).toBe('d-ok');
    expect(cta.disabled).toBe(false);
    expect(cta.aligned).toBe(1);
  });
});
