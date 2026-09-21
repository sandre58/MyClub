import { describe, expect, it } from 'vitest';
import {
  championshipTerminalRound,
  roundsWithFixtures,
} from './structureProgressionChampionshipPath';

function round(id: string, fixtureCount: number) {
  return {
    id,
    name: id,
    fixtures: Array.from({ length: fixtureCount }, (_, i) => ({ id: `${id}-${i}` })),
  };
}

describe('championshipTerminalRound', () => {
  it('returns the only round with fixtures', () => {
    expect(championshipTerminalRound([round('F', 1)])?.id).toBe('F');
  });

  it('ends the KO prefix at Finale before 3e place', () => {
    const rounds = [
      round('QF', 4),
      round('SF', 2),
      round('F', 1),
      round('3e', 1),
    ];
    expect(championshipTerminalRound(rounds)?.id).toBe('F');
  });

  it('uses the last round of a pure halving chain', () => {
    expect(
      championshipTerminalRound([
        round('R16', 8),
        round('QF', 4),
        round('SF', 2),
        round('F', 1),
      ])?.id,
    ).toBe('F');
  });

  it('skips empty rounds', () => {
    expect(
      championshipTerminalRound([
        { id: 'empty', name: 'empty', fixtures: [] },
        round('F', 1),
      ])?.id,
    ).toBe('F');
  });

  it('returns null when no fixtures', () => {
    expect(championshipTerminalRound([{ id: 'x', name: 'x', fixtures: [] }])).toBeNull();
  });
});

describe('roundsWithFixtures', () => {
  it('filters empty rounds', () => {
    expect(
      roundsWithFixtures([
        round('A', 2),
        { id: 'b', name: 'b', fixtures: [] },
      ]).map((r) => r.id),
    ).toEqual(['A']);
  });
});
