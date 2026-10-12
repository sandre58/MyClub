import type { TFunction } from 'i18next';

import { ApiError } from '../../api';
import { apiErrorLabel } from '../../i18n/apiErrorLabel';
import type { StructureStageLifecycleTransitionCode } from '../../types';

const STAGE_COMPLETION_BLOCKED = 'Application.StageCompletionBlocked';

export function lifecycleDisabledMessage(
  t: TFunction<'structure'>,
  code: string | null | undefined,
): string {
  if (!code) {
    return '';
  }
  const specific = t(`lifecycle.disabled.${code}`, { defaultValue: '' });
  if (specific.length > 0) {
    return specific;
  }
  return t('lifecycle.disabled._unknown');
}

export function lifecycleMutationMessage(
  tStructure: TFunction<'structure'>,
  error: unknown,
): string {
  if (error instanceof ApiError) {
    if (error.code === STAGE_COMPLETION_BLOCKED) {
      return tStructure('lifecycle.disabled.OpenMatches');
    }
    const localized = apiErrorLabel(error);
    if (localized) {
      return localized;
    }
    return tStructure('lifecycle.mutation._unknown');
  }
  if (error instanceof Error) {
    return error.message;
  }
  return tStructure('lifecycle.mutation._unknown');
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

export function transitionPendingLabel(
  t: TFunction<'structure'>,
  code: StructureStageLifecycleTransitionCode,
): string {
  switch (code) {
    case 'PrepareStage':
      return t('lifecycle.actions.preparing');
    case 'StartStage':
      return t('lifecycle.actions.starting');
    case 'CompleteStage':
      return t('lifecycle.actions.completing');
    default:
      return t('working');
  }
}

export function transitionSuccessToast(
  t: TFunction<'structure'>,
  code: StructureStageLifecycleTransitionCode,
): string {
  switch (code) {
    case 'PrepareStage':
      return t('lifecycle.toast.prepared');
    case 'StartStage':
      return t('lifecycle.toast.started');
    case 'CompleteStage':
      return t('lifecycle.toast.completed');
    default:
      return t('lifecycle.toast.updated');
  }
}
