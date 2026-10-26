import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  addCompetitionStage,
  fetchNeedsAttention,
  fetchStageOverview,
  fetchStageSchematic,
  fetchStructureView,
  insertCompetitionStage,
  setCompetitionStageOrder,
  ApiError,
} from '../../api';
import { notify } from '../../design-system/toastStore';
import type { StructureStageHubSummary, StructureView } from '../../types';
import { StructurePage } from './StructurePage';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchStructureView: vi.fn(),
    fetchNeedsAttention: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchStageSchematic: vi.fn(),
    addCompetitionStage: vi.fn(),
    insertCompetitionStage: vi.fn(),
    setCompetitionStageOrder: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const leagueId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const barrageId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';

function stage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: leagueId,
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

function view(overrides: Partial<StructureView> = {}): StructureView {
  return {
    competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    participants: {
      activeCount: 1,
      occupyingCount: 1,
      entries: [],
    },
    format: {
      kind: 'Championship',
      primaryStageId: leagueId,
      primaryStageName: 'League',
      primaryStageStatus: 'Draft',
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
    actions: ['AddCompetitionStage'],
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
    stages: [stage(), stage({ stageId: barrageId, name: 'Barrages' })],
    ...overrides,
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[`/competitions/${competitionId}/structure`]}
      >
        <Routes>
          <Route
            path="/competitions/:competitionId/structure"
            element={<StructurePage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function tileNames() {
  return screen
    .getAllByRole('button')
    .filter((button) => button.classList.contains('structure-topology__select'))
    .map((button) => button.textContent ?? '');
}

describe('Structure phase insert and reorder', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchNeedsAttention).mockResolvedValue({
      competitionId,
      count: 0,
      items: [],
    });
    vi.mocked(fetchStageOverview).mockResolvedValue({
      id: leagueId,
      competitionId,
      name: 'Phase',
      status: 'Draft',
      rounds: [],
      slots: [],
      draws: [],
    });
    vi.mocked(fetchStageSchematic).mockResolvedValue({
      stageId: leagueId,
      competitionId,
      name: 'Phase',
      status: 'Draft',
      formatKind: 'Championship',
      cases: [],
      connections: [],
    });
  });

  it('moves a phase with an explicit control and does not toast success', async () => {
    const user = userEvent.setup();
    const success = vi.spyOn(notify, 'success');
    const initial = view();
    const reordered = view({
      stages: [stage({ stageId: barrageId, name: 'Barrages' }), stage()],
    });
    let current = initial;
    vi.mocked(fetchStructureView).mockImplementation(async () => current);
    vi.mocked(setCompetitionStageOrder).mockImplementation(async () => {
      current = reordered;
      return reordered;
    });

    renderPage();

    const moveUp = await screen.findByRole('button', { name: 'Monter League' });
    expect(moveUp).toBeDisabled();
    const moveDown = screen.getByRole('button', { name: 'Descendre League' });
    expect(moveDown.closest('.structure-topology__tile')).not.toBeNull();
    expect(moveDown.closest('[role="toolbar"]')).toHaveAttribute(
      'aria-orientation',
      'horizontal',
    );
    const insertBefore = screen.getByRole('button', {
      name: 'Insérer une phase avant League',
    });
    expect(insertBefore).not.toHaveTextContent(/Insérer une phase/);
    expect(document.querySelector('.structure-topology__order')).toBeNull();
    expect(
      screen.queryByRole('button', { name: /Ajouter une phase/i }),
    ).not.toBeInTheDocument();
    await user.click(moveDown);

    await waitFor(() => {
      expect(setCompetitionStageOrder).toHaveBeenCalledWith(competitionId, {
        stageIds: [barrageId, leagueId],
      });
    });
    await waitFor(() => {
      expect(tileNames()[0]).toContain('Barrages');
      expect(tileNames()[1]).toContain('League');
    });
    expect(success).not.toHaveBeenCalled();
    expect(document.querySelector('.ds-toast')).toBeNull();
    success.mockRestore();
  });

  it('shows the reorder failure in the list and does not toast', async () => {
    const user = userEvent.setup();
    const success = vi.spyOn(notify, 'success');
    vi.mocked(fetchStructureView).mockResolvedValue(view());
    vi.mocked(setCompetitionStageOrder).mockRejectedValue(
      new ApiError(
        400,
        'Refusé',
        'Structure fermée',
        'Application.StructureNotMutable',
      ),
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Descendre League' }),
    );

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'La structure ne peut plus être modifiée tant que la compétition est en cours / close.',
    );
    expect(tileNames()[0]).toContain('League');
    expect(tileNames()[1]).toContain('Barrages');
    expect(success).not.toHaveBeenCalled();
    expect(document.querySelector('.ds-toast')).toBeNull();
    success.mockRestore();
  });

  it('inserts before an anchor from the leading slot without using append', async () => {
    const user = userEvent.setup();
    const success = vi.spyOn(notify, 'success');
    const createdId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    const initial = view();
    const after = view({
      stages: [
        stage({ stageId: createdId, name: 'Ouverture' }),
        stage(),
        stage({ stageId: barrageId, name: 'Barrages' }),
      ],
    });
    let current = initial;
    vi.mocked(fetchStructureView).mockImplementation(async () => current);
    vi.mocked(insertCompetitionStage).mockImplementation(async () => {
      current = after;
      return { stageId: createdId, name: 'Ouverture', structure: after };
    });

    renderPage();

    await user.click(
      await screen.findByRole('button', {
        name: 'Insérer une phase avant League',
      }),
    );

    const dialog = await screen.findByRole('dialog', {
      name: /Insérer une phase/i,
    });
    expect(within(dialog).getByText(/avant « League »/i)).toBeInTheDocument();
    await user.type(
      within(dialog).getByLabelText(/Nom de la phase/i),
      'Ouverture',
    );
    await user.click(
      within(dialog).getByRole('checkbox', { name: /Championnat/i }),
    );
    await user.click(
      within(dialog).getByRole('button', { name: /Insérer la phase/i }),
    );

    await waitFor(() => {
      expect(insertCompetitionStage).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          format: 'Championship',
          name: 'Ouverture',
          anchorStageId: leagueId,
          position: 'Before',
        }),
      );
    });
    expect(addCompetitionStage).not.toHaveBeenCalled();
    expect(success).not.toHaveBeenCalled();
    expect(document.querySelector('.ds-toast')).toBeNull();
    success.mockRestore();
  });

  it('inserts between phases after the upper anchor', async () => {
    const user = userEvent.setup();
    const createdId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    vi.mocked(fetchStructureView).mockResolvedValue(view());
    vi.mocked(insertCompetitionStage).mockResolvedValue({
      stageId: createdId,
      name: 'Milieu',
      structure: view(),
    });

    renderPage();

    expect(
      await screen.findByRole('button', {
        name: 'Insérer une phase après Barrages',
      }),
    ).toBeInTheDocument();
    await user.click(
      screen.getByRole('button', {
        name: 'Insérer une phase après League',
      }),
    );
    const dialog = await screen.findByRole('dialog', {
      name: /Insérer une phase/i,
    });
    expect(within(dialog).getByText(/après « League »/i)).toBeInTheDocument();
    await user.type(
      within(dialog).getByLabelText(/Nom de la phase/i),
      'Milieu',
    );
    await user.click(
      within(dialog).getByRole('checkbox', { name: /Championnat/i }),
    );
    await user.click(
      within(dialog).getByRole('button', { name: /Insérer la phase/i }),
    );

    await waitFor(() => {
      expect(insertCompetitionStage).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          format: 'Championship',
          name: 'Milieu',
          anchorStageId: leagueId,
          position: 'After',
        }),
      );
    });
    expect(addCompetitionStage).not.toHaveBeenCalled();
    expect(screen.queryByRole('menuitem')).not.toBeInTheDocument();
  });

  it('keeps append only when the phase list is empty', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      view({
        stages: [],
        format: {
          kind: null,
          primaryStageId: null,
          primaryStageName: null,
          primaryStageStatus: null,
        },
      }),
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: /Ajouter une phase/i }),
    );
    expect(
      await screen.findByRole('dialog', { name: /Nouvelle phase/i }),
    ).toBeInTheDocument();
    expect(insertCompetitionStage).not.toHaveBeenCalled();
    expect(
      screen.queryByRole('button', { name: /Insérer une phase/i }),
    ).not.toBeInTheDocument();
  });

  it('offers append and an insert menu when there is a single phase', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      view({ stages: [stage()] }),
    );

    renderPage();

    expect(
      await screen.findByRole('button', { name: /Ajouter une phase/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('region', { name: /Topologie/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Insérer une phase avant League' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Monter League' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Descendre League' }),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Insérer une phase' }));
    expect(
      await screen.findByRole('menuitem', {
        name: 'Insérer une phase avant League',
      }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('menuitem', {
        name: 'Insérer une phase après League',
      }),
    ).not.toBeInTheDocument();
  });

  it('hides insert, move, and add when the competition is running', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      view({
        status: 'Running',
        actions: ['WithdrawEntry', 'RenameEntry'],
      }),
    );

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Topologie' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Ajouter une phase/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Monter League' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Descendre League' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Insérer une phase/i }),
    ).not.toBeInTheDocument();
  });

  it('keeps insert and move controls when the competition is Ready', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(view({ status: 'Ready' }));

    renderPage();

    expect(
      await screen.findByRole('button', { name: 'Monter League' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Insérer une phase avant League' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Ajouter une phase/i }),
    ).not.toBeInTheDocument();
  });

  it('warns without blocking when the insert splits a qualification link', async () => {
    const user = userEvent.setup();
    const createdId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
    vi.mocked(fetchStructureView).mockResolvedValue(
      view({
        stages: [
          stage({
            qualificationIntents: [
              {
                intentId: 'q1',
                order: 1,
                sourceKind: 'Overall',
                positionFrom: 1,
                positionTo: 1,
                destinationStageId: barrageId,
              },
            ],
          }),
          stage({ stageId: barrageId, name: 'Barrages' }),
        ],
      }),
    );
    vi.mocked(insertCompetitionStage).mockResolvedValue({
      stageId: createdId,
      name: 'Milieu',
      structure: view(),
    });

    renderPage();

    await user.click(
      await screen.findByRole('button', {
        name: 'Insérer une phase après League',
      }),
    );
    const dialog = await screen.findByRole('dialog', {
      name: /Insérer une phase/i,
    });
    expect(
      within(dialog).getByText(
        /entre des phases déjà reliées par une qualification ou une progression/i,
      ),
    ).toBeInTheDocument();
    await user.type(
      within(dialog).getByLabelText(/Nom de la phase/i),
      'Milieu',
    );
    await user.click(
      within(dialog).getByRole('checkbox', { name: /Championnat/i }),
    );
    const submit = within(dialog).getByRole('button', {
      name: /Insérer la phase/i,
    });
    expect(submit).toBeEnabled();
    await user.click(submit);

    await waitFor(() => {
      expect(insertCompetitionStage).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          anchorStageId: leagueId,
          position: 'After',
          name: 'Milieu',
        }),
      );
    });
  });

  it('does not warn when the insert leaves qualification links intact', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      view({
        stages: [
          stage({
            qualificationIntents: [
              {
                intentId: 'q1',
                order: 1,
                sourceKind: 'Overall',
                positionFrom: 1,
                positionTo: 1,
                destinationStageId: barrageId,
              },
            ],
          }),
          stage({ stageId: barrageId, name: 'Barrages' }),
        ],
      }),
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', {
        name: 'Insérer une phase après Barrages',
      }),
    );
    const dialog = await screen.findByRole('dialog', {
      name: /Insérer une phase/i,
    });
    expect(within(dialog).queryByText(/déjà reliées/i)).not.toBeInTheDocument();
  });
});
