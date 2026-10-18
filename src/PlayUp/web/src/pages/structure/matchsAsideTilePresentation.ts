import type {
  StructureMaterialize,
  StructureStageHubSummary,
} from '../../types';
import {
  cupEntryRoundPlacementGaugeRatio,
  cupLegsPerMaterializedConfrontation,
  cupMatchProductionGaugeRatio,
  materializeDeterminedConfrontationsRatio,
  materializeProductionTarget,
  resolveMatchsProductionGauge,
  rosterGapBlocksMatchProductionGauge,
} from './structureMaterializeCopy';
import { resolveCompositionResolvedSeatCount } from './compositionBandGauge';
import type { StructurePlacementOperationalSectionProps } from './structurePlacementOperationalProps';

export function isPhaseRosterIncompleteMaterialize(
  materialize: StructureMaterialize | null | undefined,
): boolean {
  return materialize?.disabledReason === 'PhaseRosterIncomplete';
}

export function phaseRosterBlocksMatchProduction(
  stage: StructureStageHubSummary,
): boolean {
  return rosterGapBlocksMatchProductionGauge(stage.materialize, stage);
}

/** Qual / composition seats resolved vs Places N (not schematic occupancy). */
export function resolveAsideRosterResolutionGaugeStats(
  stage: StructureStageHubSummary,
  placement: StructurePlacementOperationalSectionProps | null,
): { resolved: number; total: number } | null {
  const total = placement?.placesN ?? stage.compositionCapacity ?? null;
  if (total == null || total <= 0) {
    return null;
  }
  const resolved = Math.min(resolveCompositionResolvedSeatCount(stage), total);
  return { resolved, total };
}

export function shouldShowAsideRosterResolutionGauge(
  stage: StructureStageHubSummary,
  stats: { resolved: number; total: number } | null,
): boolean {
  return phaseRosterBlocksMatchProduction(stage) && stats != null;
}

function championshipProspectiveMatchCapacity(
  stage: Pick<
    StructureStageHubSummary,
    | 'compositionCapacity'
    | 'numberOfLegs'
    | 'defaultTieFormat'
    | 'inboundComposition'
  >,
): number | null {
  const places =
    stage.compositionCapacity ??
    stage.inboundComposition?.configuredVolume ??
    null;
  if (places == null || places < 2) {
    return null;
  }
  const confrontations = (places * (places - 1)) / 2;
  const legs =
    stage.defaultTieFormat?.numberOfLegs ?? stage.numberOfLegs ?? 1;
  const safeLegs = legs > 0 ? legs : 1;
  return confrontations * safeLegs;
}

/**
 * Aside Matchs production gauge when the read exposes no determined confrontations yet
 * (e.g. PhaseRosterIncomplete) — prospective match slots at full Places N.
 */
export type AsideMatchProductionPresentation = {
  ratio: { filled: number; capacity: number };
  mode: 'materialized';
};

/**
 * Aside Matchs primary gauge — always match production (never "determined confrontations").
 */
export function resolveAsideMatchProductionPresentation(
  stage: StructureStageHubSummary,
): AsideMatchProductionPresentation | null {
  const materialize = stage.materialize;
  if (!materialize) {
    return null;
  }

  if (phaseRosterBlocksMatchProduction(stage)) {
    const idle = resolveAsideIdleMatchProductionGauge(stage);
    if (idle) {
      return { ratio: idle, mode: 'materialized' };
    }
  }

  const cupProd = cupMatchProductionGaugeRatio(stage, materialize);
  if (cupProd) {
    return { ratio: cupProd, mode: 'materialized' };
  }

  const stripGauge = resolveMatchsProductionGauge(materialize, stage);
  if (stripGauge?.mode === 'materialized') {
    return { ratio: stripGauge.ratio, mode: 'materialized' };
  }

  const canonicalPairs = materialize.canonicalEntryPairCount ?? 0;
  if (
    materialize.commandCode === 'MaterializeFromOccupiedSlots' &&
    canonicalPairs > 0
  ) {
    const legs = cupLegsPerMaterializedConfrontation(stage);
    return {
      ratio: {
        filled: materialize.materializedCount,
        capacity: canonicalPairs * legs,
      },
      mode: 'materialized',
    };
  }

  const target = materializeProductionTarget(materialize);
  if (target > 0) {
    const legs =
      stage.formatKind === 'Championship' || stage.formatKind === 'Swiss'
        ? cupLegsPerMaterializedConfrontation(stage)
        : 1;
    return {
      ratio: {
        filled: materialize.materializedCount,
        capacity: target * legs,
      },
      mode: 'materialized',
    };
  }

  const prospective =
    materialize.prospectiveConfrontationCount ??
    (stripGauge?.mode === 'determined' ? stripGauge.ratio.capacity : 0);
  if (prospective > 0) {
    return {
      ratio: { filled: materialize.materializedCount, capacity: prospective },
      mode: 'materialized',
    };
  }

  const idle = resolveAsideIdleMatchProductionGauge(stage);
  if (idle) {
    return { ratio: idle, mode: 'materialized' };
  }

  const entryRound = cupEntryRoundPlacementGaugeRatio(materialize);
  if (
    materialize.commandCode === 'MaterializeFromOccupiedSlots' &&
    entryRound != null
  ) {
    const legs = cupLegsPerMaterializedConfrontation(stage);
    return {
      ratio: {
        filled: materialize.materializedCount,
        capacity: entryRound.capacity * legs,
      },
      mode: 'materialized',
    };
  }

  const confrontationRows = materializeDeterminedConfrontationsRatio(materialize);
  if (
    materialize.commandCode === 'MaterializeFromOccupiedSlots' &&
    confrontationRows != null &&
    confrontationRows.capacity > 0
  ) {
    const legs = cupLegsPerMaterializedConfrontation(stage);
    const capacity =
      canonicalPairs > 0
        ? canonicalPairs * legs
        : confrontationRows.capacity * legs;
    return {
      ratio: { filled: materialize.materializedCount, capacity },
      mode: 'materialized',
    };
  }

  return null;
}

/**
 * Cup entry-round placement for aside diagnostic gauge (hub counts or confrontation rows).
 */
export function resolveAsideCupEntryRoundGaugeStats(
  materialize: StructureMaterialize,
): { filled: number; capacity: number } | null {
  const fromRead = cupEntryRoundPlacementGaugeRatio(materialize);
  if (fromRead) {
    return fromRead;
  }

  const capacity = materialize.canonicalEntryPairCount ?? 0;
  const undetermined = materialize.undeterminedCanonicalEntryPairCount ?? 0;
  if (capacity > 0 && undetermined > 0) {
    return { filled: capacity - undetermined, capacity };
  }

  if (materialize.commandCode !== 'MaterializeFromOccupiedSlots') {
    return null;
  }

  const rowCount = materialize.confrontations?.length ?? 0;
  if (rowCount <= 0 || materialize.determinedCount > 0) {
    return null;
  }

  const effectiveCapacity = capacity > 0 ? capacity : rowCount;
  return { filled: 0, capacity: effectiveCapacity };
}

export function shouldShowAsideEntryRoundBlockerGauge(
  stage: StructureStageHubSummary,
  materialize: StructureMaterialize | null | undefined,
): boolean {
  if (materialize == null || stage.formatKind !== 'Cup') {
    return false;
  }
  if (materialize.commandEnabled) {
    return false;
  }
  return resolveAsideCupEntryRoundGaugeStats(materialize) != null;
}

export function resolveAsideIdleMatchProductionGauge(
  stage: StructureStageHubSummary,
): { filled: number; capacity: number } | null {
  const materialize = stage.materialize;
  if (!materialize) {
    return null;
  }

  if (!phaseRosterBlocksMatchProduction(stage)) {
    const fromRead = resolveMatchsProductionGauge(materialize, stage);
    if (fromRead?.mode === 'materialized' && fromRead.ratio) {
      return fromRead.ratio;
    }
  }

  const target = materializeProductionTarget(materialize);
  if (target > 0) {
    return {
      filled: materialize.materializedCount,
      capacity: target,
    };
  }

  if (stage.formatKind === 'Championship' || stage.formatKind === 'Swiss') {
    const capacity = championshipProspectiveMatchCapacity(stage);
    if (capacity != null && capacity > 0) {
      return { filled: materialize.materializedCount, capacity };
    }
  }

  return null;
}
