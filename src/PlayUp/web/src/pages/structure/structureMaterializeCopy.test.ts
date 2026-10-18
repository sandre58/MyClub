import { describe, expect, it } from 'vitest';
import type { StructureMaterialize } from '../../types';
import {
  classifyMaterializePresentation,
  materializeDisabledCaption,
  materializeProductionTarget,
  showMaterializeActionBlock,
  showMaterializeFrozenRemainingBanner,
  materializeFrozenRemainingCount,
  showViewMaterializedMatchesLink,
  cupCanonicalEntryRoundIncomplete,
  cupEntryRoundPlacementGaugeRatio,
  cupMatchProductionGaugeRatio,
  materializeDeterminedConfrontationsRatio,
  resolveMatchsProductionGauge,
  hasMatchsStructuralProductionChrome,
  shouldConfirmCupMaterializeWithIncompleteEntryRound,
  resolveStartStageProductionBlocker,
} from './structureMaterializeCopy';

const base: StructureMaterialize = {
  commandCode: 'MaterializeMatches',
  labelVariant: 'create',
  determinedCount: 6,
  eligibleCount: 3,
  materializedCount: 0,
  commandEnabled: true,
  confrontations: [],
};

describe('structureMaterializeCopy', () => {
  it('offers view matches link only when the stage has attached matches', () => {
    expect(showViewMaterializedMatchesLink({ matchCount: 0 }, base)).toBe(false);
    expect(
      showViewMaterializedMatchesLink({ matchCount: 0 }, {
        ...base,
        materializedCount: 3,
      }),
    ).toBe(false);
    expect(
      showViewMaterializedMatchesLink({ matchCount: 4 }, {
        ...base,
        materializedCount: 0,
      }),
    ).toBe(false);
    expect(
      showViewMaterializedMatchesLink({ matchCount: 4 }, {
        ...base,
        materializedCount: 2,
      }),
    ).toBe(true);
    expect(showViewMaterializedMatchesLink({ matchCount: 2 })).toBe(true);
    expect(showViewMaterializedMatchesLink({ matchCount: 0 })).toBe(false);
  });

  it('uses determinedCount as production target from the read', () => {
    expect(materializeProductionTarget({ ...base, materializedCount: 3 })).toBe(
      6,
    );
  });

  it('classifies actionable when command is enabled', () => {
    expect(
      classifyMaterializePresentation(base, 'Draft', 'Draft'),
    ).toBe('actionable');
  });

  it('classifies terminal when every determined confrontation is materialized', () => {
    expect(
      classifyMaterializePresentation(
        {
          ...base,
          labelVariant: 'exhausted',
          eligibleCount: 0,
          materializedCount: 6,
          commandEnabled: false,
        },
        'Draft',
        'Draft',
      ),
    ).toBe('terminal');
  });

  it('classifies running 3/6 as frozen, not terminal', () => {
    expect(
      classifyMaterializePresentation(
        {
          ...base,
          labelVariant: 'exhausted',
          eligibleCount: 0,
          materializedCount: 3,
          commandEnabled: false,
        },
        'Running',
        'Running',
      ),
    ).toBe('frozen');
  });

  it('classifies blocked when mutable but nothing eligible yet', () => {
    expect(
      classifyMaterializePresentation(
        {
          ...base,
          labelVariant: 'idle',
          determinedCount: 0,
          eligibleCount: 0,
          materializedCount: 0,
          commandEnabled: false,
        },
        'Draft',
        'Draft',
      ),
    ).toBe('blocked');
  });

  it('maps disabledReason to i18n without inventing unknown codes', () => {
    const t = (key: string) =>
      key === 'fiche.materialize.disabledReason.StructureNotMutable'
        ? 'Structure locked'
        : key === 'fiche.materialize.blockedGeneric'
          ? 'Generic'
          : key;

    expect(
      materializeDisabledCaption(
        { ...base, commandEnabled: false, disabledReason: 'StructureNotMutable' },
        t,
      ),
    ).toBe('Structure locked');

    expect(
      materializeDisabledCaption(
        { ...base, commandEnabled: false, disabledReason: 'FutureCode' },
        t,
      ),
    ).toBe('Generic');
  });

  it('shows action block only for actionable and blocked', () => {
    expect(showMaterializeActionBlock('actionable')).toBe(true);
    expect(showMaterializeActionBlock('blocked')).toBe(true);
    expect(showMaterializeActionBlock('terminal')).toBe(false);
    expect(showMaterializeActionBlock('frozen')).toBe(false);
  });

  it('classifies Suspended partial materialize as frozen (not actionable)', () => {
    expect(
      classifyMaterializePresentation(
        {
          ...base,
          commandEnabled: false,
          materializedCount: 3,
          determinedCount: 6,
          disabledReason: 'StructureNotMutable',
        },
        'Running',
        'Suspended',
      ),
    ).toBe('frozen');
  });

  it('splits cup match production from entry-pair placement gauges', () => {
    const materialize = {
      ...base,
      commandCode: 'MaterializeFromOccupiedSlots' as const,
      determinedCount: 1,
      materializedCount: 1,
      canonicalEntryPairCount: 4,
      undeterminedCanonicalEntryPairCount: 3,
    };
    expect(
      cupMatchProductionGaugeRatio(
        { matchCount: 2, defaultTieFormat: { numberOfLegs: 2 } },
        materialize,
      ),
    ).toEqual({ filled: 2, capacity: 2 });
    expect(cupEntryRoundPlacementGaugeRatio(materialize)).toEqual({
      filled: 1,
      capacity: 4,
    });
  });

  it('detects incomplete canonical cup entry round from read counts', () => {
    const cupPartial = {
      ...base,
      commandCode: 'MaterializeFromOccupiedSlots' as const,
      canonicalEntryPairCount: 2,
      undeterminedCanonicalEntryPairCount: 1,
    };
    expect(cupCanonicalEntryRoundIncomplete(cupPartial)).toBe(true);
    expect(
      shouldConfirmCupMaterializeWithIncompleteEntryRound({
        ...cupPartial,
        commandEnabled: true,
      }),
    ).toBe(true);
    expect(
      cupCanonicalEntryRoundIncomplete({
        ...cupPartial,
        undeterminedCanonicalEntryPairCount: 0,
      }),
    ).toBe(false);
  });

  it('resolveMatchsProductionGauge prefers materialized when determinedCount > 0', () => {
    expect(
      resolveMatchsProductionGauge(
        { ...base, materializedCount: 1, determinedCount: 2 },
        { matchCount: 0 },
      ),
    ).toEqual({
      mode: 'materialized',
      ratio: { filled: 1, capacity: 2 },
    });
  });

  it('resolveMatchsProductionGauge uses confrontation rows when nothing is determined', () => {
    const materialize = {
      ...base,
      determinedCount: 0,
      confrontations: [
        {
          key: 'a',
          sideA: '1',
          sideB: '2',
          determined: false,
          materialized: false,
          eligible: false,
        },
        {
          key: 'b',
          sideA: '3',
          sideB: '4',
          determined: true,
          materialized: false,
          eligible: false,
        },
      ],
    };
    expect(materializeDeterminedConfrontationsRatio(materialize)).toEqual({
      filled: 1,
      capacity: 2,
    });
    expect(resolveMatchsProductionGauge(materialize, { matchCount: 0 })).toEqual(
      {
        mode: 'determined',
        ratio: { filled: 1, capacity: 2 },
      },
    );
  });

  it('hasMatchsStructuralProductionChrome is false with no rows or canonical pairs', () => {
    expect(
      hasMatchsStructuralProductionChrome(
        { ...base, determinedCount: 0, confrontations: [] },
        { matchCount: 0 },
      ),
    ).toBe(false);
  });

  it('resolveStartStageProductionBlocker ignores NoExploitableInitialConfrontations', () => {
    expect(
      resolveStartStageProductionBlocker({
        transition: {
          code: 'StartStage',
          enabled: false,
          disabledReasonCode: 'NoExploitableInitialConfrontations',
        },
      }),
    ).toBeNull();
  });

  it('shows frozen remaining banner only for incomplete frozen states', () => {
    const partial = {
      ...base,
      commandEnabled: false,
      materializedCount: 3,
      determinedCount: 6,
    };
    expect(materializeFrozenRemainingCount(partial)).toBe(3);
    expect(showMaterializeFrozenRemainingBanner('frozen', partial)).toBe(true);
    expect(showMaterializeFrozenRemainingBanner('terminal', partial)).toBe(
      false,
    );
    expect(
      showMaterializeFrozenRemainingBanner('frozen', {
        ...partial,
        materializedCount: 6,
      }),
    ).toBe(false);
  });
});
