import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  applyDraw,
  createAndGenerateDraw,
  fetchStageOverview,
  publishAndApplyDraw,
} from '../api';
import { clearToasts, getToastsSnapshot } from '../design-system/toastStore';
import type {
  CompetitionStatus,
  StageDraw,
  StageOverview,
  StructureStageHubSummary,
} from '../types';
import { StructureDrawDialog } from './StructureDrawDialog';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    createAndGenerateDraw: vi.fn(),
    publishAndApplyDraw: vi.fn(),
    cancelDraw: vi.fn(),
    applyDraw: vi.fn(),
  };
});

const stageId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function groupsStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId,
    name: 'Groupes',
    status: 'Draft',
    teamCount: 8,
    matchCount: 0,
    groupCount: 2,
    roundCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: true,
    hasDrawRules: true,
    numberOfPots: 4,
    compositionEntryCount: 8,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
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

function overview(
  draws: StageOverview['draws'],
  overrides: Partial<StageOverview> = {},
): StageOverview {
  return {
    id: stageId,
    competitionId,
    name: 'Groupes',
    status: 'Draft',
    slots: [],
    rounds: [],
    draws,
    ...overrides,
  };
}

function groupDraw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: 'draw-1',
    kind: 'Group',
    status: 'Draft',
    resolutionState: 'Resolved',
    slotPlacements: [],
    groupPlacements: [],
    ...overrides,
  };
}

function slotDraw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: 'draw-slot-1',
    kind: 'Slot',
    status: 'Draft',
    resolutionState: 'Resolved',
    slotPlacements: [
      {
        slotKey: 'R16-1-A',
        entryId: 'e1',
        displayName: 'Belgium',
      },
      {
        slotKey: 'R16-1-B',
        entryId: 'e2',
        displayName: 'Poland',
      },
      {
        slotKey: 'R16-2-A',
        entryId: 'e3',
        displayName: 'Turkey',
      },
      {
        slotKey: 'R16-2-B',
        entryId: 'e4',
        displayName: 'Denmark',
      },
    ],
    ...overrides,
  };
}

function cupStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return groupsStage({
    name: 'Éliminatoires',
    formatKind: 'Cup',
    groupCount: 0,
    roundCount: 4,
    numberOfPots: null,
    hasStandingRules: false,
    ...overrides,
  });
}

function renderDialog(
  stage = groupsStage(),
  open = true,
  competitionStatus: CompetitionStatus = 'Draft',
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const onClose = vi.fn();
  render(
    <QueryClientProvider client={queryClient}>
      <StructureDrawDialog
        open={open}
        onClose={onClose}
        competitionId={competitionId}
        competitionStatus={competitionStatus}
        stage={stage}
      />
    </QueryClientProvider>,
  );
  return { onClose, queryClient };
}

describe('StructureDrawDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clearToasts();
  });

  it('shows empty state and Create when there are no draws', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog();

    expect(
      await screen.findByRole('heading', { name: 'Exécutions de tirage' }),
    ).toBeInTheDocument();
    expect(await screen.findByText('Aucune exécution')).toBeInTheDocument();
    expect(
      screen.getByText(
        'Lancez un premier tirage pour peupler la forme de cette phase.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Nouveau tirage' }),
    ).toBeEnabled();
  });

  it('disables Nouveau and explains when upstream teams are not ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      groupsStage({
        compositionEntryCount: 0,
        isRootComposition: false,
      }),
    );

    expect(await screen.findByText('Aucune exécution')).toBeInTheDocument();
    const blocked =
      'Les confrontations qui déterminent les équipes de cette phase ne sont pas encore terminées. Le tirage ne peut pas encore être effectué.';
    expect(screen.getAllByText(blocked).length).toBeGreaterThanOrEqual(1);
    expect(
      screen.getByRole('button', { name: 'Nouveau tirage' }),
    ).toBeDisabled();
  });

  it('disables Nouveau and explains when a root phase has no teams', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      groupsStage({
        compositionEntryCount: 0,
        isRootComposition: true,
      }),
    );

    expect(await screen.findByText('Aucune exécution')).toBeInTheDocument();
    const blocked =
      'Ajoutez des équipes à cette phase avant de lancer le tirage.';
    expect(screen.getAllByText(blocked).length).toBeGreaterThanOrEqual(1);
    expect(
      screen.getByRole('button', { name: 'Nouveau tirage' }),
    ).toBeDisabled();
  });

  it('runs G2 create on Nouveau tirage', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));
    vi.mocked(createAndGenerateDraw).mockResolvedValue({
      drawId: 'draw-1',
      isResolved: true,
      isNoSolution: false,
    });

    renderDialog();
    await screen.findByRole('button', { name: 'Nouveau tirage' });
    await user.click(screen.getByRole('button', { name: 'Nouveau tirage' }));

    await waitFor(() => {
      expect(createAndGenerateDraw).toHaveBeenCalledWith(stageId, 'Group');
    });
    await waitFor(() => {
      expect(getToastsSnapshot().map((t) => t.message)).toContain(
        'Nouvelle exécution créée.',
      );
    });
  });

  it('creates Slot draws for Cup Nouveau (not Pairing)', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));
    vi.mocked(createAndGenerateDraw).mockResolvedValue({
      drawId: 'draw-slot-new',
      isResolved: true,
      isNoSolution: false,
    });

    renderDialog(
      cupStage({
        compositionEntryCount: 4,
        slotCount: 4,
      }),
    );

    await screen.findByRole('button', { name: 'Nouveau tirage' });
    await user.click(screen.getByRole('button', { name: 'Nouveau tirage' }));

    await waitFor(() => {
      expect(createAndGenerateDraw).toHaveBeenCalledWith(stageId, 'Slot');
    });
    expect(createAndGenerateDraw).not.toHaveBeenCalledWith(
      stageId,
      'Pairing',
    );
  });

  it('selects the new draw after Nouveau when history had a cancelled execution', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          id: 'draw-01',
          status: 'Cancelled',
        }),
      ]),
    );
    vi.mocked(createAndGenerateDraw).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview([
          groupDraw({
            id: 'draw-01',
            status: 'Cancelled',
          }),
          groupDraw({
            id: 'draw-02',
            status: 'Draft',
            resolutionState: 'Resolved',
          }),
        ]),
      );
      return {
        drawId: 'draw-02',
        isResolved: true,
        isNoSolution: false,
      };
    });

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Nouveau tirage' }),
    );

    await waitFor(() => {
      const history = screen.getByLabelText('Historique des exécutions');
      expect(
        within(history).getByRole('button', { current: true }),
      ).toHaveTextContent('Exécution #2');
    });
  });

  it('disables Nouveau when a non-Cancelled draw exists (Draft)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })]),
    );

    renderDialog();

    expect(
      await screen.findByRole('button', { name: 'Nouveau tirage' }),
    ).toBeDisabled();
  });

  it('disables Nouveau when a Published draw exists (applied or not)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          isApplied: true,
        }),
      ]),
    );

    renderDialog();

    expect(
      await screen.findByRole('button', { name: 'Nouveau tirage' }),
    ).toBeDisabled();
  });

  it('enables Nouveau when history is Cancelled-only', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          id: 'draw-old',
          status: 'Cancelled',
        }),
      ]),
    );

    renderDialog();

    expect(
      await screen.findByRole('button', { name: 'Nouveau tirage' }),
    ).toBeEnabled();
  });

  it('shows execution tiles and unified detail for a single draw', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw()]),
    );

    renderDialog();

    expect(
      await screen.findByRole('heading', { name: 'Tirage Groupe' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText('Résultat généré, prêt à être publié.'),
    ).not.toBeInTheDocument();
    expect(screen.getAllByText('Résolu').length).toBeGreaterThanOrEqual(2);
    expect(screen.queryByText('Brouillon')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Historique' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByLabelText('Historique des exécutions'),
    ).toBeInTheDocument();
    const publishAndApply = screen.getByRole('button', {
      name: 'Publier et appliquer',
    });
    expect(publishAndApply).toBeInTheDocument();
    expect(publishAndApply.className).toContain('ds-btn--primary');
  });

  it('lists group placements as dense rows', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          groupPlacements: [
            {
              groupId: 'g-a',
              groupDisplayName: 'A',
              entryId: 'e1',
              displayName: 'Équipe 1',
            },
            {
              groupId: 'g-a',
              groupDisplayName: 'A',
              entryId: 'e4',
              displayName: 'Équipe 4',
            },
            {
              groupId: 'g-b',
              groupDisplayName: 'B',
              entryId: 'e2',
              displayName: 'Équipe 2',
            },
            {
              groupId: 'g-b',
              groupDisplayName: 'B',
              entryId: 'e3',
              displayName: 'Équipe 3',
            },
          ],
        }),
      ]),
    );

    renderDialog();

    expect(await screen.findByText('Groupe A')).toBeInTheDocument();
    expect(screen.queryByText('Résultat')).not.toBeInTheDocument();
    expect(screen.getByText('Équipe 1')).toBeInTheDocument();
    expect(screen.getByText('Équipe 4')).toBeInTheDocument();
    expect(screen.getByText('Groupe B')).toBeInTheDocument();
    expect(screen.getByText('Équipe 2')).toBeInTheDocument();
    expect(screen.getByText('Équipe 3')).toBeInTheDocument();
  });

  it('renders Slot placements as dense A vs B confrontations without match numbers', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([slotDraw()]));

    renderDialog(cupStage());

    expect(
      await screen.findByRole('heading', { name: 'Tirage Emplacement' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Belgium')).toBeInTheDocument();
    expect(screen.getByText('Poland')).toBeInTheDocument();
    expect(screen.getByText('Turkey')).toBeInTheDocument();
    expect(screen.getByText('Denmark')).toBeInTheDocument();
    expect(screen.getByText('R16-1-A')).toBeInTheDocument();
    expect(screen.getByText('R16-1-B')).toBeInTheDocument();
    expect(screen.getByText('R16-2-A')).toBeInTheDocument();
    expect(screen.getByText('R16-2-B')).toBeInTheDocument();
    expect(screen.getAllByText('vs').length).toBe(2);
    expect(screen.queryByText('#1')).not.toBeInTheDocument();
    expect(
      document.querySelector('.regulation-schematic--cup'),
    ).toBeNull();
  });

  it('shows master-detail with selectable tiles when more than one draw exists', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          id: 'draw-1',
          status: 'Cancelled',
        }),
        groupDraw({
          id: 'draw-2',
          status: 'Draft',
        }),
      ]),
    );

    renderDialog();

    const history = await screen.findByLabelText('Historique des exécutions');
    const tiles = within(history).getAllByRole('button');
    // Newest first: #2 (Draft) above #1 (Cancelled)
    expect(tiles[0]).toHaveTextContent('Exécution #2');
    expect(tiles[1]).toHaveTextContent('Exécution #1');
    // One chip only (Résolu for draft resolved) — not the full matrix
    expect(within(tiles[0]).getByText('Résolu')).toBeInTheDocument();
    expect(within(tiles[0]).queryByText('Brouillon')).not.toBeInTheDocument();
    expect(within(tiles[0]).queryByText('Non appliqué')).not.toBeInTheDocument();

    await user.click(within(history).getByText('Exécution #1'));
    const detail = await screen.findByRole('heading', { name: 'Tirage Groupe' });
    const detailSection = detail.closest('section');
    expect(
      detailSection?.querySelector('.structure-domain-tile__toolbar'),
    ).toHaveTextContent('Annulé');
    expect(detailSection).toHaveClass('structure-draw-tile--muted');
    expect(
      screen.queryByText(
        'Ce tirage a été annulé. Un nouveau tirage est nécessaire pour recommencer.',
      ),
    ).not.toBeInTheDocument();
  });

  it('publishes and applies the selected draft draw', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw()]),
    );
    vi.mocked(publishAndApplyDraw).mockResolvedValue(undefined);

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Publier et appliquer' }),
    );

    await waitFor(() => {
      expect(publishAndApplyDraw).toHaveBeenCalledWith(stageId, 'draw-1', {
        fixtureIds: [],
      });
    });
    await waitFor(() => {
      expect(getToastsSnapshot().map((t) => t.message)).toContain(
        'Tirage publié et appliqué.',
      );
    });
  });

  it('shows Annuler l’exécution for Draft when competition open and stage Draft|Ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })]),
    );

    renderDialog(groupsStage({ status: 'Ready' }), true, 'Running');

    expect(
      await screen.findByRole('button', { name: 'Annuler l’exécution' }),
    ).toBeInTheDocument();
  });

  it('keeps Annuler l’exécution after Apply (A)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          isApplied: true,
        }),
      ]),
    );

    renderDialog();

    expect(
      await screen.findByRole('button', { name: 'Annuler l’exécution' }),
    ).toBeInTheDocument();
  });

  it('hides Annuler l’exécution when competition is Completed (D)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })]),
    );

    renderDialog(groupsStage(), true, 'Completed');

    await screen.findByRole('heading', { name: 'Tirage Groupe' });
    expect(
      screen.queryByRole('button', { name: 'Annuler l’exécution' }),
    ).not.toBeInTheDocument();
  });

  it('hides Annuler l’exécution when stage is Running (D)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })], { status: 'Running' }),
    );

    renderDialog(groupsStage({ status: 'Running' }), true, 'Running');

    await screen.findByRole('heading', { name: 'Tirage Groupe' });
    expect(
      screen.queryByRole('button', { name: 'Annuler l’exécution' }),
    ).not.toBeInTheDocument();
  });
});
