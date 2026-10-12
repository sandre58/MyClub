import { describe, expect, it } from 'vitest';
import {
  dedupeAttentionSituations,
  isAttentionDrawerForbiddenSource,
  partitionAttentionItems,
  presentableAttentionCount,
  presentableAttentionSituations,
} from './attentionPartition';
import type { NeedsAttentionItem } from '../types';
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
  it('drops birth and aggregate sources from the drawer (guardrail)', () => {
    const { blocking, attention } = partitionAttentionItems([
      situation('StructureGraphInvalid'),
      situation('MissingStructure'),
      situation('MissingStage'),
      situation('MissingPotRules'),
      situation('CupBracketInvalid'),
      situation('DrawNoSolution'),
      situation('QualificationPending'),
      situation('SlotFeedMissing', 'Blocking'),
      situation('NonRootUnfed', 'Warning'),
    ]);

    expect(blocking.map((item) => item.source)).toEqual([
      'MissingPotRules',
      'CupBracketInvalid',
      'DrawNoSolution',
      'QualificationPending',
      'SlotFeedMissing',
    ]);
    expect(attention.map((item) => item.source)).toEqual(['NonRootUnfed']);
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

  it('dedupeAttentionSituations collapses identical source and target', () => {
    const a = situation('QualificationPending');
    const b = { ...a };
    expect(dedupeAttentionSituations([a, b])).toHaveLength(1);
  });

  it('presentableAttentionCount is zero when only denylisted sources remain', () => {
    const wire: NeedsAttentionItem[] = [
      {
        source: 'MissingStage',
        severity: 'Blocking',
        targetType: 'Competition',
        targetId: 'c1',
      },
    ];
    expect(presentableAttentionCount(wire)).toBe(0);
    expect(presentableAttentionSituations(wire)).toEqual([]);
  });

  it('presentableAttentionCount includes blocking and warning rows', () => {
    const wire: NeedsAttentionItem[] = [
      {
        source: 'DrawNoSolution',
        severity: 'Blocking',
        targetType: 'Stage',
        targetId: 's1',
      },
      {
        source: 'NonRootUnfed',
        severity: 'Warning',
        targetType: 'Stage',
        targetId: 's2',
      },
    ];
    expect(presentableAttentionCount(wire)).toBe(2);
  });
});
