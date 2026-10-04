import type { CompetitionStatus, StageStatus, StructureMaterialize } from '../../types';

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
