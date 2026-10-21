import type { StructureReadiness } from '../types';

/** Default StructureReadiness for SPA tests (mirrors Host wire shape). */
export function structureReadiness(
  overrides: Partial<StructureReadiness> = {},
): StructureReadiness {
  return {
    readyForNextSlice: false,
    readyForDraw: false,
    readyForMaterialization: false,
    readyForSchedule: false,
    readyForMatchOperation: false,
    readyForSchedulePath: false,
    attachedMatchCount: 0,
    blockers: [],
    blockerDetails: [],
    ...overrides,
  };
}
