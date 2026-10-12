import { describe, expect, it } from 'vitest';
import { operationalStripPoolTone } from './structureOperationalCopy';

describe('operationalStripPoolTone', () => {
  it('uses danger when the strip message is danger', () => {
    expect(operationalStripPoolTone('danger', 'partial')).toBe('danger');
  });

  it('keeps partial when message is informational', () => {
    expect(operationalStripPoolTone('info', 'partial')).toBe('partial');
  });
});
