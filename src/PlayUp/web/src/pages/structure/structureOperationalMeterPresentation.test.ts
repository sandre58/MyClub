import { describe, expect, it } from 'vitest';

import {
  isDraftLikeStageStatus,
  isSuspendedStageStatus,
} from './structureOperationalMeterPresentation';

describe('structureOperationalMeterPresentation', () => {
  it('isDraftLikeStageStatus is Draft only (Suspended excluded — Δ-O3-1)', () => {
    expect(isDraftLikeStageStatus('Draft')).toBe(true);
    expect(isDraftLikeStageStatus('Suspended')).toBe(false);
    expect(isDraftLikeStageStatus('Ready')).toBe(false);
    expect(isDraftLikeStageStatus('Running')).toBe(false);
  });

  it('isSuspendedStageStatus identifies pause exploitation', () => {
    expect(isSuspendedStageStatus('Suspended')).toBe(true);
    expect(isSuspendedStageStatus('Draft')).toBe(false);
    expect(isSuspendedStageStatus('Running')).toBe(false);
  });
});
