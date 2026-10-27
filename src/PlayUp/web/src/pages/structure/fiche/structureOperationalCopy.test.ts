import { describe, expect, it } from 'vitest';
import {
  drawPoolToneToRatioGaugeTone,
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

  it('uses attention pool tone when strip message is attention (empty gauge stays neutral)', () => {
    expect(operationalStripPoolTone('attention', 'neutral')).toBe('attention');
    expect(drawPoolToneToRatioGaugeTone('attention')).toBe('attention');
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

  it('T-LAUNCH-1b: uses brand for 0/N when draw is launchable', () => {
    expect(
      resolvePlaceOccupancyTone(0, 8, {
        drawRulesEngaged: true,
        drawLaunchable: true,
      }),
    ).toBe('brand');
  });

  it('preserves brand pool tone when strip message is info', () => {
    expect(operationalStripPoolTone('info', 'brand')).toBe('brand');
  });

  it('uses partial for empty bracket when draw rules are not engaged', () => {
    expect(resolvePlaceOccupancyTone(0, 8)).toBe('partial');
  });
});
