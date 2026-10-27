import type { StructureFormatKind } from '../../../types';

/**
 * Mirrors Domain {@code StageClassification.IsNonClassifyingPhase} for Structure UX policy.
 * Cup / KO (rounds, not Swiss) — placement awards gate Prepare; classifying phases do not.
 */
export function isNonClassifyingStructurePhase(stage: {
  formatKind?: StructureFormatKind | null;
}): boolean {
  return stage.formatKind === 'Cup';
}
