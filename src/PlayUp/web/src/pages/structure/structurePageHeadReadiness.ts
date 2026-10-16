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
  presentableNonBlockingAttentionCount: number;
  readyToMaterialize: boolean;
  readyForSchedule: boolean;
  readyForNextSlice: boolean;
};

export type StructurePageHeadNoteKind =
  | 'none'
  | 'negative'
  | 'warning'
  | 'positiveMaterialization'
  | 'positiveNextSlice';

function pageHeadHasErrors(input: StructurePageHeadReadinessInput): boolean {
  return (
    input.structurePresentedDanger ||
    (input.attentionPhase === 'success' &&
      input.presentableBlockingAttentionCount > 0) ||
    (input.attentionPhase === 'error' && input.structurePresentedDanger)
  );
}

function pageHeadHasWarnings(input: StructurePageHeadReadinessInput): boolean {
  const structureWarnings =
    input.structureHasPresentedProblems && !input.structurePresentedDanger;
  const attentionWarnings =
    input.attentionPhase === 'success' &&
    input.presentableNonBlockingAttentionCount > 0;
  return structureWarnings || attentionWarnings;
}

export function resolveStructurePageHeadNoteKind(
  input: StructurePageHeadReadinessInput,
): StructurePageHeadNoteKind {
  if (input.lifecycleClosed || input.stagesEmpty) {
    return 'none';
  }

  if (pageHeadHasErrors(input)) {
    return 'negative';
  }

  if (pageHeadHasWarnings(input)) {
    return 'warning';
  }

  if (input.attentionPhase !== 'success') {
    return 'none';
  }

  if (
    input.presentableBlockingAttentionCount === 0 &&
    input.presentableNonBlockingAttentionCount === 0 &&
    !input.structureHasPresentedProblems &&
    input.readyToMaterialize &&
    !input.readyForSchedule
  ) {
    return 'positiveMaterialization';
  }

  if (
    input.presentableBlockingAttentionCount === 0 &&
    input.presentableNonBlockingAttentionCount === 0 &&
    !input.structureHasPresentedProblems &&
    !input.readyToMaterialize &&
    input.readyForNextSlice
  ) {
    return 'positiveNextSlice';
  }

  return 'none';
}
