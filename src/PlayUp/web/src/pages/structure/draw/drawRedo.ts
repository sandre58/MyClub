import { redoDraw } from '../../../api';

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
): Promise<{ drawId: string; isResolved: boolean; isNoSolution: boolean }> {
  return redoDraw(stageId, drawId);
}
