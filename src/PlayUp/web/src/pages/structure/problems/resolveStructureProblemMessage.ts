import type { TFunction } from 'i18next';

import { apiErrorLabel } from '../../../i18n/apiErrorLabel';
import { ApiError } from '../../../api';
import type { OrganizerProblemRead } from '../../../types';
import { LIFECYCLE_UNKNOWN_KEY, MUTATION_UNKNOWN_KEY } from './constants';
import { normalizeLifecycleAnticipated } from './normalizeLifecycleAnticipated';
import { normalizeLifecycleMutationError } from './normalizeLifecycleMutation';

export function resolveLifecycleAnticipatedMessage(
  t: TFunction<'structure'>,
  wireCode: string,
  problem?: OrganizerProblemRead | null,
): string {
  const { messageKey } = normalizeLifecycleAnticipated(wireCode, problem);
  const specific = t(messageKey, { defaultValue: '' });
  if (specific.length > 0) {
    return specific;
  }
  return t(LIFECYCLE_UNKNOWN_KEY);
}

export function resolveLifecycleMutationMessage(
  t: TFunction<'structure'>,
  error: unknown,
): string {
  if (error instanceof Error && !(error instanceof ApiError)) {
    return error.message;
  }

  const normalized = normalizeLifecycleMutationError(error);
  const fromProblems = t(normalized.messageKey, { defaultValue: '' });
  if (fromProblems.length > 0) {
    return fromProblems;
  }

  if (error instanceof ApiError) {
    const fromErrors = apiErrorLabel(error);
    if (fromErrors) {
      return fromErrors;
    }
  }

  return t(MUTATION_UNKNOWN_KEY);
}

const DRAFT_DETAIL_KEY = 'lifecycle.state.Draft.detail';

/** Draft Prepare empty-state body (product: StageNotReady → aggregate draft detail). */
export function resolveDraftPrepareBodyMessage(
  t: TFunction<'structure'>,
  showBlockMessage: boolean,
  disabledReason: string | null,
  problem?: OrganizerProblemRead | null,
): string {
  if (showBlockMessage && disabledReason) {
    if (disabledReason === 'StageNotReady') {
      return t(DRAFT_DETAIL_KEY);
    }
    return resolveLifecycleAnticipatedMessage(t, disabledReason, problem);
  }
  return t(DRAFT_DETAIL_KEY);
}
