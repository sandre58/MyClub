import type { StructureStageHubSummary, StructureView } from '../../types';
import { projectPresentedStageIssueCodes } from './structureDiagnosticPresentation';
import { inboundPopulationConfiguredVolume } from './structurePopulationVolume';
import { resolvePlacesN } from './structurePlaces';

/**
 * Raw Host issues + U1 PopulationShortfall when applicable, then UX projection.
 * Single source for readiness summary and banner actions (PR1 — no wider pipeline).
 */
export function stagePresentedIssueCodes(
  data: StructureView,
  stage: StructureStageHubSummary,
): string[] {
  if (stage.status === 'Completed') {
    return [];
  }
  const raw = [...(stage.structureIssues ?? [])];
  const places = resolvePlacesN(stage);
  if (places != null && places > 0) {
    const entries =
      (stage.affectationEntryCount ?? 0) +
      inboundPopulationConfiguredVolume(data, stage.stageId);
    if (entries - places < 0 && !raw.includes('PopulationShortfall')) {
      raw.push('PopulationShortfall');
    }
  }
  return projectPresentedStageIssueCodes(raw, stage);
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
