import { describe, expect, it } from 'vitest';
import type { StageDraw, StageOverview } from '../../../types';
import { resolveServerReleaseCta } from '../draw/drawActions';

type ReleaseAction = NonNullable<StageDraw['actions']>['release'];

function drawWithRelease(id: string, release: ReleaseAction): StageDraw {
  return {
    id,
    kind: 'Slot',
    status: 'Cancelled',
    resolutionState: 'Resolved',
    slotPlacements: [{ slotKey: 'S1', entryId: 'e1', displayName: 'A' }],
    groupPlacements: [],
    actions: {
      cancel: { state: 'Hidden' },
      release,
      apply: { state: 'Hidden' },
      publishAndApply: { state: 'Hidden' },
      redo: { state: 'Hidden' },
    },
  };
}

/** Mirrors StructurePhaseFiche showReleaseCta wiring (release CTA + aligned count). */
function ficheShowsReleaseCta(
  overview: Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>,
) {
  const releaseCta = resolveServerReleaseCta(overview);
  return releaseCta.draw != null && releaseCta.aligned > 0;
}

describe('StructurePhaseFiche release CTA (D7)', () => {
  it('hides Libérer when candidate is null despite an older Available draw', () => {
    const overview = {
      releaseCandidateDrawId: null,
      draws: [
        drawWithRelease('older', { state: 'Available', releasableCount: 2 }),
        drawWithRelease('newer', {
          state: 'Blocked',
          blockCode: 'Stage.MaterializedOpponentConflict',
          releasableCount: 1,
        }),
      ],
    } satisfies Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>;
    expect(ficheShowsReleaseCta(overview)).toBe(false);
  });

  it('hides Libérer after refetch when aligned releasable count drops to 0', () => {
    const before = {
      releaseCandidateDrawId: 'd1',
      draws: [
        drawWithRelease('d1', { state: 'Available', releasableCount: 1 }),
      ],
    } satisfies Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>;
    const after = {
      releaseCandidateDrawId: 'd1',
      draws: [drawWithRelease('d1', { state: 'Hidden', releasableCount: 0 })],
    } satisfies Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>;
    expect(ficheShowsReleaseCta(before)).toBe(true);
    expect(ficheShowsReleaseCta(after)).toBe(false);
  });

  it('shows Libérer when candidate is Available with aligned placements', () => {
    const overview = {
      releaseCandidateDrawId: 'd-ok',
      draws: [
        drawWithRelease('d-ok', { state: 'Available', releasableCount: 2 }),
      ],
    } satisfies Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>;
    expect(ficheShowsReleaseCta(overview)).toBe(true);
  });
});

describe('StructurePhaseFiche focus target after release', () => {
  it('uses the operational view-draw-workflow selector', () => {
    const selector =
      '[data-structure-phase-operational] button[data-action="view-draw-workflow"]:not(:disabled)';
    document.body.innerHTML = `<div data-structure-phase-operational><button type="button" data-action="view-draw-workflow">Voir le tirage</button></div>`;
    const target = document.querySelector<HTMLButtonElement>(selector);
    expect(target).not.toBeNull();
    expect(target?.dataset.action).toBe('view-draw-workflow');
  });
});
