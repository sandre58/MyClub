import { QueryClient } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';

import { queryKeys } from '../../queryKeys';
import {
  collectStructureInvalidationDescriptors,
  invalidateStructureQueries,
  overviewActionInvalidatesStageReads,
  scheduleStructureRefresh,
} from './structureInvalidation';

describe('structureInvalidation', () => {
  const competitionId = '11111111-1111-1111-1111-111111111111';
  const stageId = '22222222-2222-2222-2222-222222222222';

  it('hub scope invalidates structure only', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    await invalidateStructureQueries(queryClient, competitionId, ['hub']);

    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.competitions.structure(competitionId),
      refetchType: 'active',
    });
  });

  it('stageOverview scope invalidates stage detail only', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    await invalidateStructureQueries(
      queryClient,
      competitionId,
      ['stageOverview'],
      {
        stageId,
      },
    );

    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.stages.detail(stageId),
      refetchType: 'active',
    });
  });

  it('hubStage scope includes stage detail and schematic', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    await invalidateStructureQueries(queryClient, competitionId, ['hubStage'], {
      stageId,
    });

    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.stages.schematic(stageId),
      refetchType: 'active',
    });
    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.stages.detail(stageId),
      refetchType: 'active',
    });
    expect(spy).not.toHaveBeenCalledWith({
      queryKey: queryKeys.competitions.overview(competitionId),
      refetchType: 'active',
    });
  });

  it('hubShell scope includes attention not schematic', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    await invalidateStructureQueries(queryClient, competitionId, ['hubShell'], {
      stageId,
    });

    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.competitions.attention(competitionId),
      refetchType: 'active',
    });
    expect(spy).not.toHaveBeenCalledWith({
      queryKey: queryKeys.stages.schematic(stageId),
      refetchType: 'active',
    });
  });

  it('dedupes hubStage and hubShell when combined', () => {
    const descriptors = collectStructureInvalidationDescriptors(
      ['hubStage', 'hubShell'],
      competitionId,
      { stageId },
    );
    const structureKeys = descriptors.filter(
      (d) =>
        d.kind === 'key' &&
        JSON.stringify(d.queryKey) ===
          JSON.stringify(queryKeys.competitions.structure(competitionId)),
    );
    expect(structureKeys).toHaveLength(1);
    expect(descriptors).toHaveLength(4);
  });

  it('hubStageShell matches merged hubStage and hubShell keys', () => {
    const merged = collectStructureInvalidationDescriptors(
      ['hubStage', 'hubShell'],
      competitionId,
      { stageId },
    );
    const composite = collectStructureInvalidationDescriptors(
      ['hubStageShell'],
      competitionId,
      { stageId },
    );
    expect(composite.map((d) => JSON.stringify(d))).toEqual(
      merged.map((d) => JSON.stringify(d)),
    );
  });

  it('competitionReads includes overview and not stage schematic', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    await invalidateStructureQueries(queryClient, competitionId, [
      'competitionReads',
    ]);

    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.competitions.overview(competitionId),
      refetchType: 'active',
    });
    expect(spy).toHaveBeenCalledWith({
      queryKey: queryKeys.competitions.consultation(competitionId),
      refetchType: 'active',
    });
  });

  it('graph scope invalidates the stages prefix and schematic predicate', async () => {
    const queryClient = new QueryClient();
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    await invalidateStructureQueries(queryClient, competitionId, ['graph']);

    expect(spy).toHaveBeenCalledWith({
      queryKey: ['stages'],
      refetchType: 'active',
    });
    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        refetchType: 'active',
        predicate: expect.any(Function),
      }),
    );
  });

  it('overview commands that change stage reads invalidate the stages prefix', () => {
    expect(overviewActionInvalidatesStageReads('ApplyDraw')).toBe(true);
    expect(overviewActionInvalidatesStageReads('PublishAndApplyDraw')).toBe(
      true,
    );
    expect(overviewActionInvalidatesStageReads('PrepareStage')).toBe(true);
    expect(overviewActionInvalidatesStageReads('AddEntry')).toBe(false);
  });

  it('scheduleStructureRefresh absorbs rejected invalidation', async () => {
    const queryClient = new QueryClient();
    const error = new Error('network');
    vi.spyOn(queryClient, 'invalidateQueries').mockRejectedValue(error);
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    scheduleStructureRefresh(queryClient, competitionId, ['hub']);

    await vi.waitFor(() => {
      expect(consoleSpy).toHaveBeenCalledWith(
        '[structureInvalidation] scheduled refresh failed',
        error,
      );
    });

    consoleSpy.mockRestore();
  });
});
