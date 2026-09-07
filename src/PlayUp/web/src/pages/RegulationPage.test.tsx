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
        hasStandingRules: true,
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
        extraTimeNumberOfPeriods: 2,
        extraTimeDurationPerPeriod: 15,
        hasPenaltyShootout: true,
        penaltyInitialKicksPerTeam: 5,
        hasStandingRules: false,
        winPoints: null,
        drawPoints: null,
        lossPoints: null,
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
  it('renders frame tiles, phases, and a single edit action', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Équipes' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Match' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Classement' })).toBeInTheDocument()
    expect(screen.getByLabelText('Équipes : 8 à 16')).toBeInTheDocument()
    expect(
      screen.getByText(/phases qui produisent un classement/),
    ).toBeInTheDocument()
    expect(screen.getByText('1re MT')).toBeInTheDocument()
    expect(screen.getByText('Pause')).toBeInTheDocument()
    expect(screen.getByText('PR1')).toBeInTheDocument()
    expect(
      screen.getByLabelText(/Durée maximale du match 120 minutes/),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/Les règles générales s’appliquent/),
    ).toBeInTheDocument()
    expect(screen.queryByText('Le cadre de la compétition')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Modifier le règlement' })).toBeEnabled()
    expect(screen.getByRole('heading', { name: 'Poules' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Finale' })).toBeInTheDocument()
    expect(screen.getByText('Phase de groupe')).toBeInTheDocument()
    expect(screen.getByText('Élimination directe')).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: 'Structure' })).toHaveLength(2)
  })

  it('hides Classement when no classifying phase exists', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        stages: [
          {
            stageId: stageFinaleId,
            name: 'Finale',
            status: 'Draft',
            teamCount: 2,
            matchCount: 1,
            groupCount: 0,
            roundCount: 1,
            numberOfPeriods: 2,
            durationPerPeriod: 45,
            hasExtraTime: true,
            extraTimeNumberOfPeriods: 2,
            extraTimeDurationPerPeriod: 15,
            hasPenaltyShootout: true,
            penaltyInitialKicksPerTeam: 5,
            hasStandingRules: false,
            winPoints: null,
            drawPoints: null,
            lossPoints: null,
            hasDrawRules: false,
            hasQualificationRules: false,
            qualificationPathCount: 0,
            hasProgressionRules: false,
            progressionPathCount: 0,
            hasTieFormat: true,
            numberOfLegs: 1,
            aggregateScoring: null,
          },
        ],
      }),
    )

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Classement' })).not.toBeInTheDocument()
  })

  it('opens the regulation editor from the page action', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderPage()

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    )
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
  })

  it('keeps the edit action visible but disabled when ReplaceRegulation is unavailable', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({ actions: [] }),
    )

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Modifier le règlement' }),
    ).toBeDisabled()
  })
})
