import { describe, expect, it } from 'vitest';
import {
  areCardsComplete,
  awardedRankCount,
  cardsFromApiPaths,
  cardsToApiPaths,
  emptyPlacementCard,
  groupCardsByRound,
  hasNonContiguousRanks,
  incompleteCardReason,
  serializeCards,
  createNextPlacementCard,
} from './structurePlacementAwardDraft';

const known = new Set(['fix-1', 'fix-2']);

describe('structurePlacementAwardDraft', () => {
  it('groups Winner+Loser paths for the same fixture into one card', () => {
    const cards = cardsFromApiPaths([
      { sourceFixtureId: 'fix-1', outcome: 'Winner', rank: 1 },
      { sourceFixtureId: 'fix-1', outcome: 'Loser', rank: 2 },
      { sourceFixtureId: 'fix-2', outcome: 'Winner', rank: 3 },
    ]);
    expect(cards).toHaveLength(2);
    expect(cards[0].sourceFixtureId).toBe('fix-1');
    expect(cards[0].winnerRank).toBe('1');
    expect(cards[0].loserRank).toBe('2');
    expect(cards[1].sourceFixtureId).toBe('fix-2');
    expect(cards[1].winnerRank).toBe('3');
    expect(cards[1].loserRank).toBe('');
  });

  it('expands cards to 0..2 Domain paths', () => {
    const card = emptyPlacementCard();
    card.sourceFixtureId = 'fix-1';
    card.winnerRank = '1';
    card.loserRank = '2';
    expect(cardsToApiPaths([card])).toEqual([
      { sourceFixtureId: 'fix-1', outcome: 'Winner', rank: 1 },
      { sourceFixtureId: 'fix-1', outcome: 'Loser', rank: 2 },
    ]);

    card.loserRank = '';
    expect(cardsToApiPaths([card])).toEqual([
      { sourceFixtureId: 'fix-1', outcome: 'Winner', rank: 1 },
    ]);
  });

  it('requires fixture and at least one rank for completeness', () => {
    const a = emptyPlacementCard();
    expect(incompleteCardReason(a, [a], known)).toBe('Fixture');

    a.sourceFixtureId = 'fix-1';
    expect(incompleteCardReason(a, [a], known)).toBe('Placement');

    a.winnerRank = '1';
    expect(incompleteCardReason(a, [a], known)).toBeNull();
    expect(areCardsComplete([a], known)).toBe(true);
  });

  it('rejects invalid ranks and unknown fixtures', () => {
    const a = emptyPlacementCard();
    a.sourceFixtureId = 'fix-1';
    a.winnerRank = '0';
    expect(incompleteCardReason(a, [a], known)).toBe('Rank');

    a.winnerRank = '1';
    a.sourceFixtureId = 'missing';
    expect(incompleteCardReason(a, [a], known)).toBe('UnknownFixture');
  });

  it('hard-blocks duplicate fixtures and duplicate ranks', () => {
    const a = emptyPlacementCard();
    a.sourceFixtureId = 'fix-1';
    a.winnerRank = '1';
    const b = emptyPlacementCard();
    b.sourceFixtureId = 'fix-1';
    b.loserRank = '2';
    expect(incompleteCardReason(a, [a, b], known)).toBe('DuplicateFixture');

    b.sourceFixtureId = 'fix-2';
    b.winnerRank = '1';
    b.loserRank = '';
    expect(incompleteCardReason(a, [a, b], known)).toBe('DuplicateRank');
    expect(incompleteCardReason(b, [a, b], known)).toBe('DuplicateRank');
  });

  it('flags Winner and Loser sharing the same rank on one card', () => {
    const a = emptyPlacementCard();
    a.sourceFixtureId = 'fix-1';
    a.winnerRank = '3';
    a.loserRank = '3';
    expect(incompleteCardReason(a, [a], known)).toBe('DuplicateRank');
  });

  it('counts awarded ranks and detects gaps between ranks (not missing 1)', () => {
    const a = emptyPlacementCard();
    a.sourceFixtureId = 'fix-1';
    a.winnerRank = '1';
    a.loserRank = '2';
    const b = emptyPlacementCard();
    b.sourceFixtureId = 'fix-2';
    b.winnerRank = '4';
    expect(awardedRankCount([a, b])).toBe(3);
    expect(hasNonContiguousRanks([a, b])).toBe(true);
    expect(hasNonContiguousRanks([a])).toBe(false);

    const bronze = emptyPlacementCard();
    bronze.sourceFixtureId = 'fix-bronze';
    bronze.winnerRank = '3';
    bronze.loserRank = '4';
    expect(hasNonContiguousRanks([bronze])).toBe(false);
  });

  it('serializes for dirty detection without card ids', () => {
    const a = emptyPlacementCard();
    a.sourceFixtureId = 'fix-1';
    a.winnerRank = '1';
    const b = { ...a, id: 'other' };
    expect(serializeCards([a])).toBe(serializeCards([b]));
  });

  it('groups cards by round overview order; orphans at the end', () => {
    const rounds = [
      {
        id: 'r-sf',
        name: 'Demi-finales',
        fixtures: [{ id: 'fix-sf1' }, { id: 'fix-sf2' }],
      },
      {
        id: 'r-final',
        name: 'Finale',
        fixtures: [{ id: 'fix-final' }],
      },
      {
        id: 'r-bronze',
        name: 'Petite finale',
        fixtures: [{ id: 'fix-bronze' }],
      },
    ];

    const final = emptyPlacementCard();
    final.sourceFixtureId = 'fix-final';
    final.winnerRank = '1';
    final.loserRank = '2';

    const bronze = emptyPlacementCard();
    bronze.sourceFixtureId = 'fix-bronze';
    bronze.winnerRank = '3';
    bronze.loserRank = '4';

    const empty = emptyPlacementCard();
    const unknown = emptyPlacementCard();
    unknown.sourceFixtureId = 'gone';
    unknown.winnerRank = '5';

    // Only attributed fixtures appear — sf fixtures never listed.
    const sections = groupCardsByRound([bronze, empty, final, unknown], rounds);

    expect(sections).toHaveLength(3);
    expect(sections[0].roundName).toBe('Finale');
    expect(sections[0].cards.map((c) => c.sourceFixtureId)).toEqual([
      'fix-final',
    ]);
    expect(sections[1].roundName).toBe('Petite finale');
    expect(sections[1].cards.map((c) => c.sourceFixtureId)).toEqual([
      'fix-bronze',
    ]);
    expect(sections[2].roundId).toBeNull();
    expect(sections[2].cards.map((c) => c.id)).toEqual([
      empty.id,
      unknown.id,
    ]);
  });

  it('prefills next card with first free fixture and lowest free ranks', () => {
    const fixtures = ['fx-1', 'fx-2', 'fx-3'];
    const first = createNextPlacementCard([], fixtures);
    expect(first.sourceFixtureId).toBe('fx-1');
    expect(first.winnerRank).toBe('1');
    expect(first.loserRank).toBe('2');

    const second = createNextPlacementCard([first], fixtures);
    expect(second.sourceFixtureId).toBe('fx-2');
    expect(second.winnerRank).toBe('3');
    expect(second.loserRank).toBe('4');

    const withGap = emptyPlacementCard();
    withGap.sourceFixtureId = 'fx-1';
    withGap.winnerRank = '1';
    withGap.loserRank = '3';
    const afterGap = createNextPlacementCard([withGap], fixtures);
    expect(afterGap.sourceFixtureId).toBe('fx-2');
    expect(afterGap.winnerRank).toBe('2');
    expect(afterGap.loserRank).toBe('4');

    const full = createNextPlacementCard(
      [
        { ...first, sourceFixtureId: 'fx-1' },
        { ...second, sourceFixtureId: 'fx-2' },
        {
          id: 'x',
          sourceFixtureId: 'fx-3',
          winnerRank: '5',
          loserRank: '6',
        },
      ],
      fixtures,
    );
    expect(full.sourceFixtureId).toBe('');
    expect(full.winnerRank).toBe('7');
    expect(full.loserRank).toBe('8');
  });
});
