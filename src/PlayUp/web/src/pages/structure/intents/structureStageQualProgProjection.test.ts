import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary } from '../../../types';
import {
  outboundDestinationStageIds,
  qualificationEdges,
  stageHasInboundQualProgTo,
} from './structureStageQualProgProjection';

function stage(
  partial: Partial<StructureStageHubSummary> &
    Pick<StructureStageHubSummary, 'stageId' | 'name'>,
): StructureStageHubSummary {
  return {
    status: 'Draft',
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
    ...partial,
  };
}

describe('structureStageQualProgProjection', () => {
  it('qualificationEdges reads hub intents', () => {
    const s = stage({
      stageId: 'a',
      name: 'A',
      qualificationIntents: [
        {
          intentId: 'i1',
          order: 1,
          sourceKind: 'Overall',
          positionFrom: 1,
          positionTo: 1,
          destinationStageId: 'b',
        },
      ],
    });
    expect(qualificationEdges(s)).toHaveLength(1);
    expect(qualificationEdges(s)[0].kind).toBe('intent');
  });

  it('outboundDestinationStageIds dedupes qual and prog targets', () => {
    const s = stage({
      stageId: 'src',
      name: 'Src',
      qualificationIntents: [
        {
          intentId: 'q',
          order: 1,
          sourceKind: 'Overall',
          positionFrom: 1,
          positionTo: 1,
          destinationStageId: 'dest',
        },
      ],
      progressionIntents: [
        {
          intentId: 'p',
          order: 1,
          roundId: 'r',
          outcome: 'Winner',
          destinationStageId: 'dest',
        },
      ],
    });
    expect(outboundDestinationStageIds(s)).toEqual(['dest']);
  });

  it('stageHasInboundQualProgTo detects intent targets', () => {
    const stages = [
      stage({
        stageId: 'src',
        name: 'Src',
        progressionIntents: [
          {
            intentId: 'p',
            order: 1,
            roundId: 'r',
            outcome: 'Loser',
            destinationStageId: 'dest',
          },
        ],
      }),
      stage({ stageId: 'dest', name: 'Dest' }),
    ];
    expect(stageHasInboundQualProgTo(stages, 'dest')).toBe(true);
    expect(stageHasInboundQualProgTo(stages, 'missing')).toBe(false);
  });
});
