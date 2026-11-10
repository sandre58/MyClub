import { describe, expect, it } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { freshnessGuidance } from './freshnessGuidance';

const base: StructureResolutionOccupant = {
  target: 'Slot',
  entryId: 'e1',
  provenanceKind: 'Progression',
  sourcePathId: 'P|abc',
  state: 'Indetermine',
  slotKey: 'A',
};

describe('freshnessGuidance', () => {
  it('links an upstream-stale badge to the source phase', () => {
    expect(
      freshnessGuidance(
        {
          ...base,
          state: 'Perime',
          stalenessCause: 'AmontPerime',
          upstreamStageId: 'phase-a',
        },
        'comp',
      ),
    ).toEqual({
      kind: 'upstream',
      href: '/competitions/comp/structure?stage=phase-a',
    });
  });

  it('keeps replayable staleness other than AmontPerime off navigation guidance', () => {
    expect(
      freshnessGuidance(
        {
          ...base,
          provenanceKind: 'Qualification',
          state: 'Perime',
          stalenessCause: 'ResultatAmont',
          upstreamStageId: 'phase-a',
        },
        'comp',
      ),
    ).toBeNull();
  });

  it('points an unplayed match at the match and a tie at the match', () => {
    expect(
      freshnessGuidance(
        {
          ...base,
          indeterminacyCause: 'MatchNonJoue',
          actionMatchId: 'match-1',
          actionMatchLabel: 'Nord – Est',
          upstreamStageId: 'phase-a',
        },
        'comp',
      ),
    ).toMatchObject({ href: '/matches/match-1', matchLabel: 'Nord – Est' });

    expect(
      freshnessGuidance(
        {
          ...base,
          indeterminacyCause: 'Egalite',
          actionMatchId: 'match-2',
        },
        'comp',
      )?.href,
    ).toBe('/matches/match-2');
  });

  it('opens the standing for an empty selection and skips a missing source without a phase', () => {
    expect(
      freshnessGuidance(
        {
          ...base,
          indeterminacyCause: 'SelectionVide',
          upstreamStageId: 'poules',
        },
        'comp',
      )?.href,
    ).toBe('/competitions/comp/structure?stage=poules&section=classement');

    expect(
      freshnessGuidance(
        { ...base, indeterminacyCause: 'SourceAbsente' },
        'comp',
      ),
    ).toBeNull();
  });

  it('links a known source phase when the rule itself is missing', () => {
    expect(
      freshnessGuidance(
        {
          ...base,
          indeterminacyCause: 'SourceAbsente',
          upstreamStageId: 'phase-a',
        },
        'comp',
      )?.href,
    ).toBe('/competitions/comp/structure?stage=phase-a');
  });
});
