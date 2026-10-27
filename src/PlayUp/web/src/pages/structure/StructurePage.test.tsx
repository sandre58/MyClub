import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  addCompetitionStage,
  insertCompetitionStage,
  fetchStructureView,
  fetchNeedsAttention,
  fetchStageOverview,
  fetchStageSchematic,
  renameStage,
  ApiError,
} from '../../api';
import type { StructureStageHubSummary, StructureView } from '../../types';
import { StructurePage } from './StructurePage';
import { relevantPhaseSections } from './structureHubSections';
import { withFixturePresentedIssueCodes } from './structurePresentedIssueCodes.fixture';
import { readyMatchProductionHub } from './structureMatchProductionPresentation.testPayloads';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchStructureView: vi.fn(),
    fetchNeedsAttention: vi.fn(),
    addCompetitionStage: vi.fn(),
    insertCompetitionStage: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchStageSchematic: vi.fn(),
    renameStage: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const entryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const avalStageId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';

const compositionBandEmptyHint =
  /Ajouter des équipes à la phase ou alimenter les entrées/i;

const hubReadyReadiness = {
  readyForNextSlice: true,
  readyForDraw: true,
  readyForMaterialization: false,
  readyForSchedule: false,
  readyForMatchOperation: false,
  readyForSchedulePath: false,
  attachedMatchCount: 0,
  blockers: [] as string[],
  blockerDetails: [],
  nextConstructionStepHint: 'None' as const,
};

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
    actions: ['ReplaceDrawRules'],
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

function structureView(overrides: Partial<StructureView> = {}): StructureView {
  const viewWithoutPresented: StructureView = {
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
    actions: ['ConfigureStructure', 'AddCompetitionStage'],
    readiness: {
      readyForNextSlice: false,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers: ['InsufficientParticipants', 'MissingStage'],
      blockerDetails: [],
      nextConstructionStepHint: 'None',
    },
    stages: [],
    ...overrides,
  };
  return {
    ...viewWithoutPresented,
    stages: viewWithoutPresented.stages.map((stage) =>
      withFixturePresentedIssueCodes(viewWithoutPresented, stage),
    ),
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
    vi.mocked(addCompetitionStage).mockReset();
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
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 0,
      items: [],
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
    // Birth (0 phase): empty state owns UX — no readiness banner.
    expect(
      screen.queryByText(/Structure à finaliser/i),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Voir les problèmes/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Créer la structure/i }),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/équipes à compléter/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/forme à corriger/i)).not.toBeInTheDocument();
    expect(screen.getByText('Aucune phase')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Ajouter une phase/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/Sélectionnez une phase/i),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Topologie' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Identité' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Équipes' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Règlement' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Modifier le règlement/i }),
    ).not.toBeInTheDocument();
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
        stages: [
          championshipStage({
            actions: ['ReplaceAffectationAuthoring'],
          }),
        ],
        readiness: {
          readyForNextSlice: false,
          readyForDraw: false,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: false,
          attachedMatchCount: 0,
          blockers: [],
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'League' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('region', { name: /Topologie/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /^Match$/i }),
    ).toBeInTheDocument();
    expect(screen.getByText(compositionBandEmptyHint)).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /^Sorties$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getAllByText('Règles héritées du règlement général').length,
    ).toBeGreaterThanOrEqual(1);
    // S5: no Slots|Teams toggle — the schematic is a single populated view.
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
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Groupes' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('region', { name: /Topologie/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('region', { name: /État de la phase/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
    const launch = screen.getByRole('button', { name: /Lancer le tirage/i });
    expect(launch).toHaveClass('ds-btn--primary');
    expect(launch).not.toHaveTextContent(/Tirage aléatoire/i);
    expect(
      screen.getByRole('button', { name: /^Paramètres$/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Désactiver le tirage/i }),
    ).toBeEnabled();
    expect(
      screen.getByRole('heading', { name: /Affectation des places/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /^Sorties$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('tablist', { name: /Sections de la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('shows secondary activate-draw CTA when DrawRules are absent', async () => {
    const groups = groupesStage({
      hasDrawRules: false,
      drawExecutionBadge: undefined,
      numberOfPots: null,
      actions: ['ReplaceDrawRules'],
    });
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Groups',
          primaryStageId: groups.stageId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Draft',
        },
        stages: [groups],
      }),
    );
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: groups.stageId,
      competitionId,
      name: 'Groupes',
      status: 'Draft',
      rounds: [],
      slots: [],
      draws: [],
    });
    vi.mocked(fetchStageSchematic).mockResolvedValue({
      stageId: groups.stageId,
      competitionId,
      name: 'Groupes',
      status: 'Draft',
      formatKind: 'Groups',
      cases: [],
      connections: [],
    });

    renderStructurePage();

    expect(
      await screen.findByRole('region', { name: /État de la phase/i }),
    ).toBeInTheDocument();
    const drawSection = screen
      .getByRole('heading', { name: 'Tirage' })
      .closest('section') as HTMLElement;
    const activate = within(drawSection).getByRole('button', {
      name: /^Activer le tirage$/i,
    });
    expect(
      screen.queryByRole('button', { name: /Lancer le tirage/i }),
    ).not.toBeInTheDocument();
    expect(activate).toHaveClass('ds-btn--primary');
    expect(
      screen.queryByRole('button', { name: /Paramètres du tirage/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Désactiver le tirage/i }),
    ).not.toBeInTheDocument();
  });

  it('keeps draw params off the phase toolbar and blocks deactivate when a draw is alive', async () => {
    const groupsId = groupesStage().stageId;
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: groupsId,
      competitionId,
      name: 'Groupes',
      status: 'Draft',
      rounds: [],
      slots: [],
      draws: [
        {
          id: 'draw-1',
          kind: 'Group',
          status: 'Draft',
          resolutionState: 'NotResolved',
          slotPlacements: [],
        },
      ],
    });
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Groups',
          primaryStageId: groupsId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Draft',
        },
        stages: [groupesStage()],
        readiness: hubReadyReadiness,
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
    const deactivate = screen.getByRole('button', {
      name: /Désactiver le tirage/i,
    });
    expect(deactivate).toBeEnabled();

    expect(
      screen.queryByRole('button', { name: /Autres actions/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /^Paramètres$/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Supprimer la phase/i }),
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

  it('births the first phase via AddPhase from the empty state', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({ actions: ['AddCompetitionStage'] }),
    );
    vi.mocked(addCompetitionStage).mockResolvedValue({
      stageId,
      name: 'League',
      structure: structureView({
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        stages: [championshipStage()],
        actions: ['AddCompetitionStage'],
      }),
    });

    renderStructurePage();

    await user.click(
      await screen.findByRole('button', { name: /Ajouter une phase/i }),
    );
    const dialog = await screen.findByRole('dialog', {
      name: /Nouvelle phase/i,
    });
    await user.type(
      within(dialog).getByLabelText(/Nom de la phase/i),
      'League',
    );
    await user.click(
      within(dialog).getByRole('checkbox', { name: /Championnat/i }),
    );
    await user.click(
      within(dialog).getByRole('button', { name: /Créer la phase/i }),
    );

    await waitFor(() => {
      expect(addCompetitionStage).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          format: 'Championship',
          name: 'League',
        }),
      );
    });
  });

  it('offers single-phase split birth in PageHead when Host exposes AddCompetitionStage', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        actions: ['ConfigureStructure', 'AddCompetitionStage'],
        stages: [
          championshipStage({
            actions: ['RebuildStructure'],
            matchdayCount: 2,
            compositionCapacity: 8,
          }),
        ],
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
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'League' }),
    ).toBeInTheDocument();
    expect(
      await screen.findByRole('button', { name: /Ajouter une phase/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Insérer une phase' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Insérer une phase avant League' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /^Reconstruire$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /^Forme$/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Modifier la structure/i }),
    ).toBeInTheDocument();
  });

  it('opens EditSkeleton from the Forme CTA near the schematic', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        actions: ['AddCompetitionStage'],
        stages: [
          championshipStage({
            actions: ['RebuildStructure'],
            matchdayCount: 2,
            compositionCapacity: 8,
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

    renderStructurePage();

    await user.click(
      await screen.findByRole('button', {
        name: /Modifier la structure/i,
      }),
    );
    const dialog = await screen.findByRole('dialog');
    expect(
      within(dialog).getByRole('heading', {
        name: /Modifier la structure/i,
      }),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByText(
        /La reconstruction supprimera la structure actuelle/i,
      ),
    ).toBeInTheDocument();
    expect(within(dialog).queryByRole('switch')).not.toBeInTheDocument();
    expect(
      within(dialog).getByRole('button', { name: /^Reconstruire$/i }),
    ).toBeInTheDocument();
  });

  it('creates a phase via ChoiceTiles from a topology insert slot and selects the new stage', async () => {
    const user = userEvent.setup();
    const newStageId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
    const secondStageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
    const initial = structureView({
      actions: ['ConfigureStructure', 'AddCompetitionStage'],
      stages: [
        championshipStage(),
        championshipStage({
          stageId: secondStageId,
          name: 'Playoffs',
        }),
      ],
      format: {
        kind: 'Championship',
        primaryStageId: stageId,
        primaryStageName: 'League',
        primaryStageStatus: 'Draft',
      },
    });
    const createdStage = championshipStage({
      stageId: newStageId,
      name: 'Barrages',
      formatKind: 'Cup',
      hasStandingRules: false,
      slotCount: 4,
    });
    const afterCreate = structureView({
      actions: ['ConfigureStructure', 'AddCompetitionStage'],
      stages: [
        championshipStage(),
        createdStage,
        championshipStage({
          stageId: secondStageId,
          name: 'Playoffs',
        }),
      ],
      format: {
        kind: 'Championship',
        primaryStageId: stageId,
        primaryStageName: 'League',
        primaryStageStatus: 'Draft',
      },
    });

    let currentView = initial;
    vi.mocked(fetchStructureView).mockImplementation(async () => currentView);
    vi.mocked(insertCompetitionStage).mockImplementation(async () => {
      currentView = afterCreate;
      return {
        stageId: newStageId,
        name: 'Barrages',
        structure: afterCreate,
      };
    });

    renderStructurePage();

    await user.click(
      await screen.findByRole('button', {
        name: 'Insérer une phase après League',
      }),
    );

    const dialog = await screen.findByRole('dialog', {
      name: /Insérer une phase/i,
    });
    await user.type(
      within(dialog).getByPlaceholderText(/ex\. Saison régulière/i),
      'Temp',
    );
    await user.click(within(dialog).getByRole('button', { name: /Annuler/i }));
    expect(
      await screen.findByRole('dialog', { name: /Abandonner la création/i }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /^Abandonner$/i }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    await user.click(
      screen.getByRole('button', { name: 'Insérer une phase après League' }),
    );
    const createDialog = await screen.findByRole('dialog', {
      name: /Insérer une phase/i,
    });
    await user.type(
      within(createDialog).getByPlaceholderText(/ex\. Saison régulière/i),
      'Barrages',
    );
    expect(
      within(createDialog).getByText(/Aucun type sélectionné/i),
    ).toBeInTheDocument();
    expect(
      within(createDialog).getByText(
        /Choisissez un type pour définir la structure de la phase/i,
      ),
    ).toBeInTheDocument();

    await user.click(
      within(createDialog).getByRole('checkbox', { name: /Coupe/i }),
    );
    expect(
      within(createDialog).getByText(/Cette décision est irréversible/i),
    ).toBeInTheDocument();
    expect(
      within(createDialog).queryByText(/Aucun type sélectionné/i),
    ).not.toBeInTheDocument();
    expect(
      within(createDialog).getByRole('checkbox', { name: '16' }),
    ).toBeInTheDocument();
    expect(
      within(createDialog).queryByRole('button', { name: /Continuer/i }),
    ).not.toBeInTheDocument();
    expect(
      within(createDialog).queryByLabelText(/Nombre de journées/i),
    ).not.toBeInTheDocument();

    await user.click(within(createDialog).getByRole('checkbox', { name: '8' }));
    await user.click(
      within(createDialog).getByRole('button', { name: /Insérer la phase/i }),
    );

    await waitFor(() => {
      expect(insertCompetitionStage).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          format: 'Cup',
          name: 'Barrages',
          bracketSize: 8,
          anchorStageId: stageId,
          position: 'After',
        }),
      );
    });
    expect(addCompetitionStage).not.toHaveBeenCalled();

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    await waitFor(() => {
      const selected = document.querySelector(
        '.structure-topology__tile[data-selected="true"]',
      );
      expect(selected?.textContent).toContain('Barrages');
    });
  });

  it('shows graph ambient on topology tile; Sorties owns the fix', async () => {
    const user = userEvent.setup();
    const knockOutId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        actions: ['ReplaceQualificationRules'],
        stages: [
          groupesStage({
            structureIssues: ['DanglingQualificationTarget'],
            qualificationPathCount: 1,
            hasProgressionRules: false,
            progressionPathCount: 0,
            hasDrawRules: false,
            compositionCapacity: 8,
            affectationEntryCount: 8,
            actions: ['ReplaceQualificationRules', 'ReplaceDrawRules'],
            qualificationIntents: [
              {
                intentId: 'q-dangling',
                order: 1,
                sourceKind: 'Overall',
                positionFrom: 1,
                positionTo: 2,
                destinationStageId: knockOutId,
                destinationCount: 2,
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
          // E1 — no MissingPotRules without engaged draw
          blockers: ['StructureGraphInvalid'],
          blockerDetails: [],
        },
      }),
    );

    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 1,
      items: [
        {
          source: 'DanglingQualificationTarget',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
          params: { stageId: 'dddddddd-dddd-dddd-dddd-dddddddddddd' },
        },
      ],
    });

    renderStructurePage();

    expect(await screen.findByText(/Anomalies à traiter/i)).toBeInTheDocument();
    expect(screen.queryByText(/1 problème identifié/i)).not.toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Corriger la qualification/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText(/Pots de tirage manquants/i),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Voir les problèmes/i }),
    ).not.toBeInTheDocument();

    const topology = screen.getByRole('region', {
      name: /Topologie/i,
    });
    expect(
      within(topology).queryByText(/^1 liaison à corriger$/i),
    ).not.toBeInTheDocument();
    expect(
      within(topology).queryByRole('button', { name: /Voir la phase/i }),
    ).not.toBeInTheDocument();
    expect(
      within(topology).getByLabelText(/Écarts sur cette phase/i),
    ).toBeInTheDocument();
    expect(
      within(topology).queryByText(/Destination de qualification introuvable/i),
    ).not.toBeInTheDocument();

    // E3 — Sorties shows the graph diagnostic on its canonical tile (no header navigator).
    const sorties = await screen.findByRole('complementary', {
      name: /Sorties/i,
    });
    expect(
      within(sorties).getByText(/Destination de qualification introuvable/i),
    ).toBeInTheDocument();
    expect(
      within(sorties).queryByText(/1 liaison à corriger/i),
    ).not.toBeInTheDocument();
    expect(
      within(sorties).queryByRole('button', { name: /^Corriger$/i }),
    ).not.toBeInTheDocument();
    await user.click(
      within(sorties).getByRole('button', { name: /Modifier les sorties/i }),
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
            progressionIntents: [
              {
                intentId: 'p-winner',
                order: 1,
                roundId: 'round-1',
                outcome: 'Winner',
                destinationStageId: finaleId,
                destinationSlotKeys: ['home'],
                expandedPathCount: 1,
              },
              {
                intentId: 'p-loser',
                order: 2,
                roundId: 'round-1',
                outcome: 'Loser',
                destinationStageId: bronzeId,
                destinationSlotKeys: ['home'],
                expandedPathCount: 1,
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
          blockerDetails: [],
          nextConstructionStepHint: 'None',
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

  it('hides add-phase when Host actions omit it', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        actions: [],
        readiness: {
          readyForNextSlice: false,
          readyForDraw: false,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: false,
          attachedMatchCount: 0,
          blockers: ['InsufficientParticipants'],
          blockerDetails: [],
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Structure' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Ajouter une phase/i }),
    ).not.toBeInTheDocument();
    // Teams shortfall is not a Structure banner signal (Équipes owns correction).
    expect(
      screen.queryByText(/Structure à finaliser/i),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/équipes à compléter/i)).not.toBeInTheDocument();
  });

  it('hides production gauge when Running materialize is frozen and shows match progress', async () => {
    const groups = groupesStage({
      status: 'Running',
      hasDrawRules: false,
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'exhausted',
        determinedCount: 6,
        eligibleCount: 0,
        materializedCount: 3,
        commandEnabled: false,
        disabledReason: 'StructureNotMutable',
        confrontations: [],
      },
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: false },
        matchProgress: {
          total: 6,
          finished: 3,
          open: 3,
          cancelled: 0,
          canComplete: false,
        },
      },
    });
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        status: 'Running',
        format: {
          kind: 'Groups',
          primaryStageId: groups.stageId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Running',
        },
        stages: [groups],
        readiness: {
          readyForNextSlice: true,
          readyForDraw: false,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: true,
          attachedMatchCount: 3,
          blockers: [],
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
      }),
    );
    vi.mocked(fetchStageSchematic).mockResolvedValue({
      stageId: groups.stageId,
      competitionId,
      name: 'Groupes',
      status: 'Running',
      formatKind: 'Groups',
      cases: [],
      connections: [],
    });

    renderStructurePage();

    await screen.findByRole('heading', { name: 'Matchs' });
    expect(
      screen.queryByText(/Il reste 3 confrontations sans match créé/i),
    ).not.toBeInTheDocument();
    const matchesSection = screen
      .getByRole('heading', { name: 'Matchs' })
      .closest('.structure-phase-tile--matchs') as HTMLElement;
    expect(
      within(matchesSection).queryByLabelText(
        /Avancement de la production des matchs/i,
      ),
    ).not.toBeInTheDocument();
    expect(
      within(matchesSection).getByLabelText(/Avancement des matchs/i),
    ).toBeInTheDocument();
    expect(
      within(matchesSection).getByText('3', {
        selector: '.structure-operational-strip__ratio-filled',
      }),
    ).toBeInTheDocument();
    expect(
      within(matchesSection).getByText('6', {
        selector: '.structure-operational-strip__ratio-cap',
      }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Créer les matchs/i }),
    ).not.toBeInTheDocument();
  });

  it('shows materialize readiness status without Overview CTA', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Ready',
        },
        stages: [
          championshipStage({
            status: 'Ready',
            lifecycle: {
              transition: { code: 'StartStage', enabled: true },
            },
            materialize: {
              commandCode: 'MaterializeMatches',
              labelVariant: 'create',
              determinedCount: 1,
              eligibleCount: 1,
              materializedCount: 0,
              commandEnabled: true,
              confrontations: [],
            },
            matchProductionPresentation: readyMatchProductionHub({
              primaryCurrent: 0,
              primaryCapacity: 1,
              primaryMessageCode: 'Partial',
              materializeVisible: true,
              materializeEnabled: true,
            }),
          }),
        ],
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
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
        hubNextStep: {
          code: 'MaterializeMatches',
          stageId,
          stageName: 'League',
        },
      }),
    );

    renderStructurePage();

    expect(await screen.findByText(/^Matchs à créer$/i)).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Créer les matchs/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Vue d’ensemble/i }),
    ).not.toBeInTheDocument();
  });

  it('shows hub step label with phase name when competition has multiple stages', async () => {
    const finaleStageId = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Groups',
          primaryStageId: groupesStage().stageId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Running',
        },
        stages: [
          groupesStage({
            status: 'Running',
            compositionCapacity: 8,
            affectationEntryCount: 8,
            structureIssues: [],
          }),
          groupesStage({
            stageId: finaleStageId,
            name: 'Finale',
            status: 'Draft',
            formatKind: 'Cup',
            compositionCapacity: 8,
            affectationEntryCount: 8,
            structureIssues: [],
          }),
        ],
        readiness: hubReadyReadiness,
        hubNextStep: {
          code: 'PrepareStage',
          stageId: finaleStageId,
          stageName: 'Finale',
        },
      }),
    );

    renderStructurePage();

    expect(await screen.findByText(/À préparer · Finale/i)).toBeInTheDocument();
  });

  it('shows phase lifecycle in fiche header without Matchs tile in Draft', async () => {
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
            lifecycle: {
              transition: {
                code: 'PrepareStage',
                enabled: false,
                disabledReasonCode: 'StageNotReady',
              },
            },
          }),
        ],
      }),
    );
    vi.mocked(fetchStageSchematic).mockResolvedValue({
      stageId,
      competitionId,
      name: 'League',
      status: 'Draft',
      formatKind: 'Championship',
      cases: [],
      connections: [],
    });

    renderStructurePage();

    expect(document.querySelector('.structure-phase-status-band')).toBeNull();
    expect(
      screen.queryByRole('heading', { name: 'Matchs' }),
    ).not.toBeInTheDocument();
    expect(
      await screen.findByRole('button', {
        name: /marquer comme prête/i,
      }),
    ).toBeDisabled();
  });

  it('does not show materialization-ready banner when matches already exist', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          championshipStage({
            actions: ['MaterializeMatches'],
            matchdayCount: 2,
            compositionCapacity: 8,
            materialize: {
              commandCode: 'MaterializeMatches',
              labelVariant: 'complete',
              determinedCount: 4,
              materializedCount: 4,
              eligibleCount: 4,
              commandEnabled: true,
              confrontations: [],
            },
          }),
        ],
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        participants: {
          activeCount: 8,
          occupyingCount: 8,
          entries: [],
        },
        readiness: {
          readyForNextSlice: true,
          readyForDraw: false,
          readyForMaterialization: true,
          readyForSchedule: true,
          readyForMatchOperation: true,
          readyForSchedulePath: true,
          attachedMatchCount: 4,
          blockers: [],
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'League' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/Prête pour les matchs/i),
    ).not.toBeInTheDocument();
  });

  it('shows PageHead calm success when hub has no next step and phases are at least Ready', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Groups',
          primaryStageId: groupesStage().stageId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Ready',
        },
        stages: [
          groupesStage({
            status: 'Ready',
            compositionCapacity: 8,
            affectationEntryCount: 8,
            structureIssues: [],
          }),
        ],
        readiness: {
          readyForNextSlice: true,
          readyForDraw: false,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: false,
          attachedMatchCount: 0,
          blockers: [],
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
        hubNextStep: null,
      }),
    );

    renderStructurePage();

    expect(await screen.findByText(/Structure prête/i)).toBeInTheDocument();
  });

  it('does not show calm PageHead when hubNextStep is null but a phase is still Draft', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        format: {
          kind: 'Championship',
          primaryStageId: stageId,
          primaryStageName: 'League',
          primaryStageStatus: 'Draft',
        },
        stages: [championshipStage({ status: 'Draft', structureIssues: [] })],
        readiness: hubReadyReadiness,
        hubNextStep: null,
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'League' }),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Structure prête/i)).not.toBeInTheDocument();
  });

  it('shows PageHead negative hint when attention has items but structure synthesis is clean', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          championshipStage({
            structureIssues: [],
            compositionCapacity: 8,
            affectationEntryCount: 8,
          }),
        ],
        readiness: {
          readyForNextSlice: true,
          readyForDraw: false,
          readyForMaterialization: true,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: true,
          attachedMatchCount: 0,
          blockers: [],
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
      }),
    );
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 1,
      items: [
        {
          source: 'QualificationPending',
          severity: 'Blocking',
          targetType: 'Stage',
          targetId: stageId,
        },
      ],
    });

    renderStructurePage();

    expect(await screen.findByText(/Anomalies à traiter/i)).toBeInTheDocument();
    expect(
      screen.queryByText(/Prête pour les matchs/i),
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
        stages: [
          groupesStage({
            compositionCapacity: 8,
            affectationEntryCount: 8,
            structureIssues: [],
          }),
        ],
        participants: {
          activeCount: 8,
          occupyingCount: 8,
          entries: Array.from({ length: 8 }, (_, index) => ({
            entryId: `${index}0000000-0000-0000-0000-00000000000${index}`,
            displayName: `Team ${index}`,
            status: 'Active' as const,
          })),
        },
        readiness: {
          readyForNextSlice: true,
          readyForDraw: true,
          readyForMaterialization: false,
          readyForSchedule: false,
          readyForMatchOperation: false,
          readyForSchedulePath: false,
          attachedMatchCount: 0,
          blockers: [],
          blockerDetails: [],
          nextConstructionStepHint: 'RunDraw',
        },
      }),
    );

    renderStructurePage();

    expect(screen.queryByText(/Prêt pour le tirage/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Tirage requis/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Tirage à lancer/i)).not.toBeInTheDocument();
    expect(
      await screen.findByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Vue d’ensemble/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', {
        name: /Préparer|Démarrer|Publier|Appliquer/i,
      }),
    ).not.toBeInTheDocument();
  });

  it('hides Ready for next slice when competition lifecycle is closed', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        status: 'Completed',
        format: {
          kind: 'Groups',
          primaryStageId: groupesStage().stageId,
          primaryStageName: 'Groupes',
          primaryStageStatus: 'Completed',
        },
        stages: [groupesStage({ status: 'Completed' })],
        readiness: {
          readyForNextSlice: true,
          readyForDraw: false,
          readyForMaterialization: false,
          readyForSchedule: true,
          readyForMatchOperation: false,
          readyForSchedulePath: true,
          attachedMatchCount: 4,
          blockers: [],
          blockerDetails: [],
          nextConstructionStepHint: 'None',
        },
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Groupes' }),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Structure prête/i)).not.toBeInTheDocument();
  });

  it('keeps birth empty state when Host reports MissingStructure', async () => {
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
          blockerDetails: [],
        },
      }),
    );

    renderStructurePage();

    expect(await screen.findByText('Aucune phase')).toBeInTheDocument();
    expect(
      screen.queryByText(/Structure à finaliser/i),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Créer la structure/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Ajouter une phase/i }),
    ).toBeInTheDocument();
  });

  it('shows empty Sorties and Attribution rails with + when editable; phase toolbar has remove not add', async () => {
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
              'RenameStage',
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
            actions: ['RenameStage'],
          }),
        ],
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Demi-finales' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /^Sorties$/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /Attribution des places/i }),
    ).toBeInTheDocument();
    expect(screen.getByText('Aucune sortie')).toBeInTheDocument();
    expect(screen.getByText('Aucune attribution')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Ajouter une sortie/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Ajouter une attribution/i }),
    ).toBeInTheDocument();

    expect(
      screen.queryByRole('button', { name: /Autres actions/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Renommer la phase/i }),
    ).toBeInTheDocument();
    const remove = screen.getByRole('button', {
      name: 'Supprimer Demi-finales',
    });
    expect(remove.closest('.structure-topology__tile')).not.toBeNull();
    expect(remove).toBeEnabled();
    await user.click(remove);
    const dialog = await screen.findByRole('dialog', {
      name: /Supprimer la phase/i,
    });
    expect(dialog).toHaveTextContent(/disparaîtra de la compétition/i);
    expect(dialog).toHaveTextContent(/structure et ses équipes/i);
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
            actions: [
              'ReplaceQualificationRules',
              'ReplaceAffectationAuthoring',
            ],
          }),
        ],
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'League' }),
    ).toBeInTheDocument();
    expect(screen.getByText(compositionBandEmptyHint)).toBeInTheDocument();
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
            actions: ['ReplaceProgressionRules', 'ReplacePlacementAwardRules'],
            affectationPreviewNames: ['Alpha', 'Beta'],
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
                sourcePairKey: 'P1',
                sourceLabel: 'Finale · #1',
              },
              {
                rank: 2,
                outcome: 'Loser',
                sourcePairKey: 'P1',
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

    expect(
      await screen.findByRole('heading', { name: 'Finale' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /Équipes/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: /^Sorties$/i }),
    ).toBeInTheDocument();
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

  it('renames the phase inline from the fiche pencil', async () => {
    const user = userEvent.setup();
    vi.mocked(renameStage).mockResolvedValue(undefined);
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
            actions: ['RenameStage', 'RemoveStage'],
          }),
          championshipStage({
            stageId: avalStageId,
            name: 'Other',
            actions: ['RenameStage'],
          }),
        ],
      }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'League' }),
    ).toBeInTheDocument();
    await user.click(
      screen.getByRole('button', { name: /Renommer la phase/i }),
    );
    const input = await screen.findByRole('textbox', {
      name: /Nom de la phase/i,
    });
    await user.clear(input);
    await user.type(input, 'Saison');
    await user.click(screen.getByRole('button', { name: /^Enregistrer$/i }));

    await waitFor(() => {
      expect(renameStage).toHaveBeenCalledWith(stageId, 'Saison');
    });
  });

  describe('integration — cross-surface (PageHead, fiche, strip, Matchs)', () => {
    it('aligns negative PageHead with population shortfall and Prepare blocked (Places)', async () => {
      vi.mocked(fetchStructureView).mockResolvedValue(
        structureView({
          format: {
            kind: 'Groups',
            primaryStageId: groupesStage().stageId,
            primaryStageName: 'Groupes',
            primaryStageStatus: 'Draft',
          },
          stages: [
            groupesStage({
              compositionCapacity: 8,
              affectationEntryCount: 4,
              structureIssues: [],
              lifecycle: {
                transition: {
                  code: 'PrepareStage',
                  enabled: false,
                  disabledReasonCode: 'Places',
                },
              },
            }),
          ],
          readiness: {
            readyForNextSlice: false,
            readyForDraw: false,
            readyForMaterialization: false,
            readyForSchedule: false,
            readyForMatchOperation: false,
            readyForSchedulePath: false,
            attachedMatchCount: 0,
            blockers: [],
            blockerDetails: [],
            nextConstructionStepHint: 'None',
          },
        }),
      );
      vi.mocked(fetchStageSchematic).mockResolvedValue({
        stageId: groupesStage().stageId,
        competitionId,
        name: 'Groupes',
        status: 'Draft',
        formatKind: 'Groups',
        cases: [],
        connections: [],
      });

      renderStructurePage();

      expect(
        await screen.findByRole('button', {
          name: /marquer comme prête/i,
        }),
      ).toBeDisabled();

      expect(
        document.querySelector('.structure-operational-strip'),
      ).toBeInTheDocument();
      expect(
        document.querySelector('.structure-schematic-viewport'),
      ).toBeInTheDocument();
    });

    it('shows negative PageHead when Ready phase has danger-level population shortfall', async () => {
      vi.mocked(fetchStructureView).mockResolvedValue(
        structureView({
          stages: [
            championshipStage({
              status: 'Ready',
              compositionCapacity: 8,
              affectationEntryCount: 0,
            }),
          ],
          readiness: {
            readyForNextSlice: false,
            readyForDraw: false,
            readyForMaterialization: false,
            readyForSchedule: false,
            readyForMatchOperation: false,
            readyForSchedulePath: false,
            attachedMatchCount: 0,
            blockers: [],
            blockerDetails: [],
            nextConstructionStepHint: 'None',
          },
        }),
      );

      renderStructurePage();

      expect(
        await screen.findByText(/Anomalies à traiter/i),
      ).toBeInTheDocument();
      expect(
        screen.queryByText(/Prête pour les matchs/i),
      ).not.toBeInTheDocument();
    });

    it('blocks Prepare with Attribution copy when placement paths are structurally unreachable', async () => {
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
              structureIssues: ['AuthoredAwardUnreachable'],
              lifecycle: {
                transition: {
                  code: 'PrepareStage',
                  enabled: false,
                  disabledReasonCode: 'Attribution',
                  remediation: {
                    href: `/competitions/${competitionId}/structure?stage=${stageId}&focus=rail-attribution`,
                    labelKey: 'Attribution',
                  },
                },
              },
            }),
          ],
        }),
      );
      vi.mocked(fetchStageSchematic).mockResolvedValue({
        stageId,
        competitionId,
        name: 'Finale',
        status: 'Draft',
        formatKind: 'Cup',
        cases: [],
        connections: [],
      });

      renderStructurePage();

      await screen.findByRole('heading', { name: 'Finale' });
      const ficheHeader = document.querySelector(
        '.structure-fiche__header',
      ) as HTMLElement;
      expect(
        within(ficheHeader).getByRole('button', {
          name: /marquer comme prête/i,
        }),
      ).toBeDisabled();
    });

    it('silences SlotFeedMissing on fiche while draw is engaged but keeps other diagnostics', async () => {
      vi.mocked(fetchStructureView).mockResolvedValue(
        structureView({
          format: {
            kind: 'Groups',
            primaryStageId: groupesStage().stageId,
            primaryStageName: 'Groupes',
            primaryStageStatus: 'Draft',
          },
          stages: [
            groupesStage({
              compositionCapacity: 8,
              affectationEntryCount: 8,
              drawExecutionBadge: 'InProgress',
              structureIssues: ['SlotFeedMissing', 'CycleDetected'],
            }),
          ],
        }),
      );
      vi.mocked(fetchStageSchematic).mockResolvedValue({
        stageId: groupesStage().stageId,
        competitionId,
        name: 'Groupes',
        status: 'Draft',
        formatKind: 'Groups',
        cases: [],
        connections: [],
      });

      renderStructurePage();

      await screen.findByRole('heading', { name: 'Groupes' });
      expect(
        screen.queryByText(/Place pas encore alimentée/i),
      ).not.toBeInTheDocument();
      expect(
        screen.getByText(/Liaison circulaire entre phases/i),
      ).toBeInTheDocument();
      expect(
        await screen.findByText(/Anomalies à traiter/i),
      ).toBeInTheDocument();
    });
  });
});
