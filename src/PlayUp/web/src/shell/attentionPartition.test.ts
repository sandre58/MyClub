import { describe, expect, it } from 'vitest';
import {
  isAttentionDrawerForbiddenSource,
  partitionAttentionItems,
} from './attentionPartition';
import type { OverviewSituation } from '../types';

function situation(
  source: string,
  nature: string = 'Blocking',
): OverviewSituation {
  return {
    source,
    nature,
    targetType: 'Structure',
    targetId: 'comp-1',
    matchId: null,
    actionable: false,
    actionCode: null,
    impactCode: null,
    params: {},
  };
}

describe('attentionPartition', () => {
  it('drops construction blockers from the drawer (Q1 guardrail)', () => {
    const { blocking, attention } = partitionAttentionItems([
      situation('StructureGraphInvalid'),
      situation('MissingStructure'),
      situation('MissingStage'),
      situation('MissingPotRules'),
      situation('CupBracketInvalid'),
      situation('DrawNoSolution'),
      situation('QualificationPending'),
    ]);

    expect(blocking.map((item) => item.source)).toEqual([
      'DrawNoSolution',
      'QualificationPending',
    ]);
    expect(attention).toEqual([]);
  });

  it('keeps InsufficientParticipants (presentation; SoT remains Teams)', () => {
    const { blocking } = partitionAttentionItems([
      situation('InsufficientParticipants'),
    ]);
    expect(blocking).toHaveLength(1);
  });

  it('isAttentionDrawerForbiddenSource covers construction set', () => {
    expect(isAttentionDrawerForbiddenSource('StructureGraphInvalid')).toBe(
      true,
    );
    expect(isAttentionDrawerForbiddenSource('DrawNoSolution')).toBe(false);
  });
});
