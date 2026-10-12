import type { StructureStageHubSummary } from '../../types';
import type { MaterializePresentation } from './structureMaterializeCopy';
import { showProductionOperationalSection } from './structureOperationalCopy';
import { shouldShowMatchsLifecycle } from './structurePhaseStatusBandModel';

/** When the operational strip should include the Matchs column. */
export function shouldShowMatchsSection(stage: StructureStageHubSummary): boolean {
  if (stage.status !== 'Completed') {
    return (
      shouldShowMatchsLifecycle(stage) || showProductionOperationalSection(stage)
    );
  }
  const progressTotal = stage.lifecycle?.matchProgress?.total ?? 0;
  return stage.matchCount > 0 || progressTotal > 0;
}

/**
 * Production gauge + materialize/dematerialize actions in Matchs.
 * Running/Suspended: only when the read model still exposes actionable production.
 */
export function shouldShowProductionInMatchsSection(
  stage: StructureStageHubSummary,
  presentation: MaterializePresentation | null,
): boolean {
  if (stage.status === 'Draft' || stage.status === 'Completed') {
    return false;
  }
  if (stage.materialize == null && stage.dematerialize == null) {
    return false;
  }
  if (stage.status === 'Ready') {
    return true;
  }
  if (presentation === 'actionable') {
    return true;
  }
  if (stage.dematerialize?.enabled) {
    return true;
  }
  return false;
}

export function shouldShowMatchExploitationProgress(
  stage: StructureStageHubSummary,
): boolean {
  const progress = stage.lifecycle?.matchProgress;
  if (stage.status === 'Running' || stage.status === 'Suspended') {
    return progress != null;
  }
  if (stage.status === 'Completed') {
    return (progress?.total ?? 0) > 0 || stage.matchCount > 0;
  }
  return false;
}
