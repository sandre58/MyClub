import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import {
  shouldShowMatchsLifecycle,
  shouldShowPrimaryBlockMessage,
  shouldShowRevertBlockMessage,
} from './structurePhaseStatusBandModel';

const minimalStage = (status: StructureStageHubSummary['status']): StructureStageHubSummary => ({
  stageId: 's1',
  name: 'P',
  status,
  teamCount: 0,
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
});

describe('structurePhaseStatusBandModel', () => {
  it('shows block messages for draft prepare gates', () => {
    expect(shouldShowPrimaryBlockMessage('Draft', 'StageNotReady')).toBe(true);
    expect(shouldShowPrimaryBlockMessage('Draft', 'StructureGraphInvalid')).toBe(
      true,
    );
  });

  it('hides open-matches banner on running phase', () => {
    expect(
      shouldShowPrimaryBlockMessage('Running', 'OpenMatches'),
    ).toBe(false);
  });

  it('hides inline copy for construction frozen and unmaterialized start gates', () => {
    expect(
      shouldShowPrimaryBlockMessage('Draft', 'ConstructionFrozen'),
    ).toBe(false);
    expect(
      shouldShowPrimaryBlockMessage('Ready', 'UnmaterializedConfrontations'),
    ).toBe(false);
  });

  it('shows revert block message when disabled reason is set', () => {
    expect(shouldShowRevertBlockMessage('ProductionPresent')).toBe(true);
    expect(shouldShowRevertBlockMessage(null)).toBe(false);
  });

  it('hides matchs lifecycle when phase is completed', () => {
    expect(shouldShowMatchsLifecycle(minimalStage('Completed'))).toBe(false);
  });

  it('shows matchs lifecycle for draft without lifecycle payload', () => {
    expect(shouldShowMatchsLifecycle(minimalStage('Draft'))).toBe(true);
  });
});
