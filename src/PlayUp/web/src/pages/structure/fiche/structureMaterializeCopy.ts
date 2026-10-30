import type {
  CompetitionStatus,
  StageStatus,
  StructureMaterialize,
  StructureStageHubSummary,
} from '../../../types';
import {
  resolvePhaseRosterIncompleteMaterializeMessageKey,
  isPhaseRosterIncompleteCode,
} from '../composition/structurePhaseRosterIncompletePresentation';
import { structureDiagnosticIssueCopyFromStage } from '../issues/structurePopulationShortfallPresentation';

/**
 * Ratio denominator from the Structure read (`determinedCount`).
 * See StructureMaterializeAssembler for Cup vs Groups semantics.
 */
export function materializeProductionTarget(
  materialize: StructureMaterialize,
): number {
  return materialize.determinedCount;
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
  'actionable' | 'terminal' | 'frozen' | 'blocked';

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
  stage?: Pick<
    StructureStageHubSummary,
    'compositionEntryCount' | 'compositionCapacity'
  > | null,
): string {
  const code = materialize.disabledReason?.trim();
  if (code) {
    const key = isPhaseRosterIncompleteCode(code)
      ? resolvePhaseRosterIncompleteMaterializeMessageKey(
          stage ? structureDiagnosticIssueCopyFromStage(stage) : undefined,
        )
      : `fiche.materialize.disabledReason.${code}`;
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
  return Math.max(
    0,
    materialize.determinedCount - materialize.materializedCount,
  );
}

export function showMaterializeFrozenRemainingBanner(
  presentation: MaterializePresentation,
  materialize: StructureMaterialize,
): boolean {
  return (
    presentation === 'frozen' &&
    materializeFrozenRemainingCount(materialize) > 0
  );
}

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
  return (
    stage.status === 'Ready' && cupCanonicalEntryRoundIncomplete(materialize)
  );
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
