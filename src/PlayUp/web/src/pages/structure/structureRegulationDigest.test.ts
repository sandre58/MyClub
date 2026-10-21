import { describe, expect, it } from 'vitest';

import { confrontationFormatScopeHintKey } from './structureRegulationDigest';

describe('confrontationFormatScopeHintKey', () => {
  it('returns uniform hint when at most one segment', () => {
    expect(
      confrontationFormatScopeHintKey({ confrontationSegments: null }),
    ).toBe('fiche.confrontationFormatHint.uniform');
    expect(confrontationFormatScopeHintKey({ confrontationSegments: [] })).toBe(
      'fiche.confrontationFormatHint.uniform',
    );
    expect(
      confrontationFormatScopeHintKey({
        confrontationSegments: [
          {
            rounds: [],
            numberOfLegs: 1,
            aggregateScoring: false,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        ],
      }),
    ).toBe('fiche.confrontationFormatHint.uniform');
  });

  it('returns per-round hint when multiple segments', () => {
    expect(
      confrontationFormatScopeHintKey({
        confrontationSegments: [
          {
            rounds: [],
            numberOfLegs: 1,
            aggregateScoring: false,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
          {
            rounds: [],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        ],
      }),
    ).toBe('fiche.confrontationFormatHint.perRound');
  });
});
