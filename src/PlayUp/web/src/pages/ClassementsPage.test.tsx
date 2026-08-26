import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, fetchConsultation, fetchOrganisationView } from '../api'
import { queryKeys } from '../queryKeys'
import type {
  ConsultationStandingRow,
  ConsultationView,
  OrganisationView,
} from '../types'
import { ClassementsPage } from './ClassementsPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchConsultation: vi.fn(),
    fetchOrganisationView: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const entryA = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const entryB = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
const entryC = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
const stageId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'

function organisationView(
  overrides: Partial<OrganisationView> = {},
): OrganisationView {
  return {
    competitionId,
    name: 'Ligue Printemps',
    status: 'InProgress',
    participants: {
      activeCount: 2,
      occupyingCount: 2,
      entries: [],
    },
    format: {
      kind: 'Championship',
      primaryStageId: stageId,
      primaryStageName: 'Phase 1',
      primaryStageStatus: 'InProgress',
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
      matchdayCount: 1,
      slotCount: 0,
      hasDrawRules: false,
      numberOfPots: null,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    actions: [],
    readiness: {
      readyForNextSlice: true,
      readyForDraw: false,
      readyForMaterialization: true,
      readyForSchedule: true,
      readyForMatchOperation: true,
      readyForSchedulePath: true,
      attachedMatchCount: 1,
      blockers: [],
    },
    ...overrides,
  }
}

function standingRow(
  overrides: Partial<ConsultationStandingRow> &
    Pick<ConsultationStandingRow, 'position' | 'entryId' | 'displayName'>,
): ConsultationStandingRow {
  return {
    played: 0,
    wins: 0,
    draws: 0,
    losses: 0,
    goalsFor: 0,
    goalsAgainst: 0,
    goalDifference: 0,
    points: 0,
    ...overrides,
  }
}

function consultationView(
  overrides: Partial<ConsultationView> = {},
): ConsultationView {
  return {
    competitionId,
    name: 'Ligue Printemps',
    status: 'InProgress',
    completionMode: null,
    formatKind: 'Championship',
    formatLabel: 'Championnat',
    results: [
      {
        matchId: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
        stageId,
        fixtureId: null,
        roundId: null,
        matchdayNumber: 1,
        contextLabel: 'Journée 1',
        status: 'Finished',
        home: { entryId: entryA, displayName: 'Alpha' },
        away: { entryId: entryB, displayName: 'Beta' },
        score: { homeGoals: 3, awayGoals: 1 },
        resultType: 'Regular',
        scheduledAt: null,
      },
    ],
    standings: {
      applicable: true,
      notApplicableReason: null,
      tables: [
        {
          scope: 'Overall',
          stageId,
          stageName: 'Phase 1',
          groupId: null,
          groupName: null,
          rows: [
            standingRow({
              position: 1,
              entryId: entryA,
              displayName: 'Alpha',
              played: 1,
              wins: 1,
              points: 3,
              goalsFor: 3,
              goalsAgainst: 1,
              goalDifference: 2,
            }),
            standingRow({
              position: 2,
              entryId: entryB,
              displayName: 'Beta',
              played: 1,
              losses: 1,
              goalsFor: 1,
              goalsAgainst: 3,
              goalDifference: -2,
            }),
          ],
        },
      ],
    },
    structure: {
      formatKind: 'Championship',
      stages: [],
    },
    ...overrides,
  }
}

function renderClassementsPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/competitions/${competitionId}/classements`]}>
        <Routes>
          <Route
            path="/competitions/:competitionId/classements"
            element={<ClassementsPage />}
          />
          <Route
            path="/competitions/:competitionId"
            element={<p>Cockpit route</p>}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return queryClient
}

describe('ClassementsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView())
  })

  it('shows loading while consultation is pending', () => {
    vi.mocked(fetchConsultation).mockReturnValue(new Promise(() => {}))

    renderClassementsPage()

    expect(screen.getByRole('status')).toHaveTextContent(
      'Chargement des classements…',
    )
  })

  it('shows error when the GET fails', async () => {
    vi.mocked(fetchConsultation).mockRejectedValue(
      new ApiError(500, 'Server error'),
    )

    renderClassementsPage()

    expect(await screen.findByRole('alert')).toBeInTheDocument()
  })

  it('uses the consultation query key and a single fetch', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(consultationView())
    const client = renderClassementsPage()

    await screen.findByTestId(`standing-row-${entryA}`)

    expect(fetchConsultation).toHaveBeenCalledTimes(1)
    expect(fetchConsultation).toHaveBeenCalledWith(competitionId)
    expect(
      client.getQueryData(queryKeys.competitions.consultation(competitionId)),
    ).toBeTruthy()
  })

  it('renders applicable standings rows from the Read', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(consultationView())

    renderClassementsPage()

    expect(await screen.findByText('Classement général')).toBeInTheDocument()
    expect(screen.getByTestId(`standing-row-${entryA}`)).toHaveTextContent(
      'Alpha',
    )
    expect(screen.getByTestId(`standing-row-${entryB}`)).toHaveTextContent(
      'Beta',
    )
    expect(screen.getByText('Ligue Printemps')).toBeInTheDocument()
  })

  it('preserves Read row order (no client sort)', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(
      consultationView({
        standings: {
          applicable: true,
          notApplicableReason: null,
          tables: [
            {
              scope: 'Overall',
              stageId,
              stageName: 'Phase 1',
              groupId: null,
              groupName: null,
              rows: [
                standingRow({
                  position: 3,
                  entryId: entryC,
                  displayName: 'Gamma',
                  points: 1,
                }),
                standingRow({
                  position: 1,
                  entryId: entryA,
                  displayName: 'Alpha',
                  points: 9,
                }),
                standingRow({
                  position: 2,
                  entryId: entryB,
                  displayName: 'Beta',
                  points: 6,
                }),
              ],
            },
          ],
        },
      }),
    )

    renderClassementsPage()

    const table = await screen.findByRole('table')
    const bodies = within(table).getAllByRole('row').slice(1)
    expect(bodies.map((row) => within(row).getAllByRole('cell')[1].textContent)).toEqual([
      'Gamma',
      'Alpha',
      'Beta',
    ])
  })

  it('displays Read Diff / Pts / Position without recalculating', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(
      consultationView({
        standings: {
          applicable: true,
          notApplicableReason: null,
          tables: [
            {
              scope: 'Overall',
              stageId,
              stageName: 'Phase 1',
              groupId: null,
              groupName: null,
              rows: [
                standingRow({
                  position: 7,
                  entryId: entryA,
                  displayName: 'Alpha',
                  goalsFor: 20,
                  goalsAgainst: 10,
                  goalDifference: 99,
                  points: 42,
                }),
              ],
            },
          ],
        },
      }),
    )

    renderClassementsPage()

    const row = await screen.findByTestId(`standing-row-${entryA}`)
    const cells = within(row).getAllByRole('cell')
    expect(cells[0]).toHaveTextContent('7')
    expect(cells[6]).toHaveTextContent('20')
    expect(cells[7]).toHaveTextContent('10')
    expect(cells[8]).toHaveTextContent('+99')
    expect(cells[9]).toHaveTextContent('42')
    expect(cells[8]).not.toHaveTextContent('+10')
  })

  it('renders multiple group tables from the contract', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(
      consultationView({
        formatKind: 'Groups',
        formatLabel: 'Groupes',
        standings: {
          applicable: true,
          notApplicableReason: null,
          tables: [
            {
              scope: 'Group',
              stageId,
              stageName: 'Poules',
              groupId: '11111111-1111-1111-1111-111111111111',
              groupName: 'Groupe A',
              rows: [
                standingRow({
                  position: 1,
                  entryId: entryA,
                  displayName: 'Alpha',
                }),
              ],
            },
            {
              scope: 'Group',
              stageId,
              stageName: 'Poules',
              groupId: '22222222-2222-2222-2222-222222222222',
              groupName: 'Groupe B',
              rows: [
                standingRow({
                  position: 1,
                  entryId: entryB,
                  displayName: 'Beta',
                }),
              ],
            },
          ],
        },
      }),
    )

    renderClassementsPage()

    expect(await screen.findByText('Groupe A')).toBeInTheDocument()
    expect(screen.getByText('Groupe B')).toBeInTheDocument()
  })

  it('shows NotApplicable with CupFormat reason', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(
      consultationView({
        formatKind: 'Cup',
        formatLabel: 'Coupe',
        standings: {
          applicable: false,
          notApplicableReason: 'CupFormat',
          tables: [],
        },
      }),
    )

    renderClassementsPage()

    expect(
      await screen.findByText('Classement non applicable'),
    ).toBeInTheDocument()
    expect(
      screen.getByText('Ce format (coupe) ne produit pas de classement.'),
    ).toBeInTheDocument()
  })

  it('shows NotApplicable with NoStructure reason', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(
      consultationView({
        formatKind: null,
        formatLabel: 'Non configuré',
        standings: {
          applicable: false,
          notApplicableReason: 'NoStructure',
          tables: [],
        },
      }),
    )

    renderClassementsPage()

    expect(
      await screen.findByText(
        'Aucune structure sportive ne permet encore de calculer un classement.',
      ),
    ).toBeInTheDocument()
  })

  it('shows empty when applicable with empty rows', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(
      consultationView({
        standings: {
          applicable: true,
          notApplicableReason: null,
          tables: [
            {
              scope: 'Overall',
              stageId,
              stageName: 'Phase 1',
              groupId: null,
              groupName: null,
              rows: [],
            },
          ],
        },
      }),
    )

    renderClassementsPage()

    expect(
      await screen.findByText('Aucun classement à afficher pour le moment.'),
    ).toBeInTheDocument()
  })

  it('renders the last matchday slice from consultation results', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(consultationView())

    renderClassementsPage()

    expect(await screen.findByRole('heading', { name: 'Journée 1' })).toBeInTheDocument()
    const score = document.querySelector('.classements-result__score')
    expect(score).toHaveTextContent('3–1')
    expect(screen.queryByText('Finished')).not.toBeInTheDocument()

    const matchLink = document.querySelector(
      'a.classements-result',
    ) as HTMLAnchorElement | null
    expect(matchLink?.getAttribute('href')).toBe(
      '/matches/ffffffff-ffff-ffff-ffff-ffffffffffff',
    )

    const allMatches = screen.getByRole('link', {
      name: /Voir tous les matchs/i,
    })
    expect(allMatches).toHaveAttribute(
      'href',
      `/competitions/${competitionId}/matches`,
    )
  })

  it('renders regulation points from organisation', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(consultationView())

    renderClassementsPage()

    expect(await screen.findByText('3 pts')).toBeInTheDocument()
    expect(screen.getByText('1 pts')).toBeInTheDocument()
    expect(screen.getByText('0 pts')).toBeInTheDocument()
    expect(screen.getByText('2 × 45 min')).toBeInTheDocument()
    expect(screen.getByText('2 – 64 équipes')).toBeInTheDocument()
  })

  it('links the regulation panel to organisation', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(consultationView())

    renderClassementsPage()

    const link = await screen.findByRole('link', {
      name: /Voir l’organisation/i,
    })
    expect(link).toHaveAttribute(
      'href',
      `/competitions/${competitionId}/organisation`,
    )
  })
})
