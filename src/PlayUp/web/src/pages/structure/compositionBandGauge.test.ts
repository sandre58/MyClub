import { describe, expect, it } from 'vitest';

import {
  resolveCompositionBandResolvedGaugeStats,
  resolveCompositionResolvedSeatCount,
  resolveCompositionTeamsGaugeVariant,
} from './compositionBandGauge';

describe('compositionBandGauge', () => {
  it('prefers inbound resolved over composition band count', () => {
    expect(
      resolveCompositionResolvedSeatCount({
        compositionBandResolvedCount: 8,
        inboundComposition: {
          configuredVolume: 7,
          resolvedCount: 7,
          rules: [],
        },
      }),
    ).toBe(7);
  });

  it('resolveCompositionBandResolvedGaugeStats caps at places N', () => {
    expect(
      resolveCompositionBandResolvedGaugeStats(
        { compositionBandResolvedCount: 8 },
        7,
      ),
    ).toEqual({ resolved: 7, total: 7 });
  });

  it('resolveCompositionTeamsGaugeVariant', () => {
    expect(resolveCompositionTeamsGaugeVariant('prep', null)).toBe('entries');
    expect(
      resolveCompositionTeamsGaugeVariant('exploitation', {
        resolved: 6,
        total: 7,
      }),
    ).toBe('resolved');
  });
});
