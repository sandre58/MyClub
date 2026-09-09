import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  fetchMatchDetail,
  fetchOrganisationView,
  fetchStageOverview,
  finishMatch,
  setRunningScore,
  startMatch,
} from '../api'
import type { MatchDetail, OrganisationView, StageOverview } from '../types'
import { MatchPage } from './MatchPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchMatchDetail: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchOrganisationView: vi.fn(),
    startMatch: vi.fn(),
    finishMatch: vi.fn(),
    setRunningScore: vi.fn(),
  }
})

const matchId = '11111111-1111-1111-1111-111111111111'
const stageId = '22222222-2222-2222-2222-222222222222'
const competitionId = '33333333-3333-3333-3333-333333333333'
const fixtureId = '44444444-4444-4444-4444-444444444444'

function baseMatch(overrides: Partial<MatchDetail> = {}): MatchDetail {
  return {
    matchId,
    competitionId,
    stageId,
    status: 'Scheduled',
    home: { entryId: 'home', displayName: 'Alpha' },
    away: { entryId: 'away', displayName: 'Beta' },
    result: null,
    fixtureId,
    legIndex: 1,
    ...overrides,
  }
}

const stageOverview: StageOverview = {
  id: stageId,
  competitionId,
  name: 'JournÃ©e 1',
  status: 'Draft',
  rounds: [],
  slots: [],
  draws: [],
}

const emptyOrganisation: OrganisationView = {
  competitionId,
  name: 'Spring Cup',
  status: 'Ready',
  participants: {
    activeCount: 2,
    occupyingCount: 2,
    entries: [
      { entryId: 'home', displayName: 'Alpha', status: 'Active', declaredMembers: [] },
      { entryId: 'away', displayName: 'Beta', status: 'Active', declaredMembers: [] },
    ],
  },
  format: {
    kind: null,
    primaryStageId: null,
    primaryStageName: null,
    primaryStageStatus: null,
  },
  regulation: {
    minimumTeams: 2,
    maximumTeams: 64,
    durationPerPeriod: 45,
    numberOfPeriods: 2,
    winPoints: 3,
    drawPoints: 1,
    lossPoints: 0,
  },
  structure: {
    groupCount: 0,
    roundCount: 0,
    matchdayCount: 0,
    slotCount: 0,
    hasDrawRules: false,
    numberOfPots: null,
    matchGenerationFormat: 'SingleRoundRobin',
  },
  actions: [],
  readiness: {
    readyForNextSlice: false,
    readyForDraw: false,
    readyForMaterialization: false,
    readyForSchedule: false,
    readyForMatchOperation: false,
    readyForSchedulePath: false,
    attachedMatchCount: 0,
    blockers: [],
  },
    stages: [],
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

describe('MatchPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview)
    vi.mocked(fetchOrganisationView).mockResolvedValue(emptyOrganisation)
    vi.mocked(startMatch).mockResolvedValue(undefined)
    vi.mocked(finishMatch).mockResolvedValue(undefined)
    vi.mocked(setRunningScore).mockResolvedValue(undefined)
  })

  it('Scheduled: Start and after-the-fact Finish, empty scoreboard, no GUIDs', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({ status: 'Scheduled' }),
    )

    renderMatchPage()

    expect(
      await screen.findByRole('heading', { name: 'Alpha vs Beta' }),
    ).toBeInTheDocument()
    expect(screen.getByText('PlanifiÃ©')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'DÃ©marrer le match' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Terminer le match' }),
    ).toBeInTheDocument()
    expect(screen.queryByText(matchId)).not.toBeInTheDocument()
    expect(screen.queryByText(stageId)).not.toBeInTheDocument()
    expect(screen.queryByText(fixtureId)).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Appliquer la progression' }),
    ).not.toBeInTheDocument()
  })

  it('Start button triggers startMatch mutation', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({ status: 'Scheduled' }),
    )

    renderMatchPage()

    await user.click(
      await screen.findByRole('button', { name: 'DÃ©marrer le match' }),
    )

    await waitFor(() => {
      expect(startMatch).toHaveBeenCalledWith(matchId)
    })
  })

  it('Finish from Scheduled does not Start', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({ status: 'Scheduled' }),
    )

    renderMatchPage()

    await screen.findByRole('button', { name: 'Terminer le match' })

    const homeGoals = screen.getByLabelText(/Alpha â€” rÃ©sultat/i)
    const awayGoals = screen.getByLabelText(/Beta â€” rÃ©sultat/i)
    await user.clear(homeGoals)
    await user.type(homeGoals, '2')
    await user.clear(awayGoals)
    await user.type(awayGoals, '1')
    await user.click(screen.getByRole('button', { name: 'Terminer le match' }))

    await waitFor(() => {
      expect(finishMatch).toHaveBeenCalledWith(matchId, {
        type: 'Played',
        homeGoals: 2,
        awayGoals: 1,
        extraTimePlayed: false,
      })
    })
    expect(startMatch).not.toHaveBeenCalled()
  })

  it('Live: running score update and Finish prefills from the counter', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 3, awayGoals: 1 },
      }),
    )

    renderMatchPage()

    expect(await screen.findByText('En direct')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'DÃ©marrer le match' }),
    ).not.toBeInTheDocument()

    const runningHome = screen.getByLabelText(/Alpha â€” compteur/i)
    const runningAway = screen.getByLabelText(/Beta â€” compteur/i)
    await user.clear(runningHome)
    await user.type(runningHome, '4')
    await user.clear(runningAway)
    await user.type(runningAway, '1')
    await user.click(
      screen.getByRole('button', { name: 'Mettre Ã  jour le compteur' }),
    )

    await waitFor(() => {
      expect(setRunningScore).toHaveBeenCalledWith(matchId, {
        homeGoals: 4,
        awayGoals: 1,
      })
    })

    expect(screen.getByLabelText(/Alpha â€” rÃ©sultat/i)).toHaveValue(3)
    expect(screen.getByLabelText(/Beta â€” rÃ©sultat/i)).toHaveValue(1)

    await user.click(screen.getByRole('button', { name: 'Terminer le match' }))

    await waitFor(() => {
      expect(finishMatch).toHaveBeenCalledWith(matchId, {
        type: 'Played',
        homeGoals: 3,
        awayGoals: 1,
        extraTimePlayed: false,
      })
    })
  })

  it('successful Start refetches and shows Live', async () => {
    const user = userEvent.setup()
    const fetchMatch = vi.mocked(fetchMatchDetail)
    fetchMatch.mockImplementation(async () =>
      baseMatch({ status: 'Scheduled' }),
    )

    renderMatchPage()
    await screen.findByRole('button', { name: 'DÃ©marrer le match' })
    const callsBeforeClick = fetchMatch.mock.calls.length

    fetchMatch.mockImplementation(async () =>
      baseMatch({
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 0, awayGoals: 0 },
      }),
    )

    await user.click(screen.getByRole('button', { name: 'DÃ©marrer le match' }))

    await waitFor(() => {
      expect(startMatch).toHaveBeenCalledWith(matchId)
      expect(screen.getByText('En direct')).toBeInTheDocument()
    })
    expect(fetchMatch.mock.calls.length).toBeGreaterThan(callsBeforeClick)
  })

  it('Finished: official score, no Cup progression, no GUIDs', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({
        status: 'Finished',
        result: {
          type: 'Played',
          homeGoals: 2,
          awayGoals: 1,
          extraTimePlayed: false,
          shootout: null,
        },
      }),
    )

    renderMatchPage()

    expect(
      await screen.findByRole('heading', { name: 'Alpha vs Beta' }),
    ).toBeInTheDocument()
    expect(screen.getByText('RÃ©sultat officiel')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Terminer le match' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Appliquer la progression' }),
    ).not.toBeInTheDocument()
    expect(screen.queryByText(matchId)).not.toBeInTheDocument()
    expect(screen.queryByText(competitionId)).not.toBeInTheDocument()
    expect(screen.queryByText(fixtureId)).not.toBeInTheDocument()
  })
})
