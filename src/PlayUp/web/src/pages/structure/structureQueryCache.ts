import type { QueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../queryKeys';
import type { StructureStageHubSummary, StructureView } from '../../types';
import {
  invalidateStructureQueries,
  scheduleStructureRefresh,
  type StructureInvalidationScope,
} from './structureInvalidation';

function readStructureView(
  queryClient: QueryClient,
  competitionId: string,
): StructureView | undefined {
  return queryClient.getQueryData<StructureView>(
    queryKeys.competitions.structure(competitionId),
  );
}

export function patchStructureViewStage(
  queryClient: QueryClient,
  competitionId: string,
  stageId: string,
  patch: Partial<StructureStageHubSummary>,
): StructureView | undefined {
  const current = readStructureView(queryClient, competitionId);
  if (!current) {
    return undefined;
  }
  const next: StructureView = {
    ...current,
    stages: current.stages.map((stage) =>
      stage.stageId === stageId ? { ...stage, ...patch } : stage,
    ),
  };
  queryClient.setQueryData(queryKeys.competitions.structure(competitionId), next);
  return next;
}

export type StructureHubPatchSession = {
  competitionId: string;
  stageId?: string;
  previous: StructureView | undefined;
  refreshScopes: StructureInvalidationScope[];
};

export function beginStructureHubPatch(
  queryClient: QueryClient,
  competitionId: string,
  refreshScopes: StructureInvalidationScope[],
  stageId?: string,
): StructureHubPatchSession {
  return {
    competitionId,
    stageId,
    previous: readStructureView(queryClient, competitionId),
    refreshScopes,
  };
}

export function applyStructureHubPatch(
  queryClient: QueryClient,
  session: StructureHubPatchSession,
  patch: Partial<StructureStageHubSummary>,
): void {
  if (!session.stageId) {
    return;
  }
  patchStructureViewStage(
    queryClient,
    session.competitionId,
    session.stageId,
    patch,
  );
}

export function commitStructureHubPatch(
  queryClient: QueryClient,
  session: StructureHubPatchSession,
): void {
  scheduleStructureRefresh(
    queryClient,
    session.competitionId,
    session.refreshScopes,
    session.stageId ? { stageId: session.stageId } : undefined,
  );
}

export async function reconcileStructureHubPatch(
  queryClient: QueryClient,
  session: StructureHubPatchSession,
): Promise<boolean> {
  try {
    await invalidateStructureQueries(
      queryClient,
      session.competitionId,
      session.refreshScopes,
      session.stageId ? { stageId: session.stageId } : undefined,
    );
    return true;
  } catch {
    if (session.previous === undefined) {
      queryClient.removeQueries({
        queryKey: queryKeys.competitions.structure(session.competitionId),
      });
    } else {
      queryClient.setQueryData(
        queryKeys.competitions.structure(session.competitionId),
        session.previous,
      );
    }
    return false;
  }
}

export function patchStageNameOptimistic(
  queryClient: QueryClient,
  competitionId: string,
  stageId: string,
  name: string,
): StructureHubPatchSession {
  const session = beginStructureHubPatch(queryClient, competitionId, ['hub'], stageId);
  patchStructureViewStage(queryClient, competitionId, stageId, { name });
  commitStructureHubPatch(queryClient, session);
  void reconcileStructureHubPatch(queryClient, session);
  return session;
}

export function patchDrawRulesActivatedOptimistic(
  queryClient: QueryClient,
  competitionId: string,
  stageId: string,
  numberOfPots: number | null | undefined,
): StructureHubPatchSession {
  const session = beginStructureHubPatch(
    queryClient,
    competitionId,
    ['hubStageShell'],
    stageId,
  );
  patchStructureViewStage(queryClient, competitionId, stageId, {
    hasDrawRules: true,
    ...(numberOfPots != null ? { numberOfPots } : {}),
  });
  commitStructureHubPatch(queryClient, session);
  void reconcileStructureHubPatch(queryClient, session);
  return session;
}

export function patchDrawRulesDeactivatedOptimistic(
  queryClient: QueryClient,
  competitionId: string,
  stageId: string,
): StructureHubPatchSession {
  const session = beginStructureHubPatch(
    queryClient,
    competitionId,
    ['hubStageShell'],
    stageId,
  );
  patchStructureViewStage(queryClient, competitionId, stageId, {
    hasDrawRules: false,
    numberOfPots: null,
  });
  commitStructureHubPatch(queryClient, session);
  void reconcileStructureHubPatch(queryClient, session);
  return session;
}
