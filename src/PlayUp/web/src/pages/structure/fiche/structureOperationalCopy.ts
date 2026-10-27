import type {
  CompetitionStatus,
  StageStatus,
  StructureStageHubSummary,
} from '../../../types';
import type { RatioMessageGaugeRatioTone } from '../../../design-system/components/RatioMessageGauge';
import type { DrawCtaPoolTone } from '../draw/structureDrawCtaPool';
import type { StripOperationalMessageKind } from './structureOperationalStripUi';
import {
  classifyMaterializePresentation,
  type MaterializePresentation,
} from './structureMaterializeCopy';
import { resolveDrawCtaPoolTone } from '../draw/structureDrawCtaPool';

export function resolvePlacementPoolTone(
  filled: number,
  capacity: number | null,
): DrawCtaPoolTone {
  if (capacity == null || capacity <= 0) {
    return 'neutral';
  }
  return resolveDrawCtaPoolTone(filled, capacity);
}

/** Align ratio / gauge chrome with the operational strip message severity. */
export function drawPoolToneToRatioGaugeTone(
  tone: DrawCtaPoolTone,
): RatioMessageGaugeRatioTone {
  switch (tone) {
    case 'complete':
      return 'complete';
    case 'danger':
      return 'danger';
    case 'attention':
      return 'attention';
    case 'partial':
      return 'partial';
    case 'brand':
      return 'brand';
    default:
      return 'neutral';
  }
}

export function operationalStripPoolTone(
  messageKind: StripOperationalMessageKind,
  ratioTone: DrawCtaPoolTone,
): DrawCtaPoolTone {
  if (messageKind === 'danger') {
    return 'danger';
  }
  if (messageKind === 'attention') {
    return 'attention';
  }
  if (messageKind === 'success') {
    return 'complete';
  }
  if (ratioTone === 'brand') {
    return 'brand';
  }
  if (ratioTone === 'complete') {
    return 'complete';
  }
  if (ratioTone === 'partial' || ratioTone === 'attention') {
    return 'partial';
  }
  if (ratioTone === 'danger') {
    return 'danger';
  }
  return 'neutral';
}

/** Schematic places occupied (team or structural feed). Zero filled is actionable gap. */
export function resolvePlaceOccupancyTone(
  filled: number,
  capacity: number | null,
  options?: {
    drawRulesEngaged?: boolean;
    drawLaunchable?: boolean;
    drawLaunchSignal?: boolean;
  },
): DrawCtaPoolTone {
  if (capacity == null || capacity <= 0) {
    return 'neutral';
  }
  if (filled <= 0) {
    const launchSignal =
      options?.drawLaunchSignal ??
      (options?.drawRulesEngaged === true && options?.drawLaunchable === true);
    if (launchSignal) {
      return 'brand';
    }
    if (options?.drawRulesEngaged) {
      return 'neutral';
    }
    return 'partial';
  }
  return resolveDrawCtaPoolTone(filled, capacity);
}

/** Composition pool — resolved entries for draw; zero is normal before resolution. */
export function resolveResolvedPoolTone(
  filled: number,
  capacity: number | null,
): DrawCtaPoolTone {
  if (capacity == null || capacity <= 0) {
    return 'neutral';
  }
  if (filled <= 0) {
    return 'neutral';
  }
  return resolveDrawCtaPoolTone(filled, capacity);
}

export function materializePresentationForStage(
  stage: StructureStageHubSummary,
  competitionStatus: CompetitionStatus,
  stageStatus: StageStatus,
): MaterializePresentation | null {
  if (!stage.materialize) {
    return null;
  }
  return classifyMaterializePresentation(
    stage.materialize,
    competitionStatus,
    stageStatus,
  );
}

export function showProductionOperationalSection(
  stage: StructureStageHubSummary,
): boolean {
  return (
    stage.matchProductionPresentation?.productionVisible === true ||
    Boolean(stage.materialize ?? stage.dematerialize)
  );
}
