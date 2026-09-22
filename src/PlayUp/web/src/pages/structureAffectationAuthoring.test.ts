import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../types';
import {
  expectedPopulationWithProgDraft,
  expectedPopulationWithQualDraft,
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

describe('S1 AffectationAuthoring vs CompositionEntries', () => {
  it('ignores runtime CompositionEntries for Qual expected population', () => {
    const source = stage({
      stageId: 'r32',
      name: '32es',
      progressionPaths: Array.from({ length: 16 }, (_, i) => ({
        sourceFixtureId: `f${i}`,
        outcome: 'Winner' as const,
        destinationStageId: 'r16',
      })),
    });
    const destination = stage({
      stageId: 'r16',
      name: '16es',
      compositionCapacity: 16,
      compositionEntryCount: 16,
      affectationEntryCount: 0,
    });
    const data = view([source, destination]);

    expect(
      expectedPopulationWithQualDraft({
        data,
        destination,
        sourceStageId: 'other',
        draftQualVolume: 0,
      }),
    ).toBe(16);
  });

  it('counts Affectation authoring in Prog expected population', () => {
    const source = stage({ stageId: 'r32', name: '32es' });
    const destination = stage({
      stageId: 'r16',
      name: '16es',
      compositionCapacity: 12,
      compositionEntryCount: 12,
      affectationEntryCount: 4,
    });
    const data = view([source, destination]);

    expect(
      expectedPopulationWithProgDraft({
        data,
        destination,
        sourceStageId: source.stageId,
        draftProgVolume: 8,
      }),
    ).toBe(12);
  });
});
