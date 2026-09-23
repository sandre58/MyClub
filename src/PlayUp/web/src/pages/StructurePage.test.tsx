import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  configureStructure,
  fetchStructureView,
  fetchStageOverview,
  fetchStageSchematic,
  ApiError,
} from '../api';
import type {
  StructureStageHubSummary,
  StructureView,
} from '../types';
import { StructurePage } from './StructurePage';
import { relevantPhaseSections } from './structureHubSections';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchStructureView: vi.fn(),
    configureStructure: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchStageSchematic: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const entryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const avalStageId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';

function championshipStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId,
    name: 'League',
    status: 'Draft',
    teamCount: 2,
    matchCount: 0,
    groupCount: 0,
    roundCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: true,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Championship',
    defaultsBinding: {
      matchDuration: { isBound: true },
      extraTime: { isBound: true },
      penaltyShootout: { isBound: true },
      administrativeResult: { isBound: true },
      points: { isBound: true },
      rankingCriteria: { isBound: true },
    },
    ...overrides,
  };
}

function groupesStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
    name: 'Groupes',
    status: 'Draft',
    teamCount: 8,
    matchCount: 12,
    groupCount: 2,
    roundCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: true,
    hasDrawRules: true,
    drawExecutionBadge: 'ToLaunch',
    numberOfPots: 4,
    hasQualificationRules: true,
    qualificationPathCount: 2,
    hasProgressionRules: true,
    progressionPathCount: 4,
    hasTieFormat: false,
    formatKind: 'Groups',
    defaultsBinding: {
      matchDuration: { isBound: true },
      extraTime: { isBound: true },
      penaltyShootout: { isBound: true },
      administrativeResult: { isBound: true },
      points: { isBound: true },
      rankingCriteria: { isBound: true },
    },
    ...overrides,
  };
}

function cupStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId,
    name: 'Finale',
    status: 'Draft',
    teamCount: 2,
    matchCount: 1,
    groupCount: 0,
    roundCount: 1,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: false,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Cup',
    defaultsBinding: {
      matchDuration: { isBound: true },
      extraTime: { isBound: true },
      penaltyShootout: { isBound: true },
      administrativeResult: { isBound: true },
      points: { isBound: true },
      rankingCriteria: { isBound: true },
    },
    ...overrides,
  };
}

function structureView(
  overrides: Partial<StructureView> = {},
): StructureView {
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
    actions: ['ConfigureStructure'],
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
    stages: [],
    ...overrides,
  };
}

function renderStructurePage(initialPath?: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[
          initialPath ?? `/competitions/${competitionId}/structure`,
        ]}
      >
        <Routes>
          <Route
            path="/competitions/:competitionId/teams"
            element={<p>Teams route</p>}
          />
          <Route
            path="/competitions/:competitionId/regulation"
            element={<p>Regulation route</p>}
          />
          <Route
            path="/competitions/:competitionId/structure"
            element={<StructurePage />}
          />
          <Route
            path="/competitions/:competitionId"
            element={<p>Workspace route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );

  return { queryClient };
}

describe('relevantPhaseSections', () => {
  it('omits absent sections instead of disabling them', () => {
    expect(relevantPhaseSections(championshipStage())).toEqual([
      'construction',
    ]);
  });

  it('includes qualification, progression and tirage when present', () => {
    expect(relevantPhaseSections(groupesStage())).toEqual([
      'construction',
      'qualification',
      'progression',
      'tirage',
    ]);
  });

  it('omits tirage for Groups/Cup until DrawRules are configured', () => {
    expect(
      relevantPhaseSections(groupesStage({ hasDrawRules: false })),
    ).toEqual(['construction', 'qualification', 'progression']);
  });

  it('includes relation sections when per-phase edit actions are offered', () => {
    expect(
      relevantPhaseSections(
        championshipStage({
          actions: ['ReplaceQualificationRules', 'ReplaceProgressionRules'],
        }),
      ),
    ).toEqual(['construction', 'qualification', 'progression']);
  });
});

describe('StructurePage Structure hub', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: stageId,
      competitionId,
      name: 'Phase',
      status: 'Draft',
      rounds: [],
      slots: [],
      draws: [],
    });
    vi.mocked(fetchStageSchematic).mockResolvedValue({
      stageId,
      competitionId,
      name: 'Phase',
      status: 'Draft',
      formatKind: 'Championship',
      cases: [],
      connections: [],
    });
    vi.mocked(configureStructure).mockResolvedValue({
      stageCreated: true,
      rebuildImpact: null,
      structure: structureView({
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        stages: [championshipStage()],
      }),
    });
  });

  it('shows loading while structure is pending', () => {
    vi.mocked(fetchStructureView).mockReturnValue(new Promise(() => {}));

    renderStructurePage();

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…');
  });

  it('renders Structure hub without identity / teams / regulation panels', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Structure' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Structure non prête/i),
    ).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Identité' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Équipes' })).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Règlement' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Modifier le règlement/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /Participants insuffisants/i }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/teams`);
  });

  it('shows flat phase fiche with match tile for a championship stage', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
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
        stages: [championshipStage()],
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
      }),
    );

    renderStructurePage();

    expect(await screen.findByRole('button', { name: /League/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'League' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /^Match$/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Population/i })).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /^Sorties$/i }),
    ).not.toBeInTheDocument();
    expect(screen.getAllByText('Général').length).toBeGreaterThanOrEqual(1);
    // S5: no Slots|Équipes toggle — the schematic is a single populated view.
    expect(
      screen.queryByRole('group', { name: /Affichage de la silhouette/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('tablist', { name: /Sections de la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('shows groups fiche tiles without domain drill pages', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Groups',
          primaryStageId: groupesStage().stageId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Draft',
        },
        stages: [groupesStage()],
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
    );

    renderStructurePage();

    expect(await screen.findByRole('heading', { name: 'Groupes' })).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /Tirage/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Population/i })).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /^Sorties$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('tablist', { name: /Sections de la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('shows an error when structure read fails', async () => {
    vi.mocked(fetchStructureView).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    );

    renderStructurePage();

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    );
  });

  it('opens match rules dialog from Structure deep-link section', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          championshipStage({
            actions: [
              'ReplaceMatchRules',
              'BindToCompetition',
              'ReplaceStandingRules',
            ],
          }),
        ],
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
      }),
    );

    renderStructurePage(
      `/competitions/${competitionId}/structure?stage=${stageId}&section=matchs`,
    );

    expect(
      await screen.findByRole('dialog', {
        name: /Règles de match/i,
      }),
    ).toBeInTheDocument();
  });

  it('configures championship structure via dialog', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());

    renderStructurePage();

    const configureButtons = await screen.findAllByRole('button', {
      name: /Créer la structure/i,
    });
    await user.click(configureButtons[0]!);
    const dialog = await screen.findByRole('dialog');
    await user.selectOptions(
      await within(dialog).findByLabelText(/^Format$/i),
      'Championship',
    );
    const matchdays = within(dialog).getByLabelText(/Nombre de journées/i);
    fireEvent.change(matchdays, { target: { value: '2' } });
    await user.click(
      within(dialog).getByRole('button', { name: 'Créer la structure' }),
    );

    await waitFor(() => {
      expect(configureStructure).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          format: 'Championship',
          matchdayCount: 2,
          groupCount: null,
          participantsPerGroup: null,
          bracketSize: null,
          matchGenerationFormat: 'SingleRoundRobin',
        }),
      );
    });
  });

  it('offers add-phase when Host exposes AddCompetitionStage', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        actions: ['ConfigureStructure', 'AddCompetitionStage'],
        stages: [championshipStage()],
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
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
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('button', { name: /Ajouter une phase/i }),
    ).toBeInTheDocument();
    expect(
      screen.getAllByRole('button', { name: /Reconstruire/i }).length,
    ).toBeGreaterThanOrEqual(1);
  });

  it('surfaces structural anomalies in topology with a Qual/Prog fix CTA', async () => {
    const user = userEvent.setup();
    const knockOutId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        actions: [],
        stages: [
          groupesStage({
            structureIssues: ['DanglingQualificationTarget'],
            qualificationPathCount: 1,
            hasProgressionRules: false,
            progressionPathCount: 0,
            hasDrawRules: false,
            qualificationPaths: [
              {
                order: 1,
                selectionMode: 'Top',
                selectionValue: 2,
                destinationStageId: knockOutId,
              },
            ],
          }),
          championshipStage({
            stageId: knockOutId,
            name: 'Barrages',
            formatKind: 'Cup',
            hasStandingRules: false,
            hasQualificationRules: false,
            qualificationPathCount: 0,
          }),
        ],
        format: {
          kind: 'Groups',
          primaryStageId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Draft',
        },
        readiness: {
          readyForNextSlice: false,
          readyForDraw: false,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: false,
          attachedMatchCount: 0,
          blockers: ['StructureGraphInvalid', 'MissingPotRules'],
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByText(/Structure non prête/i),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Règles de pots manquantes/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/Graphe de structure invalide/i),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText(/points à lever/i),
    ).not.toBeInTheDocument();

    const topology = screen.getByRole('region', {
      name: /Topologie/i,
    });
    expect(
      within(topology).getByText(/^1 anomalie structurelle$/i),
    ).toBeInTheDocument();
    expect(
      within(topology).getByText(/Destination de qualification absente/i),
    ).toBeInTheDocument();
    expect(
      within(topology).getByText(/^Anomalie structurelle$/i),
    ).toBeInTheDocument();
    expect(
      within(topology).queryByText(/Affectation par tirage/i),
    ).not.toBeInTheDocument();
    expect(
      within(topology).queryByText(/Tirage (à lancer|en cours|à appliquer|appliqué)/i),
    ).not.toBeInTheDocument();
    expect(
      within(topology).queryByText(/Tirage (à définir|requis|configuré)/i),
    ).not.toBeInTheDocument();
    expect(
      within(topology).getByText(/Qualification · 1 chemin/i),
    ).toBeInTheDocument();

    await user.click(
      within(topology).getByRole('button', { name: /Corriger la relation/i }),
    );

    expect(
      await screen.findByRole('dialog', {
        name: /Qualifications/i,
      }),
    ).toBeInTheDocument();
  });

  it('signals multi-destination stages without drawing a fake graph', async () => {
    const finaleId = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
    const bronzeId = '99999999-9999-9999-9999-999999999999';
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        actions: [],
        stages: [
          championshipStage({
            name: 'Demi',
            formatKind: 'Cup',
            hasStandingRules: false,
            hasProgressionRules: true,
            progressionPathCount: 2,
            progressionPaths: [
              {
                sourceFixtureId: 'sf-1',
                outcome: 'Winner',
                destinationStageId: finaleId,
                destinationSlotKey: 'home',
              },
              {
                sourceFixtureId: 'sf-1',
                outcome: 'Loser',
                destinationStageId: bronzeId,
                destinationSlotKey: 'home',
              },
            ],
          }),
          championshipStage({
            stageId: finaleId,
            name: 'Finale',
            formatKind: 'Cup',
            hasStandingRules: false,
          }),
          championshipStage({
            stageId: bronzeId,
            name: 'Match bronze',
            formatKind: 'Cup',
            hasStandingRules: false,
          }),
        ],
        format: {
          kind: 'Cup',
          primaryStageId: stageId,
          primaryStageName: 'Demi',
          primaryStageStatus: 'Draft',
        },
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
      }),
    );

    renderStructurePage();

    const topology = await screen.findByRole('region', {
      name: /Topologie/i,
    });
    expect(
      within(topology).getByRole('button', { name: '→ Finale' }),
    ).toBeInTheDocument();
    expect(
      within(topology).getByRole('button', { name: '→ Match bronze' }),
    ).toBeInTheDocument();
    expect(
      within(topology).queryByText(/Progression ·/i),
    ).not.toBeInTheDocument();
  });

  it('disables configure when Host actions omit it', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({ actions: [] }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Structure' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Créer la structure/i }),
    ).toBeDisabled();
    expect(
      screen.getByRole('button', { name: /Ajouter une phase/i }),
    ).toBeDisabled();
  });

  it('shows materialize readiness status without Overview CTA', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        stages: [championshipStage()],
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
    );

    renderStructurePage();

    expect(
      await screen.findByText(/La compétition est prête à matérialiser/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Vue d’ensemble/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Préparer|Démarrer|Tirer|Matérialiser/i }),
    ).not.toBeInTheDocument();
  });

  it('does not promote draw readiness as Structure-global Hub status', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Groups',
          primaryStageId: groupesStage().stageId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Draft',
        },
        stages: [groupesStage()],
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
    );

    renderStructurePage();

    expect(
      screen.queryByText(/Prêt pour le tirage/i),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/Tirage requis/i)).not.toBeInTheDocument();
    expect(
      await screen.findByText(/Tirage à lancer/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Vue d’ensemble/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Préparer|Démarrer|Publier|Appliquer/i }),
    ).not.toBeInTheDocument();
  });

  it('routes MissingStructure incomplete CTA to configure dialog', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        readiness: {
          readyForNextSlice: false,
          readyForDraw: false,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: false,
          attachedMatchCount: 0,
          blockers: ['MissingStructure'],
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByText(/Structure non prête/i),
    ).toBeInTheDocument();
    await user.click(
      screen.getByRole('button', { name: /Structure manquante/i }),
    );
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
  });

  it('shows empty Sorties and Attribution rails with + when editable; omits kebab add items', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Cup',
          primaryStageId: stageId,
          primaryStageName: 'Demi-finales',
          primaryStageStatus: 'Draft',
        },
        stages: [
          cupStage({
            stageId,
            name: 'Demi-finales',
            actions: [
              'ReplaceProgressionRules',
              'ReplacePlacementAwardRules',
              'RemoveStage',
            ],
            hasProgressionRules: false,
            progressionPathCount: 0,
            hasPlacementAwardRules: false,
            placementAwardCount: 0,
          }),
          cupStage({
            stageId: avalStageId,
            name: 'Finale',
            actions: [],
          }),
        ],
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Demi-finales' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Population/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /^Sorties$/i })).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /Attribution des places/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Ajouter une sortie/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Ajouter une attribution/i }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Autres actions/i }));
    const menu = await screen.findByRole('menu', { name: /Autres actions/i });
    expect(
      within(menu).queryByRole('menuitem', { name: /Ajouter une sortie/i }),
    ).not.toBeInTheDocument();
    expect(
      within(menu).queryByRole('menuitem', {
        name: /Ajouter une attribution/i,
      }),
    ).not.toBeInTheDocument();
    expect(
      within(menu).getByRole('menuitem', { name: /Supprimer la phase/i }),
    ).toBeInTheDocument();
  });

  it('hides empty Sorties without aval peer; hides Attribution on Championship', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        stages: [
          championshipStage({
            actions: ['ReplaceQualificationRules'],
          }),
        ],
      }),
    );

    renderStructurePage();

    expect(await screen.findByRole('heading', { name: 'League' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Population/i })).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /^Sorties$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /Attribution des places/i }),
    ).not.toBeInTheDocument();
  });

  it('shows Sorties and Attribution with content and edit controls when editable', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Cup',
          primaryStageId: stageId,
          primaryStageName: 'Finale',
          primaryStageStatus: 'Draft',
        },
        stages: [
          cupStage({
            stageId,
            name: 'Finale',
            actions: [
              'ReplaceProgressionRules',
              'ReplacePlacementAwardRules',
            ],
            progressionIntents: [
              {
                intentId: 'pi-1',
                order: 0,
                roundId: 'round-1',
                roundName: 'Finale',
                outcome: 'Winner',
                destinationStageId: avalStageId,
                expandedPathCount: 1,
              },
            ],
            hasProgressionRules: true,
            progressionPathCount: 1,
            placementAwards: [
              {
                rank: 1,
                outcome: 'Winner',
                sourceFixtureId: 'fix-final',
                sourceLabel: 'Finale · #1',
              },
              {
                rank: 2,
                outcome: 'Loser',
                sourceFixtureId: 'fix-final',
                sourceLabel: 'Finale · #1',
              },
            ],
            hasPlacementAwardRules: true,
            placementAwardCount: 2,
          }),
          cupStage({
            stageId: avalStageId,
            name: 'Hors tableau',
            actions: [],
          }),
        ],
      }),
    );

    renderStructurePage();

    expect(await screen.findByRole('heading', { name: 'Finale' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Population/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /^Sorties$/i })).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /Attribution des places/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Ajouter une sortie/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Ajouter une attribution/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Modifier les sorties/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /^Modifier$/i }),
    ).toBeInTheDocument();
  });
});
