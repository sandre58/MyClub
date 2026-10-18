import { describe, expect, it } from 'vitest';
import {
  isStructureHubCalm,
  resolveStructurePageHeadNoteKind,
} from './structurePageHeadReadiness';

const base = {
  lifecycleClosed: false,
  stagesEmpty: false,
  structurePresentedDanger: false,
  structureHasPresentedProblems: false,
  attentionPhase: 'success' as const,
  presentableBlockingAttentionCount: 0,
  presentableNonBlockingAttentionCount: 0,
  hubNextStep: null,
  stages: [{ status: 'Ready' as const }],
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
        hubNextStep: {
          code: 'PrepareStage',
          stageId: 'a',
          stageName: 'League',
        },
      }),
    ).toBe('none');
  });

  it('shows info next step from hubNextStep when clean', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        stages: [{ status: 'Draft' }],
        hubNextStep: {
          code: 'PrepareStage',
          stageId: 'a',
          stageName: 'League',
        },
      }),
    ).toBe('positiveNextStep');
  });

  it('does not treat hubNextStep null with draft as calm', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        hubNextStep: null,
        stages: [{ status: 'Draft' }],
      }),
    ).toBe('none');
  });
});

describe('isStructureHubCalm', () => {
  it('is true when all phases at least Ready and hubNextStep is null', () => {
    expect(
      isStructureHubCalm({
        ...base,
        hubNextStep: null,
        stages: [{ status: 'Ready' }, { status: 'Running' }],
      }),
    ).toBe(true);
  });

  it('is false when any phase is Draft', () => {
    expect(
      isStructureHubCalm({
        ...base,
        stages: [{ status: 'Ready' }, { status: 'Draft' }],
      }),
    ).toBe(false);
  });

  it('is false when hubNextStep is set (ambiguous resolver null is not calm)', () => {
    expect(
      isStructureHubCalm({
        ...base,
        hubNextStep: {
          code: 'MaterializeMatches',
          stageId: 'a',
          stageName: 'League',
        },
      }),
    ).toBe(false);
  });

  it('is false when attention is pending', () => {
    expect(
      isStructureHubCalm({
        ...base,
        attentionPhase: 'pending',
      }),
    ).toBe(false);
  });

  it('is false when all phases are Completed', () => {
    expect(
      isStructureHubCalm({
        ...base,
        stages: [{ status: 'Completed' }],
      }),
    ).toBe(false);
  });

  it('is false when resume is possible (hub step would be set in API)', () => {
    expect(
      isStructureHubCalm({
        ...base,
        hubNextStep: {
          code: 'ResumeStage',
          stageId: 'a',
          stageName: 'League',
        },
      }),
    ).toBe(false);
  });

  it('is false when a phase is Suspended and hubNextStep is null (fail-closed)', () => {
    expect(
      isStructureHubCalm({
        ...base,
        hubNextStep: null,
        stages: [{ status: 'Ready' }, { status: 'Suspended' }],
      }),
    ).toBe(false);
  });

  it('does not show success while attention query is pending', () => {
    expect(
      resolveStructurePageHeadNoteKind({
        ...base,
        attentionPhase: 'pending',
        hubNextStep: null,
        stages: [{ status: 'Ready' }],
      }),
    ).toBe('none');
  });
});
