import { describe, expect, it } from 'vitest';

import type { StructureView } from '../../types';
import { resolveStructureHubStages } from './structureResolveStages';

function minimalView(
  overrides: Partial<StructureView> & Pick<StructureView, 'format'>,
): StructureView {
  return {
    competitionId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    name: 'Comp',
    status: 'Draft',
    participants: { activeCount: 0, occupyingCount: 8, entries: [] },
    structure: {
      groupCount: 2,
      roundCount: 0,
      hasDrawRules: true,
      numberOfPots: 4,
      swissRoundCount: 0,
    },
    regulation: {
      numberOfPeriods: 2,
      durationPerPeriod: 45,
      hasExtraTime: false,
      hasPenaltyShootout: false,
    },
    readiness: {
      readyForNextSlice: true,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers: [],
    },
    actions: [],
    stages: [],
    ...overrides,
  };
}

describe('resolveStructureHubStages', () => {
  it('returns Host stages when present', () => {
    const stage = {
      stageId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      name: 'Host',
      status: 'Draft' as const,
      teamCount: 4,
      matchCount: 0,
      numberOfPeriods: 2,
      durationPerPeriod: 45,
      hasExtraTime: false,
      hasPenaltyShootout: false,
      hasQualificationRules: false,
      qualificationPathCount: 0,
      hasProgressionRules: false,
      progressionPathCount: 0,
      hasTieFormat: false,
    };
    const data = minimalView({
      format: {
        kind: 'Groups',
        primaryStageId: stage.stageId,
        primaryStageName: 'Ignored',
        primaryStageStatus: 'Draft',
      },
      stages: [stage],
    });
    expect(resolveStructureHubStages(data)).toEqual([stage]);
  });

  it('synthesizes a single stage from format when stages[] is empty', () => {
    const data = minimalView({
      format: {
        kind: 'Groups',
        primaryStageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
        primaryStageName: 'Poules',
        primaryStageStatus: 'Running',
      },
    });
    const [resolved] = resolveStructureHubStages(data);
    expect(resolved.stageId).toBe('cccccccc-cccc-cccc-cccc-cccccccccccc');
    expect(resolved.name).toBe('Poules');
    expect(resolved.status).toBe('Running');
    expect(resolved.formatKind).toBe('Groups');
    expect(resolved.hasDrawRules).toBe(true);
    expect(resolved.teamCount).toBe(8);
  });

  it('returns empty when there is no primary stage', () => {
    const data = minimalView({
      format: { kind: 'Groups', primaryStageId: null },
    });
    expect(resolveStructureHubStages(data)).toEqual([]);
  });
});
