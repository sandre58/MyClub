import type { StageRound } from '../types';

export type FixtureOption = {
  id: string;
  label: string;
};

/** Attribution select options — Match #n (round-local order), no phase name. */
export function listFixtureOptions(
  rounds: StageRound[],
  formatMatch: (n: number) => string,
): FixtureOption[] {
  const options: FixtureOption[] = [];
  for (const round of rounds) {
    round.fixtures.forEach((fixture, index) => {
      options.push({
        id: fixture.id,
        label: formatMatch(index + 1),
      });
    });
  }
  return options;
}

export function nextPowerOfTwo(n: number): number {
  let p = 1;
  while (p < n) p *= 2;
  return Math.max(p, 2);
}
