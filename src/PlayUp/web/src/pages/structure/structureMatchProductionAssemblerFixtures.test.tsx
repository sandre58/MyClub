import { describe, expect, it } from 'vitest';

import type { StructureMatchProductionPresentation } from '../../types';
import cupExhaustedFixture from './__fixtures__/matchProduction/cup-exhausted.json';
import runningGroupsPartialFixture from './__fixtures__/matchProduction/running-groups-partial.json';

/**
 * Contract checks on assembler-exported JSON (UPDATE_MATCH_PRODUCTION_FIXTURES=1).
 * UI wiring is covered separately once the strip consumes showPrimaryGauge / frozenPartial.
 */
describe('match production assembler fixtures (hub contract)', () => {
  it('running groups partial: exploitation mode with frozenPartial 1/2', () => {
    const presentation =
      runningGroupsPartialFixture as StructureMatchProductionPresentation;

    expect(presentation.productionVisible).toBe(true);
    expect(presentation.showPrimaryGauge).toBe(false);
    expect(presentation.frozenPartial).toEqual({
      current: 1,
      capacity: 2,
      messageCode: 'FrozenPartial',
      messageKind: 'attention',
    });
    expect(presentation.materialize.visible).toBe(false);
    expect(presentation.dematerialize.visible).toBe(false);
  });

  it('cup exhausted mono-round: no followUp on Ready projection', () => {
    const presentation = cupExhaustedFixture as StructureMatchProductionPresentation;

    expect(presentation.showPrimaryGauge).toBe(true);
    expect(presentation.followUp).toBeNull();
    expect(presentation.frozenPartial).toBeNull();
  });
});
