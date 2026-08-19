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
      consultation: '/competitions',
    })
  })

  it('uses resolved competition routes when context is known', () => {
    expect(shellDestinationHrefs({ competitionId })).toEqual({
      cockpit: `/competitions/${competitionId}`,
      organisation: `/competitions/${competitionId}/organisation`,
      matches: `/competitions/${competitionId}/matches`,
      consultation: `/competitions/${competitionId}/overview`,
    })
  })

  it('keeps stage deep-link fallbacks while competition resolves', () => {
    expect(shellDestinationHrefs({ stageId })).toEqual({
      cockpit: '/',
      organisation: '/competitions',
      matches: `/stages/${stageId}/matches`,
      consultation: `/stages/${stageId}`,
    })
  })

  it('keeps match deep-link fallbacks while competition resolves', () => {
    expect(shellDestinationHrefs({ matchId })).toEqual({
      cockpit: '/',
      organisation: '/competitions',
      matches: `/matches/${matchId}`,
      consultation: '/competitions',
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
})
