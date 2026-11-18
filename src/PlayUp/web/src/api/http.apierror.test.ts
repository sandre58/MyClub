import { describe, expect, it } from 'vitest';
import { ApiError, throwIfNotOk } from './http';

describe('ApiError.entryId', () => {
  it('reads entryId at the problem root and from extensions', async () => {
    const root = new Response(
      JSON.stringify({
        title: 'Conflict',
        code: 'Stage.GroupFull',
        entryId: 'entry-root',
      }),
      { status: 409, headers: { 'Content-Type': 'application/json' } },
    );
    await expect(throwIfNotOk(root)).rejects.toMatchObject({
      entryId: 'entry-root',
      code: 'Stage.GroupFull',
    });

    const ext = new Response(
      JSON.stringify({
        title: 'Conflict',
        extensions: { code: 'Stage.GroupFull', entryId: 'entry-ext' },
      }),
      { status: 409, headers: { 'Content-Type': 'application/json' } },
    );
    await expect(throwIfNotOk(ext)).rejects.toMatchObject({
      entryId: 'entry-ext',
      code: 'Stage.GroupFull',
    });
  });

  it('omits entryId without breaking code resolution', async () => {
    const response = new Response(
      JSON.stringify({
        title: 'Locked',
        extensions: { code: 'Stage.StructureLocked' },
      }),
      { status: 409, headers: { 'Content-Type': 'application/json' } },
    );
    try {
      await throwIfNotOk(response);
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      const api = error as ApiError;
      expect(api.code).toBe('Stage.StructureLocked');
      expect(api.entryId).toBeUndefined();
    }
  });
});
