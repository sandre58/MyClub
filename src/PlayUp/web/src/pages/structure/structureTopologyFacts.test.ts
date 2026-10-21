import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import { topologyCardFacts } from './structureTopologyFacts';

const t = (key: string, options?: Record<string, unknown>) => {
  if (key === 'hub.topologyStatSwissProgress') {
    return `${options?.generated}/${options?.planned} generated`;
  }
  if (key === 'hub.topologyStatMatches') {
    return `${options?.count} matches`;
  }
  if (key === 'hub.topologyStatPlaces') {
    return `${options?.count} places`;
  }
  return key;
};

function swissHub(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: 's1',
    name: 'Swiss',
    status: 'Running',
    formatKind: 'Swiss',
    teamCount: 8,
    matchCount: 4,
    matchdayCount: 1,
    swissRoundCount: 3,
    compositionCapacity: 8,
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
  };
}

describe('topologyCardFacts — Swiss', () => {
  it('shows generated / planned rounds, not planned alone', () => {
    const facts = topologyCardFacts(swissHub(), t);
    const swissFact = facts.find((f) => f.id === 'swiss');
    expect(swissFact?.value).toBe('1/3 generated');
  });
});
