import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchCompetitionOverview } from '../api';
import {
  overviewSituation,
  overviewView,
  competitionId,
  expectOverviewRegionOrder,
  expectOverviewRegionsAbsent,
  inProgressLayoutBase,
  referenceStageGameRules,
  renderOverviewPage,
  setupDefaultStructureMock,
  stageId,
} from './competitionOverviewPageTestHelpers';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchCompetitionOverview: vi.fn(),
    fetchStructureView: vi.fn(),
    prepareStage: vi.fn(),
    prepareCompetition: vi.fn(),
    startCompetition: vi.fn(),
    materializeMatches: vi.fn(),
  };
});

describe('CompetitionOverviewPage — En cours / Terminée', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupDefaultStructureMock();
  });

  it('composes En cours sport panels from Read recentUnit / nextUnit / standingCompact', async () => {
    const entryA = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    const entryB = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
    const liveMatchId = '11111111-1111-1111-1111-111111111111';
    const finishedMatchId = '22222222-2222-2222-2222-222222222222';
    const nextMatchId = '33333333-3333-3333-3333-333333333333';

    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        constructionDimensions: {
          ...overviewView().constructionDimensions,
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
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          matches: {
            prominence: 'Dominant',
            facts: { live: '1', scheduled: '2', finished: '1', total: '4' },
          },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
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
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Classement' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId(`overview-standing-${entryA}`)).toHaveTextContent(
      'Alpha',
    );
    expect(
      screen.getByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument();
    expect(screen.getByText(/Journée 5/)).toBeInTheDocument();
    expect(
      screen.getByTestId(`overview-match-${liveMatchId}`),
    ).toHaveTextContent('Live');
    expect(
      screen.getByTestId(`overview-match-${finishedMatchId}`),
    ).toHaveTextContent('2–1');
    expect(
      screen.getByRole('heading', { name: 'Prochaines rencontres' }),
    ).toBeInTheDocument();
    expect(screen.getByText(/Journée 6/)).toBeInTheDocument();
    expect(
      screen.getByTestId(`overview-match-${nextMatchId}`),
    ).toHaveTextContent('Echo');
    expect(
      screen.getByTestId('overview-structure-condensed'),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Structure' }),
    ).toBeInTheDocument();
    expect(screen.getByTestId('overview-structure-format')).toHaveTextContent(
      'Championnat',
    );
    expect(screen.getByText(/34 journées/)).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(screen.getByText('équipes')).toBeInTheDocument();
    expect(screen.queryByText(/complètes/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Max\./)).not.toBeInTheDocument();
    expect(screen.queryByText(/Minimum .* démarrer/)).not.toBeInTheDocument();
    expect(screen.getByTestId('overview-regulation-game')).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Règlement' }),
    ).toBeInTheDocument();
    expect(screen.getByText('3 pts')).toBeInTheDocument();
    expect(screen.getByText('Victoire')).toBeInTheDocument();
    expect(screen.getByText('1 pts')).toBeInTheDocument();
    expect(screen.getByText('Nul')).toBeInTheDocument();
    expect(screen.getByText('0 pts')).toBeInTheDocument();
    expect(screen.getByText('Défaite')).toBeInTheDocument();
    expect(screen.getByText('2×45 min')).toBeInTheDocument();
    expect(screen.queryByText(/Règlement prêt/)).not.toBeInTheDocument();
    expect(screen.queryByText(/2–64 équipes/)).not.toBeInTheDocument();
    expect(screen.queryByText(/conditions à lever/)).not.toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Voir le classement' }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/classements`);
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId('overview-region-sport')).not.toHaveClass(
      'overview__sport--solo',
    );
  });

  it('shows empty states for Dernières and Prochaines when units are null', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        operationalFocus: {
          ...overviewView().operationalFocus,
          recentUnit: null,
          nextUnit: null,
          standingCompact: null,
        },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Prochaines rencontres' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Aucune unité engagée pour le moment'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Aucune prochaine journée n’est encore générée'),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId('overview-region-sport')).toHaveClass(
      'overview__sport--solo',
    );
  });

  it('shows Prochaine action when En cours has a structural tip', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
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
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Générez le prochain tour Swiss/),
    ).toBeInTheDocument();
  });

  describe('En cours layout DOM order', () => {
    it('orders Config then Sport when calm', async () => {
      vi.mocked(fetchCompetitionOverview).mockResolvedValue(
        inProgressLayoutBase(),
      );

      renderOverviewPage();

      await screen.findByTestId('overview-region-config');
      expectOverviewRegionOrder(
        'overview-region-config',
        'overview-region-sport',
      );
      expectOverviewRegionsAbsent(
        'overview-region-attention',
        'overview-region-progression',
      );
    });

    it('orders Config then Prochaine action then Sport when tip only', async () => {
      vi.mocked(fetchCompetitionOverview).mockResolvedValue(
        inProgressLayoutBase({
          naturalProgression: { code: 'GenerateNextRound' },
          availableActions: [
            {
              code: 'GenerateNextRound',
              guaranteed: false,
              stageId,
              params: {
                stageName: 'Swiss',
                roundIndex: '2',
                plannedRounds: '8',
              },
            },
          ],
        }),
      );

      renderOverviewPage();

      await screen.findByTestId('overview-region-progression');
      expectOverviewRegionOrder(
        'overview-region-config',
        'overview-region-progression',
        'overview-region-sport',
      );
      expectOverviewRegionsAbsent('overview-region-attention');
    });

    it('orders À traiter before Config when attention only', async () => {
      const situation = overviewSituation();
      vi.mocked(fetchCompetitionOverview).mockResolvedValue(
        inProgressLayoutBase({
          situations: [situation],
          attentionSummary: { count: 1, items: [situation] },
        }),
      );

      renderOverviewPage();

      await screen.findByTestId('overview-region-attention');
      expectOverviewRegionOrder(
        'overview-region-attention',
        'overview-region-config',
        'overview-region-sport',
      );
      expectOverviewRegionsAbsent('overview-region-progression');
    });

    it('orders À traiter before Config before Prochaine action when both signals exist', async () => {
      const situation = overviewSituation();
      vi.mocked(fetchCompetitionOverview).mockResolvedValue(
        inProgressLayoutBase({
          situations: [situation],
          attentionSummary: { count: 1, items: [situation] },
          naturalProgression: { code: 'GenerateNextRound' },
          availableActions: [
            {
              code: 'GenerateNextRound',
              guaranteed: false,
              stageId,
              params: {
                stageName: 'Swiss',
                roundIndex: '2',
                plannedRounds: '8',
              },
            },
          ],
        }),
      );

      renderOverviewPage();

      await screen.findByTestId('overview-region-attention');
      expectOverviewRegionOrder(
        'overview-region-attention',
        'overview-region-config',
        'overview-region-progression',
        'overview-region-sport',
      );
    });
  });

  it('composes Terminée with Podium Résultat and standing compact Classement', async () => {
    const entryA = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    const entryB = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
    const entryC = '11111111-1111-1111-1111-111111111111';
    const entryD = '22222222-2222-2222-2222-222222222222';
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        completionMode: 'Normal',
        naturalProgression: null,
        competitionOutcome: {
          presentation: 'Podium',
          places: [
            { rank: 1, entryId: entryA, displayName: 'Alpha' },
            { rank: 2, entryId: entryB, displayName: 'Bravo' },
            { rank: 3, entryId: entryC, displayName: 'Charlie' },
            { rank: 4, entryId: entryD, displayName: 'Delta' },
          ],
        },
        constructionDimensions: {
          ...overviewView().constructionDimensions,
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
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
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
                    entryId: entryA,
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
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Résultat' }),
    ).toBeInTheDocument();
    const result = screen.getByTestId('overview-outcome-podium');
    expect(result).toHaveAttribute('data-presentation', 'Podium');
    expect(screen.getByTestId(`overview-outcome-${entryA}`)).toHaveTextContent(
      /Alpha/,
    );
    expect(screen.getByTestId(`overview-outcome-${entryA}`)).toHaveTextContent(
      /Champion/,
    );
    expect(screen.getByTestId(`overview-outcome-${entryB}`)).toHaveTextContent(
      /Bravo/,
    );
    expect(screen.getByTestId(`overview-outcome-${entryC}`)).toHaveTextContent(
      /Charlie/,
    );
    expect(
      screen.queryByTestId(`overview-outcome-${entryD}`),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /Voir le classement complet/i }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/classements`);
    expect(
      screen.getByRole('heading', { name: 'Classement' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByTestId('overview-structure-condensed'),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(screen.getByTestId('overview-regulation-game')).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Prochaines rencontres' }),
    ).not.toBeInTheDocument();
    expectOverviewRegionOrder(
      'overview-region-config',
      'overview-region-sport',
    );
    expectOverviewRegionOrder(
      'overview-sport-result-column',
      'overview-sport-temporal-column',
    );
    const resultHeading = screen.getByRole('heading', { name: 'Résultat' });
    const standingHeading = screen.getByRole('heading', { name: 'Classement' });
    expect(
      resultHeading.compareDocumentPosition(standingHeading) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it('shows Cup Winner hero — finalist not staged as podium', async () => {
    const winner = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1';
    const runnerUp = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2';
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        completionMode: 'Normal',
        naturalProgression: null,
        competitionOutcome: {
          presentation: 'Winner',
          places: [
            { rank: 1, entryId: winner, displayName: 'Finaliste A' },
            { rank: 2, entryId: runnerUp, displayName: 'Finaliste B' },
          ],
        },
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          structure: {
            prominence: 'Condensed',
            facts: {
              formatKind: 'Cup',
              groupCount: '0',
              roundCount: '1',
              matchdayCount: '0',
              slotCount: '2',
            },
          },
          regulation: {
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          teams: {
            prominence: 'Condensed',
            facts: { activeCount: '2', minimumTeams: '2', maximumTeams: '64' },
          },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
          standingCompact: null,
          recentUnit: null,
          nextUnit: null,
          referenceStageGameRules: referenceStageGameRules({
            formatKind: 'Cup',
          }),
        },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Résultat' }),
    ).toBeInTheDocument();
    const result = screen.getByTestId('overview-outcome-podium');
    expect(result).toHaveAttribute('data-presentation', 'Winner');
    expect(screen.getByTestId(`overview-outcome-${winner}`)).toHaveTextContent(
      /Finaliste A/,
    );
    expect(screen.getByTestId(`overview-outcome-${winner}`)).toHaveTextContent(
      /Vainqueur/,
    );
    expect(
      screen.queryByTestId(`overview-outcome-${runnerUp}`),
    ).not.toBeInTheDocument();
    expect(result.querySelectorAll('li')).toHaveLength(0);
    expect(
      screen.queryByRole('heading', { name: 'Classement' }),
    ).not.toBeInTheDocument();
  });

  it('shows Cup + bronze as Host Podium Top-3', async () => {
    const a = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1';
    const b = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2';
    const c = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3';
    const d = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4';
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        completionMode: 'Normal',
        competitionOutcome: {
          presentation: 'Podium',
          places: [
            { rank: 1, entryId: a, displayName: 'Champ' },
            { rank: 2, entryId: b, displayName: 'Runner' },
            { rank: 3, entryId: c, displayName: 'Bronze' },
            { rank: 4, entryId: d, displayName: 'Fourth' },
          ],
        },
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          structure: {
            prominence: 'Condensed',
            facts: { formatKind: 'Cup', roundCount: '2' },
          },
          regulation: {
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          teams: {
            prominence: 'Condensed',
            facts: { activeCount: '4', minimumTeams: '2' },
          },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
          standingCompact: null,
          referenceStageGameRules: referenceStageGameRules({
            formatKind: 'Cup',
          }),
        },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByTestId('overview-outcome-podium'),
    ).toHaveAttribute('data-presentation', 'Podium');
    expect(screen.getByTestId(`overview-outcome-${c}`)).toHaveTextContent(
      /Bronze/,
    );
    expect(
      screen.queryByTestId(`overview-outcome-${d}`),
    ).not.toBeInTheDocument();
  });

  it('shows Groups-only Terminée Classement without Résultat', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        completionMode: 'Normal',
        competitionOutcome: null,
        naturalProgression: null,
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          structure: {
            prominence: 'Condensed',
            facts: { formatKind: 'Groups', groupCount: '2' },
          },
          regulation: {
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          teams: {
            prominence: 'Condensed',
            facts: { activeCount: '8', minimumTeams: '2' },
          },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
          recentUnit: null,
          nextUnit: null,
          standingCompact: {
            stageId,
            stageName: 'Phase de groupes',
            tables: [
              {
                scope: 'Group',
                groupId: 'g1',
                groupName: 'A',
                rows: [
                  {
                    position: 1,
                    entryId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',
                    displayName: 'Team A1',
                    played: 3,
                    points: 9,
                  },
                ],
              },
              {
                scope: 'Group',
                groupId: 'g2',
                groupName: 'B',
                rows: [
                  {
                    position: 1,
                    entryId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2',
                    displayName: 'Team B1',
                    played: 3,
                    points: 7,
                  },
                ],
              },
            ],
          },
          referenceStageGameRules: referenceStageGameRules({
            formatKind: 'Groups',
          }),
        },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Groupe A' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Résultat' }),
    ).not.toBeInTheDocument();
  });

  it('hides Résultat when Terminée has no CompetitionOutcome (Abandoned / Groups-only)', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        completionMode: 'Abandoned',
        competitionOutcome: null,
        naturalProgression: null,
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          structure: {
            prominence: 'Condensed',
            facts: { formatKind: 'Championship' },
          },
          regulation: {
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          teams: {
            prominence: 'Condensed',
            facts: { activeCount: '4', minimumTeams: '2' },
          },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
          standingCompact: null,
          recentUnit: null,
          nextUnit: null,
          referenceStageGameRules: referenceStageGameRules(),
        },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByTestId('overview-region-config'),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Résultat' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByTestId('overview-outcome-podium'),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Classement' }),
    ).not.toBeInTheDocument();
  });

  it('shows Prochaines on Terminée only when nextUnit is projected', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Completed',
        cycleReading: { code: 'Completed' },
        naturalProgression: null,
        operationalFocus: {
          ...overviewView().operationalFocus,
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
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Prochaines rencontres' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument();
  });

  it('hides À traiter when attention count is 0', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        situations: [],
        attentionSummary: { count: 0, items: [] },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Dernières rencontres' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'À traiter' }),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/Rien à traiter/)).not.toBeInTheDocument();
  });

  it('hides En cours Règlement when referenceStageGameRules is null', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          regulation: {
            ...overviewView().constructionDimensions.regulation,
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
          ...overviewView().operationalFocus,
          referenceStageGameRules: null,
        },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByTestId('overview-structure-condensed'),
    ).toBeInTheDocument();
    expect(
      screen.queryByTestId('overview-regulation-game'),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Règlement' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument();
  });

  it('shows Cup game-rule facts without standing points', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Running',
        cycleReading: { code: 'InProgress' },
        naturalProgression: null,
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          regulation: {
            ...overviewView().constructionDimensions.regulation,
            prominence: 'Condensed',
          },
          structure: {
            prominence: 'Condensed',
            facts: { formatKind: 'Cup', roundCount: '7' },
          },
          teams: { prominence: 'Absent', facts: {} },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
          referenceStageGameRules: referenceStageGameRules({
            formatKind: 'Cup',
            numberOfLegs: 2,
            aggregateScoring: true,
            hasExtraTime: true,
            hasPenaltyShootout: true,
          }),
        },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByTestId('overview-regulation-game'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('2 manches · cumul des scores'),
    ).toBeInTheDocument();
    expect(screen.getByText('2×45 min')).toBeInTheDocument();
    expect(screen.getByText('Prolongation · Tirs au but')).toBeInTheDocument();
    expect(screen.queryByText(/pts victoire/)).not.toBeInTheDocument();
  });
});
