import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  configureOrganisationStructure,
  fetchOrganisationView,
  replaceCompetitionRegulation,
  ApiError,
} from '../api'
import type { OrganisationView } from '../types'
import { OrganisationPage } from './OrganisationPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchOrganisationView: vi.fn(),
    replaceCompetitionRegulation: vi.fn(),
    configureOrganisationStructure: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const entryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

function organisationView(
  overrides: Partial<OrganisationView> = {},
): OrganisationView {
  return {
    competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    participants: {
      activeCount: 1,
      occupyingCount: 1,
      entries: [
        {
          entryId,
          displayName: 'Alpha',
          status: 'Active',
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
    actions: [
      'AddEntry',
      'ConfigureStructure',
      'ReplaceRegulation',
      'RenameEntry',
      'WithdrawEntry',
      'DeleteEntry',
    ],
    readiness: {
      readyForNextSlice: false,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers: ['InsufficientParticipants', 'MissingStage'],
    },
    stages: [],
    ...overrides,
  }
}

function renderOrganisationPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[`/competitions/${competitionId}/organisation`]}
      >
        <Routes>
          <Route
            path="/competitions/:competitionId/teams"
            element={<p>Teams route</p>}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<OrganisationPage />}
          />
          <Route
            path="/competitions/:competitionId"
            element={<p>Workspace route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return { queryClient }
}

describe('OrganisationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(replaceCompetitionRegulation).mockResolvedValue(
      organisationView(),
    )
    vi.mocked(configureOrganisationStructure).mockResolvedValue(
      organisationView({
        format: {
          kind: 'Championship',
          primaryStageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
      }),
    )
  })

  it('shows loading while organisation is pending', () => {
    vi.mocked(fetchOrganisationView).mockReturnValue(new Promise(() => {}))

    renderOrganisationPage()

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…')
  })

  it('renders organisation summary from the Host DTO', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    expect(
      await screen.findByRole('heading', { name: 'Organisation' }),
    ).toBeInTheDocument()
    expect(screen.getByText(/1 équipe · minimum 2/)).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /Ouvrir Équipes/i }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/teams`)
    expect(
      screen.getByRole('link', { name: /Participants insuffisants/i }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/teams`)
    expect(screen.getByText(/2–64/)).toBeInTheDocument()
    expect(
      screen.getByText('Aucun type disciplinaire autorisé'),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/Encore 2 éléments avant de démarrer/i),
    ).toBeInTheDocument()
  })

  it('shows AllowedTypes on the regulation summary', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        regulation: {
          minimumTeams: 2,
          maximumTeams: 64,
          durationPerPeriod: 45,
          numberOfPeriods: 2,
          winPoints: 3,
          drawPoints: 1,
          lossPoints: 0,
          allowedTypes: ['Yellow', 'Red'],
        },
      }),
    )

    renderOrganisationPage()

    expect(
      await screen.findByText('Discipline : Jaune, Rouge'),
    ).toBeInTheDocument()
  })

  it('opens Équipes from the teams fact panel', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await user.click(
      await screen.findByRole('link', { name: /Ouvrir Équipes/i }),
    )

    expect(screen.getByText('Teams route')).toBeInTheDocument()
  })

  it('does not render redundant competition section navigation', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await screen.findByRole('heading', { name: 'Organisation' })

    expect(
      screen.queryByRole('navigation', { name: 'Competition sections' }),
    ).not.toBeInTheDocument()
  })

  it('shows empty participants fact', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        participants: {
          activeCount: 0,
          occupyingCount: 0,
          entries: [],
        },
      }),
    )

    renderOrganisationPage()

    expect(await screen.findByText(/0 équipe · minimum 2/)).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /Ouvrir Équipes/i }),
    ).toBeInTheDocument()
  })

  it('shows an error when organisation read fails', async () => {
    vi.mocked(fetchOrganisationView).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    )

    renderOrganisationPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    )
  })

  it('navigates back to vue d’ensemble', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await user.click(
      await screen.findByRole('link', {
        name: /Vue d'ensemble/i,
      }),
    )

    expect(screen.getByText('Workspace route')).toBeInTheDocument()
  })

  it('replaces regulation with the Host payload', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await user.click(
      await screen.findByRole('button', { name: /Modifier le règlement/i }),
    )
    const dialog = await screen.findByRole('dialog')
    const minTeams = await within(dialog).findByLabelText(/Minimum d’équipes/i)
    await user.clear(minTeams)
    await user.type(minTeams, '4')
    await user.click(
      within(dialog).getByRole('button', { name: 'Enregistrer le règlement' }),
    )

    await waitFor(() => {
      expect(replaceCompetitionRegulation).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          minimumTeams: 4,
          maximumTeams: 64,
          durationPerPeriod: 45,
          numberOfPeriods: 2,
          halfTimeDuration: 15,
          winPoints: 3,
          drawPoints: 1,
          lossPoints: 0,
          allowedTypes: [],
        }),
      )
    })
  })

  it('configures championship structure with the Host payload', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await user.click(
      await screen.findByRole('button', { name: /Configurer la structure/i }),
    )
    const dialog = await screen.findByRole('dialog')
    await user.selectOptions(
      await within(dialog).findByLabelText(/^Format$/i),
      'Championship',
    )
    const matchdays = within(dialog).getByLabelText(/Nombre de journées/i)
    fireEvent.change(matchdays, { target: { value: '2' } })
    await user.click(
      within(dialog).getByRole('button', { name: 'Configurer la structure' }),
    )

    await waitFor(() => {
      expect(configureOrganisationStructure).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          format: 'Championship',
          matchdayCount: 2,
          groupCount: null,
          participantsPerGroup: null,
          bracketSize: null,
          matchGenerationFormat: 'SingleRoundRobin',
        }),
      )
    })
  })

  it('configures Championship with DoubleRoundRobin generation format', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await user.click(
      await screen.findByRole('button', { name: /Configurer la structure/i }),
    )
    const dialog = await screen.findByRole('dialog')
    await user.selectOptions(
      await within(dialog).findByLabelText(/^Format$/i),
      'Championship',
    )
    await user.selectOptions(
      within(dialog).getByLabelText(/Génération des rencontres/i),
      'DoubleRoundRobin',
    )
    await user.click(
      within(dialog).getByRole('button', { name: 'Configurer la structure' }),
    )

    await waitFor(() => {
      expect(configureOrganisationStructure).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          format: 'Championship',
          matchGenerationFormat: 'DoubleRoundRobin',
        }),
      )
    })
  })

  it('shows match generation format on a Championship structure panel', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        format: {
          kind: 'Championship',
          primaryStageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        structure: {
          groupCount: 0,
          roundCount: 0,
          matchdayCount: 34,
          slotCount: 0,
          hasDrawRules: false,
          numberOfPots: null,
          matchGenerationFormat: 'DoubleRoundRobin',
        },
      }),
    )

    renderOrganisationPage()

    expect(
      await screen.findByText(/Aller-retour \(double RR\)/i),
    ).toBeInTheDocument()
  })

  it('hides mutations when Host actions omit them', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({ actions: [] }),
    )

    renderOrganisationPage()

    expect(
      await screen.findByRole('heading', { name: 'Organisation' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Modifier le règlement/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Configurer la structure/i }),
    ).not.toBeInTheDocument()
  })

  it("shows materialize readiness and Vue d'ensemble CTA for a ready Championship", async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        format: {
          kind: 'Championship',
          primaryStageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        structure: {
          groupCount: 0,
          roundCount: 0,
          matchdayCount: 1,
          slotCount: 0,
          hasDrawRules: false,
          numberOfPots: null,
          matchGenerationFormat: 'SingleRoundRobin',
        },
        participants: {
          activeCount: 2,
          occupyingCount: 2,
          entries: [
            { entryId, displayName: 'Alpha', status: 'Active' },
            {
              entryId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
              displayName: 'Beta',
              status: 'Active',
            },
          ],
        },
        readiness: {
          readyForNextSlice: true,
          readyForDraw: false,
          readyForMaterialization: true,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: true,
          attachedMatchCount: 0,
          blockers: [],
        },
      }),
    )

    renderOrganisationPage()

    expect(
      await screen.findByText(
        /La compétition est prête à matérialiser/i,
      ),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', {
        name: /Aller à la Vue d’ensemble pour matérialiser/i,
      }),
    ).toHaveAttribute('href', `/competitions/${competitionId}`)
    expect(screen.queryByText(/Prêt pour le tirage/i)).not.toBeInTheDocument()
  })

  it('shows blockers and hides Vue d’ensemble CTA when not ready to materialize', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    expect(
      await screen.findByText(/Encore 2 éléments avant de démarrer/i),
    ).toBeInTheDocument()
    expect(screen.getByText('Participants insuffisants')).toBeInTheDocument()
    expect(screen.getByText('Phase manquante')).toBeInTheDocument()
    expect(
      screen.queryByRole('link', {
        name: /Aller à la Vue d’ensemble pour matérialiser/i,
      }),
    ).not.toBeInTheDocument()
  })

  it('shows draw readiness for Groups but not as Championship next step', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        format: {
          kind: 'Groups',
          primaryStageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          primaryStageName: 'Groups',
          primaryStageStatus: 'Draft',
        },
        readiness: {
          readyForNextSlice: true,
          readyForDraw: true,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: false,
          attachedMatchCount: 0,
          blockers: [],
        },
      }),
    )

    renderOrganisationPage()

    expect(await screen.findByText(/Prêt pour le tirage/i)).toBeInTheDocument()
  })
})
