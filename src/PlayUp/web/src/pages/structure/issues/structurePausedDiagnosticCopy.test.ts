import { describe, expect, it } from 'vitest';
import { resolveStructureDiagnosticIssueTitleKey } from './structurePausedDiagnosticCopy';

describe('structurePausedDiagnosticCopy', () => {
  it('keeps default keys when stage is Running', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey('PopulationShortfall', 'Running'),
    ).toBe('graph.issues.PopulationShortfall');
  });

  it('uses paused keys when stage is Suspended', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'PopulationShortfall',
        'Suspended',
      ),
    ).toBe('graph.issues.PopulationShortfallPaused');
  });

  it('keeps default for unknown codes on Suspended', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey('UnknownCode', 'Suspended'),
    ).toBe('graph.issues.UnknownCode');
  });
});
