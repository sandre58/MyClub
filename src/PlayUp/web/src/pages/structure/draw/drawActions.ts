import type { TFunction } from 'i18next';
import type {
  StageDraw,
  StageDrawAction,
  StageDrawActions,
  StageDrawActionState,
  StageOverview,
} from '../../../types';
import { apiErrorCodeLabel } from '../../../i18n/apiErrorLabel';
import {
  DRAW_LAUNCH_DIRECT_ASSIGNMENT,
  DRAW_LAUNCH_GRID_NOT_VACANT,
} from '../../../api/draws';
import { drawExecutionNumber } from './drawUi';

const DRAW_NO_FREE_SLOT = 'Application.DrawNoFreeSlot';
const DRAW_NOT_ENOUGH_TEAMS = 'Application.DrawNotEnoughTeams';
const DRAW_TOO_MANY_TEAMS = 'Application.DrawTooManyTeams';

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

const DRAW_GROUP_SEATS_RESERVED =
  'Application.DrawGroupSeatsReservedByInboundFeed';
const DRAW_FIXED_POT_CONFLICT = 'Application.DrawFixedPotConflict';
const DRAW_FIXED_OUTSIDE_COMPOSITION =
  'Application.DrawFixedOutsideComposition';
const DRAW_FIXED_EXCEEDS_CAPACITY = 'Application.DrawFixedExceedsCapacity';
const DRAW_GROUP_SHAPE_INVALID = 'Application.DrawGroupShapeInvalid';
const DRAW_POTS_MISSING = 'Application.DrawPotsMissing';

export type DrawRedoBlockReason =
  | 'cupFixtureAttached'
  | 'groupMatchesAttached'
  | 'launchGridNotVacant'
  | 'launchDirectAssignment'
  | 'reservedSeats'
  | 'fixedPotConflict'
  | 'fixedOutsideComposition'
  | 'fixedExceedsCapacity'
  | 'groupShape'
  | 'missingPots'
  | 'noFreeSlot'
  | 'notEnoughTeams'
  | 'tooManyTeams'
  | 'lifecycle';

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
 * Footer Libérer. Shown only for a real reliquat: releasableCount > 0 and Available (enabled),
 * or Blocked with count > 0 (disabled). Count 0 hides the CTA; Blocked with zero count stays off
 * the footer (tile / history badge carry the state).
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
  if (release.state === 'Blocked' && release.releasableCount > 0) {
    return 'disabled';
  }
  return 'hidden';
}

export function releaseBlockReason(draw: StageDraw): string | null {
  const release = drawActionsOf(draw).release;
  if (release.state !== 'Blocked' || !release.blockCode) {
    return null;
  }
  return apiErrorCodeLabel(release.blockCode) ?? release.blockCode;
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
  if (blockCode === DRAW_LAUNCH_GRID_NOT_VACANT) {
    return 'launchGridNotVacant';
  }
  if (blockCode === DRAW_LAUNCH_DIRECT_ASSIGNMENT) {
    return 'launchDirectAssignment';
  }
  if (blockCode === DRAW_NO_FREE_SLOT) {
    return 'noFreeSlot';
  }
  if (blockCode === DRAW_NOT_ENOUGH_TEAMS) {
    return 'notEnoughTeams';
  }
  if (blockCode === DRAW_TOO_MANY_TEAMS) {
    return 'tooManyTeams';
  }
  if (blockCode === DRAW_GROUP_SEATS_RESERVED) {
    return 'reservedSeats';
  }
  if (blockCode === DRAW_FIXED_POT_CONFLICT) {
    return 'fixedPotConflict';
  }
  if (blockCode === DRAW_FIXED_OUTSIDE_COMPOSITION) {
    return 'fixedOutsideComposition';
  }
  if (blockCode === DRAW_FIXED_EXCEEDS_CAPACITY) {
    return 'fixedExceedsCapacity';
  }
  if (blockCode === DRAW_GROUP_SHAPE_INVALID) {
    return 'groupShape';
  }
  if (blockCode === DRAW_POTS_MISSING) {
    return 'missingPots';
  }
  return 'lifecycle';
}

export interface ServerReleaseCta {
  draw: StageDraw | null;
  aligned: number;
  total: number;
  disabled: boolean;
  disabledReason: string | null;
}

/** Drawn targets for this execution (server `drawnTargetCount`, else test fallback). */
export function releaseDrawnTargetCount(draw: StageDraw): number {
  const fromServer = drawActionsOf(draw).release.drawnTargetCount;
  if (fromServer !== undefined) {
    return fromServer;
  }
  if (draw.kind === 'Group') {
    return draw.groupPlacements?.length ?? 0;
  }
  return draw.slotPlacements.length;
}

function cancelledReleasableDraws(drawList: readonly StageDraw[]): StageDraw[] {
  return drawList.filter(
    (item) =>
      item.status === 'Cancelled' && releaseFooterMode(item) !== 'hidden',
  );
}

/** Libérer CTA label shared by dialog, fiche, and confirmations. */
export function formatReleaseDrawnTeamsLabel(
  t: TFunction,
  draw: StageDraw,
  drawList: readonly StageDraw[],
): string {
  const count = drawActionsOf(draw).release.releasableCount;
  const base =
    count === 1
      ? t('draw:releaseDrawnTeam')
      : t('draw:releaseDrawnTeams', { n: count });
  if (cancelledReleasableDraws(drawList).length > 1) {
    return `${base} (#${drawExecutionNumber(drawList, draw.id)})`;
  }
  return base;
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
    disabledReason: null,
  };
  const candidateId = overview.releaseCandidateDrawId;
  if (candidateId == null) {
    return empty;
  }
  const draw = overview.draws.find((item) => item.id === candidateId) ?? null;
  if (draw == null || releaseFooterMode(draw) !== 'enabled') {
    return empty;
  }
  const release = drawActionsOf(draw).release;
  const total = releaseDrawnTargetCount(draw);
  return {
    draw,
    aligned: release.releasableCount,
    total,
    disabled: releaseFooterMode(draw) === 'disabled',
    disabledReason: releaseBlockReason(draw),
  };
}
