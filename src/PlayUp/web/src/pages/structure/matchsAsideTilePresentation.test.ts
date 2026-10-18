import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import {
  isPhaseRosterIncompleteMaterialize,
  resolveAsideIdleMatchProductionGauge,
  resolveAsideMatchProductionPresentation,
  resolveAsideRosterResolutionGaugeStats,
  resolveAsideCupEntryRoundGaugeStats,
  shouldShowAsideEntryRoundBlockerGauge,
  shouldShowAsideRosterResolutionGauge,
} from './matchsAsideTilePresentation';

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
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: false,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
  ...overrides,
});

describe('matchsAsideTilePresentation', () => {
  it('resolves roster gauge from inbound qual seats, not schematic occupancy', () => {
    expect(
      resolveAsideRosterResolutionGaugeStats(
        baseStage({
          compositionCapacity: 8,
          schematicOccupiedPlaceCount: 8,
          inboundComposition: {
            configuredVolume: 8,
            resolvedCount: 2,
            rules: [],
          },
        }),
        null,
      ),
    ).toEqual({ resolved: 2, total: 8 });
  });

  it('shows roster resolution gauge when roster gap blocks production', () => {
    expect(
      shouldShowAsideRosterResolutionGauge(
        baseStage({
          formatKind: 'Championship',
          materialize: {
            commandCode: 'MaterializeMatches',
            labelVariant: 'idle',
            determinedCount: 0,
            eligibleCount: 0,
            materializedCount: 0,
            commandEnabled: false,
            disabledReason: 'PhaseRosterIncomplete',
            confrontations: [],
          },
        }),
        { resolved: 2, total: 8 },
      ),
    ).toBe(true);
    expect(
      shouldShowAsideRosterResolutionGauge(
        baseStage({
          formatKind: 'Championship',
          phaseRosterPresentation: 'AwaitingInboundQualification',
          materialize: {
            commandCode: 'MaterializeMatches',
            labelVariant: 'idle',
            determinedCount: 0,
            eligibleCount: 0,
            materializedCount: 0,
            commandEnabled: false,
            disabledReason: 'NoDeterminedConfrontations',
            confrontations: Array.from({ length: 8 }, (_, i) => ({
              key: `k${i}`,
              sideA: 'A',
              sideB: 'B',
              determined: false,
              materialized: false,
              eligible: false,
            })),
          },
          lifecycle: {
            transition: {
              code: 'StartStage',
              enabled: false,
              disabledReasonCode: 'PhaseRosterIncomplete',
            },
          },
        }),
        { resolved: 2, total: 8 },
      ),
    ).toBe(true);
  });

  it('idle gauge ignores determined confrontation rows when lifecycle blocks roster', () => {
    expect(
      resolveAsideIdleMatchProductionGauge(
        baseStage({
          formatKind: 'Championship',
          compositionCapacity: 4,
          materialize: {
            commandCode: 'MaterializeMatches',
            labelVariant: 'idle',
            determinedCount: 0,
            eligibleCount: 0,
            materializedCount: 0,
            commandEnabled: false,
            disabledReason: 'NoDeterminedConfrontations',
            confrontations: Array.from({ length: 8 }, (_, i) => ({
              key: `k${i}`,
              sideA: 'A',
              sideB: 'B',
              determined: false,
              materialized: false,
              eligible: false,
            })),
          },
          lifecycle: {
            transition: {
              code: 'StartStage',
              enabled: false,
              disabledReasonCode: 'PhaseRosterIncomplete',
            },
          },
        }),
      ),
    ).toEqual({ filled: 0, capacity: 6 });
  });

  it('resolves cup entry-round stats from confrontation rows when hub omits undetermined count', () => {
    const materialize = {
      commandCode: 'MaterializeFromOccupiedSlots',
      labelVariant: 'idle',
      determinedCount: 0,
      eligibleCount: 0,
      materializedCount: 0,
      commandEnabled: false,
      confrontations: Array.from({ length: 8 }, (_, i) => ({
        key: `k${i}`,
        sideA: 'A',
        sideB: 'B',
        determined: false,
        materialized: false,
        eligible: false,
      })),
    };
    expect(resolveAsideCupEntryRoundGaugeStats(materialize)).toEqual({
      filled: 0,
      capacity: 8,
    });
    expect(
      shouldShowAsideEntryRoundBlockerGauge(
        baseStage({ formatKind: 'Cup', materialize }),
        materialize,
      ),
    ).toBe(true);
  });

  it('cup aside gauge falls back to confrontation rows when canonical counts are absent', () => {
    expect(
      resolveAsideMatchProductionPresentation(
        baseStage({
          formatKind: 'Cup',
          materialize: {
            commandCode: 'MaterializeFromOccupiedSlots',
            labelVariant: 'idle',
            determinedCount: 0,
            eligibleCount: 0,
            materializedCount: 0,
            commandEnabled: false,
            disabledReason: 'NoDeterminedConfrontations',
            confrontations: Array.from({ length: 8 }, (_, i) => ({
              key: `k${i}`,
              sideA: 'A',
              sideB: 'B',
              determined: false,
              materialized: false,
              eligible: false,
            })),
          },
        }),
      ),
    ).toEqual({
      mode: 'materialized',
      ratio: { filled: 0, capacity: 8 },
    });
  });

  it('cup ready before draw uses match gauge capacity from canonical entry pairs', () => {
    expect(
      resolveAsideMatchProductionPresentation(
        baseStage({
          formatKind: 'Cup',
          materialize: {
            commandCode: 'MaterializeFromOccupiedSlots',
            labelVariant: 'idle',
            determinedCount: 0,
            eligibleCount: 0,
            materializedCount: 0,
            commandEnabled: false,
            disabledReason: 'NoDeterminedConfrontations',
            confrontations: Array.from({ length: 8 }, (_, i) => ({
              key: `k${i}`,
              sideA: 'A',
              sideB: 'B',
              determined: false,
              materialized: false,
              eligible: false,
            })),
            canonicalEntryPairCount: 8,
            undeterminedCanonicalEntryPairCount: 8,
          },
        }),
      ),
    ).toEqual({
      mode: 'materialized',
      ratio: { filled: 0, capacity: 8 },
    });
  });

  it('prospective championship match gauge when determined is zero', () => {
    expect(
      resolveAsideIdleMatchProductionGauge(
        baseStage({
          formatKind: 'Championship',
          compositionCapacity: 4,
          numberOfLegs: 1,
          materialize: {
            commandCode: 'MaterializeMatches',
            labelVariant: 'idle',
            determinedCount: 0,
            eligibleCount: 0,
            materializedCount: 0,
            commandEnabled: false,
            disabledReason: 'PhaseRosterIncomplete',
            confrontations: [],
          },
        }),
      ),
    ).toEqual({ filled: 0, capacity: 6 });
  });

  it('detects PhaseRosterIncomplete materialize', () => {
    expect(
      isPhaseRosterIncompleteMaterialize({
        commandCode: 'MaterializeMatches',
        labelVariant: 'idle',
        determinedCount: 0,
        eligibleCount: 0,
        materializedCount: 0,
        commandEnabled: false,
        disabledReason: 'PhaseRosterIncomplete',
        confrontations: [],
      }),
    ).toBe(true);
  });
});
