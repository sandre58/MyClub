import type { StructureStageHubSummary, StructureView } from '../../types';

/**
 * Presented diagnostic codes for a phase (`GET structure` → `presentedIssueCodes`).
 * Host is the only source; absent field is treated as no presented diagnostics.
 */
export function stagePresentedIssueCodes(
  _data: StructureView,
  stage: StructureStageHubSummary,
): string[] {
  if (stage.status === 'Completed') {
    return [];
  }
  return stage.presentedIssueCodes ?? [];
}

/**
 * E1 / E6 — MissingPotRules construction gap.
 * Strictly aligned with Host: Groups && HasDrawRules && NumberOfPots == null.
 */
export function stageHasEngagedMissingPots(
  stage: StructureStageHubSummary,
): boolean {
  return (
    stage.formatKind === 'Groups' &&
    stage.hasDrawRules &&
    stage.numberOfPots == null
  );
}
