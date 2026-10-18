import { describe, expect, it } from 'vitest';
import type { StructureMaterialize } from '../../types';
import {
  classifyMaterializePresentation,
  materializeDisabledCaption,
  materializeProductionTarget,
  showMaterializeActionBlock,
  showMaterializeFrozenRemainingBanner,
  materializeFrozenRemainingCount,
  resolveProspectiveMaterializeCapacity,
  shouldShowMaterializeCommandButton,
  showViewMaterializedMatchesLink,
  cupCanonicalEntryRoundIncomplete,
  cupEntryRoundPlacementGaugeRatio,
  cupMatchProductionGaugeRatio,
  materializeDeterminedConfrontationsRatio,
  resolveMatchsProductionGauge,
  groupsPlacementGaugeRatio,
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

  it('shouldShowMaterializeCommandButton follows remaining determined work', () => {
    const terminal = {
      ...base,
      materializedCount: 6,
      eligibleCount: 0,
      commandEnabled: false,
    };
    expect(
      shouldShowMaterializeCommandButton(terminal, 'terminal'),
    ).toBe(false);

    expect(
      shouldShowMaterializeCommandButton(
        { ...base, materializedCount: 2, commandEnabled: false },
        'blocked',
      ),
    ).toBe(true);

    const rosterBlocked = {
      ...base,
      determinedCount: 0,
      materializedCount: 0,
      commandEnabled: false,
      disabledReason: 'PhaseRosterIncomplete',
    };
    expect(shouldShowMaterializeCommandButton(rosterBlocked, 'blocked')).toBe(
      false,
    );
    expect(
      shouldShowMaterializeCommandButton(rosterBlocked, 'blocked', {
        formatKind: 'Championship',
        compositionCapacity: 4,
        numberOfLegs: 1,
        defaultTieFormat: null,
        matchCount: 0,
        groupCount: 0,
        placesPerGroup: null,
      }),
    ).toBe(true);
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
        {
          matchCount: 2,
          defaultTieFormat: {
            numberOfLegs: 2,
            aggregateScoring: false,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        },
        materialize,
      ),
    ).toEqual({ filled: 2, capacity: 2 });
    expect(cupEntryRoundPlacementGaugeRatio(materialize)).toEqual({
      filled: 1,
      capacity: 4,
    });
  });

  it('cup prospective capacity drives materialize CTA visibility before any match is determined', () => {
    const materialize = {
      ...base,
      commandCode: 'MaterializeFromOccupiedSlots' as const,
      determinedCount: 0,
      materializedCount: 0,
      commandEnabled: false,
      canonicalEntryPairCount: 8,
      confrontations: [],
    };
    const stage = { matchCount: 0, formatKind: 'Cup' as const };
    expect(resolveProspectiveMaterializeCapacity(materialize, stage)).toBe(8);
    expect(
      shouldShowMaterializeCommandButton(materialize, 'blocked', stage),
    ).toBe(true);
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

  it('groups not ready exposes match gauge and separate pool gauge', () => {
    const materialize = {
      ...base,
      determinedCount: 0,
      materializedCount: 0,
      disabledReason: 'GroupsNotReady',
      groupsPlacementFilledCount: 2,
      groupsPlacementCapacityCount: 8,
      prospectiveConfrontationCount: 4,
    };
    const stage = { matchCount: 0, formatKind: 'Groups' as const };
    expect(groupsPlacementGaugeRatio(materialize)).toEqual({
      filled: 2,
      capacity: 8,
    });
    expect(
      resolveMatchsProductionGauge(
        { ...materialize, confrontations: [] },
        stage,
      ),
    ).toEqual({
      mode: 'materialized',
      ratio: { filled: 0, capacity: 4 },
    });
  });

  it('roster gap blocks determined-confrontation idle gauge when lifecycle says PhaseRosterIncomplete', () => {
    const materialize = {
      ...base,
      determinedCount: 0,
      disabledReason: 'NoDeterminedConfrontations',
      confrontations: [
        {
          key: 'x',
          sideA: 'A',
          sideB: 'B',
          determined: false,
          materialized: false,
          eligible: false,
        },
        {
          key: 'y',
          sideA: 'C',
          sideB: 'D',
          determined: false,
          materialized: false,
          eligible: false,
        },
      ],
    };
    const stage = {
      matchCount: 0,
      formatKind: 'Championship' as const,
      compositionCapacity: 4,
      lifecycle: {
        transition: {
          code: 'StartStage' as const,
          enabled: false,
          disabledReasonCode: 'PhaseRosterIncomplete',
        },
      },
    };
    expect(resolveMatchsProductionGauge(materialize, stage)).toBeNull();
  });

  it('groups not ready wins over undetermined confrontation rows (no cup idle copy)', () => {
    const materialize = {
      ...base,
      determinedCount: 0,
      materializedCount: 0,
      disabledReason: 'GroupsNotReady',
      groupsPlacementFilledCount: 1,
      groupsPlacementCapacityCount: 4,
      prospectiveConfrontationCount: 2,
      confrontations: [
        {
          key: 'x',
          sideA: 'A',
          sideB: 'B',
          determined: false,
          materialized: false,
          eligible: false,
        },
      ],
    };
    const stage = {
      matchCount: 0,
      formatKind: 'Groups' as const,
      groupCount: 2,
      placesPerGroup: 2,
    };
    expect(resolveMatchsProductionGauge(materialize, stage)).toEqual({
      mode: 'materialized',
      ratio: { filled: 0, capacity: 2 },
    });
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
