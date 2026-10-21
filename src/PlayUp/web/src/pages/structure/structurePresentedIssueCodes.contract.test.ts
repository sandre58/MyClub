import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary, StructureView } from '../../types';
import { stagePresentedIssueCodes } from './structureStagePresentedIssues';
import { stageWorstPresentedDiagnosticTone } from './structureStageTopologyPresentation';

/**
 * Contract: when Host sends `presentedIssueCodes`, SPA must not re-derive lifecycle/readiness.
 */
describe('presentedIssueCodes contract (Championship Ready)', () => {
  const hub: StructureStageHubSummary = {
    stageId: 'ch-1',
    name: 'Championnat',
    status: 'Ready',
    formatKind: 'Championship',
    teamCount: 4,
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
    affectationEntryCount: 4,
    compositionCapacity: 4,
    structureIssues: [],
    presentedIssueCodes: ['UnmaterializedConfrontations'],
    materialize: {
      commandCode: 'MaterializeMatches',
      labelVariant: 'create',
      determinedCount: 6,
      eligibleCount: 6,
      materializedCount: 0,
      commandEnabled: true,
      confrontations: [],
    },
    lifecycle: {
      transition: {
        code: 'StartStage',
        enabled: false,
        disabledReasonCode: 'UnmaterializedConfrontations',
      },
    },
  };

  const data = {
    competitionId: 'c1',
    status: 'Ready',
    format: { kind: 'Championship' },
    readiness: { blockers: [], blockerDetails: [] },
    stages: [hub],
  } as unknown as StructureView;

  it('consumes Host presentedIssueCodes even when lifecycle would imply the same code', () => {
    expect(stagePresentedIssueCodes(data, hub)).toEqual([
      'UnmaterializedConfrontations',
    ]);
  });

  it('topology danger follows presentedIssueCodes from API', () => {
    expect(stageWorstPresentedDiagnosticTone(data, hub)).toBe('danger');
  });

  it('treats omitted presentedIssueCodes as empty (no SPA inference)', () => {
    const { presentedIssueCodes: _omit, ...withoutField } = hub;
    expect(
      stagePresentedIssueCodes(data, withoutField as StructureStageHubSummary),
    ).toEqual([]);
  });

  it('does not infer Unmaterialized from lifecycle when presentedIssueCodes is empty', () => {
    const withoutHost = {
      ...hub,
      presentedIssueCodes: [],
    };
    expect(stagePresentedIssueCodes(data, withoutHost)).toEqual([]);
    expect(stageWorstPresentedDiagnosticTone(data, withoutHost)).toBeNull();
  });
});
