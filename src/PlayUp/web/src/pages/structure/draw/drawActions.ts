import type {
  StageDraw,
  StageDrawAction,
  StageDrawActions,
  StageDrawActionState,
  StageOverview,
} from '../../../types';

const hiddenAction: StageDrawAction = { state: 'Hidden' };

const hiddenActions: StageDrawActions = {
  cancel: hiddenAction,
  release: { state: 'Hidden', releasableCount: 0 },
  apply: hiddenAction,
  publishAndApply: hiddenAction,
  redo: hiddenAction,
};

/** Existing error code for a Ready group release blocked by attached matches. */
export const DRAW_RELEASE_ATTACHED_MATCHES =
  'Application.DrawReleaseBlockedByAttachedMatches';

/** Existing error code when another published draw still owns occupancy. */
export const DRAW_RELEASE_FAILURE = 'Application.DrawReleaseFailure';

export const CUP_RELEASE_CONFLICT = 'Stage.MaterializedOpponentConflict';

export type DrawRedoBlockReason =
  'cupFixtureAttached' | 'groupMatchesAttached' | 'lifecycle';

export type DrawRedoPresentation =
  | { kind: 'hidden' }
  | { kind: 'available' }
  | { kind: 'blocked'; reason: DrawRedoBlockReason };

/** Missing `actions` are Hidden so a payload without the projection offers nothing. */
export function drawActionsOf(draw: StageDraw): StageDrawActions {
  return draw.actions ?? hiddenActions;
}

export function isDrawActionAvailable(
  action: { state: StageDrawActionState } | undefined,
): boolean {
  return action?.state === 'Available';
}

/**
 * Footer Libérer. Attached-match blocks stay visible and disabled.
 * Other blocks stay off the footer; the tile carries the existing reason.
 */
export function releaseFooterMode(
  draw: StageDraw,
): 'hidden' | 'enabled' | 'disabled' {
  const release = drawActionsOf(draw).release;
  if (release.releasableCount <= 0) {
    return 'hidden';
  }
  if (release.state === 'Available') {
    return 'enabled';
  }
  if (
    release.state === 'Blocked' &&
    release.blockCode === DRAW_RELEASE_ATTACHED_MATCHES
  ) {
    return 'disabled';
  }
  return 'hidden';
}

export function releaseHoldForDraw(
  draw: StageDraw,
): 'publishedDraw' | 'attachedMatches' | null {
  const release = drawActionsOf(draw).release;
  if (release.blockCode === DRAW_RELEASE_ATTACHED_MATCHES) {
    return 'attachedMatches';
  }
  if (
    release.blockCode === DRAW_RELEASE_FAILURE &&
    release.releasableCount > 0
  ) {
    return 'publishedDraw';
  }
  return null;
}

export function redoPresentation(draw: StageDraw): DrawRedoPresentation {
  const redo = drawActionsOf(draw).redo;
  if (redo.state === 'Available') {
    return { kind: 'available' };
  }
  if (redo.state !== 'Blocked') {
    return { kind: 'hidden' };
  }
  return { kind: 'blocked', reason: redoBlockReason(redo.blockCode) };
}

export function redoBlockReason(
  blockCode: string | null | undefined,
): DrawRedoBlockReason {
  if (blockCode === DRAW_RELEASE_ATTACHED_MATCHES) {
    return 'groupMatchesAttached';
  }
  if (blockCode === CUP_RELEASE_CONFLICT) {
    return 'cupFixtureAttached';
  }
  return 'lifecycle';
}

export interface ServerReleaseCta {
  draw: StageDraw | null;
  aligned: number;
  total: number;
  disabled: boolean;
}

/** Fiche Libérer from `releaseCandidateDrawId` and that draw's release action. */
export function resolveServerReleaseCta(
  overview: Pick<StageOverview, 'draws' | 'releaseCandidateDrawId'>,
): ServerReleaseCta {
  const empty: ServerReleaseCta = {
    draw: null,
    aligned: 0,
    total: 0,
    disabled: false,
  };
  const candidateId = overview.releaseCandidateDrawId;
  if (candidateId == null) {
    return empty;
  }
  const draw = overview.draws.find((item) => item.id === candidateId) ?? null;
  if (draw == null || releaseFooterMode(draw) === 'hidden') {
    return empty;
  }
  const release = drawActionsOf(draw).release;
  const total =
    draw.kind === 'Group'
      ? (draw.groupPlacements?.length ?? 0)
      : draw.slotPlacements.length;
  return {
    draw,
    aligned: release.releasableCount,
    total,
    disabled: releaseFooterMode(draw) === 'disabled',
  };
}
