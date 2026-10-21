import { describe, expect, it } from 'vitest';
import {
  attentionSituationMeta,
  attentionSituationTitle,
} from './attentionRowCopy';
import type { OverviewSituation } from '../types';

describe('attentionRowCopy', () => {
  it('uses structure namespace for graph issue titles', () => {
    const title = attentionSituationTitle(
      {
        tone: 'attention',
        titleKey: 'graph.issues.SlotFeedMissing',
        titleNs: 'structure',
        sotHref: '/x',
        cta: null,
        canal: 'structure-topology',
      },
      'SlotFeedMissing',
    );
    expect(title).not.toBe('SlotFeedMissing');
    expect(title.length).toBeGreaterThan(3);
  });

  it('shows stage name instead of generic Phase label', () => {
    const item: OverviewSituation = {
      source: 'CycleDetected',
      nature: 'Blocking',
      targetType: 'Stage',
      targetId: 'stage-a',
      matchId: null,
      actionable: false,
      actionCode: null,
      impactCode: null,
      params: { stageId: 'stage-a' },
    };
    const meta = attentionSituationMeta(item, [
      { stageId: 'stage-a', name: 'Poules', status: 'Draft' },
    ]);
    expect(meta).toBe('Poules');
  });
});
