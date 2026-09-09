import { describe, expect, it } from 'vitest';
import {
  buildRegulationImpactPreview,
  detectChangedHeritableParts,
  isStagePersonalized,
} from './regulationImpact';
import type {
  OrganisationRegulationSummary,
  OrganisationStageHubSummary,
  ReplaceRegulationRequest,
} from '../types';

const seed: OrganisationRegulationSummary = {
  minimumTeams: 8,
  maximumTeams: 16,
  durationPerPeriod: 45,
  numberOfPeriods: 2,
  halfTimeDuration: 15,
  winPoints: 3,
  drawPoints: 1,
  lossPoints: 0,
  rankingCriteria: ['Points', 'GoalDifference', 'GoalsFor', 'HeadToHead'],
  hasExtraTime: false,
  hasPenaltyShootout: false,
  forfeitWinnerGoals: 3,
  forfeitLoserGoals: 0,
  allowedTypes: ['Yellow', 'Red'],
};

function form(
  overrides: Partial<ReplaceRegulationRequest> = {},
): ReplaceRegulationRequest {
  return {
    minimumTeams: 8,
    maximumTeams: 16,
    durationPerPeriod: 45,
    numberOfPeriods: 2,
    halfTimeDuration: 15,
    winPoints: 3,
    drawPoints: 1,
    lossPoints: 0,
    rankingCriteria: ['Points', 'GoalDifference', 'GoalsFor', 'HeadToHead'],
    hasExtraTime: false,
    hasPenaltyShootout: false,
    forfeitWinnerGoals: 3,
    forfeitLoserGoals: 0,
    allowedTypes: ['Yellow', 'Red'],
    ...overrides,
  };
}

function boundStage(
  id: string,
  overrides: Partial<OrganisationStageHubSummary> = {},
): OrganisationStageHubSummary {
  return {
    stageId: id,
    name: id,
    status: 'Draft',
    teamCount: 8,
    matchCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: true,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    defaultsBinding: {
      matchDuration: { isBound: true },
      extraTime: { isBound: true },
      penaltyShootout: { isBound: true },
      administrativeResult: { isBound: true },
      points: { isBound: true },
      rankingCriteria: { isBound: true },
    },
    ...overrides,
  };
}

describe('regulationImpact', () => {
  it('treats reordered ranking criteria as a canonical change', () => {
    const changed = detectChangedHeritableParts(
      form({
        rankingCriteria: ['Points', 'GoalsFor', 'GoalDifference', 'HeadToHead'],
      }),
      seed,
    );
    expect(changed).toEqual(['rankingCriteria']);
  });

  it('does not flag unchanged ordered criteria', () => {
    expect(detectChangedHeritableParts(form(), seed)).toEqual([]);
  });

  it('aggregates inherit vs keep-override from DefaultsBinding', () => {
    const stages: OrganisationStageHubSummary[] = [
      boundStage('1'),
      boundStage('2', {
        name: 'Finale',
        hasStandingRules: false,
        defaultsBinding: {
          matchDuration: { isBound: true },
          extraTime: { isBound: false },
          penaltyShootout: { isBound: false },
          administrativeResult: { isBound: true },
          points: null,
          rankingCriteria: null,
        },
      }),
    ];

    const preview = buildRegulationImpactPreview(
      form({ durationPerPeriod: 40 }),
      {
        regulation: seed,
        stages,
        status: 'Draft',
      },
    );

    expect(preview.hasChanges).toBe(true);
    expect(preview.demotesToDraft).toBe(false);
    expect(preview.changedParts).toEqual(['matchDuration']);
    expect(preview.byPart.matchDuration).toEqual({
      inherit: 2,
      keepOverride: 0,
    });
    expect(preview.stagesUpdatedCount).toBe(2);
    expect(preview.families).toEqual([
      {
        family: 'match',
        changedParts: ['matchDuration'],
        inherit: 2,
        keepOverride: 0,
      },
    ]);
  });

  it('demotes Ready competitions when there are changes', () => {
    const preview = buildRegulationImpactPreview(
      form({ durationPerPeriod: 40 }),
      {
        regulation: seed,
        stages: [boundStage('1')],
        status: 'Ready',
      },
    );
    expect(preview.demotesToDraft).toBe(true);
  });

  it('groups match / forfeit / standing families separately', () => {
    const preview = buildRegulationImpactPreview(
      form({
        durationPerPeriod: 40,
        forfeitWinnerGoals: 2,
        winPoints: 4,
      }),
      {
        regulation: seed,
        stages: [boundStage('1')],
        status: 'Draft',
      },
    );
    expect(preview.families.map((line) => line.family)).toEqual([
      'match',
      'forfeit',
      'standing',
    ]);
  });

  it('flags runningIgnored when heritable parts change and a stage is Running', () => {
    const preview = buildRegulationImpactPreview(
      form({ durationPerPeriod: 40 }),
      {
        regulation: seed,
        stages: [boundStage('1', { status: 'Running' })],
        status: 'Draft',
      },
    );
    expect(preview.families[0]).toMatchObject({
      family: 'match',
      inherit: 0,
      keepOverride: 0,
    });
    expect(preview.runningIgnored).toBe(true);
    expect(preview.hasKeptOverrides).toBe(false);
  });

  it('marks hasKeptOverrides only when keepOverride > 0', () => {
    const preview = buildRegulationImpactPreview(form({ hasExtraTime: true }), {
      regulation: seed,
      stages: [
        boundStage('1', {
          defaultsBinding: {
            matchDuration: { isBound: true },
            extraTime: { isBound: false },
            penaltyShootout: { isBound: true },
            administrativeResult: { isBound: true },
            points: { isBound: true },
            rankingCriteria: { isBound: true },
          },
        }),
      ],
      status: 'Draft',
    });
    expect(preview.families[0]?.keepOverride).toBe(1);
    expect(preview.hasKeptOverrides).toBe(true);
  });

  it('marks a stage personalized only from unbound parts', () => {
    expect(
      isStagePersonalized(
        boundStage('1', {
          hasStandingRules: false,
          defaultsBinding: {
            matchDuration: { isBound: true },
            extraTime: { isBound: true },
            penaltyShootout: { isBound: true },
            administrativeResult: { isBound: true },
            points: null,
            rankingCriteria: null,
          },
        }),
      ),
    ).toBe(false);

    expect(
      isStagePersonalized(
        boundStage('2', {
          hasStandingRules: false,
          defaultsBinding: {
            matchDuration: { isBound: true },
            extraTime: { isBound: false },
            penaltyShootout: { isBound: false },
            administrativeResult: { isBound: true },
            points: null,
            rankingCriteria: null,
          },
        }),
      ),
    ).toBe(true);
  });
});
