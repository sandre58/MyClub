import { describe, expect, it } from 'vitest'
import { cockpitSituation, cockpitView } from '../test/cockpitFixtures'
import {
  actionPresentationSlot,
  actionsForDraw,
  actionsForStage,
  closurePresentation,
  isProminenceVisible,
  operationalBlocks,
  primaryTeamActions,
  secondaryActions,
  sortConstructionSlots,
} from './cockpitComposition'
import { cockpitActionKey } from './cockpitActions'

describe('cockpitComposition', () => {
  it('hides Absent prominence', () => {
    expect(isProminenceVisible('Absent')).toBe(false)
    expect(isProminenceVisible('Dominant')).toBe(true)
  })

  it('orders construction slots by prominence without Absent', () => {
    const slots = sortConstructionSlots(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          teams: { prominence: 'Present', facts: { activeCount: '1' } },
          structure: { prominence: 'Dominant', facts: { formatKind: 'None' } },
          regulation: {
            ...cockpitView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          matches: { prominence: 'Absent', facts: { total: '0' } },
        },
      }),
    )
    expect(slots).toEqual(['structure', 'teams', 'regulation'])
  })

  it('maps action codes to presentation slots without inventing métier rules', () => {
    expect(actionPresentationSlot('AddEntry')).toBe('teams')
    expect(actionPresentationSlot('ConfigureStructure')).toBe('structure')
    expect(actionPresentationSlot('PublishDraw')).toBe('operational')
    expect(actionPresentationSlot('CompleteCompetition')).toBe('closure')
    // Lifecycle Prepare/Start stay secondary — closure slot is hidden during Construction.
    expect(actionPresentationSlot('PrepareCompetition')).toBe('secondary')
    expect(actionPresentationSlot('StartCompetition')).toBe('secondary')
  })

  it('hides closure in Construction when not completable', () => {
    expect(
      closurePresentation(
        cockpitView({
          cycleReading: { code: 'Construction' },
          closureHint: { canCompleteNormally: false, blockerCodes: [] },
        }),
      ),
    ).toBe('hidden')
  })

  it('shows full closure when completable', () => {
    expect(
      closurePresentation(
        cockpitView({
          cycleReading: { code: 'InProgress' },
          closureHint: { canCompleteNormally: true, blockerCodes: [] },
        }),
      ),
    ).toBe('full')
  })

  it('omits empty match blocks in Construction', () => {
    const blocks = operationalBlocks(
      cockpitView({
        cycleReading: { code: 'Construction' },
        operationalFocus: {
          stages: [{ stageId: 's', name: 'P', status: 'Draft' }],
          draws: [],
          matchCounts: {
            live: 0,
            scheduled: 0,
            finished: 0,
            postponed: 0,
            cancelled: 0,
            total: 0,
          },
          upcomingMatches: [],
          swissByes: [],
        },
      }),
    )
    expect(blocks).toEqual(['stages'])
  })

  it('excludes already-rendered and team-admin actions from the secondary strip', () => {
    const actions = [
      { code: 'AddEntry', guaranteed: false },
      { code: 'RenameEntry', guaranteed: false },
      { code: 'MaterializeMatches', guaranteed: false },
    ]
    const rendered = new Set([cockpitActionKey(actions[0])])
    expect(secondaryActions(actions, rendered).map((a) => a.code)).toEqual([
      'MaterializeMatches',
    ])
  })

  it('keeps only AddEntry as primary team cockpit action', () => {
    expect(
      primaryTeamActions([
        { code: 'AddEntry', guaranteed: false },
        { code: 'RenameEntry', guaranteed: false },
        { code: 'WithdrawEntry', guaranteed: false },
      ]).map((a) => a.code),
    ).toEqual(['AddEntry'])
  })

  it('attaches PrepareStage and PublishDraw to stage/draw ids from the Read', () => {
    const stageId = 'stage-1'
    const drawId = 'draw-1'
    const actions = [
      { code: 'PrepareStage', guaranteed: false, stageId },
      { code: 'PublishDraw', guaranteed: false, stageId, drawId },
      { code: 'PrepareStage', guaranteed: false, stageId: 'other' },
    ]
    expect(actionsForStage(actions, stageId).map((a) => a.code)).toEqual([
      'PrepareStage',
    ])
    expect(actionsForDraw(actions, stageId, drawId).map((a) => a.code)).toEqual([
      'PublishDraw',
    ])
  })

  it('does not treat attentionSummary as a separate situation source', () => {
    const situation = cockpitSituation()
    const view = cockpitView({
      situations: [situation],
      attentionSummary: { count: 1, items: [situation] },
    })
    expect(view.attentionSummary.items[0].source).toBe(view.situations[0].source)
  })
})
