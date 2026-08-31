import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  addDeclaredMember,
  fetchOrganisationView,
  removeDeclaredMember,
  renameDeclaredMember,
  ApiError,
} from '../api'
import type { DeclaredMember, OrganisationView } from '../types'
import { EntryRosterPage } from './EntryRosterPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchOrganisationView: vi.fn(),
    addDeclaredMember: vi.fn(),
    removeDeclaredMember: vi.fn(),
    renameDeclaredMember: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const entryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const playerId = '11111111-1111-1111-1111-111111111111'
const staffId = '22222222-2222-2222-2222-222222222222'

function player(
  overrides: Partial<DeclaredMember> = {},
): DeclaredMember {
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
    ...overrides,
  }
}

function renderRosterPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[
          `/competitions/${competitionId}/organisation/entries/${entryId}`,
        ]}
      >
        <Routes>
          <Route
            path="/competitions/:competitionId/organisation/entries/:entryId"
            element={<EntryRosterPage />}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation hub</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('EntryRosterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(addDeclaredMember).mockResolvedValue(organisationView())
    vi.mocked(removeDeclaredMember).mockResolvedValue(organisationView())
    vi.mocked(renameDeclaredMember).mockResolvedValue(organisationView())
  })

  it('treats an empty player list as a licit state', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderRosterPage()

    expect(await screen.findByRole('heading', { name: 'Alpha' })).toBeInTheDocument()
    expect(screen.getByText('Aucun joueur')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Ajouter' }),
    ).toBeInTheDocument()
  })

  it('lists players only and hides staff', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({}, {
        declaredMembers: [
          player(),
          { memberId: staffId, displayName: 'Coach', role: 'Staff' },
        ],
      }),
    )

    renderRosterPage()

    expect(await screen.findByText('Dupont')).toBeInTheDocument()
    expect(screen.queryByText('Coach')).not.toBeInTheDocument()
  })

  it('adds a player and stays on the page for serial add', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockImplementation(async () => {
      if (vi.mocked(addDeclaredMember).mock.calls.length > 0) {
        return organisationView(
          {},
          { declaredMembers: [player({ displayName: 'Martin' })] },
        )
      }
      return organisationView()
    })

    renderRosterPage()

    await user.type(
      await screen.findByLabelText(/Nom du joueur/i),
      'Martin',
    )
    await user.click(screen.getByRole('button', { name: 'Ajouter' }))

    await waitFor(() => {
      expect(addDeclaredMember).toHaveBeenCalledWith(competitionId, entryId, {
        displayName: 'Martin',
        role: 'Player',
      })
    })
    expect(await screen.findByText('Martin')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Alpha' })).toBeInTheDocument()
    expect(screen.queryByText('Organisation hub')).not.toBeInTheDocument()
    expect(screen.getByLabelText(/Nom du joueur/i)).toHaveValue('')
  })

  it('asks for a product confirmation before removing a player', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm')
    vi.mocked(fetchOrganisationView).mockImplementation(async () => {
      if (vi.mocked(removeDeclaredMember).mock.calls.length > 0) {
        return organisationView()
      }
      return organisationView({}, { declaredMembers: [player()] })
    })

    renderRosterPage()

    await user.click(await screen.findByRole('button', { name: 'Retirer' }))

    expect(removeDeclaredMember).not.toHaveBeenCalled()
    expect(confirmSpy).not.toHaveBeenCalled()
    expect(
      screen.getByText(/Cette inscription sera perdue/i),
    ).toBeInTheDocument()

    await user.click(
      screen.getByRole('button', { name: 'Confirmer le retrait' }),
    )

    await waitFor(() => {
      expect(removeDeclaredMember).toHaveBeenCalledWith(
        competitionId,
        entryId,
        playerId,
      )
    })
    expect(await screen.findByText('Aucun joueur')).toBeInTheDocument()
  })

  it('renames a player inline without leaving the page', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockImplementation(async () => {
      if (vi.mocked(renameDeclaredMember).mock.calls.length > 0) {
        return organisationView(
          {},
          { declaredMembers: [player({ displayName: 'Martin' })] },
        )
      }
      return organisationView({}, { declaredMembers: [player()] })
    })

    renderRosterPage()

    await user.click(await screen.findByRole('button', { name: 'Renommer' }))
    const renameInput = screen.getByLabelText(/Nouveau nom/i)
    expect(renameInput).toHaveValue('Dupont')
    await user.clear(renameInput)
    await user.type(renameInput, 'Martin')
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }))

    await waitFor(() => {
      expect(renameDeclaredMember).toHaveBeenCalledWith(
        competitionId,
        entryId,
        playerId,
        { displayName: 'Martin' },
      )
    })
    expect(await screen.findByText('Martin')).toBeInTheDocument()
    expect(screen.queryByLabelText(/Nouveau nom/i)).not.toBeInTheDocument()
  })

  it('shows the sheet-blocked refusal after confirm', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({}, { declaredMembers: [player()] }),
    )
    vi.mocked(removeDeclaredMember).mockRejectedValue(
      new ApiError(
        400,
        'still on a sheet',
        'still on a sheet',
        'Application.DeclaredMemberReferencedByMatchSheet',
      ),
    )

    renderRosterPage()

    await user.click(await screen.findByRole('button', { name: 'Retirer' }))
    await user.click(
      screen.getByRole('button', { name: 'Confirmer le retrait' }),
    )

    expect(await screen.findByRole('alert')).toHaveTextContent(
      /feuille de match/i,
    )
    expect(screen.getByText('Dupont')).toBeInTheDocument()
  })

  it('is read-only when the entry is withdrawn', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {},
        {
          status: 'Withdrawn',
          declaredMembers: [player()],
        },
      ),
    )

    renderRosterPage()

    expect(await screen.findByText('Dupont')).toBeInTheDocument()
    expect(
      screen.getByText(/ne peuvent plus être modifiés/i),
    ).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Ajouter' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Retirer' })).not.toBeInTheDocument()
  })

  it('is read-only when the entry is excluded', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView(
        {},
        {
          status: 'Excluded',
          declaredMembers: [player()],
        },
      ),
    )

    renderRosterPage()

    expect(await screen.findByText('Dupont')).toBeInTheDocument()
    expect(screen.getByText('Exclu')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Ajouter' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Retirer' })).not.toBeInTheDocument()
  })

  it('returns to Organisation', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderRosterPage()

    await user.click(await screen.findByRole('link', { name: /Organisation/ }))

    expect(screen.getByText('Organisation hub')).toBeInTheDocument()
  })
})
