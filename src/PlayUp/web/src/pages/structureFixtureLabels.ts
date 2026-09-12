import type { StageRound } from '../types';

export type FixtureOption = {
  id: string;
  label: string;
};

/** Same shape as hub SourceLabel: round · #order [· A vs B when keys exist]. */
export function listFixtureOptions(rounds: StageRound[]): FixtureOption[] {
  const options: FixtureOption[] = [];
  for (const round of rounds) {
    round.fixtures.forEach((fixture, index) => {
      const order = index + 1;
      const a = fixture.slotAKey?.trim();
      const b = fixture.slotBKey?.trim();
      const hasSlots = !!a || !!b;
      const label = hasSlots
        ? `${round.name} · #${order} · ${a || '—'} vs ${b || '—'}`
        : `${round.name} · #${order}`;
      options.push({ id: fixture.id, label });
    });
  }
  return options;
}

export function nextPowerOfTwo(n: number): number {
  let p = 1;
  while (p < n) p *= 2;
  return Math.max(p, 2);
}
