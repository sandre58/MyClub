import { describe, expect, it } from 'vitest';
import {
  resolveActiveDestination,
  shellDestinationHrefs,
} from './shellDestinations';

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

describe('shellDestinationHrefs', () => {
  it('routes competition-scoped links to the list when context is absent', () => {
    expect(shellDestinationHrefs({})).toEqual({
      overview: '/',
      structure: '/',
      matches: '/',
      classements: '/',
      teams: '/',
      regulation: '/',
    });
  });

  it('uses resolved competition routes when context is known', () => {
    expect(shellDestinationHrefs({ competitionId })).toEqual({
      overview: `/competitions/${competitionId}`,
      structure: `/competitions/${competitionId}/structure`,
      matches: `/competitions/${competitionId}/matches`,
      classements: `/competitions/${competitionId}/classements`,
      teams: `/competitions/${competitionId}/teams`,
      regulation: `/competitions/${competitionId}/regulation`,
    });
  });

  it('keeps stage deep-link fallbacks while competition resolves', () => {
    expect(shellDestinationHrefs({ stageId })).toEqual({
      overview: '/',
      structure: '/',
      matches: `/stages/${stageId}/matches`,
      classements: '/',
      teams: '/',
      regulation: '/',
    });
  });

  it('keeps match deep-link fallbacks while competition resolves', () => {
    expect(shellDestinationHrefs({ matchId })).toEqual({
      overview: '/',
      structure: '/',
      matches: `/matches/${matchId}`,
      classements: '/',
      teams: '/',
      regulation: '/',
    });
  });
});

describe('resolveActiveDestination', () => {
  it("maps workspace and list routes to Vue d'ensemble", () => {
    expect(resolveActiveDestination('/')).toBe('overview');
    expect(resolveActiveDestination('/competitions')).toBe('overview');
    expect(resolveActiveDestination(`/competitions/${competitionId}`)).toBe(
      'overview',
    );
  });

  it('maps /structure URL to Structure destination', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/structure`),
    ).toBe('structure');
  });

  it('maps teams routes to Équipes', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/teams`),
    ).toBe('teams');
    expect(
      resolveActiveDestination(
        `/competitions/${competitionId}/teams/${competitionId}`,
      ),
    ).toBe('teams');
  });

  it('maps regulation routes to Règlement', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/regulation`),
    ).toBe('regulation');
  });

  it('maps match hub and stage routes to Matchs', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/matches`),
    ).toBe('matches');
    expect(resolveActiveDestination(`/stages/${stageId}/matches`)).toBe(
      'matches',
    );
    expect(resolveActiveDestination(`/matches/${matchId}`)).toBe('matches');
    expect(resolveActiveDestination(`/stages/${stageId}`)).toBe('matches');
  });

  it('maps classements routes to Classements', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/classements`),
    ).toBe('classements');
  });

  it('does not treat overview as a shell destination', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/overview`),
    ).toBeNull();
  });

  it('returns null for routes outside the shell destinations', () => {
    expect(resolveActiveDestination('/foundations')).toBeNull();
  });
});
