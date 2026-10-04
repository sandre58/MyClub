import { describe, expect, it } from 'vitest';
import type { StructureMaterialize } from '../../types';
import {
  classifyMaterializePresentation,
  materializeDisabledCaption,
  materializeProductionTarget,
  showMaterializeActionBlock,
  showMaterializeFrozenRemainingBanner,
  materializeFrozenRemainingCount,
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
