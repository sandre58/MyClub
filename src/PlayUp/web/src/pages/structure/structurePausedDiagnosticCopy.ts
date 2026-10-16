import type { StageStatus } from '../../types';
import { structureDiagnosticTitleKey } from './structureDiagnosticPresentation';
import { isSuspendedStageStatus } from './structureOperationalMeterPresentation';

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

/**
 * Issue label for fiche banners/rails — same gravity (tone) as policy, pause copy when Suspended.
 */
export function resolveStructureDiagnosticIssueTitleKey(
  code: string,
  stageStatus?: StageStatus | null,
): string {
  const baseKey = structureDiagnosticTitleKey(code);
  if (!isSuspendedStageStatus(stageStatus)) {
    return baseKey;
  }
  if (!STRUCTURE_DIAGNOSTIC_PAUSED_TITLE_CODES.has(code)) {
    return baseKey;
  }
  return `graph.issues.${code}Paused`;
}
