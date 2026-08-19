import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  fetchCompetitionOverview,
  fetchCompetitions,
  fetchMatchDetail,
  fetchMatchesByStage,
  fetchNeedsAttention,
  fetchStageOverview,
} from '../api'
import { AppLayout } from '../AppLayout'
import { HomePage } from '../pages/HomePage'

vi.mock('../api', () => ({
  fetchCompetitions: vi.fn(),
  fetchCompetitionOverview: vi.fn(),
  fetchStageOverview: vi.fn(),
  fetchMatchDetail: vi.fn(),
  fetchNeedsAttention: vi.fn(),
  fetchMatchesByStage: vi.fn(),
}))

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc'

function renderWithShell(initialEntry: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<HomePage />} />
            <Route
              path="/competitions/:competitionId"
              element={<p>Workspace page</p>}
            />
            <Route path="/stages/:stageId" element={<p>Stage page</p>} />
            <Route path="/matches/:matchId" element={<p>Match page</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('AttentionDrawer', () => {
  beforeEach(() => {
    vi.mocked(fetchCompetitions).mockResolvedValue([
      { id: competitionId, name: 'Coupe U18', status: 'Running' },
    ])
    vi.mocked(fetchCompetitionOverview).mockResolvedValue({
      id: competitionId,
      name: 'Coupe U18',
      status: 'Running',
      entries: [],
      stages: [{ stageId, name: 'Group stage', status: 'Running' }],
    })
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      items: [],
      count: 0,
    })
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: stageId,
      competitionId,
      name: 'Group stage',
      status: 'Running',
      rounds: [],
      slots: [],
      draws: [],
    })
    vi.mocked(fetchMatchDetail).mockResolvedValue({
      matchId,
      competitionId,
      stageId,
      status: 'Scheduled',
      home: { entryId: 'h1', displayName: 'Home' },
      away: { entryId: 'a1', displayName: 'Away' },
      result: null,
      fixtureId: null,
      legIndex: null,
    })
    vi.mocked(fetchMatchesByStage).mockResolvedValue([])
  })

  it('is closed by default', () => {
    renderWithShell(`/competitions/${competitionId}`)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('opens from the header trigger', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )

    expect(screen.getByRole('dialog', { name: 'À traiter' })).toBeInTheDocument()
  })

  it('shows the empty state at count 0', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )

    expect(
      await screen.findByText('Rien à traiter pour l\'instant'),
    ).toBeInTheDocument()
  })

  it('lists attention items when count is greater than 0', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 1,
      items: [
        {
          source: 'ProgressionPending',
          reason: 'Progression en attente',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
      ],
    })

    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    )

    expect(await screen.findByText('Progression en attente')).toBeInTheDocument()
    expect(screen.getByText(/ProgressionPending · Stage/i)).toBeInTheDocument()
  })

  it('closes via the close button', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )
    await user.click(screen.getByRole('button', { name: 'Fermer' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  it('closes via Escape', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )
    await user.keyboard('{Escape}')

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  it('navigates to an item route and closes the drawer', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 1,
      items: [
        {
          source: 'StageReady',
          reason: 'Stage needs preparation',
          severity: 'Warning',
          targetType: 'Stage',
          targetId: stageId,
        },
      ],
    })

    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    )
    await user.click(await screen.findByRole('link', { name: /Stage needs preparation/i }))

    expect(await screen.findByText('Stage page')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('moves focus into the drawer when opened', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    })
    await user.click(trigger)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Fermer' })).toHaveFocus()
    })
  })

  it('returns focus to the trigger when closed', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    })
    await user.click(trigger)
    await user.click(screen.getByRole('button', { name: 'Fermer' }))

    await waitFor(() => {
      expect(trigger).toHaveFocus()
    })
  })

  it('explains the absence of competition context calmly', async () => {
    const user = userEvent.setup()
    renderWithShell('/')

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )

    expect(
      await screen.findByText(
        'Ouvrez une compétition pour voir ce qui demande votre attention.',
      ),
    ).toBeInTheDocument()
  })

  it('works on a stage deep link with resolved competition context', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 1,
      items: [
        {
          source: 'DrawPending',
          reason: 'Draw pending',
          severity: 'Warning',
          targetType: 'Stage',
          targetId: stageId,
        },
      ],
    })

    const user = userEvent.setup()
    renderWithShell(`/stages/${stageId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, 1 élément' }),
    )

    expect(await screen.findByText('Draw pending')).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'À traiter' })).toHaveTextContent(
      'Coupe U18',
    )
  })

  it('closes via the backdrop', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )
    await user.click(screen.getByRole('button', { name: 'Fermer À traiter' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  it('isolates the shell frame while open', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    expect(document.querySelector('.shell__frame')).not.toHaveAttribute('inert')

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )

    expect(document.querySelector('.shell__frame')).toHaveAttribute('inert')
  })

  it('does not introduce a new /attention route', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    await user.click(
      await screen.findByRole('button', { name: 'À traiter, aucun élément' }),
    )

    expect(screen.getByText('Workspace page')).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'À traiter' })).toBeInTheDocument()
  })
})
