import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary } from '../types';
import { resolvePlacesN, resolvePlacesPerGroup } from './structurePlaces';

function stage(
  overrides: Partial<StructureStageHubSummary>,
): StructureStageHubSummary {
  return {
    stageId: 's1',
    name: 'Phase',
    status: 'Draft',
    teamCount: 0,
    matchCount: 0,
    groupCount: 0,
    roundCount: 0,
    matchdayCount: 0,
    slotCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    extraTimeNumberOfPeriods: null,
    extraTimeDurationPerPeriod: null,
    hasPenaltyShootout: false,
    penaltyInitialKicksPerTeam: null,
    hasStandingRules: false,
    winPoints: null,
    drawPoints: null,
    lossPoints: null,
    hasDrawRules: false,
    drawMode: null,
    numberOfPots: null,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    numberOfLegs: null,
    aggregateScoring: null,
    ...overrides,
  };
}

describe('resolvePlacesN', () => {
  it('prefers compositionCapacity', () => {
    expect(
      resolvePlacesN(
        stage({ compositionCapacity: 16, slotCount: 8, formatKind: 'Cup' }),
      ),
    ).toBe(16);
  });

  it('Cup falls back to slotCount', () => {
    expect(
      resolvePlacesN(stage({ formatKind: 'Cup', slotCount: 8 })),
    ).toBe(8);
  });

  it('Groups prefers placesPerGroup over numberOfPots', () => {
    expect(
      resolvePlacesN(
        stage({
          formatKind: 'Groups',
          groupCount: 4,
          placesPerGroup: 4,
          numberOfPots: null,
          teamCount: 0,
        }),
      ),
    ).toBe(16);
  });

  it('Groups derives from groupCount × numberOfPots when capacity missing', () => {
    expect(
      resolvePlacesN(
        stage({
          formatKind: 'Groups',
          groupCount: 4,
          numberOfPots: 4,
          teamCount: 0,
        }),
      ),
    ).toBe(16);
  });
});

describe('resolvePlacesPerGroup', () => {
  it('uses placesPerGroup when groups are empty (not Math.max 1)', () => {
    expect(
      resolvePlacesPerGroup(
        stage({
          formatKind: 'Groups',
          groupCount: 4,
          placesPerGroup: 4,
          numberOfPots: null,
          teamCount: 0,
        }),
      ),
    ).toBe(4);
  });

  it('returns null when capacity unknown — never invents 1', () => {
    expect(
      resolvePlacesPerGroup(
        stage({
          formatKind: 'Groups',
          groupCount: 4,
          teamCount: 0,
          numberOfPots: null,
          placesPerGroup: null,
        }),
      ),
    ).toBeNull();
  });
});
