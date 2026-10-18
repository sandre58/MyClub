import type { StageStatus } from '../../types';
import { structureDiagnosticPresentation } from './structureDiagnosticPresentationPolicy';
import type { StructureDiagnosticPresentationContext } from './structureDiagnosticPresentationPolicy';
import {
  isCompletedStageStatus,
  isDraftLikeStageStatus,
  isSuspendedStageStatus,
  presentationToneToMeterKind,
} from './structureOperationalMeterPresentation';
import type { StripOperationalMessageKind } from './structureOperationalStripUi';
import type { TopologyDrawExecutionBadge } from './drawUi';
import { isPlacementDrawLaunchSignal } from './drawUi';

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
  /** Draw rules engaged on this stage — launchability gates 0/N action signal. */
  drawRulesEngaged?: boolean;
  /** `createGate.ok` when draw rules engaged. */
  drawLaunchable?: boolean;
  drawExecutionBadge?: TopologyDrawExecutionBadge | null;
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
    drawRulesEngaged = false,
    drawLaunchable = false,
    drawExecutionBadge,
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

  if (isSuspendedStageStatus(stageStatus)) {
    if (occupiedPlacesCount === 0) {
      return {
        text: t('fiche.operational.placement.stateMessage.emptyPaused'),
        kind: 'info',
      };
    }
    const remaining = capacity! - occupiedPlacesCount;
    return {
      text: t('fiche.operational.placement.stateMessage.partialPaused', {
        count: remaining,
      }),
      kind: 'info',
    };
  }

  if (isCompletedStageStatus(stageStatus)) {
    if (occupiedPlacesCount === 0) {
      return {
        text: t('fiche.operational.placement.stateMessage.finalEmpty'),
        kind: 'info',
      };
    }
    return {
      text: t('fiche.operational.placement.stateMessage.finalPartial', {
        occupied: occupiedPlacesCount,
        capacity,
      }),
      kind: 'info',
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
    if (drawRulesEngaged && occupiedPlacesCount === 0) {
      return 'info';
    }
    if (occupiedPlacesCount === 0) {
      return 'info';
    }
    return 'attention';
  };

  const drawLaunchSignal = isPlacementDrawLaunchSignal({
    drawRulesEngaged,
    drawLaunchable,
    drawExecutionBadge,
    occupiedPlacesCount,
  });

  if (occupiedPlacesCount === 0) {
    if (drawLaunchSignal && !slotFeedMissingActive) {
      return {
        text: t('readiness.nextStep.RunDraw'),
        kind: 'info',
      };
    }
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
    if (isSuspendedStageStatus(presentationContext?.stageStatus)) {
      return {
        text: t('composition.gauge.populationShortfallPaused', {
          count: Math.abs(gap),
        }),
        kind: 'info',
      };
    }
    if (isCompletedStageStatus(presentationContext?.stageStatus)) {
      return {
        text: t('composition.gauge.populationFinalShortfall', {
          count: Math.abs(gap),
        }),
        kind: 'info',
      };
    }
    const tone = structureDiagnosticPresentation(
      'PopulationShortfall',
      presentationContext ?? {},
    ).tone;
    return {
      text: t('entries.meter.shortfall', { count: Math.abs(gap) }),
      kind: presentationToneToMeterKind(tone),
    };
  }
  if (isCompletedStageStatus(presentationContext?.stageStatus)) {
    return {
      text: t('composition.gauge.populationFinalSurplus', { count: gap }),
      kind: 'info',
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
  startStageBlocker?: 'IncompleteEntryRound' | 'UnmaterializedConfrontations' | null;
  productionGaugeMode?: 'materialized' | 'determined' | null;
  /** Groups pools incomplete — match production not actionable (Cup entry-round parity). */
  groupsPoolsIncomplete?: boolean;
  /** Championship/Swiss roster gap — prospective match gauge, not a danger signal. */
  phaseRosterBlocksProduction?: boolean;
}): StripMeterCopy {
  const {
    t,
    blockedCaption,
    gaugeRatio,
    hasProduction,
    startStageBlocker = null,
    productionGaugeMode = null,
    groupsPoolsIncomplete = false,
    phaseRosterBlocksProduction = false,
  } = input;

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
  if (productionGaugeMode === 'determined') {
    if (gaugeRatio.filled <= 0) {
      return {
        text: t(
          'fiche.operational.production.stateMessage.noDeterminedConfrontations',
        ),
        kind: 'attention',
      };
    }
    if (gaugeRatio.filled < gaugeRatio.capacity) {
      const remaining = gaugeRatio.capacity - gaugeRatio.filled;
      return {
        text: t(
          'fiche.operational.production.stateMessage.determinedPartial',
          { count: remaining },
        ),
        kind: 'attention',
      };
    }
    return {
      text: t('fiche.operational.production.stateMessage.determinedComplete'),
      kind: 'success',
    };
  }
  if (gaugeRatio.filled >= gaugeRatio.capacity) {
    return {
      text: t('fiche.operational.production.stateMessage.complete'),
      kind: 'success',
    };
  }
  if (groupsPoolsIncomplete && gaugeRatio.filled < gaugeRatio.capacity) {
    return {
      text: t(
        'fiche.operational.production.stateMessage.matchesBlockedByGroupsPools',
      ),
      kind: 'danger',
    };
  }
  const remaining = gaugeRatio.capacity - gaugeRatio.filled;
  if (remaining <= 0) {
    return {
      text: t('fiche.operational.production.stateMessage.complete'),
      kind: 'success',
    };
  }
  const kind = phaseRosterBlocksProduction
    ? 'info'
    : startStageBlocker === 'UnmaterializedConfrontations'
      ? 'danger'
      : 'attention';
  return {
    text: t('fiche.operational.production.stateMessage.partial', {
      count: remaining,
    }),
    kind,
  };
}

export function entryRoundPlacementMessageKind(
  blocksPhaseStart: boolean,
): StripMeterCopy['kind'] {
  return blocksPhaseStart ? 'danger' : 'attention';
}

export function resolveGroupsPlacementMeterCopy(input: {
  t: (key: string, opts?: Record<string, unknown>) => string;
  gaugeRatio: { filled: number; capacity: number };
}): { text: string; kind: StripMeterCopy['kind'] } | null {
  const { t, gaugeRatio } = input;
  if (gaugeRatio.capacity <= 0) {
    return null;
  }
  if (gaugeRatio.filled >= gaugeRatio.capacity) {
    return {
      text: t('fiche.operational.production.stateMessage.groupsPlacementReady'),
      kind: 'success',
    };
  }
  return {
    text: t(
      'fiche.operational.production.stateMessage.groupsPlacementIncomplete',
      {
        filled: gaugeRatio.filled,
        capacity: gaugeRatio.capacity,
      },
    ),
    kind: 'attention',
  };
}

export function resolveEntryRoundPlacementMeterCopy(input: {
  t: (key: string, opts?: Record<string, unknown>) => string;
  gaugeRatio: { filled: number; capacity: number };
  blocksPhaseStart?: boolean;
}): { text: string; kind: StripMeterCopy['kind'] } | null {
  const { t, gaugeRatio, blocksPhaseStart = false } = input;
  const remaining = gaugeRatio.capacity - gaugeRatio.filled;
  if (remaining <= 0) {
    return null;
  }
  const kind = entryRoundPlacementMessageKind(blocksPhaseStart);
  return {
    text: t('fiche.operational.production.stateMessage.entryRoundRemainingBeforeStart', {
      count: remaining,
    }),
    kind,
  };
}
