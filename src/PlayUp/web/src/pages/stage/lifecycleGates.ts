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
 * Release CTA: Cancelled Slot or Group draw still aligning with stage
 * occupancy, under the same competition × stage gates as Cancel
 * (Draft or Ready). The server may still refuse a Group release while
 * the stage is Ready and matches are attached.
 */
export function canReleaseDrawAlignedPlacements(input: {
  competitionStatus: CompetitionStatus;
  stageStatus: StageStatus;
  drawStatus: DrawStatus;
  drawKind: 'Slot' | 'Group';
  alignedPlacementCount: number;
  /** When true, occupancy is owned by a live Published+Applied execution — no release on Cancelled history. */
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
