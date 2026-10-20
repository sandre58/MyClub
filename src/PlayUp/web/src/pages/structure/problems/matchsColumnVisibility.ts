import type { StructureStageHubSummary, StageStatus } from '../../../types';

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

  if (
    disabledReasonCode === 'ConstructionFrozen' ||
    disabledReasonCode === 'UnmaterializedConfrontations'
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

const LIFECYCLE_REMEDIATION_LABEL_KEYS = new Set([
  'InitialRoundNotPlayable',
  'IncompleteEntryRound',
  'NoDeterminedConfrontations',
]);

export function resolveLifecycleRemediationLabel(
  t: (key: string) => string,
  disabledReasonCode: string,
  remediationLabelKey?: string | null,
): string | null {
  if (disabledReasonCode === 'ConstructionFrozen') {
    return t('lifecycle.statusBand.viewOverview');
  }
  if (
    remediationLabelKey &&
    LIFECYCLE_REMEDIATION_LABEL_KEYS.has(remediationLabelKey)
  ) {
    return t(`lifecycle.remediation.${remediationLabelKey}`);
  }
  return null;
}

export function shouldShowLifecycleRemediationLink(
  disabledReasonCode: string | null | undefined,
  remediationHrefValue: string | null,
  remediationLabelKey?: string | null,
): boolean {
  if (!remediationHrefValue || !disabledReasonCode) {
    return false;
  }
  if (disabledReasonCode === 'ConstructionFrozen') {
    return true;
  }
  return Boolean(
    remediationLabelKey &&
      LIFECYCLE_REMEDIATION_LABEL_KEYS.has(remediationLabelKey),
  );
}

/** Inline revert-disable hints on the control, not as paragraph copy. */
export function shouldShowRevertDisabledParagraph(
  disabledReasonCode: string | null | undefined,
): boolean {
  if (!disabledReasonCode) {
    return false;
  }
  return (
    disabledReasonCode !== 'ProductionPresent' &&
    disabledReasonCode !== 'MatchLifecycleStarted' &&
    disabledReasonCode !== 'MatchLiveInProgress'
  );
}

export function shouldShowDraftPrepareDisabledTooltip(
  disabledReasonCode: string | null | undefined,
  actionEnabled: boolean,
): boolean {
  return Boolean(disabledReasonCode) && !actionEnabled;
}

/** Fiche header — never show lifecycle paragraph copy (tooltips + compact remediation only). */
export function shouldShowHeaderLifecycleInlineCopy(
  _disabledReasonCode: string | null | undefined,
): boolean {
  return false;
}

/** Fiche header lifecycle toolbar — action only (no inline hint or remediation link). */
export function shouldShowHeaderLifecycleRemediation(
  _disabledReasonCode: string | null | undefined,
  _actionEnabled: boolean,
  _remediationHrefValue: string | null,
  _remediationLabelKey?: string | null,
): boolean {
  return false;
}

/** Lifecycle UI in the Matchs operational column. */
export function shouldShowMatchsLifecycle(
  stage: StructureStageHubSummary,
): boolean {
  if (stage.status === 'Completed') {
    return false;
  }
  const lifecycle = stage.lifecycle;
  if (!lifecycle) {
    return true;
  }
  const transition = lifecycle.transition;
  const revert = lifecycle.revertTransition;
  if (transition?.code || revert?.code) {
    return true;
  }
  if (
    (stage.status === 'Running' || stage.status === 'Suspended') &&
    lifecycle.matchProgress != null
  ) {
    return true;
  }
  return stage.status === 'Draft' || stage.status === 'Ready';
}
