import { describe, expect, it } from 'vitest';
import {
  countStructureIssuesByFamily,
  isSlotFeedAwaitingDraw,
  projectPresentedIssueCodes,
  projectPresentedStageIssueCodes,
  structureDiagnosticTile,
  structureDiagnosticTitleKey,
  structureDiagnosticTone,
  structureIssueBannersForTile,
  structureIssuesForTile,
} from './structureDiagnosticPresentation';
import type { StructureStageHubSummary } from '../../types';

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

describe('structureDiagnosticPresentation', () => {
  it('maps SlotFeedMissing without draw as danger', () => {
    expect(structureDiagnosticTone('SlotFeedMissing')).toBe('danger');
    expect(structureDiagnosticTile('SlotFeedMissing')).toBe('places');
  });

  it('silences SlotFeedMissing when draw is engaged (presentation only)', () => {
    const withDraw = stage({
      stageId: 'a',
      name: 'Poules',
      hasDrawRules: true,
      drawExecutionBadge: 'ToLaunch',
      structureIssues: ['SlotFeedMissing', 'CycleDetected'],
    });
    expect(isSlotFeedAwaitingDraw(withDraw)).toBe(true);
    expect(
      projectPresentedStageIssueCodes(
        ['SlotFeedMissing', 'CycleDetected'],
        withDraw,
      ),
    ).toEqual(['CycleDetected']);
    expect(
      structureIssueBannersForTile(['SlotFeedMissing'], 'places', {
        silenceSlotFeedMissing: true,
      }),
    ).toEqual([]);
  });

  it('keeps SlotFeedMissing when there is no draw', () => {
    const noDraw = stage({
      stageId: 'a',
      name: 'Poules',
      hasDrawRules: false,
      structureIssues: ['SlotFeedMissing'],
    });
    expect(
      projectPresentedStageIssueCodes(['SlotFeedMissing'], noDraw),
    ).toEqual(['SlotFeedMissing']);
  });

  it('merges RESULT for banners and presented counts', () => {
    expect(
      projectPresentedIssueCodes([
        'AuthoredAwardUnreachable',
        'ExpectedRankUnreachable',
      ]),
    ).toEqual(['AuthoredAwardUnreachable']);
    expect(
      countStructureIssuesByFamily(['AuthoredAwardUnreachable']),
    ).toEqual({
      graph: 0,
      feed: 0,
      capacity: 0,
      execution: 0,
      result: 1,
    });
  });

  it('keeps AuthoredAward title on path reachability (not Places N)', () => {
    expect(structureDiagnosticTitleKey('AuthoredAwardUnreachable')).toBe(
      'graph.issues.AuthoredAwardUnreachable',
    );
    expect(structureDiagnosticTone('AuthoredAwardUnreachable')).toBe(
      'attention',
    );
  });

  it('keeps OverCapacity and SlotFeedMissing on distinct tiles', () => {
    const codes = ['OverCapacityEdge', 'SlotFeedMissing'];
    expect(structureIssuesForTile(codes, 'capacity')).toEqual([
      'OverCapacityEdge',
    ]);
    expect(structureIssuesForTile(codes, 'places')).toEqual(['SlotFeedMissing']);
  });

  it('surfaces OverExtraction on Sorties (capacity family, exits tile)', () => {
    expect(structureDiagnosticTile('OverExtraction')).toBe('exits');
    expect(
      countStructureIssuesByFamily(['OverExtraction']).capacity,
    ).toBe(1);
  });
});
