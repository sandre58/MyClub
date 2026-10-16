import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import {
  buildConfrontationDigestTokens,
  buildMatchDigestTokens,
  buildStandingDigestTokens,
} from './structureRegulationDigest';

const t = (key: string) => key;

const baseStage = (
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary => ({
  stageId: 's1',
  name: 'P',
  status: 'Ready',
  teamCount: 0,
  matchCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  halfTimeDuration: 15,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: false,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
  winPoints: 3,
  drawPoints: 1,
  lossPoints: 0,
  rankingCriteria: ['Points'],
  ...overrides,
});

describe('structureRegulationDigest', () => {
  it('omits extra time and penalties when disabled', () => {
    const tokens = buildMatchDigestTokens(
      baseStage({
        hasExtraTime: false,
        hasPenaltyShootout: false,
      }),
      t,
    );
    expect(tokens.map((token) => token.key)).toEqual(['regulation']);
  });

  it('includes extra time and penalties only when configured', () => {
    const tokens = buildMatchDigestTokens(
      baseStage({
        hasExtraTime: true,
        extraTimeNumberOfPeriods: 2,
        extraTimeDurationPerPeriod: 15,
        hasPenaltyShootout: true,
        penaltyInitialKicksPerTeam: 5,
      }),
      t,
    );
    expect(tokens.map((token) => token.key)).toEqual([
      'regulation',
      'extra',
      'penalties',
    ]);
  });

  it('builds standing tokens for points and criteria', () => {
    const tokens = buildStandingDigestTokens(baseStage(), t);
    expect(tokens.map((token) => token.key)).toContain('points');
    expect(tokens.map((token) => token.key)).toContain('criteria');
  });

  it('builds confrontation tokens from stage defaults', () => {
    const tokens = buildConfrontationDigestTokens(
      baseStage({ numberOfLegs: 2, aggregateScoring: true }),
      t,
    );
    expect(tokens.length).toBeGreaterThan(0);
  });
});
