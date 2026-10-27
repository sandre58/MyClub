import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../../../types';
import { inboundFeeds } from './structurePhaseFeeds';
import {
  expectedPopulationWithQualDraft,
  expectedPopulationWithProgDraft,
  expectedPopulationOccupancy,
  inboundPopulationConfiguredVolume,
  qualificationPathVolume,
} from './structurePopulationVolume';

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
    matchdayCount: 0,
    slotCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: false,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    compositionEntryCount: 0,
    affectationEntryCount: 0,
    compositionCapacity: 2,
    ...partial,
  };
}

function view(stages: StructureStageHubSummary[]): StructureView {
  return { stages } as StructureView;
}

describe('structurePopulationVolume', () => {
  it('counts Range qualification volume', () => {
    expect(
      qualificationPathVolume({
        order: 1,
        rankingScope: 'Group',
        selectionMode: 'Range',
        selectionValue: 1,
        selectionEndValue: 2,
        destinationStageId: 'final',
      }),
    ).toBe(2);
  });

  it('inbound volume counts Prog Population and Prog Place', () => {
    const data = view([
      stage({
        stageId: 'sf',
        name: 'Demis',
        progressionIntents: [
          {
            intentId: 'p1',
            order: 1,
            roundId: 'r1',
            outcome: 'Winner',
            destinationStageId: 'final',
            expandedPathCount: 1,
          },
          {
            intentId: 'p2',
            order: 2,
            roundId: 'r1',
            outcome: 'Winner',
            destinationStageId: 'final',
            expandedPathCount: 1,
          },
          {
            intentId: 'p3',
            order: 3,
            roundId: 'r1',
            outcome: 'Loser',
            destinationStageId: 'third',
            expandedPathCount: 1,
          },
        ],
      }),
      stage({ stageId: 'final', name: 'Finale', compositionCapacity: 2 }),
    ]);
    expect(inboundPopulationConfiguredVolume(data, 'final')).toBe(2);
  });

  it('expectedPopulationOccupancy sums affectation and inbound feeds', () => {
    const final = stage({
      stageId: 'final',
      name: 'Finale',
      compositionCapacity: 4,
      affectationEntryCount: 2,
    });
    const data = view([
      stage({
        stageId: 'sf',
        name: 'Demis',
        qualificationIntents: [
          {
            intentId: 'q1',
            order: 1,
            sourceKind: 'EachGroup',
            positionFrom: 1,
            positionTo: 2,
            destinationStageId: 'final',
            destinationCount: 2,
          },
        ],
      }),
      final,
    ]);
    expect(expectedPopulationOccupancy(data, final)).toBe(4);
  });

  it('inboundPopulationConfiguredVolume matches sum of inboundFeeds row volumes (U1 intent projection)', () => {
    const final = stage({
      stageId: 'final',
      name: 'Finale',
      compositionCapacity: 4,
      affectationEntryCount: 2,
    });
    const data = view([
      stage({
        stageId: 'sf',
        name: 'Demis',
        qualificationIntents: [
          {
            intentId: 'q1',
            order: 1,
            sourceKind: 'EachGroup',
            positionFrom: 1,
            positionTo: 2,
            destinationStageId: 'final',
            destinationCount: 2,
          },
        ],
      }),
      final,
    ]);
    const feeds = inboundFeeds(data, 'final', 'fr', (key) => key);
    const feedVolume = feeds.reduce((sum, row) => sum + row.volume, 0);
    expect(inboundPopulationConfiguredVolume(data, 'final')).toBe(feedVolume);
    expect(expectedPopulationOccupancy(data, final)).toBe(
      (final.affectationEntryCount ?? 0) + feedVolume,
    );
  });

  it('adds Prog Place from same source to Qual draft Z (4/2 case)', () => {
    const demis = stage({
      stageId: 'sf',
      name: 'Demis',
      progressionIntents: [
        {
          intentId: 'p1',
          order: 1,
          roundId: 'r1',
          outcome: 'Winner',
          destinationStageId: 'final',
          expandedPathCount: 1,
        },
        {
          intentId: 'p2',
          order: 2,
          roundId: 'r1',
          outcome: 'Winner',
          destinationStageId: 'final',
          expandedPathCount: 1,
        },
      ],
    });
    const finale = stage({
      stageId: 'final',
      name: 'Finale',
      compositionCapacity: 2,
      compositionEntryCount: 0,
      affectationEntryCount: 0,
    });
    const data = view([demis, finale]);

    expect(
      expectedPopulationWithQualDraft({
        data,
        destination: finale,
        sourceStageId: demis.stageId,
        draftQualVolume: 2,
      }),
    ).toBe(4);
  });

  it('substitutes current-source Qual with draft Z', () => {
    const groups = stage({
      stageId: 'groups',
      name: 'Groupes',
      qualificationIntents: [
        {
          intentId: 'q1',
          order: 1,
          sourceKind: 'SingleGroup',
          positionFrom: 1,
          positionTo: 1,
          destinationStageId: 'final',
          groupId: 'g1',
          destinationCount: 1,
        },
        {
          intentId: 'q2',
          order: 2,
          sourceKind: 'SingleGroup',
          positionFrom: 1,
          positionTo: 1,
          destinationStageId: 'final',
          groupId: 'g2',
          destinationCount: 1,
        },
      ],
    });
    const demis = stage({
      stageId: 'sf',
      name: 'Demis',
      progressionIntents: [
        {
          intentId: 'p1',
          order: 1,
          roundId: 'r1',
          outcome: 'Winner',
          destinationStageId: 'final',
          expandedPathCount: 1,
        },
        {
          intentId: 'p2',
          order: 2,
          roundId: 'r1',
          outcome: 'Winner',
          destinationStageId: 'final',
          expandedPathCount: 1,
        },
      ],
    });
    const finale = stage({
      stageId: 'final',
      name: 'Finale',
      compositionCapacity: 2,
      compositionEntryCount: 0,
      affectationEntryCount: 0,
    });
    const data = view([groups, demis, finale]);

    // Persisted Groups Qual (2) excluded; Demis Prog (2); draft Z=0 → 2
    expect(
      expectedPopulationWithQualDraft({
        data,
        destination: finale,
        sourceStageId: groups.stageId,
        draftQualVolume: 0,
      }),
    ).toBe(2);

    // Draft Qual +1 on top of Demis → 3
    expect(
      expectedPopulationWithQualDraft({
        data,
        destination: finale,
        sourceStageId: groups.stageId,
        draftQualVolume: 1,
      }),
    ).toBe(3);
  });

  it('expectedPopulationWithProgDraft substitutes Prog from authoring source', () => {
    const groups = stage({
      stageId: 'groups',
      name: 'Groupes',
      progressionIntents: [
        {
          intentId: 'p1',
          order: 1,
          roundId: 'r1',
          outcome: 'Winner',
          destinationStageId: 'demis',
          expandedPathCount: 1,
        },
        {
          intentId: 'p2',
          order: 2,
          roundId: 'r1',
          outcome: 'Winner',
          destinationStageId: 'demis',
          expandedPathCount: 1,
        },
      ],
    });
    const demis = stage({
      stageId: 'demis',
      name: 'Demis',
      compositionCapacity: 4,
      compositionEntryCount: 0,
      affectationEntryCount: 0,
    });
    const data = view([groups, demis]);

    expect(
      expectedPopulationWithProgDraft({
        data,
        destination: demis,
        sourceStageId: 'other',
        draftProgVolume: 0,
      }),
    ).toBe(2);

    expect(
      expectedPopulationWithProgDraft({
        data,
        destination: demis,
        sourceStageId: groups.stageId,
        draftProgVolume: 3,
      }),
    ).toBe(3);
  });

  it('uses qualificationIntents destinationCount for inbound volume', () => {
    const data = view([
      stage({
        stageId: 'groups',
        name: 'Groupes',
        qualificationIntents: [
          {
            intentId: 'q1',
            order: 1,
            sourceKind: 'Overall',
            positionFrom: 1,
            positionTo: 1,
            destinationStageId: 'final',
            destinationCount: 3,
          },
        ],
      }),
      stage({ stageId: 'final', name: 'Finale', compositionCapacity: 8 }),
    ]);
    expect(inboundPopulationConfiguredVolume(data, 'final')).toBe(3);
  });

  it('uses progressionIntents expandedPathCount for inbound volume', () => {
    const data = view([
      stage({
        stageId: 'cup',
        name: 'Cup',
        progressionIntents: [
          {
            intentId: 'p1',
            order: 1,
            roundId: 'r1',
            outcome: 'Winner',
            destinationStageId: 'final',
            expandedPathCount: 4,
          },
        ],
      }),
      stage({ stageId: 'final', name: 'Finale', compositionCapacity: 8 }),
    ]);
    expect(inboundPopulationConfiguredVolume(data, 'final')).toBe(4);
  });
});
