import { describe, expect, it } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { freshnessAction } from './freshnessGuidance';

const base: StructureResolutionOccupant = {
  target: 'Slot',
  entryId: 'e1',
  provenanceKind: 'Progression',
  sourcePathId: 'P|abc',
  state: 'Indetermine',
  slotKey: 'A',
  cascadeReplaceable: false,
};

describe('freshnessAction', () => {
  it('navigates an upstream-stale badge to the source phase', () => {
    expect(
      freshnessAction(
        {
          ...base,
          state: 'Perime',
          stalenessCause: 'AmontPerime',
          upstreamStageId: 'phase-a',
        },
        'comp',
        { canReplace: true },
      ),
    ).toEqual({
      kind: 'navigate',
      href: '/competitions/comp/structure?stage=phase-a',
      labelKey: 'resolution.freshness.upstreamLink',
    });
  });

  it('replaces a stale upstream result when the surface can replay it', () => {
    expect(
      freshnessAction(
        {
          ...base,
          provenanceKind: 'Qualification',
          state: 'Perime',
          stalenessCause: 'ResultatAmont',
          upstreamStageId: 'phase-a',
          cascadeReplaceable: true,
        },
        'comp',
        { canReplace: true },
      ),
    ).toEqual({ kind: 'replace' });

    expect(
      freshnessAction(
        {
          ...base,
          provenanceKind: 'Qualification',
          state: 'Perime',
          stalenessCause: 'ResultatAmont',
          cascadeReplaceable: true,
        },
        'comp',
        { canReplace: false },
      ),
    ).toBeNull();
  });

  it('replaces a changed qualification rule only when the replay is possible', () => {
    const occupant: StructureResolutionOccupant = {
      ...base,
      provenanceKind: 'Qualification',
      state: 'Perime',
      stalenessCause: 'RegleQualification',
      cascadeReplaceable: false,
    };
    expect(freshnessAction(occupant, 'comp', { canReplace: true })).toBeNull();
    expect(
      freshnessAction({ ...occupant, cascadeReplaceable: true }, 'comp', {
        canReplace: true,
      }),
    ).toEqual({ kind: 'replace' });
  });

  it('points an unplayed match and a tie at the match, then the source phase', () => {
    expect(
      freshnessAction(
        {
          ...base,
          indeterminacyCause: 'MatchNonJoue',
          actionMatchId: 'match-1',
          actionMatchLabel: 'Nord – Est',
          upstreamStageId: 'phase-a',
        },
        'comp',
        { canReplace: false },
      ),
    ).toEqual({
      kind: 'navigate',
      href: '/matches/match-1',
      labelKey: 'resolution.freshness.indeterminacy.MatchNonJoue.actionNamed',
      labelParams: { match: 'Nord – Est' },
    });

    expect(
      freshnessAction(
        {
          ...base,
          indeterminacyCause: 'Egalite',
          actionMatchId: 'match-2',
          upstreamStageId: 'phase-a',
        },
        'comp',
        { canReplace: false },
      ),
    ).toMatchObject({
      kind: 'navigate',
      href: '/matches/match-2',
      labelKey: 'resolution.freshness.indeterminacy.Egalite.action',
    });

    const upstreamMatch = freshnessAction(
      {
        ...base,
        indeterminacyCause: 'MatchNonJoue',
        upstreamStageId: 'phase-a',
      },
      'comp',
      { canReplace: false },
    );
    expect(upstreamMatch?.kind).toBe('navigate');
    if (upstreamMatch?.kind === 'navigate') {
      expect(upstreamMatch.href).toBe(
        '/competitions/comp/structure?stage=phase-a',
      );
    }
  });

  it('opens the standing for an empty selection', () => {
    expect(
      freshnessAction(
        {
          ...base,
          indeterminacyCause: 'SelectionVide',
          upstreamStageId: 'poules',
        },
        'comp',
        { canReplace: false },
      ),
    ).toMatchObject({
      kind: 'navigate',
      href: '/competitions/comp/structure?stage=poules&section=classement',
      labelKey: 'resolution.freshness.indeterminacy.SelectionVide.action',
    });
  });

  it('links a known source for a missing rule and skips a missing source without a phase', () => {
    expect(
      freshnessAction(
        {
          ...base,
          indeterminacyCause: 'SourceAbsente',
          upstreamStageId: 'phase-a',
        },
        'comp',
        { canReplace: false },
      ),
    ).toMatchObject({
      href: '/competitions/comp/structure?stage=phase-a',
      labelKey: 'resolution.freshness.indeterminacy.SourceAbsente.action',
    });

    expect(
      freshnessAction(
        { ...base, indeterminacyCause: 'SourceAbsente' },
        'comp',
        { canReplace: false },
      ),
    ).toBeNull();

    const autre = freshnessAction(
      { ...base, indeterminacyCause: 'Autre', upstreamStageId: 'phase-a' },
      'comp',
      { canReplace: false },
    );
    expect(autre?.kind).toBe('navigate');
    if (autre?.kind === 'navigate') {
      expect(autre.href).toBe('/competitions/comp/structure?stage=phase-a');
    }

    expect(
      freshnessAction({ ...base, indeterminacyCause: 'Autre' }, 'comp', {
        canReplace: false,
      }),
    ).toBeNull();
  });
});
