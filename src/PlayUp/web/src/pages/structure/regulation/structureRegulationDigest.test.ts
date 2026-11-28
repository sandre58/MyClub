import { describe, expect, it } from 'vitest';

import {
  buildConfrontationDigestTokens,
  confrontationFormatDiffersFromPhase,
  confrontationFormatScopeHintKey,
} from './structureRegulationDigest.build';

const tStructure = (key: string, options?: Record<string, unknown>) => {
  const table: Record<string, string> = {
    'fiche.confrontationPhasePrefix': 'Phase',
    'stage.legsOne': '1 manche',
    'stage.legsTwo': '2 manches',
    'fiche.confrontationAdded': `+ ${String(options?.rule ?? '')}`,
    'fiche.confrontationWithout': `sans ${String(options?.rule ?? '')}`,
    'fiche.confrontationMoreRounds': `+ ${String(options?.count ?? '')} tours`,
  };
  return table[key] ?? key;
};

const tReg = (key: string) => {
  const table: Record<string, string> = {
    'tokens.tieAggregateShort': 'cumul',
    'tokens.tieAwayGoalsShort': 'buts ext.',
    'tokens.tieExtraTime': 'Prolongations',
    'tokens.tiePenalties': 'TAB',
  };
  return table[key] ?? key;
};

describe('confrontationFormatScopeHintKey', () => {
  it('returns uniform hint when segments empty or equivalent to phase', () => {
    expect(
      confrontationFormatScopeHintKey({
        confrontationSegments: null,
        defaultTieFormat: {
          numberOfLegs: 1,
          aggregateScoring: false,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        },
      }),
    ).toBe('fiche.confrontationFormatHint.uniform');
    expect(
      confrontationFormatScopeHintKey({
        confrontationSegments: [],
        defaultTieFormat: {
          numberOfLegs: 1,
          aggregateScoring: false,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        },
      }),
    ).toBe('fiche.confrontationFormatHint.uniform');
    expect(
      confrontationFormatScopeHintKey({
        defaultTieFormat: {
          numberOfLegs: 2,
          aggregateScoring: true,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        },
        confrontationSegments: [
          {
            rounds: [
              {
                roundId: 'a',
                name: 'Finale',
                sortOrder: 0,
                inheritsDefaultTieFormat: false,
              },
            ],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        ],
      }),
    ).toBe('fiche.confrontationFormatHint.uniform');
  });

  it('returns per-round hint when a segment signature differs from phase', () => {
    expect(
      confrontationFormatScopeHintKey({
        defaultTieFormat: {
          numberOfLegs: 2,
          aggregateScoring: true,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: true,
        },
        confrontationSegments: [
          {
            rounds: [],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: true,
          },
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
    ).toBe('fiche.confrontationFormatHint.perRound');
  });
});

describe('confrontationFormatDiffersFromPhase', () => {
  it('is false for multiple segments equal to phase default', () => {
    expect(
      confrontationFormatDiffersFromPhase({
        defaultTieFormat: {
          numberOfLegs: 2,
          aggregateScoring: true,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        },
        confrontationSegments: [
          {
            rounds: [],
            numberOfLegs: 2,
            aggregateScoring: true,
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
    ).toBe(false);
  });

  it('is false when phase default is null and segments are empty', () => {
    expect(
      confrontationFormatDiffersFromPhase({
        defaultTieFormat: null,
        confrontationSegments: [],
      }),
    ).toBe(false);
  });
});

describe('buildConfrontationDigestTokens', () => {
  it('lists phase line and per-round deltas when segment differs', () => {
    const tokens = buildConfrontationDigestTokens(
      {
        defaultTieFormat: {
          numberOfLegs: 2,
          aggregateScoring: true,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: true,
        },
        confrontationSegments: [
          {
            rounds: [
              {
                roundId: 'f',
                name: 'Finale',
                sortOrder: 1,
                inheritsDefaultTieFormat: true,
              },
            ],
            numberOfLegs: 1,
            aggregateScoring: false,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: true,
          },
        ],
      } as never,
      tStructure,
      tReg,
    );
    expect(tokens[0]?.key).toBe('phase-format');
    expect(tokens.some((t) => t.key === 'round-f')).toBe(true);
  });

  it('omits round lines when override signature matches phase', () => {
    const tokens = buildConfrontationDigestTokens(
      {
        defaultTieFormat: {
          numberOfLegs: 1,
          aggregateScoring: false,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        },
        confrontationSegments: [
          {
            rounds: [
              {
                roundId: 'f',
                name: 'Finale',
                sortOrder: 0,
                inheritsDefaultTieFormat: false,
              },
            ],
            numberOfLegs: 1,
            aggregateScoring: false,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        ],
      } as never,
      tStructure,
      tReg,
    );
    expect(tokens).toHaveLength(1);
    expect(tokens[0]?.key).toBe('phase-format');
  });
});
