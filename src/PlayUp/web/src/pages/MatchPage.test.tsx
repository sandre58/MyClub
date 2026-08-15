import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  fetchMatchDetail,
  fetchStageOverview,
  finishMatch,
  startMatch,
} from '../api'
import type { MatchDetail, StageOverview } from '../types'
import { MatchPage } from './MatchPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchMatchDetail: vi.fn(),
    fetchStageOverview: vi.fn(),
    startMatch: vi.fn(),
    finishMatch: vi.fn(),
    applyProgressionOutcome: vi.fn(),
  }
})

const matchId = '11111111-1111-1111-1111-111111111111'
const stageId = '22222222-2222-2222-2222-222222222222'

function baseMatch(overrides: Partial<MatchDetail> = {}): MatchDetail {
  return {
    matchId,
    competitionId: '33333333-3333-3333-3333-333333333333',
    stageId,
    status: 0,
    home: { entryId: 'home', displayName: 'Alpha' },
    away: { entryId: 'away', displayName: 'Beta' },
    result: null,
    fixtureId: '44444444-4444-4444-4444-444444444444',
    legIndex: 1,
    ...overrides,
  }
}

const stageOverview: StageOverview = {
  id: stageId,
  competitionId: '33333333-3333-3333-3333-333333333333',
  name: 'QF',
  status: 0,
  rounds: [],
  slots: [
    { slotKey: 'SF1-A', entryId: null, displayName: null },
  ],
  draws: [],
}

function renderMatchPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/matches/${matchId}`]}>
        <Routes>
          <Route path="/matches/:matchId" element={<MatchPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return { queryClient }
}

describe('MatchPage command loop', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview)
    vi.mocked(startMatch).mockResolvedValue(undefined)
    vi.mocked(finishMatch).mockResolvedValue(undefined)
  })

  it('Start button triggers startMatch mutation', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockResolvedValue(baseMatch({ status: 0 }))

    renderMatchPage()

    const startButton = await screen.findByRole('button', {
      name: 'Start match',
    })
    await user.click(startButton)

    await waitFor(() => {
      expect(startMatch).toHaveBeenCalledWith(matchId)
    })
  })

  it('Finish form submits the FinishMatch request body', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockResolvedValue(baseMatch({ status: 1 }))

    renderMatchPage()

    await screen.findByRole('button', { name: 'Finish match' })

    const homeGoals = screen.getByLabelText(/Alpha goals/i)
    const awayGoals = screen.getByLabelText(/Beta goals/i)
    await user.clear(homeGoals)
    await user.type(homeGoals, '2')
    await user.clear(awayGoals)
    await user.type(awayGoals, '1')
    await user.click(screen.getByRole('button', { name: 'Finish match' }))

    await waitFor(() => {
      expect(finishMatch).toHaveBeenCalledWith(matchId, {
        type: 0,
        homeGoals: 2,
        awayGoals: 1,
        extraTimePlayed: false,
      })
    })
  })

  it('successful Start invalidates then refetches the match', async () => {
    const user = userEvent.setup()
    const fetchMatch = vi.mocked(fetchMatchDetail)
    fetchMatch.mockImplementation(async () => baseMatch({ status: 0 }))

    renderMatchPage()
    await screen.findByRole('button', { name: 'Start match' })
    const callsBeforeClick = fetchMatch.mock.calls.length

    // After Start succeeds, invalidation refetches — return Live from then on.
    fetchMatch.mockImplementation(async () => baseMatch({ status: 1 }))

    await user.click(screen.getByRole('button', { name: 'Start match' }))

    await waitFor(() => {
      expect(startMatch).toHaveBeenCalledWith(matchId)
      expect(screen.getByText('Live', { exact: true })).toBeInTheDocument()
    })
    expect(fetchMatch.mock.calls.length).toBeGreaterThan(callsBeforeClick)
  })
})
