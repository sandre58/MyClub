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

/** Single-round shape with stored round tie differing from stage default at load. */
export function singleRoundDriftRoundId(
  roundIds: string[],
  baselineRounds: Record<string, TieFormState>,
  baselinePhase: TieFormState,
): string | null {
  if (roundIds.length !== 1) {
    return null;
  }
  const roundId = roundIds[0]!;
  if (inheritsFromPhaseAtLoad(roundId, baselineRounds, baselinePhase)) {
    return null;
  }
  return roundId;
}
