import type { StageStatus } from '../../types';
import { isSuspendedStageStatus } from './structureOperationalMeterPresentation';
import {
  stageOperationalCompositionFull,
  type StructureDiagnosticIssueCopyContext,
} from './structurePopulationShortfallPresentation';
type IssueTitleVariant = 'long' | 'short';

export type PhaseRosterIncompleteCopyVariant =
  'bandIncomplete' | 'eligibilityGap' | 'neutral';

const CODE = 'PhaseRosterIncomplete';

export function isPhaseRosterIncompleteCode(code: string): boolean {
  return code === CODE;
}

/**
 * Presentation-only split when {@link CODE} is shown.
 * Eligibility gap requires full composition band; band incomplete when seats remain open.
 */
export function resolvePhaseRosterIncompleteCopyVariant(
  copyContext?: StructureDiagnosticIssueCopyContext,
): PhaseRosterIncompleteCopyVariant {
  const capacity = copyContext?.compositionCapacity;
  if (capacity == null || capacity <= 0) {
    return 'neutral';
  }
  if (stageOperationalCompositionFull(copyContext ?? {})) {
    return 'eligibilityGap';
  }
  return 'bandIncomplete';
}

function rosterTitleKey(
  variant: PhaseRosterIncompleteCopyVariant,
  titleVariant: IssueTitleVariant,
  paused: boolean,
): string {
  if (variant === 'neutral') {
    return titleVariant === 'short'
      ? 'graph.issuesShort.PhaseRosterIncomplete'
      : 'graph.issues.PhaseRosterIncomplete';
  }
  if (variant === 'eligibilityGap') {
    if (paused) {
      return titleVariant === 'short'
        ? 'graph.issuesShort.PhaseRosterIncompleteEligibilityGap'
        : 'graph.issues.PhaseRosterIncompleteEligibilityGapPaused';
    }
    return titleVariant === 'short'
      ? 'graph.issuesShort.PhaseRosterIncompleteEligibilityGap'
      : 'graph.issues.PhaseRosterIncompleteEligibilityGap';
  }
  if (paused) {
    return titleVariant === 'short'
      ? 'graph.issuesShort.PhaseRosterIncomplete'
      : 'graph.issues.PhaseRosterIncompletePaused';
  }
  return titleVariant === 'short'
    ? 'graph.issuesShort.PhaseRosterIncomplete'
    : 'graph.issues.PhaseRosterIncomplete';
}

export function resolvePhaseRosterIncompleteTitleKey(
  stageStatus?: StageStatus | null,
  copyContext?: StructureDiagnosticIssueCopyContext,
  titleVariant: IssueTitleVariant = 'long',
): string {
  const variant = resolvePhaseRosterIncompleteCopyVariant(copyContext);
  return rosterTitleKey(
    variant,
    titleVariant,
    isSuspendedStageStatus(stageStatus),
  );
}

export function resolvePhaseRosterIncompleteDescriptionKey(
  stageStatus?: StageStatus | null,
  copyContext?: StructureDiagnosticIssueCopyContext,
): string | null {
  const variant = resolvePhaseRosterIncompleteCopyVariant(copyContext);
  const paused = isSuspendedStageStatus(stageStatus);
  if (variant === 'neutral') {
    return paused
      ? 'graph.issueDescriptions.PhaseRosterIncompleteNeutralPaused'
      : 'graph.issueDescriptions.PhaseRosterIncompleteNeutral';
  }
  if (variant === 'eligibilityGap') {
    return paused
      ? 'graph.issueDescriptions.PhaseRosterIncompleteEligibilityGapPaused'
      : 'graph.issueDescriptions.PhaseRosterIncompleteEligibilityGap';
  }
  return paused
    ? 'graph.issueDescriptions.PhaseRosterIncompleteBandIncompletePaused'
    : 'graph.issueDescriptions.PhaseRosterIncompleteBandIncomplete';
}

/** Lifecycle anticipated + materialize blocked captions (structure namespace). */
export function resolvePhaseRosterIncompleteOperationalMessageKey(
  copyContext?: StructureDiagnosticIssueCopyContext,
): string {
  const variant = resolvePhaseRosterIncompleteCopyVariant(copyContext);
  switch (variant) {
    case 'eligibilityGap':
      return 'problems.lifecycle.PhaseRosterIncompleteEligibilityGap';
    case 'bandIncomplete':
      return 'problems.lifecycle.PhaseRosterIncompleteBandIncomplete';
    default:
      return 'problems.lifecycle.PhaseRosterIncomplete';
  }
}

export function resolvePhaseRosterIncompleteMaterializeMessageKey(
  copyContext?: StructureDiagnosticIssueCopyContext,
): string {
  const variant = resolvePhaseRosterIncompleteCopyVariant(copyContext);
  switch (variant) {
    case 'eligibilityGap':
      return 'fiche.materialize.disabledReason.PhaseRosterIncompleteEligibilityGap';
    case 'bandIncomplete':
      return 'fiche.materialize.disabledReason.PhaseRosterIncompleteBandIncomplete';
    default:
      return 'fiche.materialize.disabledReason.PhaseRosterIncomplete';
  }
}
