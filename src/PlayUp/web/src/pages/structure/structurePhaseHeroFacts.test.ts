import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import { resolveStructurePhaseHeroFacts } from './structurePhaseHeroFacts';

function stage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: 's1',
    name: 'P',
    status: 'Draft',
    teamCount: 0,
    matchCount: 0,
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

describe('resolveStructurePhaseHeroFacts', () => {
  it('Championship uses configuredMatchdayCount including zero', () => {
    const withDays = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Championship',
        configuredMatchdayCount: 5,
        matchdayCount: 0,
      }),
      false,
    );
    expect(withDays).toContainEqual({ statKey: 'matchdays', value: 5 });

    const zero = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Championship',
        configuredMatchdayCount: 0,
        matchdayCount: 0,
      }),
      false,
    );
    expect(zero).toContainEqual({ statKey: 'matchdays', value: 0 });
  });

  it('Championship defaults configured matchdays to zero when absent on wire', () => {
    const facts = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Championship',
        matchdayCount: 1,
      }),
      false,
    );
    expect(facts).toContainEqual({ statKey: 'matchdays', value: 0 });
  });

  it('does not use matchdayCount for Championship hero', () => {
    const facts = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Championship',
        configuredMatchdayCount: 0,
        matchdayCount: 3,
      }),
      false,
    );
    expect(facts).toContainEqual({ statKey: 'matchdays', value: 0 });
  });

  it('never includes materialized match count', () => {
    const facts = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Championship',
        configuredMatchdayCount: 2,
        materialize: {
          commandCode: 'MaterializeMatches',
          labelVariant: 'create',
          determinedCount: 6,
          eligibleCount: 6,
          materializedCount: 4,
          commandEnabled: true,
        },
      }),
      false,
    );
    expect(facts.some((f) => f.statKey === 'matches')).toBe(false);
  });

  it('Places N uses compositionCapacity only', () => {
    const fromHub = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Cup',
        roundCount: 2,
        compositionCapacity: 8,
        slotCount: 4,
      }),
      false,
    );
    expect(fromHub).toContainEqual({ statKey: 'places', value: 8 });

    const noFallback = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Cup',
        roundCount: 2,
        slotCount: 8,
        compositionCapacity: null,
      }),
      false,
    );
    expect(noFallback.find((f) => f.statKey === 'places')).toBeUndefined();
  });

  it('hides Places N when placement strip owns the signal', () => {
    const facts = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Groups',
        groupCount: 2,
        compositionCapacity: 8,
      }),
      true,
    );
    expect(facts).toContainEqual({ statKey: 'groups', value: 2 });
    expect(facts.find((f) => f.statKey === 'places')).toBeUndefined();
  });

  it('Groups and Cup topology stats', () => {
    expect(
      resolveStructurePhaseHeroFacts(
        stage({ formatKind: 'Groups', groupCount: 4 }),
        false,
      ),
    ).toEqual([{ statKey: 'groups', value: 4 }]);

    expect(
      resolveStructurePhaseHeroFacts(
        stage({ formatKind: 'Cup', roundCount: 3 }),
        false,
      ),
    ).toEqual([{ statKey: 'rounds', value: 3 }]);
  });

  it('Swiss rounds from hub wire fields', () => {
    const facts = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Swiss',
        matchdayCount: 1,
        swissRoundCount: 3,
      }),
      false,
    );
    expect(facts).toContainEqual({ statKey: 'swiss-rounds', value: '1/3' });
  });

  it('Swiss Completed shows generated count only without ratio', () => {
    const facts = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Swiss',
        status: 'Completed',
        matchdayCount: 3,
        swissRoundCount: 5,
      }),
      false,
    );
    expect(facts).toContainEqual({ statKey: 'swiss-rounds', value: 3 });
  });

  it('Swiss Running keeps progress ratio', () => {
    const facts = resolveStructurePhaseHeroFacts(
      stage({
        formatKind: 'Swiss',
        status: 'Running',
        matchdayCount: 3,
        swissRoundCount: 5,
      }),
      false,
    );
    expect(facts).toContainEqual({ statKey: 'swiss-rounds', value: '3/5' });
  });
});
