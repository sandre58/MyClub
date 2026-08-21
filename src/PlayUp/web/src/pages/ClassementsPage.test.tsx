import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, fetchConsultation } from '../api'
import { queryKeys } from '../queryKeys'
import type {
  ConsultationStandingRow,
  ConsultationView,
} from '../types'
import { ClassementsPage } from './ClassementsPage'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchConsultation: vi.fn(),
  }
})

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const entryA = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
const entryB = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
const entryC = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
const stageId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'

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
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return queryClient
}

describe('ClassementsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
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

    await screen.findByText('Alpha')

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
    expect(screen.getByText('Alpha')).toBeInTheDocument()
    expect(screen.getByText('Beta')).toBeInTheDocument()
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

  it('does not render detailed results from the composite payload', async () => {
    vi.mocked(fetchConsultation).mockResolvedValue(consultationView())

    renderClassementsPage()

    await screen.findByText('Alpha')

    expect(screen.queryByText('Journée 1')).not.toBeInTheDocument()
    expect(screen.queryByText('3–1')).not.toBeInTheDocument()
    expect(screen.queryByText('Finished')).not.toBeInTheDocument()
  })
})
