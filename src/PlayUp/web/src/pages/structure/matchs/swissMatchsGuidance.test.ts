import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../../types';
import { resolveSwissMatchsGuidance } from './swissMatchsGuidance';

function swissStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: 's-swiss',
    name: 'Swiss',
    status: 'Ready',
    formatKind: 'Swiss',
    teamCount: 8,
    matchCount: 0,
    swissRoundCount: 3,
    matchdayCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    ...overrides,
  };
}

describe('resolveSwissMatchsGuidance', () => {
  it('returns null for non-Swiss formats', () => {
    expect(
      resolveSwissMatchsGuidance(swissStage({ formatKind: 'Championship' })),
    ).toBeNull();
  });

  it('A — Ready, no generated rounds', () => {
    const view = resolveSwissMatchsGuidance(
      swissStage({ status: 'Ready', matchdayCount: 0 }),
    );
    expect(view?.kind).toBe('A');
    expect(view?.messageKey).toBe('fiche.swissGuidance.readyNoRounds');
  });

  it('B — Running, no generated rounds', () => {
    const view = resolveSwissMatchsGuidance(
      swissStage({ status: 'Running', matchdayCount: 0 }),
    );
    expect(view?.kind).toBe('B');
  });

  it('B — Suspended with zero rounds matches Running guidance', () => {
    const view = resolveSwissMatchsGuidance(
      swissStage({ status: 'Suspended', matchdayCount: 0 }),
    );
    expect(view?.kind).toBe('B');
  });

  it('C — open matches', () => {
    const view = resolveSwissMatchsGuidance(
      swissStage({
        status: 'Running',
        matchdayCount: 1,
        lifecycle: {
          transition: { code: 'CompleteStage', enabled: false },
          matchProgress: {
            total: 4,
            finished: 2,
            cancelled: 0,
            open: 2,
            canComplete: false,
          },
        },
      }),
    );
    expect(view?.kind).toBe('C');
  });

  it('D — all matches closed, rounds remain', () => {
    const view = resolveSwissMatchsGuidance(
      swissStage({
        status: 'Running',
        matchdayCount: 1,
        lifecycle: {
          transition: { code: 'CompleteStage', enabled: false },
          matchProgress: {
            total: 4,
            finished: 4,
            cancelled: 0,
            open: 0,
            canComplete: false,
          },
        },
      }),
    );
    expect(view?.kind).toBe('D');
  });

  it('neutral when all matches cancelled (false D guard)', () => {
    const view = resolveSwissMatchsGuidance(
      swissStage({
        status: 'Running',
        matchdayCount: 1,
        lifecycle: {
          transition: { code: 'CompleteStage', enabled: false },
          matchProgress: {
            total: 4,
            finished: 0,
            cancelled: 4,
            open: 0,
            canComplete: false,
          },
        },
      }),
    );
    expect(view?.kind).toBe('neutral');
  });

  it('E — all planned rounds generated', () => {
    const view = resolveSwissMatchsGuidance(
      swissStage({
        status: 'Running',
        matchdayCount: 3,
        swissRoundCount: 3,
        lifecycle: {
          transition: { code: 'CompleteStage', enabled: false },
          matchProgress: {
            total: 12,
            finished: 6,
            cancelled: 0,
            open: 6,
            canComplete: false,
          },
        },
      }),
    );
    expect(view?.kind).toBe('E');
  });

  it('does not expose GenerateNextRound action codes', () => {
    const cases = [
      swissStage({ status: 'Ready' }),
      swissStage({ status: 'Running', matchdayCount: 0 }),
      swissStage({
        status: 'Running',
        matchdayCount: 1,
        lifecycle: {
          transition: { code: 'CompleteStage', enabled: false },
          matchProgress: {
            total: 4,
            finished: 2,
            cancelled: 0,
            open: 2,
            canComplete: false,
          },
        },
      }),
    ];
    for (const stage of cases) {
      const view = resolveSwissMatchsGuidance(stage);
      const serialized = JSON.stringify(view);
      expect(serialized).not.toContain('GenerateNextRound');
      expect(serialized).not.toContain('RunSwissRounds');
      expect(serialized).not.toContain('link');
    }
  });
});
