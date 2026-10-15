export type AttentionQueryPhase = 'pending' | 'success' | 'error';

export type StructurePageHeadReadinessInput = {
  lifecycleClosed: boolean;
  stagesEmpty: boolean;
  /** Structure hub: danger-level presented diagnostics (or shape invalid). */
  structurePresentedDanger: boolean;
  /** Structure hub: any presented problem (including warnings) — blocks positive notes. */
  structureHasPresentedProblems: boolean;
  attentionPhase: AttentionQueryPhase;
  presentableBlockingAttentionCount: number;
  readyToMaterialize: boolean;
  readyForSchedule: boolean;
  readyForNextSlice: boolean;
};

export type StructurePageHeadNoteKind =
  | 'none'
  | 'negative'
  | 'positiveMaterialization'
  | 'positiveNextSlice';

export function resolveStructurePageHeadNoteKind(
  input: StructurePageHeadReadinessInput,
): StructurePageHeadNoteKind {
  if (input.lifecycleClosed || input.stagesEmpty) {
    return 'none';
  }

  const showNegative =
    input.structurePresentedDanger ||
    (input.attentionPhase === 'success' &&
      input.presentableBlockingAttentionCount > 0) ||
    (input.attentionPhase === 'error' && input.structurePresentedDanger);

  if (showNegative) {
    return 'negative';
  }

  if (input.attentionPhase !== 'success') {
    return 'none';
  }

  if (
    input.presentableBlockingAttentionCount === 0 &&
    !input.structureHasPresentedProblems &&
    input.readyToMaterialize &&
    !input.readyForSchedule
  ) {
    return 'positiveMaterialization';
  }

  if (
    input.presentableBlockingAttentionCount === 0 &&
    !input.structureHasPresentedProblems &&
    !input.readyToMaterialize &&
    input.readyForNextSlice
  ) {
    return 'positiveNextSlice';
  }

  return 'none';
}
