import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import { ApiError } from '../../../api/http';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  cancelDraw,
  createAndGenerateDraw,
  DrawGenerateFailedError,
  fetchStageOverview,
  publishAndApplyDraw,
  releaseDrawAlignedPlacements,
  rerunDrawGeneration,
} from '../../../api';
import {
  clearToasts,
  getToastsSnapshot,
} from '../../../design-system/toastStore';
import type {
  CompetitionStatus,
  StageDraw,
  StageOverview,
  StructureStageHubSummary,
} from '../../../types';
import { StructureDrawDialog } from './StructureDrawDialog';
import { scheduleStructureRefresh } from '../structureInvalidation';

vi.mock('../structureInvalidation', async (importOriginal) => {
  const actual =
    await importOriginal<typeof import('../structureInvalidation')>();
  return {
    ...actual,
    scheduleStructureRefresh: vi.fn(actual.scheduleStructureRefresh),
  };
});

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    createAndGenerateDraw: vi.fn(),
    publishAndApplyDraw: vi.fn(),
    cancelDraw: vi.fn(),
    applyDraw: vi.fn(),
    releaseDrawAlignedPlacements: vi.fn(),
    rerunDrawGeneration: vi.fn(),
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
    bracketPairs: [],
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
        slotKey: 'S1',
        entryId: 'e1',
        displayName: 'Belgium',
      },
      {
        slotKey: 'S2',
        entryId: 'e2',
        displayName: 'Poland',
      },
      {
        slotKey: 'S3',
        entryId: 'e3',
        displayName: 'Turkey',
      },
      {
        slotKey: 'S4',
        entryId: 'e4',
        displayName: 'Denmark',
      },
    ],
    ...overrides,
  };
}

const cupBracketPairs = [
  { pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' },
  { pairKey: 'P2', slotAKey: 'S3', slotBKey: 'S4' },
];

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
        structureStages={[stage]}
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
      await screen.findByRole('heading', {
        name: 'Exécutions de tirage — Groupes',
      }),
    ).toBeInTheDocument();
    expect(await screen.findByText('Aucune exécution')).toBeInTheDocument();
    expect(
      screen.getByText(
        'Lancez un premier tirage pour placer les équipes de cette phase.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Lancer le tirage' }),
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
    expect(
      screen.getByText(
        'Lancez un premier tirage pour placer les équipes de cette phase.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
    expect(
      screen.queryByText(
        'Les confrontations qui déterminent les équipes de cette phase ne sont pas encore terminées. Le tirage ne peut pas encore être effectué.',
      ),
    ).not.toBeInTheDocument();
  });

  it('disables Nouveau and shows inline info when a root phase has no teams', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      groupsStage({
        compositionEntryCount: 0,
        isRootComposition: true,
      }),
    );

    expect(await screen.findByText('Aucune exécution')).toBeInTheDocument();
    expect(
      screen.getByText(
        'Ajoutez d’abord les équipes de cette phase avant de lancer un tirage.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
  });

  it('runs G2 create on Lancer le tirage', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));
    vi.mocked(createAndGenerateDraw).mockResolvedValue({
      drawId: 'draw-1',
      isResolved: true,
      isNoSolution: false,
    });

    renderDialog();
    await screen.findByRole('button', { name: 'Lancer le tirage' });
    await user.click(screen.getByRole('button', { name: 'Lancer le tirage' }));

    await waitFor(() => {
      expect(createAndGenerateDraw).toHaveBeenCalledWith(stageId, 'Group');
    });
    await waitFor(() => {
      expect(getToastsSnapshot().map((t) => t.message)).toContain(
        'Nouvelle exécution créée.',
      );
    });
    expect(scheduleStructureRefresh).toHaveBeenCalledWith(
      expect.anything(),
      competitionId,
      ['stageOverview'],
      { stageId },
    );
    expect(scheduleStructureRefresh).not.toHaveBeenCalledWith(
      expect.anything(),
      competitionId,
      ['hubStageShell'],
      expect.anything(),
    );
  });

  it('toasts warning when Create+Generate returns NoSolution', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));
    vi.mocked(createAndGenerateDraw).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview([
          groupDraw({
            id: 'draw-nosol',
            status: 'Draft',
            resolutionState: 'NoSolution',
            groupPlacements: [],
          }),
        ]),
      );
      return {
        drawId: 'draw-nosol',
        isResolved: false,
        isNoSolution: true,
      };
    });

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    );

    await waitFor(() => {
      expect(getToastsSnapshot().map((t) => t.message)).toContain(
        'Exécution créée — aucune résolution valide.',
      );
    });
    expect(
      await screen.findByText('Aucune résolution valide n’a pu être générée.'),
    ).toBeInTheDocument();
    expect(
      screen.queryByText('Nouvelle exécution créée.'),
    ).not.toBeInTheDocument();
  });

  it('keeps Draft and projects generation interrupted when Generate fails after Create', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));
    vi.mocked(createAndGenerateDraw).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview([
          groupDraw({
            id: 'draw-orphan',
            status: 'Draft',
            resolutionState: 'NotResolved',
            groupPlacements: [],
          }),
        ]),
      );
      throw new DrawGenerateFailedError('draw-orphan', new Error('timeout'));
    });

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    );

    await waitFor(() => {
      const toasts = getToastsSnapshot();
      expect(toasts.map((t) => t.message)).toContain(
        'Génération interrompue. L’exécution a été créée sans résultat.',
      );
      expect(toasts.some((t) => t.tone === 'error')).toBe(true);
    });
    expect(
      await screen.findByText('Génération interrompue'),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        'L’exécution a été créée, mais la génération n’a pas pu être terminée.',
      ),
    ).toBeInTheDocument();
    // No sticky MutationError footer after create path clears.
    expect(
      screen.queryByText('Draw generation failed after create'),
    ).not.toBeInTheDocument();
  });

  it('shows EmptyState for Cancelled+NotResolved (badge only, no phrase)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          id: 'draw-cancelled-empty',
          status: 'Cancelled',
          resolutionState: 'NotResolved',
          groupPlacements: [],
        }),
      ]),
    );

    renderDialog();

    await screen.findByText('Aucun résultat');
    expect(screen.getAllByText('Annulé').length).toBeGreaterThan(0);
    expect(
      screen.queryByText(
        'Exécution annulée. Un nouveau tirage est nécessaire pour recommencer.',
      ),
    ).not.toBeInTheDocument();
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
        compositionCapacity: 4,
      }),
    );

    await screen.findByRole('button', { name: 'Lancer le tirage' });
    await user.click(screen.getByRole('button', { name: 'Lancer le tirage' }));

    await waitFor(() => {
      expect(createAndGenerateDraw).toHaveBeenCalledWith(stageId, 'Slot');
    });
    expect(createAndGenerateDraw).not.toHaveBeenCalledWith(stageId, 'Pairing');
  });

  it('disables Cup Nouveau when composition does not match Places', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      cupStage({
        compositionEntryCount: 15,
        compositionCapacity: 16,
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
    expect(
      screen.getByText(
        'Il faut 16 équipes pour 16 places ; la composition en a 15.',
      ),
    ).toBeInTheDocument();
    expect(createAndGenerateDraw).not.toHaveBeenCalled();
  });

  it('disables Cup Nouveau when a Place has an occupant', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([], {
        slots: [
          {
            slotKey: 'R16-1-A',
            entryId: 'e1',
            displayName: 'Belgium',
            coveredByCompleteFixture: false,
          },
          {
            slotKey: 'R16-1-B',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
      }),
    );

    renderDialog(
      cupStage({
        compositionEntryCount: 16,
        compositionCapacity: 16,
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
    expect(
      screen.getByText(
        'Libérez les placements existants avant de lancer un nouveau tirage.',
      ),
    ).toBeInTheDocument();
  });

  it('disables Cup Nouveau for DirectAssignment when count matches', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      cupStage({
        compositionEntryCount: 16,
        compositionCapacity: 16,
        directAssignmentCount: 1,
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
    expect(
      screen.getByText(
        'Libérez les affectations directes avant de lancer un nouveau tirage.',
      ),
    ).toBeInTheDocument();
  });

  it('allows Cup Nouveau when Qual/Prog feeds are vacant (entryId null)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([], {
        slots: [
          {
            slotKey: 'R16-1-A',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
          {
            slotKey: 'R16-1-B',
            entryId: null,
            displayName: null,
            coveredByCompleteFixture: false,
          },
        ],
      }),
    );

    renderDialog(
      cupStage({
        compositionEntryCount: 16,
        compositionCapacity: 16,
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeEnabled();
  });

  it('enables Cup Nouveau after Libérer clears aligned occupants', async () => {
    const user = userEvent.setup();
    const occupiedSlots = [
      {
        slotKey: 'R16-1-A',
        entryId: 'e1',
        displayName: 'Belgium',
        coveredByCompleteFixture: false,
      },
      {
        slotKey: 'R16-1-B',
        entryId: 'e2',
        displayName: 'Poland',
        coveredByCompleteFixture: false,
      },
    ];
    const cancelled = slotDraw({
      id: 'draw-cancelled',
      status: 'Cancelled',
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
      ],
    });

    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([cancelled], { slots: occupiedSlots }),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview([cancelled], {
          slots: occupiedSlots.map((slot) => ({
            ...slot,
            entryId: null,
            displayName: null,
          })),
        }),
      );
      return { releasedCount: 2, skippedCount: 0 };
    });

    renderDialog(
      cupStage({
        compositionEntryCount: 16,
        compositionCapacity: 16,
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();

    await user.click(
      await screen.findByRole('button', { name: 'Libérer 2/2' }),
    );
    const confirmDialog = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirmDialog).getByRole('button', {
        name: 'Libérer les placements du tirage',
      }),
    );

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Lancer le tirage' }),
      ).toBeEnabled();
    });
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
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    );

    await waitFor(() => {
      expect(
        screen.getByText('Résultat généré, prêt à être publié.'),
      ).toBeInTheDocument();
    });
    expect(screen.getByText('Exécutions précédentes (1)')).toBeInTheDocument();
    expect(
      screen.queryByLabelText('Historique des exécutions'),
    ).not.toBeInTheDocument();
  });

  it('hides Lancer when a non-Cancelled draw exists (Draft)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })]),
    );

    renderDialog();

    expect(
      await screen.findByRole('button', { name: 'Relancer' }),
    ).toBeEnabled();
    expect(
      screen.queryByRole('button', { name: 'Lancer le tirage' }),
    ).not.toBeInTheDocument();
  });

  it('hides Lancer when a Published applied draw exists', async () => {
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
      await screen.findByRole('button', { name: 'Refaire le tirage' }),
    ).toBeEnabled();
    expect(
      screen.queryByRole('button', { name: 'Lancer le tirage' }),
    ).not.toBeInTheDocument();
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
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeEnabled();
  });

  it('shows execution tiles and unified detail for a single draw', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));

    renderDialog();

    expect(
      await screen.findByRole('heading', { name: 'Tirage Groupe' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Résultat généré, prêt à être publié.'),
    ).toBeInTheDocument();
    expect(screen.getAllByText('Résolu')).toHaveLength(1);
    expect(screen.queryByText('Brouillon')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Historique' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByLabelText('Historique des exécutions'),
    ).not.toBeInTheDocument();
    const publishAndApply = screen.getByRole('button', {
      name: 'Valider le tirage',
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

    const schematic = await screen.findByLabelText('Résultat');
    expect(schematic).toHaveClass('regulation-schematic--groups');
    expect(document.querySelector('.draw-group-list')).toBeNull();
    expect(within(schematic).getAllByText('Groupe A')).toHaveLength(1);
    expect(within(schematic).getByText('Équipe 1')).toBeInTheDocument();
    expect(within(schematic).getByText('Équipe 4')).toBeInTheDocument();
    expect(within(schematic).getAllByText('Groupe B')).toHaveLength(1);
    expect(within(schematic).getByText('Équipe 2')).toBeInTheDocument();
    expect(within(schematic).getByText('Équipe 3')).toBeInTheDocument();
    expect(within(schematic).getAllByText('Place libre')).toHaveLength(4);
    const cards = schematic.querySelectorAll('.regulation-schematic__card');
    expect(cards[0]).toHaveTextContent('Groupe A');
    expect(cards[0]).toHaveTextContent('Équipe 1');
  });

  it('renders Slot placements as dense A vs B from BracketPairs without match numbers', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([slotDraw()], { bracketPairs: cupBracketPairs }),
    );

    renderDialog(cupStage());

    expect(
      await screen.findByRole('heading', { name: 'Tirage Emplacement' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Belgium')).toBeInTheDocument();
    expect(screen.getByText('Poland')).toBeInTheDocument();
    expect(screen.getByText('Turkey')).toBeInTheDocument();
    expect(screen.getByText('Denmark')).toBeInTheDocument();
    expect(screen.getByText('P1 · S1/S2')).toBeInTheDocument();
    expect(screen.getByText('P2 · S3/S4')).toBeInTheDocument();
    expect(screen.getAllByText('vs')).toHaveLength(2);
    expect(screen.queryByText('#1')).not.toBeInTheDocument();
    expect(document.querySelector('.regulation-schematic--cup')).toBeNull();
    expect(document.querySelector('.schematic-cup')).toBeNull();
  });

  it('does not fall back to flat Place → Entry when BracketPairs cover Cup S{n}', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            slotPlacements: [
              { slotKey: 'S1', entryId: 'e1', displayName: 'Belgium' },
              { slotKey: 'S2', entryId: 'e2', displayName: 'Poland' },
            ],
          }),
        ],
        {
          bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
        },
      ),
    );

    renderDialog(cupStage());

    expect(
      await screen.findByRole('heading', { name: 'Tirage Emplacement' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Belgium')).toBeInTheDocument();
    expect(screen.getByText('Poland')).toBeInTheDocument();
    expect(screen.getByText('P1 · S1/S2')).toBeInTheDocument();
    expect(screen.getByText('vs')).toBeInTheDocument();
    expect(screen.queryByText('→')).not.toBeInTheDocument();
  });

  it('keeps an uncovered Slot placement as a flat Place → Entry row', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            slotPlacements: [
              { slotKey: 'S1', entryId: 'e1', displayName: 'Belgium' },
              { slotKey: 'S2', entryId: 'e2', displayName: 'Poland' },
              { slotKey: 'S9', entryId: 'e9', displayName: 'Spain' },
            ],
          }),
        ],
        {
          bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
        },
      ),
    );

    renderDialog(cupStage());

    expect(
      await screen.findByRole('heading', { name: 'Tirage Emplacement' }),
    ).toBeInTheDocument();
    const leftovers = screen.getByRole('list', { name: 'Placements' });
    expect(within(leftovers).getByText('S9')).toBeInTheDocument();
    expect(within(leftovers).getByText('Spain')).toBeInTheDocument();
    expect(within(leftovers).getByText('→')).toBeInTheDocument();
    expect(leftovers.querySelector('.regulation-schematic')).toBeNull();
  });

  it('shows Sans solution as a danger alert and keeps publish hidden', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          resolutionState: 'NoSolution',
          groupPlacements: [],
        }),
      ]),
    );

    renderDialog();

    const message = await screen.findByText(
      'Aucune résolution valide n’a pu être générée.',
    );
    expect(message.closest('.ds-alert')).toHaveClass('ds-alert--danger');
    expect(screen.getAllByText('Sans solution').length).toBeGreaterThan(0);
    expect(
      screen.queryByRole('button', { name: 'Valider le tirage' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Appliquer' }),
    ).not.toBeInTheDocument();
  });

  it('keeps the applied phrase off the title (Appliqué chip only)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          resolutionState: 'Resolved',
          isApplied: true,
          groupPlacements: [
            {
              groupId: 'g-a',
              groupDisplayName: 'A',
              entryId: 'e1',
              displayName: 'Équipe 1',
            },
          ],
        }),
      ]),
    );

    renderDialog();

    expect(await screen.findByText('Équipe 1')).toBeInTheDocument();
    expect(
      screen.queryByText('Résultat appliqué à la phase.'),
    ).not.toBeInTheDocument();
    expect(screen.getAllByText('Publié').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Appliqué').length).toBeGreaterThan(0);
  });

  it('keeps the current execution full width and folds cancelled runs', async () => {
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

    const current = await screen.findByRole('heading', {
      name: 'Tirage Groupe',
    });
    expect(current.closest('section')).toHaveTextContent('Résolu');
    expect(current.closest('section')).not.toHaveTextContent('Brouillon');
    expect(
      screen.queryByLabelText('Historique des exécutions'),
    ).not.toBeInTheDocument();
    expect(screen.getByText('Exécutions précédentes (1)')).toBeInTheDocument();

    await user.click(screen.getByText('Exécutions précédentes (1)'));
    const previous = await screen.findByRole('heading', {
      name: 'Exécution #1',
    });
    expect(previous.closest('section')).toHaveTextContent('Annulé');
    expect(
      screen.queryByText(
        'Exécution annulée. Un nouveau tirage est nécessaire pour recommencer.',
      ),
    ).not.toBeInTheDocument();
  });

  it('clears sticky publish-and-apply error when the dialog reopens', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    vi.mocked(publishAndApplyDraw).mockRejectedValue(
      new ApiError(
        409,
        'Conflict',
        'Entry appears on materialized match',
        'MaterializedOpponentConflict',
      ),
    );

    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const onClose = vi.fn();
    const { rerender } = render(
      <QueryClientProvider client={queryClient}>
        <StructureDrawDialog
          open
          onClose={onClose}
          competitionId={competitionId}
          competitionStatus="Draft"
          stage={groupsStage()}
          structureStages={[groupsStage()]}
        />
      </QueryClientProvider>,
    );

    await user.click(
      await screen.findByRole('button', { name: 'Valider le tirage' }),
    );
    expect(await screen.findByRole('alert')).toHaveTextContent(
      /équipe figure déjà sur un match créé|already on a created match/i,
    );

    rerender(
      <QueryClientProvider client={queryClient}>
        <StructureDrawDialog
          open={false}
          onClose={onClose}
          competitionId={competitionId}
          competitionStatus="Draft"
          stage={groupsStage()}
          structureStages={[groupsStage()]}
        />
      </QueryClientProvider>,
    );
    rerender(
      <QueryClientProvider client={queryClient}>
        <StructureDrawDialog
          open
          onClose={onClose}
          competitionId={competitionId}
          competitionStatus="Draft"
          stage={groupsStage()}
          structureStages={[groupsStage()]}
        />
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
  });

  it('publishes and applies the selected draft draw', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    vi.mocked(publishAndApplyDraw).mockResolvedValue(undefined);

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Valider le tirage' }),
    );

    await waitFor(() => {
      expect(publishAndApplyDraw).toHaveBeenCalledWith(stageId, 'draw-1');
    });
    await waitFor(() => {
      expect(getToastsSnapshot().map((t) => t.message)).toContain(
        'Tirage publié et appliqué.',
      );
    });
  });

  it('shows Abandonner for Draft when competition open and stage Draft|Ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })]),
    );

    renderDialog(groupsStage({ status: 'Ready' }), true, 'Running');

    expect(
      await screen.findByRole('button', { name: 'Abandonner' }),
    ).toBeInTheDocument();
  });

  it('offers Refaire instead of Abandonner after Apply when the phase is Draft', async () => {
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
      await screen.findByRole('button', { name: 'Refaire le tirage' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Abandonner' }),
    ).not.toBeInTheDocument();
  });

  it('hides Abandonner when competition is Completed (D)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })]),
    );

    renderDialog(groupsStage(), true, 'Completed');

    await screen.findByRole('heading', { name: 'Tirage Groupe' });
    expect(
      screen.queryByRole('button', { name: 'Abandonner' }),
    ).not.toBeInTheDocument();
  });

  it('hides Abandonner when stage is Running (D)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft' })], { status: 'Running' }),
    );

    renderDialog(groupsStage({ status: 'Running' }), true, 'Running');

    await screen.findByRole('heading', { name: 'Tirage Groupe' });
    expect(
      screen.queryByRole('button', { name: 'Abandonner' }),
    ).not.toBeInTheDocument();
  });

  it('shows Libérer CTA and remaining count for Cancelled Slot with aligned places (D)', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Cancelled',
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
            ],
          }),
        ],
        {
          slots: [
            {
              slotKey: 'R16-1-A',
              entryId: 'e1',
              displayName: 'Belgium',
              coveredByCompleteFixture: false,
            },
            {
              slotKey: 'R16-1-B',
              entryId: 'e9',
              displayName: 'Other',
              coveredByCompleteFixture: false,
            },
          ],
        },
      ),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockResolvedValue({
      releasedCount: 1,
      skippedCount: 1,
    });

    renderDialog(cupStage());

    expect(
      await screen.findByRole('button', { name: 'Libérer 1/2' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Annulé, placements non libérés'),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/placements encore présents/),
    ).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Libérer 1/2' }));
    const confirm = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', {
        name: 'Libérer les placements du tirage',
      }),
    );

    await waitFor(() => {
      expect(releaseDrawAlignedPlacements).toHaveBeenCalledWith(
        stageId,
        'draw-slot-1',
      );
    });
    await waitFor(() => {
      expect(getToastsSnapshot().map((t) => t.message)).toContain(
        'Placements libérés (1).',
      );
    });
  });

  it('hides Libérer when Cancelled Slot has no aligned places (D)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Cancelled',
            resolutionState: 'Resolved',
            slotPlacements: [
              {
                slotKey: 'R16-1-A',
                entryId: 'e1',
                displayName: 'Belgium',
              },
            ],
          }),
        ],
        {
          slots: [
            {
              slotKey: 'R16-1-A',
              entryId: 'e9',
              displayName: 'Other',
              coveredByCompleteFixture: false,
            },
          ],
        },
      ),
    );

    renderDialog(cupStage());

    expect(
      await screen.findByText('Exécutions précédentes (1)'),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/placements encore présents/),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText('Annulé, placements non libérés'),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', {
        name: /Libérer/,
      }),
    ).not.toBeInTheDocument();
  });

  it('reruns generation on the same draw and does not create an execution', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    vi.mocked(rerunDrawGeneration).mockResolvedValue({
      drawId: 'draw-1',
      isResolved: true,
      isNoSolution: false,
    });

    renderDialog();
    await user.click(await screen.findByRole('button', { name: 'Relancer' }));

    await waitFor(() => {
      expect(rerunDrawGeneration).toHaveBeenCalledWith(stageId, 'draw-1');
    });
    expect(createAndGenerateDraw).not.toHaveBeenCalled();
    await waitFor(() => {
      expect(getToastsSnapshot().map((toast) => toast.message)).toContain(
        'Tirage relancé.',
      );
    });
  });

  it('keeps the same draw when a rerun hits the launch guard', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    vi.mocked(rerunDrawGeneration).mockRejectedValue(
      new ApiError(
        409,
        'Conflict',
        'grid',
        'Application.DrawLaunchGridNotVacant',
      ),
    );

    renderDialog();
    await user.click(await screen.findByRole('button', { name: 'Relancer' }));

    expect(
      await screen.findByText(
        'Le tirage ne peut pas être relancé tant que des places du tableau sont occupées.',
      ),
    ).toBeInTheDocument();
    expect(createAndGenerateDraw).not.toHaveBeenCalled();
    expect(rerunDrawGeneration).toHaveBeenCalledWith(stageId, 'draw-1');
  });

  it('projects generation failure on rerun without a new execution', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    vi.mocked(rerunDrawGeneration).mockRejectedValue(
      new DrawGenerateFailedError('draw-1', new Error('timeout')),
    );

    renderDialog();
    await user.click(await screen.findByRole('button', { name: 'Relancer' }));

    await waitFor(() => {
      expect(getToastsSnapshot().map((toast) => toast.message)).toContain(
        'Génération interrompue. L’exécution a été créée sans résultat.',
      );
    });
    expect(createAndGenerateDraw).not.toHaveBeenCalled();
    expect(rerunDrawGeneration).toHaveBeenCalledTimes(1);
  });

  it('refuses Refaire for a group phase that is Ready and explains why', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            status: 'Published',
            isApplied: true,
          }),
        ],
        { status: 'Ready' },
      ),
    );

    renderDialog(groupsStage({ status: 'Ready' }));

    expect(
      await screen.findByText(
        'Le tirage ne peut pas être refait tant que la phase n’est pas en brouillon.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Refaire le tirage' }),
    ).not.toBeInTheDocument();
  });

  it('refuses Cup Refaire when a touched pair has an attached match', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Published',
            isApplied: true,
          }),
        ],
        {
          bracketPairs: cupBracketPairs,
          slots: [
            {
              slotKey: 'S1',
              entryId: 'e1',
              displayName: 'Belgium',
              coveredByCompleteFixture: true,
            },
            {
              slotKey: 'S2',
              entryId: 'e2',
              displayName: 'Poland',
              coveredByCompleteFixture: true,
            },
            {
              slotKey: 'S3',
              entryId: 'e3',
              displayName: 'Turkey',
              coveredByCompleteFixture: false,
            },
            {
              slotKey: 'S4',
              entryId: 'e4',
              displayName: 'Denmark',
              coveredByCompleteFixture: false,
            },
          ],
          rounds: [
            {
              id: 'round-1',
              name: 'Tour 1',
              fixtures: [
                {
                  id: 'fx-1',
                  slotAKey: 'S1',
                  slotBKey: 'S2',
                  attachments: [{ matchId: 'match-1', legIndex: 0 }],
                },
              ],
            },
          ],
        },
      ),
    );

    renderDialog(
      cupStage({
        compositionEntryCount: 4,
        compositionCapacity: 4,
      }),
    );

    expect(
      await screen.findByText(/un match est rattaché/),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Refaire le tirage' }),
    ).not.toBeInTheDocument();
  });

  it('redoes an applied draw as cancel, release, then a new preview', async () => {
    const user = userEvent.setup();
    const order: string[] = [];
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          isApplied: true,
          groupPlacements: [
            {
              groupId: 'g-a',
              groupDisplayName: 'A',
              entryId: 'e1',
              displayName: 'Équipe 1',
            },
          ],
        }),
      ]),
    );
    vi.mocked(cancelDraw).mockImplementation(async () => {
      order.push('cancel');
    });
    vi.mocked(releaseDrawAlignedPlacements).mockImplementation(async () => {
      order.push('release');
      return { releasedCount: 1, skippedCount: 0 };
    });
    vi.mocked(createAndGenerateDraw).mockImplementation(async () => {
      order.push('create');
      return {
        drawId: 'draw-next',
        isResolved: true,
        isNoSolution: false,
      };
    });

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Refaire le tirage' }),
    );
    const confirm = await screen.findByRole('dialog', {
      name: 'Refaire le tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', { name: 'Refaire le tirage' }),
    );

    await waitFor(() => {
      expect(order).toEqual(['cancel', 'release', 'create']);
    });
    expect(cancelDraw).toHaveBeenCalledWith(stageId, 'draw-1');
    expect(releaseDrawAlignedPlacements).toHaveBeenCalledWith(
      stageId,
      'draw-1',
    );
    expect(createAndGenerateDraw).toHaveBeenCalledWith(stageId, 'Group');
  });

  it('marks seeds and constraints as not applied and keeps pots × groups', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      groupsStage({
        numberOfSeeds: 2,
        drawConstraints: [
          {
            type: 'MaxSameAssociationPerGroup',
            enforcement: 'Required',
            maxPerGroup: 1,
          },
          { type: 'SameTeamAvoidance', enforcement: 'Preferred' },
        ],
      }),
    );

    expect(await screen.findByText('4 pots × 2 groupes')).toBeInTheDocument();
    expect(screen.getByText('2 têtes de série')).toBeInTheDocument();
    expect(
      screen.getByText(/Max\. même association \/ groupe/),
    ).toBeInTheDocument();
    expect(screen.getByText(/Éviter la même équipe/)).toBeInTheDocument();
    expect(
      document.querySelectorAll('.structure-draw-parameters__mark'),
    ).toHaveLength(3);
  });

  it('alerts when the draw result is stale', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          isObsoleteForConsumption: true,
        }),
      ]),
    );

    renderDialog();

    const alert = await screen.findByText(/ne correspond plus à la phase/);
    expect(alert.closest('.ds-alert')).toHaveClass('ds-alert--warning');
  });

  it('validates a draft without an apply confirmation', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    vi.mocked(publishAndApplyDraw).mockResolvedValue(undefined);

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Valider le tirage' }),
    );

    await waitFor(() => {
      expect(publishAndApplyDraw).toHaveBeenCalledWith(stageId, 'draw-1');
    });
    expect(
      screen.queryByRole('dialog', { name: 'Appliquer le tirage ?' }),
    ).not.toBeInTheDocument();
  });
});
