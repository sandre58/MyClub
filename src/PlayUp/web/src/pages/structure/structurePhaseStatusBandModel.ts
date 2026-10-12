import type {
  StructureStageHubSummary,
  StructureStageLifecycleMutationCode,
  StructureStageLifecycleRevertCode,
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

export function isLifecycleRevertCode(
  code: string | null | undefined,
): code is StructureStageLifecycleRevertCode {
  return code === 'RevertToDraftStage' || code === 'RevertToReadyStage';
}

export function isLifecycleMutationCode(
  code: string | null | undefined,
): code is StructureStageLifecycleMutationCode {
  return isLifecycleActionCode(code) || isLifecycleRevertCode(code);
}

export function shouldShowPrimaryBlockMessage(
  status: StageStatus,
  disabledReasonCode: string | null | undefined,
): boolean {
  if (!disabledReasonCode) {
    return false;
  }

  if (
    (status === 'Running' || status === 'Suspended') &&
    disabledReasonCode === 'OpenMatches'
  ) {
    return false;
  }

  return true;
}

export function shouldShowRevertBlockMessage(
  disabledReasonCode: string | null | undefined,
): boolean {
  return Boolean(disabledReasonCode);
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

export function shouldShowLifecycleRemediationLink(
  disabledReasonCode: string | null | undefined,
  remediationHrefValue: string | null,
): boolean {
  if (!remediationHrefValue || !disabledReasonCode) {
    return false;
  }
  return true;
}

export function matchProgressRemaining(total: number, finished: number, cancelled: number): number {
  return Math.max(0, total - finished - cancelled);
}
