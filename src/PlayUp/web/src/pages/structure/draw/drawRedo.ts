import { redoDraw } from '../../../api';
import type { DrawResolutionState, DrawStatus } from '../../../types/enums';

/**
 * Refaire an applied draw.
 *
 * Server-atomic (D4B): one `POST …/redo` call runs cancel, release aligned placements, and
 * preview a new resolution in a single transaction. A failure at any step persists nothing —
 * there is no longer a partial "cancelled but not released" or "released but no new preview"
 * state to recover from, so there is no step-specific stopped error.
 */
export function redoAppliedDraw(
  stageId: string,
  drawId: string,
): Promise<{
  drawId: string;
  isResolved: boolean;
  isNoSolution: boolean;
  status: DrawStatus;
  resolutionState: DrawResolutionState;
}> {
  return redoDraw(stageId, drawId);
}
