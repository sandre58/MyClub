import { describe, expect, it } from 'vitest'
import {
  cockpitActionKey,
  resolveCockpitActionIntent,
} from './cockpitActions'
import { cockpitIds, cockpitView } from '../test/cockpitFixtures'
import { situationHref } from './cockpitNavigation'

const { competitionId, stageId, drawId, matchId } = cockpitIds

describe('resolveCockpitActionIntent', () => {
  it('executes PrepareStage when stageId is provided by the Read', () => {
    const view = cockpitView()
    const intent = resolveCockpitActionIntent(
      { code: 'PrepareStage', guaranteed: false, stageId },
      view,
    )
    expect(intent.kind).toBe('execute')
  })

  it('navigates organisation actions instead of inventing POST bodies', () => {
    const intent = resolveCockpitActionIntent(
      { code: 'AddEntry', guaranteed: false },
      cockpitView(),
    )
    expect(intent).toEqual({
      kind: 'navigate',
      to: `/competitions/${competitionId}/organisation`,
    })
  })

  it('navigates Pairing ApplyDraw to the stage workspace', () => {
    const view = cockpitView({
      operationalFocus: {
        ...cockpitView().operationalFocus,
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
    const intent = resolveCockpitActionIntent(
      { code: 'ApplyDraw', guaranteed: false, stageId, drawId },
      view,
    )
    expect(intent).toEqual({ kind: 'navigate', to: `/stages/${stageId}` })
  })

  it('builds a stable action key from Host ids', () => {
    expect(
      cockpitActionKey({
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
          actionCode: null,
          params: {},
        },
        competitionId,
      ),
    ).toBe(`/matches/${matchId}`)
  })
})
