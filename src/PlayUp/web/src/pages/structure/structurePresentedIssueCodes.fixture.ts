import type { StructureStageHubSummary, StructureView } from '../../types';
import { projectPresentedStageIssueCodes } from './structureDiagnosticPresentation';
import { inboundPopulationConfiguredVolume } from './structurePopulationVolume';
import { resolvePlacesN } from './structurePlaces';

/**
 * Test-only: build explicit `presentedIssueCodes` for fixtures (Host graph + U1 slice).
 * Do not use in production code — mirrors StructurePresentedStageDiagnostics graph path only.
 */
export function fixturePresentedIssueCodesFromStructure(
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

/** Merge fixture stage with Host-shaped `presentedIssueCodes` when omitted. */
export function withFixturePresentedIssueCodes(
  data: StructureView,
  stage: StructureStageHubSummary,
): StructureStageHubSummary {
  if (stage.presentedIssueCodes != null) {
    return stage;
  }
  return {
    ...stage,
    presentedIssueCodes: fixturePresentedIssueCodesFromStructure(data, stage),
  };
}
