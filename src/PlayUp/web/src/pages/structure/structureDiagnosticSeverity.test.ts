import { describe, expect, it } from 'vitest';
import { structureProductSeverity } from './structureDiagnosticSeverity';
import { structureDiagnosticTone } from './structureDiagnosticPresentation';

describe('structureDiagnosticSeverity (D5)', () => {
  it('classifies blocking vs warning codes in Draft', () => {
    expect(
      structureProductSeverity('OverCapacityEdge', { stageStatus: 'Draft' }),
    ).toBe('warning');
    expect(
      structureProductSeverity('SlotFeedMissing', { stageStatus: 'Draft' }),
    ).toBe('warning');
    expect(
      structureProductSeverity('SlotFeedMissing', { stageStatus: 'Ready' }),
    ).toBe('blocking');
    expect(
      structureProductSeverity('PopulationShortfall', { stageStatus: 'Draft' }),
    ).toBe('warning');
    expect(
      structureProductSeverity('PopulationShortfall', { stageStatus: 'Ready' }),
    ).toBe('blocking');
    expect(
      structureProductSeverity('NonRootUnfed', { stageStatus: 'Ready' }),
    ).toBe('warning');
  });

  it('AuthoredAwardUnreachable is blocking on Cup only (G-UI1)', () => {
    expect(
      structureProductSeverity('AuthoredAwardUnreachable', {
        formatKind: 'Cup',
      }),
    ).toBe('blocking');
    expect(
      structureProductSeverity('AuthoredAwardUnreachable', {
        formatKind: 'Championship',
      }),
    ).toBe('warning');
    expect(structureProductSeverity('AuthoredAwardUnreachable')).toBe(
      'blocking',
    );
  });

  it('maps severity to diagnostic tone', () => {
    expect(
      structureDiagnosticTone('PopulationShortfall', { stageStatus: 'Draft' }),
    ).toBe('attention');
    expect(
      structureDiagnosticTone('PopulationShortfall', { stageStatus: 'Ready' }),
    ).toBe('danger');
    expect(
      structureDiagnosticTone('OverCapacityEdge', { stageStatus: 'Draft' }),
    ).toBe('attention');
    expect(
      structureDiagnosticTone('NonRootUnfed', {
        stageStatus: 'Draft',
        isEntryBaselineStage: false,
        hasInboundQualProgFeed: false,
        affectationEntryCount: 0,
      }),
    ).toBe('progress');
    expect(
      structureDiagnosticTone('AuthoredAwardUnreachable', {
        formatKind: 'Cup',
      }),
    ).toBe('danger');
    expect(
      structureDiagnosticTone('AuthoredAwardUnreachable', {
        formatKind: 'Groups',
      }),
    ).toBe('attention');
  });
});
