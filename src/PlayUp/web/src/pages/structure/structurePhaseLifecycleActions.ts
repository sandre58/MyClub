import type { TFunction } from 'i18next';

import type {
  StructureStageLifecycleMutationCode,
  StructureStageLifecycleRevertCode,
  StructureStageLifecycleTransitionCode,
} from '../../types';
import {
  resolveLifecycleAnticipatedMessage,
  resolveLifecycleMutationMessage,
} from './problems';

/** @deprecated Prefer {@link resolveLifecycleAnticipatedMessage} from `./problems`. */
export function lifecycleDisabledMessage(
  t: TFunction<'structure'>,
  code: string | null | undefined,
): string {
  if (!code) {
    return '';
  }
  return resolveLifecycleAnticipatedMessage(t, code);
}

/** @deprecated Prefer {@link resolveLifecycleMutationMessage} from `./problems`. */
export function lifecycleMutationMessage(
  tStructure: TFunction<'structure'>,
  error: unknown,
): string {
  return resolveLifecycleMutationMessage(tStructure, error);
}

export function transitionActionLabel(
  t: TFunction<'structure'>,
  code: StructureStageLifecycleTransitionCode,
): string {
  switch (code) {
    case 'PrepareStage':
      return t('lifecycle.actions.prepare');
    case 'StartStage':
      return t('lifecycle.actions.start');
    case 'CompleteStage':
      return t('lifecycle.actions.complete');
    default:
      return code;
  }
}

export function revertActionLabel(
  t: TFunction<'structure'>,
  code: StructureStageLifecycleRevertCode,
): string {
  switch (code) {
    case 'RevertToDraftStage':
      return t('lifecycle.actions.revertToDraft');
    case 'RevertToReadyStage':
      return t('lifecycle.actions.cancelStart');
    case 'SuspendStage':
      return t('lifecycle.actions.suspend');
    case 'ResumeStage':
      return t('lifecycle.actions.resume');
    default:
      return code;
  }
}

export function mutationPendingLabel(
  t: TFunction<'structure'>,
  code: StructureStageLifecycleMutationCode,
): string {
  switch (code) {
    case 'PrepareStage':
      return t('lifecycle.actions.preparing');
    case 'StartStage':
      return t('lifecycle.actions.starting');
    case 'CompleteStage':
      return t('lifecycle.actions.completing');
    case 'RevertToDraftStage':
      return t('lifecycle.actions.revertingToDraft');
    case 'RevertToReadyStage':
      return t('lifecycle.actions.cancelStarting');
    case 'SuspendStage':
      return t('lifecycle.actions.suspending');
    case 'ResumeStage':
      return t('lifecycle.actions.resuming');
    default:
      return t('working');
  }
}

export function mutationSuccessToast(
  t: TFunction<'structure'>,
  code: StructureStageLifecycleMutationCode,
): string {
  switch (code) {
    case 'PrepareStage':
      return t('lifecycle.toast.prepared');
    case 'StartStage':
      return t('lifecycle.toast.started');
    case 'CompleteStage':
      return t('lifecycle.toast.completed');
    case 'RevertToDraftStage':
      return t('lifecycle.toast.revertedToDraft');
    case 'RevertToReadyStage':
      return t('lifecycle.toast.cancelledStart');
    case 'SuspendStage':
      return t('lifecycle.toast.suspended');
    case 'ResumeStage':
      return t('lifecycle.toast.resumed');
    default:
      return t('lifecycle.toast.updated');
  }
}

/** @deprecated use {@link mutationPendingLabel} */
export const transitionPendingLabel = mutationPendingLabel;

/** @deprecated use {@link mutationSuccessToast} */
export const transitionSuccessToast = mutationSuccessToast;
