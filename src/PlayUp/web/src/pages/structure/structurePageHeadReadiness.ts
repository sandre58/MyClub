import type { StageStatus, StructureHubNextStep } from '../../types';

export type AttentionQueryPhase = 'pending' | 'success' | 'error';

export type StructurePageHeadStageSnapshot = {
  status: StageStatus;
};

export type StructurePageHeadReadinessInput = {
  lifecycleClosed: boolean;
  stagesEmpty: boolean;
  structurePresentedDanger: boolean;
  structureHasPresentedProblems: boolean;
  attentionPhase: AttentionQueryPhase;
  presentableBlockingAttentionCount: number;
  presentableNonBlockingAttentionCount: number;
  hubNextStep: StructureHubNextStep | null | undefined;
  stages: StructurePageHeadStageSnapshot[];
};

export type StructurePageHeadNoteKind =
  | 'none'
  | 'negative'
  | 'warning'
  | 'positiveNextStep'
  | 'positiveCalm';

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

/** Hub calm (success pill) — independent from resolver null (see product matrix). */
export function isStructureHubCalm(input: StructurePageHeadReadinessInput): boolean {
  if (input.lifecycleClosed || input.stagesEmpty) {
    return false;
  }

  if (input.hubNextStep) {
    return false;
  }

  if (input.attentionPhase !== 'success') {
    return false;
  }

  if (
    input.presentableBlockingAttentionCount > 0 ||
    input.presentableNonBlockingAttentionCount > 0 ||
    input.structureHasPresentedProblems
  ) {
    return false;
  }

  const statuses = input.stages.map((stage) => stage.status);
  if (statuses.some((status) => status === 'Draft' || status === 'Suspended')) {
    return false;
  }

  if (statuses.length > 0 && statuses.every((status) => status === 'Completed')) {
    return false;
  }

  return statuses.every(
    (status) =>
      status === 'Ready' ||
      status === 'Running' ||
      status === 'Suspended' ||
      status === 'Completed',
  );
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

  if (input.hubNextStep) {
    return 'positiveNextStep';
  }

  if (isStructureHubCalm(input)) {
    return 'positiveCalm';
  }

  return 'none';
}
