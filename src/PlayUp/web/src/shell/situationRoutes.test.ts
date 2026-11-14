import { describe, expect, it } from 'vitest';
import {
  runtimeQualificationProgressionHref,
  situationHref,
} from './situationRoutes';
import type { OverviewSituation } from '../types';

const baseSituation = (): Omit<
  OverviewSituation,
  'source' | 'targetType' | 'targetId'
> => ({
  nature: 'Blocking',
  matchId: null,
  actionable: true,
  actionCode: 'ApplyQualification',
  impactCode: 'BlocksProgression',
  params: {},
  problem: null,
});

describe('situationRoutes', () => {
  it('deep-links QualificationConflict on a slot to qualification + attribution rail', () => {
    const href = situationHref(
      {
        ...baseSituation(),
        source: 'QualificationConflict',
        targetType: 'Slot',
        targetId: 'dest-stage:place-1',
        params: { slotKey: 'place-1', destinationStageId: 'dest-stage' },
      },
      'comp-1',
    );
    expect(href).toBe(
      '/competitions/comp-1/structure?stage=dest-stage&section=qualification&focus=rail-attribution',
    );
  });

  it('deep-links ConfrontationUndecidable to confrontation section', () => {
    const href = situationHref(
      {
        ...baseSituation(),
        source: 'ConfrontationUndecidable',
        nature: 'Blocking',
        targetType: 'Stage',
        targetId: 'stage-cup',
        params: { stageId: 'stage-cup' },
      },
      'comp-1',
    );
    expect(href).toBe(
      '/competitions/comp-1/structure?stage=stage-cup&section=confrontation',
    );
  });

  it('opens ConfrontationUndecidable on fiche only when phase is not editable', () => {
    const href = situationHref(
      {
        ...baseSituation(),
        source: 'ConfrontationUndecidable',
        nature: 'Blocking',
        targetType: 'Stage',
        targetId: 'stage-cup',
        params: { stageId: 'stage-cup', stageStatus: 'Completed' },
      },
      'comp-1',
      { stageEditable: false },
    );
    expect(href).toBe('/competitions/comp-1/structure?stage=stage-cup');
  });

  it('deep-links ResolutionPerime on a slot to operational placement', () => {
    const href = situationHref(
      {
        ...baseSituation(),
        source: 'ResolutionPerime',
        nature: 'Warning',
        targetType: 'Slot',
        targetId: 'stage-d:Tete',
        actionable: true,
        actionCode: 'ConfigureStructure',
        impactCode: null,
        params: { stageId: 'stage-d', slotKey: 'Tete', count: '1' },
      },
      'comp-1',
    );
    expect(href).toBe(
      '/competitions/comp-1/structure?stage=stage-d&focus=operational-placement',
    );
  });

  it('deep-links ProgressionConflict on a slot to progression + attribution rail', () => {
    const href = situationHref(
      {
        ...baseSituation(),
        source: 'ProgressionConflict',
        targetType: 'Slot',
        targetId: 'dest-stage:place-2',
        actionCode: 'ApplyProgression',
        params: { slotKey: 'place-2', destinationStageId: 'dest-stage' },
      },
      'comp-1',
    );
    expect(href).toBe(
      '/competitions/comp-1/structure?stage=dest-stage&section=progression&focus=rail-attribution',
    );
  });

  it('runtimeQualificationProgressionHref ignores non-runtime sources', () => {
    expect(
      runtimeQualificationProgressionHref(
        {
          ...baseSituation(),
          source: 'DrawNoSolution',
          targetType: 'Draw',
          targetId: 'draw-1',
        },
        'comp-1',
      ),
    ).toBeNull();
  });

  it('does not force rail deep-link for ProgressionPending with only a Stage target', () => {
    expect(
      situationHref(
        {
          ...baseSituation(),
          source: 'ProgressionPending',
          targetType: 'Stage',
          targetId: 'only-stage',
          actionCode: 'ApplyProgression',
          params: {},
        },
        'comp-1',
      ),
    ).toBe('/competitions/comp-1/structure?stage=only-stage');
  });
});
