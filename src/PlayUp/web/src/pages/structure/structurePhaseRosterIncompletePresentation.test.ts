import { describe, expect, it } from 'vitest';

import {
  resolvePhaseRosterIncompleteCopyVariant,
  resolvePhaseRosterIncompleteMaterializeMessageKey,
  resolvePhaseRosterIncompleteOperationalMessageKey,
} from './structurePhaseRosterIncompletePresentation';
import {
  resolveStructureDiagnosticIssueDescriptionKey,
  resolveStructureDiagnosticIssueTitleKey,
} from './structurePausedDiagnosticCopy';

describe('PhaseRosterIncomplete presentation (arbitrage C)', () => {
  const bandIncomplete = { compositionEntryCount: 7, compositionCapacity: 8 };
  const eligibilityGap = { compositionEntryCount: 8, compositionCapacity: 8 };

  it('ready recette shape selects band incomplete variant', () => {
    expect(resolvePhaseRosterIncompleteCopyVariant(bandIncomplete)).toBe(
      'bandIncomplete',
    );
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'PhaseRosterIncomplete',
        'Ready',
        bandIncomplete,
      ),
    ).toBe('graph.issues.PhaseRosterIncomplete');
    expect(
      resolveStructureDiagnosticIssueDescriptionKey(
        'PhaseRosterIncomplete',
        'Ready',
        bandIncomplete,
      ),
    ).toBe('graph.issueDescriptions.PhaseRosterIncompleteBandIncomplete');
    expect(
      resolvePhaseRosterIncompleteOperationalMessageKey(bandIncomplete),
    ).toBe('problems.lifecycle.PhaseRosterIncompleteBandIncomplete');
    expect(
      resolvePhaseRosterIncompleteMaterializeMessageKey(bandIncomplete),
    ).toBe('fiche.materialize.disabledReason.PhaseRosterIncompleteBandIncomplete');
  });

  it('full band hub selects eligibility gap variant', () => {
    expect(resolvePhaseRosterIncompleteCopyVariant(eligibilityGap)).toBe(
      'eligibilityGap',
    );
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'PhaseRosterIncomplete',
        'Ready',
        eligibilityGap,
      ),
    ).toBe('graph.issues.PhaseRosterIncompleteEligibilityGap');
    expect(
      resolveStructureDiagnosticIssueDescriptionKey(
        'PhaseRosterIncomplete',
        'Ready',
        eligibilityGap,
      ),
    ).toBe('graph.issueDescriptions.PhaseRosterIncompleteEligibilityGap');
  });

  it('uses neutral operational keys without composition context', () => {
    expect(resolvePhaseRosterIncompleteCopyVariant(undefined)).toBe('neutral');
    expect(resolvePhaseRosterIncompleteOperationalMessageKey(undefined)).toBe(
      'problems.lifecycle.PhaseRosterIncomplete',
    );
    expect(resolvePhaseRosterIncompleteMaterializeMessageKey(undefined)).toBe(
      'fiche.materialize.disabledReason.PhaseRosterIncomplete',
    );
  });

  it('leaves AwaitingInboundQualification copy unchanged', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'AwaitingInboundQualification',
        'Ready',
        eligibilityGap,
      ),
    ).toBe('graph.issues.AwaitingInboundQualification');
  });

  it('does not change other diagnostics', () => {
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'OverCapacityEdge',
        'Ready',
        eligibilityGap,
      ),
    ).toBe('graph.issues.OverCapacityEdge');
  });
});
