import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('./http', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./http')>();
  return {
    ...actual,
    postNoContent: vi.fn(),
    sendJson: vi.fn(),
  };
});

import { ApiError, sendJson } from './http';
import {
  createAndGenerateDraw,
  DrawGenerateFailedError,
  isStageDrawLaunchRefusalError,
  isStageDrawRerunLaunchGuardError,
  rerunDrawGeneration,
} from './draws';

const stageId = 'stage-1';
const drawId = 'draw-1';

const LAUNCH_REFUSAL_CODES = [
  'Application.DrawGroupSeatsReservedByInboundFeed',
  'Application.DrawFixedPotConflict',
  'Application.DrawFixedOutsideComposition',
  'Application.DrawFixedExceedsCapacity',
  'Application.DrawGroupShapeInvalid',
  'Application.DrawPotsMissing',
  'Application.DrawObsoleteForConsumption',
  'Stage.NonCancelledDrawAlreadyExists',
] as const;

function refusal(code: string): ApiError {
  return new ApiError(409, 'Conflict', 'refused', code);
}

describe('rerunDrawGeneration', () => {
  beforeEach(() => {
    vi.mocked(sendJson).mockReset();
  });

  it('configures Default inputs and generates the same draw', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockResolvedValueOnce({
        drawId,
        isResolved: true,
        isNoSolution: false,
      });

    await expect(
      rerunDrawGeneration(stageId, drawId, 'Group'),
    ).resolves.toEqual({
      drawId,
      isResolved: true,
      isNoSolution: false,
    });

    expect(sendJson).toHaveBeenNthCalledWith(
      1,
      'POST',
      `/stages/${stageId}/draws/${drawId}/inputs`,
      { intent: 'Default' },
    );
    expect(sendJson).toHaveBeenNthCalledWith(
      2,
      'POST',
      `/stages/${stageId}/draws/${drawId}/generate`,
    );
    expect(vi.mocked(sendJson).mock.calls.map((call) => call[1])).not.toContain(
      `/stages/${stageId}/draws`,
    );
  });

  it('configures Partial inputs when relancing a slot draw', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockResolvedValueOnce({
        drawId,
        isResolved: true,
        isNoSolution: false,
      });

    await rerunDrawGeneration(stageId, drawId, 'Slot');

    expect(sendJson).toHaveBeenNthCalledWith(
      1,
      'POST',
      `/stages/${stageId}/draws/${drawId}/inputs`,
      { intent: 'Partial' },
    );
  });

  it('rethrows StageDrawRerunLaunchGuard codes without wrapping them', async () => {
    const guard = new ApiError(
      409,
      'Conflict',
      'grid',
      'Application.DrawLaunchGridNotVacant',
    );
    vi.mocked(sendJson).mockRejectedValueOnce(guard);

    await expect(rerunDrawGeneration(stageId, drawId, 'Group')).rejects.toBe(
      guard,
    );
    expect(isStageDrawRerunLaunchGuardError(guard)).toBe(true);
    expect(isStageDrawLaunchRefusalError(guard)).toBe(true);
    expect(sendJson).toHaveBeenCalledTimes(1);
  });

  it.each(LAUNCH_REFUSAL_CODES)(
    'rethrows %s from inputs as ApiError',
    async (code) => {
      const error = refusal(code);
      vi.mocked(sendJson).mockRejectedValueOnce(error);

      await expect(rerunDrawGeneration(stageId, drawId, 'Group')).rejects.toBe(
        error,
      );
      expect(error).not.toBeInstanceOf(DrawGenerateFailedError);
    },
  );

  it.each(LAUNCH_REFUSAL_CODES)(
    'rethrows %s from generate as ApiError',
    async (code) => {
      const error = refusal(code);
      vi.mocked(sendJson)
        .mockResolvedValueOnce({ drawId })
        .mockRejectedValueOnce(error);

      await expect(rerunDrawGeneration(stageId, drawId, 'Group')).rejects.toBe(
        error,
      );
    },
  );

  it('wraps a generate failure as DrawGenerateFailedError for the same id', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockRejectedValueOnce(new Error('boom'));

    const error = await rerunDrawGeneration(stageId, drawId, 'Group').catch(
      (cause: unknown) => cause,
    );
    expect(error).toBeInstanceOf(DrawGenerateFailedError);
    expect(error).toMatchObject({ drawId });
  });

  it('wraps an HTTP 500 as DrawGenerateFailedError', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockRejectedValueOnce(new ApiError(500, 'Error', 'down'));

    const error = await rerunDrawGeneration(stageId, drawId, 'Group').catch(
      (cause: unknown) => cause,
    );
    expect(error).toBeInstanceOf(DrawGenerateFailedError);
    expect(error).toMatchObject({ drawId });
  });
});

describe('createAndGenerateDraw', () => {
  beforeEach(() => {
    vi.mocked(sendJson).mockReset();
  });

  it('posts Default to Create and inputs for Groups', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockResolvedValueOnce({ drawId })
      .mockResolvedValueOnce({
        drawId,
        isResolved: true,
        isNoSolution: false,
      });

    await createAndGenerateDraw(stageId, 'Group');

    expect(sendJson).toHaveBeenNthCalledWith(
      1,
      'POST',
      `/stages/${stageId}/draws`,
      { kind: 'Group', intent: 'Default' },
    );
    expect(sendJson).toHaveBeenNthCalledWith(
      2,
      'POST',
      `/stages/${stageId}/draws/${drawId}/inputs`,
      { intent: 'Default' },
    );
    expect(sendJson).toHaveBeenNthCalledWith(
      3,
      'POST',
      `/stages/${stageId}/draws/${drawId}/generate`,
    );
  });

  it('posts Partial to Create and inputs for Slot', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockResolvedValueOnce({ drawId })
      .mockResolvedValueOnce({
        drawId,
        isResolved: true,
        isNoSolution: false,
      });

    await createAndGenerateDraw(stageId, 'Slot');

    expect(sendJson).toHaveBeenNthCalledWith(
      1,
      'POST',
      `/stages/${stageId}/draws`,
      { kind: 'Slot', intent: 'Partial' },
    );
    expect(sendJson).toHaveBeenNthCalledWith(
      2,
      'POST',
      `/stages/${stageId}/draws/${drawId}/inputs`,
      { intent: 'Partial' },
    );
  });

  it('leaves a Create refusal as ApiError', async () => {
    const error = refusal('Application.DrawFixedPotConflict');
    vi.mocked(sendJson).mockRejectedValueOnce(error);

    await expect(createAndGenerateDraw(stageId, 'Group')).rejects.toBe(error);
    expect(sendJson).toHaveBeenCalledTimes(1);
  });

  it.each(LAUNCH_REFUSAL_CODES)(
    'rethrows %s from inputs as ApiError',
    async (code) => {
      const error = refusal(code);
      vi.mocked(sendJson)
        .mockResolvedValueOnce({ drawId })
        .mockRejectedValueOnce(error);

      await expect(createAndGenerateDraw(stageId, 'Group')).rejects.toBe(error);
    },
  );

  it.each(LAUNCH_REFUSAL_CODES)(
    'rethrows %s from generate as ApiError',
    async (code) => {
      const error = refusal(code);
      vi.mocked(sendJson)
        .mockResolvedValueOnce({ drawId })
        .mockResolvedValueOnce({ drawId })
        .mockRejectedValueOnce(error);

      await expect(createAndGenerateDraw(stageId, 'Group')).rejects.toBe(error);
    },
  );

  it('wraps a technical failure after Create and keeps the draw id', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockRejectedValueOnce(new ApiError(500, 'Error', 'down'));

    const error = await createAndGenerateDraw(stageId, 'Group').catch(
      (cause: unknown) => cause,
    );
    expect(error).toBeInstanceOf(DrawGenerateFailedError);
    expect(error).toMatchObject({ drawId });
  });

  it('wraps a network failure after Create and keeps the draw id', async () => {
    vi.mocked(sendJson)
      .mockResolvedValueOnce({ drawId })
      .mockRejectedValueOnce(new TypeError('Failed to fetch'));

    const error = await createAndGenerateDraw(stageId, 'Group').catch(
      (cause: unknown) => cause,
    );
    expect(error).toBeInstanceOf(DrawGenerateFailedError);
    expect(error).toMatchObject({ drawId });
  });
});
