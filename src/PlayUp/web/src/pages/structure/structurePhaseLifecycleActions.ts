import type { TFunction } from 'i18next';

import type { StructureStageLifecycleTransitionCode } from '../../types';

export function lifecycleDisabledMessage(
  t: TFunction<'structure'>,
  code: string | null | undefined,
  fallback?: string | null,
): string {
  if (!code) {
    return fallback ?? '';
  }
  return t(`lifecycle.disabled.${code}`, { defaultValue: fallback ?? code });
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
