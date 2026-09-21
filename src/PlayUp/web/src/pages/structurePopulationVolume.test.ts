import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../types';
import {
  expectedPopulationWithQualDraft,
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
        progressionPaths: [
          {
            sourceFixtureId: 'f1',
            outcome: 'Winner',
            destinationStageId: 'final',
            destinationSlotKey: null,
          },
          {
            sourceFixtureId: 'f2',
            outcome: 'Winner',
            destinationStageId: 'final',
            destinationSlotKey: 'F-A',
          },
          {
            sourceFixtureId: 'f3',
            outcome: 'Loser',
            destinationStageId: 'third',
            destinationSlotKey: 'B-A',
          },
        ],
      }),
      stage({ stageId: 'final', name: 'Finale', compositionCapacity: 2 }),
    ]);
    expect(inboundPopulationConfiguredVolume(data, 'final')).toBe(2);
  });

  it('adds Prog Place from same source to Qual draft Z (4/2 case)', () => {
    const demis = stage({
      stageId: 'sf',
      name: 'Demis',
      progressionPaths: [
        {
          sourceFixtureId: 'f1',
          outcome: 'Winner',
          destinationStageId: 'final',
          destinationSlotKey: 'F-A',
        },
        {
          sourceFixtureId: 'f2',
          outcome: 'Winner',
          destinationStageId: 'final',
          destinationSlotKey: 'F-B',
        },
      ],
    });
    const finale = stage({
      stageId: 'final',
      name: 'Finale',
      compositionCapacity: 2,
      compositionEntryCount: 0,
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
      qualificationPaths: [
        {
          order: 1,
          rankingScope: 'Group',
          selectionMode: 'Position',
          selectionValue: 1,
          destinationStageId: 'final',
          groupId: 'g1',
        },
        {
          order: 2,
          rankingScope: 'Group',
          selectionMode: 'Position',
          selectionValue: 1,
          destinationStageId: 'final',
          groupId: 'g2',
        },
      ],
    });
    const demis = stage({
      stageId: 'sf',
      name: 'Demis',
      progressionPaths: [
        {
          sourceFixtureId: 'f1',
          outcome: 'Winner',
          destinationStageId: 'final',
          destinationSlotKey: null,
        },
        {
          sourceFixtureId: 'f2',
          outcome: 'Winner',
          destinationStageId: 'final',
          destinationSlotKey: null,
        },
      ],
    });
    const finale = stage({
      stageId: 'final',
      name: 'Finale',
      compositionCapacity: 2,
      compositionEntryCount: 0,
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
});
