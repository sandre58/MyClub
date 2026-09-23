import type { CompetitionStatus, DrawStatus, StageStatus } from '../types';

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
 * `CancelDraw`. Option C (fixtures / Live|Finished matches) is intentionally
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
