import { render } from '@testing-library/react';
import { createElement } from 'react';
import { beforeAll, describe, expect, it } from 'vitest';

import i18n from '../../../i18n';
import frStructure from '../../../i18n/locales/fr/structure.json';
import frRegulation from '../../../i18n/locales/fr/regulation.json';
import {
  buildConfrontationDigestTokens,
  confrontationFormatDiffersFromPhase,
  confrontationFormatScopeHintKey,
  confrontationRoundDigestDeltaParts,
} from './structureRegulationDigest.build';
import { tieFormFromSummary } from '../../stage/tieFormatForm';

beforeAll(async () => {
  await i18n.changeLanguage('fr');
});

function tStructure(key: string, options?: Record<string, unknown>) {
  return i18n.t(key, { ns: 'structure', ...options });
}

function tReg(key: string, options?: Record<string, unknown>) {
  return i18n.t(key, { ns: 'regulation', ...options });
}

describe('structure i18n keys for confrontation digest', () => {
  it('resolves regulation legs labels from bundled fr locale', () => {
    expect(tStructure('regulation.legsOne')).toBe(
      frStructure.regulation.legsOne,
    );
    expect(tStructure('regulation.legsTwo')).toBe(
      frStructure.regulation.legsTwo,
    );
    expect(tReg('tokens.tiePenalties')).toBe(frRegulation.tokens.tiePenalties);
  });
});

describe('confrontationRoundDigestDeltaParts', () => {
  it('omits rules that match the phase (no TAB duplication)', () => {
    const phase = tieFormFromSummary({
      numberOfLegs: 2,
      aggregateScoring: true,
      hasAwayGoalsRule: false,
      hasTieExtraTime: false,
      hasTiePenaltyShootout: true,
    });
    const round = tieFormFromSummary({
      numberOfLegs: 1,
      aggregateScoring: false,
      hasAwayGoalsRule: false,
      hasTieExtraTime: false,
      hasTiePenaltyShootout: true,
    });
    const parts = confrontationRoundDigestDeltaParts(
      round,
      phase,
      tStructure,
      tReg,
    );
    expect(parts).toEqual([frStructure.regulation.legsOne]);
    expect(parts.join(' ')).not.toContain(frRegulation.tokens.tiePenalties);
  });
});

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
    const roundToken = tokens.find((token) => token.key === 'round-f');
    expect(roundToken).toBeDefined();
    const { container } = render(
      createElement('div', null, roundToken!.content),
    );
    const roundText = container.textContent ?? '';
    expect(roundText).toContain(frStructure.regulation.legsOne);
    expect(roundText).not.toContain(frRegulation.tokens.tiePenalties);
    expect((roundText.match(/TAB/g) ?? []).length).toBe(0);
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
