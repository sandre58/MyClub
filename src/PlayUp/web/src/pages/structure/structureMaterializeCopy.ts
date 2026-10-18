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

  if (materialize.determinedCount > 0) {
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

  const determined = materializeDeterminedConfrontationsRatio(materialize);
  if (determined) {
    return { mode: 'determined', ratio: determined };
  }

  return null;
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
