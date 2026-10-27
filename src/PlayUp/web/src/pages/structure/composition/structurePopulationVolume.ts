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
} from '../../../types';
import {
  progressionEdgeDestination,
  progressionEdges,
  progressionEdgeVolume,
  qualificationEdgeDestination,
  qualificationEdges,
  qualificationEdgeVolume,
} from '../intents/structureStageQualProgProjection';

/** Volume of Entries promised by one qualification path. */
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
    for (const edge of qualificationEdges(source)) {
      if (qualificationEdgeDestination(edge) !== destinationStageId) continue;
      n += qualificationEdgeVolume(edge);
    }
    for (const edge of progressionEdges(source)) {
      if (progressionEdgeDestination(edge) !== destinationStageId) continue;
      n += progressionEdgeVolume(edge);
    }
  }
  return n;
}

/** U1 meter — affectation authoring + inbound Qual/Prog configured volume. */
export function expectedPopulationOccupancy(
  data: StructureView,
  destination: StructureStageHubSummary,
): number {
  return (
    (destination.affectationEntryCount ?? 0) +
    inboundPopulationConfiguredVolume(data, destination.stageId)
  );
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
  const affectation = destination.affectationEntryCount ?? 0;
  let qualFromOthers = 0;
  let progInbound = 0;

  for (const source of data.stages) {
    for (const edge of qualificationEdges(source)) {
      if (qualificationEdgeDestination(edge) !== destination.stageId) continue;
      if (source.stageId === sourceStageId) continue;
      qualFromOthers += qualificationEdgeVolume(edge);
    }
    for (const edge of progressionEdges(source)) {
      if (progressionEdgeDestination(edge) !== destination.stageId) continue;
      progInbound += progressionEdgeVolume(edge);
    }
  }

  return (
    affectation + qualFromOthers + Math.max(0, draftQualVolume) + progInbound
  );
}

/**
 * Expected Places N occupancy of `destination` while authoring Prog from `sourceStageId`.
 * Persisted Prog from the current source is replaced by `draftProgVolume` (Z).
 *
 * X = Affectation + Qual→dest (all) + Prog(others) + Z
 */
export function expectedPopulationWithProgDraft(args: {
  data: StructureView;
  destination: StructureStageHubSummary;
  sourceStageId: string;
  draftProgVolume: number;
}): number {
  const { data, destination, sourceStageId, draftProgVolume } = args;
  const affectation = destination.affectationEntryCount ?? 0;
  let qualInbound = 0;
  let progFromOthers = 0;

  for (const source of data.stages) {
    for (const edge of qualificationEdges(source)) {
      if (qualificationEdgeDestination(edge) !== destination.stageId) continue;
      qualInbound += qualificationEdgeVolume(edge);
    }
    for (const edge of progressionEdges(source)) {
      if (progressionEdgeDestination(edge) !== destination.stageId) continue;
      if (source.stageId === sourceStageId) continue;
      progFromOthers += progressionEdgeVolume(edge);
    }
  }

  return (
    affectation + qualInbound + progFromOthers + Math.max(0, draftProgVolume)
  );
}
