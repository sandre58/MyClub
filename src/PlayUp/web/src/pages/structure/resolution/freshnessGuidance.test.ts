import { describe, expect, it } from 'vitest';

import type { StructureResolutionOccupant } from '../../../types';
import { freshnessAction, indeterminacyExplanation } from './freshnessGuidance';

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

  it('drops dead-end checks for an empty selection, a missing source, and Autre', () => {
    for (const indeterminacyCause of [
      'SelectionVide',
      'SourceAbsente',
      'Autre',
      'ConditionNonRemplie',
    ] as const) {
      expect(
        freshnessAction(
          {
            ...base,
            indeterminacyCause,
            upstreamStageId: 'poules',
          },
          'comp',
          { canReplace: false },
        ),
      ).toBeNull();
    }
  });

  it('opens the standing when several teams are tied', () => {
    expect(
      freshnessAction(
        {
          ...base,
          indeterminacyCause: 'EgaliteClassement',
          upstreamStageId: 'poules',
        },
        'comp',
        { canReplace: false },
      ),
    ).toMatchObject({
      kind: 'navigate',
      href: '/competitions/comp/structure?stage=poules&section=classement',
      labelKey: 'resolution.freshness.indeterminacy.EgaliteClassement.action',
    });

    expect(
      freshnessAction(
        { ...base, indeterminacyCause: 'EgaliteClassement' },
        'comp',
        { canReplace: false },
      ),
    ).toBeNull();
  });

  it('opens the source phase for an invalid source setup and does not release it', () => {
    expect(
      freshnessAction(
        {
          ...base,
          indeterminacyCause: 'ConfigurationInvalide',
          upstreamStageId: 'poules',
        },
        'comp',
        { canReplace: false },
      ),
    ).toEqual({
      kind: 'navigate',
      href: '/competitions/comp/structure?stage=poules',
      labelKey:
        'resolution.freshness.indeterminacy.ConfigurationInvalide.action',
    });

    expect(
      freshnessAction(
        { ...base, indeterminacyCause: 'ConfigurationInvalide' },
        'comp',
        { canReplace: false },
      ),
    ).toMatchObject({ href: '/competitions/comp/regulation' });
  });

  it('names the team and the unplayed match in the explanation', () => {
    expect(
      indeterminacyExplanation(
        {
          ...base,
          indeterminacyCause: 'MatchNonJoue',
          actionMatchLabel: 'Est – Sud',
        },
        'Nord',
      ),
    ).toEqual({
      team: 'Nord',
      detailKey: 'resolution.freshness.indeterminacy.MatchNonJoue.clauseNamed',
      detailParams: { match: 'Est – Sud' },
    });

    expect(
      indeterminacyExplanation(
        { ...base, indeterminacyCause: 'ConfigurationInvalide' },
        'Nord',
      )?.detailKey,
    ).toBe('resolution.freshness.indeterminacy.ConfigurationInvalide.clause');

    expect(
      indeterminacyExplanation(
        { ...base, indeterminacyCause: 'MatchNonJoue' },
        '  ',
      ),
    ).toBeNull();
  });

  it('releases a releasable occupant when the surface offers the release', () => {
    expect(
      freshnessAction(
        {
          ...base,
          state: 'Indetermine',
          indeterminacyCause: 'SelectionVide',
          releasable: true,
        },
        'comp',
        { canReplace: true, canRelease: true },
      ),
    ).toEqual({ kind: 'release' });

    expect(
      freshnessAction(
        {
          ...base,
          state: 'Indetermine',
          indeterminacyCause: 'SelectionVide',
          releasable: true,
        },
        'comp',
        { canReplace: true },
      ),
    ).toBeNull();
  });
});
