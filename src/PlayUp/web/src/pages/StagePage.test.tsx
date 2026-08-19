import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  applyDraw,
  fetchCompetitionOverview,
  fetchStageOverview,
  prepareStage,
  publishDraw,
  startStage,
} from '../api'
import type { StageDraw, StageOverview, StageRound, StageSlot } from '../types'
import {
  getDrawUiProjection,
  isPairingDrawApplied,
  isSlotDrawApplied,
  resolvePairingFixtureIds,
} from './drawUi'
import { StagePage } from './StagePage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    fetchCompetitionOverview: vi.fn(),
    prepareStage: vi.fn(),
    startStage: vi.fn(),
    publishDraw: vi.fn(),
    applyDraw: vi.fn(),
  }
})

const stageId = '22222222-2222-2222-2222-222222222222'
const competitionId = '33333333-3333-3333-3333-333333333333'
const drawId = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
const slotDrawId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
const fixtureId = 'ffffffff-ffff-ffff-ffff-ffffffffffff'
const entryA = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const entryB = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

function baseOverview(overrides: Partial<StageOverview> = {}): StageOverview {
  return {
    id: stageId,
    competitionId,
    name: 'QF',
    status: 'Draft',
    rounds: [],
    slots: [],
    draws: [],
    ...overrides,
  }
}

function pairingDraw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: drawId,
    kind: 'Pairing',
    status: 'Draft',
    resolutionState: 'Resolved',
    pairings: [
      {
        entryAId: entryA,
        entryADisplayName: 'Alpha',
        entryBId: entryB,
        entryBDisplayName: 'Beta',
      },
    ],
    slotPlacements: [],
    ...overrides,
  }
}

function slotDraw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: slotDrawId,
    kind: 'Slot',
    status: 'Published',
    resolutionState: 'Resolved',
    pairings: [],
    slotPlacements: [
      {
        slotKey: 'SF1-A',
        entryId: entryA,
        displayName: 'Alpha',
      },
    ],
    ...overrides,
  }
}

function oneEmptyFixtureRound(): StageRound[] {
  return [
    {
      id: 'rrrrrrrr-rrrr-rrrr-rrrr-rrrrrrrrrrrr',
      name: 'R1',
      fixtures: [
        {
          id: fixtureId,
          slotAKey: null,
          slotBKey: null,
          attachments: [],
        },
      ],
    },
  ]
}

function renderStagePage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/stages/${stageId}`]}>
        <Routes>
          <Route path="/stages/:stageId" element={<StagePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('isSlotDrawApplied', () => {
  const slotsOccupied: StageSlot[] = [
    { slotKey: 'SF1-A', entryId: entryA, displayName: 'Alpha' },
  ]
  const slotsEmpty: StageSlot[] = [
    { slotKey: 'SF1-A', entryId: null, displayName: null },
  ]
  const slotsWrong: StageSlot[] = [
    { slotKey: 'SF1-A', entryId: entryB, displayName: 'Beta' },
  ]

  it('is true when every placement matches the stage slot occupant', () => {
    expect(isSlotDrawApplied(slotDraw(), slotsOccupied)).toBe(true)
  })

  it('is false when a target slot is empty', () => {
    expect(isSlotDrawApplied(slotDraw(), slotsEmpty)).toBe(false)
  })

  it('is false when a slot has a different entry', () => {
    expect(isSlotDrawApplied(slotDraw(), slotsWrong)).toBe(false)
  })
})

describe('resolvePairingFixtureIds', () => {
  it('maps pairing[i] to fixture[i] when counts match', () => {
    expect(
      resolvePairingFixtureIds(pairingDraw(), oneEmptyFixtureRound()),
    ).toEqual([fixtureId])
  })

  it('returns null when counts differ', () => {
    expect(resolvePairingFixtureIds(pairingDraw(), [])).toBeNull()
  })
})

describe('isPairingDrawApplied', () => {
  it('is true when each mapped fixture already has an attachment', () => {
    const rounds: StageRound[] = [
      {
        id: 'r1',
        name: 'R1',
        fixtures: [
          {
            id: fixtureId,
            slotAKey: null,
            slotBKey: null,
            attachments: [{ matchId: 'm1', legIndex: 1 }],
          },
        ],
      },
    ]
    expect(isPairingDrawApplied(pairingDraw({ status: 'Published' }), rounds)).toBe(true)
  })

  it('is false when a target fixture has no attachment', () => {
    expect(
      isPairingDrawApplied(pairingDraw({ status: 'Published' }), oneEmptyFixtureRound()),
    ).toBe(false)
  })
})

describe('getDrawUiProjection', () => {
  it('describes draft + not resolved without results', () => {
    const ui = getDrawUiProjection(
      pairingDraw({ resolutionState: 'NotResolved', pairings: [] }),
      [],
    )
    expect(ui.message).toMatch(/preparation/i)
    expect(ui.showResults).toBe(false)
  })

  it('describes draft + resolved as not published', () => {
    const ui = getDrawUiProjection(pairingDraw(), [])
    expect(ui.message).toMatch(/not published/i)
    expect(ui.showResults).toBe(true)
  })
})

describe('StagePage prepare', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchCompetitionOverview).mockResolvedValue({
      id: competitionId,
      name: 'Dev Seed Cup',
      status: 'Draft',
      entries: [],
      stages: [],
    })
    vi.mocked(prepareStage).mockResolvedValue(undefined)
    vi.mocked(startStage).mockResolvedValue(undefined)
    vi.mocked(publishDraw).mockResolvedValue(undefined)
    vi.mocked(applyDraw).mockResolvedValue(undefined)
  })

  it('shows Prepare stage when status is Draft', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Draft' }))

    renderStagePage()

    expect(
      await screen.findByRole('button', { name: 'Prepare stage' }),
    ).toBeEnabled()
    expect(screen.getByText('Brouillon')).toBeInTheDocument()
  })

  it('hides Prepare stage when status is Ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Ready' }))

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Prêt')).toBeInTheDocument()
    })
    expect(
      screen.queryByRole('button', { name: /Prepare stage/i }),
    ).not.toBeInTheDocument()
  })

  it('hides Prepare stage when status is Running', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Running' }))

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('En cours')).toBeInTheDocument()
    })
    expect(
      screen.queryByRole('button', { name: /Prepare stage/i }),
    ).not.toBeInTheDocument()
  })

  it('Prepare calls prepareStage and shows Ready after refetch', async () => {
    const user = userEvent.setup()
    let prepared = false

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({ status: prepared ? 'Ready' : 'Draft' }),
    )
    vi.mocked(prepareStage).mockImplementation(async () => {
      prepared = true
    })

    renderStagePage()
    await user.click(
      await screen.findByRole('button', { name: 'Prepare stage' }),
    )

    await waitFor(() => {
      expect(prepareStage).toHaveBeenCalledWith(stageId)
      expect(screen.getByText('Prêt')).toBeInTheDocument()
    })
    expect(
      screen.queryByRole('button', { name: /Prepare stage/i }),
    ).not.toBeInTheDocument()
  })

  it('disables Prepare while pending', async () => {
    const user = userEvent.setup()
    let resolvePrepare!: () => void
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Draft' }))
    vi.mocked(prepareStage).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolvePrepare = () => resolve(undefined)
        }),
    )

    renderStagePage()
    await user.click(
      await screen.findByRole('button', { name: 'Prepare stage' }),
    )

    expect(
      await screen.findByRole('button', { name: 'Preparing stage…' }),
    ).toBeDisabled()

    resolvePrepare()
    await waitFor(() => {
      expect(prepareStage).toHaveBeenCalledWith(stageId)
    })
  })

  it('shows Prepare error and keeps Draft with button usable', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Draft' }))
    vi.mocked(prepareStage).mockRejectedValue(new Error('Prepare blocked'))

    renderStagePage()
    await user.click(
      await screen.findByRole('button', { name: 'Prepare stage' }),
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Prepare blocked')
    expect(screen.getByText('Brouillon')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Prepare stage' }),
    ).toBeEnabled()
  })
})

describe('StagePage start', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchCompetitionOverview).mockResolvedValue({
      id: competitionId,
      name: 'Dev Seed Cup',
      status: 'Draft',
      entries: [],
      stages: [],
    })
    vi.mocked(prepareStage).mockResolvedValue(undefined)
    vi.mocked(startStage).mockResolvedValue(undefined)
    vi.mocked(publishDraw).mockResolvedValue(undefined)
    vi.mocked(applyDraw).mockResolvedValue(undefined)
  })

  it('shows Start stage when status is Ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Ready' }))

    renderStagePage()

    expect(
      await screen.findByRole('button', { name: 'Start stage' }),
    ).toBeEnabled()
    expect(screen.getByText('Prêt')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Prepare stage/i }),
    ).not.toBeInTheDocument()
  })

  it('hides Start stage when status is Draft', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Draft' }))

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Brouillon')).toBeInTheDocument()
    })
    expect(
      screen.queryByRole('button', { name: /Start stage/i }),
    ).not.toBeInTheDocument()
  })

  it('hides Start stage when status is Running', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Running' }))

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('En cours')).toBeInTheDocument()
    })
    expect(
      screen.queryByRole('button', { name: /Start stage/i }),
    ).not.toBeInTheDocument()
  })

  it('hides Start stage when status is Completed', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Completed' }))

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Terminé')).toBeInTheDocument()
    })
    expect(
      screen.queryByRole('button', { name: /Start stage/i }),
    ).not.toBeInTheDocument()
  })

  it('Start calls startStage and shows Running after refetch', async () => {
    const user = userEvent.setup()
    let started = false

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({ status: started ? 'Running' : 'Ready' }),
    )
    vi.mocked(startStage).mockImplementation(async () => {
      started = true
    })

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Start stage' }))

    await waitFor(() => {
      expect(startStage).toHaveBeenCalledWith(stageId)
      expect(screen.getByText('En cours')).toBeInTheDocument()
    })
    expect(
      screen.queryByRole('button', { name: /Start stage/i }),
    ).not.toBeInTheDocument()
  })

  it('disables Start while pending', async () => {
    const user = userEvent.setup()
    let resolveStart!: () => void
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Ready' }))
    vi.mocked(startStage).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveStart = () => resolve(undefined)
        }),
    )

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Start stage' }))

    expect(
      await screen.findByRole('button', { name: 'Starting stage…' }),
    ).toBeDisabled()

    resolveStart()
    await waitFor(() => {
      expect(startStage).toHaveBeenCalledWith(stageId)
    })
  })

  it('shows Start error and keeps Ready with button usable', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchStageOverview).mockResolvedValue(baseOverview({ status: 'Ready' }))
    vi.mocked(startStage).mockRejectedValue(new Error('Start blocked'))

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Start stage' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Start blocked')
    expect(screen.getByText('Prêt')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Start stage' })).toBeEnabled()
  })
})

describe('StagePage draws', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchCompetitionOverview).mockResolvedValue({
      id: competitionId,
      name: 'Dev Seed Cup',
      status: 'Draft',
      entries: [],
      stages: [],
    })
    vi.mocked(prepareStage).mockResolvedValue(undefined)
    vi.mocked(startStage).mockResolvedValue(undefined)
    vi.mocked(publishDraw).mockResolvedValue(undefined)
    vi.mocked(applyDraw).mockResolvedValue(undefined)
  })

  it('shows pairing result for draft + resolved without Apply', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ draws: [pairingDraw()] }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(
        screen.getByText('Draw resolved but not published.'),
      ).toBeInTheDocument()
    })
    expect(
      screen.getByRole('heading', { name: /Appariement draw/i }),
    ).toBeInTheDocument()
    expect(screen.getByText('Résolu')).toBeInTheDocument()
    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(screen.getByText('Beta')).toBeInTheDocument()
    expect(screen.getByText('vs')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Publish draw' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Apply draw/i }),
    ).not.toBeInTheDocument()
  })

  it('hides Publish and Apply for draft + not resolved', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [
          pairingDraw({
            resolutionState: 'NotResolved',
            pairings: [],
          }),
        ],
      }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(
        screen.getByText('Draw in preparation — no result yet.'),
      ).toBeInTheDocument()
    })
    expect(screen.queryByText('Result')).not.toBeInTheDocument()
    expect(screen.queryByText('Alpha')).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Publish draw/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Apply draw/i }),
    ).not.toBeInTheDocument()
  })

  it('shows No solution without Publish or Apply', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [
          pairingDraw({
            resolutionState: 'NoSolution',
            pairings: [],
          }),
        ],
      }),
    )

    renderStagePage()

    expect(
      await screen.findByText(
        'No admissible solution was found for this draw.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByText('Aucune solution')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Publish draw/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Apply draw/i }),
    ).not.toBeInTheDocument()
  })

  it('shows Cancelled without Publish or Apply', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [
          pairingDraw({
            status: 'Cancelled',
            resolutionState: 'Resolved',
          }),
        ],
      }),
    )

    renderStagePage()

    expect(
      await screen.findByText(
        'This draw was cancelled. A new draw is required to run again.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByText('Annulé')).toBeInTheDocument()
    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Publish draw/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Apply draw/i }),
    ).not.toBeInTheDocument()
  })

  it('shows Published with Apply when not yet applied', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [pairingDraw({ status: 'Published' })],
        rounds: oneEmptyFixtureRound(),
      }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Published draw.')).toBeInTheDocument()
    })
    expect(screen.getByText('Publié')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Publish draw/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Apply draw' }),
    ).toBeInTheDocument()
  })

  it('shows derived Applied when slot placements match stage slots', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [{ slotKey: 'SF1-A', entryId: entryA, displayName: 'Alpha' }],
        draws: [slotDraw()],
      }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Applied')).toBeInTheDocument()
    })
    expect(
      screen.getByText(
        'Published draw. Placements match the current stage slots.',
      ),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Placements' }),
    ).toBeInTheDocument()
    expect(screen.getAllByText('SF1-A').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('Alpha').length).toBeGreaterThanOrEqual(1)
    expect(
      screen.queryByRole('button', { name: /Apply draw/i }),
    ).not.toBeInTheDocument()
  })

  it('does not show Applied when slot occupants do not match', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [{ slotKey: 'SF1-A', entryId: null, displayName: null }],
        draws: [slotDraw()],
      }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Published draw.')).toBeInTheDocument()
    })
    expect(screen.queryByText('Applied')).not.toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Apply draw' }),
    ).toBeInTheDocument()
  })

  it('Publish calls publishDraw and shows Published after refetch', async () => {
    const user = userEvent.setup()
    let published = false

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({
        draws: [pairingDraw({ status: published ? 'Published' : 'Draft' })],
        rounds: oneEmptyFixtureRound(),
      }),
    )
    vi.mocked(publishDraw).mockImplementation(async () => {
      published = true
    })

    renderStagePage()
    const publishButton = await screen.findByRole('button', {
      name: 'Publish draw',
    })
    await user.click(publishButton)

    await waitFor(() => {
      expect(publishDraw).toHaveBeenCalledWith(stageId, drawId)
      expect(screen.getByText('Publié')).toBeInTheDocument()
      expect(screen.getByText('Published draw.')).toBeInTheDocument()
    })
  })

  it('disables Publish while pending', async () => {
    const user = userEvent.setup()
    let resolvePublish!: () => void
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ draws: [pairingDraw()] }),
    )
    vi.mocked(publishDraw).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolvePublish = () => resolve(undefined)
        }),
    )

    renderStagePage()
    const publishButton = await screen.findByRole('button', {
      name: 'Publish draw',
    })
    await user.click(publishButton)

    expect(
      await screen.findByRole('button', { name: 'Publishing draw…' }),
    ).toBeDisabled()

    resolvePublish()
    await waitFor(() => {
      expect(publishDraw).toHaveBeenCalled()
    })
  })

  it('shows Publish error message', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ draws: [pairingDraw()] }),
    )
    vi.mocked(publishDraw).mockRejectedValue(new Error('Publish blocked'))

    renderStagePage()
    await user.click(
      await screen.findByRole('button', { name: 'Publish draw' }),
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Publish blocked')
  })

  it('Apply Slot confirms then posts empty fixtureIds and shows Applied', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    let applied = false

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: applied ? entryA : null,
            displayName: applied ? 'Alpha' : null,
          },
        ],
        draws: [slotDraw()],
      }),
    )
    vi.mocked(applyDraw).mockImplementation(async () => {
      applied = true
    })

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Apply draw' }))

    expect(confirmSpy).toHaveBeenCalled()
    await waitFor(() => {
      expect(applyDraw).toHaveBeenCalledWith(stageId, slotDrawId, {
        fixtureIds: [],
      })
      expect(screen.getByText('Applied')).toBeInTheDocument()
    })

    confirmSpy.mockRestore()
  })

  it('disables Apply while pending', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    let resolveApply!: () => void

    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [{ slotKey: 'SF1-A', entryId: null, displayName: null }],
        draws: [slotDraw()],
      }),
    )
    vi.mocked(applyDraw).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveApply = () => resolve(undefined)
        }),
    )

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Apply draw' }))

    expect(
      await screen.findByRole('button', { name: 'Applying draw…' }),
    ).toBeDisabled()

    resolveApply()
    await waitFor(() => {
      expect(applyDraw).toHaveBeenCalled()
    })

    confirmSpy.mockRestore()
  })

  it('shows Apply error message', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [{ slotKey: 'SF1-A', entryId: null, displayName: null }],
        draws: [slotDraw()],
      }),
    )
    vi.mocked(applyDraw).mockRejectedValue(new Error('Apply blocked'))

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Apply draw' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Apply blocked')

    confirmSpy.mockRestore()
  })

  it('cancelling Apply confirmation does not call applyDraw', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)

    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [{ slotKey: 'SF1-A', entryId: null, displayName: null }],
        draws: [slotDraw()],
      }),
    )

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Apply draw' }))

    expect(confirmSpy).toHaveBeenCalled()
    expect(applyDraw).not.toHaveBeenCalled()

    confirmSpy.mockRestore()
  })

  it('Apply Pairing posts 1:1 fixtureIds after confirmation', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [pairingDraw({ status: 'Published' })],
        rounds: oneEmptyFixtureRound(),
      }),
    )

    renderStagePage()
    await user.click(await screen.findByRole('button', { name: 'Apply draw' }))

    await waitFor(() => {
      expect(applyDraw).toHaveBeenCalledWith(stageId, drawId, {
        fixtureIds: [fixtureId],
      })
    })

    confirmSpy.mockRestore()
  })

  it('hides Apply for published Pairing when fixtures already have attachments', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [pairingDraw({ status: 'Published' })],
        rounds: [
          {
            id: 'r1',
            name: 'R1',
            fixtures: [
              {
                id: fixtureId,
                slotAKey: null,
                slotBKey: null,
                attachments: [{ matchId: 'm1', legIndex: 1 }],
              },
            ],
          },
        ],
      }),
    )

    renderStagePage()

    expect(
      await screen.findByText(
        'Published draw. Target fixtures already have attached matches.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByText('Applied')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Apply draw/i }),
    ).not.toBeInTheDocument()
  })
})
