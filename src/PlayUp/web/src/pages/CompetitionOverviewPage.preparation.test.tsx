import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  fetchCompetitionOverview,
  fetchOrganisationView,
  materializeMatches,
  prepareCompetition,
  prepareStage,
  startCompetition,
} from '../api';
import { CompetitionOverviewPage } from './CompetitionOverviewPage';
import {
  overviewIds,
  overviewSituation,
  overviewView,
  competitionId,
  expectOverviewRegionOrder,
  expectOverviewRegionsAbsent,
  renderOverviewPage,
  setupDefaultOrganisationMock,
  stageId,
} from './competitionOverviewPageTestHelpers';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchCompetitionOverview: vi.fn(),
    fetchOrganisationView: vi.fn(),
    prepareStage: vi.fn(),
    prepareCompetition: vi.fn(),
    startCompetition: vi.fn(),
    materializeMatches: vi.fn(),
  };
});

describe('CompetitionOverviewPage — Construction / Préparation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupDefaultOrganisationMock();
  });

  it('shows loading while the overview is pending', () => {
    vi.mocked(fetchCompetitionOverview).mockReturnValue(new Promise(() => {}));

    renderOverviewPage();

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…');
  });

  it('renders Préparation overview without cycle panel or console blocks', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        situations: [
          overviewSituation({
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
            overviewSituation({
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
    );

    renderOverviewPage();

    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument();
    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Structure' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Règlement' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'À traiter' }),
    ).toBeInTheDocument();
    expect(screen.getByText(/2×45 min/)).toBeInTheDocument();
    expect(
      screen.queryByText(/situation\(s\) à traiter/i),
    ).not.toBeInTheDocument();
    expect(screen.getAllByText('Participants insuffisants')).toHaveLength(1);
    expect(screen.getByText('Minimum requis : 2')).toBeInTheDocument();
    expect(screen.queryByText(/Bloque la préparation/)).not.toBeInTheDocument();
    expect(
      screen.getByText(/Finalisez la configuration de la phase/),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Clôture' }),
    ).not.toBeInTheDocument();
    expect(screen.queryByText('Focus opérationnel')).not.toBeInTheDocument();
    expect(screen.queryByText('Espaces métier')).not.toBeInTheDocument();
    expect(screen.queryByText('Socle de construction')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Actions disponibles' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Préparer la phase/i }),
    ).toBeInTheDocument();
    const structure = screen.getByTestId('overview-structure-construction');
    expect(
      within(structure).queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /Ajouter une équipe/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Renommer une équipe/i }),
    ).not.toBeInTheDocument();
    expectOverviewRegionOrder(
      'overview-region-progression',
      'overview-region-attention',
      'overview-region-config',
      'overview-region-structure',
    );
  });

  describe('Préparation layout DOM order', () => {
    it('orders Prochaine action then config then Structure when tip only', async () => {
      vi.mocked(fetchCompetitionOverview).mockResolvedValue(
        overviewView({
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
      );

      renderOverviewPage();

      await screen.findByTestId('overview-region-progression');
      expectOverviewRegionOrder(
        'overview-region-progression',
        'overview-region-config',
        'overview-region-structure',
      );
      expectOverviewRegionsAbsent('overview-region-attention');
    });

    it('orders À traiter then config then Structure when attention only', async () => {
      const situation = overviewSituation();
      vi.mocked(fetchCompetitionOverview).mockResolvedValue(
        overviewView({
          naturalProgression: null,
          availableActions: [{ code: 'AddEntry', guaranteed: false }],
          situations: [situation],
          attentionSummary: { count: 1, items: [situation] },
        }),
      );

      renderOverviewPage();

      await screen.findByTestId('overview-region-attention');
      expectOverviewRegionOrder(
        'overview-region-attention',
        'overview-region-config',
        'overview-region-structure',
      );
      expectOverviewRegionsAbsent('overview-region-progression');
    });

    it('keeps InsufficientParticipants on À traiter and minimum on Équipes without tip AddEntry', async () => {
      const situation = overviewSituation({
        params: { minimumTeams: '2', activeCount: '1' },
      });
      vi.mocked(fetchCompetitionOverview).mockResolvedValue(
        overviewView({
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
      );

      renderOverviewPage();

      expect(
        await screen.findByRole('heading', { name: 'À traiter' }),
      ).toBeInTheDocument();
      expect(screen.getByText('Participants insuffisants')).toBeInTheDocument();
      expect(screen.getByText('Minimum requis : 2')).toBeInTheDocument();
      expect(
        screen.getByRole('button', { name: /Préparer la phase/i }),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Continuer la préparation/i }),
      ).not.toBeInTheDocument();
      const progression = screen.getByTestId('overview-region-progression');
      expect(
        within(progression).queryByRole('link', {
          name: /Ajouter une équipe/i,
        }),
      ).not.toBeInTheDocument();
    });
  });

  it('Préparation Équipes — minimum insuffisant : signal requis, pas de badge complet ni max', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          teams: {
            prominence: 'Dominant',
            facts: { activeCount: '1', minimumTeams: '4', maximumTeams: '8' },
          },
        },
        availableActions: [{ code: 'AddEntry', guaranteed: false }],
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Minimum requis : 4')).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /Ajouter une équipe/i }),
    ).toBeInTheDocument();
    expect(screen.queryByText(/complète/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Max\./i)).not.toBeInTheDocument();
  });

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
    });
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          teams: {
            prominence: 'Present',
            facts: { activeCount: '4', minimumTeams: '4', maximumTeams: '8' },
          },
        },
        availableActions: [{ code: 'AddEntry', guaranteed: false }],
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(screen.getByText('4')).toBeInTheDocument();
    expect(screen.queryByText(/Minimum requis/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/complète/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Max\./i)).not.toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /Ajouter une équipe/i }),
    ).toBeInTheDocument();
  });

  it('does not invent actions absent from availableActions', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        operationalFocus: {
          ...overviewView().operationalFocus,
          stages: [{ stageId, name: 'Phase 1', status: 'Ready' }],
        },
        availableActions: [],
      }),
    );

    renderOverviewPage();

    await screen.findByRole('heading', { name: 'Équipes' });
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Préparer la compétition/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Démarrer la compétition/i }),
    ).not.toBeInTheDocument();
  });

  it('renders PrepareCompetition only when projected by availableActions', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        availableActions: [{ code: 'PrepareCompetition', guaranteed: false }],
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('button', { name: /Préparer la compétition/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Démarrer la compétition/i }),
    ).not.toBeInTheDocument();
  });

  it('Préparation Prochaine action — calme : carte absente (null + pas de lifecycle)', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        naturalProgression: null,
        availableActions: [{ code: 'AddEntry', guaranteed: false }],
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Prochaine action' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText(/Continuer la préparation/i),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText(/Complétez les équipes/i),
    ).not.toBeInTheDocument();
  });

  it('Préparation Prochaine action — lifecycle seul quand naturalProgression est null', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        naturalProgression: null,
        availableActions: [{ code: 'StartCompetition', guaranteed: false }],
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Démarrer la compétition/i }),
    ).toBeInTheDocument();
  });

  it('Préparation Prochaine action — tip structurante sans empiler PrepareCompetition', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
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
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'Prochaine action' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Préparer la phase/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Préparer la compétition/i }),
    ).not.toBeInTheDocument();
  });

  it('executes PrepareCompetition then StartCompetition via Host and invalidates overview', async () => {
    const user = userEvent.setup();
    vi.mocked(prepareCompetition).mockResolvedValue(undefined);
    vi.mocked(startCompetition).mockResolvedValue(undefined);
    vi.mocked(fetchCompetitionOverview)
      .mockResolvedValueOnce(
        overviewView({
          status: 'Draft',
          availableActions: [{ code: 'PrepareCompetition', guaranteed: false }],
        }),
      )
      .mockResolvedValueOnce(
        overviewView({
          status: 'Ready',
          availableActions: [{ code: 'StartCompetition', guaranteed: false }],
        }),
      )
      .mockResolvedValueOnce(
        overviewView({
          status: 'Running',
          cycleReading: { code: 'InProgress' },
          naturalProgression: null,
          availableActions: [],
        }),
      );

    renderOverviewPage();

    await user.click(
      await screen.findByRole('button', { name: /Préparer la compétition/i }),
    );

    await waitFor(() => {
      expect(prepareCompetition).toHaveBeenCalledWith(competitionId);
    });

    await user.click(
      await screen.findByRole('button', { name: /Démarrer la compétition/i }),
    );

    await waitFor(() => {
      expect(startCompetition).toHaveBeenCalledWith(competitionId);
    });

    await waitFor(() => {
      expect(
        screen.queryByRole('button', { name: /Préparer la compétition/i }),
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Démarrer la compétition/i }),
      ).not.toBeInTheDocument();
    });
    expect(
      screen.queryByRole('heading', { name: 'Où en est-on ?' }),
    ).not.toBeInTheDocument();
  });

  it('uses Host readiness copy without inventing draw chrome on overview', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        operationalFocus: {
          ...overviewView().operationalFocus,
          draws: [
            {
              stageId,
              drawId: overviewIds.drawId,
              kind: 'Slot',
              status: 'Published',
              resolutionState: 'Resolved',
              isApplied: true,
            },
          ],
        },
      }),
    );

    renderOverviewPage();

    await screen.findByRole('heading', { name: 'Équipes' });
    expect(screen.queryByText(/Appliqué/)).not.toBeInTheDocument();
    expect(screen.queryByText('Focus opérationnel')).not.toBeInTheDocument();
  });

  it('navigates Fixture targets via Host matchId without client join', async () => {
    const user = userEvent.setup();
    const fixtureId = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        situations: [
          overviewSituation({
            source: 'ProgressionPending',
            nature: 'Blocking',
            targetType: 'Fixture',
            targetId: fixtureId,
            matchId: overviewIds.matchId,
            actionable: true,
            actionCode: 'ApplyProgression',
            impactCode: 'BlocksProgression',
            params: {},
          }),
        ],
        attentionSummary: {
          count: 1,
          items: [
            overviewSituation({
              source: 'ProgressionPending',
              nature: 'Blocking',
              targetType: 'Fixture',
              targetId: fixtureId,
              matchId: overviewIds.matchId,
              actionable: true,
              actionCode: 'ApplyProgression',
              impactCode: 'BlocksProgression',
              params: {},
            }),
          ],
        },
      }),
    );

    renderOverviewPage();

    await user.click(
      await screen.findByRole('link', { name: /Progression en attente/i }),
    );
    expect(screen.getByText('Match route')).toBeInTheDocument();
  });

  it('executes a projected action and invalidates the overview query', async () => {
    const user = userEvent.setup();
    vi.mocked(prepareStage).mockResolvedValue(undefined);
    vi.mocked(fetchCompetitionOverview)
      .mockResolvedValueOnce(
        overviewView({
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
        overviewView({
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
            ...overviewView().operationalFocus,
            stages: [{ stageId, name: 'Phase 1', status: 'Ready' }],
          },
        }),
      );

    renderOverviewPage();

    await user.click(
      await screen.findByRole('button', { name: /Préparer la phase/i }),
    );

    await waitFor(() => {
      expect(prepareStage).toHaveBeenCalledWith(stageId);
    });

    expect(
      await screen.findByRole('button', { name: /Démarrer la phase/i }),
    ).toBeInTheDocument();
  });

  it('Préparation Règlement — faits only (points, durée, action locale)', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
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
    );

    renderOverviewPage();

    const regulation = await screen.findByTestId(
      'overview-regulation-construction',
    );
    expect(regulation).toBeInTheDocument();
    expect(within(regulation).getByText('2×45 min')).toBeInTheDocument();
    expect(within(regulation).getByText('Victoire')).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /Modifier le règlement/i }),
    ).toBeInTheDocument();
    expect(
      within(regulation).queryByText(/Règlement prêt/i),
    ).not.toBeInTheDocument();
    expect(
      within(regulation).queryByText(/Matérialisation/i),
    ).not.toBeInTheDocument();
    expect(within(regulation).queryByText(/–/)).not.toBeInTheDocument();
  });

  it('Préparation Structure — faits only (format, rows, ConfigureStructure, pas de pilotage phase)', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
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
          ...overviewView().operationalFocus,
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
    );

    renderOverviewPage();

    const structure = await screen.findByTestId(
      'overview-structure-construction',
    );
    expect(within(structure).getByText('Championnat')).toBeInTheDocument();
    expect(within(structure).getByText(/Phase 1/)).toBeInTheDocument();
    expect(within(structure).getByText(/2 journées/)).toBeInTheDocument();
    expect(within(structure).getByText(/Aucun match créé/)).toBeInTheDocument();
    expect(
      within(structure).getByRole('link', { name: /Configurer la structure/i }),
    ).toBeInTheDocument();
    expect(
      within(structure).queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument();
    expect(
      within(structure).queryByText(/matchs? à créer/i),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Préparer la phase/i }),
    ).toBeInTheDocument();
  });

  it('Préparation Structure — format non configuré sans checkmark', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          structure: {
            prominence: 'Present',
            facts: { formatKind: 'None' },
          },
        },
      }),
    );

    renderOverviewPage();

    const structure = await screen.findByTestId(
      'overview-structure-construction',
    );
    expect(
      within(structure).getByText('Format non configuré'),
    ).toBeInTheDocument();
    expect(
      structure.querySelector('.overview-row__mark'),
    ).not.toBeInTheDocument();
  });

  it('renders regulation factual summary without transition readiness UI', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
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
    );

    renderOverviewPage();

    expect(await screen.findByText(/2×45 min/)).toBeInTheDocument();
    expect(
      screen.queryByText(/Règlement prêt pour la suite/),
    ).not.toBeInTheDocument();
    expect(screen.queryByText('Tirage')).not.toBeInTheDocument();
  });

  it('previews attention with the same item recipe as the drawer', async () => {
    const situation = overviewSituation();
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        situations: [situation, situation],
        attentionSummary: {
          count: 3,
          items: [situation, situation, situation],
        },
      }),
    );

    renderOverviewPage();

    expect(
      (await screen.findAllByRole('heading', { name: 'À traiter' })).length,
    ).toBe(1);
    expect(screen.getAllByText('Participants insuffisants')).toHaveLength(2);
    expect(
      screen.getByText(/3 situation\(s\) — détail dans le panneau À traiter/),
    ).toBeInTheDocument();
  });

  it('does not show quantity hint when attention count is 1', async () => {
    const situation = overviewSituation();
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        situations: [situation],
        attentionSummary: { count: 1, items: [situation] },
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: 'À traiter' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Participants insuffisants')).toBeInTheDocument();
    expect(
      screen.queryByText(/situation\(s\) — détail dans le panneau/),
    ).not.toBeInTheDocument();
  });

  it('hides Absent match dimension and console operational chrome', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        constructionDimensions: {
          ...overviewView().constructionDimensions,
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
    );

    renderOverviewPage();

    await screen.findByRole('heading', { name: 'Équipes' });
    expect(
      screen.queryByRole('heading', { name: 'Matchs', level: 3 }),
    ).not.toBeInTheDocument();
    expect(screen.queryByText('Focus opérationnel')).not.toBeInTheDocument();
  });

  it('navigates to Équipes from the teams dimension panel', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(overviewView());

    renderOverviewPage();

    await user.click(
      await screen.findByRole('link', { name: /Voir les équipes/i }),
    );

    expect(screen.getByText('Teams route')).toBeInTheDocument();
  });

  it('shows an error when the overview read fails', async () => {
    vi.mocked(fetchCompetitionOverview).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    );

    renderOverviewPage();

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    );
  });

  it('materializes matches then offers Voir les matchs and invalidates match lists', async () => {
    const user = userEvent.setup();
    const matchId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
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
    );
    vi.mocked(materializeMatches).mockResolvedValue({
      createdCount: 1,
      attachedMatchIds: [matchId],
      alreadyComplete: false,
    });

    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[`/competitions/${competitionId}`]}>
          <Routes>
            <Route
              path="/competitions/:competitionId"
              element={<CompetitionOverviewPage />}
            />
            <Route
              path="/competitions/:competitionId/matches"
              element={<p>Match hub route</p>}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    const materializeButtons = await screen.findAllByRole('button', {
      name: /Créer les matchs/i,
    });
    await user.click(materializeButtons[0]);

    await waitFor(() => {
      expect(materializeMatches).toHaveBeenCalledWith(stageId);
    });

    expect(
      await screen.findByRole('heading', { name: /Matchs créés/i }),
    ).toBeInTheDocument();
    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: ['matches', 'by-stage', stageId],
      }),
    );

    await user.click(screen.getByRole('link', { name: /Voir les matchs/i }));
    expect(screen.getByText('Match hub route')).toBeInTheDocument();
  });

  it('composes GeneratedCalendar from preparationFocus without Structure', async () => {
    vi.mocked(fetchCompetitionOverview).mockResolvedValue(
      overviewView({
        status: 'Ready',
        preparationFocus: 'GeneratedCalendar',
        calendarSummary: {
          matchdayCount: 3,
          matchCount: 6,
          matchdays: [
            { matchdayNumber: 1, matchCount: 2 },
            { matchdayNumber: 2, matchCount: 2 },
            { matchdayNumber: 3, matchCount: 2 },
          ],
          nextMatch: {
            matchId: overviewIds.matchId,
            stageId,
            matchdayNumber: 1,
            scheduledAt: null,
            homeDisplayName: 'Alpha',
            awayDisplayName: 'Bravo',
          },
        },
        constructionDimensions: {
          ...overviewView().constructionDimensions,
          structure: {
            prominence: 'Present',
            facts: {
              formatKind: 'Championship',
              matchdayCount: '3',
            },
          },
          matches: {
            prominence: 'Condensed',
            facts: { total: '6' },
          },
        },
        operationalFocus: {
          ...overviewView().operationalFocus,
          matchCounts: {
            live: 0,
            scheduled: 6,
            finished: 0,
            postponed: 0,
            cancelled: 0,
            total: 6,
          },
        },
        availableActions: [{ code: 'StartCompetition', guaranteed: false }],
        naturalProgression: null,
      }),
    );

    renderOverviewPage();

    expect(
      await screen.findByRole('heading', { name: /Calendrier sportif/i }),
    ).toBeInTheDocument();
    expect(screen.getByTestId('overview-calendar-panel')).toHaveTextContent(
      /3 journées/,
    );
    expect(screen.getByTestId('overview-calendar-panel')).toHaveTextContent(
      /Alpha – Bravo/,
    );
    expectOverviewRegionOrder(
      'overview-region-progression',
      'overview-region-calendar',
      'overview-region-config',
    );
    expectOverviewRegionsAbsent('overview-region-structure');
    expect(
      screen.queryByRole('heading', { name: /^Structure$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /Prochaine action/i }),
    ).toBeInTheDocument();
  });
});
