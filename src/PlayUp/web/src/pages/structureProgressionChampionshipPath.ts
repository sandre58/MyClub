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
 * Longest classic KO prefix among rounds that have fixtures
 * (each round halves the previous fixture count). Terminal = last of prefix.
 * Remaining rounds (e.g. 3ᵉ after Finale) are excluded.
 */
export function championshipTerminalRound<T extends ChampionshipPathRound>(
  rounds: readonly T[],
): T | null {
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

export function roundsWithFixtures<T extends ChampionshipPathRound>(
  rounds: readonly T[],
): T[] {
  return rounds.filter((r) => r.fixtures.length > 0);
}
