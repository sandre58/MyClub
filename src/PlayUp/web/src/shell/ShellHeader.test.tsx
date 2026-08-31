import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  fetchCompetitionOverview,
  fetchCompetitions,
  fetchMatchDetail,
  fetchCompetitionCockpit,
  fetchStageOverview,
} from '../api'
import { AppLayout } from '../AppLayout'
import { cockpitView } from '../test/cockpitFixtures'
import type { CockpitSituation } from '../types'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchCompetitions: vi.fn(),
    fetchCompetitionOverview: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchMatchDetail: vi.fn(),
    fetchCompetitionCockpit: vi.fn(),
  }
})

function attentionCockpit(items: Partial<CockpitSituation>[]) {
  const normalized: CockpitSituation[] = items.map((item) => ({
    source: item.source ?? 'InsufficientParticipants',
    nature: item.nature ?? 'Blocking',
    targetType: item.targetType ?? null,
    targetId: item.targetId ?? null,
    matchId: item.matchId ?? null,
    actionable: item.actionable ?? Boolean(item.actionCode),
    actionCode: item.actionCode ?? null,
    impactCode: item.impactCode ?? null,
    params: item.params ?? {},
  }))
  return cockpitView({
    competitionId,
    name: 'Coupe U18',
    status: 'Running',
    attentionSummary: { count: normalized.length, items: normalized },
    situations: normalized,
  })
}

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
            <Route
              path="/"
              element={
                <main id="main">
                  <h1>Play’up</h1>
                </main>
              }
            />
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
      logoMediaId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
    })
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(attentionCockpit([]))
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

  it('does not show the PLAY\'UP wordmark in the header', () => {
    renderWithShell('/')

    expect(screen.queryByText("PLAY'UP")).not.toBeInTheDocument()
  })

  it('shows the current competition when available', async () => {
    renderWithShell(`/competitions/${competitionId}`)

    expect(await screen.findByText('Coupe U18')).toBeInTheDocument()
    expect(await screen.findByText('En cours')).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Changer de compétition' }),
    ).toHaveAttribute('href', '/')
    const crest = document.querySelector('.shell-header__crest img')
    expect(crest).toHaveAttribute(
      'src',
      '/media/dddddddd-dddd-dddd-dddd-dddddddddddd/content',
    )
  })

  it('renders the no-selection state when competitions exist', async () => {
    renderWithShell('/')

    expect(await screen.findByText('Choisir une compétition')).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Changer de compétition' }),
    ).toHaveAttribute('href', '/')
  })

  it('renders the empty-host state when no competitions exist', async () => {
    vi.mocked(fetchCompetitions).mockResolvedValue([])
    renderWithShell('/')

    expect(
      await screen.findByText('Aucune compétition'),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Ouvrir la liste des compétitions' }),
    ).toHaveAttribute('href', '/')
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

  it('keeps À traiter visible but disabled when count is 0', async () => {
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, aucun élément',
    })
    expect(trigger).toBeInTheDocument()
    expect(trigger).toBeDisabled()
    expect(screen.queryByText('0')).not.toBeInTheDocument()
  })

  it('shows attention state when count is greater than 0', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      attentionCockpit([
        {
          source: 'InsufficientParticipants',
          nature: 'Blocking',
          targetType: 'Organisation',
          targetId: competitionId,
          matchId: null,
          actionCode: 'AddEntry',
          params: {},
        },
      ]),
    )
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 1 élément',
    })
    expect(trigger).toBeEnabled()
    expect(screen.getByText('1')).toBeInTheDocument()
  })

  it('uses plural aria-label when count is 2', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      attentionCockpit([
        {
          source: 'InsufficientParticipants',
          nature: 'Blocking',
          targetType: 'Organisation',
          targetId: competitionId,
          matchId: null,
          actionCode: 'AddEntry',
          params: {},
        },
        {
          source: 'MissingStructure',
          nature: 'Blocking',
          targetType: 'Organisation',
          targetId: competitionId,
          matchId: null,
          actionCode: 'ConfigureStructure',
          params: {},
        },
      ]),
    )
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 2 éléments',
    })
    expect(trigger).toHaveAttribute('aria-label', 'À traiter, 2 éléments')
    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('links the attention trigger to the drawer panel', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      attentionCockpit([
        {
          source: 'InsufficientParticipants',
          nature: 'Blocking',
          targetType: 'Organisation',
          targetId: competitionId,
          matchId: null,
          actionCode: 'AddEntry',
          params: {},
        },
      ]),
    )
    const user = userEvent.setup()
    renderWithShell(`/competitions/${competitionId}`)

    const trigger = await screen.findByRole('button', {
      name: 'À traiter, 1 élément',
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
    expect(screen.getByRole('heading', { name: 'Play’up' })).toBeInTheDocument()
  })
})
