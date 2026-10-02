import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../../types';
import {
  computeStructureReadinessSummary,
  classifyReadinessHeaderTone,
  stageAmbientKinds,
  stagePopulationGap,
} from './structureReadinessSummary';

function stage(
  partial: Partial<StructureStageHubSummary> &
    Pick<StructureStageHubSummary, 'stageId' | 'name'>,
): StructureStageHubSummary {
  return {
    status: 'Draft',
    teamCount: 0,
    matchCount: 0,
    groupCount: 2,
    roundCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: true,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Groups',
    placesPerGroup: 4,
    compositionCapacity: 8,
    affectationEntryCount: 0,
    ...partial,
  };
}

function view(
  stages: StructureStageHubSummary[],
  blockers: string[] = [],
): StructureView {
  return {
    competitionId: 'comp',
    name: 'Test',
    status: 'Draft',
    participants: {
      activeCount: 0,
      occupyingCount: 0,
      entries: [],
    },
    format: {
      kind: 'Groups',
      primaryStageId: stages[0]?.stageId ?? null,
      primaryStageName: stages[0]?.name ?? null,
      primaryStageStatus: 'Draft',
    },
    structure: {
      groupCount: 2,
      roundCount: 0,
      matchdayCount: 0,
      slotCount: 0,
      hasDrawRules: false,
      numberOfPots: null,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    regulation: {
      minimumTeams: 2,
      maximumTeams: 64,
      durationPerPeriod: 45,
      numberOfPeriods: 2,
      winPoints: 3,
      drawPoints: 1,
      lossPoints: 0,
    },
    readiness: {
      readyForNextSlice: false,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers,
    },
    actions: [],
    stages,
  };
}

describe('structureReadinessSummary', () => {
  it('yields empty summary for birth (0 phase) despite Host birth blockers', () => {
    const summary = computeStructureReadinessSummary(
      view([], ['MissingStage', 'InsufficientParticipants']),
      [],
    );
    expect(summary.hasAny).toBe(false);
    expect(summary.headerTone).toBe('none');
    expect(summary.shapeInvalid).toBe(false);
  });

  it('ignores InsufficientParticipants when phases exist (Teams owns correction)', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      affectationEntryCount: 8,
      compositionCapacity: 8,
    });
    const summary = computeStructureReadinessSummary(
      view([poules], ['InsufficientParticipants']),
      [poules],
    );
    expect(summary.hasAny).toBe(false);
    expect(summary.headerTone).toBe('none');
  });

  it('counts graph issues under the graph family', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      structureIssues: ['DanglingQualificationTarget'],
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const summary = computeStructureReadinessSummary(view([poules]), [poules]);
    expect(summary.familyCounts).toEqual([
      { family: 'graph', count: 1 },
      { family: 'capacity', count: 1 },
    ]);
    expect(summary.missingPotsPhaseCount).toBe(0);
    expect(summary.hasAny).toBe(true);
    expect(summary.headerTone).toBe('danger');
    expect(summary.totalProblemCount).toBe(2);
  });

  it('counts population shortfall under capacity when no structureIssues', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const summary = computeStructureReadinessSummary(view([poules]), [poules]);
    expect(summary.familyCounts).toEqual([{ family: 'capacity', count: 1 }]);
    expect(summary.headerTone).toBe('attention');
    expect(stagePopulationGap(view([poules]), poules)).toBe(-8);
  });

  it('keeps OverCapacity and SlotFeedMissing as distinct family counts', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      structureIssues: ['OverCapacityEdge', 'SlotFeedMissing'],
      affectationEntryCount: 8,
      compositionCapacity: 8,
      hasDrawRules: false,
    });
    const summary = computeStructureReadinessSummary(view([poules]), [poules]);
    expect(summary.familyCounts).toEqual([
      { family: 'feed', count: 1 },
      { family: 'capacity', count: 1 },
    ]);
    expect(summary.headerTone).toBe('danger');
  });

  it('silences SlotFeedMissing from header when draw is engaged', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      structureIssues: ['SlotFeedMissing'],
      hasDrawRules: true,
      drawExecutionBadge: 'ToLaunch',
      numberOfPots: 2,
      affectationEntryCount: 8,
      compositionCapacity: 8,
    });
    const summary = computeStructureReadinessSummary(view([poules]), [poules]);
    expect(summary.hasAny).toBe(false);
    expect(summary.familyCounts).toEqual([]);
  });

  it('counts RESULT under result family as one presented problem', () => {
    const final = stage({
      stageId: 'a',
      name: 'Finale',
      formatKind: 'Cup',
      structureIssues: ['AuthoredAwardUnreachable', 'ExpectedRankUnreachable'],
      hasStandingRules: false,
      affectationEntryCount: 2,
      compositionCapacity: 2,
      groupCount: 0,
    });
    const summary = computeStructureReadinessSummary(view([final]), [final]);
    expect(summary.familyCounts).toEqual([{ family: 'result', count: 1 }]);
    expect(summary.totalProblemCount).toBe(1);
    expect(summary.headerTone).toBe('attention');
  });

  it('counts engaged missing pots only for Groups when no graph/shortfall', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      formatKind: 'Groups',
      hasDrawRules: true,
      numberOfPots: null,
      affectationEntryCount: 8,
      compositionCapacity: 8,
    });
    const summary = computeStructureReadinessSummary(view([poules]), [poules]);
    expect(summary.missingPotsPhaseCount).toBe(1);
    expect(summary.familyCounts).toEqual([]);
    expect(summary.headerTone).toBe('attention');
  });

  it('does not treat Cup null pots as MissingPotRules (Host-aligned E1/E6)', () => {
    const cup = stage({
      stageId: 'a',
      name: 'Finale',
      formatKind: 'Cup',
      hasDrawRules: true,
      numberOfPots: null,
      compositionCapacity: 8,
      affectationEntryCount: 8,
      hasStandingRules: false,
      groupCount: 0,
      placesPerGroup: undefined,
    });
    expect(stageAmbientKinds(view([cup]), cup)).toEqual([]);
    const summary = computeStructureReadinessSummary(view([cup]), [cup]);
    expect(summary.missingPotsPhaseCount).toBe(0);
    expect(summary.hasAny).toBe(false);
  });

  it('does not treat !hasDrawRules as draw ambient', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      hasDrawRules: false,
      numberOfPots: null,
      affectationEntryCount: 8,
      compositionCapacity: 8,
    });
    expect(stageAmbientKinds(view([poules]), poules)).toEqual([]);
    const summary = computeStructureReadinessSummary(view([poules]), [poules]);
    expect(summary.hasAny).toBe(false);
    expect(summary.headerTone).toBe('none');
  });

  it('lists ambient kinds by business family without inventing structureIssues', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      structureIssues: ['DanglingQualificationTarget'],
      hasDrawRules: true,
      numberOfPots: null,
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    expect(stageAmbientKinds(view([poules]), poules)).toEqual([
      'graph',
      'capacity',
      'drawPots',
    ]);
  });

  it('classifies header tone by diagnostic gravity, not child chrome', () => {
    expect(
      classifyReadinessHeaderTone({
        codes: ['PopulationShortfall'],
        missingPotsPhaseCount: 1,
        shapeInvalid: false,
      }),
    ).toBe('attention');
    expect(
      classifyReadinessHeaderTone({
        codes: ['DanglingQualificationTarget', 'SlotFeedMissing'],
        missingPotsPhaseCount: 0,
        shapeInvalid: false,
      }),
    ).toBe('danger');
    expect(
      classifyReadinessHeaderTone({
        codes: [],
        missingPotsPhaseCount: 0,
        shapeInvalid: true,
      }),
    ).toBe('danger');
  });
});
