import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  applyDraw,
  fetchCompetitionDetail,
  fetchStageOverview,
  fetchStageSchematic,
  materializeCupFromOccupiedSlots,
  prepareStage,
  publishAndApplyDraw,
  startStage,
} from '../api';
import type {
  StageDraw,
  StageOverview,
  StageSchematic,
  StageSlot,
} from '../types';
import { getDrawUiProjection, isSlotDrawApplied } from './drawUi';
import { StagePage } from './StagePage';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    fetchStageSchematic: vi.fn(),
    fetchCompetitionDetail: vi.fn(),
    prepareStage: vi.fn(),
    startStage: vi.fn(),
    publishAndApplyDraw: vi.fn(),
    applyDraw: vi.fn(),
    materializeCupFromOccupiedSlots: vi.fn(),
  };
});

const stageId = '22222222-2222-2222-2222-222222222222';
const competitionId = '33333333-3333-3333-3333-333333333333';
const slotDrawId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
const fixtureId = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
const entryA = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const entryB = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

function baseOverview(overrides: Partial<StageOverview> = {}): StageOverview {
  return {
    id: stageId,
    competitionId,
    name: 'QF',
    status: 'Draft',
    rounds: [],
    slots: [],
    draws: [],
    bracketPairs: [],
    ...overrides,
  };
}

function baseSchematic(overrides?: Partial<StageSchematic>): StageSchematic {
  return {
    stageId,
    competitionId,
    name: 'QF',
    status: 'Draft',
    formatKind: 'Cup',
    cases: [],
    connections: [],
    ...overrides,
  };
}

function slotDraw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: slotDrawId,
    kind: 'Slot',
    status: 'Published',
    resolutionState: 'Resolved',
    slotPlacements: [
      {
        slotKey: 'SF1-A',
        entryId: entryA,
        displayName: 'Alpha',
      },
    ],
    ...overrides,
  };
}

async function confirmApplyInDialog(user: ReturnType<typeof userEvent.setup>) {
  const dialog = await screen.findByRole('dialog', {
    name: 'Appliquer le tirage ?',
  });
  await user.click(within(dialog).getByRole('button', { name: 'Appliquer' }));
}

function renderStagePage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/stages/${stageId}`]}>
        <Routes>
          <Route path="/stages/:stageId" element={<StagePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('isSlotDrawApplied', () => {
  const slotsOccupied: StageSlot[] = [
    {
      slotKey: 'SF1-A',
      entryId: entryA,
      displayName: 'Alpha',
      coveredByCompleteFixture: false,
    },
  ];
  const slotsEmpty: StageSlot[] = [
    {
      slotKey: 'SF1-A',
      entryId: null,
      displayName: null,
      coveredByCompleteFixture: false,
    },
  ];
  const slotsWrong: StageSlot[] = [
    {
      slotKey: 'SF1-A',
      entryId: entryB,
      displayName: 'Beta',
      coveredByCompleteFixture: false,
    },
  ];

  it('is true when every placement matches the stage slot occupant', () => {
    expect(isSlotDrawApplied(slotDraw(), slotsOccupied)).toBe(true);
  });

  it('is false when a target slot is empty', () => {
    expect(isSlotDrawApplied(slotDraw(), slotsEmpty)).toBe(false);
  });

  it('is false when a slot has a different entry', () => {
    expect(isSlotDrawApplied(slotDraw(), slotsWrong)).toBe(false);
  });
});

describe('getDrawUiProjection', () => {
  it('describes draft + not resolved as generation interrupted', () => {
    const ui = getDrawUiProjection(
      slotDraw({
        status: 'Draft',
        resolutionState: 'NotResolved',
        slotPlacements: [],
      }),
      [],
    );
    expect(ui.messageKey).toBe('generationInterrupted');
    expect(ui.showResults).toBe(false);
  });

  it('describes draft + resolved as not published', () => {
    const ui = getDrawUiProjection(slotDraw({ status: 'Draft' }), []);
    expect(ui.messageKey).toBe('draftResolved');
    expect(ui.showResults).toBe(true);
  });
});

describe('StagePage prepare', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchCompetitionDetail).mockResolvedValue({
      id: competitionId,
      name: 'Dev Seed Cup',
      status: 'Draft',
      entries: [],
      stages: [],
    });
    vi.mocked(prepareStage).mockResolvedValue(undefined);
    vi.mocked(startStage).mockResolvedValue(undefined);
    vi.mocked(publishAndApplyDraw).mockResolvedValue(undefined);
    vi.mocked(applyDraw).mockResolvedValue(undefined);
  });

  it('shows Prepare stage when status is Draft', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Draft' }),
    );

    renderStagePage();

    expect(
      await screen.findByRole('button', { name: 'Préparer la phase' }),
    ).toBeEnabled();
    expect(screen.getByText('Brouillon')).toBeInTheDocument();
  });

  it('hides Prepare stage when status is Ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Ready' }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(screen.getByText('Prêt')).toBeInTheDocument();
    });
    expect(
      screen.queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('hides Prepare stage when status is Running', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Running' }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(screen.getByText('En cours')).toBeInTheDocument();
    });
    expect(
      screen.queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('Prepare calls prepareStage and shows Ready after refetch', async () => {
    const user = userEvent.setup();
    let prepared = false;

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({ status: prepared ? 'Ready' : 'Draft' }),
    );
    vi.mocked(prepareStage).mockImplementation(async () => {
      prepared = true;
    });

    renderStagePage();
    await user.click(
      await screen.findByRole('button', { name: 'Préparer la phase' }),
    );

    await waitFor(() => {
      expect(prepareStage).toHaveBeenCalledWith(stageId);
      expect(screen.getByText('Prêt')).toBeInTheDocument();
    });
    expect(
      screen.queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('disables Prepare while pending', async () => {
    const user = userEvent.setup();
    let resolvePrepare!: () => void;
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Draft' }),
    );
    vi.mocked(prepareStage).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolvePrepare = () => resolve(undefined);
        }),
    );

    renderStagePage();
    await user.click(
      await screen.findByRole('button', { name: 'Préparer la phase' }),
    );

    expect(
      await screen.findByRole('button', { name: 'Préparation…' }),
    ).toBeDisabled();

    resolvePrepare();
    await waitFor(() => {
      expect(prepareStage).toHaveBeenCalledWith(stageId);
    });
  });

  it('shows Prepare error and keeps Draft with button usable', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Draft' }),
    );
    vi.mocked(prepareStage).mockRejectedValue(new Error('Prepare blocked'));

    renderStagePage();
    await user.click(
      await screen.findByRole('button', { name: 'Préparer la phase' }),
    );

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Prepare blocked',
    );
    expect(screen.getByText('Brouillon')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Préparer la phase' }),
    ).toBeEnabled();
  });
});

describe('StagePage start', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchCompetitionDetail).mockResolvedValue({
      id: competitionId,
      name: 'Dev Seed Cup',
      status: 'Draft',
      entries: [],
      stages: [],
    });
    vi.mocked(prepareStage).mockResolvedValue(undefined);
    vi.mocked(startStage).mockResolvedValue(undefined);
    vi.mocked(publishAndApplyDraw).mockResolvedValue(undefined);
    vi.mocked(applyDraw).mockResolvedValue(undefined);
  });

  it('shows Start stage when status is Ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Ready' }),
    );

    renderStagePage();

    expect(
      await screen.findByRole('button', { name: 'Démarrer la phase' }),
    ).toBeEnabled();
    expect(screen.getByText('Prêt')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Préparer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('hides Start stage when status is Draft', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Draft' }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(screen.getByText('Brouillon')).toBeInTheDocument();
    });
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('hides Start stage when status is Running', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Running' }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(screen.getByText('En cours')).toBeInTheDocument();
    });
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('hides Start stage when status is Completed', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Completed' }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(screen.getByText('Terminé')).toBeInTheDocument();
    });
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('Start calls startStage and shows Running after refetch', async () => {
    const user = userEvent.setup();
    let started = false;

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({ status: started ? 'Running' : 'Ready' }),
    );
    vi.mocked(startStage).mockImplementation(async () => {
      started = true;
    });

    renderStagePage();
    await user.click(
      await screen.findByRole('button', { name: 'Démarrer la phase' }),
    );

    await waitFor(() => {
      expect(startStage).toHaveBeenCalledWith(stageId);
      expect(screen.getByText('En cours')).toBeInTheDocument();
    });
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('disables Start while pending', async () => {
    const user = userEvent.setup();
    let resolveStart!: () => void;
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Ready' }),
    );
    vi.mocked(startStage).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveStart = () => resolve(undefined);
        }),
    );

    renderStagePage();
    await user.click(
      await screen.findByRole('button', { name: 'Démarrer la phase' }),
    );

    expect(
      await screen.findByRole('button', { name: 'Démarrage…' }),
    ).toBeDisabled();

    resolveStart();
    await waitFor(() => {
      expect(startStage).toHaveBeenCalledWith(stageId);
    });
  });

  it('shows Start error and keeps Ready with button usable', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ status: 'Ready' }),
    );
    vi.mocked(startStage).mockRejectedValue(new Error('Start blocked'));

    renderStagePage();
    await user.click(
      await screen.findByRole('button', { name: 'Démarrer la phase' }),
    );

    expect(await screen.findByRole('alert')).toHaveTextContent('Start blocked');
    expect(screen.getByText('Prêt')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Démarrer la phase' }),
    ).toBeEnabled();
  });
});

describe('StagePage draws', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchCompetitionDetail).mockResolvedValue({
      id: competitionId,
      name: 'Dev Seed Cup',
      status: 'Draft',
      entries: [],
      stages: [],
    });
    vi.mocked(prepareStage).mockResolvedValue(undefined);
    vi.mocked(startStage).mockResolvedValue(undefined);
    vi.mocked(publishAndApplyDraw).mockResolvedValue(undefined);
    vi.mocked(applyDraw).mockResolvedValue(undefined);
    vi.mocked(fetchStageSchematic).mockResolvedValue(baseSchematic());
    vi.mocked(materializeCupFromOccupiedSlots).mockResolvedValue({
      createdCount: 0,
      attachedMatchIds: [],
      alreadyComplete: false,
    });
  });

  it('shows Slot result for draft + resolved without Apply', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ draws: [slotDraw({ status: 'Draft' })] }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(
        screen.getByText('Résultat généré, prêt à être publié.'),
      ).toBeInTheDocument();
    });
    expect(
      screen.getByRole('heading', { name: /Tirage Emplacement/i }),
    ).toBeInTheDocument();
    expect(screen.getByText('Résolu')).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Résultat' }),
    ).toBeInTheDocument();
    expect(screen.getAllByText('SF1-A').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Alpha').length).toBeGreaterThanOrEqual(1);
    expect(
      screen.getByRole('button', { name: 'Publier et appliquer' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Appliquer' }),
    ).not.toBeInTheDocument();
  });

  it('hides Publish and Apply for draft + not resolved', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [
          slotDraw({
            status: 'Draft',
            resolutionState: 'NotResolved',
            slotPlacements: [],
          }),
        ],
      }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(screen.getByText('Génération interrompue')).toBeInTheDocument();
    });
    expect(screen.queryByText('Placements')).not.toBeInTheDocument();
    expect(screen.queryByText('Alpha')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Publier et appliquer/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Appliquer' }),
    ).not.toBeInTheDocument();
  });

  it('shows No solution without Publish or Apply', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [
          slotDraw({
            status: 'Draft',
            resolutionState: 'NoSolution',
            slotPlacements: [],
          }),
        ],
      }),
    );

    renderStagePage();

    expect(
      await screen.findByText('Aucune résolution valide n’a pu être générée.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Aucune solution')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Publier et appliquer/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Appliquer' }),
    ).not.toBeInTheDocument();
  });

  it('shows Cancelled without Publish or Apply', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        draws: [
          slotDraw({
            status: 'Cancelled',
            resolutionState: 'Resolved',
          }),
        ],
      }),
    );

    renderStagePage();

    expect(
      await screen.findByText(
        'Exécution annulée. Un nouveau tirage est nécessaire pour recommencer.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByText('Annulé')).toBeInTheDocument();
    expect(screen.getAllByText('Alpha').length).toBeGreaterThanOrEqual(1);
    expect(
      screen.queryByRole('button', { name: /Publier et appliquer/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Appliquer' }),
    ).not.toBeInTheDocument();
  });

  it('shows Published with Apply when not yet applied', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw()],
      }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(
        screen.getByText('Résultat publié, en attente d’application.'),
      ).toBeInTheDocument();
    });
    expect(screen.getByText('Publié')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Publier et appliquer/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Appliquer' }),
    ).toBeInTheDocument();
  });

  it('shows derived Applied when slot placements match stage slots', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: entryA,
            displayName: 'Alpha',
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw()],
      }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(screen.getByText('Appliqué')).toBeInTheDocument();
    });
    expect(
      screen.getByText('Résultat appliqué à la phase.'),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Résultat' }),
    ).toBeInTheDocument();
    expect(screen.getAllByText('SF1-A').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Alpha').length).toBeGreaterThanOrEqual(1);
    expect(
      screen.queryByRole('button', { name: 'Appliquer' }),
    ).not.toBeInTheDocument();
  });

  it('does not show Applied when slot occupants do not match', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw()],
      }),
    );

    renderStagePage();

    await waitFor(() => {
      expect(
        screen.getByText('Résultat publié, en attente d’application.'),
      ).toBeInTheDocument();
    });
    expect(screen.queryByText('Appliqué')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Appliquer' }),
    ).toBeInTheDocument();
  });

  it('Publish calls publishAndApplyDraw and shows Published after refetch', async () => {
    const user = userEvent.setup();
    let published = false;

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw({ status: published ? 'Published' : 'Draft' })],
      }),
    );
    vi.mocked(publishAndApplyDraw).mockImplementation(async () => {
      published = true;
    });

    renderStagePage();
    const publishButton = await screen.findByRole('button', {
      name: 'Publier et appliquer',
    });
    await user.click(publishButton);

    await waitFor(() => {
      expect(publishAndApplyDraw).toHaveBeenCalledWith(stageId, slotDrawId);
      expect(screen.getByText('Publié')).toBeInTheDocument();
      expect(
        screen.getByText('Résultat publié, en attente d’application.'),
      ).toBeInTheDocument();
    });
  });

  it('disables Publish while pending', async () => {
    const user = userEvent.setup();
    let resolvePublish!: () => void;
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ draws: [slotDraw({ status: 'Draft' })] }),
    );
    vi.mocked(publishAndApplyDraw).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolvePublish = () => resolve(undefined);
        }),
    );

    renderStagePage();
    const publishButton = await screen.findByRole('button', {
      name: 'Publier et appliquer',
    });
    await user.click(publishButton);

    expect(
      await screen.findByRole('button', {
        name: 'Publication et application…',
      }),
    ).toBeDisabled();

    resolvePublish();
    await waitFor(() => {
      expect(publishAndApplyDraw).toHaveBeenCalled();
    });
  });

  it('shows Publish error message', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({ draws: [slotDraw({ status: 'Draft' })] }),
    );
    vi.mocked(publishAndApplyDraw).mockRejectedValue(
      new Error('Publish blocked'),
    );

    renderStagePage();
    await user.click(
      await screen.findByRole('button', { name: 'Publier et appliquer' }),
    );

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Publish blocked',
    );
  });

  it('Apply Slot confirms then posts apply and shows Applied', async () => {
    const user = userEvent.setup();
    let applied = false;

    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: applied ? entryA : null,
            displayName: applied ? 'Alpha' : null,
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw()],
      }),
    );
    vi.mocked(applyDraw).mockImplementation(async () => {
      applied = true;
    });

    renderStagePage();
    await user.click(await screen.findByRole('button', { name: 'Appliquer' }));
    await confirmApplyInDialog(user);

    await waitFor(() => {
      expect(applyDraw).toHaveBeenCalledWith(stageId, slotDrawId);
      expect(screen.getByText('Appliqué')).toBeInTheDocument();
    });
  });

  it('disables Apply while pending', async () => {
    const user = userEvent.setup();
    let resolveApply!: () => void;

    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw()],
      }),
    );
    vi.mocked(applyDraw).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveApply = () => resolve(undefined);
        }),
    );

    renderStagePage();
    await user.click(await screen.findByRole('button', { name: 'Appliquer' }));
    await confirmApplyInDialog(user);

    const confirmDialog = await screen.findByRole('dialog', {
      name: 'Appliquer le tirage ?',
    });
    expect(
      within(confirmDialog).getByRole('button', { name: 'Application…' }),
    ).toBeDisabled();

    resolveApply();
    await waitFor(() => {
      expect(applyDraw).toHaveBeenCalled();
    });
  });

  it('shows Apply error message', async () => {
    const user = userEvent.setup();

    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw()],
      }),
    );
    vi.mocked(applyDraw).mockRejectedValue(new Error('Apply blocked'));

    renderStagePage();
    await user.click(await screen.findByRole('button', { name: 'Appliquer' }));
    await confirmApplyInDialog(user);

    expect(await screen.findAllByText('Apply blocked')).not.toHaveLength(0);
  });

  it('cancelling Apply confirmation does not call applyDraw', async () => {
    const user = userEvent.setup();

    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
        draws: [slotDraw()],
      }),
    );

    renderStagePage();
    await user.click(await screen.findByRole('button', { name: 'Appliquer' }));

    const dialog = await screen.findByRole('dialog', {
      name: 'Appliquer le tirage ?',
    });
    await user.click(within(dialog).getByRole('button', { name: 'Annuler' }));

    expect(applyDraw).not.toHaveBeenCalled();
  });

  it('Confrontations lists eligible BracketPairs and posts pairKeys', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        status: 'Draft',
        rounds: [
          {
            id: 'r1',
            name: 'SF',
            fixtures: [
              {
                id: fixtureId,
                slotAKey: 'SF1-A',
                slotBKey: 'SF1-B',
                attachments: [{ matchId: 'm1', legIndex: 1 }],
              },
            ],
          },
        ],
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: entryA,
            displayName: 'Alpha',
            coveredByCompleteFixture: true,
          },
          {
            slotKey: 'SF1-B',
            entryId: entryB,
            displayName: 'Beta',
            coveredByCompleteFixture: true,
          },
          {
            slotKey: 'SF2-A',
            entryId: entryA,
            displayName: 'Alpha',
            coveredByCompleteFixture: false,
          },
          {
            slotKey: 'SF2-B',
            entryId: entryB,
            displayName: 'Beta',
            coveredByCompleteFixture: false,
          },
        ],
      }),
    );
    vi.mocked(fetchStageSchematic).mockResolvedValue(
      baseSchematic({
        connections: [
          {
            fixtureId,
            roundOrder: 0,
            slotAKey: 'SF1-A',
            slotBKey: 'SF1-B',
            matchNumber: 1,
            pairKey: 'P1',
          },
          {
            fixtureId: null,
            roundOrder: 0,
            slotAKey: 'SF2-A',
            slotBKey: 'SF2-B',
            matchNumber: 0,
            pairKey: 'P2',
          },
        ],
      }),
    );
    vi.mocked(materializeCupFromOccupiedSlots).mockResolvedValue({
      createdCount: 1,
      attachedMatchIds: ['m2'],
      alreadyComplete: false,
    });

    renderStagePage();

    expect(
      await screen.findByRole('heading', { name: 'Confrontations' }),
    ).toBeInTheDocument();
    expect(
      await screen.findByText(/1 paire\(s\) déjà matérialisée\(s\)/i),
    ).toBeInTheDocument();
    expect(screen.getByText(/déjà générée/i)).toBeInTheDocument();
    expect(screen.getByText(/SF2-A ↔ SF2-B/)).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', {
        name: /Générer les confrontations \(1\)/i,
      }),
    );

    await waitFor(() => {
      expect(materializeCupFromOccupiedSlots).toHaveBeenCalledWith(stageId, [
        'P2',
      ]);
    });
  });

  it('Confrontations shows all-covered when no eligible pairs remain', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      baseOverview({
        status: 'Draft',
        rounds: [{ id: 'r1', name: 'SF', fixtures: [] }],
        slots: [
          {
            slotKey: 'SF1-A',
            entryId: entryA,
            displayName: 'Alpha',
            coveredByCompleteFixture: true,
          },
          {
            slotKey: 'SF1-B',
            entryId: entryB,
            displayName: 'Beta',
            coveredByCompleteFixture: true,
          },
        ],
      }),
    );
    vi.mocked(fetchStageSchematic).mockResolvedValue(
      baseSchematic({
        connections: [
          {
            fixtureId,
            roundOrder: 0,
            slotAKey: 'SF1-A',
            slotBKey: 'SF1-B',
            matchNumber: 1,
            pairKey: 'P1',
          },
        ],
      }),
    );

    renderStagePage();

    expect(
      await screen.findByText(
        /Toutes les confrontations possibles sont déjà générées/i,
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Générer les confrontations/i }),
    ).not.toBeInTheDocument();
  });
});
