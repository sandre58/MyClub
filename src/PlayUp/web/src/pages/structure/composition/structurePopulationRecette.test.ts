import { describe, expect, it } from 'vitest';

import runningRootU1Satisfied from '../__fixtures__/populationRecette/running-root-u1-satisfied.json';
import runningRootU1Deficit from '../__fixtures__/populationRecette/running-root-u1-deficit-full-composition.json';
import readyRootSevenSeats from '../__fixtures__/populationRecette/ready-root-seven-affectation-seats.json';
import { structureDiagnosticTitleKey } from '../issues/structureDiagnosticPresentation';
import { resolveStructureDiagnosticIssueTitleKey } from '../issues/structurePausedDiagnosticCopy';
import { expectedPopulationOccupancy } from './structurePopulationVolume';
import {
  stageHasPopulationShortfall,
  stagePopulationGap,
} from '../issues/structureReadinessSummary';
import { stagePresentedIssueCodes } from '../issues/structureStagePresentedIssues';
import type { StructureStageHubSummary, StructureView } from '../../../types';
import { structureReadiness } from '../../../test/structureTestFixtures';

type PopulationRecetteObservation = {
  stageStatus: string;
  affectationEntryCount: number;
  compositionEntryCount: number;
  compositionCapacity: number;
  compositionIneligibleCount: number;
  expectedPopulationOccupancy: number;
  inboundConfiguredVolume: number;
  placesN: number;
  u1PopulationGap: number;
  participantsActiveCount: number;
  participantsOccupyingCount: number;
  withdrawnEntryCount: number;
  isRosterComplete: boolean;
  structureIssues: string[];
  presentedIssueCodes: string[];
  presentedOnlyIssueCodes: string[];
  materializeDisabledReason: string | null;
  lifecycleTransitionDisabledReasonCode: string | null;
};

function hubFromObservation(
  obs: PopulationRecetteObservation,
): StructureStageHubSummary {
  return {
    stageId: 'recette-stage',
    name: 'Recette',
    status: obs.stageStatus as StructureStageHubSummary['status'],
    teamCount: obs.participantsOccupyingCount,
    matchCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Championship',
    affectationEntryCount: obs.affectationEntryCount,
    compositionEntryCount: obs.compositionEntryCount,
    compositionCapacity: obs.compositionCapacity,
    compositionIneligibleCount: obs.compositionIneligibleCount,
    isRootComposition: true,
    structureIssues: obs.structureIssues,
    presentedIssueCodes: obs.presentedIssueCodes,
  };
}

function viewFromObservation(obs: PopulationRecetteObservation): StructureView {
  const hub = hubFromObservation(obs);
  return {
    competitionId: 'recette-competition',
    name: 'Recette competition',
    status: obs.stageStatus === 'Running' ? 'Running' : 'Ready',
    format: {
      kind: 'Championship',
      primaryStageId: hub.stageId,
      primaryStageName: hub.name,
      primaryStageStatus: hub.status,
    },
    regulation: {
      minimumTeams: 2,
      maximumTeams: 64,
      durationPerPeriod: 45,
      numberOfPeriods: 2,
      winPoints: 3,
      drawPoints: 1,
      lossPoints: 0,
    },
    structure: {
      groupCount: 0,
      roundCount: 0,
      matchdayCount: 0,
      slotCount: 0,
      hasDrawRules: false,
      numberOfPots: null,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    actions: [],
    readiness: structureReadiness(),
    participants: {
      activeCount: obs.participantsActiveCount,
      occupyingCount: obs.participantsOccupyingCount,
      entries: [],
    },
    stages: [hub],
  };
}

function titleKeysForCodes(
  codes: readonly string[],
  stageStatus: StructureStageHubSummary['status'],
  hub: StructureStageHubSummary,
): string[] {
  return codes.map((code) =>
    resolveStructureDiagnosticIssueTitleKey(code, stageStatus, {
      compositionEntryCount: hub.compositionEntryCount,
      compositionCapacity: hub.compositionCapacity,
    }),
  );
}

function assertU1Counters(
  obs: PopulationRecetteObservation,
  data: StructureView,
  hub: StructureStageHubSummary,
) {
  expect(expectedPopulationOccupancy(data, hub)).toBe(
    obs.expectedPopulationOccupancy,
  );
  expect(obs.u1PopulationGap).toBe(
    obs.expectedPopulationOccupancy - obs.placesN,
  );
  expect(stagePopulationGap(data, hub)).toBe(obs.u1PopulationGap);
  expect(stageHasPopulationShortfall(data, hub)).toBe(obs.u1PopulationGap < 0);
}

function assertDiagnosticLayers(
  obs: PopulationRecetteObservation,
  data: StructureView,
  hub: StructureStageHubSummary,
) {
  assertU1Counters(obs, data, hub);

  expect(hub.structureIssues).toEqual(obs.structureIssues);
  expect(stagePresentedIssueCodes(data, hub)).toEqual(obs.presentedIssueCodes);

  const rawSet = new Set(obs.structureIssues);
  const presentedOnly = obs.presentedIssueCodes.filter(
    (code) => !rawSet.has(code),
  );
  expect(presentedOnly).toEqual(obs.presentedOnlyIssueCodes);

  const stageStatus = obs.stageStatus as StructureStageHubSummary['status'];
  expect(titleKeysForCodes(obs.structureIssues, stageStatus, hub)).toEqual(
    obs.structureIssues.map((code) => structureDiagnosticTitleKey(code)),
  );
  const expectedPresentedTitleKeys = obs.presentedIssueCodes.map((code) =>
    resolveStructureDiagnosticIssueTitleKey(code, stageStatus, {
      compositionEntryCount: hub.compositionEntryCount,
      compositionCapacity: hub.compositionCapacity,
    }),
  );
  expect(titleKeysForCodes(obs.presentedIssueCodes, stageStatus, hub)).toEqual(
    expectedPresentedTitleKeys,
  );
}

describe('population recette fixtures (B/C observation)', () => {
  it('running root U1 satisfied: occupation vs eligibility witness', () => {
    const obs = runningRootU1Satisfied as PopulationRecetteObservation;
    const hub = hubFromObservation(obs);
    const data = viewFromObservation(obs);

    assertDiagnosticLayers(obs, data, hub);

    expect(obs.participantsOccupyingCount).toBe(obs.placesN);
    expect(obs.participantsActiveCount).toBe(7);
    expect(obs.withdrawnEntryCount).toBe(1);
    expect(obs.compositionIneligibleCount).toBe(1);
    expect(obs.isRosterComplete).toBe(false);

    expect(obs.structureIssues).not.toContain('PopulationShortfall');
    expect(obs.presentedIssueCodes).not.toContain('PopulationShortfall');
    expect(obs.presentedOnlyIssueCodes).toEqual([]);
    expect(stageHasPopulationShortfall(data, hub)).toBe(false);
  });

  it('ready root: raw SlotFeedMissing vs presented synthesis (U1 + roster)', () => {
    const obs = readyRootSevenSeats as PopulationRecetteObservation;
    const hub = hubFromObservation(obs);
    const data = viewFromObservation(obs);

    assertDiagnosticLayers(obs, data, hub);

    expect(obs.structureIssues).toContain('SlotFeedMissing');
    expect(obs.structureIssues).not.toContain('PopulationShortfall');
    expect(obs.presentedIssueCodes).toContain('PopulationShortfall');
    expect(obs.presentedIssueCodes).toContain('PhaseRosterIncomplete');
    expect(obs.presentedOnlyIssueCodes).toContain('PopulationShortfall');
    expect(obs.isRosterComplete).toBe(false);
    expect(obs.materializeDisabledReason).toBe('PhaseRosterIncomplete');
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'PhaseRosterIncomplete',
        'Ready',
        {
          compositionEntryCount: hub.compositionEntryCount,
          compositionCapacity: hub.compositionCapacity,
        },
      ),
    ).toBe('graph.issues.PhaseRosterIncomplete');
    expect(
      resolveStructureDiagnosticIssueTitleKey('PopulationShortfall', 'Ready', {
        compositionEntryCount: hub.compositionEntryCount,
        compositionCapacity: hub.compositionCapacity,
      }),
    ).toBe('graph.issues.PopulationShortfall');
  });

  it('running root U1 deficit: PopulationShortfall only in presentation layer', () => {
    const obs = runningRootU1Deficit as PopulationRecetteObservation;
    const hub = hubFromObservation(obs);
    const data = viewFromObservation(obs);

    assertDiagnosticLayers(obs, data, hub);

    expect(obs.compositionEntryCount).toBe(obs.compositionCapacity);
    expect(obs.structureIssues).not.toContain('PopulationShortfall');
    expect(obs.presentedIssueCodes).toEqual(['PopulationShortfall']);
    expect(obs.presentedOnlyIssueCodes).toEqual(['PopulationShortfall']);
    expect(stageHasPopulationShortfall(data, hub)).toBe(true);
    expect(
      resolveStructureDiagnosticIssueTitleKey(
        'PopulationShortfall',
        'Running',
        {
          compositionEntryCount: hub.compositionEntryCount,
          compositionCapacity: hub.compositionCapacity,
        },
      ),
    ).toBe('graph.issues.PopulationShortfallFullComposition');
  });
});
