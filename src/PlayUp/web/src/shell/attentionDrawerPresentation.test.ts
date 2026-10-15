import { describe, expect, it } from 'vitest';
import type { CompetitionStageSummary, OverviewSituation } from '../types';
import {
  attentionDrawerLedeKey,
  shouldUseNeutralAttentionDrawerLede,
} from './attentionDrawerPresentation';

function structureRow(
  source: string,
  targetId: string,
  params?: Record<string, string>,
): OverviewSituation {
  return {
    source,
    nature: 'Warning',
    targetType: 'Stage',
    targetId,
    matchId: null,
    actionable: false,
    actionCode: null,
    impactCode: null,
    params: params ?? { stageId: targetId },
  };
}

function t5Row(source: string): OverviewSituation {
  return {
    source,
    nature: 'Blocking',
    targetType: 'Stage',
    targetId: 'stage-run',
    matchId: null,
    actionable: true,
    actionCode: 'ApplyQualification',
    impactCode: null,
    params: { stageId: 'stage-run' },
  };
}

const suspendedStage: CompetitionStageSummary = {
  stageId: 'stage-sus',
  name: 'Cup',
  status: 'Suspended',
};

const runningStage: CompetitionStageSummary = {
  stageId: 'stage-run',
  name: 'Groups',
  status: 'Running',
};

describe('shouldUseNeutralAttentionDrawerLede', () => {
  it('is false when there are no presentable situations', () => {
    expect(shouldUseNeutralAttentionDrawerLede([], [suspendedStage])).toBe(
      false,
    );
  });

  it('is true when every row is structure on Suspended', () => {
    expect(
      shouldUseNeutralAttentionDrawerLede(
        [structureRow('PopulationShortfall', 'stage-sus')],
        [suspendedStage],
      ),
    ).toBe(true);
  });

  it('is false when a T5 row is present', () => {
    expect(
      shouldUseNeutralAttentionDrawerLede(
        [
          structureRow('PopulationShortfall', 'stage-sus'),
          t5Row('QualificationPending'),
        ],
        [suspendedStage, runningStage],
      ),
    ).toBe(false);
  });

  it('is false when a structure row is on Running', () => {
    expect(
      shouldUseNeutralAttentionDrawerLede(
        [structureRow('SlotFeedMissing', 'stage-run')],
        [runningStage],
      ),
    ).toBe(false);
  });

  it('CupBracketInvalid uses params.stageId — neutral when that stage is Suspended', () => {
    const row: OverviewSituation = {
      source: 'CupBracketInvalid',
      nature: 'Blocking',
      targetType: 'Competition',
      targetId: 'comp-1',
      matchId: null,
      actionable: false,
      actionCode: null,
      impactCode: null,
      params: { stageId: 'stage-sus' },
    };
    expect(
      shouldUseNeutralAttentionDrawerLede([row], [suspendedStage]),
    ).toBe(true);
  });

  it('CupBracketInvalid without params.stageId keeps action lede', () => {
    const row: OverviewSituation = {
      source: 'CupBracketInvalid',
      nature: 'Blocking',
      targetType: 'Competition',
      targetId: 'comp-1',
      matchId: null,
      actionable: false,
      actionCode: null,
      impactCode: null,
      params: {},
    };
    expect(
      shouldUseNeutralAttentionDrawerLede([row], [suspendedStage]),
    ).toBe(false);
  });

  it('CupBracketInvalid with unknown stageId keeps action lede', () => {
    const row: OverviewSituation = {
      source: 'CupBracketInvalid',
      nature: 'Blocking',
      targetType: 'Competition',
      targetId: 'comp-1',
      matchId: null,
      actionable: false,
      actionCode: null,
      impactCode: null,
      params: { stageId: 'missing' },
    };
    expect(
      shouldUseNeutralAttentionDrawerLede([row], [suspendedStage]),
    ).toBe(false);
  });
});

describe('attentionDrawerLedeKey', () => {
  it('maps to paused copy when neutral predicate holds', () => {
    expect(
      attentionDrawerLedeKey(
        [structureRow('SlotFeedMissing', 'stage-sus')],
        [suspendedStage],
      ),
    ).toBe('attention.ledePaused');
  });

  it('maps to default lede otherwise', () => {
    expect(attentionDrawerLedeKey([], [suspendedStage])).toBe('attention.lede');
  });
});
