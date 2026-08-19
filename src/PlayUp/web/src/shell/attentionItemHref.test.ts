import { describe, expect, it } from 'vitest'
import type { NeedsAttentionItem } from '../types'
import { attentionItemHref } from './attentionItemHref'

describe('attentionItemHref', () => {
  const baseItem: NeedsAttentionItem = {
    source: 'Test',
    reason: 'Reason',
    severity: 'Warning',
    targetType: null,
    targetId: null,
  }

  it('maps Stage targets to stage routes', () => {
    expect(
      attentionItemHref({
        ...baseItem,
        targetType: 'Stage',
        targetId: 'stage-1',
      }),
    ).toBe('/stages/stage-1')
  })

  it('maps Match targets to match routes', () => {
    expect(
      attentionItemHref({
        ...baseItem,
        targetType: 'Match',
        targetId: 'match-1',
      }),
    ).toBe('/matches/match-1')
  })

  it('maps Organisation targets to organisation routes', () => {
    expect(
      attentionItemHref({
        ...baseItem,
        targetType: 'Organisation',
        targetId: 'comp-1',
      }),
    ).toBe('/competitions/comp-1/organisation')
  })

  it('maps Competition targets to workspace routes', () => {
    expect(
      attentionItemHref({
        ...baseItem,
        targetType: 'Competition',
        targetId: 'comp-1',
      }),
    ).toBe('/competitions/comp-1')
  })

  it('maps Slot targets to stage routes using the stage prefix', () => {
    expect(
      attentionItemHref({
        ...baseItem,
        targetType: 'Slot',
        targetId: 'stage-1:slot-a',
      }),
    ).toBe('/stages/stage-1')
  })

  it('maps Fixture targets via match rows', () => {
    expect(
      attentionItemHref(
        {
          ...baseItem,
          targetType: 'Fixture',
          targetId: 'fixture-1',
        },
        [
          {
            stageName: 'Group stage',
            match: {
              matchId: 'match-1',
              stageId: 'stage-1',
              fixtureId: 'fixture-1',
              roundId: null,
              status: 'Scheduled',
              home: { entryId: 'h1', displayName: 'Home' },
              away: { entryId: 'a1', displayName: 'Away' },
              score: null,
            },
          },
        ],
      ),
    ).toBe('/matches/match-1')
  })

  it('returns null when targetId is missing', () => {
    expect(
      attentionItemHref({
        ...baseItem,
        targetType: 'Stage',
        targetId: null,
      }),
    ).toBeNull()
  })
})
