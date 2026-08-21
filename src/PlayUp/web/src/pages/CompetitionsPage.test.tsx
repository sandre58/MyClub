import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, createCompetition, fetchCompetitions } from '../api'
import type { CompetitionListItem, WorkspaceSummary } from '../types'
import { CompetitionsPage } from './CompetitionsPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchCompetitions: vi.fn(),
    createCompetition: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'

function listItem(
  overrides: Partial<CompetitionListItem> = {},
): CompetitionListItem {
  return {
    id: competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    ...overrides,
  }
}

function createdSummary(
  overrides: Partial<WorkspaceSummary> = {},
): WorkspaceSummary {
  return {
    id: competitionId,
    name: 'New Cup',
    status: 'Draft',
    nextActionCode: 'ContinueOrganisation',
    attentionCount: 0,
    completionMode: null,
    canCompleteNormally: false,
    completionBlockers: null,
    ...overrides,
  }
}

function renderCompetitionsPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/competitions']}>
        <Routes>
          <Route path="/competitions" element={<CompetitionsPage />} />
          <Route
            path="/competitions/:competitionId"
            element={<p>Cockpit route</p>}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation route</p>}
          />
          <Route
            path="/competitions/:competitionId/overview"
            element={<p>Overview route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CompetitionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading while the list is pending', () => {
    vi.mocked(fetchCompetitions).mockReturnValue(new Promise(() => {}))

    renderCompetitionsPage()

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…')
  })

  it('shows an empty state with create CTA when the Host returns no competitions', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([])

    renderCompetitionsPage()

    expect(
      await screen.findByText(/Aucune compétition/i),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Créer une compétition/i }),
    ).toBeInTheDocument()
    expect(screen.getByLabelText(/^Nom$/i)).toBeInTheDocument()
  })

  it('renders competition rows with string status labels', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([
      listItem({ status: 'Running' }),
      listItem({
        id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        name: 'Autumn League',
        status: 'Ready',
      }),
    ])

    renderCompetitionsPage()

    expect(
      await screen.findByRole('link', { name: /Spring Cup/i }),
    ).toBeInTheDocument()
    expect(screen.getByText('En cours')).toBeInTheDocument()
    expect(screen.getByText('Autumn League')).toBeInTheDocument()
    expect(screen.getByText('Prêt')).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: /Nouvelle compétition/i }),
    ).toBeInTheDocument()
  })

  it('navigates to the competition cockpit on row click', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitions).mockResolvedValue([listItem()])

    renderCompetitionsPage()

    await user.click(
      await screen.findByRole('link', { name: /Spring Cup/i }),
    )

    expect(screen.getByText('Cockpit route')).toBeInTheDocument()
  })

  it('shows an error when the list read fails', async () => {
    vi.mocked(fetchCompetitions).mockRejectedValue(
      new ApiError(500, 'Host unavailable'),
    )

    renderCompetitionsPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Host unavailable (500)',
    )
  })

  it('creates a competition and navigates to Organisation', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitions).mockResolvedValue([])
    vi.mocked(createCompetition).mockResolvedValue(
      createdSummary({ name: 'Tournoi printemps' }),
    )

    renderCompetitionsPage()

    const nameInput = await screen.findByLabelText(/^Nom$/i)
    await user.type(nameInput, 'Tournoi printemps')
    await user.click(screen.getByRole('button', { name: /^Créer$/i }))

    await waitFor(() => {
      expect(createCompetition).toHaveBeenCalledWith({
        name: 'Tournoi printemps',
      })
    })

    expect(await screen.findByText('Organisation route')).toBeInTheDocument()
    expect(screen.queryByText('Overview route')).not.toBeInTheDocument()
    expect(screen.queryByText('Cockpit route')).not.toBeInTheDocument()
  })

  it('shows API error and does not navigate on create failure', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitions).mockResolvedValue([])
    vi.mocked(createCompetition).mockRejectedValue(
      new ApiError(400, 'Competition name cannot be empty.'),
    )

    renderCompetitionsPage()

    await user.type(await screen.findByLabelText(/^Nom$/i), 'X')
    await user.click(screen.getByRole('button', { name: /^Créer$/i }))

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.queryByText('Organisation route')).not.toBeInTheDocument()
  })

  it('disables submit while create is pending (no double submit)', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitions).mockResolvedValue([])
    let resolveCreate: (value: WorkspaceSummary) => void = () => {}
    vi.mocked(createCompetition).mockReturnValue(
      new Promise((resolve) => {
        resolveCreate = resolve
      }),
    )

    renderCompetitionsPage()

    await user.type(await screen.findByLabelText(/^Nom$/i), 'Pending Cup')
    await user.click(screen.getByRole('button', { name: /^Créer$/i }))

    expect(
      await screen.findByRole('button', { name: /Création/i }),
    ).toBeDisabled()
    expect(createCompetition).toHaveBeenCalledTimes(1)

    resolveCreate(createdSummary({ name: 'Pending Cup' }))
    expect(await screen.findByText('Organisation route')).toBeInTheDocument()
  })

  it('keeps submit disabled when the name is blank', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([])

    renderCompetitionsPage()

    expect(await screen.findByRole('button', { name: /^Créer$/i })).toBeDisabled()
  })
})
