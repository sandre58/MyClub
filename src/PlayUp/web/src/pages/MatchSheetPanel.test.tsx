import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  addDeclaredParticipation,
  changeDeclaredParticipationCompositionStatus,
  fetchMatchDetail,
  fetchOrganisationView,
  fetchStageOverview,
  finishMatch,
  removeDeclaredParticipation,
  setRunningScore,
  startMatch,
} from '../api'
import type {
  DeclaredParticipation,
  MatchDetail,
  OrganisationView,
  StageOverview,
} from '../types'
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
    addDeclaredParticipation: vi.fn(),
    removeDeclaredParticipation: vi.fn(),
    changeDeclaredParticipationCompositionStatus: vi.fn(),
  }
})

const matchId = '11111111-1111-1111-1111-111111111111'
const stageId = '22222222-2222-2222-2222-222222222222'
const competitionId = '33333333-3333-3333-3333-333333333333'
const fixtureId = '44444444-4444-4444-4444-444444444444'
const homeEntryId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const awayEntryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const dupontId = '11111111-1111-1111-1111-111111111101'
const martinId = '11111111-1111-1111-1111-111111111102'

function baseMatch(overrides: Partial<MatchDetail> = {}): MatchDetail {
  return {
    matchId,
    competitionId,
    stageId,
    status: 'Scheduled',
    home: { entryId: homeEntryId, displayName: 'Alpha' },
    away: { entryId: awayEntryId, displayName: 'Beta' },
    result: null,
    fixtureId,
    legIndex: 1,
    hasObservedLive: false,
    declaredParticipations: [],
    ...overrides,
  }
}

function organisationView(): OrganisationView {
  return {
    competitionId,
    name: 'Spring Cup',
    status: 'Ready',
    participants: {
      activeCount: 2,
      occupyingCount: 2,
      entries: [
        {
          entryId: homeEntryId,
          displayName: 'Alpha',
          status: 'Active',
          declaredMembers: [
            { memberId: dupontId, displayName: 'Dupont', role: 'Player' },
            { memberId: martinId, displayName: 'Martin', role: 'Player' },
          ],
        },
        {
          entryId: awayEntryId,
          displayName: 'Beta',
          status: 'Active',
          declaredMembers: [],
        },
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

function participation(
  overrides: Partial<DeclaredParticipation> = {},
): DeclaredParticipation {
  return {
    memberId: dupontId,
    displayName: 'Dupont',
    side: 'Home',
    compositionStatus: 'Starter',
    jerseyNumber: 9,
    ...overrides,
  }
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

describe('MatchPage sheet (Lot 2)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview)
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())
    vi.mocked(addDeclaredParticipation).mockResolvedValue()
    vi.mocked(removeDeclaredParticipation).mockResolvedValue()
    vi.mocked(changeDeclaredParticipationCompositionStatus).mockResolvedValue()
    vi.mocked(startMatch).mockResolvedValue()
    vi.mocked(finishMatch).mockResolvedValue()
    vi.mocked(setRunningScore).mockResolvedValue()
  })

  it('adds a home starter from the organisation roster', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(addDeclaredParticipation).mock.calls.length > 0) {
        return baseMatch({
          declaredParticipations: [participation()],
        })
      }
      return baseMatch()
    })

    renderMatchPage()

    expect(
      await screen.findByRole('heading', { name: 'Feuille de match' }),
    ).toBeInTheDocument()
    expect(
      await screen.findByRole('link', { name: /Effectif Â· Alpha/i }),
    ).toBeInTheDocument()

    const homeHeading = screen.getByRole('heading', { name: 'Domicile' })
    const homeColumn = homeHeading.closest('.match-sheet__side')
    expect(homeColumn).not.toBeNull()
    await user.selectOptions(
      within(homeColumn as HTMLElement).getByLabelText(/Joueur/i),
      dupontId,
    )
    await user.type(
      within(homeColumn as HTMLElement).getByPlaceholderText('ex. 9'),
      '9',
    )
    await user.click(
      within(homeColumn as HTMLElement).getByRole('button', {
        name: 'Ajouter Ã  la feuille',
      }),
    )

    await waitFor(() => {
      expect(addDeclaredParticipation).toHaveBeenCalledWith(matchId, {
        memberId: dupontId,
        side: 'Home',
        compositionStatus: 'Starter',
        jerseyNumber: 9,
      })
    })
    expect(
      await screen.findByText('Dupont', { selector: '.match-sheet__name' }),
    ).toBeInTheDocument()
    expect(screen.getByText('nÂ°9')).toBeInTheDocument()
  })

  it('toggles starter to bench', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (
        vi.mocked(changeDeclaredParticipationCompositionStatus).mock.calls
          .length > 0
      ) {
        return baseMatch({
          declaredParticipations: [
            participation({ compositionStatus: 'Bench', jerseyNumber: null }),
          ],
        })
      }
      return baseMatch({
        declaredParticipations: [participation({ jerseyNumber: null })],
      })
    })

    renderMatchPage()

    await user.click(await screen.findByRole('button', { name: 'RemplaÃ§ant' }))

    await waitFor(() => {
      expect(changeDeclaredParticipationCompositionStatus).toHaveBeenCalledWith(
        matchId,
        dupontId,
        'Bench',
      )
    })
  })

  it('confirms before removing a participation', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(removeDeclaredParticipation).mock.calls.length > 0) {
        return baseMatch()
      }
      return baseMatch({
        declaredParticipations: [participation({ jerseyNumber: null })],
      })
    })

    renderMatchPage()

    await user.click(await screen.findByRole('button', { name: 'Retirer' }))
    expect(removeDeclaredParticipation).not.toHaveBeenCalled()
    await user.click(
      screen.getByRole('button', { name: 'Confirmer le retrait' }),
    )

    await waitFor(() => {
      expect(removeDeclaredParticipation).toHaveBeenCalledWith(
        matchId,
        dupontId,
      )
    })
  })

  it('is read-only after Live is observed', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 0, awayGoals: 0 },
        declaredParticipations: [participation({ jerseyNumber: null })],
      }),
    )

    renderMatchPage()

    expect(
      await screen.findByText('Dupont', { selector: '.match-sheet__name' }),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/nâ€™est plus modifiable/i),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Ajouter Ã  la feuille' }),
    ).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Retirer' })).not.toBeInTheDocument()
  })
})
