import type { StructureView } from '../types';

/** True when the progression destination targets phase Population (no Place). */
export function isPopulationDestination(
  slotKey: string | null | undefined,
): boolean {
  return slotKey == null || slotKey.trim() === '';
}

/**
 * Upstream stage names that have Progression paths targeting this phase's Population.
 * Graph rule context only — not per-entry provenance (A2 option 2).
 */
export function inboundPopulationProgressionSourceNames(
  data: StructureView,
  stageId: string,
): string[] {
  const names: string[] = [];
  const seen = new Set<string>();
  for (const source of data.stages) {
    for (const path of source.progressionPaths ?? []) {
      if (path.destinationStageId !== stageId) {
        continue;
      }
      if (!isPopulationDestination(path.destinationSlotKey)) {
        continue;
      }
      if (seen.has(source.stageId)) {
        continue;
      }
      seen.add(source.stageId);
      names.push(source.name);
    }
  }
  return names;
}
