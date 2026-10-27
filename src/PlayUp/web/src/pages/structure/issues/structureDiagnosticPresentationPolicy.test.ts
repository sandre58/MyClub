import { describe, expect, it } from 'vitest';
import {
  structureDiagnosticPresentation,
  structurePresentationPhaseBucket,
} from './structureDiagnosticPresentationPolicy';

describe('structureDiagnosticPresentationPolicy', () => {
  it('Draft PopulationShortfall is warning/attention', () => {
    const p = structureDiagnosticPresentation('PopulationShortfall', {
      stageStatus: 'Draft',
    });
    expect(p.severity).toBe('warning');
    expect(p.tone).toBe('attention');
  });

  it('Ready PopulationShortfall is visual danger', () => {
    const p = structureDiagnosticPresentation('PopulationShortfall', {
      stageStatus: 'Ready',
    });
    expect(p.severity).toBe('blocking');
    expect(p.tone).toBe('danger');
  });

  it('Running PopulationShortfall stays attention', () => {
    const p = structureDiagnosticPresentation('PopulationShortfall', {
      stageStatus: 'Running',
    });
    expect(p.severity).toBe('warning');
    expect(p.tone).toBe('attention');
  });

  it('Ready SourcePopulationShortfall is danger', () => {
    const p = structureDiagnosticPresentation('SourcePopulationShortfall', {
      stageStatus: 'Ready',
    });
    expect(p.severity).toBe('blocking');
    expect(p.tone).toBe('danger');
  });

  it('OverCapacityEdge stays attention across buckets', () => {
    for (const status of ['Draft', 'Ready', 'Running'] as const) {
      const p = structureDiagnosticPresentation('OverCapacityEdge', {
        stageStatus: status,
      });
      expect(p.tone).toBe('attention');
      expect(p.severity).toBe('warning');
    }
  });

  it('NonRootUnfed progress when incompleteness expected', () => {
    const p = structureDiagnosticPresentation('NonRootUnfed', {
      stageStatus: 'Draft',
      isEntryBaselineStage: false,
      hasInboundQualProgFeed: false,
      affectationEntryCount: 0,
    });
    expect(p.tone).toBe('progress');
  });

  it('NonRootUnfed attention when inbound configured', () => {
    const p = structureDiagnosticPresentation('NonRootUnfed', {
      stageStatus: 'Draft',
      hasInboundQualProgFeed: true,
    });
    expect(p.tone).toBe('attention');
  });

  it('Ready NonRootUnfed attention', () => {
    const p = structureDiagnosticPresentation('NonRootUnfed', {
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('attention');
    expect(p.severity).toBe('warning');
  });

  it('AwaitingInboundQualification progress on Ready', () => {
    const p = structureDiagnosticPresentation('AwaitingInboundQualification', {
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('progress');
    expect(p.severity).toBe('warning');
  });

  it('Cup AuthoredAward danger', () => {
    const p = structureDiagnosticPresentation('AuthoredAwardUnreachable', {
      formatKind: 'Cup',
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('danger');
  });

  it('classifying AuthoredAward attention', () => {
    const p = structureDiagnosticPresentation('AuthoredAwardUnreachable', {
      formatKind: 'Championship',
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('attention');
  });

  it('Completed bucket is hidden', () => {
    expect(structurePresentationPhaseBucket('Completed')).toBe('hidden');
    const p = structureDiagnosticPresentation('CycleDetected', {
      stageStatus: 'Completed',
    });
    expect(p.severity).toBe('hidden');
    expect(p.tone).toBeNull();
  });

  it('Draft SlotFeedMissing is attention', () => {
    const p = structureDiagnosticPresentation('SlotFeedMissing', {
      stageStatus: 'Draft',
    });
    expect(p.severity).toBe('warning');
    expect(p.tone).toBe('attention');
  });

  it('Ready SlotFeedMissing is danger', () => {
    const p = structureDiagnosticPresentation('SlotFeedMissing', {
      stageStatus: 'Ready',
    });
    expect(p.severity).toBe('blocking');
    expect(p.tone).toBe('danger');
  });

  it('Suspended uses the Running bucket (attention for over-capacity)', () => {
    expect(structurePresentationPhaseBucket('Suspended')).toBe('Running');
    const p = structureDiagnosticPresentation('OverCapacityEdge', {
      stageStatus: 'Suspended',
    });
    expect(p.tone).toBe('attention');
  });
});
