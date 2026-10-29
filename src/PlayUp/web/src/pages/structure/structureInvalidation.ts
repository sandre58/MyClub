import type { QueryClient, QueryKey } from '@tanstack/react-query';
import { queryKeys } from '../../queryKeys';

export type StructureInvalidationScope =
  | 'hub'
  | 'hubStage'
  | 'hubShell'
  /** Stage overview only (draw list in workflow dialog). */
  | 'stageOverview'
  | 'competitionReads'
  | 'graph'
  /** hub + stage detail + schematic + attention (deduped union). */
  | 'hubStageShell';

const INVALIDATE_OPTIONS = {
  refetchType: 'active' as const,
};

type InvalidateDescriptor =
  | { kind: 'key'; queryKey: QueryKey }
  | {
      kind: 'predicate';
      predicate: (query: { queryKey: QueryKey }) => boolean;
    };

function keySignature(descriptor: InvalidateDescriptor): string {
  if (descriptor.kind === 'key') {
    return JSON.stringify(descriptor.queryKey);
  }
  return 'predicate:all-schematics';
}

function expandScope(
  scope: StructureInvalidationScope,
  competitionId: string,
  stageId: string | undefined,
): InvalidateDescriptor[] {
  switch (scope) {
    case 'hub':
      return [
        {
          kind: 'key',
          queryKey: queryKeys.competitions.structure(competitionId),
        },
      ];
    case 'hubStage':
      if (!stageId) {
        throw new Error('hubStage invalidation requires stageId');
      }
      return [
        {
          kind: 'key',
          queryKey: queryKeys.competitions.structure(competitionId),
        },
        { kind: 'key', queryKey: queryKeys.stages.detail(stageId) },
        { kind: 'key', queryKey: queryKeys.stages.schematic(stageId) },
      ];
    case 'hubShell':
      if (!stageId) {
        throw new Error('hubShell invalidation requires stageId');
      }
      return [
        {
          kind: 'key',
          queryKey: queryKeys.competitions.structure(competitionId),
        },
        { kind: 'key', queryKey: queryKeys.stages.detail(stageId) },
        {
          kind: 'key',
          queryKey: queryKeys.competitions.attention(competitionId),
        },
      ];
    case 'stageOverview':
      if (!stageId) {
        throw new Error('stageOverview invalidation requires stageId');
      }
      return [{ kind: 'key', queryKey: queryKeys.stages.detail(stageId) }];
    case 'hubStageShell':
      if (!stageId) {
        throw new Error('hubStageShell invalidation requires stageId');
      }
      return [
        ...expandScope('hubStage', competitionId, stageId),
        ...expandScope('hubShell', competitionId, stageId),
      ];
    case 'competitionReads':
      return [
        {
          kind: 'key',
          queryKey: queryKeys.competitions.structure(competitionId),
        },
        {
          kind: 'key',
          queryKey: queryKeys.competitions.detail(competitionId),
        },
        {
          kind: 'key',
          queryKey: queryKeys.competitions.overview(competitionId),
        },
        {
          kind: 'key',
          queryKey: queryKeys.competitions.consultation(competitionId),
        },
        {
          kind: 'key',
          queryKey: queryKeys.competitions.attention(competitionId),
        },
      ];
    case 'graph':
      return [
        ...expandScope('competitionReads', competitionId, stageId),
        {
          kind: 'key',
          queryKey: ['stages'],
        },
        {
          kind: 'predicate',
          predicate: (query) =>
            Array.isArray(query.queryKey) &&
            query.queryKey[0] === 'stages' &&
            query.queryKey[2] === 'schematic',
        },
      ];
    default: {
      const _exhaustive: never = scope;
      return _exhaustive;
    }
  }
}

/** Dedupes invalidate descriptors when multiple scopes are combined. */
export function collectStructureInvalidationDescriptors(
  scopes: StructureInvalidationScope[],
  competitionId: string,
  options?: { stageId?: string },
): InvalidateDescriptor[] {
  const stageId = options?.stageId;
  const seen = new Set<string>();
  const out: InvalidateDescriptor[] = [];
  for (const scope of scopes) {
    for (const descriptor of expandScope(scope, competitionId, stageId)) {
      const sig = keySignature(descriptor);
      if (seen.has(sig)) {
        continue;
      }
      seen.add(sig);
      out.push(descriptor);
    }
  }
  return out;
}

const STAGE_READ_OVERVIEW_ACTIONS = new Set([
  'MaterializeMatches',
  'MaterializeFromOccupiedSlots',
  'PrepareStage',
  'StartStage',
  'ApplyDraw',
  'PublishAndApplyDraw',
  'ApplyQualification',
  'ApplyProgression',
]);

/** Overview commands whose success changes stage draw actions. */
export function overviewActionInvalidatesStageReads(code: string): boolean {
  return STAGE_READ_OVERVIEW_ACTIONS.has(code);
}

export async function invalidateStructureQueries(
  queryClient: QueryClient,
  competitionId: string,
  scopes: StructureInvalidationScope[],
  options?: { stageId?: string },
): Promise<void> {
  const descriptors = collectStructureInvalidationDescriptors(
    scopes,
    competitionId,
    options,
  );
  await Promise.all(
    descriptors.map((descriptor) =>
      descriptor.kind === 'key'
        ? queryClient.invalidateQueries({
            queryKey: descriptor.queryKey,
            ...INVALIDATE_OPTIONS,
          })
        : queryClient.invalidateQueries({
            predicate: descriptor.predicate,
            ...INVALIDATE_OPTIONS,
          }),
    ),
  );
}

function logStructureRefreshError(error: unknown) {
  console.error('[structureInvalidation] scheduled refresh failed', error);
}

export function scheduleQueryRefresh(
  queryClient: QueryClient,
  queryKey: QueryKey,
): void {
  void queryClient
    .invalidateQueries({ queryKey, ...INVALIDATE_OPTIONS })
    .catch(logStructureRefreshError);
}

/** Invalidates without blocking callers; absorbs promise rejections. */
export function scheduleStructureRefresh(
  queryClient: QueryClient,
  competitionId: string,
  scopes: StructureInvalidationScope[],
  options?: { stageId?: string },
): void {
  void invalidateStructureQueries(
    queryClient,
    competitionId,
    scopes,
    options,
  ).catch(logStructureRefreshError);
}
