import type {
  StructureMatchProductionPresentation,
  StructureStageHubSummary,
} from '../../types';

/** Attaches an explicit hub projection for component tests (no backend recompute). */
export function attachMatchProductionPresentation(
  stage: StructureStageHubSummary,
  matchProductionPresentation: StructureMatchProductionPresentation | null,
): StructureStageHubSummary {
  return { ...stage, matchProductionPresentation };
}
