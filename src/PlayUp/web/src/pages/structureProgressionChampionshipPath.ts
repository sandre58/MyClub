// -----------------------------------------------------------------------
// Championship-path terminal round for Prog Winner Sorties.
// Mirrors Domain ProgressionChampionshipPath (halving KO prefix).
// SoT: Winner = championship exit — not max(round list order).
// -----------------------------------------------------------------------

export type ChampionshipPathRound = {
  id: string;
  name: string;
  fixtures: readonly unknown[];
};

/**
 * Mono-round form (Cup V1): that round is terminal without fixtures.
 * Multi-round interim: longest classic KO prefix among rounds with fixtures
 * (halving). Remaining rounds (e.g. 3ᵉ after Finale) excluded.
 */
export function championshipTerminalRound<T extends ChampionshipPathRound>(
  rounds: readonly T[],
): T | null {
  if (rounds.length === 0) {
    return null;
  }
  if (rounds.length === 1) {
    return rounds[0]!;
  }

  const withFixtures = rounds.filter((r) => r.fixtures.length > 0);
  if (withFixtures.length === 0) {
    return null;
  }
  if (withFixtures.length === 1) {
    return withFixtures[0]!;
  }

  const prefix: T[] = [withFixtures[0]!];
  for (let i = 1; i < withFixtures.length; i++) {
    const previousCount = prefix[prefix.length - 1]!.fixtures.length;
    const current = withFixtures[i]!;
    if (previousCount > 1 && current.fixtures.length === previousCount / 2) {
      prefix.push(current);
      continue;
    }
    break;
  }
  return prefix[prefix.length - 1]!;
}

/** Rounds that can expand Sorties — fixtures and/or structural (all rounds when mono). */
export function roundsWithFixtures<T extends ChampionshipPathRound>(
  rounds: readonly T[],
): T[] {
  if (rounds.length === 1) {
    return [...rounds];
  }
  return rounds.filter((r) => r.fixtures.length > 0);
}
