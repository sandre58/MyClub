import { describe, expect, it } from 'vitest'
import {
  overviewActionKey,
  resolveOverviewActionIntent,
} from './overviewActions'
import { overviewIds, overviewView } from '../test/overviewFixtures'
import { situationHref } from './overviewNavigation'

const { competitionId, stageId, drawId, matchId } = overviewIds

describe('resolveOverviewActionIntent', () => {
  it('executes PrepareStage when stageId is provided by the Read', () => {
    const view = overviewView()
    const intent = resolveOverviewActionIntent(
      { code: 'PrepareStage', guaranteed: false, stageId },
      view,
    )
    expect(intent.kind).toBe('execute')
  })

  it('executes PrepareCompetition as a bodyless Host command', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'PrepareCompetition', guaranteed: false },
      overviewView(),
    )
    expect(intent.kind).toBe('execute')
  })

  it('executes StartCompetition as a bodyless Host command', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'StartCompetition', guaranteed: false },
      overviewView(),
    )
    expect(intent.kind).toBe('execute')
  })

  it('keeps unknown action codes unsupported', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'InventedAction', guaranteed: false },
      overviewView(),
    )
    expect(intent).toEqual({ kind: 'unsupported' })
  })

  it('navigates organisation actions instead of inventing POST bodies', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'AddEntry', guaranteed: false },
      overviewView(),
    )
    expect(intent).toEqual({
      kind: 'navigate',
      to: `/competitions/${competitionId}/organisation`,
    })
  })

  it('navigates OpenConsultation to Classements', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'OpenConsultation', guaranteed: false },
      overviewView(),
    )
    expect(intent).toEqual({
      kind: 'navigate',
      to: `/competitions/${competitionId}/classements`,
    })
  })

  it('navigates Pairing ApplyDraw to the stage workspace', () => {
    const view = overviewView({
      operationalFocus: {
        ...overviewView().operationalFocus,
        draws: [
          {
            stageId,
            drawId,
            kind: 'Pairing',
            status: 'Published',
            resolutionState: 'Resolved',
            isApplied: false,
          },
        ],
      },
    })
    const intent = resolveOverviewActionIntent(
      { code: 'ApplyDraw', guaranteed: false, stageId, drawId },
      view,
    )
    expect(intent).toEqual({ kind: 'navigate', to: `/stages/${stageId}` })
  })

  it('navigates MaterializeFromOccupiedSlots to the stage pairing UI', () => {
    const intent = resolveOverviewActionIntent(
      { code: 'MaterializeFromOccupiedSlots', guaranteed: false, stageId },
      overviewView(),
    )
    expect(intent).toEqual({ kind: 'navigate', to: `/stages/${stageId}` })
  })

    it('executes GenerateNextRound when stageId is provided', () => {
      const intent = resolveOverviewActionIntent(
        { code: 'GenerateNextRound', guaranteed: false, stageId },
        overviewView(),
      )
      expect(intent.kind).toBe('execute')
    })

    it('builds a stable action key from Host ids', () => {
    expect(
      overviewActionKey({
        code: 'PublishDraw',
        guaranteed: false,
        stageId,
        drawId,
      }),
    ).toBe(`PublishDraw:${stageId}:${drawId}::`)
  })
})

describe('situationHref', () => {
  it('prefers Host matchId over Fixture join', () => {
    expect(
      situationHref(
        {
          source: 'ProgressionPending',
          nature: 'Blocking',
          targetType: 'Fixture',
          targetId: 'fixture-id',
          matchId,
          actionable: true,
          actionCode: 'ApplyProgression',
          impactCode: 'BlocksProgression',
          params: {},
        },
        competitionId,
      ),
    ).toBe(`/matches/${matchId}`)
  })
})
