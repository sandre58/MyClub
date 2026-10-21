import type {
  StructureStageLifecycleMutationCode,
  StructureStageLifecycleRevertCode,
  StructureStageLifecycleTransitionCode,
} from '../../types';

export {
  remediationHref,
  shouldShowLifecycleRemediationLink,
  shouldShowMatchsLifecycle,
  shouldShowPrimaryBlockMessage,
  shouldShowRevertBlockMessage,
  shouldShowRevertDisabledParagraph,
} from './problems/matchsColumnVisibility';

export function isLifecycleActionCode(
  code: string | null | undefined,
): code is StructureStageLifecycleTransitionCode {
  return (
    code === 'PrepareStage' || code === 'StartStage' || code === 'CompleteStage'
  );
}

export function isLifecycleRevertCode(
  code: string | null | undefined,
): code is StructureStageLifecycleRevertCode {
  return (
    code === 'RevertToDraftStage' ||
    code === 'RevertToReadyStage' ||
    code === 'SuspendStage' ||
    code === 'ResumeStage'
  );
}

export function isLifecycleMutationCode(
  code: string | null | undefined,
): code is StructureStageLifecycleMutationCode {
  return isLifecycleActionCode(code) || isLifecycleRevertCode(code);
}

export function matchProgressRemaining(
  total: number,
  finished: number,
  cancelled: number,
): number {
  return Math.max(0, total - finished - cancelled);
}

/** Exploitation gauge: finished + cancelled (matches no longer open). */
export function matchProgressClosed(
  finished: number,
  cancelled: number,
): number {
  return finished + cancelled;
}

export function matchProgressAllClosed(
  total: number,
  finished: number,
  cancelled: number,
  open: number,
): boolean {
  return (
    total > 0 && open === 0 && matchProgressClosed(finished, cancelled) >= total
  );
}
