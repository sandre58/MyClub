import { describe, expect, it } from 'vitest';
import { resolveStructurePageHeadNoteKind } from './structurePageHeadReadiness';

const base = {
  lifecycleClosed: false,
  stagesEmpty: false,
  hasAny: false,
  attentionPhase: 'success' as const,
  presentableAttentionCount: 0,
  readyToMaterialize: false,
  readyForSchedule: false,
  readyForNextSlice: false,
};

describe('resolveStructurePageHeadNoteKind', () => {
  it('shows negative when attention has presentable items but structure hasAny is false', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        presentableAttentionCount: 1,
      }),
    ).toBe('negative');
  });

  it('shows negative when structure hasAny even if attention is empty', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        hasAny: true,
      }),
    ).toBe('negative');
  });

  it('does not show positive while attention is pending', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        attentionPhase: 'pending',
        readyToMaterialize: true,
      }),
    ).toBe('none');
  });

  it('shows positive materialization when clean and attention succeeded empty', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        readyToMaterialize: true,
      }),
    ).toBe('positiveMaterialization');
  });

  it('shows negative on attention error when structure hasAny', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        attentionPhase: 'error',
        hasAny: true,
      }),
    ).toBe('negative');
  });
});
