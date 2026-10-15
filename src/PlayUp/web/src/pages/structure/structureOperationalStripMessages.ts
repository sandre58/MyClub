import type { StageStatus } from '../../types';
import { structureDiagnosticPresentation } from './structureDiagnosticPresentationPolicy';
import type { StructureDiagnosticPresentationContext } from './structureDiagnosticPresentationPolicy';
import {
  isDraftLikeStageStatus,
  presentationToneToMeterKind,
} from './structureOperationalMeterPresentation';
import type { StripOperationalMessageKind } from './structureOperationalStripUi';

export type StripMeterCopy = {
  text: string;
  kind: StripOperationalMessageKind;
};

export function resolvePlaceOccupancyMeterCopy(input: {
  t: (key: string, opts?: Record<string, unknown>) => string;
  occupancyReady: boolean;
  unknownCapacity: boolean;
  capacity: number | null;
  occupiedPlacesCount: number | null;
  slotFeedMissingActive: boolean;
  stageStatus?: StageStatus | null;
  slotFeedPresentationContext?: StructureDiagnosticPresentationContext;
}): StripMeterCopy {
  const {
    t,
    occupancyReady,
    unknownCapacity,
    capacity,
    occupiedPlacesCount,
    slotFeedMissingActive,
    stageStatus,
    slotFeedPresentationContext,
  } = input;

  if (!occupancyReady) {
    return {
      text: t('fiche.operational.placement.stateMessage.loading'),
      kind: 'info',
    };
  }
  if (unknownCapacity || occupiedPlacesCount == null) {
    return {
      text: t('fiche.operational.placement.stateMessage.unknown'),
      kind: 'info',
    };
  }
  if (occupiedPlacesCount >= capacity!) {
    return {
      text: t('fiche.operational.placement.stateMessage.complete'),
      kind: 'success',
    };
  }

  const incompleteKind = (): StripOperationalMessageKind => {
    if (isDraftLikeStageStatus(stageStatus)) {
      return 'attention';
    }
    if (slotFeedMissingActive) {
      const tone = structureDiagnosticPresentation(
        'SlotFeedMissing',
        slotFeedPresentationContext ?? { stageStatus },
      ).tone;
      return presentationToneToMeterKind(tone);
    }
    if (occupiedPlacesCount === 0) {
      return 'info';
    }
    return 'attention';
  };

  if (occupiedPlacesCount === 0) {
    return {
      text: t('fiche.operational.placement.stateMessage.empty'),
      kind: incompleteKind(),
    };
  }

  const remaining = capacity! - occupiedPlacesCount;
  return {
    text: t('fiche.operational.placement.stateMessage.partial', {
      count: remaining,
    }),
    kind: incompleteKind(),
  };
}

export function resolveCompositionPopulationMeterCopy(input: {
  t: (key: string, opts?: Record<string, unknown>) => string;
  current: number;
  capacity: number;
  presentationContext?: StructureDiagnosticPresentationContext;
}): StripMeterCopy {
  const { t, current, capacity, presentationContext } = input;
  const gap = current - capacity;
  if (gap === 0) {
    return {
      text: t('composition.gauge.populationComplete'),
      kind: 'success',
    };
  }
  if (gap < 0) {
    const tone = structureDiagnosticPresentation(
      'PopulationShortfall',
      presentationContext ?? {},
    ).tone;
    return {
      text: t('entries.meter.shortfall', { count: Math.abs(gap) }),
      kind: presentationToneToMeterKind(tone),
    };
  }
  const overTone = structureDiagnosticPresentation(
    'OverCapacityEdge',
    presentationContext ?? {},
  ).tone;
  return {
    text: t('entries.meter.surplus', { count: gap }),
    kind: presentationToneToMeterKind(overTone),
  };
}

export function resolveDrawPoolMeterCopy(input: {
  t: (key: string, opts?: Record<string, unknown>) => string;
  filled: number;
  capacity: number | null;
  unknownCapacity: boolean;
}): StripMeterCopy {
  const { t, filled, capacity, unknownCapacity } = input;
  if (unknownCapacity || capacity == null || capacity <= 0) {
    return {
      text: t('fiche.operational.draw.stateMessage.unknown'),
      kind: 'info',
    };
  }
  if (filled >= capacity) {
    return {
      text: t('fiche.operational.draw.stateMessage.poolReady'),
      kind: 'success',
    };
  }
  return {
    text: t('fiche.operational.draw.stateMessage.pool', { filled, capacity }),
    kind: 'info',
  };
}

export function resolveProductionMeterCopy(input: {
  t: (key: string, opts?: Record<string, unknown>) => string;
  blockedCaption: string | null;
  gaugeRatio: { filled: number; capacity: number } | null;
  hasProduction: boolean;
}): StripMeterCopy {
  const { t, blockedCaption, gaugeRatio, hasProduction } = input;

  if (!hasProduction) {
    return {
      text: t('fiche.operational.production.stateMessage.unavailable'),
      kind: 'info',
    };
  }
  if (blockedCaption) {
    return { text: blockedCaption, kind: 'attention' };
  }
  if (gaugeRatio == null) {
    return {
      text: t('fiche.operational.production.statusUnknown'),
      kind: 'info',
    };
  }
  if (gaugeRatio.filled >= gaugeRatio.capacity) {
    return {
      text: t('fiche.operational.production.stateMessage.complete'),
      kind: 'success',
    };
  }
  const remaining = gaugeRatio.capacity - gaugeRatio.filled;
  return {
    text: t('fiche.operational.production.stateMessage.partial', {
      count: remaining,
    }),
    kind: 'attention',
  };
}
