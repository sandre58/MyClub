/** Cancel succeeded and a later Refaire step did not. The draw id is unchanged. */
export class DrawRedoStoppedError extends Error {
  readonly step: 'release' | 'preview';
  readonly cause: unknown;

  constructor(step: 'release' | 'preview', cause: unknown) {
    super(
      step === 'release'
        ? 'Draw redo stopped before placements were released'
        : 'Draw redo stopped before a new preview',
    );
    this.name = 'DrawRedoStoppedError';
    this.step = step;
    this.cause = cause;
  }
}

/**
 * Refaire orchestration: cancel, then release, then preview.
 * Whether those steps are allowed is projected by the server. This function
 * only stops at the first failing step.
 */
export async function redoAppliedDraw<T>(input: {
  cancel: () => Promise<unknown>;
  release: () => Promise<unknown>;
  preview: () => Promise<T>;
}): Promise<T> {
  await input.cancel();
  try {
    await input.release();
  } catch (cause) {
    throw new DrawRedoStoppedError('release', cause);
  }
  try {
    return await input.preview();
  } catch (cause) {
    throw new DrawRedoStoppedError('preview', cause);
  }
}
