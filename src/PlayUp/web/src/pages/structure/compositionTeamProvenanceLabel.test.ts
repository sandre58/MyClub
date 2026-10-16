import { describe, expect, it } from 'vitest';
import { compactCompositionProvenanceBadge } from './compositionTeamProvenanceLabel';

describe('compactCompositionProvenanceBadge', () => {
  it('omits peer name when a single upstream phase feeds the band', () => {
    expect(
      compactCompositionProvenanceBadge(
        'Phase poules',
        {
          badge: '1er',
          context: 'Poule A',
          family: 'place',
        },
        1,
      ),
    ).toBe('1er·A');
  });

  it('prefixes peer when several upstream phases are merged', () => {
    expect(
      compactCompositionProvenanceBadge(
        'Phase de groupes',
        {
          badge: '2e',
          context: 'Groupe B',
          family: 'place',
        },
        2,
      ),
    ).toBe('groupes · 2e·B');
  });

  it('shortens progression outcome and match context', () => {
    expect(
      compactCompositionProvenanceBadge(
        'Quarts',
        {
          badge: 'Vainqueur',
          context: 'QF1 · Match #3',
          family: 'result',
        },
        1,
      ),
    ).toBe('Vain. · QF1 #3');
  });

  it('keeps points gate extra when present', () => {
    expect(
      compactCompositionProvenanceBadge(
        'Poules',
        {
          badge: '3e',
          context: 'Classement',
          extra: '≥ 4 pts',
          family: 'place',
        },
        1,
      ),
    ).toBe('3e·Cls. · ≥ 4 pts');
  });
});
