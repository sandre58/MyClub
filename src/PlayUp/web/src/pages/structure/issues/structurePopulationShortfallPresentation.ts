import type { StructureStageHubSummary } from '../../../types';

export type StructureDiagnosticIssueCopyContext = {
  compositionEntryCount?: number;
  compositionCapacity?: number | null;
};

/** Operational composition band is filled (includes forfeit / ineligible entries). */
export function stageOperationalCompositionFull(
  stage: StructureDiagnosticIssueCopyContext,
): boolean {
  const capacity = stage.compositionCapacity;
  if (capacity == null || capacity <= 0) {
    return false;
  }
  const entries = stage.compositionEntryCount ?? 0;
  return entries >= capacity;
}

export function structureDiagnosticIssueCopyFromStage(
  stage: Pick<
    StructureStageHubSummary,
    'compositionEntryCount' | 'compositionCapacity'
  >,
): StructureDiagnosticIssueCopyContext {
  return {
    compositionEntryCount: stage.compositionEntryCount,
    compositionCapacity: stage.compositionCapacity,
  };
}

export function populationShortfallUsesFullCompositionCopy(
  code: string,
  copyContext?: StructureDiagnosticIssueCopyContext,
): boolean {
  return (
    code === 'PopulationShortfall' &&
    copyContext != null &&
    stageOperationalCompositionFull(copyContext)
  );
}
