import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import { buildPhaseContextSummary } from './structureTopologyFacts';

const t = (key: string, options?: Record<string, unknown>) => {
  if (key === 'fiche.phaseContextTeams' && options?.count != null) {
    return `${options.count} équipes`;
  }
  if (key === 'hub.topologyStatMatchdays') {
    return `${options?.count ?? 0} journées`;
  }
  if (key === 'hub.topologyStatGroupGrid') {
    return `${options?.groups}×${options?.size}`;
  }
  return key;
};

const baseStage: StructureStageHubSummary = {
  stageId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  name: 'Groupes',
  status: 'Draft',
  teamCount: 8,
  matchCount: 0,
  formatKind: 'Groups',
  groupCount: 2,
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
};

describe('buildPhaseContextSummary', () => {
  it('joins format, teams, and topology facts for single-phase hub', () => {
    const summary = buildPhaseContextSummary(baseStage, t);
    expect(summary).toContain('Groupes');
    expect(summary).toContain('8 équipes');
    expect(summary).toContain('2×');
  });
});
