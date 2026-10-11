import type {
  StructureStageHubSummary,
  StructureStageLifecyclePrepareChecklist,
  StructureStageLifecycleTransitionCode,
  StageStatus,
} from '../../types';

export function isLifecycleActionCode(
  code: string | null | undefined,
): code is StructureStageLifecycleTransitionCode {
  return (
    code === 'PrepareStage' ||
    code === 'StartStage' ||
    code === 'CompleteStage'
  );
}

export const DRAFT_PREPARE_STEP_IDS = [
  'Regulation',
  'Teams',
  'Places',
  'Draw',
  'Exits',
  'Matches',
] as const;

export type DraftPrepareStepId = (typeof DRAFT_PREPARE_STEP_IDS)[number];

const STEP_CHECKLIST_CODES: Record<
  DraftPrepareStepId,
  readonly string[]
> = {
  Regulation: [],
  Teams: ['Entries'],
  Places: ['Places'],
  Draw: ['Draw'],
  Exits: ['Exits'],
  Matches: ['Matches'],
};

export type DraftPrepareStepStatus = 'ok' | 'ko' | 'na';

export function resolveDraftPrepareStepStatus(
  stepId: DraftPrepareStepId,
  checklist: StructureStageLifecyclePrepareChecklist | null | undefined,
  disabledReasonCode: string | null | undefined,
): DraftPrepareStepStatus {
  if (stepId === 'Regulation') {
    return disabledReasonCode === 'StructureGraphInvalid' ? 'ko' : 'ok';
  }

  const codes = STEP_CHECKLIST_CODES[stepId];
  const items =
    checklist?.items.filter((item) => codes.includes(item.code)) ?? [];
  if (items.length === 0 || items.every((item) => !item.applicable)) {
    return 'na';
  }
  return items.some((item) => item.applicable && !item.satisfied) ? 'ko' : 'ok';
}

export function shouldShowPrimaryBlockMessage(
  status: StageStatus,
  disabledReasonCode: string | null | undefined,
): boolean {
  if (!disabledReasonCode) {
    return false;
  }
  if (
    status === 'Draft' &&
    (disabledReasonCode === 'StageNotReady' ||
      disabledReasonCode === 'StructureGraphInvalid')
  ) {
    return false;
  }
  if (status === 'Running' && disabledReasonCode === 'OpenMatches') {
    return false;
  }
  if (status === 'Suspended' && disabledReasonCode === 'OpenMatches') {
    return false;
  }
  return true;
}

export function remediationHref(
  competitionId: string,
  stage: StructureStageHubSummary,
): string | null {
  const href = stage.lifecycle?.transition?.remediation?.href;
  if (href && href.startsWith('/')) {
    return href;
  }
  if (
    stage.lifecycle?.transition?.disabledReasonCode === 'ConstructionFrozen'
  ) {
    return `/competitions/${competitionId}/overview`;
  }
  return null;
}

export function matchProgressRemaining(total: number, finished: number, cancelled: number): number {
  return Math.max(0, total - finished - cancelled);
}
