import { describe, expect, it, vi } from 'vitest';
import { DrawRedoStoppedError, redoAppliedDraw } from './drawRedo';

describe('redoAppliedDraw', () => {
  it('runs cancel, then release, then preview', async () => {
    const order: string[] = [];
    const result = await redoAppliedDraw({
      cancel: async () => {
        order.push('cancel');
      },
      release: async () => {
        order.push('release');
      },
      preview: async () => {
        order.push('preview');
        return 'previewed';
      },
    });
    expect(order).toEqual(['cancel', 'release', 'preview']);
    expect(result).toBe('previewed');
  });

  it('stops at release and keeps the cause', async () => {
    const preview = vi.fn();
    const cause = new Error('release refused');
    const act = redoAppliedDraw({
      cancel: async () => undefined,
      release: async () => {
        throw cause;
      },
      preview,
    });
    await expect(act).rejects.toMatchObject({
      name: 'DrawRedoStoppedError',
      step: 'release',
      cause,
    });
    expect(preview).not.toHaveBeenCalled();
  });

  it('stops at preview after release succeeded', async () => {
    const cause = new Error('preview refused');
    const act = redoAppliedDraw({
      cancel: async () => undefined,
      release: async () => undefined,
      preview: async () => {
        throw cause;
      },
    });
    await expect(act).rejects.toBeInstanceOf(DrawRedoStoppedError);
    await expect(act).rejects.toMatchObject({ step: 'preview', cause });
  });
});
