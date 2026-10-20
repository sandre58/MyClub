import { describe, expect, it } from 'vitest';

import {
  resolveStructureDiagnosticIssueDescriptionKey,
  resolveStructureDiagnosticIssueTitleKey,
} from './structurePausedDiagnosticCopy';
import { stageOperationalCompositionFull } from './structurePopulationShortfallPresentation';

describe('stageOperationalCompositionFull', () => {
  it('is true when composition entries meet capacity (forfeit still counts)', () => {
    expect(
      stageOperationalCompositionFull({
        compositionEntryCount: 8,
        compositionCapacity: 8,
      }),
    ).toBe(true);
  });

  it('is false when composition is below capacity', () => {
    expect(
      stageOperationalCompositionFull({
        compositionEntryCount: 7,
        compositionCapacity: 8,
      }),
    ).toBe(false);
  });

  it('is false when capacity is unknown', () => {
    expect(
      stageOperationalCompositionFull({
        compositionEntryCount: 8,
        compositionCapacity: null,
      }),
    ).toBe(false);
  });
});

describe('PopulationShortfall presentation copy (arbitrage B)', () => {
  const full = { compositionEntryCount: 8, compositionCapacity: 8 };
  const partial = { compositionEntryCount: 7, compositionCapacity: 8 };

  it('uses full-composition title when U1 shortfall and band is full', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'PopulationShortfall',
        'Running',
        full,
      ),
    ).toBe('graph.issues.PopulationShortfallFullComposition');
    expect(
      resolveStructureDiagnosticIssueDescriptionKey(
        'PopulationShortfall',
        'Running',
        full,
      ),
    ).toBe('graph.issueDescriptions.PopulationShortfallFullComposition');
  });

  it('keeps legacy title when composition is incomplete', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'PopulationShortfall',
        'Ready',
        partial,
      ),
    ).toBe('graph.issues.PopulationShortfall');
    expect(
      resolveStructureDiagnosticIssueDescriptionKey(
        'PopulationShortfall',
        'Ready',
        partial,
      ),
    ).toBeNull();
  });

  it('does not change other capacity diagnostics', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'OverCapacityEdge',
        'Ready',
        full,
      ),
    ).toBe('graph.issues.OverCapacityEdge');
  });
});
