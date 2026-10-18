import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../../types';
import { stageWorstPresentedDiagnosticTone } from './structureStageTopologyPresentation';

function view(stages: StructureStageHubSummary[]): StructureView {
  return {
    competitionId: 'c1',
    status: 'Draft',
    format: { kind: 'Groups' },
    readiness: {
      blockers: [],
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForNextSlice: false,
    },
    stages,
  } as unknown as StructureView;
}

function stage(
  partial: Partial<StructureStageHubSummary> & Pick<StructureStageHubSummary, 'stageId'>,
): StructureStageHubSummary {
  return {
    name: 'Poules',
    status: 'Draft',
    formatKind: 'Groups',
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
    affectationEntryCount: 0,
    compositionCapacity: 8,
    structureIssues: [],
    ...partial,
  };
}

describe('stageWorstPresentedDiagnosticTone', () => {
  it('returns null for Completed phases', () => {
    const s = stage({
      stageId: 'a',
      status: 'Completed',
      structureIssues: ['DanglingQualificationTarget'],
    });
    expect(stageWorstPresentedDiagnosticTone(view([s]), s)).toBeNull();
  });

  it('returns danger for blocking graph issues', () => {
    const s = stage({
      stageId: 'a',
      structureIssues: ['DanglingQualificationTarget'],
    });
    expect(stageWorstPresentedDiagnosticTone(view([s]), s)).toBe('danger');
  });

  it('returns attention for Draft population shortfall', () => {
    const s = stage({
      stageId: 'a',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    expect(stageWorstPresentedDiagnosticTone(view([s]), s)).toBe('attention');
  });
});
