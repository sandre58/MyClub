import { describe, expect, it, vi } from 'vitest';
import { redoAppliedDraw } from './drawRedo';
import { redoDraw } from '../../../api';

vi.mock('../../../api', () => ({
  redoDraw: vi.fn(),
}));

describe('redoAppliedDraw', () => {
  it('delegates to the single atomic redo endpoint', async () => {
    vi.mocked(redoDraw).mockResolvedValue({
      drawId: 'draw-next',
      isResolved: true,
      isNoSolution: false,
      status: 'Draft',
      resolutionState: 'Resolved',
    });

    const result = await redoAppliedDraw('stage-1', 'draw-1');

    expect(redoDraw).toHaveBeenCalledWith('stage-1', 'draw-1');
    expect(redoDraw).toHaveBeenCalledTimes(1);
    expect(result).toEqual({
      drawId: 'draw-next',
      isResolved: true,
      isNoSolution: false,
      status: 'Draft',
      resolutionState: 'Resolved',
    });
  });

  it('propagates a no-solution outcome without throwing', async () => {
    vi.mocked(redoDraw).mockResolvedValue({
      drawId: 'draw-next',
      isResolved: false,
      isNoSolution: true,
      status: 'Draft',
      resolutionState: 'NoSolution',
    });

    const result = await redoAppliedDraw('stage-1', 'draw-1');

    expect(result.isNoSolution).toBe(true);
  });

  it('propagates a failure from the endpoint unchanged', async () => {
    const cause = new Error('redo refused');
    vi.mocked(redoDraw).mockRejectedValue(cause);

    await expect(redoAppliedDraw('stage-1', 'draw-1')).rejects.toBe(cause);
  });
});
