import { describe, expect, it } from 'vitest';

import {
  resolveCompositionPopulationMeterCopy,
  resolveEntryRoundPlacementMeterCopy,
  resolvePlaceOccupancyMeterCopy,
  resolveProductionMeterCopy,
} from './structureOperationalStripMessages';

const t = (key: string) => key;

describe('resolvePlaceOccupancyMeterCopy', () => {
  it('uses info for empty occupancy when SlotFeedMissing is not active', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 0,
        slotFeedMissingActive: false,
      }).kind,
    ).toBe('info');
  });

  it('T-LAUNCH-0: uses info for 0/N when draw rules engaged but gate closed', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 0,
        slotFeedMissingActive: false,
        drawRulesEngaged: true,
        drawLaunchable: false,
        stageStatus: 'Ready',
      }).kind,
    ).toBe('info');
  });

  it('T-LAUNCH-1b: uses RunDraw copy and info when draw is launchable at 0/N', () => {
    const result = resolvePlaceOccupancyMeterCopy({
      t,
      occupancyReady: true,
      unknownCapacity: false,
      capacity: 8,
      occupiedPlacesCount: 0,
      slotFeedMissingActive: false,
      drawRulesEngaged: true,
      drawLaunchable: true,
      stageStatus: 'Ready',
    });
    expect(result.kind).toBe('info');
    expect(result.text).toBe('readiness.nextStep.RunDraw');
  });

  it('T-LAUNCH-1c: uses RunDraw in Draft when draw rules engaged at 0/N', () => {
    const result = resolvePlaceOccupancyMeterCopy({
      t,
      occupancyReady: true,
      unknownCapacity: false,
      capacity: 8,
      occupiedPlacesCount: 0,
      slotFeedMissingActive: false,
      drawRulesEngaged: true,
      drawLaunchable: false,
      drawExecutionBadge: 'ToLaunch',
      stageStatus: 'Draft',
    });
    expect(result.text).toBe('readiness.nextStep.RunDraw');
    expect(result.kind).toBe('info');
  });

  it('T-LAUNCH-1d: uses RunDraw when draw rules engaged and badge ToLaunch at 0/N in Ready', () => {
    const result = resolvePlaceOccupancyMeterCopy({
      t,
      occupancyReady: true,
      unknownCapacity: false,
      capacity: 8,
      occupiedPlacesCount: 0,
      slotFeedMissingActive: false,
      drawRulesEngaged: true,
      drawLaunchable: false,
      drawExecutionBadge: 'ToLaunch',
      stageStatus: 'Ready',
    });
    expect(result.text).toBe('readiness.nextStep.RunDraw');
  });

  it('uses danger for empty occupancy when SlotFeedMissing is active in Ready', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 0,
        slotFeedMissingActive: true,
        stageStatus: 'Ready',
      }).kind,
    ).toBe('danger');
  });

  it('uses neutral attention when no confrontation is determined on row gauge', () => {
    expect(
      resolveProductionMeterCopy({
        t,
        blockedCaption: null,
        gaugeRatio: { filled: 0, capacity: 4 },
        hasProduction: true,
        productionGaugeMode: 'determined',
      }),
    ).toEqual({
      text: 'fiche.operational.production.stateMessage.noDeterminedConfrontations',
      kind: 'attention',
    });
  });

  it('uses attention for partial match production gap', () => {
    expect(
      resolveProductionMeterCopy({
        t,
        blockedCaption: null,
        gaugeRatio: { filled: 0, capacity: 3 },
        hasProduction: true,
      }).kind,
    ).toBe('attention');
  });

  it('uses danger when match quota is prospective but groups pools block production', () => {
    const result = resolveProductionMeterCopy({
      t,
      blockedCaption: null,
      gaugeRatio: { filled: 0, capacity: 24 },
      hasProduction: true,
      productionGaugeMode: 'materialized',
      groupsPoolsIncomplete: true,
    });
    expect(result.kind).toBe('danger');
    expect(result.text).toBe(
      'fiche.operational.production.stateMessage.matchesBlockedByGroupsPools',
    );
  });

  it('uses danger for entry-round meter when start is blocked by incomplete pairs', () => {
    expect(
      resolveEntryRoundPlacementMeterCopy({
        t,
        gaugeRatio: { filled: 1, capacity: 4 },
        blocksPhaseStart: true,
      })?.kind,
    ).toBe('danger');
  });

  it('uses attention for entry-round meter when pairs incomplete but start not blocked', () => {
    expect(
      resolveEntryRoundPlacementMeterCopy({
        t,
        gaugeRatio: { filled: 1, capacity: 4 },
        blocksPhaseStart: false,
      })?.kind,
    ).toBe('attention');
  });

  it('uses danger when start is blocked on unmaterialized confrontations', () => {
    expect(
      resolveProductionMeterCopy({
        t,
        blockedCaption: null,
        gaugeRatio: { filled: 1, capacity: 4 },
        hasProduction: true,
        startStageBlocker: 'UnmaterializedConfrontations',
      }).kind,
    ).toBe('danger');
  });

  it('shows match production complete when quota filled even if start blocked for entry round', () => {
    const result = resolveProductionMeterCopy({
      t,
      blockedCaption: null,
      gaugeRatio: { filled: 2, capacity: 2 },
      hasProduction: true,
      startStageBlocker: 'IncompleteEntryRound',
    });
    expect(result.kind).toBe('success');
    expect(result.text).toBe('fiche.operational.production.stateMessage.complete');
  });

  it('uses attention for partial occupancy in Draft when SlotFeedMissing is active', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 1,
        slotFeedMissingActive: true,
        stageStatus: 'Draft',
      }).kind,
    ).toBe('attention');
  });

  it('uses attention for empty occupancy when SlotFeedMissing is active in Draft', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 0,
        slotFeedMissingActive: true,
        stageStatus: 'Draft',
      }).kind,
    ).toBe('attention');
  });

  it('uses attention for partial bracket fill in Draft without SlotFeedMissing', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 3,
        slotFeedMissingActive: false,
        stageStatus: 'Draft',
      }).kind,
    ).toBe('attention');
  });

  it('uses attention for partial bracket fill in Ready', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 3,
        slotFeedMissingActive: false,
        stageStatus: 'Ready',
      }).kind,
    ).toBe('attention');
  });

  it('uses info and pause copy for partial occupancy in Suspended (Δ-O3-1)', () => {
    const result = resolvePlaceOccupancyMeterCopy({
      t,
      occupancyReady: true,
      unknownCapacity: false,
      capacity: 8,
      occupiedPlacesCount: 3,
      slotFeedMissingActive: true,
      stageStatus: 'Suspended',
    });
    expect(result.kind).toBe('info');
    expect(result.text).toBe(
      'fiche.operational.placement.stateMessage.partialPaused',
    );
  });

  it('uses info final readout for partial occupancy in Completed (Δ-O4-1)', () => {
    const result = resolvePlaceOccupancyMeterCopy({
      t,
      occupancyReady: true,
      unknownCapacity: false,
      capacity: 8,
      occupiedPlacesCount: 3,
      slotFeedMissingActive: false,
      stageStatus: 'Completed',
    });
    expect(result.kind).toBe('info');
    expect(result.text).toBe(
      'fiche.operational.placement.stateMessage.finalPartial',
    );
  });

  it('uses info and pause copy for empty occupancy in Suspended', () => {
    const result = resolvePlaceOccupancyMeterCopy({
      t,
      occupancyReady: true,
      unknownCapacity: false,
      capacity: 8,
      occupiedPlacesCount: 0,
      slotFeedMissingActive: false,
      stageStatus: 'Suspended',
    });
    expect(result.kind).toBe('info');
    expect(result.text).toBe(
      'fiche.operational.placement.stateMessage.emptyPaused',
    );
  });
});

describe('resolveCompositionPopulationMeterCopy', () => {
  it('uses pause readout for population shortfall in Suspended', () => {
    const result = resolveCompositionPopulationMeterCopy({
      t,
      current: 4,
      capacity: 8,
      presentationContext: { stageStatus: 'Suspended' },
    });
    expect(result.kind).toBe('info');
    expect(result.text).toBe('composition.gauge.populationShortfallPaused');
  });

  it('uses final-state info for population shortfall in Completed (Δ-O4-1)', () => {
    const result = resolveCompositionPopulationMeterCopy({
      t,
      current: 4,
      capacity: 8,
      presentationContext: { stageStatus: 'Completed' },
    });
    expect(result.kind).toBe('info');
    expect(result.text).toBe('composition.gauge.populationFinalShortfall');
  });

  it('keeps diagnostic tone for population shortfall in Ready', () => {
    const result = resolveCompositionPopulationMeterCopy({
      t,
      current: 4,
      capacity: 8,
      presentationContext: { stageStatus: 'Ready' },
    });
    expect(result.text).toBe('entries.meter.shortfall');
  });
});
