import { describe, expect, it } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { freshnessPanel } from './freshnessGuidance';

const base: StructureResolutionOccupant = {
  target: 'Slot',
  entryId: 'e1',
  provenanceKind: 'Progression',
  sourcePathId: 'P|abc',
  state: 'Indetermine',
  slotKey: 'A',
};

describe('freshnessPanel', () => {
  it('links an upstream-stale badge to the source phase', () => {
    expect(
      freshnessPanel(
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

  it('keeps replayable staleness other than AmontPerime off the guidance panel', () => {
    expect(
      freshnessPanel(
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
      freshnessPanel(
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
      freshnessPanel(
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
      freshnessPanel(
        {
          ...base,
          indeterminacyCause: 'SelectionVide',
          upstreamStageId: 'poules',
        },
        'comp',
      )?.href,
    ).toBe('/competitions/comp/structure?stage=poules&section=classement');

    expect(
      freshnessPanel({ ...base, indeterminacyCause: 'SourceAbsente' }, 'comp')
        ?.href,
    ).toBeNull();
  });

  it('links a known source phase when the rule itself is missing', () => {
    expect(
      freshnessPanel(
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
