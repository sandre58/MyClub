import { describe, expect, it } from 'vitest';

import { ratioToneForOperationalMeter } from './structureOperationalStripRatioTone';

describe('ratioToneForOperationalMeter', () => {
  it('uses partial tone for attention gap even when filled is zero', () => {
    expect(ratioToneForOperationalMeter(0, 6, 'attention')).toBe('partial');
  });
});
