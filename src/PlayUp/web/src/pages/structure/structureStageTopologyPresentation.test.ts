import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../../types';
import { stageWorstPresentedDiagnosticTone } from './structureStageTopologyPresentation';
import { withFixturePresentedIssueCodes } from './structurePresentedIssueCodes.fixture';

function view(stages: StructureStageHubSummary[]): StructureView {
  const draft = {
    competitionId: 'c1',
    status: 'Draft',
    format: { kind: 'Groups' },
    readiness: {
      blockers: [],
      blockerDetails: [],
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForNextSlice: false,
    },
    stages,
  } as unknown as StructureView;
  return {
    ...draft,
    stages: draft.stages.map((hub) =>
      withFixturePresentedIssueCodes(draft, hub),
    ),
  };
}

function stage(
  partial: Partial<StructureStageHubSummary> &
    Pick<StructureStageHubSummary, 'stageId'>,
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
    const data = view([s]);
    expect(stageWorstPresentedDiagnosticTone(data, data.stages[0]!)).toBeNull();
  });

  it('returns danger for blocking graph issues', () => {
    const s = stage({
      stageId: 'a',
      structureIssues: ['DanglingQualificationTarget'],
    });
    const data = view([s]);
    expect(stageWorstPresentedDiagnosticTone(data, data.stages[0]!)).toBe(
      'danger',
    );
  });

  it('returns attention for Draft population shortfall', () => {
    const s = stage({
      stageId: 'a',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const data = view([s]);
    expect(stageWorstPresentedDiagnosticTone(data, data.stages[0]!)).toBe(
      'attention',
    );
  });

  it('returns danger when Host presentedIssueCodes includes CupBracketInvalid', () => {
    const cup = stage({
      stageId: 'cup-1',
      formatKind: 'Cup',
      structureIssues: [],
      presentedIssueCodes: ['CupBracketInvalid'],
    });
    expect(stageWorstPresentedDiagnosticTone(view([cup]), cup)).toBe('danger');
  });

  it('returns null for informative progress presented codes', () => {
    const ch = stage({
      stageId: 'ch-1',
      status: 'Ready',
      formatKind: 'Championship',
      structureIssues: [],
      presentedIssueCodes: ['AwaitingInboundQualification'],
    });
    expect(stageWorstPresentedDiagnosticTone(view([ch]), ch)).toBeNull();
  });

  it('returns danger when Host presentedIssueCodes includes UnmaterializedConfrontations', () => {
    const ch = stage({
      stageId: 'ch-1',
      status: 'Ready',
      formatKind: 'Championship',
      structureIssues: [],
      presentedIssueCodes: ['UnmaterializedConfrontations'],
    });
    expect(stageWorstPresentedDiagnosticTone(view([ch]), ch)).toBe('danger');
  });
});
