import { describe, expect, it } from 'vitest';

import {
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
});
