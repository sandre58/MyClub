import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { fetchOrganisationView, replaceCompetitionRegulation } from '../api'
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
      allowedTypes: ['Yellow', 'Red'],
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
      forfeitWinnerGoals: 3,
      forfeitLoserGoals: 0,
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
        name: 'Groupes',
        status: 'Draft',
        teamCount: 8,
        matchCount: 12,
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
        rankingCriteria: ['Points', 'Wins', 'GoalDifference'],
        hasDrawRules: true,
        drawMode: 'Random',
        numberOfPots: 4,
        hasQualificationRules: true,
        qualificationPathCount: 2,
        hasProgressionRules: false,
        progressionPathCount: 0,
        hasTieFormat: false,
        numberOfLegs: null,
        aggregateScoring: null,
        formatKind: 'Groups',
      },
      {
        stageId: stageFinaleId,
        name: 'Finale',
        status: 'Draft',
        teamCount: 2,
        matchCount: 0,
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
        numberOfLegs: 2,
        aggregateScoring: true,
        hasAwayGoalsRule: true,
        hasTieExtraTime: true,
        hasTiePenaltyShootout: true,
        hasPlacementAwardRules: true,
        placementAwardCount: 2,
        placementAwards: [
          { rank: 1, outcome: 'Winner' },
          { rank: 2, outcome: 'Loser' },
        ],
        formatKind: 'Cup',
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
    expect(screen.getAllByRole('heading', { name: 'Match' }).length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByRole('heading', { name: 'Classement' }).length).toBeGreaterThanOrEqual(1)
    expect(screen.getByRole('heading', { name: 'Disciplinaire' })).toBeInTheDocument()
    expect(screen.getByLabelText('Équipes : 8 à 16')).toBeInTheDocument()
    expect(screen.getByLabelText(/Score administratif en cas de forfait/)).toBeInTheDocument()
    expect(screen.getByText('Forfait')).toBeInTheDocument()
    expect(screen.getByText('Carton(s) autorisé(s)')).toBeInTheDocument()
    expect(screen.getByLabelText('Jaune')).toBeInTheDocument()
    expect(screen.getByLabelText('Rouge')).toBeInTheDocument()
    expect(screen.getByText('Barème de points')).toBeInTheDocument()
    expect(screen.getAllByText('Différence de buts').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('1re MT')).toBeInTheDocument()
    expect(screen.getByText('Pause')).toBeInTheDocument()
    expect(screen.getByText('PR1')).toBeInTheDocument()
    expect(
      screen.getByLabelText(/Durée maximale du match 120 minutes/),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Modifier le règlement' })).toBeEnabled()
    expect(screen.getByRole('heading', { name: 'Groupes' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Finale' })).toBeInTheDocument()
    expect(screen.getAllByText('Brouillon').length).toBeGreaterThanOrEqual(2)
    expect(screen.getByText('Aller-retour')).toBeInTheDocument()
    expect(screen.getByText('Cumul des scores')).toBeInTheDocument()
    expect(screen.getByText('Buts à l’extérieur')).toBeInTheDocument()
    expect(screen.getByText('Tirage aléatoire')).toBeInTheDocument()
    expect(screen.getByText('pots')).toBeInTheDocument()
    expect(screen.getAllByText('4').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('Diffère du règlement global')).toBeInTheDocument()
    expect(screen.getByLabelText('Vainqueur → place 1')).toBeInTheDocument()
    expect(screen.getByLabelText('Perdant → place 2')).toBeInTheDocument()
    expect(
      screen.getByLabelText('2 chemins de qualification'),
    ).toBeInTheDocument()
    expect(screen.getByText('chemins')).toBeInTheDocument()
    expect(screen.getAllByText('2×45′').length).toBeGreaterThanOrEqual(1)
    expect(screen.getAllByText(/Prolongations/).length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('· 2×15′')).toBeInTheDocument()
    expect(screen.getAllByText(/TAB/).length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('· 5 tirs')).toBeInTheDocument()
    expect(screen.getByText('Tirs au but')).toBeInTheDocument()
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
            formatKind: 'Cup',
          },
        ],
      }),
    )

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Classement' })).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Disciplinaire' })).toBeInTheDocument()
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

  it('shows forfeit under phase Classement and draw seeds with constraints', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        stages: [
          {
            stageId,
            name: 'Groupes',
            status: 'Draft',
            teamCount: 8,
            matchCount: 12,
            groupCount: 2,
            roundCount: 0,
            numberOfPeriods: 2,
            durationPerPeriod: 45,
            hasExtraTime: true,
            extraTimeNumberOfPeriods: 2,
            extraTimeDurationPerPeriod: 15,
            hasPenaltyShootout: true,
            penaltyInitialKicksPerTeam: 5,
            hasStandingRules: true,
            winPoints: 3,
            drawPoints: 1,
            lossPoints: 0,
            rankingCriteria: [
              'Points',
              'GoalDifference',
              'GoalsFor',
              'HeadToHead',
            ],
            forfeitWinnerGoals: 3,
            forfeitLoserGoals: 0,
            hasDrawRules: true,
            drawMode: 'Random',
            numberOfPots: 4,
            numberOfSeeds: 2,
            drawConstraints: [
              {
                type: 'SameAssociationAvoidance',
                enforcement: 'Preferred',
              },
              {
                type: 'MaxSameAssociationPerGroup',
                enforcement: 'Required',
                maxPerGroup: 1,
              },
            ],
            hasQualificationRules: false,
            qualificationPathCount: 0,
            hasProgressionRules: false,
            progressionPathCount: 0,
            hasTieFormat: false,
            numberOfLegs: null,
            aggregateScoring: null,
            formatKind: 'Groups',
          },
        ],
      }),
    )

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.queryByText('Diffère du règlement global')).not.toBeInTheDocument()
    expect(screen.getAllByText('Forfait').length).toBeGreaterThanOrEqual(2)
    expect(
      screen.getByTitle('Score administratif en cas de forfait : 3–0'),
    ).toBeInTheDocument()
    expect(screen.getByText('têtes de série')).toBeInTheDocument()
    expect(screen.getByText('Éviter la même association')).toBeInTheDocument()
    expect(screen.getByText(/Souhaité/)).toBeInTheDocument()
    expect(screen.getByText('Max. même association / groupe')).toBeInTheDocument()
    expect(screen.getByText(/Obligatoire/)).toBeInTheDocument()
    expect(screen.getByText(/max\. 1/)).toBeInTheDocument()
  })

  it('does not invent Swiss rounds when swissRoundCount is unknown', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        stages: [
          {
            stageId,
            name: 'Suisse',
            status: 'Draft',
            teamCount: 16,
            matchCount: 0,
            groupCount: 0,
            roundCount: 0,
            numberOfPeriods: 2,
            durationPerPeriod: 45,
            hasExtraTime: true,
            extraTimeNumberOfPeriods: 2,
            extraTimeDurationPerPeriod: 15,
            hasPenaltyShootout: true,
            penaltyInitialKicksPerTeam: 5,
            hasStandingRules: true,
            winPoints: 3,
            drawPoints: 1,
            lossPoints: 0,
            rankingCriteria: [
              'Points',
              'GoalDifference',
              'GoalsFor',
              'HeadToHead',
            ],
            hasDrawRules: false,
            hasQualificationRules: false,
            qualificationPathCount: 0,
            hasProgressionRules: false,
            progressionPathCount: 0,
            hasTieFormat: false,
            numberOfLegs: null,
            aggregateScoring: null,
            formatKind: 'Swiss',
            swissRoundCount: null,
          },
        ],
      }),
    )

    renderPage()

    expect(await screen.findByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.getByLabelText('Système suisse')).toBeInTheDocument()
    expect(screen.queryByLabelText(/Système suisse · 3 rondes/)).not.toBeInTheDocument()
  })

  it('opens a sectioned editor with sync banner for eligible stages', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderPage()

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    )

    const dialog = await screen.findByRole('dialog')
    expect(
      within(dialog).getByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument()
    expect(
      within(dialog).getByRole('heading', { name: 'Match' }),
    ).toBeInTheDocument()
    expect(
      within(dialog).getByRole('heading', { name: 'Classement' }),
    ).toBeInTheDocument()
    expect(
      within(dialog).getByRole('heading', { name: 'Disciplinaire' }),
    ).toBeInTheDocument()
    expect(
      within(dialog).getByText(
        /Les modifications seront appliquées aux phases encore en préparation/,
      ),
    ).toBeInTheDocument()
    expect(within(dialog).getByLabelText(/Activer les prolongations/)).toBeChecked()
    expect(within(dialog).getByLabelText(/Activer les tirs au but/)).toBeChecked()
  })

  it('hides the sync banner when no Draft/Ready stage is eligible', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        stages: [
          {
            stageId,
            name: 'Groupes',
            status: 'Running',
            teamCount: 8,
            matchCount: 12,
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
            hasDrawRules: false,
            hasQualificationRules: false,
            qualificationPathCount: 0,
            hasProgressionRules: false,
            progressionPathCount: 0,
            hasTieFormat: false,
            numberOfLegs: null,
            aggregateScoring: null,
            formatKind: 'Groups',
          },
        ],
      }),
    )

    renderPage()

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    )

    const dialog = await screen.findByRole('dialog')
    expect(
      within(dialog).queryByText(
        /Les modifications seront appliquées aux phases encore en préparation/,
      ),
    ).not.toBeInTheDocument()
  })

  it('asks for Ready confirmation before submitting', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({ status: 'Ready' }),
    )
    vi.mocked(replaceCompetitionRegulation).mockResolvedValue(undefined as never)

    renderPage()

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    )
    await user.click(
      await screen.findByRole('button', { name: 'Enregistrer le règlement' }),
    )

    expect(
      await screen.findByRole('heading', {
        name: 'Modifier et rouvrir la compétition ?',
      }),
    ).toBeInTheDocument()
    expect(replaceCompetitionRegulation).not.toHaveBeenCalled()

    await user.click(
      screen.getByRole('button', { name: 'Modifier et rouvrir' }),
    )

    await waitFor(() => {
      expect(replaceCompetitionRegulation).toHaveBeenCalled()
    })
  })

  it('opens the Match section from a tile shortcut and submits full regulation', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())
    vi.mocked(replaceCompetitionRegulation).mockResolvedValue(undefined as never)

    renderPage()

    await user.click(
      await screen.findByRole('button', { name: 'Modifier la section Match' }),
    )

    const dialog = await screen.findByRole('dialog')
    expect(
      within(dialog).getByRole('heading', { name: 'Match' }),
    ).toBeInTheDocument()

    await user.click(
      within(dialog).getByRole('button', { name: 'Enregistrer le règlement' }),
    )

    await waitFor(() => {
      expect(replaceCompetitionRegulation).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          durationPerPeriod: 45,
          numberOfPeriods: 2,
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
          forfeitWinnerGoals: 3,
          forfeitLoserGoals: 0,
        }),
      )
    })
  })
})
