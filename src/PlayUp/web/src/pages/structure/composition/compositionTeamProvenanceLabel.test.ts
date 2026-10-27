import { describe, expect, it } from 'vitest';
import {
  compactCompositionProvenanceBadge,
  fullInterPhaseProvenanceLabel,
  progressionOutcomeFromRule,
  shortenPeerName,
} from './compositionTeamProvenanceLabel';

describe('compactCompositionProvenanceBadge', () => {
  it('formats group qualification as rank and Gr. id (no redundant phase)', () => {
    expect(
      compactCompositionProvenanceBadge('Phase poules', {
        badge: '1er',
        context: 'Poule A',
        family: 'place',
      }),
    ).toBe('1er · Gr. A');
  });

  it('formats progression anchor without W/L (outcome is chip chrome)', () => {
    expect(
      compactCompositionProvenanceBadge('Quarts', {
        badge: 'Vainqueur',
        context: 'P1',
        family: 'result',
      }),
    ).toBe('Quarts · P1');
    expect(
      compactCompositionProvenanceBadge('Huitièmes de finale', {
        badge: 'Perdant',
        context: 'P2',
        family: 'result',
      }),
    ).toBe('Huitième… · P2');
  });

  it('omits phase prefix for across-groups place (Meilleurs)', () => {
    expect(
      compactCompositionProvenanceBadge('Phase de groupes', {
        badge: '1er',
        context: 'Meilleurs 1',
        family: 'place',
      }),
    ).toBe('1er · Mlr. 1');
  });

  it('keeps points gate extra when present', () => {
    expect(
      compactCompositionProvenanceBadge('Poules', {
        badge: '3e',
        context: 'Classement',
        extra: '≥ 4 pts',
        family: 'place',
      }),
    ).toBe('3e · Cls. · ≥ 4 pts');
  });

  it('drops Match #n from compact context', () => {
    expect(
      compactCompositionProvenanceBadge('Quarts', {
        badge: 'Vainqueur',
        context: 'P1 · Match #3',
        family: 'result',
      }),
    ).toBe('Quarts · P1');
  });
});

describe('fullInterPhaseProvenanceLabel', () => {
  it('orders progression tooltip as outcome · phase · PairKey', () => {
    expect(
      fullInterPhaseProvenanceLabel('Huitièmes de finale', {
        badge: 'Vainqueur',
        context: 'P1',
        family: 'result',
      }),
    ).toBe('Vainqueur · Huitièmes de finale · P1');
  });
});

describe('progressionOutcomeFromRule', () => {
  it('reads outcome from progressionOutcome or badge label', () => {
    expect(
      progressionOutcomeFromRule({
        badge: 'Vainqueur',
        family: 'result',
        progressionOutcome: 'Winner',
      }),
    ).toBe('Winner');
    expect(
      progressionOutcomeFromRule({
        badge: 'Perdant',
        family: 'result',
      }),
    ).toBe('Loser');
  });
});

describe('shortenPeerName', () => {
  it('strips Phase de prefix', () => {
    expect(shortenPeerName('Phase de groupes')).toBe('groupes');
  });
});
