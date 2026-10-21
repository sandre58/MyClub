import type { StageStatus } from '../../types';
import { structureDiagnosticTitleKey } from './structureDiagnosticPresentation';
import { isSuspendedStageStatus } from './structureOperationalMeterPresentation';
import {
  resolvePhaseRosterIncompleteDescriptionKey,
  resolvePhaseRosterIncompleteTitleKey,
  isPhaseRosterIncompleteCode,
} from './structurePhaseRosterIncompletePresentation';
import {
  populationShortfallUsesFullCompositionCopy,
  type StructureDiagnosticIssueCopyContext,
} from './structurePopulationShortfallPresentation';

/** Catalog codes with `graph.issues.{code}Paused` i18n (presentation only). */
export const STRUCTURE_DIAGNOSTIC_PAUSED_TITLE_CODES: ReadonlySet<string> =
  new Set([
    'SlotFeedMissing',
    'SlotFeedInvalid',
    'SlotFeedMultiple',
    'CycleDetected',
    'RoutingNondeterminism',
    'NonRootUnfed',
    'DanglingQualificationTarget',
    'DanglingProgressionTarget',
    'MissingQualificationDestinationSlot',
    'MissingQualificationDestinationGroup',
    'MissingProgressionDestinationSlot',
    'MissingProgressionDestinationGroup',
    'OverCapacityEdge',
    'OverExtraction',
    'PopulationShortfall',
    'SourcePopulationShortfall',
    'ConfrontationUndecidable',
    'AuthoredAwardUnreachable',
    'ExpectedRankUnreachable',
  ]);

export type StructureDiagnosticIssueTitleVariant = 'long' | 'short';

function populationShortfallTitleKey(
  variant: StructureDiagnosticIssueTitleVariant,
  paused: boolean,
): string {
  if (paused) {
    return variant === 'short'
      ? 'graph.issuesShort.PopulationShortfallFullComposition'
      : 'graph.issues.PopulationShortfallFullCompositionPaused';
  }
  return variant === 'short'
    ? 'graph.issuesShort.PopulationShortfallFullComposition'
    : 'graph.issues.PopulationShortfallFullComposition';
}

/**
 * Issue label for fiche banners/rails — same gravity (tone) as policy, pause copy when Suspended.
 */
export function resolveStructureDiagnosticIssueTitleKey(
  code: string,
  stageStatus?: StageStatus | null,
  copyContext?: StructureDiagnosticIssueCopyContext,
  variant: StructureDiagnosticIssueTitleVariant = 'long',
): string {
  const paused = isSuspendedStageStatus(stageStatus);

  if (populationShortfallUsesFullCompositionCopy(code, copyContext)) {
    return populationShortfallTitleKey(variant, paused);
  }

  if (isPhaseRosterIncompleteCode(code)) {
    return resolvePhaseRosterIncompleteTitleKey(
      stageStatus,
      copyContext,
      variant,
    );
  }

  const baseKey =
    variant === 'short'
      ? `graph.issuesShort.${code}`
      : structureDiagnosticTitleKey(code);

  if (!paused) {
    return baseKey;
  }
  if (!STRUCTURE_DIAGNOSTIC_PAUSED_TITLE_CODES.has(code)) {
    return baseKey;
  }
  return `graph.issues.${code}Paused`;
}

/** Optional body copy when the default shortfall title would misread occupancy. */
export function resolveStructureDiagnosticIssueDescriptionKey(
  code: string,
  stageStatus?: StageStatus | null,
  copyContext?: StructureDiagnosticIssueCopyContext,
): string | null {
  if (populationShortfallUsesFullCompositionCopy(code, copyContext)) {
    return isSuspendedStageStatus(stageStatus)
      ? 'graph.issueDescriptions.PopulationShortfallFullCompositionPaused'
      : 'graph.issueDescriptions.PopulationShortfallFullComposition';
  }
  if (isPhaseRosterIncompleteCode(code)) {
    return resolvePhaseRosterIncompleteDescriptionKey(stageStatus, copyContext);
  }
  return null;
}
