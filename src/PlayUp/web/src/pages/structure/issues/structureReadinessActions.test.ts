import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../../../types';
import {
  collectAllStructureReadinessActionItems,
  resolveStructureDiagnosticNavigation,
} from './structureReadinessActions';
import { withFixturePresentedIssueCodes } from './structurePresentedIssueCodes.fixture';

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function baseView(
  partial: Partial<StructureView> & Pick<StructureView, 'stages'>,
): StructureView {
  const { stages, ...rest } = partial;
  const draft: StructureView = {
    competitionId,
    name: 'Comp',
    status: 'Draft',
    actions: [],
    format: {
      kind: 'Groups',
      primaryStageId: stages[0]?.stageId ?? null,
      primaryStageName: stages[0]?.name ?? null,
      primaryStageStatus: 'Draft',
    },
    participants: { activeCount: 0, occupyingCount: 0, entries: [] },
    structure: {
      groupCount: 0,
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
      numberOfPeriods: 2,
      durationPerPeriod: 45,
      winPoints: 3,
      drawPoints: 1,
      lossPoints: 0,
      hasExtraTime: false,
      hasPenaltyShootout: false,
    },
    readiness: {
      readyForNextSlice: false,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers: [],
      blockerDetails: [],
    },
    stages,
    ...rest,
  };
  return {
    ...draft,
    stages: draft.stages.map((hub) =>
      withFixturePresentedIssueCodes(draft, hub),
    ),
  };
}

function stage(
  partial: Partial<StructureStageHubSummary> &
    Pick<StructureStageHubSummary, 'stageId' | 'name'>,
): StructureStageHubSummary {
  return {
    status: 'Draft',
    teamCount: 0,
    matchCount: 0,
    groupCount: 0,
    roundCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: false,
    hasDrawRules: false,
    numberOfPots: null,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Groups',
    ...partial,
  };
}

describe('structureReadinessActions', () => {
  it('limits action items to three by severity', () => {
    // SlotFeedMissing is danger in Ready, attention in Draft; Cup-only blocking for awards.
    const stages = [
      stage({
        stageId: 's1',
        name: 'A',
        status: 'Ready',
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
      }),
      stage({
        stageId: 's4',
        name: 'D',
        structureIssues: ['AuthoredAwardUnreachable'],
      }),
    ];
    const data = baseView({ stages });
    const all = collectAllStructureReadinessActionItems(data, data.stages);
    expect(all).toHaveLength(4);
    expect(all.slice(0, 3).map((item) => item.tone)).toEqual([
      'danger',
      'danger',
      'attention',
    ]);
  });

  it('maps qualification issues to section deep-link', () => {
    expect(
      resolveStructureDiagnosticNavigation({
        competitionId,
        stageId: 's1',
        code: 'DanglingQualificationTarget',
        cta: 'qualification',
      }),
    ).toBe(
      `/competitions/${competitionId}/structure?stage=s1&section=qualification`,
    );
  });

  it('maps placement CTA to manual dialog when draw is not engaged', () => {
    expect(
      resolveStructureDiagnosticNavigation({
        competitionId,
        stageId: 's1',
        stage: stage({ stageId: 's1', name: 'Cup', hasDrawRules: false }),
        code: 'SlotFeedMissing',
        cta: 'places',
      }),
    ).toBe(`/competitions/${competitionId}/structure?stage=s1&manual=1`);
  });

  it('maps placement CTA to draw workflow when draw is engaged', () => {
    expect(
      resolveStructureDiagnosticNavigation({
        competitionId,
        stageId: 's1',
        stage: stage({ stageId: 's1', name: 'Cup', hasDrawRules: true }),
        code: 'SlotFeedMissing',
        cta: 'places',
      }),
    ).toBe(`/competitions/${competitionId}/structure?stage=s1&draw=1`);
  });

  it('emits distinct cup bracket actions per owner stage', () => {
    const cupA = stage({ stageId: 'cup-a', name: 'Cup A', formatKind: 'Cup' });
    const cupB = stage({ stageId: 'cup-b', name: 'Cup B', formatKind: 'Cup' });
    const data = baseView({
      stages: [cupA, cupB],
      readiness: {
        readyForNextSlice: false,
        readyForDraw: false,
        readyForMaterialization: false,
        readyForSchedule: false,
        readyForMatchOperation: false,
        readyForSchedulePath: false,
        attachedMatchCount: 0,
        blockers: ['CupBracketInvalid'],
        blockerDetails: [
          { code: 'CupBracketInvalid', stageId: 'cup-a' },
          { code: 'CupBracketInvalid', stageId: 'cup-b' },
        ],
      },
    });
    const items = collectAllStructureReadinessActionItems(data, [cupA, cupB]);
    const cupItems = items.filter((i) => i.code === 'CupBracketInvalid');
    expect(cupItems).toHaveLength(2);
    expect(cupItems.map((i) => i.stageId).sort()).toEqual(['cup-a', 'cup-b']);
    expect(cupItems[0]?.href).toContain('section=construction');
    expect(cupItems[1]?.href).toContain('section=construction');
    expect(new Set(cupItems.map((i) => i.id)).size).toBe(2);
  });

  it('maps MissingPotRules to tirage section deep-link (lot B entry)', () => {
    expect(
      resolveStructureDiagnosticNavigation({
        competitionId,
        stageId: 's1',
        code: 'MissingPotRules',
        cta: 'qualification',
      }),
    ).toBe(`/competitions/${competitionId}/structure?stage=s1&section=tirage`);
  });
});
