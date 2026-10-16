import { describe, expect, it } from 'vitest';
import {
  operationalStripPoolTone,
  resolvePlaceOccupancyTone,
} from './structureOperationalCopy';

describe('operationalStripPoolTone', () => {
  it('uses danger when the strip message is danger', () => {
    expect(operationalStripPoolTone('danger', 'partial')).toBe('danger');
  });

  it('keeps partial when message is informational', () => {
    expect(operationalStripPoolTone('info', 'partial')).toBe('partial');
  });
});

describe('resolvePlaceOccupancyTone', () => {
  it('T-LAUNCH-0: uses neutral for 0/N when draw rules engaged but not launchable', () => {
    expect(
      resolvePlaceOccupancyTone(0, 8, {
        drawRulesEngaged: true,
        drawLaunchable: false,
      }),
    ).toBe('neutral');
  });

  it('T-LAUNCH-1: uses partial for 0/N when draw is launchable', () => {
    expect(
      resolvePlaceOccupancyTone(0, 8, {
        drawRulesEngaged: true,
        drawLaunchable: true,
      }),
    ).toBe('partial');
  });

  it('uses partial for empty bracket when draw rules are not engaged', () => {
    expect(resolvePlaceOccupancyTone(0, 8)).toBe('partial');
  });
});
