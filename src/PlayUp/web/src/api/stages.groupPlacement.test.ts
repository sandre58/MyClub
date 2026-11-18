import { afterEach, describe, expect, it, vi } from 'vitest';
import { assignEntryToGroup, clearEntryFromGroup } from './stages';

describe('group placement routes', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('puts an entry in a group with no body', async () => {
    const fetchMock = vi.fn(async () => new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);

    await assignEntryToGroup('stage id', 'group/a', 'entry b');

    expect(fetchMock).toHaveBeenCalledWith(
      '/stages/stage id/groups/group%2Fa/entries/entry%20b',
      expect.objectContaining({ method: 'PUT', body: undefined }),
    );
  });

  it('clears an entry from a group with no body', async () => {
    const fetchMock = vi.fn(async () => new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);

    await clearEntryFromGroup('stage', 'group', 'entry');

    expect(fetchMock).toHaveBeenCalledWith(
      '/stages/stage/groups/group/entries/entry',
      expect.objectContaining({ method: 'DELETE', body: undefined }),
    );
  });
});
