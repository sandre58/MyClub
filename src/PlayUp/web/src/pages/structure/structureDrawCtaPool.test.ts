import { describe, expect, it } from 'vitest';
import { resolveDrawCtaPoolTone } from './structureDrawCtaPool';

describe('resolveDrawCtaPoolTone', () => {
  it('maps fill ratio to pool tone', () => {
    expect(resolveDrawCtaPoolTone(0, 8)).toBe('neutral');
    expect(resolveDrawCtaPoolTone(3, 8)).toBe('partial');
    expect(resolveDrawCtaPoolTone(8, 8)).toBe('complete');
  });
});
