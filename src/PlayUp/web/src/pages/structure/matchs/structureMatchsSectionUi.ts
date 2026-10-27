import type { StructureStageHubSummary } from '../../../types';
import type { MaterializePresentation } from '../fiche/structureMaterializeCopy';
import { showViewMaterializedMatchesLink } from '../fiche/structureMaterializeCopy';
import {
  matchProductionDematerializeVisible,
  matchProductionMaterializeVisible,
  resolveMatchProductionGaugeView,
} from './structureMatchProductionPresentation';
import { shouldShowMatchsLifecycle } from '../fiche/structurePhaseStatusBandModel';
import { resolveSwissMatchsGuidance } from './swissMatchsGuidance';

/** Mirrors `MatchesColumn` body/toolbar signals when `omitLifecycle` (aside-matchs). */
export type AsideMatchsStripContent = {
  showProductionBody: boolean;
  showProductionLeadingActions: boolean;
  showExploitationProgress: boolean;
  /** Swiss sport guidance without classic match production projection. */
  showSwissGuidanceOnly: boolean;
  /** Toolbar link when the hub exposes attached matches (any non-Draft status). */
  showViewMatchesLink: boolean;
};

/**
 * Pure contract for what the aside Matchs strip can present (O-MAT-VIS-1).
 * Shared with fiche tile visibility — not used for composition layout.
 */
export function resolveAsideMatchsStripContent(
  stage: StructureStageHubSummary,
  _presentation: MaterializePresentation | null,
): AsideMatchsStripContent {
  const none: AsideMatchsStripContent = {
    showProductionBody: false,
    showProductionLeadingActions: false,
    showExploitationProgress: false,
    showSwissGuidanceOnly: false,
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

  const hubProduction = stage.matchProductionPresentation;
  const showProduction = hubProduction?.productionVisible === true;
  const gaugeRatio = resolveMatchProductionGaugeView(stage)?.ratio ?? null;
  const swissGuidance =
    stage.formatKind === 'Swiss' ? resolveSwissMatchsGuidance(stage) : null;
  const showSwissGuidanceOnly =
    swissGuidance != null && hubProduction?.productionVisible !== true;
  const hubExploitationOnly =
    hubProduction?.productionVisible === true &&
    hubProduction.showPrimaryGauge === false;
  const showProductionBody =
    gaugeRatio != null ||
    hubProduction?.frozenPartial != null ||
    hubProduction?.prerequisiteGauge != null ||
    showSwissGuidanceOnly ||
    (!hubExploitationOnly &&
      showProduction &&
      (materialize != null || dematerialize != null));
  const showProductionLeadingActions =
    showProduction &&
    (matchProductionMaterializeVisible(stage) ||
      matchProductionDematerializeVisible(stage));

  return {
    showProductionBody,
    showProductionLeadingActions,
    showExploitationProgress,
    showSwissGuidanceOnly,
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
export function shouldShowMatchsSection(
  stage: StructureStageHubSummary,
): boolean {
  if (stage.status !== 'Completed') {
    return (
      shouldShowMatchsLifecycle(stage) ||
      stage.matchProductionPresentation?.productionVisible === true ||
      Boolean(stage.materialize ?? stage.dematerialize)
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
): boolean {
  return stage.matchProductionPresentation?.productionVisible === true;
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
