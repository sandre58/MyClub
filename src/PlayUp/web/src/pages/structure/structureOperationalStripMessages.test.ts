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

  it('uses attention for empty occupancy when SlotFeedMissing is active', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 0,
        slotFeedMissingActive: true,
      }).kind,
    ).toBe('attention');
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

  it('uses attention for partial occupancy when SlotFeedMissing is active', () => {
    expect(
      resolvePlaceOccupancyMeterCopy({
        t,
        occupancyReady: true,
        unknownCapacity: false,
        capacity: 8,
        occupiedPlacesCount: 1,
        slotFeedMissingActive: true,
      }).kind,
    ).toBe('attention');
  });
});
