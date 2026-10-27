import { describe, expect, it } from 'vitest';
import type { StructureMaterialize } from '../../../types';
import {
  classifyMaterializePresentation,
  materializeDisabledCaption,
  materializeProductionTarget,
  showMaterializeActionBlock,
  showMaterializeFrozenRemainingBanner,
  materializeFrozenRemainingCount,
  showViewMaterializedMatchesLink,
  cupCanonicalEntryRoundIncomplete,
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
    expect(showViewMaterializedMatchesLink({ matchCount: 0 }, base)).toBe(
      false,
    );
    expect(
      showViewMaterializedMatchesLink(
        { matchCount: 0 },
        {
          ...base,
          materializedCount: 3,
        },
      ),
    ).toBe(false);
    expect(
      showViewMaterializedMatchesLink(
        { matchCount: 4 },
        {
          ...base,
          materializedCount: 2,
        },
      ),
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
    expect(classifyMaterializePresentation(base, 'Draft', 'Draft')).toBe(
      'actionable',
    );
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

  it('materializeDisabledCaption uses roster variant when stage context is provided', () => {
    const t = (key: string) =>
      key ===
      'fiche.materialize.disabledReason.PhaseRosterIncompleteBandIncomplete'
        ? 'Band incomplete caption'
        : key;
    expect(
      materializeDisabledCaption(
        {
          ...base,
          disabledReason: 'PhaseRosterIncomplete',
          commandEnabled: false,
        },
        t,
        { compositionEntryCount: 7, compositionCapacity: 8 },
      ),
    ).toBe('Band incomplete caption');
  });

  it('materializeDisabledCaption maps known disabled reasons', () => {
    const t = (key: string) =>
      key === 'fiche.materialize.disabledReason.GroupsNotReady'
        ? 'Groups blocked'
        : key;
    expect(
      materializeDisabledCaption(
        { ...base, disabledReason: 'GroupsNotReady', commandEnabled: false },
        t,
      ),
    ).toBe('Groups blocked');
  });

  it('showMaterializeActionBlock is true for actionable and blocked', () => {
    expect(showMaterializeActionBlock('actionable')).toBe(true);
    expect(showMaterializeActionBlock('blocked')).toBe(true);
    expect(showMaterializeActionBlock('frozen')).toBe(false);
  });
});
