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
  configureOrganisationStructure,
  fetchOrganisationView,
  ApiError,
} from '../api';
import type {
  OrganisationStageHubSummary,
  OrganisationView,
} from '../types';
import { StructurePage } from './StructurePage';
import { relevantSwitcherSections } from './structureHubSections';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchOrganisationView: vi.fn(),
    configureOrganisationStructure: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const entryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

function championshipStage(
  overrides: Partial<OrganisationStageHubSummary> = {},
): OrganisationStageHubSummary {
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
  overrides: Partial<OrganisationStageHubSummary> = {},
): OrganisationStageHubSummary {
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

describe('relevantSwitcherSections', () => {
  it('omits absent sections instead of disabling them', () => {
    expect(relevantSwitcherSections(championshipStage())).toEqual([
      'construction',
    ]);
  });

  it('includes qualification, progression and tirage when present', () => {
    expect(relevantSwitcherSections(groupesStage())).toEqual([
      'construction',
      'qualification',
      'progression',
      'tirage',
    ]);
  });

  it('includes relation sections when per-phase edit actions are offered', () => {
    expect(
      relevantSwitcherSections(
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
    vi.mocked(configureOrganisationStructure).mockResolvedValue({
      stageCreated: true,
      rebuildImpact: null,
      organisation: organisationView({
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

  it('shows loading while organisation is pending', () => {
    vi.mocked(fetchOrganisationView).mockReturnValue(new Promise(() => {}));

    renderStructurePage();

    expect(screen.getByRole('status')).toHaveTextContent('Chargement…');
  });

  it('renders Structure hub without identity / teams / regulation panels', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView());

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Structure' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Topologie' }),
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

  it('shows master-detail for a championship stage without progression chrome', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
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
    expect(screen.getByText(/Matchs : Suit le cadre/i)).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Qualification/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Progression/i }),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: '2 équipes' }));

    expect(
      await screen.findByRole('heading', { name: 'Construction' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Aller-retour \(double RR\)/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('tablist', { name: /Sections de la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('supports overview drill-in and contextual section switcher', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
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
    await user.click(
      screen.getByRole('button', { name: '2 groupes · 8 équipes' }),
    );

    expect(
      await screen.findByRole('tablist', { name: /Sections de la phase/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Construction' })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    expect(screen.getByRole('tab', { name: 'Qualification' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Progression' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Tirage' })).toBeInTheDocument();
    expect(screen.queryByRole('tab', { name: 'Matchs' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('tab', { name: 'Qualification' }));
    expect(
      await screen.findByText(/2 chemins de qualification/i),
    ).toBeInTheDocument();
  });

  it('shows an error when organisation read fails', async () => {
    vi.mocked(fetchOrganisationView).mockRejectedValue(
      new ApiError(404, 'Competition was not found.'),
    );

    renderStructurePage();

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Introuvable. Vérifiez l'identifiant dans l'URL.",
    );
  });

  it('hydrates drill-in from Structure deep-link query params', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
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
      await screen.findByText(/Les règles de match suivent le cadre/i),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Personnaliser les matchs/i }),
    ).toBeInTheDocument();
  });

  it('configures championship structure via dialog', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchOrganisationView).mockResolvedValue(organisationView());

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
      expect(configureOrganisationStructure).toHaveBeenCalledWith(
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
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
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
  });

  it('hides configure when Host actions omit it', async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({ actions: [] }),
    );

    renderStructurePage();

    expect(
      await screen.findByRole('heading', { name: 'Structure' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Créer la structure/i }),
    ).not.toBeInTheDocument();
  });

  it("shows materialize readiness CTA for a ready Championship", async () => {
    vi.mocked(fetchOrganisationView).mockResolvedValue(
      organisationView({
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
      screen.getByRole('link', {
        name: /Aller à la Vue d’ensemble pour matérialiser/i,
      }),
    ).toHaveAttribute('href', `/competitions/${competitionId}`);
  });
});
