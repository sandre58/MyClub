import type { ReplaceRoundTieFormatRequest } from '../../types';
import { cloneTie, sameTie, type TieFormState } from './tieFormatForm';

export type TieFormatSessionFlags = {
  inheritsAtLoad: Record<string, boolean>;
  sessionOverride: Record<string, true>;
  sessionRealigned: Record<string, true>;
};

/** At load: round effective tie matches stage default (copied default, not an override). */
export function inheritsFromPhaseAtLoad(
  roundId: string,
  baselineRounds: Record<string, TieFormState>,
  baselinePhase: TieFormState,
): boolean {
  const round = baselineRounds[roundId];
  if (!round) {
    return true;
  }
  return sameTie(round, baselinePhase);
}

export function buildInheritsAtLoad(
  roundIds: string[],
  baselineRounds: Record<string, TieFormState>,
  baselinePhase: TieFormState,
): Record<string, boolean> {
  const out: Record<string, boolean> = {};
  for (const roundId of roundIds) {
    out[roundId] = inheritsFromPhaseAtLoad(
      roundId,
      baselineRounds,
      baselinePhase,
    );
  }
  return out;
}

export function roundEffectivelyInherits(
  roundId: string,
  flags: TieFormatSessionFlags,
): boolean {
  if (flags.sessionOverride[roundId]) {
    return false;
  }
  return Boolean(
    flags.inheritsAtLoad[roundId] || flags.sessionRealigned[roundId],
  );
}

export function isRoundCustomized(
  roundId: string,
  flags: TieFormatSessionFlags,
): boolean {
  return !roundEffectivelyInherits(roundId, flags);
}

export function applyPhaseFormToInheritedRounds(
  roundIds: string[],
  flags: TieFormatSessionFlags,
  phaseForm: TieFormState,
  roundForms: Record<string, TieFormState>,
): Record<string, TieFormState> {
  const next = { ...roundForms };
  for (const roundId of roundIds) {
    if (!roundEffectivelyInherits(roundId, flags)) {
      continue;
    }
    next[roundId] = cloneTie(phaseForm);
  }
  return next;
}

/** Round tie updates to persist (cascade inheritors when default changes). */
export function roundPersistenceUpdates(
  roundIds: string[],
  flags: TieFormatSessionFlags,
  phaseForm: TieFormState,
  roundForms: Record<string, TieFormState>,
  baselineRounds: Record<string, TieFormState>,
): Record<string, TieFormState> {
  const updates: Record<string, TieFormState> = {};
  for (const roundId of roundIds) {
    const baseline = baselineRounds[roundId];
    if (!baseline) {
      continue;
    }
    const target = isRoundCustomized(roundId, flags)
      ? roundForms[roundId]
      : phaseForm;
    if (!target) {
      continue;
    }
    if (!sameTie(target, baseline)) {
      updates[roundId] = target;
    }
  }
  return updates;
}

export function shouldShowRoundNavigation(
  roundCount: number,
  canEditRounds: boolean,
): boolean {
  return canEditRounds && roundCount > 1;
}

/** Single-round historical drift: DTO says inheriting but stored copy still diverges from phase at load. */
export function singleRoundDriftRoundId(
  roundIds: string[],
  inheritsAtLoad: Record<string, boolean>,
  baselineRounds: Record<string, TieFormState>,
  baselinePhase: TieFormState,
): string | null {
  if (roundIds.length !== 1) {
    return null;
  }
  const roundId = roundIds[0]!;
  if (!inheritsAtLoad[roundId]) {
    return null;
  }
  const round = baselineRounds[roundId];
  if (!round || sameTie(round, baselinePhase)) {
    return null;
  }
  return roundId;
}

export type AtomicRoundEdit = ReplaceRoundTieFormatRequest & { roundId: string };

/** PUT /tie-format-edits round payloads (inheritors at load omit edits on default-only saves). */
export function buildAtomicRoundEdits(
  roundIds: string[],
  inheritsAtLoad: Record<string, boolean>,
  flags: TieFormatSessionFlags,
  phaseForm: TieFormState,
  roundForms: Record<string, TieFormState>,
  toRoundRequest: (
    form: TieFormState,
    clear: boolean,
  ) => ReplaceRoundTieFormatRequest,
): AtomicRoundEdit[] {
  return roundIds.flatMap((roundId) => {
    const inherits = roundEffectivelyInherits(roundId, flags);
    if (inherits) {
      if (inheritsAtLoad[roundId]) {
        return [];
      }
      return [{ roundId, ...toRoundRequest(phaseForm, true) }];
    }
    const form = roundForms[roundId];
    if (!form) {
      return [];
    }
    return [{ roundId, ...toRoundRequest(form, false) }];
  });
}
