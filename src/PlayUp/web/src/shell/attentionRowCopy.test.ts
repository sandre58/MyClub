import { describe, expect, it } from 'vitest';
import {
  attentionSituationMeta,
  attentionSituationSubtitle,
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

  it('resolves stage name from destinationStageId on Slot attention rows', () => {
    const item: OverviewSituation = {
      source: 'QualificationConflict',
      nature: 'Blocking',
      targetType: 'Slot',
      targetId: 'stage-z:slot-a',
      matchId: null,
      actionable: true,
      actionCode: 'ApplyQualification',
      impactCode: 'BlocksProgression',
      params: { slotKey: 'slot-a', destinationStageId: 'stage-z' },
    };
    const meta = attentionSituationMeta(item, [
      { stageId: 'stage-z', name: 'Phase finale', status: 'Running' },
    ]);
    expect(meta).toBe('Phase finale');
  });

  it('adds a business subtitle for QualificationConflict', () => {
    const subtitle = attentionSituationSubtitle({
      source: 'QualificationConflict',
      nature: 'Blocking',
      targetType: 'Slot',
      targetId: 's:1',
      matchId: null,
      actionable: true,
      actionCode: 'ApplyQualification',
      impactCode: 'BlocksProgression',
      params: {},
    });
    expect(subtitle).not.toBeNull();
    expect(subtitle!.length).toBeGreaterThan(20);
  });
});
