import { describe, expect, it } from 'vitest';
import {
  structureDiagnosticPresentation,
  structurePresentationPhaseBucket,
} from './structureDiagnosticPresentationPolicy';

describe('structureDiagnosticPresentationPolicy (R-Pres)', () => {
  it('R-Pres-PS-1 Draft PopulationShortfall is warning/attention', () => {
    const p = structureDiagnosticPresentation('PopulationShortfall', {
      stageStatus: 'Draft',
    });
    expect(p.severity).toBe('warning');
    expect(p.tone).toBe('attention');
  });

  it('R-Pres-PS-2 Ready PopulationShortfall is visual danger', () => {
    const p = structureDiagnosticPresentation('PopulationShortfall', {
      stageStatus: 'Ready',
    });
    expect(p.severity).toBe('blocking');
    expect(p.tone).toBe('danger');
  });

  it('R-Pres-PS-3 Running PopulationShortfall stays attention', () => {
    const p = structureDiagnosticPresentation('PopulationShortfall', {
      stageStatus: 'Running',
    });
    expect(p.severity).toBe('warning');
    expect(p.tone).toBe('attention');
  });

  it('R-Pres-SPS-1 Ready SourcePopulationShortfall is danger', () => {
    const p = structureDiagnosticPresentation('SourcePopulationShortfall', {
      stageStatus: 'Ready',
    });
    expect(p.severity).toBe('blocking');
    expect(p.tone).toBe('danger');
  });

  it('R-Pres-OC-1 OverCapacityEdge stays attention across buckets', () => {
    for (const status of ['Draft', 'Ready', 'Running'] as const) {
      const p = structureDiagnosticPresentation('OverCapacityEdge', {
        stageStatus: status,
      });
      expect(p.tone).toBe('attention');
      expect(p.severity).toBe('warning');
    }
  });

  it('R-Pres-NR-1 NonRootUnfed progress when incompleteness expected', () => {
    const p = structureDiagnosticPresentation('NonRootUnfed', {
      stageStatus: 'Draft',
      isEntryBaselineStage: false,
      hasInboundQualProgFeed: false,
      affectationEntryCount: 0,
    });
    expect(p.tone).toBe('progress');
  });

  it('R-Pres-NR-2 NonRootUnfed attention when inbound configured', () => {
    const p = structureDiagnosticPresentation('NonRootUnfed', {
      stageStatus: 'Draft',
      hasInboundQualProgFeed: true,
    });
    expect(p.tone).toBe('attention');
  });

  it('R-Pres-NR-3 Ready NonRootUnfed attention', () => {
    const p = structureDiagnosticPresentation('NonRootUnfed', {
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('attention');
    expect(p.severity).toBe('warning');
  });

  it('R-Pres-A3-1 AwaitingInboundQualification progress on Ready', () => {
    const p = structureDiagnosticPresentation('AwaitingInboundQualification', {
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('progress');
    expect(p.severity).toBe('warning');
  });

  it('R-Pres-AA-1 Cup AuthoredAward danger', () => {
    const p = structureDiagnosticPresentation('AuthoredAwardUnreachable', {
      formatKind: 'Cup',
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('danger');
  });

  it('R-Pres-AA-2 classifying AuthoredAward attention', () => {
    const p = structureDiagnosticPresentation('AuthoredAwardUnreachable', {
      formatKind: 'Championship',
      stageStatus: 'Ready',
    });
    expect(p.tone).toBe('attention');
  });

  it('R-Pres-ST-1 Completed bucket is hidden', () => {
    expect(structurePresentationPhaseBucket('Completed')).toBe('hidden');
    const p = structureDiagnosticPresentation('CycleDetected', {
      stageStatus: 'Completed',
    });
    expect(p.severity).toBe('hidden');
    expect(p.tone).toBeNull();
  });

  it('R-Pres-SFM-1 Draft SlotFeedMissing is attention', () => {
    const p = structureDiagnosticPresentation('SlotFeedMissing', {
      stageStatus: 'Draft',
    });
    expect(p.severity).toBe('warning');
    expect(p.tone).toBe('attention');
  });

  it('R-Pres-SFM-2 Ready SlotFeedMissing is danger', () => {
    const p = structureDiagnosticPresentation('SlotFeedMissing', {
      stageStatus: 'Ready',
    });
    expect(p.severity).toBe('blocking');
    expect(p.tone).toBe('danger');
  });

  it('R-Pres-ST-2 Suspended uses Running bucket (attention for OC)', () => {
    expect(structurePresentationPhaseBucket('Suspended')).toBe('Running');
    const p = structureDiagnosticPresentation('OverCapacityEdge', {
      stageStatus: 'Suspended',
    });
    expect(p.tone).toBe('attention');
  });
});
