import type { StructureStageHubSummary } from '../../types';
import type { MaterializePresentation } from './structureMaterializeCopy';
import { showViewMaterializedMatchesLink } from './structureMaterializeCopy';
import { productionRatio, showProductionOperationalSection } from './structureOperationalCopy';
import { shouldShowMatchsLifecycle } from './structurePhaseStatusBandModel';

/** Mirrors `MatchesColumn` body/toolbar signals when `omitLifecycle` (aside-matchs). */
export type AsideMatchsStripContent = {
  showProductionBody: boolean;
  showProductionLeadingActions: boolean;
  showExploitationProgress: boolean;
  /** Toolbar link when the hub exposes attached matches (any non-Draft status). */
  showViewMatchesLink: boolean;
};

/**
 * Pure contract for what the aside Matchs strip can present (O-MAT-VIS-1 / O-MAT-IMPL-1).
 * Shared with fiche tile visibility — not used for composition layout.
 */
export function resolveAsideMatchsStripContent(
  stage: StructureStageHubSummary,
  presentation: MaterializePresentation | null,
): AsideMatchsStripContent {
  const none: AsideMatchsStripContent = {
    showProductionBody: false,
    showProductionLeadingActions: false,
    showExploitationProgress: false,
    showViewMatchesLink: false,
  };

  if (stage.status === 'Draft') {
    return none;
  }

  const materialize = stage.materialize;
  const dematerialize = stage.dematerialize;
  const progress = stage.lifecycle?.matchProgress;
  const showExploitationProgress =
    shouldShowMatchExploitationProgress(stage) && progress != null;

  const showViewMatchesLink = showViewMaterializedMatchesLink(
    stage,
    materialize ?? undefined,
  );

  if (stage.status === 'Completed') {
    return {
      ...none,
      showExploitationProgress,
      showViewMatchesLink,
    };
  }

  const showProduction = shouldShowProductionInMatchsSection(stage, presentation);
  const gaugeRatio = productionRatio(materialize ?? undefined);
  const canShowMetric =
    showProduction &&
    gaugeRatio != null &&
    (materialize != null || dematerialize != null);
  const showProductionBody =
    canShowMetric ||
    (showProduction && (materialize != null || dematerialize != null));
  const showMaterializePrimary =
    materialize != null && presentation === 'actionable';
  const showProductionLeadingActions =
    showProduction && (showMaterializePrimary || dematerialize?.enabled === true);

  return {
    showProductionBody,
    showProductionLeadingActions,
    showExploitationProgress,
    showViewMatchesLink,
  };
}

export function hasUsefulAsideMatchsContent(
  stage: StructureStageHubSummary,
  presentation: MaterializePresentation | null,
): boolean {
  if (stage.status === 'Draft') {
    return false;
  }
  const content = resolveAsideMatchsStripContent(stage, presentation);
  return (
    content.showProductionBody ||
    content.showProductionLeadingActions ||
    content.showExploitationProgress ||
    content.showViewMatchesLink
  );
}

/** Aside Matchs tile — hidden in Draft; visible only when aside strip has useful content. */
export function shouldShowAsideMatchsTile(
  stage: StructureStageHubSummary,
  presentation: MaterializePresentation | null,
): boolean {
  return hasUsefulAsideMatchsContent(stage, presentation);
}

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
