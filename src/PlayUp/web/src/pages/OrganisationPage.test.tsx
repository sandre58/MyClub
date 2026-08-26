import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  addCompetitionEntry,
  configureOrganisationStructure,
  excludeCompetitionEntry,
  fetchOrganisationView,
  renameCompetitionEntry,
  replaceCompetitionRegulation,
  withdrawCompetitionEntry,
  ApiError,
} from '../api'
import type { OrganisationView } from '../types'
import { OrganisationPage } from './OrganisationPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchOrganisationView: vi.fn(),
    addCompetitionEntry: vi.fn(),
    renameCompetitionEntry: vi.fn(),
    withdrawCompetitionEntry: vi.fn(),
    excludeCompetitionEntry: vi.fn(),
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
      'ExcludeEntry',
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

async function openTeamsAddDialog(
  user: ReturnType<typeof userEvent.setup>,
) {
  await user.click(
    await screen.findByRole('button', { name: /Ajouter une équipe/i }),
  )
  return screen.findByRole('dialog')
}

async function openTeamsManageDialog(
  user: ReturnType<typeof userEvent.setup>,
) {
  await user.click(
    await screen.findByRole('button', { name: /Gérer les équipes/i }),
  )
  return screen.findByRole('dialog')
}

describe('OrganisationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(addCompetitionEntry).mockResolvedValue(organisationView())
    vi.mocked(renameCompetitionEntry).mockResolvedValue(organisationView())
    vi.mocked(withdrawCompetitionEntry).mockResolvedValue(organisationView())
    vi.mocked(excludeCompetitionEntry).mockResolvedValue(organisationView())
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
    expect(await screen.findByText('Alpha')).toBeInTheDocument()
    expect(
      screen.getByLabelText('Inscription complète'),
    ).toBeInTheDocument()
    expect(screen.getByText('Participants insuffisants')).toBeInTheDocument()
    expect(screen.getByText(/2–64/)).toBeInTheDocument()
    expect(
      screen.getByText(/Encore 2 éléments avant de démarrer/i),
    ).toBeInTheDocument()
  })

  it('does not render redundant competition section navigation', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await screen.findByRole('heading', { name: 'Organisation' })

    expect(
      screen.queryByRole('navigation', { name: 'Competition sections' }),
    ).not.toBeInTheDocument()
  })

  it('shows empty participants state', async () => {
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

    expect(await screen.findByText(/Aucune inscription/i)).toBeInTheDocument()
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

  it('adds an entry with the Host payload and refreshes', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockImplementation(async () => {
      if (vi.mocked(addCompetitionEntry).mock.calls.length > 0) {
        return organisationView({
          participants: {
            activeCount: 2,
            occupyingCount: 2,
            entries: [
              { entryId, displayName: 'Alpha', status: 'Active' },
              {
                entryId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
                displayName: 'Beta',
                status: 'Active',
              },
            ],
          },
        })
      }

      return organisationView()
    })

    renderOrganisationPage()

    const dialog = await openTeamsAddDialog(user)
    await user.type(
      await within(dialog).findByLabelText(/Nom de la nouvelle inscription/i),
      'Beta',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Ajouter' }))

    await waitFor(() => {
      expect(addCompetitionEntry).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          displayName: 'Beta',
        }),
      )
    })
    expect((await screen.findAllByText('Beta')).length).toBeGreaterThan(0)
  })

  it('shows pending state while adding an entry', async () => {
    const user = userEvent.setup()
    let resolveAdd!: (value: OrganisationView) => void
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())
    vi.mocked(addCompetitionEntry).mockReturnValue(
      new Promise((resolve) => {
        resolveAdd = resolve
      }),
    )

    renderOrganisationPage()

    const dialog = await openTeamsAddDialog(user)
    await user.type(
      await within(dialog).findByLabelText(/Nom de la nouvelle inscription/i),
      'Beta',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Ajouter' }))

    expect(
      await within(dialog).findByRole('button', { name: 'Ajout…' }),
    ).toBeDisabled()

    resolveAdd(organisationView())
  })

  it('shows add entry error from the Host', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())
    vi.mocked(addCompetitionEntry).mockRejectedValue(
      new ApiError(400, 'Entry capacity exceeded'),
    )

    renderOrganisationPage()

    const dialog = await openTeamsAddDialog(user)
    await user.type(
      await within(dialog).findByLabelText(/Nom de la nouvelle inscription/i),
      'Overflow',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Ajouter' }))

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(
      'Entry capacity exceeded (400)',
    )
  })

  it('renames an entry with the Host payload', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    const dialog = await openTeamsManageDialog(user)
    const renameInput = await within(dialog).findByLabelText(/Renommer/i)
    await user.clear(renameInput)
    await user.type(renameInput, 'Alpha FC')
    await user.click(within(dialog).getByRole('button', { name: 'Renommer' }))

    await waitFor(() => {
      expect(renameCompetitionEntry).toHaveBeenCalledWith(
        competitionId,
        entryId,
        { displayName: 'Alpha FC' },
      )
    })
  })

  it('withdraws an entry after confirmation', async () => {
    const user = userEvent.setup()
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    const dialog = await openTeamsManageDialog(user)
    await user.click(within(dialog).getByRole('button', { name: 'Retirer' }))

    await waitFor(() => {
      expect(withdrawCompetitionEntry).toHaveBeenCalledWith(
        competitionId,
        entryId,
      )
    })
  })

  it('excludes an entry after confirmation', async () => {
    const user = userEvent.setup()
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    const dialog = await openTeamsManageDialog(user)
    await user.click(within(dialog).getByRole('button', { name: 'Exclure' }))

    await waitFor(() => {
      expect(excludeCompetitionEntry).toHaveBeenCalledWith(
        competitionId,
        entryId,
      )
    })
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
      screen.queryByRole('button', { name: /Ajouter une équipe/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Gérer les équipes/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Modifier le règlement/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Configurer la structure/i }),
    ).not.toBeInTheDocument()
  })

  it('shows materialize readiness and Cockpit CTA for a ready Championship', async () => {
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

  it('invalidates cockpit query after adding an entry', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())
    const { queryClient } = renderOrganisationPage()
    const spy = vi.spyOn(queryClient, 'invalidateQueries')

    const dialog = await openTeamsAddDialog(user)
    await user.type(
      await within(dialog).findByPlaceholderText(/Alpha FC/i),
      'Beta',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Ajouter' }))

    await waitFor(() => {
      expect(spy).toHaveBeenCalledWith(
        expect.objectContaining({
          queryKey: ['competitions', competitionId, 'cockpit'],
        }),
      )
    })
  })
})
