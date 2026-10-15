import { describe, expect, it } from 'vitest';
import { structureProductSeverity } from './structureDiagnosticSeverity';
import { structureDiagnosticTone } from './structureDiagnosticPresentation';

describe('structureDiagnosticSeverity (D5)', () => {
  it('classifies blocking vs warning codes', () => {
    expect(structureProductSeverity('OverCapacityEdge')).toBe('warning');
    expect(structureProductSeverity('SlotFeedMissing')).toBe('blocking');
    expect(structureProductSeverity('PopulationShortfall')).toBe('warning');
    expect(structureProductSeverity('SourcePopulationShortfall')).toBe('warning');
    expect(structureProductSeverity('NonRootUnfed')).toBe('warning');
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

  it('maps severity to diagnostic tone without downgrading blocking', () => {
    expect(structureDiagnosticTone('PopulationShortfall')).toBe('attention');
    expect(structureDiagnosticTone('OverCapacityEdge')).toBe('attention');
    expect(structureDiagnosticTone('NonRootUnfed')).toBe('progress');
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
