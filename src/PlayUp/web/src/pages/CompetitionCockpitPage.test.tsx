import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  ApiError,
  fetchCompetitionCockpit,
  prepareStage,
} from '../api'
import { CompetitionCockpitPage } from './CompetitionCockpitPage'
import { cockpitIds, cockpitView } from '../test/cockpitFixtures'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchCompetitionCockpit: vi.fn(),
    prepareStage: vi.fn(),
  }
})

const { competitionId, stageId } = cockpitIds

function renderCockpitPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/competitions/${competitionId}`]}>
        <Routes>
          <Route
            path="/competitions/:competitionId"
            element={<CompetitionCockpitPage />}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation route</p>}
          />
          <Route
            path="/competitions/:competitionId/matches"
            element={<p>Match hub route</p>}
          />
          <Route
            path="/competitions/:competitionId/overview"
            element={<p>Overview route</p>}
          />
          <Route path="/stages/:stageId" element={<p>Stage route</p>} />
          <Route path="/matches/:matchId" element={<p>Match route</p>} />
          <Route path="/competitions" element={<p>List route</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CompetitionCockpitPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading while the cockpit is pending', () => {
    vi.mocked(fetchCompetitionCockpit).mockReturnValue(new Promise(() => {}))

    renderCockpitPage()

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…')
  })

  it('renders cycle, dimensions, progression and closure from the Host DTO', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        situations: [
          {
            source: 'InsufficientParticipants',
            nature: 'Blocking',
            targetType: 'Organisation',
            targetId: competitionId,
            matchId: null,
            actionCode: 'AddEntry',
            params: { minimumTeams: '2', activeCount: '1' },
          },
        ],
        attentionSummary: {
          count: 1,
          items: [
            {
              source: 'InsufficientParticipants',
              nature: 'Blocking',
              targetType: 'Organisation',
              targetId: competitionId,
              matchId: null,
              actionCode: 'AddEntry',
              params: { minimumTeams: '2', activeCount: '1' },
            },
          ],
        },
        availableActions: [
          { code: 'AddEntry', guaranteed: false },
          {
            code: 'PrepareStage',
            guaranteed: false,
            stageId,
            params: { stageName: 'Phase 1' },
          },
        ],
        naturalProgression: { code: 'ContinueOrganisation' },
        closureHint: {
          canCompleteNormally: false,
          blockerCodes: ['ScheduledMatches'],
        },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Spring Cup' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Lecture de cycle' }),
    ).toBeInTheDocument()
    expect(screen.getAllByText('Construction').length).toBeGreaterThan(0)
    expect(screen.getByText('Équipes')).toBeInTheDocument()
    expect(screen.getByText('Structure')).toBeInTheDocument()
    expect(screen.getByText('Règlement')).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Matchs', level: 3 }),
    ).toBeInTheDocument()
    expect(screen.getAllByText('Participants insuffisants').length).toBeGreaterThan(0)
    expect(screen.getByText('Continuer la préparation')).toBeInTheDocument()
    expect(screen.getByText(/Non appliqué/)).toBeInTheDocument()
    expect(
      screen.getByText('Clôture normale impossible pour le moment'),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/Des matchs sont encore planifiés/),
    ).toBeInTheDocument()

    // Action comes from availableActions — not reconstructed from status.
    expect(
      screen.getByRole('button', { name: /Préparer la phase/i }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Ajouter une équipe/i }),
    ).toBeInTheDocument()
  })

  it('does not invent actions absent from availableActions', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        operationalFocus: {
          ...cockpitView().operationalFocus,
          stages: [{ stageId, name: 'Phase 1', status: 'Ready' }],
        },
        availableActions: [],
      }),
    )

    renderCockpitPage()

    await screen.findByRole('heading', { name: 'Spring Cup' })
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument()
  })

  it('uses Host isApplied and does not recompute draw applied state', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        operationalFocus: {
          ...cockpitView().operationalFocus,
          draws: [
            {
              stageId,
              drawId: cockpitIds.drawId,
              kind: 'Slot',
              status: 'Published',
              resolutionState: 'Resolved',
              isApplied: true,
            },
          ],
        },
      }),
    )

    renderCockpitPage()

    expect(await screen.findByText(/Appliqué/)).toBeInTheDocument()
  })

  it('navigates Fixture targets via Host matchId without client join', async () => {
    const user = userEvent.setup()
    const fixtureId = 'ffffffff-ffff-ffff-ffff-ffffffffffff'
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        situations: [
          {
            source: 'ProgressionPending',
            nature: 'Blocking',
            targetType: 'Fixture',
            targetId: fixtureId,
            matchId: cockpitIds.matchId,
            actionCode: 'ApplyProgression',
            params: {},
          },
        ],
      }),
    )

    renderCockpitPage()

    await user.click(await screen.findByRole('link', { name: 'Ouvrir' }))
    expect(screen.getByText('Match route')).toBeInTheDocument()
  })

  it('executes a projected action and invalidates the cockpit query', async () => {
    const user = userEvent.setup()
    vi.mocked(prepareStage).mockResolvedValue(undefined)
    vi.mocked(fetchCompetitionCockpit)
      .mockResolvedValueOnce(
        cockpitView({
          availableActions: [
            {
              code: 'PrepareStage',
              guaranteed: false,
              stageId,
              params: { stageName: 'Phase 1' },
            },
          ],
        }),
      )
      .mockResolvedValueOnce(
        cockpitView({
          availableActions: [
            {
              code: 'StartStage',
              guaranteed: false,
              stageId,
              params: { stageName: 'Phase 1' },
            },
          ],
          operationalFocus: {
            ...cockpitView().operationalFocus,
            stages: [{ stageId, name: 'Phase 1', status: 'Ready' }],
          },
        }),
      )

    renderCockpitPage()

    await user.click(
      await screen.findByRole('button', { name: /Préparer la phase/i }),
    )

    await waitFor(() => {
      expect(prepareStage).toHaveBeenCalledWith(stageId)
    })

    expect(
      await screen.findByRole('button', { name: /Démarrer la phase/i }),
    ).toBeInTheDocument()
  })

  it('shows an error when the cockpit read fails', async () => {
    vi.mocked(fetchCompetitionCockpit).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    )

    renderCockpitPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    )
  })

  it('navigates to organisation from spaces', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(cockpitView())

    renderCockpitPage()

    const links = await screen.findAllByRole('link', { name: /Organisation/i })
    await user.click(links[links.length - 1])

    expect(screen.getByText('Organisation route')).toBeInTheDocument()
  })
})
