import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  ApiError,
  fetchCompetitionCockpit,
  fetchOrganisationView,
  materializeMatches,
  prepareCompetition,
  prepareStage,
  startCompetition,
} from '../api'
import { CompetitionCockpitPage } from './CompetitionCockpitPage'
import { cockpitIds, cockpitSituation, cockpitView, referenceStageGameRules } from '../test/cockpitFixtures'
import type { CockpitView } from '../types'

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>()
  return {
    ...actual,
    fetchCompetitionCockpit: vi.fn(),
    fetchOrganisationView: vi.fn(),
    prepareStage: vi.fn(),
    prepareCompetition: vi.fn(),
    startCompetition: vi.fn(),
    materializeMatches: vi.fn(),
  }
})

const { competitionId, stageId } = cockpitIds

function renderCockpitPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/competitions/${competitionId}`]}>
        <Routes>
          <Route
            path="/competitions/:competitionId"
            element={<CompetitionCockpitPage />}
          />
          <Route
            path="/competitions/:competitionId/organisation"
            element={<p>Organisation route</p>}
          />
          <Route
            path="/competitions/:competitionId/matches"
            element={<p>Match hub route</p>}
          />
          <Route
            path="/competitions/:competitionId/classements"
            element={<p>Classements route</p>}
          />
          <Route
            path="/competitions/:competitionId/overview"
            element={<p>Overview route</p>}
          />
          <Route path="/stages/:stageId" element={<p>Stage route</p>} />
          <Route path="/matches/:matchId" element={<p>Match route</p>} />
          <Route path="/competitions" element={<p>List route</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function inProgressLayoutBase(overrides: Partial<CockpitView> = {}): CockpitView {
  return cockpitView({
    status: 'Running',
    cycleReading: { code: 'InProgress' },
    constructionDimensions: {
      ...cockpitView().constructionDimensions,
      teams: {
        prominence: 'Condensed',
        facts: { activeCount: '4', minimumTeams: '2', maximumTeams: '64' },
      },
      structure: {
        prominence: 'Condensed',
        facts: {
          formatKind: 'Championship',
          groupCount: '0',
          roundCount: '0',
          matchdayCount: '34',
          slotCount: '0',
        },
      },
      regulation: {
        ...cockpitView().constructionDimensions.regulation,
        prominence: 'Condensed',
      },
      matches: {
        prominence: 'Dominant',
        facts: { live: '0', scheduled: '0', finished: '0', total: '0' },
      },
    },
    operationalFocus: {
      ...cockpitView().operationalFocus,
      referenceStageGameRules: referenceStageGameRules(),
    },
    naturalProgression: null,
    availableActions: [],
    attentionSummary: { count: 0, items: [] },
    ...overrides,
  })
}

function expectOverviewRegionOrder(...regionTestIds: string[]) {
  const overview = document.querySelector('.overview')
  expect(overview).not.toBeNull()
  const elements = regionTestIds.map((id) =>
    overview!.querySelector(`[data-testid="${id}"]`),
  )
  for (const el of elements) {
    expect(el).not.toBeNull()
  }
  for (let i = 0; i < elements.length - 1; i++) {
    const relation = elements[i]!.compareDocumentPosition(elements[i + 1]!)
    expect(relation & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  }
}

function expectOverviewRegionsAbsent(...regionTestIds: string[]) {
  const overview = document.querySelector('.overview')
  expect(overview).not.toBeNull()
  for (const id of regionTestIds) {
    expect(overview!.querySelector(`[data-testid="${id}"]`)).toBeNull()
  }
}

describe('CompetitionCockpitPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchOrganisationView).mockResolvedValue({
      competitionId,
      name: 'Spring Cup',
      status: 'Draft',
      participants: {
        activeCount: 1,
        occupyingCount: 1,
        entries: [
          {
            entryId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
            displayName: 'FC Test',
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
    })
  })

  it('shows loading while the cockpit is pending', () => {
    vi.mocked(fetchCompetitionCockpit).mockReturnValue(new Promise(() => {}))

    renderCockpitPage()

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…')
  })

  it('renders Préparation overview without cycle panel or console blocks', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        situations: [
          cockpitSituation({
            source: 'InsufficientParticipants',
            actionCode: 'AddEntry',
            actionable: true,
            impactCode: 'BlocksConstruction',
            params: { minimumTeams: '2', activeCount: '1' },
          }),
        ],
        attentionSummary: {
          count: 1,
          items: [
            cockpitSituation({
              source: 'InsufficientParticipants',
              actionCode: 'AddEntry',
              actionable: true,
              impactCode: 'BlocksConstruction',
              params: { minimumTeams: '2', activeCount: '1' },
            }),
          ],
        },
        availableActions: [
          { code: 'AddEntry', guaranteed: false },
          {
            code: 'PrepareStage',
            guaranteed: false,
            stageId,
            params: { stageName: 'Phase 1' },
          },
        ],
        naturalProgression: { code: 'PrepareStage' },
        closureHint: {
          canCompleteNormally: false,
          blockerCodes: ['ScheduledMatches'],
        },
      }),
    )

    renderCockpitPage()

    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument()
    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Équipes' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Structure' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'À traiter' })).toBeInTheDocument()
    expect(screen.getByText(/2×45 min/)).toBeInTheDocument()
    expect(screen.queryByText(/situation\(s\) à traiter/i)).not.toBeInTheDocument()
    expect(screen.getAllByText('Participants insuffisants')).toHaveLength(1)
    expect(screen.getByText('Minimum requis : 2')).toBeInTheDocument()
    expect(screen.queryByText(/Bloque la préparation/)).not.toBeInTheDocument()
    expect(
      screen.getByText(/Finalisez la configuration de la phase/),
    ).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Clôture' })).not.toBeInTheDocument()
    expect(screen.queryByText('Focus opérationnel')).not.toBeInTheDocument()
    expect(screen.queryByText('Espaces métier')).not.toBeInTheDocument()
    expect(screen.queryByText('Socle de construction')).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Actions disponibles' }),
    ).not.toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Préparer la phase/i }),
    ).toBeInTheDocument()
    const structure = screen.getByTestId('overview-structure-construction')
    expect(
      within(structure).queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /Ajouter une équipe/i }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Renommer une équipe/i }),
    ).not.toBeInTheDocument()
    expectOverviewRegionOrder(
      'overview-region-progression',
      'overview-region-attention',
      'overview-region-config',
      'overview-region-structure',
    )
  })

  describe('Préparation layout DOM order', () => {
    it('orders Prochaine action then config then Structure when tip only', async () => {
      vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
        cockpitView({
          naturalProgression: { code: 'PrepareStage' },
          availableActions: [
            {
              code: 'PrepareStage',
              guaranteed: false,
              stageId,
              params: { stageName: 'Phase 1' },
            },
          ],
          situations: [],
          attentionSummary: { count: 0, items: [] },
        }),
      )

      renderCockpitPage()

      await screen.findByTestId('overview-region-progression')
      expectOverviewRegionOrder(
        'overview-region-progression',
        'overview-region-config',
        'overview-region-structure',
      )
      expectOverviewRegionsAbsent('overview-region-attention')
    })

    it('orders À traiter then config then Structure when attention only', async () => {
      const situation = cockpitSituation()
      vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
        cockpitView({
          naturalProgression: null,
          availableActions: [{ code: 'AddEntry', guaranteed: false }],
          situations: [situation],
          attentionSummary: { count: 1, items: [situation] },
        }),
      )

      renderCockpitPage()

      await screen.findByTestId('overview-region-attention')
      expectOverviewRegionOrder(
        'overview-region-attention',
        'overview-region-config',
        'overview-region-structure',
      )
      expectOverviewRegionsAbsent('overview-region-progression')
    })

    it('keeps InsufficientParticipants on À traiter and minimum on Équipes without tip AddEntry', async () => {
      const situation = cockpitSituation({
        params: { minimumTeams: '2', activeCount: '1' },
      })
      vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
        cockpitView({
          naturalProgression: { code: 'PrepareStage' },
          availableActions: [
            { code: 'AddEntry', guaranteed: false },
            {
              code: 'PrepareStage',
              guaranteed: false,
              stageId,
              params: { stageName: 'Phase 1' },
            },
          ],
          situations: [situation],
          attentionSummary: { count: 1, items: [situation] },
        }),
      )

      renderCockpitPage()

      expect(
        await screen.findByRole('heading', { name: 'À traiter' }),
      ).toBeInTheDocument()
      expect(screen.getByText('Participants insuffisants')).toBeInTheDocument()
      expect(screen.getByText('Minimum requis : 2')).toBeInTheDocument()
      expect(
        screen.getByRole('button', { name: /Préparer la phase/i }),
      ).toBeInTheDocument()
      expect(
        screen.queryByRole('button', { name: /Continuer la préparation/i }),
      ).not.toBeInTheDocument()
      const progression = screen.getByTestId('overview-region-progression')
      expect(
        within(progression).queryByRole('link', { name: /Ajouter une équipe/i }),
      ).not.toBeInTheDocument()
    })
  })

  it('Préparation Équipes — minimum insuffisant : signal requis, pas de badge complet ni max', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          teams: {
            prominence: 'Dominant',
            facts: { activeCount: '1', minimumTeams: '4', maximumTeams: '8' },
          },
        },
        availableActions: [{ code: 'AddEntry', guaranteed: false }],
      }),
    )

    renderCockpitPage()

    expect(await screen.findByRole('heading', { name: 'Équipes' })).toBeInTheDocument()
    expect(screen.getByText('Minimum requis : 4')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Ajouter une équipe/i })).toBeInTheDocument()
    expect(screen.queryByText(/complète/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/Max\./i)).not.toBeInTheDocument()
  })

  it('Préparation Équipes — minimum atteint : count + crests, silence readiness', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue({
      competitionId,
      name: 'Spring Cup',
      status: 'Draft',
      participants: {
        activeCount: 4,
        occupyingCount: 4,
        entries: [
          { entryId: 'e1', displayName: 'A', status: 'Active' },
          { entryId: 'e2', displayName: 'B', status: 'Active' },
          { entryId: 'e3', displayName: 'C', status: 'Active' },
          { entryId: 'e4', displayName: 'D', status: 'Active' },
        ],
      },
      regulation: {
        minimumTeams: 4,
        maximumTeams: 8,
        durationPerPeriod: 45,
        numberOfPeriods: 2,
        winPoints: 3,
        drawPoints: 1,
        lossPoints: 0,
      },
      format: { kind: 'Championship', primaryStageId: stageId },
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
    })
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          teams: {
            prominence: 'Present',
            facts: { activeCount: '4', minimumTeams: '4', maximumTeams: '8' },
          },
        },
        availableActions: [{ code: 'AddEntry', guaranteed: false }],
      }),
    )

    renderCockpitPage()

    expect(await screen.findByRole('heading', { name: 'Équipes' })).toBeInTheDocument()
    expect(screen.getByText('4')).toBeInTheDocument()
    expect(screen.queryByText(/Minimum requis/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/complète/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/Max\./i)).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Ajouter une équipe/i })).toBeInTheDocument()
  })

  it('does not invent actions absent from availableActions', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        operationalFocus: {
          ...cockpitView().operationalFocus,
          stages: [{ stageId, name: 'Phase 1', status: 'Ready' }],
        },
        availableActions: [],
      }),
    )

    renderCockpitPage()

    await screen.findByRole('heading', { name: 'Équipes' })
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Préparer la compétition/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Démarrer la compétition/i }),
    ).not.toBeInTheDocument()
  })

  it('renders PrepareCompetition only when projected by availableActions', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        availableActions: [
          { code: 'PrepareCompetition', guaranteed: false },
        ],
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('button', { name: /Préparer la compétition/i }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Démarrer la compétition/i }),
    ).not.toBeInTheDocument()
  })

  it('Préparation Prochaine action — calme : carte absente (null + pas de lifecycle)', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        naturalProgression: null,
        availableActions: [{ code: 'AddEntry', guaranteed: false }],
      }),
    )

    renderCockpitPage()

    expect(await screen.findByRole('heading', { name: 'Équipes' })).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument()
    expect(screen.queryByText(/Continuer la préparation/i)).not.toBeInTheDocument()
    expect(
      screen.queryByText(/Complétez les équipes/i),
    ).not.toBeInTheDocument()
  })

  it('Préparation Prochaine action — lifecycle seul quand naturalProgression est null', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        naturalProgression: null,
        availableActions: [{ code: 'StartCompetition', guaranteed: false }],
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Démarrer la compétition/i }),
    ).toBeInTheDocument()
  })

  it('Préparation Prochaine action — tip structurante sans empiler PrepareCompetition', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        naturalProgression: { code: 'PrepareStage' },
        availableActions: [
          {
            code: 'PrepareStage',
            guaranteed: false,
            stageId,
            params: { stageName: 'Phase 1' },
          },
          { code: 'PrepareCompetition', guaranteed: false },
        ],
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Préparer la phase/i }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /Préparer la compétition/i }),
    ).not.toBeInTheDocument()
  })

  it('executes PrepareCompetition then StartCompetition via Host and invalidates cockpit', async () => {
    const user = userEvent.setup()
    vi.mocked(prepareCompetition).mockResolvedValue(undefined)
    vi.mocked(startCompetition).mockResolvedValue(undefined)
    vi.mocked(fetchCompetitionCockpit)
      .mockResolvedValueOnce(
        cockpitView({
          status: 'Draft',
          availableActions: [
            { code: 'PrepareCompetition', guaranteed: false },
          ],
        }),
      )
      .mockResolvedValueOnce(
        cockpitView({
          status: 'Ready',
          availableActions: [
            { code: 'StartCompetition', guaranteed: false },
          ],
        }),
      )
      .mockResolvedValueOnce(
        cockpitView({
          status: 'Running',
          cycleReading: { code: 'InProgress' },
          naturalProgression: null,
          availableActions: [],
        }),
      )

    renderCockpitPage()

    await user.click(
      await screen.findByRole('button', { name: /Préparer la compétition/i }),
    )

    await waitFor(() => {
      expect(prepareCompetition).toHaveBeenCalledWith(competitionId)
    })

    await user.click(
      await screen.findByRole('button', { name: /Démarrer la compétition/i }),
    )

    await waitFor(() => {
      expect(startCompetition).toHaveBeenCalledWith(competitionId)
    })

    await waitFor(() => {
      expect(
        screen.queryByRole('button', { name: /Préparer la compétition/i }),
      ).not.toBeInTheDocument()
      expect(
        screen.queryByRole('button', { name: /Démarrer la compétition/i }),
      ).not.toBeInTheDocument()
    })
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument()
  })

  it('composes En cours sport panels from Read recentUnit / nextUnit / standingCompact', async () => {
    const entryA = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
    const entryB = 'ffffffff-ffff-ffff-ffff-ffffffffffff'
    const liveMatchId = '11111111-1111-1111-1111-111111111111'
    const finishedMatchId = '22222222-2222-2222-2222-222222222222'
    const nextMatchId = '33333333-3333-3333-3333-333333333333'

    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          teams: {
            prominence: 'Condensed',
            facts: {
              activeCount: '4',
              minimumTeams: '2',
              maximumTeams: '64',
            },
          },
          structure: {
            prominence: 'Condensed',
            facts: {
              formatKind: 'Championship',
              groupCount: '0',
              roundCount: '0',
              matchdayCount: '34',
              slotCount: '0',
            },
          },
          regulation: {
            ...cockpitView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          matches: {
            prominence: 'Dominant',
            facts: { live: '1', scheduled: '2', finished: '1', total: '4' },
          },
        },
        operationalFocus: {
          ...cockpitView().operationalFocus,
          matchCounts: {
            live: 1,
            scheduled: 2,
            finished: 1,
            postponed: 0,
            cancelled: 0,
            total: 4,
          },
          recentUnit: {
            stageId,
            stageName: 'Phase 1',
            unitKind: 'Matchday',
            unitKey: '5',
            matchdayNumber: 5,
            roundName: null,
            matchCount: 2,
            matches: [
              {
                matchId: liveMatchId,
                stageId,
                status: 'Live',
                scheduledAt: null,
                homeDisplayName: 'Alpha',
                awayDisplayName: 'Bravo',
                score: null,
              },
              {
                matchId: finishedMatchId,
                stageId,
                status: 'Finished',
                scheduledAt: '2026-08-20T15:00:00Z',
                homeDisplayName: 'Charlie',
                awayDisplayName: 'Delta',
                score: { homeGoals: 2, awayGoals: 1 },
              },
            ],
          },
          nextUnit: {
            stageId,
            stageName: 'Phase 1',
            unitKind: 'Matchday',
            unitKey: '6',
            matchdayNumber: 6,
            roundName: null,
            matchCount: 1,
            matches: [
              {
                matchId: nextMatchId,
                stageId,
                status: 'Scheduled',
                scheduledAt: '2026-08-27T18:00:00Z',
                homeDisplayName: 'Echo',
                awayDisplayName: 'Foxtrot',
                score: null,
              },
            ],
          },
          standingCompact: {
            stageId,
            stageName: 'Phase 1',
            tables: [
              {
                scope: 'Overall',
                groupId: null,
                groupName: null,
                rows: [
                  {
                    position: 1,
                    entryId: entryA,
                    displayName: 'Alpha',
                    played: 2,
                    points: 6,
                  },
                  {
                    position: 2,
                    entryId: entryB,
                    displayName: 'Bravo',
                    played: 2,
                    points: 3,
                  },
                ],
              },
            ],
          },
          referenceStageGameRules: referenceStageGameRules(),
        },
        naturalProgression: null,
        availableActions: [],
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Classement' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument()
    expect(screen.getByTestId(`overview-standing-${entryA}`)).toHaveTextContent(
      'Alpha',
    )
    expect(
      screen.getByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument()
    expect(screen.getByText(/Journée 5/)).toBeInTheDocument()
    expect(screen.getByTestId(`overview-match-${liveMatchId}`)).toHaveTextContent(
      'Live',
    )
    expect(screen.getByTestId(`overview-match-${finishedMatchId}`)).toHaveTextContent(
      '2–1',
    )
    expect(
      screen.getByRole('heading', { name: 'Prochaines rencontres' }),
    ).toBeInTheDocument()
    expect(screen.getByText(/Journée 6/)).toBeInTheDocument()
    expect(screen.getByTestId(`overview-match-${nextMatchId}`)).toHaveTextContent(
      'Echo',
    )
    expect(screen.getByTestId('overview-structure-condensed')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Structure' })).toBeInTheDocument()
    expect(screen.getByTestId('overview-structure-format')).toHaveTextContent(
      'Championnat',
    )
    expect(screen.getByText(/34 journées/)).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Équipes' })).toBeInTheDocument()
    expect(screen.getByText('équipes')).toBeInTheDocument()
    expect(screen.queryByText(/complètes/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Max\./)).not.toBeInTheDocument()
    expect(screen.queryByText(/Minimum .* démarrer/)).not.toBeInTheDocument()
    expect(screen.getByTestId('overview-regulation-game')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Règlement' })).toBeInTheDocument()
    expect(screen.getByText('3 pts')).toBeInTheDocument()
    expect(screen.getByText('Victoire')).toBeInTheDocument()
    expect(screen.getByText('1 pts')).toBeInTheDocument()
    expect(screen.getByText('Nul')).toBeInTheDocument()
    expect(screen.getByText('0 pts')).toBeInTheDocument()
    expect(screen.getByText('Défaite')).toBeInTheDocument()
    expect(screen.getByText('2×45 min')).toBeInTheDocument()
    expect(screen.queryByText(/Règlement prêt/)).not.toBeInTheDocument()
    expect(screen.queryByText(/2–64 équipes/)).not.toBeInTheDocument()
    expect(screen.queryByText(/conditions à lever/)).not.toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Voir le classement' }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/classements`)
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument()
    expect(screen.getByTestId('overview-region-sport')).not.toHaveClass(
      'overview__sport--solo',
    )
  })

  it('shows empty states for Dernières and Prochaines when units are null', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        operationalFocus: {
          ...cockpitView().operationalFocus,
          recentUnit: null,
          nextUnit: null,
          standingCompact: null,
        },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Prochaines rencontres' }),
    ).toBeInTheDocument()
    expect(
      screen.getByText('Aucune unité engagée pour le moment'),
    ).toBeInTheDocument()
    expect(
      screen.getByText('Aucune prochaine journée n’est encore générée'),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument()
    expect(screen.getByTestId('overview-region-sport')).toHaveClass(
      'overview__sport--solo',
    )
  })

  it('shows Prochaine action when En cours has a structural tip', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: { code: 'GenerateNextRound' },
        availableActions: [
          {
            code: 'GenerateNextRound',
            guaranteed: false,
            stageId,
            params: { stageName: 'Swiss', roundIndex: '2', plannedRounds: '8' },
          },
        ],
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument()
    expect(
      screen.getByText(/Générez le prochain tour Swiss/),
    ).toBeInTheDocument()
  })

  describe('En cours layout DOM order', () => {
    it('orders Config then Sport when calm', async () => {
      vi.mocked(fetchCompetitionCockpit).mockResolvedValue(inProgressLayoutBase())

      renderCockpitPage()

      await screen.findByTestId('overview-region-config')
      expectOverviewRegionOrder('overview-region-config', 'overview-region-sport')
      expectOverviewRegionsAbsent(
        'overview-region-attention',
        'overview-region-progression',
      )
    })

    it('orders Config then Prochaine action then Sport when tip only', async () => {
      vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
        inProgressLayoutBase({
          naturalProgression: { code: 'GenerateNextRound' },
          availableActions: [
            {
              code: 'GenerateNextRound',
              guaranteed: false,
              stageId,
              params: { stageName: 'Swiss', roundIndex: '2', plannedRounds: '8' },
            },
          ],
        }),
      )

      renderCockpitPage()

      await screen.findByTestId('overview-region-progression')
      expectOverviewRegionOrder(
        'overview-region-config',
        'overview-region-progression',
        'overview-region-sport',
      )
      expectOverviewRegionsAbsent('overview-region-attention')
    })

    it('orders À traiter before Config when attention only', async () => {
      const situation = cockpitSituation()
      vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
        inProgressLayoutBase({
          situations: [situation],
          attentionSummary: { count: 1, items: [situation] },
        }),
      )

      renderCockpitPage()

      await screen.findByTestId('overview-region-attention')
      expectOverviewRegionOrder(
        'overview-region-attention',
        'overview-region-config',
        'overview-region-sport',
      )
      expectOverviewRegionsAbsent('overview-region-progression')
    })

    it('orders À traiter before Config before Prochaine action when both signals exist', async () => {
      const situation = cockpitSituation()
      vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
        inProgressLayoutBase({
          situations: [situation],
          attentionSummary: { count: 1, items: [situation] },
          naturalProgression: { code: 'GenerateNextRound' },
          availableActions: [
            {
              code: 'GenerateNextRound',
              guaranteed: false,
              stageId,
              params: { stageName: 'Swiss', roundIndex: '2', plannedRounds: '8' },
            },
          ],
        }),
      )

      renderCockpitPage()

      await screen.findByTestId('overview-region-attention')
      expectOverviewRegionOrder(
        'overview-region-attention',
        'overview-region-config',
        'overview-region-progression',
        'overview-region-sport',
      )
    })
  })

  it('composes Terminée like En cours without Prochaines when nextUnit is null', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        naturalProgression: null,
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          teams: {
            prominence: 'Condensed',
            facts: { activeCount: '4', minimumTeams: '2', maximumTeams: '64' },
          },
          structure: {
            prominence: 'Condensed',
            facts: {
              formatKind: 'Championship',
              groupCount: '0',
              roundCount: '0',
              matchdayCount: '34',
              slotCount: '0',
            },
          },
          regulation: {
            ...cockpitView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
        },
        operationalFocus: {
          ...cockpitView().operationalFocus,
          recentUnit: {
            stageId,
            stageName: 'Phase 1',
            unitKind: 'Matchday',
            unitKey: '34',
            matchdayNumber: 34,
            roundName: null,
            matchCount: 0,
            matches: [],
          },
          nextUnit: null,
          standingCompact: {
            stageId,
            stageName: 'Phase 1',
            tables: [
              {
                scope: 'Overall',
                groupId: null,
                groupName: null,
                rows: [
                  {
                    position: 1,
                    entryId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
                    displayName: 'Alpha',
                    played: 10,
                    points: 24,
                  },
                ],
              },
            ],
          },
          referenceStageGameRules: referenceStageGameRules(),
        },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Classement' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument()
    expect(screen.getByTestId('overview-structure-condensed')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Équipes' })).toBeInTheDocument()
    expect(screen.getByTestId('overview-regulation-game')).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Prochaines rencontres' }),
    ).not.toBeInTheDocument()
    expectOverviewRegionOrder('overview-region-config', 'overview-region-sport')
  })

  it('shows Prochaines on Terminée only when nextUnit is projected', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        naturalProgression: null,
        operationalFocus: {
          ...cockpitView().operationalFocus,
          recentUnit: null,
          nextUnit: {
            stageId,
            stageName: 'Phase 1',
            unitKind: 'Matchday',
            unitKey: '35',
            matchdayNumber: 35,
            roundName: null,
            matchCount: 0,
            matches: [],
          },
          standingCompact: null,
          referenceStageGameRules: null,
        },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Prochaines rencontres' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument()
  })

  it('uses Host readiness copy without inventing draw chrome on overview', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        operationalFocus: {
          ...cockpitView().operationalFocus,
          draws: [
            {
              stageId,
              drawId: cockpitIds.drawId,
              kind: 'Slot',
              status: 'Published',
              resolutionState: 'Resolved',
              isApplied: true,
            },
          ],
        },
      }),
    )

    renderCockpitPage()

    await screen.findByRole('heading', { name: 'Équipes' })
    expect(screen.queryByText(/Appliqué/)).not.toBeInTheDocument()
    expect(screen.queryByText('Focus opérationnel')).not.toBeInTheDocument()
  })

  it('navigates Fixture targets via Host matchId without client join', async () => {
    const user = userEvent.setup()
    const fixtureId = 'ffffffff-ffff-ffff-ffff-ffffffffffff'
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        situations: [
          cockpitSituation({
            source: 'ProgressionPending',
            nature: 'Blocking',
            targetType: 'Fixture',
            targetId: fixtureId,
            matchId: cockpitIds.matchId,
            actionable: true,
            actionCode: 'ApplyProgression',
            impactCode: 'BlocksProgression',
            params: {},
          }),
        ],
        attentionSummary: {
          count: 1,
          items: [
            cockpitSituation({
              source: 'ProgressionPending',
              nature: 'Blocking',
              targetType: 'Fixture',
              targetId: fixtureId,
              matchId: cockpitIds.matchId,
              actionable: true,
              actionCode: 'ApplyProgression',
              impactCode: 'BlocksProgression',
              params: {},
            }),
          ],
        },
      }),
    )

    renderCockpitPage()

    await user.click(
      await screen.findByRole('link', { name: /Progression en attente/i }),
    )
    expect(screen.getByText('Match route')).toBeInTheDocument()
  })

  it('executes a projected action and invalidates the cockpit query', async () => {
    const user = userEvent.setup()
    vi.mocked(prepareStage).mockResolvedValue(undefined)
    vi.mocked(fetchCompetitionCockpit)
      .mockResolvedValueOnce(
        cockpitView({
          availableActions: [
            {
              code: 'PrepareStage',
              guaranteed: false,
              stageId,
              params: { stageName: 'Phase 1' },
            },
          ],
          naturalProgression: { code: 'PrepareStage' },
        }),
      )
      .mockResolvedValueOnce(
        cockpitView({
          availableActions: [
            {
              code: 'StartStage',
              guaranteed: false,
              stageId,
              params: { stageName: 'Phase 1' },
            },
          ],
          naturalProgression: { code: 'StartStage' },
          operationalFocus: {
            ...cockpitView().operationalFocus,
            stages: [{ stageId, name: 'Phase 1', status: 'Ready' }],
          },
        }),
      )

    renderCockpitPage()

    await user.click(
      await screen.findByRole('button', { name: /Préparer la phase/i }),
    )

    await waitFor(() => {
      expect(prepareStage).toHaveBeenCalledWith(stageId)
    })

    expect(
      await screen.findByRole('button', { name: /Démarrer la phase/i }),
    ).toBeInTheDocument()
  })

  it('Préparation Règlement — faits only (points, durée, action locale)', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          regulation: {
            prominence: 'Present',
            competition: {
              minimumTeams: 2,
              maximumTeams: 64,
              durationPerPeriod: 45,
              numberOfPeriods: 2,
              winPoints: 3,
              drawPoints: 1,
              lossPoints: 0,
            },
            stage: null,
            competitionRegulationMutable: true,
            transitionReadiness: [
              {
                transition: 'MaterializeMatches',
                ready: false,
                blockerCodes: ['InsufficientParticipants'],
              },
            ],
          },
        },
        availableActions: [{ code: 'ReplaceRegulation', guaranteed: false }],
      }),
    )

    renderCockpitPage()

    const regulation = await screen.findByTestId('overview-regulation-construction')
    expect(regulation).toBeInTheDocument()
    expect(within(regulation).getByText('2×45 min')).toBeInTheDocument()
    expect(within(regulation).getByText('Victoire')).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /Modifier le règlement/i }),
    ).toBeInTheDocument()
    expect(within(regulation).queryByText(/Règlement prêt/i)).not.toBeInTheDocument()
    expect(within(regulation).queryByText(/Matérialisation/i)).not.toBeInTheDocument()
    expect(within(regulation).queryByText(/–/)).not.toBeInTheDocument()
  })

  it('Préparation Structure — faits only (format, rows, ConfigureStructure, pas de pilotage phase)', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          structure: {
            prominence: 'Present',
            facts: {
              formatKind: 'Championship',
              groupCount: '0',
              roundCount: '0',
              matchdayCount: '2',
              slotCount: '0',
            },
          },
        },
        operationalFocus: {
          ...cockpitView().operationalFocus,
          stages: [{ stageId, name: 'Phase 1', status: 'Draft' }],
          matchCounts: {
            live: 0,
            scheduled: 0,
            finished: 0,
            postponed: 0,
            cancelled: 0,
            total: 0,
          },
        },
        availableActions: [
          { code: 'ConfigureStructure', guaranteed: false },
          {
            code: 'PrepareStage',
            guaranteed: false,
            stageId,
            params: { stageName: 'Phase 1' },
          },
        ],
        naturalProgression: { code: 'PrepareStage' },
      }),
    )

    renderCockpitPage()

    const structure = await screen.findByTestId('overview-structure-construction')
    expect(within(structure).getByText('Championnat')).toBeInTheDocument()
    expect(within(structure).getByText(/Phase 1/)).toBeInTheDocument()
    expect(within(structure).getByText(/2 journées/)).toBeInTheDocument()
    expect(within(structure).getByText(/Aucun match créé/)).toBeInTheDocument()
    expect(
      within(structure).getByRole('link', { name: /Configurer la structure/i }),
    ).toBeInTheDocument()
    expect(
      within(structure).queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument()
    expect(within(structure).queryByText(/matchs? à créer/i)).not.toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /Préparer la phase/i }),
    ).toBeInTheDocument()
  })

  it('Préparation Structure — format non configuré sans checkmark', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          structure: {
            prominence: 'Present',
            facts: { formatKind: 'None' },
          },
        },
      }),
    )

    renderCockpitPage()

    const structure = await screen.findByTestId('overview-structure-construction')
    expect(within(structure).getByText('Format non configuré')).toBeInTheDocument()
    expect(structure.querySelector('.overview-row__mark')).not.toBeInTheDocument()
  })

  it('renders regulation factual summary without transition readiness UI', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          regulation: {
            prominence: 'Present',
            competition: {
              minimumTeams: 2,
              maximumTeams: 64,
              durationPerPeriod: 45,
              numberOfPeriods: 2,
              winPoints: 3,
              drawPoints: 1,
              lossPoints: 0,
            },
            stage: null,
            competitionRegulationMutable: true,
            transitionReadiness: [
              {
                transition: 'MaterializeMatches',
                ready: true,
                blockerCodes: [],
              },
            ],
          },
        },
      }),
    )

    renderCockpitPage()

    expect(await screen.findByText(/2×45 min/)).toBeInTheDocument()
    expect(screen.queryByText(/Règlement prêt pour la suite/)).not.toBeInTheDocument()
    expect(screen.queryByText('Tirage')).not.toBeInTheDocument()
  })

  it('previews attention with the same item recipe as the drawer', async () => {
    const situation = cockpitSituation()
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        situations: [situation, situation],
        attentionSummary: {
          count: 3,
          items: [situation, situation, situation],
        },
      }),
    )

    renderCockpitPage()

    expect(
      (await screen.findAllByRole('heading', { name: 'À traiter' })).length,
    ).toBe(1)
    expect(screen.getAllByText('Participants insuffisants')).toHaveLength(2)
    expect(screen.getAllByText('Bloquant').length).toBeGreaterThan(0)
    expect(
      screen.getByText(/3 situation\(s\) — détail dans le panneau À traiter/),
    ).toBeInTheDocument()
  })

  it('hides À traiter when attention count is 0', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        situations: [],
        attentionSummary: { count: 0, items: [] },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'À traiter' }),
    ).not.toBeInTheDocument()
    expect(screen.queryByText(/Rien à traiter/)).not.toBeInTheDocument()
  })

  it('does not show quantity hint when attention count is 1', async () => {
    const situation = cockpitSituation()
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        situations: [situation],
        attentionSummary: { count: 1, items: [situation] },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByRole('heading', { name: 'À traiter' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Participants insuffisants')).toBeInTheDocument()
    expect(
      screen.queryByText(/situation\(s\) — détail dans le panneau/),
    ).not.toBeInTheDocument()
  })

  it('hides Absent match dimension and console operational chrome', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          matches: { prominence: 'Absent', facts: { total: '0' } },
        },
        operationalFocus: {
          stages: [],
          draws: [],
          matchCounts: {
            live: 0,
            scheduled: 0,
            finished: 0,
            postponed: 0,
            cancelled: 0,
            total: 0,
          },
          swissByes: [],
          recentUnit: null,
          nextUnit: null,
          standingCompact: null,
          referenceStageGameRules: null,
        },
      }),
    )

    renderCockpitPage()

    await screen.findByRole('heading', { name: 'Équipes' })
    expect(
      screen.queryByRole('heading', { name: 'Matchs', level: 3 }),
    ).not.toBeInTheDocument()
    expect(screen.queryByText('Focus opérationnel')).not.toBeInTheDocument()
  })

  it('hides En cours Règlement when referenceStageGameRules is null', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          regulation: {
            ...cockpitView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          structure: {
            prominence: 'Condensed',
            facts: { formatKind: 'Championship', matchdayCount: '10' },
          },
          teams: {
            prominence: 'Condensed',
            facts: { activeCount: '4', minimumTeams: '2' },
          },
        },
        operationalFocus: {
          ...cockpitView().operationalFocus,
          referenceStageGameRules: null,
        },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByTestId('overview-structure-condensed'),
    ).toBeInTheDocument()
    expect(screen.queryByTestId('overview-regulation-game')).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Règlement' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument()
  })

  it('shows Cup game-rule facts without standing points', async () => {
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        constructionDimensions: {
          ...cockpitView().constructionDimensions,
          regulation: {
            ...cockpitView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          structure: {
            prominence: 'Condensed',
            facts: { formatKind: 'Cup', roundCount: '7' },
          },
          teams: { prominence: 'Absent', facts: {} },
        },
        operationalFocus: {
          ...cockpitView().operationalFocus,
          referenceStageGameRules: referenceStageGameRules({
            formatKind: 'Cup',
            numberOfLegs: 2,
            aggregateScoring: true,
            hasExtraTime: true,
            hasPenaltyShootout: true,
          }),
        },
      }),
    )

    renderCockpitPage()

    expect(
      await screen.findByTestId('overview-regulation-game'),
    ).toBeInTheDocument()
    expect(screen.getByText('2 manches · cumul des scores')).toBeInTheDocument()
    expect(screen.getByText('2×45 min')).toBeInTheDocument()
    expect(screen.getByText('Prolongation · Tirs au but')).toBeInTheDocument()
    expect(
      screen.queryByText(/pts victoire/),
    ).not.toBeInTheDocument()
  })

  it('navigates to organisation from a dimension panel', async () => {
    const user = userEvent.setup()
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(cockpitView())

    renderCockpitPage()

    await user.click(
      await screen.findByRole('link', { name: /Voir les équipes/i }),
    )

    expect(screen.getByText('Organisation route')).toBeInTheDocument()
  })

  it('shows an error when the cockpit read fails', async () => {
    vi.mocked(fetchCompetitionCockpit).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    )

    renderCockpitPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    )
  })

  it('materializes matches then offers Voir les matchs and invalidates match lists', async () => {
    const user = userEvent.setup()
    const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
    vi.mocked(fetchCompetitionCockpit).mockResolvedValue(
      cockpitView({
        availableActions: [
          {
            code: 'MaterializeMatches',
            guaranteed: false,
            stageId,
            drawId: null,
            matchId: null,
            fixtureId: null,
          },
        ],
        naturalProgression: { code: 'MaterializeMatches' },
      }),
    )
    vi.mocked(materializeMatches).mockResolvedValue({
      createdCount: 1,
      attachedMatchIds: [matchId],
      alreadyComplete: false,
    })

    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    })
    const spy = vi.spyOn(queryClient, 'invalidateQueries')

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[`/competitions/${competitionId}`]}>
          <Routes>
            <Route
              path="/competitions/:competitionId"
              element={<CompetitionCockpitPage />}
            />
            <Route
              path="/competitions/:competitionId/matches"
              element={<p>Match hub route</p>}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    )

    const materializeButtons = await screen.findAllByRole('button', {
      name: /Créer les matchs/i,
    })
    await user.click(materializeButtons[0])

    await waitFor(() => {
      expect(materializeMatches).toHaveBeenCalledWith(stageId)
    })

    expect(
      await screen.findByRole('heading', { name: /Matchs créés/i }),
    ).toBeInTheDocument()
    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: ['matches', 'by-stage', stageId],
      }),
    )

    await user.click(screen.getByRole('link', { name: /Voir les matchs/i }))
    expect(screen.getByText('Match hub route')).toBeInTheDocument()
  })
})
