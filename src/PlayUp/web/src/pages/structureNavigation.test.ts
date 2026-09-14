import { describe, expect, it } from 'vitest';
import {
  isStructureSectionId,
  parseStructureDeepLink,
  structureDeepLink,
} from './structureNavigation';

describe('structureNavigation', () => {
  it('builds a stage+section deep-link', () => {
    expect(
      structureDeepLink({
        competitionId: 'comp-1',
        stageId: 'stage-1',
        section: 'matchs',
      }),
    ).toBe('/competitions/comp-1/structure?stage=stage-1&section=matchs');
  });

  it('omits section for phase overview', () => {
    expect(
      structureDeepLink({
        competitionId: 'comp-1',
        stageId: 'stage-1',
      }),
    ).toBe('/competitions/comp-1/structure?stage=stage-1');
  });

  it('parses stage, section, and round', () => {
    expect(
      parseStructureDeepLink('stage=stage-1&section=tirage&round=round-9'),
    ).toEqual({
      stageId: 'stage-1',
      section: 'tirage',
      roundId: 'round-9',
      compose: false,
    });
  });

  it('parses compose deep-link', () => {
    expect(parseStructureDeepLink('stage=stage-1&compose=1')).toEqual({
      stageId: 'stage-1',
      section: null,
      roundId: null,
      compose: true,
    });
    expect(
      structureDeepLink({
        competitionId: 'comp-1',
        stageId: 'stage-1',
        compose: true,
      }),
    ).toBe('/competitions/comp-1/structure?stage=stage-1&compose=1');
  });

  it('rejects unknown sections', () => {
    expect(isStructureSectionId('matchs')).toBe(true);
    expect(isStructureSectionId('unknown')).toBe(false);
    expect(parseStructureDeepLink('stage=s1&section=unknown').section).toBe(
      null,
    );
  });
});
