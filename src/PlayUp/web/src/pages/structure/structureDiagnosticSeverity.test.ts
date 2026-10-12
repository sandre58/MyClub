import { describe, expect, it } from 'vitest';
import { structureProductSeverity } from './structureDiagnosticSeverity';
import { structureDiagnosticTone } from './structureDiagnosticPresentation';

describe('structureDiagnosticSeverity (D5)', () => {
  it('classifies blocking vs warning codes', () => {
    expect(structureProductSeverity('OverCapacityEdge')).toBe('warning');
    expect(structureProductSeverity('SlotFeedMissing')).toBe('blocking');
    expect(structureProductSeverity('PopulationShortfall')).toBe('blocking');
    expect(structureProductSeverity('NonRootUnfed')).toBe('warning');
    expect(structureProductSeverity('AuthoredAwardUnreachable')).toBe(
      'warning',
    );
  });

  it('maps severity to diagnostic tone without downgrading blocking', () => {
    expect(structureDiagnosticTone('PopulationShortfall')).toBe('danger');
    expect(structureDiagnosticTone('OverCapacityEdge')).toBe('attention');
    expect(structureDiagnosticTone('NonRootUnfed')).toBe('progress');
    expect(structureDiagnosticTone('AuthoredAwardUnreachable')).toBe(
      'attention',
    );
  });
});
