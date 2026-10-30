/**
 * Draw create / generate / publish / apply orchestration.
 */
import type { DrawResolutionState, DrawStatus } from '../types/enums';
import { ApiError, postNoContent, sendJson } from './http';

/**
 * ConfigureDrawInputs refusals that stay as {@link ApiError}:
 * Rerun vacant-grid guard, and partial slot refusals (no free slot / count mismatch).
 */
const STAGE_DRAW_RERUN_LAUNCH_GUARD_CODES = new Set([
  'Application.DrawLaunchGridNotVacant',
  'Application.DrawLaunchDirectAssignmentPresent',
  'Application.DrawNoFreeSlot',
  'Application.DrawNotEnoughTeams',
  'Application.DrawTooManyTeams',
]);

export function isStageDrawRerunLaunchGuardError(
  error: unknown,
): error is ApiError {
  return (
    error instanceof ApiError &&
    error.code != null &&
    STAGE_DRAW_RERUN_LAUNCH_GUARD_CODES.has(error.code)
  );
}

/** POST /stages/{stageId}/draws/{drawId}/publish → 204 */
export function publishDraw(stageId: string, drawId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/publish`);
}

/**
 * Happy path — Publish then Apply (Host orchestration, two durable steps).
 * Not Domain-atomic: Apply failure leaves Published + not applied; resume with applyDraw.
 * POST /stages/{stageId}/draws/{drawId}/publish-and-apply → 204
 */
export function publishAndApplyDraw(
  stageId: string,
  drawId: string,
): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/publish-and-apply`);
}

/** POST /stages/{stageId}/draws/{drawId}/cancel → 204 */
export function cancelDraw(stageId: string, drawId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/cancel`);
}

/**
 * Release Places still exactly matching this Slot draw's resolution.
 * POST /stages/{stageId}/draws/{drawId}/release-aligned-placements → 200
 */
export function releaseDrawAlignedPlacements(
  stageId: string,
  drawId: string,
): Promise<{ releasedCount: number; skippedCount: number }> {
  return sendJson(
    'POST',
    `/stages/${stageId}/draws/${drawId}/release-aligned-placements`,
  );
}

/** POST /stages/{stageId}/draws → 201 DrawSummary */
export function createDraw(
  stageId: string,
  kind: 'Slot' | 'Group',
  intent: 'Default' | 'Rerun' | 'Partial' = 'Default',
): Promise<{ drawId: string }> {
  return sendJson('POST', `/stages/${stageId}/draws`, { kind, intent });
}

/** POST /stages/{stageId}/draws/{drawId}/inputs → DrawSummary */
export function configureDrawInputs(
  stageId: string,
  drawId: string,
  intent: 'Default' | 'Rerun' | 'Partial' = 'Default',
): Promise<{ drawId: string }> {
  return sendJson('POST', `/stages/${stageId}/draws/${drawId}/inputs`, {
    intent,
  });
}

/** POST /stages/{stageId}/draws/{drawId}/generate → DrawGeneration */
export function generateDraw(
  stageId: string,
  drawId: string,
): Promise<{
  drawId: string;
  isResolved: boolean;
  isNoSolution: boolean;
}> {
  return sendJson('POST', `/stages/${stageId}/draws/${drawId}/generate`);
}

/**
 * One UI gesture for a new draw: Create → inputs → Generate.
 * Slot uses Partial (free slots only). Group uses Rerun (full reshuffle).
 * Generate is not exposed as a separate product action.
 *
 * If Create succeeded but inputs/Generate fail technically, throws
 * {@link DrawGenerateFailedError} with the created drawId (Draft orphan — SPA projects it).
 */
export async function createAndGenerateDraw(
  stageId: string,
  kind: 'Slot' | 'Group',
): Promise<{ drawId: string; isResolved: boolean; isNoSolution: boolean }> {
  const intent = kind === 'Slot' ? 'Partial' : 'Rerun';
  const created = await createDraw(stageId, kind, intent);
  try {
    await configureDrawInputs(stageId, created.drawId, intent);
    const generated = await generateDraw(stageId, created.drawId);
    return {
      drawId: created.drawId,
      isResolved: generated.isResolved,
      isNoSolution: generated.isNoSolution,
    };
  } catch (cause) {
    throw new DrawGenerateFailedError(created.drawId, cause);
  }
}

/**
 * Relance a draft on the same draw id: inputs then Generate.
 * Slot uses Partial; Group uses Rerun. Does not create another execution.
 * Launch-guard and partial refusals stay {@link ApiError}.
 * Any other failure is wrapped as {@link DrawGenerateFailedError} for that id.
 */
export async function rerunDrawGeneration(
  stageId: string,
  drawId: string,
  kind: 'Slot' | 'Group',
): Promise<{
  drawId: string;
  isResolved: boolean;
  isNoSolution: boolean;
}> {
  const intent = kind === 'Slot' ? 'Partial' : 'Rerun';
  try {
    await configureDrawInputs(stageId, drawId, intent);
    return await generateDraw(stageId, drawId);
  } catch (cause) {
    if (isStageDrawRerunLaunchGuardError(cause)) {
      throw cause;
    }
    throw new DrawGenerateFailedError(drawId, cause);
  }
}

/** Create succeeded; Generate (or inputs) failed — Draft exists server-side. */
export class DrawGenerateFailedError extends Error {
  readonly drawId: string;
  readonly cause: unknown;

  constructor(drawId: string, cause?: unknown) {
    super('Draw generation failed after create');
    this.name = 'DrawGenerateFailedError';
    this.drawId = drawId;
    this.cause = cause;
  }
}

export function isDrawGenerateFailedError(
  error: unknown,
): error is DrawGenerateFailedError {
  return error instanceof DrawGenerateFailedError;
}

/** POST /stages/{stageId}/draws/{drawId}/apply → 204 */
export function applyDraw(stageId: string, drawId: string): Promise<void> {
  return postNoContent(`/stages/${stageId}/draws/${drawId}/apply`);
}

/**
 * Refaire — single atomic server transaction: cancel the draw, release its aligned
 * placements, then preview a new resolution (create → Rerun inputs → generate) for the
 * same draw kind. All-or-nothing: a failure at any step persists nothing.
 * POST /stages/{stageId}/draws/{drawId}/redo → 200
 */
export function redoDraw(
  stageId: string,
  drawId: string,
): Promise<{
  drawId: string;
  isResolved: boolean;
  isNoSolution: boolean;
  status: DrawStatus;
  resolutionState: DrawResolutionState;
}> {
  return sendJson('POST', `/stages/${stageId}/draws/${drawId}/redo`);
}
