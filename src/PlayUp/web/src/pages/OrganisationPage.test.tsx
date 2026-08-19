import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
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
      label: 'Non configuré',
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
      hints: ['Add more teams', 'Configure structure'],
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
          label: 'Championnat',
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

    expect(screen.getByRole('status')).toHaveTextContent('Loading…')
  })

  it('renders organisation summary from the Host DTO', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    expect(
      await screen.findByRole('heading', { name: 'Organisation' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Alpha')).toBeInTheDocument()
    // Entry status is a badge next to the name (13.5), no longer “Alpha (Active)”.
    expect(screen.getByText('Active')).toBeInTheDocument()
    expect(screen.getByText('InsufficientParticipants')).toBeInTheDocument()
    expect(screen.getByText(/2–64/)).toBeInTheDocument()
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

    expect(await screen.findByText(/No entries yet/i)).toBeInTheDocument()
  })

  it('shows an error when organisation read fails', async () => {
    vi.mocked(fetchOrganisationView).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    )

    renderOrganisationPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Not found. Check the id in the URL.',
    )
  })

  it('navigates back to workspace', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    await user.click(
      await screen.findByRole('link', { name: /Back to workspace/i }),
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

    await user.type(
      await screen.findByLabelText(/New entry name/i),
      'Beta',
    )
    await user.click(screen.getByRole('button', { name: 'Add entry' }))

    await waitFor(() => {
      expect(addCompetitionEntry).toHaveBeenCalledWith(competitionId, {
        displayName: 'Beta',
      })
    })
    expect(await screen.findByText('Beta')).toBeInTheDocument()
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

    await user.type(
      await screen.findByLabelText(/New entry name/i),
      'Beta',
    )
    await user.click(screen.getByRole('button', { name: 'Add entry' }))

    expect(
      await screen.findByRole('button', { name: 'Adding…' }),
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

    await user.type(
      await screen.findByLabelText(/New entry name/i),
      'Overflow',
    )
    await user.click(screen.getByRole('button', { name: 'Add entry' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Entry capacity exceeded (400)',
    )
  })

  it('renames an entry with the Host payload', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())

    renderOrganisationPage()

    const renameInput = await screen.findByLabelText(/Rename/i)
    await user.clear(renameInput)
    await user.type(renameInput, 'Alpha FC')
    await user.click(screen.getByRole('button', { name: 'Rename' }))

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

    await user.click(await screen.findByRole('button', { name: 'Withdraw' }))

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

    await user.click(await screen.findByRole('button', { name: 'Exclude' }))

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

    const minTeams = await screen.findByLabelText(/Minimum teams/i)
    await user.clear(minTeams)
    await user.type(minTeams, '4')
    await user.click(screen.getByRole('button', { name: 'Save regulation' }))

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

    await user.selectOptions(
      await screen.findByLabelText(/^Format$/i),
      'Championship',
    )
    const matchdays = screen.getByLabelText(/Matchday count/i)
    fireEvent.change(matchdays, { target: { value: '2' } })
    await user.click(
      screen.getByRole('button', { name: 'Configure structure' }),
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
        }),
      )
    })
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
      screen.queryByRole('button', { name: 'Add entry' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Save regulation' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Configure structure' }),
    ).not.toBeInTheDocument()
  })
})
