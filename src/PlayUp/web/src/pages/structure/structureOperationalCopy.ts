import type {
  CompetitionStatus,
  StageStatus,
  StructureMaterialize,
  StructureStageHubSummary,
} from '../../types';
import type { RatioMessageGaugeRatioTone } from '../../design-system/components/RatioMessageGauge';
import type { DrawCtaPoolTone } from './structureDrawCtaPool';
import type { StripOperationalMessageKind } from './structureOperationalStripUi';
import {
  classifyMaterializePresentation,
  materializeProductionTarget,
  type MaterializePresentation,
} from './structureMaterializeCopy';
import { resolveDrawCtaPoolTone } from './structureDrawCtaPool';

export function resolvePlacementPoolTone(
  filled: number,
  capacity: number | null,
): DrawCtaPoolTone {
  if (capacity == null || capacity <= 0) {
    return 'neutral';
  }
  return resolveDrawCtaPoolTone(filled, capacity);
}

/** Align ratio / gauge chrome with the operational strip message severity (D5). */
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
  if (ratioTone === 'complete' || ratioTone === 'brand') {
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
): DrawCtaPoolTone {
  if (capacity == null || capacity <= 0) {
    return 'neutral';
  }
  if (filled <= 0) {
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
  return Boolean(stage.materialize ?? stage.dematerialize);
}

export function productionRatio(
  materialize: StructureMaterialize | undefined,
): { filled: number; capacity: number } | null {
  if (!materialize) {
    return null;
  }
  const capacity = materializeProductionTarget(materialize);
  if (capacity <= 0) {
    return null;
  }
  return { filled: materialize.materializedCount, capacity };
}
