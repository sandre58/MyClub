import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fetchCompetitionOverview, fetchStageOverview } from '../api'
import type { StageDraw, StageOverview, StageSlot } from '../types'
import { getDrawUiProjection, isSlotDrawApplied } from './drawUi'
import { StagePage } from './StagePage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    fetchCompetitionOverview: vi.fn(),
  }
})

const stageId = '22222222-2222-2222-2222-222222222222'
const competitionId = '33333333-3333-3333-3333-333333333333'
const entryA = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const entryB = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

function baseOverview(overrides: Partial<StageOverview> = {}): StageOverview {
  return {
    id: stageId,
    competitionId,
    name: 'QF',
    status: 0,
    rounds: [],
    slots: [],
    draws: [],
    ...overrides,
  }
}

function pairingDraw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
    kind: 2,
    status: 0,
    resolutionState: 1,
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
    id: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
    kind: 0,
    status: 1,
    resolutionState: 1,
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

describe('getDrawUiProjection', () => {
  it('describes draft + not resolved without results', () => {
    const ui = getDrawUiProjection(
      pairingDraw({ resolutionState: 0, pairings: [] }),
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

describe('StagePage draws', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchCompetitionOverview).mockResolvedValue({
      id: competitionId,
      name: 'Dev Seed Cup',
      status: 0,
      entries: [],
      stages: [],
    })
  })

  it('shows pairing result for draft + resolved', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ draws: [pairingDraw()] }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Draw resolved but not published.')).toBeInTheDocument()
    })
    expect(screen.getByRole('heading', { name: 'Pairing draw' })).toBeInTheDocument()
    expect(screen.getByText('Resolved')).toBeInTheDocument()
    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(screen.getByText('Beta')).toBeInTheDocument()
    expect(screen.getByText('vs')).toBeInTheDocument()
  })

  it('hides result for draft + not resolved', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [
          pairingDraw({
            resolutionState: 0,
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
  })

  it('shows Published for published + resolved', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [pairingDraw({ status: 1 })],
      }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Published draw.')).toBeInTheDocument()
    })
    expect(screen.getByText('Published')).toBeInTheDocument()
  })

  it('shows derived Applied when slot placements match stage slots', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          { slotKey: 'SF1-A', entryId: entryA, displayName: 'Alpha' },
        ],
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
    expect(screen.getByRole('heading', { name: 'Placements' })).toBeInTheDocument()
    expect(screen.getAllByText('SF1-A').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('Alpha').length).toBeGreaterThanOrEqual(1)
  })

  it('does not show Applied when slot occupants do not match', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          { slotKey: 'SF1-A', entryId: null, displayName: null },
        ],
        draws: [slotDraw()],
      }),
    )

    renderStagePage()

    await waitFor(() => {
      expect(screen.getByText('Published draw.')).toBeInTheDocument()
    })
    expect(screen.queryByText('Applied')).not.toBeInTheDocument()
  })
})
