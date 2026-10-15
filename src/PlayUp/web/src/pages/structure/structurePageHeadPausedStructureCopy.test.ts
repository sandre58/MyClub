import { describe, expect, it } from 'vitest';
import { shouldUsePausedStructurePageHeadCopy } from './structurePageHeadPausedStructureCopy';

const summaryBase = {
  presentedStructureProblemsSuspendedOnly: true,
  hasAny: true,
  showPageHeadNegative: false,
  shapeInvalid: false,
};

describe('shouldUsePausedStructurePageHeadCopy', () => {
  it('enables paused copy for structure-only warning when all contributors are Suspended', () => {
    expect(
      shouldUsePausedStructurePageHeadCopy({
        noteKind: 'warning',
        summary: summaryBase,
        presentableBlockingAttentionCount: 0,
        presentableNonBlockingAttentionCount: 0,
      }),
    ).toBe(true);
  });

  it('disables paused copy when a Running or Ready stage also contributes', () => {
    expect(
      shouldUsePausedStructurePageHeadCopy({
        noteKind: 'warning',
        summary: {
          ...summaryBase,
          presentedStructureProblemsSuspendedOnly: false,
        },
        presentableBlockingAttentionCount: 0,
        presentableNonBlockingAttentionCount: 0,
      }),
    ).toBe(false);
  });

  it('disables paused copy when the attention drawer has items', () => {
    expect(
      shouldUsePausedStructurePageHeadCopy({
        noteKind: 'warning',
        summary: summaryBase,
        presentableBlockingAttentionCount: 0,
        presentableNonBlockingAttentionCount: 1,
      }),
    ).toBe(false);
    expect(
      shouldUsePausedStructurePageHeadCopy({
        noteKind: 'warning',
        summary: summaryBase,
        presentableBlockingAttentionCount: 1,
        presentableNonBlockingAttentionCount: 0,
      }),
    ).toBe(false);
  });

  it('enables paused copy for structure-only danger when shape is valid', () => {
    expect(
      shouldUsePausedStructurePageHeadCopy({
        noteKind: 'negative',
        summary: {
          ...summaryBase,
          showPageHeadNegative: true,
        },
        presentableBlockingAttentionCount: 0,
        presentableNonBlockingAttentionCount: 0,
      }),
    ).toBe(true);
  });

  it('disables paused copy for shape invalid even when stages are Suspended', () => {
    expect(
      shouldUsePausedStructurePageHeadCopy({
        noteKind: 'negative',
        summary: {
          ...summaryBase,
          showPageHeadNegative: true,
          shapeInvalid: true,
        },
        presentableBlockingAttentionCount: 0,
        presentableNonBlockingAttentionCount: 0,
      }),
    ).toBe(false);
  });
});
