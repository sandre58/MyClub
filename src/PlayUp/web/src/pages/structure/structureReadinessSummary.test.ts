import { describe, expect, it } from 'vitest';
import type {
  StructureReadinessBlocker,
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import { collectAllStructureReadinessActionItems } from './structureReadinessActions';
import {
  computeStructureReadinessSummary,
  classifyReadinessHeaderTone,
  presentedStructureProblemsSuspendedOnly,
  stageAmbientKinds,
  stagePopulationGap,
} from './structureReadinessSummary';
import { withFixturePresentedIssueCodes } from './structurePresentedIssueCodes.fixture';

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
  blockerDetails: StructureReadinessBlocker[] = [],
): StructureView {
  const draft: StructureView = {
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
      blockerDetails,
    },
    actions: [],
    stages,
  };
  return {
    ...draft,
    stages: draft.stages.map((hub) =>
      withFixturePresentedIssueCodes(draft, hub),
    ),
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
    const data = view([poules]);
    const summary = computeStructureReadinessSummary(data, data.stages);
    expect(summary.familyCounts).toEqual([
      { family: 'graph', count: 1 },
      { family: 'capacity', count: 1 },
    ]);
    expect(summary.missingPotsPhaseCount).toBe(0);
    expect(summary.hasAny).toBe(true);
    expect(summary.headerTone).toBe('danger');
    expect(summary.presentedProblemCount).toBe(2);
    expect(summary.presentedProblemCount).toBe(2);
  });

  it('counts population shortfall under capacity when no structureIssues', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const data = view([poules]);
    const summary = computeStructureReadinessSummary(data, data.stages);
    expect(summary.familyCounts).toEqual([{ family: 'capacity', count: 1 }]);
    expect(summary.headerTone).toBe('attention');
    expect(summary.showPageHeadNegative).toBe(false);
    expect(stagePopulationGap(view([poules]), poules)).toBe(-8);
  });

  it('sets showPageHeadNegative when Ready population shortfall is danger', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      status: 'Ready',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const data = view([poules]);
    const summary = computeStructureReadinessSummary(data, data.stages);
    expect(summary.headerTone).toBe('danger');
    expect(summary.showPageHeadNegative).toBe(true);
  });

  it('keeps OverCapacity and SlotFeedMissing as distinct family counts', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      status: 'Ready',
      structureIssues: ['OverCapacityEdge', 'SlotFeedMissing'],
      affectationEntryCount: 8,
      compositionCapacity: 8,
      hasDrawRules: false,
    });
    const data = view([poules]);
    const summary = computeStructureReadinessSummary(data, data.stages);
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
    const data = view([poules]);
    const summary = computeStructureReadinessSummary(data, data.stages);
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
    const data = view([final]);
    const summary = computeStructureReadinessSummary(data, data.stages);
    expect(summary.familyCounts).toEqual([{ family: 'result', count: 1 }]);
    expect(summary.presentedProblemCount).toBe(1);
    expect(summary.headerTone).toBe('danger');
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
    const data = view([poules]);
    expect(data.stages[0]?.presentedIssueCodes).toContain('MissingPotRules');
    const summary = computeStructureReadinessSummary(data, data.stages);
    const potActions = collectAllStructureReadinessActionItems(
      data,
      data.stages,
    ).filter((item) => item.code === 'MissingPotRules');
    expect(summary.missingPotsPhaseCount).toBe(1);
    expect(summary.presentedProblemCount).toBe(1);
    expect(summary.familyCounts).toEqual([]);
    expect(potActions).toHaveLength(1);
    expect(summary.headerTone).toBe('danger');
  });

  it('counts MissingPotRules once when Host already sent the code', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      formatKind: 'Groups',
      hasDrawRules: true,
      numberOfPots: null,
      affectationEntryCount: 8,
      compositionCapacity: 8,
      presentedIssueCodes: ['MissingPotRules'],
    });
    const data = view([poules]);
    const summary = computeStructureReadinessSummary(data, data.stages);
    const potActions = collectAllStructureReadinessActionItems(
      data,
      data.stages,
    ).filter((item) => item.code === 'MissingPotRules');
    expect(summary.missingPotsPhaseCount).toBe(1);
    expect(summary.presentedProblemCount).toBe(1);
    expect(summary.familyCounts).toEqual([]);
    expect(potActions).toHaveLength(1);
    expect(potActions[0]?.id).toBe('a:MissingPotRules');
    expect(potActions[0]?.href).toContain('section=tirage');
    expect(stageAmbientKinds(data, data.stages[0]!)).toEqual(['drawPots']);
  });

  it('does not invent MissingPotRules when Host presentedIssueCodes is empty', () => {
    const poules = stage({
      stageId: 'a',
      name: 'Poules',
      formatKind: 'Groups',
      hasDrawRules: true,
      numberOfPots: null,
      affectationEntryCount: 8,
      compositionCapacity: 8,
      presentedIssueCodes: [],
    });
    const data = view([poules]);
    const summary = computeStructureReadinessSummary(data, data.stages);
    expect(summary.missingPotsPhaseCount).toBe(0);
    expect(summary.presentedProblemCount).toBe(0);
    expect(summary.hasAny).toBe(false);
    expect(stageAmbientKinds(data, data.stages[0]!)).toEqual([]);
    expect(
      collectAllStructureReadinessActionItems(data, data.stages).filter(
        (item) => item.code === 'MissingPotRules',
      ),
    ).toHaveLength(0);
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
    const cupData = view([cup]);
    expect(stageAmbientKinds(cupData, cupData.stages[0]!)).toEqual([]);
    const summary = computeStructureReadinessSummary(cupData, cupData.stages);
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
    const data = view([poules]);
    expect(stageAmbientKinds(data, data.stages[0]!)).toEqual([]);
    const summary = computeStructureReadinessSummary(data, data.stages);
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
    const ambientData = view([poules]);
    expect(stageAmbientKinds(ambientData, ambientData.stages[0]!)).toEqual([
      'graph',
      'capacity',
      'drawPots',
    ]);
  });

  it('separates presented problem count from actionable count', () => {
    const stages = [
      stage({
        stageId: 's1',
        name: 'A',
        structureIssues: ['SlotFeedMissing'],
      }),
      stage({
        stageId: 's2',
        name: 'B',
        structureIssues: ['CycleDetected'],
      }),
      stage({
        stageId: 's3',
        name: 'C',
        structureIssues: ['NonRootUnfed'],
        affectationEntryCount: 8,
        compositionCapacity: 8,
      }),
      stage({
        stageId: 's4',
        name: 'D',
        structureIssues: ['AuthoredAwardUnreachable'],
        formatKind: 'Cup',
        hasStandingRules: false,
        groupCount: 0,
        affectationEntryCount: 2,
        compositionCapacity: 2,
      }),
    ];
    const data = view(stages);
    const summary = computeStructureReadinessSummary(data, data.stages);
    const actionable = collectAllStructureReadinessActionItems(
      data,
      data.stages,
    ).length;
    expect(summary.presentedProblemCount).toBeGreaterThan(3);
    expect(summary.actionableCount).toBe(actionable);
    expect(summary.actionableCount).toBeGreaterThan(3);
  });

  it('presentedStructureProblemsSuspendedOnly is true when only Suspended stages contribute', () => {
    const suspended = stage({
      stageId: 's1',
      name: 'Cup',
      status: 'Suspended',
      formatKind: 'Cup',
      affectationEntryCount: 2,
      compositionCapacity: 8,
    });
    const data = view([suspended]);
    expect(presentedStructureProblemsSuspendedOnly(data, data.stages)).toBe(
      true,
    );
    expect(
      computeStructureReadinessSummary(data, data.stages)
        .presentedStructureProblemsSuspendedOnly,
    ).toBe(true);
  });

  it('presentedStructureProblemsSuspendedOnly is false when a Ready stage also contributes', () => {
    const suspended = stage({
      stageId: 's1',
      name: 'Cup',
      status: 'Suspended',
      formatKind: 'Cup',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const ready = stage({
      stageId: 's2',
      name: 'Groups',
      status: 'Ready',
      structureIssues: ['SlotFeedMissing'],
      hasDrawRules: false,
    });
    const data = view([suspended, ready]);
    expect(presentedStructureProblemsSuspendedOnly(data, data.stages)).toBe(
      false,
    );
  });

  it('presentedStructureProblemsSuspendedOnly is false when a Running stage also contributes', () => {
    const suspended = stage({
      stageId: 's1',
      name: 'Cup',
      status: 'Suspended',
      formatKind: 'Cup',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const running = stage({
      stageId: 's2',
      name: 'Groups',
      status: 'Running',
      structureIssues: ['CycleDetected'],
    });
    const data = view([suspended, running]);
    expect(presentedStructureProblemsSuspendedOnly(data, data.stages)).toBe(
      false,
    );
  });

  it('presentedStructureProblemsSuspendedOnly is false when shape is invalid', () => {
    const suspended = stage({
      stageId: 's1',
      name: 'Cup',
      status: 'Suspended',
      formatKind: 'Cup',
      affectationEntryCount: 0,
      compositionCapacity: 8,
    });
    const data = view([suspended], ['CupBracketInvalid']);
    expect(presentedStructureProblemsSuspendedOnly(data, data.stages)).toBe(
      false,
    );
  });

  it('classifies header tone by diagnostic gravity, not child chrome', () => {
    expect(
      classifyReadinessHeaderTone({
        codeTone: 'attention',
        missingPotsPhaseCount: 1,
        shapeInvalid: false,
      }),
    ).toBe('attention');
    expect(
      classifyReadinessHeaderTone({
        codeTone: 'danger',
        missingPotsPhaseCount: 0,
        shapeInvalid: false,
      }),
    ).toBe('danger');
    expect(
      classifyReadinessHeaderTone({
        codeTone: 'none',
        missingPotsPhaseCount: 0,
        shapeInvalid: true,
      }),
    ).toBe('danger');
  });
});
