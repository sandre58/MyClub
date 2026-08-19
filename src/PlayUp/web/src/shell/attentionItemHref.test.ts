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
})
