import { describe, expect, it } from 'vitest';
import {
  occupancyMeterRatio,
  occupancyMeterState,
} from './OccupancyMeter';
import { expandOccurrences } from './structureQualificationDraft';
import { expectedPopulationWithQualDraft } from './structurePopulationVolume';
import type {
  StructureStageHubSummary,
  StructureView,
} from '../../types';

describe('OccupancyMeter', () => {
  it('maps current vs capacity to exact | short | over', () => {
    expect(occupancyMeterState(4, 4)).toBe('exact');
    expect(occupancyMeterState(2, 4)).toBe('short');
    expect(occupancyMeterState(6, 4)).toBe('over');
    expect(occupancyMeterRatio(6, 4)).toBe(1);
    expect(occupancyMeterRatio(2, 4)).toBe(0.5);
  });
});

/**
 * OverExtraction seed meter inputs (investigation — do not “fix” DestinationDraftMeter yet).
 * Source Poules 2×2 → Places N = 4; EachGroup 1–3 → 6 outbound.
 * Destination Championship Hub compositionCapacity today = CountActiveEntries (roster), not form Places N.
 */
describe('OverExtraction DestinationDraftMeter Hub inputs', () => {
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
      formatKind: 'Championship',
      compositionCapacity: null,
      affectationEntryCount: 0,
      ...partial,
    };
  }

  it('draft EachGroup 1–3 × 2 groups expands to 6 (outbound volume)', () => {
    const occurrences = expandOccurrences(
      {
        id: 'q1',
        sourceKind: 'EachGroup',
        groupId: '',
        groupName: '',
        positionFrom: '1',
        positionTo: '3',
        acrossGroupsPosition: '1',
        conditionKind: 'none',
        minimumPoints: '',
        targetKind: 'population',
        destinationStageId: 'next',
        destinationSlotKeys: [],
        destinationGroupIds: [],
        destinationForm: false,
      },
      [
        { id: 'g1', name: 'A' },
        { id: 'g2', name: 'B' },
      ],
    );
    expect(occurrences).toHaveLength(6);
  });

  it('with Championship Y = roster (4), hydrated draft yields X=6 → meter 6/4 not 0/6', () => {
    const source = stage({
      stageId: 'poules',
      name: 'Poules',
      formatKind: 'Groups',
      groupCount: 2,
      placesPerGroup: 2,
      compositionCapacity: 4,
      qualificationPaths: [
        {
          order: 1,
          rankingScope: 'Group',
          selectionMode: 'Range',
          selectionValue: 1,
          selectionEndValue: 3,
          destinationStageId: 'next',
          groupId: 'g1',
        },
        {
          order: 2,
          rankingScope: 'Group',
          selectionMode: 'Range',
          selectionValue: 1,
          selectionEndValue: 3,
          destinationStageId: 'next',
          groupId: 'g2',
        },
      ],
    });
    const destination = stage({
      stageId: 'next',
      name: 'Phase suivante',
      formatKind: 'Championship',
      // Hub ResolvePlaces(Championship) = CountActiveEntries → 4 for this seed.
      compositionCapacity: 4,
      affectationEntryCount: 0,
    });
    const data = {
      stages: [source, destination],
    } as StructureView;

    const expandN = 6;
    const x = expectedPopulationWithQualDraft({
      data,
      destination,
      sourceStageId: source.stageId,
      draftQualVolume: expandN,
    });
    const y = destination.compositionCapacity;

    expect(x).toBe(6);
    expect(y).toBe(4);
    // Observed UI "0/6" is NOT this hydrated contract — investigate expandOccurrences=0
    // (schematic groups empty) or a wrong Y source before changing the meter.
  });

  it('expandOccurrences returns 0 without groups → X=0 (candidate for observed 0/Y)', () => {
    expect(
      expandOccurrences(
        {
          id: 'q1',
          sourceKind: 'EachGroup',
          groupId: '',
          groupName: '',
          positionFrom: '1',
          positionTo: '3',
          acrossGroupsPosition: '1',
          conditionKind: 'none',
          minimumPoints: '',
          targetKind: 'population',
          destinationStageId: 'next',
          destinationSlotKeys: [],
          destinationGroupIds: [],
          destinationForm: false,
        },
        [],
      ),
    ).toHaveLength(0);

    const destination = stage({
      stageId: 'next',
      name: 'Phase suivante',
      compositionCapacity: 4,
    });
    const x = expectedPopulationWithQualDraft({
      data: { stages: [destination] } as StructureView,
      destination,
      sourceStageId: 'poules',
      draftQualVolume: 0,
    });
    expect(x).toBe(0);
  });
});
