import { describe, expect, it } from 'vitest';
import type { StructureStageHubSummary } from '../../types';
import { sortiesAvalPeerStages } from './structureSortiesDestinations';

function stage(stageId: string, name = stageId): StructureStageHubSummary {
  return { stageId, name } as StructureStageHubSummary;
}

describe('sortiesAvalPeerStages', () => {
  const stages = [
    stage('poules', 'Poules'),
    stage('demi', 'Demi-finales'),
    stage('finale', 'Finale'),
  ];

  it('excludes self and amont; keeps aval peers in Structure order', () => {
    expect(
      sortiesAvalPeerStages(stages, 'poules').map((s) => s.stageId),
    ).toEqual(['demi', 'finale']);
    expect(sortiesAvalPeerStages(stages, 'demi').map((s) => s.stageId)).toEqual(
      ['finale'],
    );
  });

  it('returns empty when source is last (no aval)', () => {
    expect(sortiesAvalPeerStages(stages, 'finale')).toEqual([]);
  });

  it('returns empty when source is unknown', () => {
    expect(sortiesAvalPeerStages(stages, 'unknown')).toEqual([]);
  });

  it('returns empty for a single-stage competition', () => {
    expect(sortiesAvalPeerStages([stage('only')], 'only')).toEqual([]);
  });
});
