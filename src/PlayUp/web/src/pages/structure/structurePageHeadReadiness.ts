export type AttentionQueryPhase = 'pending' | 'success' | 'error';

export type StructurePageHeadReadinessInput = {
  lifecycleClosed: boolean;
  stagesEmpty: boolean;
  hasAny: boolean;
  attentionPhase: AttentionQueryPhase;
  presentableAttentionCount: number;
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
    input.hasAny ||
    (input.attentionPhase === 'success' && input.presentableAttentionCount > 0) ||
    (input.attentionPhase === 'error' && input.hasAny);

  if (showNegative) {
    return 'negative';
  }

  if (input.attentionPhase !== 'success') {
    return 'none';
  }

  if (
    input.presentableAttentionCount === 0 &&
    !input.hasAny &&
    input.readyToMaterialize &&
    !input.readyForSchedule
  ) {
    return 'positiveMaterialization';
  }

  if (
    input.presentableAttentionCount === 0 &&
    !input.hasAny &&
    !input.readyToMaterialize &&
    input.readyForNextSlice
  ) {
    return 'positiveNextSlice';
  }

  return 'none';
}
