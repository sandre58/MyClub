import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fetchCompetitionCockpit } from '../api'
import {
  cockpitSituation,
  cockpitView,
  competitionId,
  expectOverviewRegionOrder,
  expectOverviewRegionsAbsent,
  inProgressLayoutBase,
  referenceStageGameRules,
  renderCockpitPage,
  setupDefaultOrganisationMock,
  stageId,
} from './competitionCockpitPageTestHelpers'

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

describe('CompetitionCockpitPage — En cours / Terminée', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setupDefaultOrganisationMock()
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
})
