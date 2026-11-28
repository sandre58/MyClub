import { render } from '@testing-library/react';
import { createElement } from 'react';
import { beforeAll, describe, expect, it } from 'vitest';

import i18n from '../../../i18n';
import frRegulation from '../../../i18n/locales/fr/regulation.json';
import {
  buildConfrontationDigestTokens,
  confrontationDeltaFactsFromForms,
  confrontationFormatDiffersFromPhase,
  confrontationFormatScopeHintKey,
  confrontationFactsFromForm,
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

describe('confrontationDeltaFactsFromForms', () => {
  it('shows explicit sans TAB when phase has shootout and round does not', () => {
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
      hasTiePenaltyShootout: false,
    });
    const chips = confrontationDeltaFactsFromForms(
      round,
      phase,
      'test',
      tStructure,
      tReg,
    );
    const tabChip = chips.find((c) => c.key === 'test-tab');
    expect(tabChip).toBeDefined();
    expect(String(tabChip!.content)).toBe(
      tStructure('fiche.confrontationWithout', {
        rule: frRegulation.tokens.tiePenalties,
      }),
    );
    expect(String(tabChip!.content)).not.toContain('+');
    expect(chips.find((c) => c.key === 'test-legs')?.content).toBe(
      frRegulation.tokens.tieOneLeg,
    );
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
        ],
      }),
    ).toBe(false);
  });
});

describe('buildConfrontationDigestTokens', () => {
  it('renders phase row without Phase label and long regulation tokens', () => {
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
                sortOrder: 0,
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
    const phaseRow = tokens[0];
    expect(phaseRow?.variant).toBe('confrontation-row');
    const { container } = render(createElement('div', null, phaseRow!.content));
    const text = container.textContent ?? '';
    expect(text).toContain(frRegulation.tokens.tieTwoLegs);
    expect(text).not.toMatch(/Phase\s*:/);
    expect(text).not.toContain('·');
    expect(text).not.toContain('+');
  });

  it('omits round rows when override signature matches phase', () => {
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
    expect(
      confrontationFactsFromForm(
        tieFormFromSummary({
          numberOfLegs: 1,
          aggregateScoring: false,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        }),
        'x',
        tReg,
      )[0]?.content,
    ).toBe(frRegulation.tokens.tieOneLeg);
  });
});
