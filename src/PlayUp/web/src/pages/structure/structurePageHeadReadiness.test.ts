import { describe, expect, it } from 'vitest';
import { resolveStructurePageHeadNoteKind } from './structurePageHeadReadiness';

const base = {
  lifecycleClosed: false,
  stagesEmpty: false,
  structurePresentedDanger: false,
  structureHasPresentedProblems: false,
  attentionPhase: 'success' as const,
  presentableBlockingAttentionCount: 0,
  presentableNonBlockingAttentionCount: 0,
  readyToMaterialize: false,
  readyForSchedule: false,
  readyForNextSlice: false,
};

describe('resolveStructurePageHeadNoteKind', () => {
  it('shows negative when attention has blocking items but structure is clean', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        presentableBlockingAttentionCount: 1,
      }),
    ).toBe('negative');
  });

  it('shows negative when structure has danger-level presentation', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        structurePresentedDanger: true,
        structureHasPresentedProblems: true,
      }),
    ).toBe('negative');
  });

  it('shows warning for warning-only structure problems', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        structureHasPresentedProblems: true,
      }),
    ).toBe('warning');
  });

  it('shows negative when errors and warnings are both present', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        structurePresentedDanger: true,
        structureHasPresentedProblems: true,
        presentableNonBlockingAttentionCount: 2,
      }),
    ).toBe('negative');
  });

  it('shows warning for non-blocking attention only', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        presentableNonBlockingAttentionCount: 1,
      }),
    ).toBe('warning');
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

  it('shows negative on attention error when structure has danger', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        attentionPhase: 'error',
        structurePresentedDanger: true,
        structureHasPresentedProblems: true,
      }),
    ).toBe('negative');
  });
});
