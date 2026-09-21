// -----------------------------------------------------------------------
// Expected Places N occupancy (meter C / U1 capacity).
// Affectation + Qual + Prog (Population and Place). Qual draft substitutes
// persisted Qual from the authoring source only.
// -----------------------------------------------------------------------

import type {
  SelectionMode,
  StructureQualificationPath,
  StructureStageHubSummary,
  StructureView,
} from '../types';

/** Volume of Entrées promised by one qualification path. */
export function qualificationPathVolume(
  path: StructureQualificationPath,
): number {
  switch (path.selectionMode as SelectionMode) {
    case 'Position':
      return 1;
    case 'Range': {
      const end = path.selectionEndValue ?? path.selectionValue;
      return Math.max(1, end - path.selectionValue + 1);
    }
    case 'Top':
    case 'Bottom':
    case 'Best':
    case 'Worst':
    default:
      return Math.max(1, path.selectionValue || 1);
  }
}

/**
 * Configured inbound volume toward a phase Places N (Draft expected occupancy).
 * Qual (all sources) + Prog (Population and Place) — Place fills occupy capacity too.
 */
export function inboundPopulationConfiguredVolume(
  data: StructureView,
  destinationStageId: string,
): number {
  let n = 0;
  for (const source of data.stages) {
    for (const path of source.qualificationPaths ?? []) {
      if (path.destinationStageId !== destinationStageId) continue;
      n += qualificationPathVolume(path);
    }
    for (const path of source.progressionPaths ?? []) {
      if (path.destinationStageId !== destinationStageId) continue;
      n += 1;
    }
  }
  return n;
}

/**
 * Expected Places N occupancy of `destination` while authoring Qual from `sourceStageId`.
 * Persisted Qual from the current source is replaced by `draftQualVolume` (Z).
 *
 * X = Affectation + Qual(others) + Z + Prog→dest (all sources, Population and Place)
 */
export function expectedPopulationWithQualDraft(args: {
  data: StructureView;
  destination: StructureStageHubSummary;
  sourceStageId: string;
  draftQualVolume: number;
}): number {
  const { data, destination, sourceStageId, draftQualVolume } = args;
  const affectation = destination.compositionEntryCount ?? 0;
  let qualFromOthers = 0;
  let progInbound = 0;

  for (const source of data.stages) {
    for (const path of source.qualificationPaths ?? []) {
      if (path.destinationStageId !== destination.stageId) continue;
      if (source.stageId === sourceStageId) continue;
      qualFromOthers += qualificationPathVolume(path);
    }
    for (const path of source.progressionPaths ?? []) {
      if (path.destinationStageId !== destination.stageId) continue;
      progInbound += 1;
    }
  }

  return (
    affectation +
    qualFromOthers +
    Math.max(0, draftQualVolume) +
    progInbound
  );
}
