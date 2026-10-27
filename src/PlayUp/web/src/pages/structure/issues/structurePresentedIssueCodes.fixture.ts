import type { StructureStageHubSummary, StructureView } from '../../../types';
import { projectPresentedStageIssueCodes } from './structureDiagnosticPresentation';
import { inboundPopulationConfiguredVolume } from '../composition/structurePopulationVolume';
import { resolvePlacesN } from '../composition/structurePlaces';

/**
 * Test-only Host shape for fixtures. Production SPA reads `presentedIssueCodes`
 * and does not apply this rule.
 * Mirrors BuildReadiness: Groups + draw engaged + null pots emits MissingPotRules
 * on blocker details, which CollectPresentedIssueCodes folds into the codes.
 */
function fixtureHostEmitsMissingPotRules(
  stage: StructureStageHubSummary,
): boolean {
  return (
    stage.formatKind === 'Groups' &&
    stage.hasDrawRules &&
    stage.numberOfPots == null
  );
}

/**
 * Test-only: build explicit `presentedIssueCodes` for fixtures (Host graph + U1
 * slice, plus MissingPotRules when Host readiness would emit it).
 * Do not use in production code.
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
  const projected = projectPresentedStageIssueCodes(raw, stage);
  if (
    fixtureHostEmitsMissingPotRules(stage) &&
    !projected.includes('MissingPotRules')
  ) {
    projected.push('MissingPotRules');
  }
  return projected;
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
