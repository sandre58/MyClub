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

const DRAFT_PREPARE_STEP_CORE = [
  'Regulation',
  'Teams',
  'Places',
  'Draw',
  'Exits',
] as const;

export type DraftPrepareStepId =
  | (typeof DRAFT_PREPARE_STEP_CORE)[number]
  | 'Attribution';

/** @deprecated use {@link resolveDraftPrepareStepIds} */
export const DRAFT_PREPARE_STEP_IDS = DRAFT_PREPARE_STEP_CORE;

/**
 * Prepare checklist rows (Host `BuildPrepareChecklist`) — SPA step mapping:
 * - Regulation: `StructureGraphInvalid` on transition (not a checklist row).
 * - Teams → Entries: root phase only; competition Active entries ≥ MinimumTeams.
 * - Places → Places: expected occupancy (affectation + inbound) vs Places N at root; else composition pool.
 * - Attribution → Attribution: root Groups/Cup; affectation or full composition pool.
 * - Draw → Draw: when draw engaged; badge Applied.
 * - Exits → Exits: qual/prog without exit structure issues.
 * - Matches: not projected — materialization is not required for Prepare (Ready).
 *
 * Inscriptions (Entries) = minimum compétition ; Places = jauge Population vs Places N.
 */
export function resolveDraftPrepareStepIds(
  checklist: StructureStageLifecyclePrepareChecklist | null | undefined,
): DraftPrepareStepId[] {
  const attributionApplicable = checklist?.items.some(
    (item) => item.code === 'Attribution' && item.applicable,
  );
  if (attributionApplicable) {
    return [
      'Regulation',
      'Teams',
      'Places',
      'Attribution',
      'Draw',
      'Exits',
    ];
  }
  return [...DRAFT_PREPARE_STEP_CORE];
}

const STEP_CHECKLIST_CODES: Record<
  DraftPrepareStepId,
  readonly string[]
> = {
  Regulation: [],
  Teams: ['Entries'],
  Places: ['Places'],
  Attribution: ['Attribution'],
  Draw: ['Draw'],
  Exits: ['Exits'],
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
  prepareChecklist?: StructureStageLifecyclePrepareChecklist | null,
): boolean {
  if (!disabledReasonCode) {
    return false;
  }

  if (status === 'Draft') {
    if (disabledReasonCode === 'StageNotReady') {
      return false;
    }
    if (disabledReasonCode === 'StructureGraphInvalid') {
      return (
        resolveDraftPrepareStepStatus(
          'Regulation',
          prepareChecklist,
          disabledReasonCode,
        ) !== 'ko'
      );
    }
    if (disabledReasonCode === 'ConfrontationUndecidable') {
      return (
        resolveDraftPrepareStepStatus('Exits', prepareChecklist, disabledReasonCode) !==
        'ko'
      );
    }
  }

  if (
    (status === 'Running' || status === 'Suspended') &&
    disabledReasonCode === 'OpenMatches'
  ) {
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

export function shouldShowLifecycleRemediationLink(
  disabledReasonCode: string | null | undefined,
  remediationHrefValue: string | null,
): boolean {
  if (!remediationHrefValue || !disabledReasonCode) {
    return false;
  }
  if (disabledReasonCode === 'StageNotReady') {
    return false;
  }
  return true;
}

export function matchProgressRemaining(total: number, finished: number, cancelled: number): number {
  return Math.max(0, total - finished - cancelled);
}
