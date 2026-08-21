import { describe, expect, it } from 'vitest'
import { resolveActiveDestination, shellDestinationHrefs } from './shellDestinations'

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc'

describe('shellDestinationHrefs', () => {
  it('routes competition-scoped links to the list when context is absent', () => {
    expect(shellDestinationHrefs({})).toEqual({
      cockpit: '/',
      organisation: '/competitions',
      matches: '/competitions',
      classements: '/competitions',
    })
  })

  it('uses resolved competition routes when context is known', () => {
    expect(shellDestinationHrefs({ competitionId })).toEqual({
      cockpit: `/competitions/${competitionId}`,
      organisation: `/competitions/${competitionId}/organisation`,
      matches: `/competitions/${competitionId}/matches`,
      classements: `/competitions/${competitionId}/classements`,
    })
  })

  it('keeps stage deep-link fallbacks while competition resolves', () => {
    expect(shellDestinationHrefs({ stageId })).toEqual({
      cockpit: '/',
      organisation: '/competitions',
      matches: `/stages/${stageId}/matches`,
      classements: '/competitions',
    })
  })

  it('keeps match deep-link fallbacks while competition resolves', () => {
    expect(shellDestinationHrefs({ matchId })).toEqual({
      cockpit: '/',
      organisation: '/competitions',
      matches: `/matches/${matchId}`,
      classements: '/competitions',
    })
  })
})

describe('resolveActiveDestination', () => {
  it('maps workspace and list routes to Cockpit', () => {
    expect(resolveActiveDestination('/')).toBe('cockpit')
    expect(resolveActiveDestination('/competitions')).toBe('cockpit')
    expect(resolveActiveDestination(`/competitions/${competitionId}`)).toBe(
      'cockpit',
    )
  })

  it('maps organisation routes to Organisation', () => {
    expect(
      resolveActiveDestination(
        `/competitions/${competitionId}/organisation`,
      ),
    ).toBe('organisation')
  })

  it('maps match hub and stage routes to Matchs', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/matches`),
    ).toBe('matches')
    expect(resolveActiveDestination(`/stages/${stageId}/matches`)).toBe(
      'matches',
    )
    expect(resolveActiveDestination(`/matches/${matchId}`)).toBe('matches')
    expect(resolveActiveDestination(`/stages/${stageId}`)).toBe('matches')
  })

  it('maps classements routes to Classements', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/classements`),
    ).toBe('classements')
  })

  it('does not treat overview as a shell destination', () => {
    expect(
      resolveActiveDestination(`/competitions/${competitionId}/overview`),
    ).toBeNull()
  })

  it('returns null for routes outside the shell destinations', () => {
    expect(resolveActiveDestination('/foundations')).toBeNull()
  })
})
