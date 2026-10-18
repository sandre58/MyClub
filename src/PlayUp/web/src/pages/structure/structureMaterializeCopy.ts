import type {
  CompetitionStatus,
  StageStatus,
  StructureMaterialize,
  StructureStageHubSummary,
} from '../../types';

import {
  resolveDrawCtaPoolTone,
  type DrawCtaPoolTone,
} from './structureDrawCtaPool';

/**
 * Ratio denominator from the Structure read (`determinedCount`).
 * See StructureMaterializeAssembler for Cup vs Groups semantics.
 */
export function materializeProductionTarget(
  materialize: StructureMaterialize,
): number {
  return materialize.determinedCount;
}

export type MatchsProductionGaugeMode = 'materialized' | 'determined';

/** Groups / Championship — determined slots vs exposed confrontation rows (not match production). */
export function materializeDeterminedConfrontationsRatio(
  materialize: StructureMaterialize,
): { filled: number; capacity: number } | null {
  const rows = materialize.confrontations;
  if (!rows?.length) {
    return null;
  }
  const filled = rows.reduce(
    (count, row) => count + (row.determined ? 1 : 0),
    0,
  );
  return { filled, capacity: rows.length };
}

/**
 * Primary Matchs strip gauge — materialized matches when `determinedCount > 0`,
 * otherwise determined-confrontation rows when the read exposes them.
 */
export function resolveMatchsProductionGauge(
  materialize: StructureMaterialize | null | undefined,
  stage:
    | Pick<
        StructureStageHubSummary,
        'matchCount' | 'defaultTieFormat' | 'numberOfLegs'
      >
    | null
    | undefined,
): {
  mode: MatchsProductionGaugeMode;
  ratio: { filled: number; capacity: number };
} | null {
  if (!materialize) {
    return null;
  }

  if (materialize.determinedCount > 0 && !isGroupsPoolPlacementIncomplete(materialize)) {
    let ratio: { filled: number; capacity: number } | null = null;
    if (
      materialize.commandCode === 'MaterializeFromOccupiedSlots' &&
      stage != null
    ) {
      ratio = cupMatchProductionGaugeRatio(stage, materialize);
    } else {
      const capacity = materializeProductionTarget(materialize);
      if (capacity > 0) {
        ratio = {
          filled: materialize.materializedCount,
          capacity,
        };
      }
    }
    if (ratio) {
      return { mode: 'materialized', ratio };
    }
  }

  const groupsProspective = groupsProspectiveMatchProductionGauge(
    materialize,
    stage,
  );
  if (groupsProspective) {
    return { mode: 'materialized', ratio: groupsProspective };
  }

  const determined = materializeDeterminedConfrontationsRatio(materialize);
  if (determined && !isGroupsPoolPlacementIncomplete(materialize)) {
    return { mode: 'determined', ratio: determined };
  }

  return null;
}

/** Groups pools not full enough for confrontation production (read or legacy hub). */
/** Ready Groups: pools incomplete and match production command is off (SPA parity when lifecycle read lags). */
export function groupsPoolsBlockPhaseStart(
  stage: Pick<StructureStageHubSummary, 'status' | 'materialize'>,
): boolean {
  if (stage.status !== 'Ready') {
    return false;
  }
  const materialize = stage.materialize;
  if (!materialize || materialize.commandEnabled) {
    return false;
  }
  return isGroupsPoolPlacementIncomplete(materialize);
}

export function isGroupsPoolPlacementIncomplete(
  materialize: StructureMaterialize,
): boolean {
  if (materialize.disabledReason === 'GroupsNotReady') {
    return true;
  }
  const capacity = materialize.groupsPlacementCapacityCount ?? 0;
  const filled = materialize.groupsPlacementFilledCount ?? 0;
  return capacity > 0 && filled < capacity;
}

function roundRobinPairCountPerPool(placesPerGroup: number): number {
  return placesPerGroup >= 2 ? (placesPerGroup * (placesPerGroup - 1)) / 2 : 0;
}

function resolveGroupsProspectiveConfrontationCapacity(
  materialize: StructureMaterialize,
  stage?:
    | Pick<
        StructureStageHubSummary,
        'formatKind' | 'groupCount' | 'placesPerGroup'
      >
    | null,
): number {
  const fromRead = materialize.prospectiveConfrontationCount ?? 0;
  if (fromRead > 0) {
    return fromRead;
  }
  if (!isGroupsPoolPlacementIncomplete(materialize)) {
    return 0;
  }
  if (stage?.formatKind !== 'Groups') {
    return 0;
  }
  const groups = stage.groupCount ?? 0;
  const perGroup = stage.placesPerGroup ?? 0;
  if (groups <= 0 || perGroup < 2) {
    return 0;
  }
  return groups * roundRobinPairCountPerPool(perGroup);
}

/** Matchs strip gauge while Groups pools are incomplete (0 / prospective confrontations). */
export function groupsProspectiveMatchProductionGauge(
  materialize: StructureMaterialize,
  stage?:
    | Pick<
        StructureStageHubSummary,
        'formatKind' | 'groupCount' | 'placesPerGroup'
      >
    | null,
): { filled: number; capacity: number } | null {
  if (!isGroupsPoolPlacementIncomplete(materialize)) {
    return null;
  }
  const capacity = resolveGroupsProspectiveConfrontationCapacity(
    materialize,
    stage,
  );
  if (capacity <= 0) {
    return null;
  }
  return { filled: materialize.materializedCount, capacity };
}

export function hasMatchsStructuralProductionChrome(
  materialize: StructureMaterialize | null | undefined,
  stage:
    | Pick<
        StructureStageHubSummary,
        'matchCount' | 'defaultTieFormat' | 'numberOfLegs'
      >
    | null
    | undefined,
): boolean {
  if (!materialize) {
    return false;
  }
  return (
    resolveMatchsProductionGauge(materialize, stage) != null ||
    cupEntryRoundPlacementGaugeRatio(materialize) != null
  );
}

export function shouldShowBlockedGroupsMaterializeCta(
  materialize: StructureMaterialize,
  presentation: MaterializePresentation,
): boolean {
  return (
    presentation === 'blocked' && isGroupsPoolPlacementIncomplete(materialize)
  );
}

export function materializeRatioTone(
  materialize: StructureMaterialize,
): DrawCtaPoolTone {
  const target = materializeProductionTarget(materialize);
  return resolveDrawCtaPoolTone(materialize.materializedCount, target);
}

/** Mirrors StructureMaterializeAssembler mutability — UI layout only, not a disabled reason. */
export function isMaterializeStructureMutable(
  commandCode: StructureMaterialize['commandCode'],
  competitionStatus: CompetitionStatus,
  stageStatus: StageStatus,
): boolean {
  if (commandCode === 'MaterializeFromOccupiedSlots') {
    return (
      competitionStatus !== 'Suspended' &&
      competitionStatus !== 'Completed' &&
      competitionStatus !== 'Archived' &&
      (stageStatus === 'Draft' || stageStatus === 'Ready')
    );
  }

  return (
    (competitionStatus === 'Draft' || competitionStatus === 'Ready') &&
    (stageStatus === 'Draft' || stageStatus === 'Ready')
  );
}

export type MaterializePresentation =
  | 'actionable'
  | 'terminal'
  | 'frozen'
  | 'blocked';

/**
 * Presentation branch from read counts + lifecycle status.
 * Blocked captions use `disabledReason` on the read when present.
 */
export function classifyMaterializePresentation(
  materialize: StructureMaterialize,
  competitionStatus: CompetitionStatus,
  stageStatus: StageStatus,
): MaterializePresentation {
  if (materialize.commandEnabled) {
    return 'actionable';
  }

  const target = materialize.determinedCount;
  const { materializedCount } = materialize;

  if (target > 0 && materializedCount >= target) {
    return 'terminal';
  }

  if (
    target > 0 &&
    materializedCount < target &&
    !isMaterializeStructureMutable(
      materialize.commandCode,
      competitionStatus,
      stageStatus,
    )
  ) {
    return 'frozen';
  }

  return 'blocked';
}

export function materializeDisabledCaption(
  materialize: StructureMaterialize,
  t: (key: string) => string,
): string {
  const code = materialize.disabledReason?.trim();
  if (code) {
    const key = `fiche.materialize.disabledReason.${code}`;
    const msg = t(key);
    if (msg !== key) {
      return msg;
    }
  }
  return t('fiche.materialize.blockedGeneric');
}

export function showMaterializeActionBlock(
  presentation: MaterializePresentation,
): presentation is 'actionable' | 'blocked' {
  return presentation === 'actionable' || presentation === 'blocked';
}

export function materializeFrozenRemainingCount(
  materialize: StructureMaterialize,
): number {
  return Math.max(0, materialize.determinedCount - materialize.materializedCount);
}

export function showMaterializeFrozenRemainingBanner(
  presentation: MaterializePresentation,
  materialize: StructureMaterialize,
): boolean {
  return (
    presentation === 'frozen' && materializeFrozenRemainingCount(materialize) > 0
  );
}

/**
 * Phase matches list link — when the hub reports attached matches on this stage.
 * Uses `stage.matchCount` (not materialize.materializedCount alone) so stale
 * production read counts do not offer an empty matches route.
 */
/** Canonical Cup mono-round entry round still has vacant or incomplete pairs (decision UX tranche 2). */
export function resolveStartStageProductionBlocker(
  lifecycle: StructureStageHubSummary['lifecycle'] | null | undefined,
): 'IncompleteEntryRound' | 'UnmaterializedConfrontations' | null {
  const transition = lifecycle?.transition;
  if (transition?.code !== 'StartStage' || transition.enabled) {
    return null;
  }
  const reason = transition.disabledReasonCode;
  if (
    reason === 'IncompleteEntryRound' ||
    reason === 'UnmaterializedConfrontations'
  ) {
    return reason;
  }
  return null;
}

export function cupCanonicalEntryRoundIncomplete(
  materialize: StructureMaterialize | null | undefined,
): boolean {
  if (materialize?.commandCode !== 'MaterializeFromOccupiedSlots') {
    return false;
  }

  const canonical = materialize.canonicalEntryPairCount ?? 0;
  const undetermined = materialize.undeterminedCanonicalEntryPairCount ?? 0;
  return canonical > 0 && undetermined > 0;
}

/** Canonical entry-round gaps block Start while the phase is Ready (Cup gate 1a). */
export function cupEntryRoundBlocksPhaseStart(
  stage: Pick<StructureStageHubSummary, 'status'>,
  materialize: StructureMaterialize | null | undefined,
): boolean {
  return stage.status === 'Ready' && cupCanonicalEntryRoundIncomplete(materialize);
}

/** Leg count for Cup materialize match production (hub tie format). */
export function cupLegsPerMaterializedConfrontation(
  stage: Pick<
    StructureStageHubSummary,
    'defaultTieFormat' | 'numberOfLegs'
  >,
): number {
  const legs =
    stage.defaultTieFormat?.numberOfLegs ?? stage.numberOfLegs ?? 1;
  return legs > 0 ? legs : 1;
}

/**
 * Matchs tile gauge: attached matches vs match slots for determined confrontations (legs-aware).
 * Distinct from canonical entry-pair placement — see {@link cupEntryRoundPlacementGaugeRatio}.
 */
export function cupMatchProductionGaugeRatio(
  stage: Pick<StructureStageHubSummary, 'matchCount' | 'defaultTieFormat' | 'numberOfLegs'>,
  materialize: StructureMaterialize,
): { filled: number; capacity: number } | null {
  const legs = cupLegsPerMaterializedConfrontation(stage);
  const determined = materialize.determinedCount;
  if (determined <= 0 && stage.matchCount <= 0) {
    return null;
  }
  const capacity = Math.max(determined, materialize.materializedCount) * legs;
  if (capacity <= 0) {
    return null;
  }
  return { filled: stage.matchCount, capacity };
}

/** Groups placement gauge when pools are not ready for confrontation production. */
export function groupsPlacementGaugeRatio(
  materialize: StructureMaterialize,
): { filled: number; capacity: number } | null {
  if (!isGroupsPoolPlacementIncomplete(materialize)) {
    return null;
  }
  const capacity = materialize.groupsPlacementCapacityCount ?? 0;
  const filled = materialize.groupsPlacementFilledCount ?? 0;
  if (capacity <= 0) {
    return null;
  }
  return { filled, capacity };
}

/** Entry-round placement gauge (canonical pairs with two distinct entries). */
export function cupEntryRoundPlacementGaugeRatio(
  materialize: StructureMaterialize,
): { filled: number; capacity: number } | null {
  const capacity = materialize.canonicalEntryPairCount ?? 0;
  const undetermined = materialize.undeterminedCanonicalEntryPairCount ?? 0;
  if (capacity <= 0 || undetermined <= 0) {
    return null;
  }
  return { filled: capacity - undetermined, capacity };
}

export function shouldConfirmCupMaterializeWithIncompleteEntryRound(
  materialize: StructureMaterialize | null | undefined,
): boolean {
  return (
    materialize?.commandEnabled === true &&
    cupCanonicalEntryRoundIncomplete(materialize)
  );
}

export function showViewMaterializedMatchesLink(
  stage: Pick<StructureStageHubSummary, 'matchCount'>,
  materialize?: StructureMaterialize | null,
): boolean {
  if (stage.matchCount <= 0) {
    return false;
  }
  if (materialize != null) {
    return materialize.materializedCount > 0;
  }
  return true;
}
