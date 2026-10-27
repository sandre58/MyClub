import { describe, expect, it } from 'vitest';

import {
  resolveCompositionPopulationMeterCopy,
  resolveEntryRoundPlacementMeterCopy,
  resolveGroupsPlacementMeterCopy,
  resolvePlaceOccupancyMeterCopy,
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

  it('uses danger for entry-round meter when start is blocked by incomplete pairs', () => {
    expect(
      resolveEntryRoundPlacementMeterCopy({
        t,
        gaugeRatio: { filled: 1, capacity: 4 },
        blocksPhaseStart: true,
      })?.kind,
    ).toBe('danger');
  });

  it('uses danger for entry-round meter when pairs incomplete (aligned with drawer gate)', () => {
    expect(
      resolveEntryRoundPlacementMeterCopy({
        t,
        gaugeRatio: { filled: 1, capacity: 4 },
        blocksPhaseStart: false,
      })?.kind,
    ).toBe('danger');
  });

  it('uses info awaiting-draw copy for groups placement when presentation kind is info', () => {
    const result = resolveGroupsPlacementMeterCopy({
      t,
      gaugeRatio: { filled: 0, capacity: 8 },
      presentationKind: 'info',
    });
    expect(result.kind).toBe('info');
    expect(result.text).toBe(
      'fiche.operational.production.stateMessage.groupsPlacementAwaitingDraw',
    );
  });

  it('uses info awaiting-draw copy when presentation kind is info', () => {
    const result = resolveEntryRoundPlacementMeterCopy({
      t,
      gaugeRatio: { filled: 0, capacity: 8 },
      blocksPhaseStart: false,
      presentationKind: 'info',
    });
    expect(result?.kind).toBe('info');
    expect(result?.text).toBe(
      'fiche.operational.production.stateMessage.entryRoundAwaitingDraw',
    );
  });

  it('uses success when all entry pairs are ready', () => {
    const result = resolveEntryRoundPlacementMeterCopy({
      t,
      gaugeRatio: { filled: 8, capacity: 8 },
      blocksPhaseStart: false,
    });
    expect(result?.kind).toBe('success');
    expect(result?.text).toBe(
      'fiche.operational.production.stateMessage.entryRoundAllReady',
    );
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

  it('uses info and pause copy for partial occupancy in Suspended', () => {
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

  it('uses info final readout for partial occupancy in Completed', () => {
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

  it('uses final-state info for population shortfall in Completed', () => {
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
