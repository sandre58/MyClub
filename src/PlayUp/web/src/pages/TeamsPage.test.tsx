import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  addCompetitionEntry,
  addDeclaredMember,
  deleteCompetitionEntry,
  fetchOrganisationView,
  removeDeclaredMember,
  removeDeclaredMembers,
  renameCompetitionEntry,
  renameDeclaredMember,
  withdrawCompetitionEntry,
  ApiError,
} from '../api'
import type { DeclaredMember, OrganisationView } from '../types'
import { TeamsPage } from './TeamsPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchOrganisationView: vi.fn(),
    addCompetitionEntry: vi.fn(),
    renameCompetitionEntry: vi.fn(),
    deleteCompetitionEntry: vi.fn(),
    withdrawCompetitionEntry: vi.fn(),
    addDeclaredMember: vi.fn(),
    removeDeclaredMember: vi.fn(),
    removeDeclaredMembers: vi.fn(),
    renameDeclaredMember: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const entryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const secondEntryId = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
const playerId = '11111111-1111-1111-1111-111111111111'
const staffId = '22222222-2222-2222-2222-222222222222'

function player(overrides: Partial<DeclaredMember> = {}): DeclaredMember {
  return {
    memberId: playerId,
    displayName: 'Dupont',
    role: 'Player',
    ...overrides,
  }
}

function organisationView(
  overrides: Partial<OrganisationView> = {},
  entryOverrides: Partial<OrganisationView['participants']['entries'][number]> = {},
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
          declaredMembers: [],
          ...entryOverrides,
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
    actions: ['AddEntry', 'RenameEntry', 'DeleteEntry'],
    readiness: {
      readyForNextSlice: false,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers: ['InsufficientParticipants'],
    },
    stages: [],
    ...overrides,
  }
}

function renderTeamsPage(initialPath = `/competitions/${competitionId}/teams`) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route
            path="/competitions/:competitionId/teams/:entryId"
            element={<TeamsPage />}
          />
          <Route
            path="/competitions/:competitionId/teams"
            element={<TeamsPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return { queryClient }
}

function identityNameInput(dialog: HTMLElement) {
  return within(dialog).getByRole('textbox', {
    name: (accessibleName) =>
      accessibleName.replace(/\s*\*$/, '').trim() === 'Nom',
  })
}

function identityShortNameInput(dialog: HTMLElement) {
  return within(dialog).getByRole('textbox', {
    name: (accessibleName) =>
      accessibleName.replace(/\s*\*$/, '').trim() === 'Nom court',
  })
}

describe('TeamsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(addCompetitionEntry).mockResolvedValue(organisationView())
    vi.mocked(renameCompetitionEntry).mockResolvedValue(organisationView())
    vi.mocked(deleteCompetitionEntry).mockResolvedValue(organisationView())
    vi.mocked(withdrawCompetitionEntry).mockResolvedValue(organisationView())
    vi.mocked(addDeclaredMember).mockResolvedValue(organisationView())
    vi.mocked(removeDeclaredMember).mockResolvedValue(organisationView())
    vi.mocked(removeDeclaredMembers).mockResolvedValue(organisationView())
    vi.mocked(renameDeclaredMember).mockResolvedValue(organisationView())
  })

  it('shows the plateau gauge in Draft and Ready', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    expect(
      await screen.findByRole('heading', { name: /Équipes.*1/ }),
    ).toBeInTheDocument()
    expect(screen.getByText('Alpha')).toBeInTheDocument()

    const titleCount = document.querySelector('.ds-page-head__title-meta')
    expect(titleCount).not.toBeNull()
    expect(titleCount).toHaveTextContent('1')

    const plateau = document.querySelector('.teams__plateau')
    expect(plateau).not.toBeNull()
    expect(plateau).toHaveTextContent('Encore 1 équipe nécessaire')
    expect(plateau).toHaveTextContent('min. 2')
    expect(plateau).toHaveTextContent('63 places disponibles')
    expect(plateau).not.toHaveTextContent('ÉQUIPES ACTIVES')
    expect(plateau).not.toHaveTextContent('SEUIL DE DÉMARRAGE')

    const gauge = screen.getByRole('progressbar')
    expect(gauge).toHaveAttribute('aria-valuenow', '1')
    expect(gauge).toHaveAttribute('aria-valuemax', '64')
    expect(gauge).toHaveAttribute('data-tone', 'error')
  })

  it('shows the plateau gauge in all competition statuses', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        status: 'Running',
        actions: ['WithdrawEntry', 'RenameEntry'],
      }),
    )

    renderTeamsPage()

    expect(
      await screen.findByRole('heading', { name: /Équipes.*1/ }),
    ).toBeInTheDocument()
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('counts withdrawn entries in the title and gauge (occupation)', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        status: 'Running',
        actions: ['WithdrawEntry', 'RenameEntry'],
        participants: {
          activeCount: 1,
          occupyingCount: 2,
          entries: [
            {
              entryId,
              displayName: 'Alpha',
              status: 'Active',
              declaredMembers: [],
            },
            {
              entryId: secondEntryId,
              displayName: 'Beta',
              status: 'Withdrawn',
              declaredMembers: [],
            },
          ],
        },
      }),
    )

    renderTeamsPage()

    expect(
      await screen.findByRole('heading', { name: /Équipes.*2/ }),
    ).toBeInTheDocument()
    expect(document.querySelector('.ds-page-head__title-meta')).toHaveTextContent('2')
    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '2')
    expect(screen.getByText('Forfait')).toBeInTheDocument()
  })

  it('does not show Qualified or Eliminated badges (statuses removed from entry)', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {
          status: 'Running',
          actions: ['WithdrawEntry', 'RenameEntry'],
          participants: {
            activeCount: 2,
            occupyingCount: 2,
            entries: [
              {
                entryId,
                displayName: 'Alpha',
                status: 'Active',
                declaredMembers: [],
              },
              {
                entryId: secondEntryId,
                displayName: 'Beta',
                status: 'Active',
                declaredMembers: [],
              },
            ],
          },
        },
      ),
    )

    renderTeamsPage()

    expect(await screen.findByText('Alpha')).toBeInTheDocument()
    expect(screen.queryByText('Qualifié')).not.toBeInTheDocument()
    expect(screen.queryByText('Éliminé')).not.toBeInTheDocument()
  })

  it('shows player counts on the tile and in the fiche headings', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {},
        {
          declaredMembers: [
            player(),
            {
              memberId: staffId,
              displayName: 'Coach',
              role: 'Staff',
            },
          ],
        },
      ),
    )

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    expect(await screen.findByLabelText('1 joueur')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Joueurs· 1' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Staff· 1' })).toBeInTheDocument()
  })

  it('disables member remove when already on a match sheet', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {},
        {
          declaredMembers: [
            player({ referencedOnMatchSheet: true }),
          ],
        },
      ),
    )

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    const remove = await screen.findByRole('button', { name: 'Supprimer Dupont' })
    expect(remove).toBeDisabled()
    expect(remove).toHaveAttribute('title', 'Déjà sur une feuille de match')
  })

  it('shows a Forfait badge on a withdrawn team', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({}, { status: 'Withdrawn' }),
    )

    renderTeamsPage()

    expect(await screen.findByText('Forfait')).toBeInTheDocument()
  })

  it('does not allow withdrawing an already withdrawn team', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        { status: 'Running', actions: ['WithdrawEntry', 'RenameEntry'] },
        { status: 'Withdrawn' },
      ),
    )

    renderTeamsPage()

    expect(await screen.findByText('Forfait')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Faire forfait — Alpha' })).toBeDisabled()
  })

  it('shows empty tiles for the missing minimum and adds from them', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    expect(
      await screen.findByRole('button', { name: 'Inscrire une équipe' }),
    ).toBeInTheDocument()
    expect(screen.getByText('À inscrire')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Inscrire une équipe' }))
    expect(await screen.findByRole('dialog')).toHaveAccessibleName(
      'Ajouter une équipe',
    )
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
              { entryId: secondEntryId, displayName: 'Beta', status: 'Active' },
            ],
          },
        })
      }
      return organisationView()
    })

    renderTeamsPage()

    await user.click(
      await screen.findByRole('button', { name: /Ajouter une équipe/i }),
    )
    const dialog = await screen.findByRole('dialog')
    await user.type(identityNameInput(dialog), 'Beta')
    expect(identityShortNameInput(dialog)).toHaveValue('BETA')
    await user.click(within(dialog).getByRole('button', { name: 'Ajouter' }))

    await waitFor(() => {
      expect(addCompetitionEntry).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          displayName: 'Beta',
          shortName: 'BETA',
        }),
      )
    })
    expect((await screen.findAllByText('Beta')).length).toBeGreaterThan(0)
  })

  it('warns on duplicate display name without blocking submit', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    await user.click(
      await screen.findByRole('button', { name: /Ajouter une équipe/i }),
    )
    const dialog = await screen.findByRole('dialog')
    await user.type(identityNameInput(dialog), 'Alpha')
    expect(
      within(dialog).getByText('Une équipe porte déjà ce nom.'),
    ).toBeInTheDocument()
    expect(
      within(dialog).getByRole('button', { name: 'Ajouter' }),
    ).toBeEnabled()
  })

  it('confirms before closing a dirty identity dialog', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    await user.click(
      await screen.findByRole('button', { name: /Ajouter une équipe/i }),
    )
    const dialog = await screen.findByRole('dialog')
    await user.type(identityNameInput(dialog), 'Gamma')
    await user.click(within(dialog).getByRole('button', { name: 'Fermer' }))

    const confirm = await screen.findByRole('dialog', {
      name: 'Quitter sans enregistrer ?',
    })
    expect(
      within(confirm).getByText(
        'Les modifications non enregistrées seront perdues.',
      ),
    ).toBeInTheDocument()
    await user.click(within(confirm).getByRole('button', { name: 'Annuler' }))
    expect(screen.getByRole('dialog', { name: /Ajouter une équipe/i })).toBeInTheDocument()
  })

  it('greys the add button and shows cap reached in the plateau reading at capacity', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        participants: {
          activeCount: 2,
          occupyingCount: 2,
          entries: [
            { entryId, displayName: 'Alpha', status: 'Active' },
            { entryId: secondEntryId, displayName: 'Beta', status: 'Active' },
          ],
        },
        regulation: {
          minimumTeams: 2,
          maximumTeams: 2,
          durationPerPeriod: 45,
          numberOfPeriods: 2,
          winPoints: 3,
          drawPoints: 1,
          lossPoints: 0,
        },
      }),
    )

    renderTeamsPage()

    expect(await screen.findByText('Capacité maximale atteinte')).toBeInTheDocument()
    expect(screen.queryByText('✓ Minimum atteint')).not.toBeInTheDocument()
    expect(screen.queryByText(/places disponibles/i)).not.toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Ajouter une équipe/i }),
    ).toBeDisabled()
    expect(
      screen.queryByRole('button', { name: 'Inscrire une équipe' }),
    ).not.toBeInTheDocument()

    const gauge = screen.getByRole('progressbar')
    expect(gauge).toHaveAttribute('aria-valuenow', '2')
    expect(gauge).toHaveAttribute('aria-valuemax', '2')
    expect(gauge).toHaveAttribute('data-tone', 'info')
  })

  it('keeps the roster panel visible with an idle state', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    expect(
      await screen.findByText('Aucune équipe sélectionnée'),
    ).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Équipes.*1/ })).toBeInTheDocument()
  })

  it('keeps list view with selection after roster back on a narrow viewport', async () => {
    vi.spyOn(window, 'matchMedia').mockImplementation((query: string) => ({
      matches: query === '(max-width: 51.999rem)',
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }))

    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))
    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'detail',
    )

    await user.click(screen.getByRole('button', { name: 'Équipes' }))

    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'list',
    )
    expect(screen.getByRole('checkbox', { name: 'Sélectionner Alpha' })).toBeChecked()
    expect(document.querySelector('.ds-selection-bar__count')).toHaveTextContent(
      '1 équipe sélectionnée',
    )
  })

  it('opens the roster drawer from a tile and keeps the grid', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    await screen.findByText('Alpha')
    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'list',
    )

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))

    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'detail',
    )
    expect(
      screen.getByRole('button', { name: 'Équipes' }),
    ).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'Alpha' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Joueurs· 0' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Staff· 0' })).toBeInTheDocument()
    expect(
      screen.queryByText('Aucune équipe sélectionnée'),
    ).not.toBeInTheDocument()
  })

  it('shows selection chrome and switches to multi view', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        participants: {
          activeCount: 2,
          occupyingCount: 2,
          entries: [
            { entryId, displayName: 'Alpha', status: 'Active' },
            { entryId: secondEntryId, displayName: 'Beta', status: 'Active' },
          ],
        },
      }),
    )

    const user = userEvent.setup()
    renderTeamsPage()

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))
    await user.click(
      await screen.findByRole('checkbox', { name: 'Sélectionner Beta' }),
    )

    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'multi',
    )
    expect(document.querySelector('.teams__ops-row')).toBeInTheDocument()
    expect(document.querySelector('.ds-selection-bar__count')).toHaveTextContent(
      '2 équipes sélectionnées',
    )
    expect(document.querySelector('.teams__multi-band')).toBeNull()
  })

  it('returns to the list from the roster back control', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))
    await user.click(screen.getByRole('button', { name: 'Équipes' }))

    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'list',
    )
    expect(screen.getByRole('checkbox', { name: 'Sélectionner Alpha' })).toBeChecked()
    expect(
      screen.queryByText('Aucune équipe sélectionnée'),
    ).not.toBeInTheDocument()
  })

  it('lists players and staff in the drawer', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {},
        {
          declaredMembers: [
            player(),
            { memberId: staffId, displayName: 'Coach', role: 'Staff' },
          ],
        },
      ),
    )

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    expect(await screen.findByText('Dupont')).toBeInTheDocument()
    expect(screen.getByText('Coach')).toBeInTheDocument()
  })

  it('adds a staff member from the drawer', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockImplementation(async () => {
      if (vi.mocked(addDeclaredMember).mock.calls.length > 0) {
        return organisationView(
          {},
          {
            declaredMembers: [
              { memberId: staffId, displayName: 'Coach', role: 'Staff' },
            ],
          },
        )
      }
      return organisationView()
    })

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    await user.type(await screen.findByLabelText(/^Nom$/), 'Coach')
    await user.click(screen.getByRole('button', { name: 'Ajouter comme staff' }))
    await user.click(screen.getByRole('menuitem', { name: 'Ajouter comme staff' }))

    await waitFor(() => {
      expect(addDeclaredMember).toHaveBeenCalledWith(competitionId, entryId, {
        displayName: 'Coach',
        role: 'Staff',
      })
    })
    expect(await screen.findByText('Coach')).toBeInTheDocument()
  })

  it('opens identity dialog from the roster fiche pencil', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    await user.click(
      await screen.findByRole('button', { name: 'Modifier l’identité' }),
    )
    expect(
      await screen.findByRole('dialog', { name: 'Identité de l’équipe' }),
    ).toBeInTheDocument()
  })

  it('removes a selectable lot of members and skips those on match sheets', async () => {
    const user = userEvent.setup()
    const blockedId = '33333333-3333-3333-3333-333333333333'
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {},
        {
          declaredMembers: [
            player(),
            {
              memberId: blockedId,
              displayName: 'Sheeted',
              role: 'Player',
              referencedOnMatchSheet: true,
            },
            {
              memberId: staffId,
              displayName: 'Coach',
              role: 'Staff',
            },
          ],
        },
      ),
    )

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    await user.click(
      await screen.findByRole('checkbox', { name: 'Sélectionner Dupont' }),
    )
    await user.click(screen.getByRole('checkbox', { name: 'Sélectionner Sheeted' }))
    await user.click(screen.getByRole('checkbox', { name: 'Sélectionner Coach' }))
    await user.click(
      screen.getByRole('button', { name: 'Supprimer la sélection' }),
    )

    const confirm = await screen.findByRole('dialog')
    expect(confirm).toHaveTextContent('2 personnes peuvent être retirées')
    expect(within(confirm).getByRole('listitem')).toHaveTextContent('Sheeted')
    expect(confirm).toHaveTextContent(
      '1 personne ne peut pas être retirée (encore sur une feuille de match)',
    )
    await user.click(
      within(confirm).getByRole('button', { name: 'Supprimer 2 personnes' }),
    )

    await waitFor(() => {
      expect(removeDeclaredMembers).toHaveBeenCalledWith(
        competitionId,
        entryId,
        { memberIds: [playerId, staffId] },
      )
    })
    expect(removeDeclaredMember).not.toHaveBeenCalled()
  })

  it('asks for confirmation before removing a player', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockImplementation(async () => {
      if (vi.mocked(removeDeclaredMember).mock.calls.length > 0) {
        return organisationView()
      }
      return organisationView({}, { declaredMembers: [player()] })
    })

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    await user.click(await screen.findByRole('button', { name: 'Supprimer Dupont' }))
    expect(removeDeclaredMember).not.toHaveBeenCalled()

    const confirm = await screen.findByRole('dialog', {
      name: 'Retirer « Dupont » ?',
    })
    await user.click(within(confirm).getByRole('button', { name: 'Retirer' }))

    await waitFor(() => {
      expect(removeDeclaredMember).toHaveBeenCalledWith(
        competitionId,
        entryId,
        playerId,
      )
    })
  })

  it('renames a member inline without changing role', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockImplementation(async () => {
      if (vi.mocked(renameDeclaredMember).mock.calls.length > 0) {
        return organisationView(
          {},
          {
            declaredMembers: [
              { ...player(), displayName: 'Jean Dupont' },
            ],
          },
        )
      }
      return organisationView({}, { declaredMembers: [player()] })
    })

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    await user.click(await screen.findByRole('button', { name: 'Modifier Dupont' }))
    const input = screen.getByDisplayValue('Dupont')
    await user.clear(input)
    await user.type(input, 'Jean Dupont')
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }))

    await waitFor(() => {
      expect(renameDeclaredMember).toHaveBeenCalledWith(
        competitionId,
        entryId,
        playerId,
        { displayName: 'Jean Dupont' },
      )
    })
    expect(await screen.findByText('Jean Dupont')).toBeInTheDocument()
  })

  it('suspends member selection while a row is being renamed', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {},
        {
          declaredMembers: [
            player(),
            { memberId: staffId, displayName: 'Coach', role: 'Staff' },
          ],
        },
      ),
    )

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    const selectDupont = await screen.findByRole('checkbox', {
      name: 'Sélectionner Dupont',
    })
    await user.click(selectDupont)
    expect(selectDupont).toBeChecked()
    expect(screen.getByText('1 personne sélectionnée')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Modifier Dupont' }))
    expect(screen.getByRole('checkbox', { name: 'Sélectionner Dupont' })).toBeDisabled()
    expect(screen.getByRole('checkbox', { name: 'Sélectionner Coach' })).toBeDisabled()
    expect(
      screen.getByRole('button', { name: 'Supprimer la sélection' }),
    ).toBeDisabled()
  })

  it('clears multi-selection on Escape, but not while ConfirmDialog is open', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        participants: {
          activeCount: 2,
          occupyingCount: 2,
          entries: [
            { entryId, displayName: 'Alpha', status: 'Active' },
            { entryId: secondEntryId, displayName: 'Beta', status: 'Active' },
          ],
        },
      }),
    )

    const user = userEvent.setup()
    renderTeamsPage()

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))
    await user.click(
      await screen.findByRole('checkbox', { name: 'Sélectionner Beta' }),
    )
    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'multi',
    )

    await user.click(screen.getByRole('button', { name: 'Supprimer' }))
    const confirm = await screen.findByRole('dialog', {
      name: /Supprimer/,
    })

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(confirm).not.toBeInTheDocument()
    })
    expect(document.querySelector('.teams')).toHaveAttribute(
      'data-teams-view',
      'multi',
    )
    expect(screen.getByRole('checkbox', { name: 'Sélectionner Alpha' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Sélectionner Beta' })).toBeChecked()

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(document.querySelector('.teams')).toHaveAttribute(
        'data-teams-view',
        'list',
      )
    })
    expect(
      screen.queryByRole('checkbox', { name: 'Sélectionner Alpha' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('checkbox', { name: 'Sélectionner Beta' }),
    ).not.toBeInTheDocument()
  })

  it('clears selection on Escape when no overlay is open', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        participants: {
          activeCount: 2,
          occupyingCount: 2,
          entries: [
            { entryId, displayName: 'Alpha', status: 'Active' },
            { entryId: secondEntryId, displayName: 'Beta', status: 'Active' },
          ],
        },
      }),
    )

    const user = userEvent.setup()
    renderTeamsPage()

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))
    await user.click(
      await screen.findByRole('checkbox', { name: 'Sélectionner Beta' }),
    )

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(document.querySelector('.teams')).toHaveAttribute(
        'data-teams-view',
        'list',
      )
    })
    expect(
      screen.queryByRole('checkbox', { name: 'Sélectionner Alpha' }),
    ).not.toBeInTheDocument()
  })

  it('deletes an entry from the selected tile', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderTeamsPage()

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))
    await user.click(screen.getByRole('button', { name: 'Supprimer Alpha' }))

    const confirm = await screen.findByRole('dialog', {
      name: 'Supprimer « Alpha » ?',
    })
    expect(confirm).toHaveTextContent(
      'Les matchs qui la concernent seront aussi supprimés',
    )
    await user.click(within(confirm).getByRole('button', { name: 'Supprimer' }))

    await waitFor(() => {
      expect(deleteCompetitionEntry).toHaveBeenCalledWith(competitionId, entryId)
    })
    expect(withdrawCompetitionEntry).not.toHaveBeenCalled()
  })

  it('withdraws when DeleteEntry is not available', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        status: 'Running',
        actions: ['WithdrawEntry'],
      }),
    )

    renderTeamsPage()

    await user.click(await screen.findByRole('button', { name: 'Alpha' }))
    await user.click(screen.getByRole('button', { name: 'Faire forfait — Alpha' }))

    const confirm = await screen.findByRole('dialog', {
      name: 'Faire forfait — « Alpha » ?',
    })
    expect(confirm).toHaveTextContent(
      'Ses matchs restants seront terminés par forfait administratif',
    )
    await user.click(within(confirm).getByRole('button', { name: 'Faire forfait' }))

    await waitFor(() => {
      expect(withdrawCompetitionEntry).toHaveBeenCalledWith(
        competitionId,
        entryId,
      )
    })
    expect(deleteCompetitionEntry).not.toHaveBeenCalled()
  })

  it('is read-only when the competition is completed', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
        status: 'Completed',
        actions: [],
      }),
    )

    renderTeamsPage()

    expect(await screen.findByText('Alpha')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Ajouter une équipe/i }),
    ).toBeDisabled()
    expect(
      screen.queryByRole('checkbox', { name: /Sélectionner Alpha/ }),
    ).not.toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Modifier Alpha' }),
    ).toBeEnabled()
    expect(
      screen.getByRole('button', { name: 'Faire forfait — Alpha' }),
    ).toBeDisabled()
  })

  it('keeps roster editable when the competition is completed', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        { status: 'Completed', actions: [] },
        { declaredMembers: [player()] },
      ),
    )

    renderTeamsPage(`/competitions/${competitionId}/teams/${entryId}`)

    expect(
      await screen.findByRole('button', { name: 'Modifier Dupont' }),
    ).toBeEnabled()
    expect(
      screen.getByRole('button', { name: 'Supprimer Dupont' }),
    ).toBeEnabled()
  })
})
