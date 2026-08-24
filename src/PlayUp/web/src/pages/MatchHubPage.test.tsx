import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  ApiError,
  fetchCompetitionOverview,
  fetchMatchesByStage,
} from '../api'
import type { CompetitionOverview, MatchSummary } from '../types'
import { MatchHubPage } from './MatchHubPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchCompetitionOverview: vi.fn(),
    fetchMatchesByStage: vi.fn(),
  }
})

const competitionId = '33333333-3333-3333-3333-333333333333'
const stageId = '22222222-2222-2222-2222-222222222222'
const matchId = '11111111-1111-1111-1111-111111111111'

function overview(
  overrides: Partial<CompetitionOverview> = {},
): CompetitionOverview {
  return {
    id: competitionId,
    name: 'Spring Cup',
    status: 'Running',
    entries: [],
    stages: [{ stageId, name: 'QF', status: 'Running' }],
    ...overrides,
  }
}

function matchSummary(overrides: Partial<MatchSummary> = {}): MatchSummary {
  return {
    matchId,
    stageId,
    status: 'Scheduled',
    home: { entryId: 'home', displayName: 'Alpha' },
    away: { entryId: 'away', displayName: 'Beta' },
    score: null,
    fixtureId: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
    roundId: null,
    ...overrides,
  }
}

function renderMatchHub() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[`/competitions/${competitionId}/matches`]}
      >
        <Routes>
          <Route
            path="/competitions/:competitionId/matches"
            element={<MatchHubPage />}
          />
          <Route
            path="/matches/:matchId"
            element={<p>Match detail route</p>}
          />
          <Route
            path="/competitions/:competitionId"
            element={<p>Workspace route</p>}
          />
          <Route
            path="/competitions/:competitionId/classements"
            element={<p>Classements route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('MatchHubPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading while hub reads are pending', () => {
    vi.mocked(fetchCompetitionOverview).mockReturnValue(new Promise(() => {}))

    renderMatchHub()

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…')
  })

  it('renders empty when stages exist but no matches', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([])

    renderMatchHub()

    expect(await screen.findByText(/Aucun match/i)).toBeInTheDocument()
    expect(screen.getByText(/RAS · tous les résultats/i)).toBeInTheDocument()
  })

  it('renders competition matches grouped in the calendar', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([
      matchSummary({ status: 'Live' }),
    ])

    renderMatchHub()

    expect(await screen.findByText('Calendrier des matchs')).toBeInTheDocument()
    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(screen.getByText('Beta')).toBeInTheDocument()
    expect(screen.getAllByText('QF').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText('En cours').length).toBeGreaterThanOrEqual(1)
  })

  it('shows Read context, scheduled time, score and result type without inventing values', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([
      matchSummary({
        status: 'Finished',
        score: { homeGoals: 2, awayGoals: 1 },
        matchdayNumber: 3,
        resultType: 'Played',
        scheduledAt: '2026-09-01T15:00:00.000Z',
      }),
    ])

    renderMatchHub()

    expect(await screen.findByText('Journée 3')).toBeInTheDocument()
    expect(screen.getByText('2–1')).toBeInTheDocument()
    expect(screen.getByText(/Joué|Played/i)).toBeInTheDocument()
    expect(screen.queryByText('Consultation')).not.toBeInTheDocument()
  })

  it('prefers roundName from the Read over matchday formatting', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([
      matchSummary({
        status: 'Finished',
        score: { homeGoals: 1, awayGoals: 0 },
        roundName: 'Demi-finale',
        matchdayNumber: 99,
        resultType: 'Forfeit',
      }),
    ])

    renderMatchHub()

    expect(await screen.findByText('Demi-finale')).toBeInTheDocument()
    expect(screen.queryByText('Journée 99')).not.toBeInTheDocument()
    expect(screen.getByText(/Forfait|Forfeit/i)).toBeInTheDocument()
  })

  it('does not render a page-level attention card', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([])

    renderMatchHub()

    await screen.findByText(/Aucun match/i)

    expect(screen.queryByText(/Rien à traiter pour le moment/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/Progression en attente/i)).not.toBeInTheDocument()
  })

  it('shows an error when overview read fails', async () => {
    vi.mocked(fetchCompetitionOverview).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    )

    renderMatchHub()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    )
  })

  it('navigates to match detail from a hub row', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([matchSummary()])

    renderMatchHub()

    await user.click(
      await screen.findByRole('link', { name: /Alpha – Beta/i }),
    )

    expect(screen.getByText('Match detail route')).toBeInTheDocument()
  })

  it('navigates back to vue d’ensemble', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([])

    renderMatchHub()

    await user.click(
      await screen.findByRole('link', {
        name: /Vue d'ensemble/i,
      }),
    )

    expect(screen.getByText('Workspace route')).toBeInTheDocument()
  })

  it('links to classements when finished matches exist', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overview())
    vi.mocked(fetchMatchesByStage).mockResolvedValue([
      matchSummary({
        status: 'Finished',
        score: { homeGoals: 1, awayGoals: 0 },
        matchdayNumber: 1,
      }),
    ])

    renderMatchHub()

    const link = await screen.findByRole('link', {
      name: /Voir le classement/i,
    })
    expect(link).toHaveAttribute(
      'href',
      `/competitions/${competitionId}/classements`,
    )
  })
})
