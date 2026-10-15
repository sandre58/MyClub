import type { TFunction } from 'i18next';

import { ApiError } from '../../../api';
import type { StructureStageHubSummary } from '../../../types';
import { resolveLifecycleMutationMessage } from './resolveStructureProblemMessage';

const READY_CONSUMER_WIRE = 'Application.ReadyConsumerWouldBreak';

function resolveConsumerStageLabels(
  consumerStageIds: readonly string[],
  stages: readonly StructureStageHubSummary[],
): string[] {
  const nameById = new Map(stages.map((stage) => [stage.stageId, stage.name]));
  return consumerStageIds.map((id) => nameById.get(id) ?? id);
}

/**
 * Structure mutation footer copy — lifecycle/problems namespace plus D1 consumer names when present.
 */
export function formatStructureMutationMessage(
  tStructure: TFunction<'structure'>,
  tCommon: TFunction<'common'>,
  error: unknown,
  stages: readonly StructureStageHubSummary[],
): string {
  const base = resolveLifecycleMutationMessage(tStructure, error);

  if (!(error instanceof ApiError) || error.code !== READY_CONSUMER_WIRE) {
    return wrapWithStatus(tCommon, error, base);
  }

  const ids = error.consumerStageIds;
  if (!ids || ids.length === 0) {
    return wrapWithStatus(tCommon, error, base);
  }

  const labels = resolveConsumerStageLabels(ids, stages);
  const withConsumers = `${base} ${tStructure('problems.lifecycle.ReadyConsumerWouldBreakConsumers', {
    phases: labels.join(', '),
  })}`;
  return wrapWithStatus(tCommon, error, withConsumers);
}

function wrapWithStatus(
  tCommon: TFunction<'common'>,
  error: unknown,
  message: string,
): string {
  if (error instanceof ApiError) {
    return tCommon('errorWithStatus', {
      message,
      status: error.status,
    });
  }
  return message;
}
