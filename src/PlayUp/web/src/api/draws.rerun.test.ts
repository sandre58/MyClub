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
  DrawGenerateFailedError,
  isStageDrawRerunLaunchGuardError,
  rerunDrawGeneration,
} from './draws';

const stageId = 'stage-1';
const drawId = 'draw-1';

describe('rerunDrawGeneration', () => {
  beforeEach(() => {
    vi.mocked(sendJson).mockReset();
  });

  it('configures Rerun inputs and generates the same draw', async () => {
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
      { intent: 'Rerun' },
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
    expect(sendJson).toHaveBeenCalledTimes(1);
  });

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
});
