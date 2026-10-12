import { describe, expect, it } from 'vitest';
import {
  attentionSeverityToneFromItems,
  attentionSeverityToneFromSituations,
} from './attentionSeverityTone';

describe('attentionSeverityTone', () => {
  it('prefers blocking when any item is blocking', () => {
    expect(
      attentionSeverityToneFromItems([
        { severity: 'Warning' },
        { severity: 'Blocking' },
      ]),
    ).toBe('blocking');
  });

  it('is warning when all items are non-blocking', () => {
    expect(attentionSeverityToneFromItems([{ severity: 'Warning' }])).toBe(
      'warning',
    );
    expect(
      attentionSeverityToneFromSituations([{ nature: 'Warning' }]),
    ).toBe('warning');
  });
});
