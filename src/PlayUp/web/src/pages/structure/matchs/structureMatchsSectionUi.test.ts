import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../../types';
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
    expect(
      shouldShowAsideMatchsTile(baseStage({ status: 'Draft' }), null),
    ).toBe(false);
    expect(shouldShowMatchsSection(baseStage({ status: 'Draft' }))).toBe(true);
  });

  it('shows aside Matchs tile for Swiss Ready when guidance applies', () => {
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
    ).toBe(true);
  });

  it('hides aside Matchs tile for Swiss Draft', () => {
    expect(
      shouldShowAsideMatchsTile(
        baseStage({
          status: 'Draft',
          formatKind: 'Swiss',
          swissRoundCount: 3,
        }),
        null,
      ),
    ).toBe(false);
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

  it('follows hub matchProductionPresentation.productionVisible', () => {
    expect(
      shouldShowProductionInMatchsSection(
        baseStage({
          matchProductionPresentation: {
            productionVisible: false,
            showPrimaryGauge: true,
            primaryGauge: {
              current: 0,
              capacity: 6,
              messageCode: 'Partial',
              messageKind: 'attention',
            },
            primaryGaugeMode: 'materialized',
            prerequisiteGauge: null,
            followUp: null,
            materialize: {
              visible: true,
              enabled: true,
              commandCode: 'MaterializeMatches',
              labelVariant: 'create',
              disabledReason: null,
            },
            dematerialize: {
              visible: false,
              enabled: false,
              disabledReason: null,
              scope: null,
            },
          },
        }),
      ),
    ).toBe(false);
    expect(
      shouldShowProductionInMatchsSection(
        baseStage({
          status: 'Completed',
          matchProductionPresentation: {
            productionVisible: false,
            showPrimaryGauge: true,
            primaryGauge: {
              current: 6,
              capacity: 6,
              messageCode: 'Complete',
              messageKind: 'success',
            },
            primaryGaugeMode: 'materialized',
            prerequisiteGauge: null,
            followUp: null,
            materialize: {
              visible: false,
              enabled: false,
              commandCode: 'MaterializeMatches',
              labelVariant: 'exhausted',
              disabledReason: null,
            },
            dematerialize: {
              visible: false,
              enabled: false,
              disabledReason: null,
              scope: null,
            },
          },
        }),
      ),
    ).toBe(false);
    expect(
      shouldShowProductionInMatchsSection(
        baseStage({
          status: 'Running',
          matchProductionPresentation: {
            productionVisible: true,
            showPrimaryGauge: true,
            primaryGauge: {
              current: 2,
              capacity: 6,
              messageCode: 'Partial',
              messageKind: 'attention',
            },
            primaryGaugeMode: 'materialized',
            prerequisiteGauge: null,
            followUp: null,
            materialize: {
              visible: true,
              enabled: true,
              commandCode: 'MaterializeMatches',
              labelVariant: 'create',
              disabledReason: null,
            },
            dematerialize: {
              visible: false,
              enabled: false,
              disabledReason: null,
              scope: null,
            },
          },
        }),
      ),
    ).toBe(true);
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
