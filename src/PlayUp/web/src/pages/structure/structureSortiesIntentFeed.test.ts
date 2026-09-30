import { describe, expect, it } from 'vitest';
import type {
  StructureProgressionIntent,
  StructureQualificationIntent,
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import {
  outboundSortiesFeeds,
  progressionIntentRow,
  qualificationIntentRow,
  stageHasOutboundIntents,
} from './structureSortiesIntentFeed';

const t = (key: string, opts?: Record<string, unknown>) => {
  if (key === 'qualification.summary.selectionPair') {
    return `${opts?.from} et ${opts?.to}`;
  }
  if (key === 'qualification.summary.selectionRange') {
    return `${opts?.from}–${opts?.to}`;
  }
  if (key === 'qualification.summary.scopeEachGroup') {
    return 'Chaque groupe';
  }
  if (key === 'qualification.summary.scopeAcrossPlace') {
    return `Meilleurs ${opts?.place}`;
  }
  if (key === 'fiche.rule.winner') {
    return (opts?.count as number) > 1 ? 'Vainqueurs' : 'Vainqueur';
  }
  if (key === 'fiche.rule.loser') {
    return (opts?.count as number) > 1 ? 'Perdants' : 'Perdant';
  }
  if (key === 'progression.roundFallback') return `Tour ${opts?.id}`;
  return key;
};

function viewWithStages(stages: StructureStageHubSummary[]): StructureView {
  return {
    competitionId: 'c1',
    stages,
  } as StructureView;
}

describe('structureSortiesIntentFeed', () => {
  it('splits AcrossGroups range into rank chip + place label', () => {
    const intent: StructureQualificationIntent = {
      intentId: 'q-across',
      order: 1,
      sourceKind: 'AcrossGroups',
      positionFrom: 1,
      positionTo: 4,
      acrossGroupsPosition: 3,
      destinationStageId: 'demi',
      destinationCount: 4,
    };
    const data = viewWithStages([
      { stageId: 'groups', name: 'Poules' } as StructureStageHubSummary,
      { stageId: 'demi', name: 'Demi-finales' } as StructureStageHubSummary,
    ]);
    const row = qualificationIntentRow(data, intent, 'fr', t);
    expect(row.badge).toBe('1er–4e');
    expect(row.context).toBe('Meilleurs 3e');
  });

  it('splits EachGroup 1–2 into selection chip + scope context', () => {
    const intent: StructureQualificationIntent = {
      intentId: 'q1',
      order: 1,
      sourceKind: 'EachGroup',
      positionFrom: 1,
      positionTo: 2,
      destinationStageId: 'final',
      destinationCount: 4,
    };
    const data = viewWithStages([
      { stageId: 'groups', name: 'Poules' } as StructureStageHubSummary,
      { stageId: 'final', name: 'Finale' } as StructureStageHubSummary,
    ]);
    const row = qualificationIntentRow(data, intent, 'fr', t);
    expect(row.badge).toBe('1er et 2e');
    expect(row.context).toBe('Chaque groupe');
    expect(row.peerName).toBe('Finale');
    expect(row.volume).toBe(4);
  });

  it('pluralizes winner chip when expand volume > 1', () => {
    const intent: StructureProgressionIntent = {
      intentId: 'p1',
      order: 1,
      roundId: 'r1',
      roundName: 'Demi-finales',
      outcome: 'Winner',
      destinationStageId: 'sf',
      expandedPathCount: 2,
    };
    const source = {
      stageId: 'demi',
      name: 'Demi-finales',
    } as StructureStageHubSummary;
    const data = viewWithStages([
      source,
      { stageId: 'sf', name: 'Finale' } as StructureStageHubSummary,
    ]);
    const row = progressionIntentRow(data, source, intent, t);
    expect(row.badge).toBe('Vainqueurs');
    expect(row.volume).toBe(2);
  });

  it('hides progression round when it matches the source phase name', () => {
    const intent: StructureProgressionIntent = {
      intentId: 'p1',
      order: 1,
      roundId: 'r1',
      roundName: 'Demi-finales',
      outcome: 'Winner',
      destinationStageId: 'sf',
      expandedPathCount: 1,
    };
    const source = {
      stageId: 'demi',
      name: 'Demi-finales',
    } as StructureStageHubSummary;
    const data = viewWithStages([
      source,
      { stageId: 'sf', name: 'Finale' } as StructureStageHubSummary,
    ]);
    const row = progressionIntentRow(data, source, intent, t);
    expect(row.badge).toBe('Vainqueur');
    expect(row.context).toBe('');
  });

  it('keeps progression round when it differs from the source phase', () => {
    const intent: StructureProgressionIntent = {
      intentId: 'p1',
      order: 1,
      roundId: 'r1',
      roundName: 'Quarts',
      outcome: 'Loser',
      destinationStageId: 'rep',
      expandedPathCount: 4,
    };
    const source = {
      stageId: 'cup',
      name: 'Coupe',
    } as StructureStageHubSummary;
    const data = viewWithStages([
      source,
      { stageId: 'rep', name: 'Repêchage' } as StructureStageHubSummary,
    ]);
    const row = progressionIntentRow(data, source, intent, t);
    expect(row.badge).toBe('Perdants');
    expect(row.context).toBe('Quarts');
  });

  it('builds outbound feeds from qual + prog intents', () => {
    const qual: StructureQualificationIntent = {
      intentId: 'q1',
      order: 1,
      sourceKind: 'EachGroup',
      positionFrom: 1,
      positionTo: 1,
      destinationStageId: 'ko',
      destinationCount: 4,
    };
    const prog: StructureProgressionIntent = {
      intentId: 'p1',
      order: 1,
      roundId: 'r1',
      roundName: 'Quarts',
      outcome: 'Winner',
      destinationStageId: 'sf',
      expandedPathCount: 4,
    };
    const stage = {
      stageId: 'groups',
      name: 'Poules',
      qualificationIntents: [qual],
      progressionIntents: [prog],
    } as StructureStageHubSummary;
    const data = viewWithStages([
      stage,
      { stageId: 'ko', name: 'KO' } as StructureStageHubSummary,
      { stageId: 'sf', name: 'Demi' } as StructureStageHubSummary,
    ]);
    expect(stageHasOutboundIntents(stage)).toBe(true);
    const feeds = outboundSortiesFeeds(data, stage, 'fr', t, []);
    expect(feeds).toHaveLength(2);
    expect(feeds[0]!.badge).toBe('1er');
    expect(feeds[0]!.context).toBe('Chaque groupe');
    expect(feeds[1]!.badge).toBe('Vainqueurs');
    expect(feeds[1]!.context).toBe('Quarts');
  });
});
