import type { TFunction } from 'i18next';

import {
  apiErrorLabel,
  genericNetworkLabel,
  isGenericNetworkFailure,
} from '../../../i18n/apiErrorLabel';
import { ApiError } from '../../../api';
import type { CompetitionStatus, OrganizerProblemRead } from '../../../types';
import {
  isPhaseRosterIncompleteCode,
  resolvePhaseRosterIncompleteOperationalMessageKey,
} from '../composition/structurePhaseRosterIncompletePresentation';
import type { StructureDiagnosticIssueCopyContext } from '../issues/structurePopulationShortfallPresentation';
import { LIFECYCLE_UNKNOWN_KEY, MUTATION_UNKNOWN_KEY } from './constants';
import { normalizeLifecycleAnticipated } from './normalizeLifecycleAnticipated';
import { normalizeLifecycleMutationError } from './normalizeLifecycleMutation';

export function resolveLifecycleAnticipatedMessage(
  t: TFunction<'structure'>,
  wireCode: string,
  problem?: OrganizerProblemRead | null,
  rosterCopyContext?: StructureDiagnosticIssueCopyContext,
): string {
  if (isPhaseRosterIncompleteCode(wireCode)) {
    const key =
      resolvePhaseRosterIncompleteOperationalMessageKey(rosterCopyContext);
    const specific = t(key, { defaultValue: '' });
    if (specific.length > 0) {
      return specific;
    }
  }

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
  if (isGenericNetworkFailure(error)) {
    return genericNetworkLabel();
  }

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
    const detail = error.detail?.trim();
    if (detail) {
      return detail;
    }
  }

  return t(MUTATION_UNKNOWN_KEY);
}

const DRAFT_DETAIL_KEY = 'lifecycle.state.Draft.detail';
const DRAFT_DETAIL_LIVE_KEY =
  'lifecycle.state.Draft.detailUnderLiveCompetition';

/** Running or Suspended competition: Draft copy stays on operational authoring. */
export function isLiveCompetitionForDraftCopy(
  status: CompetitionStatus | null | undefined,
): boolean {
  return status === 'Running' || status === 'Suspended';
}

function draftDetailKey(competitionStatus?: CompetitionStatus | null): string {
  return isLiveCompetitionForDraftCopy(competitionStatus)
    ? DRAFT_DETAIL_LIVE_KEY
    : DRAFT_DETAIL_KEY;
}

/** Draft Prepare empty-state body (product: StageNotReady → aggregate draft detail). */
export function resolveDraftPrepareBodyMessage(
  t: TFunction<'structure'>,
  showBlockMessage: boolean,
  disabledReason: string | null,
  problem?: OrganizerProblemRead | null,
  rosterCopyContext?: StructureDiagnosticIssueCopyContext,
  competitionStatus?: CompetitionStatus | null,
): string {
  const detailKey = draftDetailKey(competitionStatus);
  if (showBlockMessage && disabledReason) {
    if (disabledReason === 'StageNotReady') {
      return t(detailKey);
    }
    return resolveLifecycleAnticipatedMessage(
      t,
      disabledReason,
      problem,
      rosterCopyContext,
    );
  }
  return t(detailKey);
}
