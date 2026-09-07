import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { fetchOrganisationView } from '../api'
import { RegulationPage } from './RegulationPage'
import type { OrganisationView } from '../types'

vi.mock('../api', () => ({
  fetchOrganisationView: vi.fn(),
  replaceCompetitionRegulation: vi.fn(),
}))

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const stageFinaleId = 'cccccccc-cccc-cccc-cccc-cccccccccccc'

function organisationView(
  overrides: Partial<OrganisationView> = {},
): OrganisationView {
  return {
    competitionId,
    name: 'Coupe',
    status: 'Draft',
    participants: {
      activeCount: 0,
      occupyingCount: 0,
      entries: [],
    },
    format: {
      kind: null,
      primaryStageId: null,
      primaryStageName: null,
      primaryStageStatus: null,
    },
    regulation: {
      minimumTeams: 8,
      maximumTeams: 16,
      durationPerPeriod: 45,
      numberOfPeriods: 2,
      winPoints: 3,
      drawPoints: 1,
      lossPoints: 0,
      allowedTypes: [],
      halfTimeDuration: 15,
      hasExtraTime: true,
      extraTimeDurationPerPeriod: 15,
      extraTimeNumberOfPeriods: 2,
      hasPenaltyShootout: true,
      penaltyInitialKicksPerTeam: 5,
      rankingCriteria: [
        'Points',
        'GoalDifference',
        'GoalsFor',
        'HeadToHead',
      ],
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
    actions: ['ReplaceRegulation'],
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
    stages: [
      {
        stageId,
        name: 'Poules',
        status: 'Draft',
        teamCount: 4,
        matchCount: 6,
        groupCount: 2,
        roundCount: 0,
        numberOfPeriods: 2,
        durationPerPeriod: 45,
        hasExtraTime: false,
        hasPenaltyShootout: false,
        winPoints: 3,
        drawPoints: 1,
        lossPoints: 0,
        hasDrawRules: true,
        drawMode: 'Random',
        numberOfPots: 2,
        hasQualificationRules: true,
        qualificationPathCount: 2,
        hasProgressionRules: false,
        progressionPathCount: 0,
        hasTieFormat: false,
        numberOfLegs: null,
        aggregateScoring: null,
      },
      {
        stageId: stageFinaleId,
        name: 'Finale',
        status: 'Draft',
        teamCount: 8,
        matchCount: 7,
        groupCount: 0,
        roundCount: 3,
        numberOfPeriods: 2,
        durationPerPeriod: 45,
        hasExtraTime: true,
        hasPenaltyShootout: true,
        winPoints: 3,
        drawPoints: 1,
        lossPoints: 0,
        hasDrawRules: false,
        hasQualificationRules: false,
        qualificationPathCount: 0,
        hasProgressionRules: true,
        progressionPathCount: 2,
        hasTieFormat: true,
        numberOfLegs: 1,
        aggregateScoring: null,
      },
    ],
    ...overrides,
  }
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/competitions/${competitionId}/regulation`]}>
        <Routes>
          <Route
            path="/competitions/:competitionId/regulation"
            element={<RegulationPage />}
          />
          <Route path="/stages/:stageId" element={<p>Stage</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('RegulationPage', () => {
  it('renders rich frame tiles and phase schematics', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Entrées' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Match' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Classement' })).toBeInTheDocument()
    expect(screen.getByLabelText('Capacité 8 à 16 équipes')).toBeInTheDocument()
    expect(screen.getByText('1re MT')).toBeInTheDocument()
    expect(screen.getByText('2e MT')).toBeInTheDocument()
    expect(screen.getByText('Pause')).toBeInTheDocument()
    expect(screen.getByText('90 min de jeu')).toBeInTheDocument()
    expect(screen.getByText('Prolongations')).toBeInTheDocument()
    expect(screen.getByText('PR1')).toBeInTheDocument()
    expect(screen.getByText('5 tirs / équipe')).toBeInTheDocument()
    expect(screen.getByText('Attribution des points')).toBeInTheDocument()
    expect(screen.getByText('Goal average')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Poules/i })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Finale/i })).toBeInTheDocument()
    expect(screen.getByText('Qualification')).toBeInTheDocument()
    expect(screen.getByLabelText('2 poules')).toBeInTheDocument()
    expect(screen.getByLabelText('Tableau éliminatoire')).toBeInTheDocument()
    expect(
      screen.getByText(/Les règles générales s’appliquent/),
    ).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: /Structure →/ })).toHaveLength(2)
  })

  it('shows Modifier on frame tiles and opens the editor when allowed', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderPage()

    const editButtons = await screen.findAllByRole('button', {
      name: 'Modifier →',
    })
    expect(editButtons).toHaveLength(3)
    await user.click(editButtons[0]!)
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
  })

  it('disables Modifier when ReplaceRegulation is unavailable', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({ actions: [] }),
    )

    renderPage()

    const editButtons = await screen.findAllByRole('button', {
      name: 'Modifier →',
    })
    expect(editButtons[0]).toBeDisabled()
  })
})
