import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  completeStage,
  previewDefinitiveResolutionRelease,
  type DefinitiveResolutionReleaseResponse,
} from '../../../api';
import { useStructureStageLifecycleMutation } from './useStructureStageLifecycleMutation';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    completeStage: vi.fn(),
    previewDefinitiveResolutionRelease: vi.fn(),
  };
});

vi.mock('../structureInvalidation', () => ({
  scheduleStructureRefresh: vi.fn(),
}));

const preview: DefinitiveResolutionReleaseResponse = {
  versionToken: 'token',
  releases: [
    {
      stageId: 'dest',
      target: 'Slot',
      slotKey: 'A',
      groupId: null,
      entryId: 'nord',
      entryName: 'Nord',
      stageName: 'Terminal',
      placeLabel: 'A',
      populationRemoved: false,
      sideEffect: false,
    },
  ],
  detachedMatches: [],
  exclusions: [],
  affectedOccupants: [],
};

function createWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
  };
}

describe('useStructureStageLifecycleMutation', () => {
  beforeEach(() => {
    vi.mocked(completeStage).mockReset();
    vi.mocked(previewDefinitiveResolutionRelease).mockReset();
    vi.mocked(completeStage).mockResolvedValue(undefined);
  });

  it('opens the grouped preview after the phase is closed', async () => {
    vi.mocked(previewDefinitiveResolutionRelease).mockResolvedValue(preview);
    const { result } = renderHook(
      () => useStructureStageLifecycleMutation('competition', 'source'),
      { wrapper: createWrapper() },
    );

    result.current.mutate('CompleteStage');

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });
    await waitFor(() => {
      expect(result.current.definitiveOffer?.versionToken).toBe('token');
    });
    expect(completeStage).toHaveBeenCalledWith('source');
    expect(previewDefinitiveResolutionRelease).toHaveBeenCalledWith('source', {
      destinationStageId: null,
    });
  });

  it('still closes the phase when the preview cannot be read', async () => {
    vi.mocked(previewDefinitiveResolutionRelease).mockRejectedValue(
      new Error('preview down'),
    );
    const { result } = renderHook(
      () => useStructureStageLifecycleMutation('competition', 'source'),
      { wrapper: createWrapper() },
    );

    result.current.mutate('CompleteStage');

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });
    expect(result.current.definitiveOffer).toBeNull();
  });
});
