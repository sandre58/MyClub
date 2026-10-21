import { describe, expect, it } from 'vitest';

import {
  applyPhaseFormToInheritedRounds,
  buildInheritsAtLoad,
  isRoundCustomized,
  roundPersistenceUpdates,
  shouldShowRoundNavigation,
  singleRoundDriftRoundId,
  type TieFormatSessionFlags,
} from './tieFormatEditorState';
import type { TieFormState } from './tieFormatForm';

const phaseDefault: TieFormState = {
  numberOfLegs: 1,
  aggregateScoring: false,
  hasAwayGoalsRule: false,
  hasExtraTimeRule: false,
  hasPenaltyShootoutRule: false,
};

const phaseTwoLegs: TieFormState = {
  numberOfLegs: 2,
  aggregateScoring: true,
  hasAwayGoalsRule: true,
  hasExtraTimeRule: true,
  hasPenaltyShootoutRule: false,
};

const customRound: TieFormState = {
  numberOfLegs: 1,
  aggregateScoring: false,
  hasAwayGoalsRule: false,
  hasExtraTimeRule: true,
  hasPenaltyShootoutRule: true,
};

function flags(
  inheritsAtLoad: Record<string, boolean>,
  overrides: Partial<TieFormatSessionFlags> = {},
): TieFormatSessionFlags {
  return {
    inheritsAtLoad,
    sessionOverride: overrides.sessionOverride ?? {},
    sessionRealigned: overrides.sessionRealigned ?? {},
  };
}

describe('tieFormatEditorState', () => {
  it('marks inheritors at load from baseline comparison', () => {
    const inherits = buildInheritsAtLoad(
      ['r1', 'r2'],
      { r1: phaseDefault, r2: customRound },
      phaseDefault,
    );
    expect(inherits.r1).toBe(true);
    expect(inherits.r2).toBe(false);
  });

  it('propagates phase edits to inheritors only', () => {
    const session = flags({ r1: true, r2: false });
    const next = applyPhaseFormToInheritedRounds(
      ['r1', 'r2'],
      session,
      phaseTwoLegs,
      { r1: phaseDefault, r2: customRound },
    );
    expect(next.r1).toEqual(phaseTwoLegs);
    expect(next.r2).toEqual(customRound);
  });

  it('does not treat inheritors as customized when phase form changes', () => {
    const session = flags({ r1: true });
    expect(isRoundCustomized('r1', session)).toBe(false);
  });

  it('keeps customized rounds when phase form changes', () => {
    const session = flags({ r1: false });
    expect(isRoundCustomized('r1', session)).toBe(true);
  });

  it('session override marks customized until cleared by realign flow', () => {
    const session = flags({ r1: true }, { sessionOverride: { r1: true } });
    expect(isRoundCustomized('r1', session)).toBe(true);
  });

  it('realign session flag restores inherit semantics', () => {
    const session = flags({ r1: false }, { sessionRealigned: { r1: true } });
    expect(isRoundCustomized('r1', session)).toBe(false);
  });

  it('persists cascade for inheritors when phase changes', () => {
    const session = flags({ r1: true, r2: false });
    const updates = roundPersistenceUpdates(
      ['r1', 'r2'],
      session,
      phaseTwoLegs,
      { r1: phaseTwoLegs, r2: customRound },
      { r1: phaseDefault, r2: customRound },
    );
    expect(updates.r1).toEqual(phaseTwoLegs);
    expect(updates.r2).toBeUndefined();
  });

  it('persists customized round when its form changed', () => {
    const changed = { ...customRound, hasPenaltyShootoutRule: false };
    const session = flags({ r1: false });
    const updates = roundPersistenceUpdates(
      ['r1'],
      session,
      phaseDefault,
      { r1: changed },
      { r1: customRound },
    );
    expect(updates.r1).toEqual(changed);
  });

  it('hides round navigation for a single round', () => {
    expect(shouldShowRoundNavigation(1, true)).toBe(false);
    expect(shouldShowRoundNavigation(2, true)).toBe(true);
  });

  it('detects single-round historical drift at load', () => {
    expect(
      singleRoundDriftRoundId(['r1'], { r1: customRound }, phaseDefault),
    ).toBe('r1');
    expect(
      singleRoundDriftRoundId(['r1'], { r1: phaseDefault }, phaseDefault),
    ).toBeNull();
    expect(
      singleRoundDriftRoundId(['r1', 'r2'], { r1: customRound }, phaseDefault),
    ).toBeNull();
  });
});
