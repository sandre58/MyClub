import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import {
  matchProgressRemaining,
  remediationHref,
  resolveDraftPrepareStepIds,
  resolveDraftPrepareStepStatus,
  shouldShowLifecycleRemediationLink,
  shouldShowPrimaryBlockMessage,
} from './structurePhaseStatusBandModel';

const baseStage: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'League',
  status: 'Draft',
  teamCount: 4,
  matchCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: false,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
};

describe('structurePhaseStatusBandModel', () => {
  it('resolves draft step status from checklist and regulation gate', () => {
    const checklist = {
      allSatisfied: false,
      items: [
        { code: 'Entries', applicable: true, satisfied: false },
        { code: 'Draw', applicable: false, satisfied: false },
      ],
    };

    expect(resolveDraftPrepareStepStatus('Teams', checklist, null)).toBe('ko');
    expect(resolveDraftPrepareStepStatus('Draw', checklist, null)).toBe('na');
    expect(
      resolveDraftPrepareStepStatus('Regulation', checklist, 'StructureGraphInvalid'),
    ).toBe('ko');
  });

  it('hides StageNotReady band (stepper + ops tile own population signals)', () => {
    expect(shouldShowPrimaryBlockMessage('Draft', 'StageNotReady', null)).toBe(
      false,
    );
  });

  it('hides redundant block messages when stepper already explains the gate', () => {
    const checklist = {
      allSatisfied: false,
      items: [
        { code: 'Exits', applicable: true, satisfied: false },
      ],
    };
    expect(
      shouldShowPrimaryBlockMessage(
        'Draft',
        'StructureGraphInvalid',
        checklist,
      ),
    ).toBe(false);
    expect(
      shouldShowPrimaryBlockMessage(
        'Draft',
        'ConfrontationUndecidable',
        checklist,
      ),
    ).toBe(false);
    expect(
      shouldShowPrimaryBlockMessage('Running', 'OpenMatches', null),
    ).toBe(false);
    expect(
      shouldShowPrimaryBlockMessage('Draft', 'ConstructionFrozen', null),
    ).toBe(true);
  });

  it('inserts Attribution step when checklist row is applicable', () => {
    expect(resolveDraftPrepareStepIds(null)).not.toContain('Attribution');
    expect(
      resolveDraftPrepareStepIds({
        allSatisfied: false,
        items: [{ code: 'Attribution', applicable: true, satisfied: false }],
      }),
    ).toContain('Attribution');
  });

  it('hides remediation link for StageNotReady', () => {
    expect(
      shouldShowLifecycleRemediationLink('StageNotReady', '/competitions/x/structure'),
    ).toBe(false);
  });

  it('computes remaining matches', () => {
    expect(matchProgressRemaining(20, 12, 1)).toBe(7);
  });

  it('uses overview link for ConstructionFrozen when remediation is absent', () => {
    const href = remediationHref('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', {
      ...baseStage,
      lifecycle: {
        transition: {
          code: 'PrepareStage',
          enabled: false,
          disabledReasonCode: 'ConstructionFrozen',
        },
      },
    });

    expect(href).toBe(
      '/competitions/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/overview',
    );
  });
});
