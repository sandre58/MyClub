import type { CompetitionStatus, DrawStatus, StageStatus } from '../../types';

/**
 * Mirrors Host `EnsureCompetitionAllowsLifecycleMutation`
 * (blocks Completed | Archived only).
 */
export function allowsCompetitionLifecycleMutation(
  status: CompetitionStatus,
): boolean {
  return status !== 'Completed' && status !== 'Archived';
}

/**
 * Mirrors Domain `Stage.EnsureDraftOrReady`.
 */
export function allowsStageDraftOrReadyMutation(status: StageStatus): boolean {
  return status === 'Draft' || status === 'Ready';
}

/**
 * Draw execution statuses that Domain `Draw.Cancel` accepts
 * (Cancelled is a no-op server-side — no Cancel CTA).
 */
export function isDrawExecutionCancellable(status: DrawStatus): boolean {
  return status === 'Draft' || status === 'Published';
}

/**
 * SPA Cancel CTA for a draw execution — same conjunction as Host + Domain
 * `CancelDraw`. Fixtures and Live or Finished matches are intentionally
 * out of scope.
 */
export function canCancelDrawExecution(input: {
  competitionStatus: CompetitionStatus;
  stageStatus: StageStatus;
  drawStatus: DrawStatus;
}): boolean {
  return (
    allowsCompetitionLifecycleMutation(input.competitionStatus) &&
    allowsStageDraftOrReadyMutation(input.stageStatus) &&
    isDrawExecutionCancellable(input.drawStatus)
  );
}

/**
 * Group release is refused while the phase is Ready and a match is attached.
 * The button stays visible; the caller disables it and shows the reason.
 */
export function groupReleaseBlockedByAttachedMatches(input: {
  drawKind: 'Slot' | 'Group';
  stageStatus: StageStatus;
  attachedMatchCount: number;
}): boolean {
  return (
    input.drawKind === 'Group' &&
    input.stageStatus === 'Ready' &&
    input.attachedMatchCount > 0
  );
}

/** True when Libérer should be offered, before the attached-match disable. */
export function releaseDrawPlacementsVisible(input: {
  competitionStatus: CompetitionStatus;
  stageStatus: StageStatus;
  drawStatus: DrawStatus;
  drawKind: 'Slot' | 'Group';
  alignedPlacementCount: number;
  stageHasPublishedAppliedDraw?: boolean;
}): boolean {
  return (
    allowsCompetitionLifecycleMutation(input.competitionStatus) &&
    allowsStageDraftOrReadyMutation(input.stageStatus) &&
    !input.stageHasPublishedAppliedDraw &&
    input.drawStatus === 'Cancelled' &&
    (input.drawKind === 'Slot' || input.drawKind === 'Group') &&
    input.alignedPlacementCount > 0
  );
}

/**
 * Release CTA: Cancelled Slot or Group draw still aligning with stage
 * occupancy, under the same competition × stage gates as Cancel
 * (Draft or Ready). Group + Ready + attached matches is visible but not allowed.
 */
export function canReleaseDrawAlignedPlacements(input: {
  competitionStatus: CompetitionStatus;
  stageStatus: StageStatus;
  drawStatus: DrawStatus;
  drawKind: 'Slot' | 'Group';
  alignedPlacementCount: number;
  /** When true, occupancy is owned by a live Published+Applied execution — no release on Cancelled history. */
  stageHasPublishedAppliedDraw?: boolean;
  /** Host matchCount. Group + Ready + a positive count disables Libérer. */
  attachedMatchCount?: number;
}): boolean {
  if (!releaseDrawPlacementsVisible(input)) {
    return false;
  }
  return !groupReleaseBlockedByAttachedMatches({
    drawKind: input.drawKind,
    stageStatus: input.stageStatus,
    attachedMatchCount: input.attachedMatchCount ?? 0,
  });
}
