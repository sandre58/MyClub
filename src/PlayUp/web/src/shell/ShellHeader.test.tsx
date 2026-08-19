import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  fetchCompetitionOverview,
  fetchCompetitions,
  fetchMatchDetail,
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
              path="/competitions"
              element={<p>Competition list page</p>}
            />
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

describe('ShellHeader', () => {
  beforeEach(() => {
    vi.mocked(fetchCompetitions).mockResolvedValue([
      { id: competitionId, name: 'Coupe U18', status: 'Running' },
    ])
    vi.mocked(fetchCompetitionOverview).mockResolvedValue({
      id: competitionId,
      name: 'Coupe U18',
      status: 'Running',
      entries: [],
      stages: [],
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
  })

  it('is rendered inside AppShell', () => {
    renderWithShell('/')

    expect(document.querySelector('.shell-header')).toBeInTheDocument()
  })

  it('shows the PLAY\'UP wordmark', () => {
    renderWithShell('/')

    expect(screen.getByText("PLAY'UP")).toBeInTheDocument()
  })

  it('shows the current competition when available', async () => {
    renderWithShell(`/competitions/${competitionId}`)

    expect(await screen.findByText('Coupe U18')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Changer' })).toHaveAttribute(
      'href',
      '/competitions',
    )
  })

  it('renders the no-selection state when competitions exist', async () => {
    renderWithShell('/')

    expect(await screen.findByText('Choisir une compétition')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Changer' })).toHaveAttribute(
      'href',
      '/competitions',
    )
  })

  it('renders the empty-host state when no competitions exist', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([])
    renderWithShell('/')

    expect(
      await screen.findByText('Aucune compétition'),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Liste' })).toHaveAttribute(
      'href',
      '/competitions',
    )
  })

  it('resolves competition context from a stage deep link', async () => {
    renderWithShell(`/stages/${stageId}`)

    expect(await screen.findByText('Coupe U18')).toBeInTheDocument()
    expect(fetchStageOverview).toHaveBeenCalledWith(stageId)
  })

  it('resolves competition context from a match deep link', async () => {
    renderWithShell(`/matches/${matchId}`)

    expect(await screen.findByText('Coupe U18')).toBeInTheDocument()
    expect(fetchMatchDetail).toHaveBeenCalledWith(matchId)
  })

  it('keeps À traiter visible when count is 0', async () => {
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    })
    expect(trigger).toBeInTheDocument()
    expect(trigger).not.toHaveClass('shell-header__attention--active')
    expect(screen.getByText('0')).toBeInTheDocument()
  })

  it('shows attention state when count is greater than 0', async () => {
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      items: [{ source: 'x', reason: 'y', severity: 'z', targetType: null, targetId: null }],
      count: 1,
    })
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 1 élément',
    })
    expect(trigger).toHaveClass('shell-header__attention--active')
    expect(screen.getByText('1')).toBeInTheDocument()
  })

  it('links the attention trigger to the drawer panel', async () => {
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    })
    expect(trigger).toHaveAttribute(
      'aria-controls',
      'shell-attention-drawer-panel',
    )

    await user.click(trigger)

    expect(trigger).toHaveAttribute('aria-expanded', 'true')
    expect(
      document.getElementById('shell-attention-drawer-panel'),
    ).toBeInTheDocument()
  })

  it('does not change the route when rendering the header', async () => {
    renderWithShell('/')

    await waitFor(() => {
      expect(screen.getByText('Choisir une compétition')).toBeInTheDocument()
    })
    expect(screen.getByRole('heading', { name: 'Welcome' })).toBeInTheDocument()
  })
})
