import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import {
  shouldShowAsideMatchsTile,
  shouldShowMatchExploitationProgress,
  shouldShowMatchsSection,
  shouldShowProductionInMatchsSection,
} from './structureMatchsSectionUi';

const baseStage = (
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary => ({
  stageId: 's1',
  name: 'P',
  status: 'Draft',
  teamCount: 0,
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
  ...overrides,
});

describe('structureMatchsSectionUi', () => {
  it('hides aside Matchs tile in Draft even when lifecycle applies', () => {
    expect(shouldShowAsideMatchsTile(baseStage({ status: 'Draft' }), null)).toBe(
      false,
    );
    expect(shouldShowMatchsSection(baseStage({ status: 'Draft' }))).toBe(true);
  });

  it('hides aside Matchs tile for placement/lifecycle-only Ready (O-MAT-IMPL-1)', () => {
    expect(
      shouldShowAsideMatchsTile(
        baseStage({
          status: 'Ready',
          lifecycle: {
            transition: {
              code: 'StartStage',
              enabled: false,
              disabledReasonCode: 'UnmaterializedConfrontations',
            },
          },
        }),
        null,
      ),
    ).toBe(false);
    expect(
      shouldShowAsideMatchsTile(
        baseStage({
          status: 'Ready',
          formatKind: 'Swiss',
          swissRoundCount: 3,
          matchdayCount: 0,
        }),
        null,
      ),
    ).toBe(false);
  });

  it('shows aside Matchs tile for Completed view-matches link without progress body', () => {
    expect(
      shouldShowAsideMatchsTile(
        baseStage({
          status: 'Completed',
          matchCount: 4,
          materialize: {
            commandCode: 'MaterializeMatches',
            labelVariant: 'exhausted',
            determinedCount: 4,
            eligibleCount: 0,
            materializedCount: 4,
            commandEnabled: false,
            confrontations: [],
          },
        }),
        'terminal',
      ),
    ).toBe(true);
  });

  it('shows Matchs section for Completed when matches exist', () => {
    expect(
      shouldShowMatchsSection(
        baseStage({ status: 'Completed', matchCount: 3 }),
      ),
    ).toBe(true);
  });

  it('hides production in Draft and Completed', () => {
    expect(
      shouldShowProductionInMatchsSection(
        baseStage({ materialize: { commandCode: 'MaterializeMatches', labelVariant: 'create', determinedCount: 1, eligibleCount: 1, materializedCount: 0, commandEnabled: true, confrontations: [] } }),
        'actionable',
      ),
    ).toBe(false);
    expect(
      shouldShowProductionInMatchsSection(
        baseStage({ status: 'Completed', materialize: { commandCode: 'MaterializeMatches', labelVariant: 'exhausted', determinedCount: 1, eligibleCount: 0, materializedCount: 1, commandEnabled: false, confrontations: [] } }),
        'terminal',
      ),
    ).toBe(false);
  });

  it('hides production in Suspended unless actionable or dematerialize enabled', () => {
    const partialMaterialize = {
      commandCode: 'MaterializeMatches' as const,
      labelVariant: 'exhausted' as const,
      determinedCount: 6,
      eligibleCount: 0,
      materializedCount: 3,
      commandEnabled: false,
      disabledReason: 'StructureNotMutable',
      confrontations: [],
    };
    const suspended = baseStage({
      status: 'Suspended',
      materialize: partialMaterialize,
    });
    expect(
      shouldShowProductionInMatchsSection(suspended, 'frozen'),
    ).toBe(false);
    expect(
      shouldShowProductionInMatchsSection(suspended, 'blocked'),
    ).toBe(false);
    expect(
      shouldShowProductionInMatchsSection(
        baseStage({
          status: 'Suspended',
          materialize: partialMaterialize,
          dematerialize: { enabled: true, scope: 'FullPhase' },
        }),
        'frozen',
      ),
    ).toBe(true);
  });

  it('shows production in Running only when actionable', () => {
    const running = baseStage({
      status: 'Running',
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'create',
        determinedCount: 6,
        eligibleCount: 2,
        materializedCount: 4,
        commandEnabled: true,
        confrontations: [],
      },
    });
    expect(
      shouldShowProductionInMatchsSection(running, 'actionable'),
    ).toBe(true);
    expect(
      shouldShowProductionInMatchsSection(running, 'terminal'),
    ).toBe(false);
  });

  it('shows exploitation progress in Running and Completed', () => {
    expect(
      shouldShowMatchExploitationProgress(
        baseStage({
          status: 'Running',
          lifecycle: {
            transition: { code: 'CompleteStage', enabled: false },
            matchProgress: {
              total: 2,
              finished: 1,
              open: 1,
              cancelled: 0,
              canComplete: false,
            },
          },
        }),
      ),
    ).toBe(true);
    expect(
      shouldShowMatchExploitationProgress(
        baseStage({ status: 'Completed', matchCount: 1 }),
      ),
    ).toBe(true);
  });
});
