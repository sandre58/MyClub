import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { useState } from 'react';
import { ApiError } from '../../../api/http';
import userEvent from '@testing-library/user-event';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
  beforeAll,
  afterEach,
  afterAll,
} from 'vitest';
import {
  drawActions,
  hiddenDrawActions,
  withDrawActions,
} from '../../../test/withDrawActions';
import {
  cancelDraw,
  createAndGenerateDraw,
  DrawGenerateFailedError,
  fetchStageOverview,
  publishAndApplyDraw,
  redoDraw,
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
import { queryKeys } from '../../../queryKeys';
import { scheduleStructureRefresh } from '../structureInvalidation';

const scrollIntoViewMock = vi.fn();
const originalScrollIntoView = Element.prototype.scrollIntoView;

beforeAll(() => {
  Element.prototype.scrollIntoView = scrollIntoViewMock;
});

afterAll(() => {
  Element.prototype.scrollIntoView = originalScrollIntoView;
});

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
    redoDraw: vi.fn(),
  };
});

const drawDialogCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), '../structure-dialogs.css'),
  'utf8',
);

const stageId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const releaseAttachedMatchesError =
  'Impossible de libérer les placements : des matchs sont déjà rattachés à cette phase. Détachez-les ou repassez la phase en brouillon.';

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
    drawCreateLaunch: { allowed: true, blockReasonCode: null },
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
  releaseCandidateDrawId?: string | null,
): StageOverview {
  return withDrawActions(
    {
      id: stageId,
      competitionId,
      name: 'Groupes',
      status: 'Draft',
      slots: [],
      rounds: [],
      draws,
      bracketPairs: [],
      ...overrides,
    },
    releaseCandidateDrawId === undefined ? {} : { releaseCandidateDrawId },
  );
}

function releaseAvailable(releasableCount: number) {
  return drawActions({
    release: { state: 'Available', releasableCount },
  });
}

function releaseBlocked(releasableCount: number, blockCode: string) {
  return drawActions({
    release: { state: 'Blocked', blockCode, releasableCount },
  });
}

function appliedDrawActions() {
  return drawActions({
    cancel: { state: 'Available' },
    redo: { state: 'Available' },
  });
}

function applyAvailableActions() {
  return drawActions({
    apply: { state: 'Available' },
  });
}

function redoBlockedActions(blockCode: string) {
  return drawActions({
    cancel: { state: 'Available' },
    redo: { state: 'Blocked', blockCode },
  });
}

function groupDraw(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: 'draw-1',
    kind: 'Group',
    status: 'Draft',
    resolutionState: 'Resolved',
    slotPlacements: [],
    groupPlacements: [],
    actions: drawActions({
      cancel: { state: 'Available' },
      publishAndApply: { state: 'Available' },
    }),
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
    actions: drawActions({
      cancel: { state: 'Available' },
      publishAndApply: { state: 'Available' },
    }),
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
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructureDrawDialog
          open={open}
          onClose={onClose}
          competitionId={competitionId}
          competitionStatus={competitionStatus}
          stage={stage}
          structureStages={[stage]}
        />
      </QueryClientProvider>
    </MemoryRouter>,
  );
  return { onClose, queryClient };
}

describe('structure draw dialog height', () => {
  it('keeps the draw dialog in the shared panel rules', () => {
    expect(drawDialogCss).not.toMatch(/dvh/);
    expect(drawDialogCss).toMatch(
      /\.ds-dialog:has\(\.structure-qualification\) \.ds-dialog__panel,\s*\.ds-dialog:has\(\.structure-progression\) \.ds-dialog__panel,\s*\.ds-dialog:has\(\.structure-draw-dialog\) \.ds-dialog__panel \{[^}]*max-height:\s*min\(92vh,\s*52rem\)/,
    );
    expect(drawDialogCss).toMatch(
      /\.ds-dialog:has\(\.structure-draw-dialog\) \.ds-dialog__body \{[^}]*overflow:\s*auto/,
    );
    expect(drawDialogCss).not.toMatch(
      /\.ds-dialog:has\(\.structure-draw-dialog\) \.ds-dialog__body \{[^}]*overflow:\s*hidden/,
    );
    expect(drawDialogCss).not.toMatch(
      /@media \(max-height:\s*36rem\)[\s\S]*structure-draw-dialog/,
    );
    expect(drawDialogCss).toMatch(
      /\.structure-draw-dialog \{[^}]*flex:\s*none/,
    );
    expect(drawDialogCss).toMatch(
      /\.structure-draw-execution \{[^}]*flex:\s*none/,
    );
    expect(drawDialogCss).toMatch(
      /\.structure-draw-dialog \.ds-empty--idle \{[^}]*flex:\s*none/,
    );
    expect(drawDialogCss).not.toMatch(/structure-draw-footer__start/);
    expect(drawDialogCss).not.toMatch(/structure-draw-title__jump/);
    expect(drawDialogCss).not.toMatch(/structure-draw-previous__grid/);
    expect(drawDialogCss).not.toMatch(/structure-draw-cancelled/);
    expect(drawDialogCss).not.toMatch(/structure-draw-history-row/);
    expect(drawDialogCss).toMatch(
      /\.structure-draw-previous__item\s*\{[^}]*opacity:\s*0\.7/,
    );
    expect(drawDialogCss).not.toMatch(
      /structure-draw-previous__tile[\s\S]*data-tone/,
    );
    expect(drawDialogCss).toMatch(/\.structure-draw-previous__meta\s*\{/);
    expect(drawDialogCss).toMatch(/\.structure-draw-previous__lead\s*\{/);
    expect(drawDialogCss).toMatch(
      /\.structure-draw-previous__trigger > \.ds-selectable-tile__chevron\s*\{[^}]*flex:\s*none[^}]*align-self:\s*flex-start/,
    );
    expect(drawDialogCss).toMatch(
      /\.structure-draw-previous__panel\s*\{[^}]*\}/,
    );
    expect(drawDialogCss).not.toMatch(
      /\.structure-draw-previous__panel\s*\{[^}]*border-block-start/,
    );
    expect(drawDialogCss).toMatch(
      /@media \(prefers-reduced-motion:\s*reduce\)[\s\S]*\.structure-draw-previous__panel[\s\S]*animation:\s*none/,
    );
    const attribution = drawDialogCss.match(
      /\.ds-dialog:has\(\.structure-attribution\) \.ds-dialog__panel \{[^}]*\}/,
    );
    expect(attribution?.[0]).not.toMatch(/structure-draw-dialog/);
  });
});

describe('StructureDrawDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clearToasts();
    scrollIntoViewMock.mockClear();
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
        drawCreateLaunch: {
          allowed: false,
          blockReasonCode: 'UpstreamCompositionPoolEmpty',
        },
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

  it('shows the server empty-pool reason even when the pool count is filled', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      groupsStage({
        compositionEntryCount: 8,
        isRootComposition: true,
        drawCreateLaunch: {
          allowed: false,
          blockReasonCode: 'EmptyCompositionPool',
        },
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
            actions: hiddenDrawActions(),
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

    expect(
      await screen.findByText('Aucun tirage en cours'),
    ).toBeInTheDocument();
    const cancelledTrigger = screen.getByRole('button', {
      name: /Exécution #1/,
    });
    expect(cancelledTrigger).toHaveAttribute('aria-expanded', 'false');
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
        drawCreateLaunch: {
          allowed: false,
          blockReasonCode: 'PlacesCountMismatch',
        },
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

  it('disables Cup Nouveau when the hub says places are occupied', async () => {
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
        drawCreateLaunch: {
          allowed: false,
          blockReasonCode: 'NoFreeSlot',
        },
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
    expect(
      screen.getByText(
        'Toutes les places de cette phase sont déjà occupées ou réservées. Le tirage ne remplace pas les équipes déjà placées.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(
        'Le tirage remplira uniquement les places libres. Les équipes déjà placées restent en place.',
      ),
    ).not.toBeInTheDocument();
  });

  it('disables Cup Nouveau when unplaced teams do not fill the free places', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      cupStage({
        compositionEntryCount: 2,
        compositionCapacity: 4,
        drawCreateLaunch: {
          allowed: false,
          blockReasonCode: 'NotEnoughTeams',
        },
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
    expect(
      screen.getByText(
        'Les équipes non placées ne suffisent pas à remplir les places encore libres.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Ouvrir les équipes' }),
    ).toBeInTheDocument();
  });

  it('shows the server direct-assignment reason even when the hub count is zero', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog(
      cupStage({
        compositionEntryCount: 16,
        compositionCapacity: 16,
        directAssignmentCount: 0,
        drawCreateLaunch: {
          allowed: false,
          blockReasonCode: 'DirectAssignmentPresent',
        },
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

  it('enables Cup Nouveau when the hub allows it even if slots have occupants', async () => {
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
        drawCreateLaunch: { allowed: true, blockReasonCode: null },
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeEnabled();
    expect(
      screen.queryByText(
        'Libérez les placements existants avant de lancer un nouveau tirage.',
      ),
    ).not.toBeInTheDocument();
    expect(
      screen.getByText(
        'Le tirage remplira uniquement les places libres. Les équipes déjà placées restent en place.',
      ),
    ).toBeInTheDocument();
  });

  it('keeps Cup launch on the hub decision after Libérer clears occupants', async () => {
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
      actions: releaseAvailable(2),
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
      overview([cancelled], { slots: occupiedSlots }, 'draw-cancelled'),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview(
          [{ ...cancelled, actions: hiddenDrawActions() }],
          {
            slots: occupiedSlots.map((slot) => ({
              ...slot,
              entryId: null,
              displayName: null,
            })),
          },
          null,
        ),
      );
      return { releasedCount: 2, skippedCount: 0 };
    });

    renderDialog(
      cupStage({
        compositionEntryCount: 16,
        compositionCapacity: 16,
        drawCreateLaunch: {
          allowed: false,
          blockReasonCode: 'BracketPlacesOccupied',
        },
      }),
    );

    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();

    const releaseButton = await screen.findByRole('button', {
      name: 'Libérer les 2 équipes tirées',
    });
    await user.click(releaseButton);
    const confirmDialog = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirmDialog).getByRole('button', {
        name: 'Libérer les 2 équipes tirées',
      }),
    );

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Lancer le tirage' }),
      ).toBeDisabled();
    });
    expect(
      screen.getByText(
        'Libérez les placements existants avant de lancer un nouveau tirage.',
      ),
    ).toBeInTheDocument();
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
        screen.getByRole('heading', { name: 'Exécution #2' }),
      ).toBeInTheDocument();
    });
    expect(
      screen.queryByText('Résultat généré, prêt à être publié.'),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', {
        name: /Exécution #1/,
      }),
    ).toHaveAttribute('aria-expanded', 'false');
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
          actions: appliedDrawActions(),
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
      await screen.findByRole('heading', { name: 'Exécution #1' }),
    ).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
    expect(screen.getByText('équipes')).toBeInTheDocument();
    expect(screen.getByText('Brouillon')).toBeInTheDocument();
    expect(
      screen.queryByText('Résultat généré, prêt à être publié.'),
    ).not.toBeInTheDocument();
    expect(screen.queryByText('Résolu')).not.toBeInTheDocument();
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
    const abandon = screen.getByRole('button', { name: 'Abandonner' });
    const rerun = screen.getByRole('button', { name: 'Relancer' });
    expect(abandon.className).toContain('ds-btn--destructive');
    expect(abandon.compareDocumentPosition(rerun)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
    expect(rerun.compareDocumentPosition(publishAndApply)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
    expect(screen.queryByRole('tablist')).not.toBeInTheDocument();
    expect(screen.getByText('groupes')).toBeInTheDocument();
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
      await screen.findByRole('listitem', { name: 'P1 · S1 vs S2' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Belgium')).toBeInTheDocument();
    expect(screen.getByText('Poland')).toBeInTheDocument();
    expect(screen.getByText('Turkey')).toBeInTheDocument();
    expect(screen.getByText('Denmark')).toBeInTheDocument();
    expect(screen.getByText('P1')).toBeInTheDocument();
    expect(screen.getByText('S1 vs S2')).toBeInTheDocument();
    expect(screen.getByText('P2')).toBeInTheDocument();
    expect(screen.getByText('S3 vs S4')).toBeInTheDocument();
    expect(document.querySelector('.ds-interactive-row__chevron')).toBeNull();
    expect(document.querySelector('.draw-confrontation')).toBeNull();
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
      await screen.findByRole('listitem', { name: 'P1 · S1 vs S2' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Belgium')).toBeInTheDocument();
    expect(screen.getByText('Poland')).toBeInTheDocument();
    expect(screen.getByText('S1 vs S2')).toBeInTheDocument();
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

    const leftovers = await screen.findByRole('list', { name: 'Placements' });
    expect(
      within(leftovers).getByRole('listitem', { name: 'Spain · S9' }),
    ).toBeInTheDocument();
    expect(within(leftovers).getByText('Spain')).toBeInTheDocument();
    expect(within(leftovers).queryByText('→')).not.toBeInTheDocument();
    expect(leftovers.querySelector('.ds-match-row')).not.toBeNull();
  });

  it('shows Sans solution as a danger alert and keeps publish hidden', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          resolutionState: 'NoSolution',
          groupPlacements: [],
          actions: hiddenDrawActions(),
        }),
      ]),
    );

    renderDialog();

    const message = await screen.findByText(
      'Aucune résolution valide n’a pu être générée.',
    );
    expect(message.closest('.ds-alert')).toHaveClass('ds-alert--danger');
    expect(screen.getByText('Brouillon')).toBeInTheDocument();
    expect(screen.queryByText('Sans solution')).not.toBeInTheDocument();
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
          actions: appliedDrawActions(),
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
    expect(screen.queryByText('Appliqué')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Refaire le tirage' }).className,
    ).toContain('ds-btn--primary');
  });

  it('keeps the active execution primary and discloses cancelled runs below', async () => {
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

    expect(
      await screen.findByRole('heading', { name: 'Exécution #2' }),
    ).toBeInTheDocument();
    expect(screen.getAllByText('Brouillon').length).toBeGreaterThan(0);
    expect(document.querySelector('.ds-panel .ds-panel')).toBeNull();
    expect(
      screen.queryByLabelText('Historique des exécutions'),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Exécutions précédentes (1)' }),
    ).toBeInTheDocument();
    const cancelledTile = screen.getByRole('button', {
      name: /Exécution #1/,
    });
    expect(
      cancelledTile.closest('.structure-draw-previous__item'),
    ).not.toBeNull();
    expect(cancelledTile.closest('.ds-selectable-tile')).not.toHaveAttribute(
      'data-tone',
    );
    expect(cancelledTile).toHaveAttribute('aria-expanded', 'false');
    expect(cancelledTile).not.toHaveAttribute('aria-controls');

    await user.click(cancelledTile);
    expect(cancelledTile).toHaveAttribute('aria-expanded', 'true');
    expect(cancelledTile).toHaveAttribute('aria-controls');
    expect(
      within(cancelledTile).queryByText('Libérée'),
    ).not.toBeInTheDocument();
    expect(
      within(cancelledTile).queryByText(/non libérés/),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText(
        'Exécution annulée. Un nouveau tirage est nécessaire pour recommencer.',
      ),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Valider le tirage' }),
    ).toBeInTheDocument();
  });

  it('structures previous-run tiles with header badge, Libérée meta, and disclosure state', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-aaa',
            status: 'Cancelled',
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
          groupDraw({ id: 'draw-zzz', status: 'Draft' }),
        ],
        { groups: [{ id: 'g-a', name: 'A', entryIds: ['e2'] }] },
      ),
    );
    renderDialog();
    expect(
      screen.queryByRole('button', { name: /exécutions précédentes/i }),
    ).not.toBeInTheDocument();
    const trigger = await screen.findByRole('button', { name: /Exécution #1/ });
    const tile = trigger.closest('.ds-selectable-tile') as HTMLElement;
    const header = trigger.closest('.structure-draw-previous__header');
    expect(header?.tagName).toBe('H5');
    expect(header?.querySelector(':scope > button')).toBe(trigger);
    const lead = trigger.querySelector('.structure-draw-previous__lead');
    expect(lead).not.toBeNull();
    const meta = lead!.querySelector('.structure-draw-previous__meta');
    expect(meta).not.toBeNull();
    expect(
      lead!.querySelector('.structure-draw-previous__title'),
    ).toBeInTheDocument();
    expect(within(meta as HTMLElement).getByText('Annulé')).toBeInTheDocument();
    expect(
      within(meta as HTMLElement).getByText('Libérée'),
    ).toBeInTheDocument();
    const chevron = trigger.querySelector(
      ':scope > .ds-selectable-tile__chevron',
    );
    expect(chevron).toBeTruthy();
    expect(lead!.contains(chevron)).toBe(false);
    expect(
      lead!.compareDocumentPosition(chevron!) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(tile).toHaveAttribute('data-selected', 'false');
    await user.click(trigger);
    expect(tile).toHaveAttribute('data-selected', 'true');
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
          actions: appliedDrawActions(),
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
    expect(
      screen.queryByRole('button', { name: 'Annuler le tirage' }),
    ).not.toBeInTheDocument();
  });

  it('hides Abandonner when competition is Completed (D)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft', actions: hiddenDrawActions() })]),
    );

    renderDialog(groupsStage(), true, 'Completed');

    expect(
      await screen.findByRole('heading', { name: 'Exécution #1' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Abandonner' }),
    ).not.toBeInTheDocument();
  });

  it('hides Abandonner when stage is Running (D)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([groupDraw({ status: 'Draft', actions: hiddenDrawActions() })], {
        status: 'Running',
      }),
    );

    renderDialog(groupsStage({ status: 'Running' }), true, 'Running');

    expect(
      await screen.findByRole('heading', { name: 'Exécution #1' }),
    ).toBeInTheDocument();
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
            actions: releaseAvailable(1),
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
        'draw-slot-1',
      ),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockResolvedValue({
      releasedCount: 1,
      skippedCount: 1,
    });

    renderDialog(cupStage());

    expect(await screen.findByText('1/2 non libérés')).toBeInTheDocument();
    const releaseButton = screen.getByRole('button', {
      name: 'Libérer les 1 équipes tirées',
    });
    expect(releaseButton.closest('.structure-draw-footer')).not.toBeNull();
    expect(releaseButton.closest('summary')).toBeNull();
    expect(
      screen.queryByText(/placements encore présents/),
    ).not.toBeInTheDocument();
    await user.click(releaseButton);
    const confirm = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', {
        name: 'Libérer les 1 équipes tirées',
      }),
    );

    await waitFor(() => {
      expect(releaseDrawAlignedPlacements).toHaveBeenCalledWith(
        stageId,
        'draw-slot-1',
      );
    });
    expect(scheduleStructureRefresh).toHaveBeenCalledWith(
      expect.anything(),
      competitionId,
      ['hubStageShell'],
      { stageId },
    );
    await waitFor(() => {
      expect(getToastsSnapshot().map((t) => t.message)).toContain(
        'Placements libérés (1).',
      );
    });
  });

  it('targets the newest releasable cancelled draw from the footer', async () => {
    const user = userEvent.setup();
    const slot = (slotKey: string, entryId: string, displayName: string) => ({
      slotKey,
      entryId,
      displayName,
      coveredByCompleteFixture: false,
    });
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            id: 'draw-aaa',
            status: 'Cancelled',
            actions: releaseAvailable(2),
            slotPlacements: [
              { slotKey: 'S1', entryId: 'e1', displayName: 'Older A' },
              { slotKey: 'S2', entryId: 'e2', displayName: 'Older B' },
            ],
          }),
          slotDraw({
            id: 'draw-zzz',
            status: 'Cancelled',
            actions: releaseAvailable(1),
            slotPlacements: [
              { slotKey: 'S3', entryId: 'e3', displayName: 'Newer' },
            ],
          }),
        ],
        {
          slots: [
            slot('S1', 'e1', 'Older A'),
            slot('S2', 'e2', 'Older B'),
            slot('S3', 'e3', 'Newer'),
          ],
        },
        'draw-zzz',
      ),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockResolvedValue({
      releasedCount: 1,
      skippedCount: 0,
    });

    renderDialog(cupStage());

    const footer = document.querySelector('.structure-draw-footer');
    expect(footer).not.toBeNull();
    const footerRelease = await within(footer as HTMLElement).findByRole(
      'button',
      { name: 'Libérer les 1 équipes tirées' },
    );
    expect(
      within(footer as HTMLElement).queryByRole('button', {
        name: 'Libérer les 2 équipes tirées',
      }),
    ).not.toBeInTheDocument();

    await user.click(footerRelease);
    const confirm = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', {
        name: 'Libérer les 1 équipes tirées',
      }),
    );

    await waitFor(() => {
      expect(releaseDrawAlignedPlacements).toHaveBeenCalledWith(
        stageId,
        'draw-zzz',
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
      await screen.findByText('Aucun tirage en cours'),
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
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({ id: 'draw-aaa', status: 'Cancelled' }),
        groupDraw({ id: 'draw-zzz', status: 'Draft' }),
      ]),
    );
    vi.mocked(rerunDrawGeneration).mockResolvedValue({
      drawId: 'draw-zzz',
      isResolved: true,
      isNoSolution: false,
    });

    renderDialog();
    await user.click(await screen.findByRole('button', { name: 'Relancer' }));

    await waitFor(() => {
      expect(rerunDrawGeneration).toHaveBeenCalledWith(
        stageId,
        'draw-zzz',
        'Group',
      );
    });
    expect(createAndGenerateDraw).not.toHaveBeenCalled();
    await waitFor(() => {
      expect(getToastsSnapshot().map((toast) => toast.message)).toContain(
        'Tirage relancé.',
      );
    });
    expect(
      screen.getByRole('heading', { name: 'Exécution #2' }),
    ).toBeInTheDocument();
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
    expect(rerunDrawGeneration).toHaveBeenCalledWith(
      stageId,
      'draw-1',
      'Group',
    );
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

  it('allows Refaire for an applied group draw while the phase is Ready', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            status: 'Published',
            isApplied: true,
            actions: appliedDrawActions(),
          }),
        ],
        { status: 'Ready' },
      ),
    );

    renderDialog(groupsStage({ status: 'Ready' }));

    const redo = await screen.findByRole('button', {
      name: 'Refaire le tirage',
    });
    expect(redo.className).toContain('ds-btn--primary');
    expect(
      screen.queryByRole('button', { name: 'Annuler le tirage' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText(/n’est pas en brouillon/),
    ).not.toBeInTheDocument();
  });

  it('refuses Cup Refaire when a touched pair has an attached match', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Published',
            isApplied: true,
            actions: redoBlockedActions('Stage.MaterializedOpponentConflict'),
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
    const cancelApplied = screen.getByRole('button', {
      name: 'Annuler le tirage',
    });
    expect(cancelApplied.className).toContain('ds-btn--destructive');
    expect(cancelApplied.className).not.toContain('ds-btn--primary');
  });

  it('redoes an applied draw through the single atomic redo endpoint', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          isApplied: true,
          actions: appliedDrawActions(),
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
    vi.mocked(redoDraw).mockResolvedValue({
      drawId: 'draw-next',
      isResolved: true,
      isNoSolution: false,
      status: 'Draft',
      resolutionState: 'Resolved',
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
      expect(redoDraw).toHaveBeenCalledWith(stageId, 'draw-1');
    });
    expect(redoDraw).toHaveBeenCalledTimes(1);
    expect(cancelDraw).not.toHaveBeenCalled();
    expect(releaseDrawAlignedPlacements).not.toHaveBeenCalled();
    expect(createAndGenerateDraw).not.toHaveBeenCalled();
  });

  it('shows an obsolete badge when the draw result is stale', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          isApplied: true,
          isObsoleteForConsumption: true,
          actions: appliedDrawActions(),
        }),
      ]),
    );

    renderDialog();

    expect(await screen.findByText('Obsolète')).toBeInTheDocument();
    expect(
      screen.queryByText(/ne correspond plus à la phase/),
    ).not.toBeInTheDocument();
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

  it('cancels an applied draw from a confirmation when Refaire is blocked', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Published',
            isApplied: true,
            actions: redoBlockedActions('Stage.MaterializedOpponentConflict'),
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
    vi.mocked(cancelDraw).mockResolvedValue(undefined);

    renderDialog(
      cupStage({
        compositionEntryCount: 4,
        compositionCapacity: 4,
      }),
    );

    const cancelApplied = await screen.findByRole('button', {
      name: 'Annuler le tirage',
    });
    expect(cancelApplied.className).toContain('ds-btn--destructive');
    expect(cancelApplied.className).not.toContain('ds-btn--primary');
    expect(
      screen.queryByRole('button', { name: 'Refaire le tirage' }),
    ).not.toBeInTheDocument();

    await user.click(cancelApplied);
    expect(cancelDraw).not.toHaveBeenCalled();
    const confirm = await screen.findByRole('dialog', {
      name: 'Annuler le tirage ?',
    });
    expect(confirm).toHaveTextContent(/ne retire pas les placements/);
    await user.click(
      within(confirm).getByRole('button', { name: 'Annuler le tirage' }),
    );

    await waitFor(() => {
      expect(cancelDraw).toHaveBeenCalledWith(stageId, 'draw-slot-1');
    });
  });

  it('hides Annuler le tirage when the phase can no longer cancel', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            status: 'Published',
            isApplied: true,
            actions: drawActions({
              redo: { state: 'Blocked', blockCode: 'Stage.StructureLocked' },
            }),
          }),
        ],
        { status: 'Running' },
      ),
    );

    renderDialog(groupsStage({ status: 'Running' }), true, 'Running');

    expect(
      await screen.findByText(/dans l’état actuel de la phase/),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Annuler le tirage' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Refaire le tirage' }),
    ).not.toBeInTheDocument();
  });

  it('omits the unreleased alert when a cancelled draw cannot be released', async () => {
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
          status: 'Running',
          slots: [
            {
              slotKey: 'R16-1-A',
              entryId: 'e1',
              displayName: 'Belgium',
              coveredByCompleteFixture: false,
            },
          ],
        },
      ),
    );

    renderDialog(cupStage({ status: 'Running' }), true, 'Running');

    expect(
      await screen.findByText('Aucun tirage en cours'),
    ).toBeInTheDocument();
    expect(
      screen.queryByText('Annulé, placements non libérés'),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Libérer/ }),
    ).not.toBeInTheDocument();
  });

  it('keeps the confirm dialog open with an inline error when the atomic redo is refused', async () => {
    const user = userEvent.setup();
    const placement = {
      groupId: 'g-a',
      groupDisplayName: 'A',
      entryId: 'e1',
      displayName: 'Équipe 1',
    };
    const groups = [{ id: 'g-a', name: 'A', entryIds: ['e1'] }];
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            status: 'Published',
            isApplied: true,
            groupPlacements: [placement],
            actions: appliedDrawActions(),
          }),
        ],
        { groups },
      ),
    );

    vi.mocked(redoDraw).mockRejectedValue(
      new ApiError(
        409,
        'Conflict',
        'redo refused',
        'Application.DrawReleaseBlockedByAttachedMatches',
      ),
    );

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Refaire le tirage' }),
    );
    const confirm = await screen.findByRole('dialog', {
      name: 'Refaire le tirage ?',
    });
    expect(
      within(confirm).getByRole('button', { name: 'Refaire le tirage' })
        .className,
    ).toContain('ds-btn--destructive');
    await user.click(
      within(confirm).getByRole('button', { name: 'Refaire le tirage' }),
    );

    expect(redoDraw).toHaveBeenCalledWith(stageId, 'draw-1');
    expect(redoDraw).toHaveBeenCalledTimes(1);
    expect(
      await within(confirm).findByText(/Détachez-les/),
    ).toBeInTheDocument();
    expect(
      await screen.findByRole('dialog', { name: 'Refaire le tirage ?' }),
    ).toBeInTheDocument();
    expect(cancelDraw).not.toHaveBeenCalled();
    expect(releaseDrawAlignedPlacements).not.toHaveBeenCalled();
    expect(createAndGenerateDraw).not.toHaveBeenCalled();
    expect(screen.queryByText('1/1 non libérés')).not.toBeInTheDocument();
  });

  it('focuses Lancer, then Valider after a draft appears', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));
    const { queryClient } = renderDialog();
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Lancer le tirage' }),
      ).toHaveFocus();
    });

    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    await queryClient.invalidateQueries();
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Valider le tirage' }),
      ).toHaveFocus();
    });
  });

  it('focuses Appliquer for a published draw that is not applied', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          resolutionState: 'Resolved',
          isApplied: false,
          actions: applyAvailableActions(),
        }),
      ]),
    );
    renderDialog();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Appliquer' })).toHaveFocus();
    });
  });

  it('focuses Refaire for an applied draw', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Published',
          isApplied: true,
          actions: appliedDrawActions(),
        }),
      ]),
    );
    renderDialog();
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Refaire le tirage' }),
      ).toHaveFocus();
    });
  });

  it('focuses the header close when Refaire is blocked', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Published',
            isApplied: true,
            actions: redoBlockedActions('Stage.MaterializedOpponentConflict'),
          }),
        ],
        {
          bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
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
          ],
          rounds: [
            {
              id: 'round-1',
              name: 'Tour',
              fixtures: [
                {
                  id: 'fx',
                  slotAKey: 'S1',
                  slotBKey: 'S2',
                  attachments: [{ matchId: 'm1', legIndex: 0 }],
                },
              ],
            },
          ],
        },
      ),
    );
    renderDialog(
      cupStage({ compositionEntryCount: 4, compositionCapacity: 4 }),
    );
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Annuler le tirage' }),
      ).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Fermer' })).toHaveFocus();
    });
    expect(
      screen.getByRole('button', { name: 'Annuler le tirage' }),
    ).not.toHaveFocus();
  });

  it('opens and closes a single cancelled disclosure tile', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          id: 'draw-a',
          status: 'Cancelled',
          groupPlacements: [
            {
              groupId: 'g-a',
              groupDisplayName: 'A',
              entryId: 'e1',
              displayName: 'Alpha',
            },
          ],
        }),
        groupDraw({
          id: 'draw-b',
          status: 'Draft',
          groupPlacements: [
            {
              groupId: 'g-b',
              groupDisplayName: 'B',
              entryId: 'e2',
              displayName: 'Beta',
            },
          ],
        }),
      ]),
    );
    renderDialog();
    expect(await screen.findByText('Beta')).toBeInTheDocument();
    expect(screen.queryByText('Alpha')).not.toBeInTheDocument();

    const cancelledTile = screen.getByRole('button', {
      name: /Exécution #1/,
    });
    await user.click(cancelledTile);
    expect(cancelledTile).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(screen.getByText('Beta')).toBeInTheDocument();

    await user.click(cancelledTile);
    expect(cancelledTile).toHaveAttribute('aria-expanded', 'false');
    expect(screen.queryByText('Alpha')).not.toBeInTheDocument();
  });

  it('does not move focus when a cancelled tile is toggled', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({ id: 'draw-a', status: 'Cancelled' }),
        groupDraw({ id: 'draw-b', status: 'Draft' }),
      ]),
    );
    renderDialog();
    const validate = await screen.findByRole('button', {
      name: 'Valider le tirage',
    });
    await waitFor(() => {
      expect(validate).toHaveFocus();
    });
    const cancelledTile = screen.getByRole('button', {
      name: /Exécution #1/,
    });
    await user.click(cancelledTile);
    expect(cancelledTile).toHaveFocus();
    expect(
      screen.queryByRole('button', { name: 'Lancer le tirage' }),
    ).not.toBeInTheDocument();
    expect(validate).not.toHaveFocus();
  });

  it('returns focus to Lancer after Libérer succeeds', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    const placement = {
      slotKey: 'S1',
      entryId: 'e1',
      displayName: 'Belgium',
    };
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            id: 'draw-old',
            status: 'Cancelled',
            actions: releaseAvailable(1),
            slotPlacements: [placement],
          }),
        ],
        {
          slots: [
            {
              slotKey: 'S1',
              entryId: 'e1',
              displayName: 'Belgium',
              coveredByCompleteFixture: false,
            },
          ],
        },
        'draw-old',
      ),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview(
          [
            slotDraw({
              id: 'draw-old',
              status: 'Cancelled',
              actions: hiddenDrawActions(),
              slotPlacements: [placement],
            }),
          ],
          {
            slots: [
              {
                slotKey: 'S1',
                entryId: null,
                displayName: null,
                coveredByCompleteFixture: false,
              },
            ],
          },
          null,
        ),
      );
      return { releasedCount: 1, skippedCount: 0 };
    });
    renderDialog(
      cupStage({
        compositionEntryCount: 16,
        compositionCapacity: 16,
      }),
    );
    const release = await screen.findByRole('button', {
      name: 'Libérer les 1 équipes tirées',
    });
    release.focus();
    await user.click(release);
    const confirm = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', {
        name: 'Libérer les 1 équipes tirées',
      }),
    );
    await waitFor(() => {
      expect(
        screen.queryByRole('button', { name: /Libérer/ }),
      ).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Lancer le tirage' }),
      ).toBeEnabled();
    });
    await vi.advanceTimersByTimeAsync(160);
    expect(
      screen.getByRole('button', { name: 'Lancer le tirage' }),
    ).toHaveFocus();
    expect(screen.getByRole('button', { name: 'Fermer' })).not.toHaveFocus();
    vi.useRealTimers();
  });

  describe('release footer focus (B2)', () => {
    afterEach(() => {
      vi.useRealTimers();
    });

    it('shows Libérer on the older cancelled draw once the newer draw is cleared', async () => {
      const placementOld = {
        groupId: 'g-a',
        groupDisplayName: 'A',
        entryId: 'e1',
        displayName: 'Older',
      };
      const payload = overview(
        [
          groupDraw({
            id: 'draw-aaa',
            status: 'Cancelled',
            groupPlacements: [placementOld],
            actions: releaseAvailable(1),
          }),
          groupDraw({
            id: 'draw-zzz',
            status: 'Cancelled',
            groupPlacements: [],
          }),
        ],
        {
          groups: [
            { id: 'g-a', name: 'A', entryIds: ['e1'] },
            { id: 'g-b', name: 'B', entryIds: [] },
          ],
        },
        'draw-aaa',
      );
      vi.mocked(fetchStageOverview).mockResolvedValue(payload);
      renderDialog(groupsStage());
      expect(
        await screen.findByRole('button', { name: 'Libérer #1 (1/1)' }),
      ).toBeInTheDocument();
    });

    it('focuses the next Libérer when another cancelled draw stays releasable', async () => {
      vi.useFakeTimers({ shouldAdvanceTime: true });
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      const placementOld = {
        groupId: 'g-a',
        groupDisplayName: 'A',
        entryId: 'e1',
        displayName: 'Older',
      };
      const placementNew = {
        groupId: 'g-b',
        groupDisplayName: 'B',
        entryId: 'e2',
        displayName: 'Newer',
      };
      const beforeRelease = overview(
        [
          groupDraw({
            id: 'draw-aaa',
            status: 'Cancelled',
            groupPlacements: [placementOld],
            actions: releaseAvailable(1),
          }),
          groupDraw({
            id: 'draw-zzz',
            status: 'Cancelled',
            groupPlacements: [placementNew],
            actions: releaseAvailable(1),
          }),
        ],
        {
          groups: [
            { id: 'g-a', name: 'A', entryIds: ['e1'] },
            { id: 'g-b', name: 'B', entryIds: ['e2'] },
          ],
        },
        'draw-zzz',
      );
      const afterRelease = overview(
        [
          groupDraw({
            id: 'draw-aaa',
            status: 'Cancelled',
            groupPlacements: [placementOld],
            actions: releaseAvailable(1),
          }),
          groupDraw({
            id: 'draw-zzz',
            status: 'Cancelled',
            groupPlacements: [],
          }),
        ],
        {
          groups: [
            { id: 'g-a', name: 'A', entryIds: ['e1'] },
            { id: 'g-b', name: 'B', entryIds: [] },
          ],
        },
        'draw-aaa',
      );
      vi.mocked(releaseDrawAlignedPlacements).mockResolvedValue({
        releasedCount: 1,
        skippedCount: 0,
      });
      vi.mocked(fetchStageOverview).mockImplementation(async () => {
        if (vi.mocked(releaseDrawAlignedPlacements).mock.calls.length > 0) {
          return afterRelease;
        }
        return beforeRelease;
      });
      renderDialog(groupsStage());
      await user.click(
        await screen.findByRole('button', { name: 'Libérer #2 (1/1)' }),
      );
      const confirm = await screen.findByRole('dialog', {
        name: 'Libérer les placements du tirage ?',
      });
      await user.click(
        within(confirm).getByRole('button', {
          name: 'Libérer les placements du tirage',
        }),
      );
      await waitFor(() => {
        expect(
          screen.queryByRole('dialog', {
            name: 'Libérer les placements du tirage ?',
          }),
        ).not.toBeInTheDocument();
      });
      await waitFor(() => {
        expect(
          screen.getByRole('button', { name: 'Libérer #1 (1/1)' }),
        ).toBeInTheDocument();
      });
      await vi.advanceTimersByTimeAsync(160);
      await waitFor(() => {
        expect(
          screen.getByRole('button', { name: 'Libérer #1 (1/1)' }),
        ).toHaveFocus();
      });
    });

    it('focuses Libérer after a partial release on the same draw', async () => {
      vi.useFakeTimers({ shouldAdvanceTime: true });
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview(
          [
            slotDraw({
              status: 'Cancelled',
              actions: releaseAvailable(1),
              slotPlacements: [
                { slotKey: 'S1', entryId: 'e1', displayName: 'A' },
                { slotKey: 'S2', entryId: 'e2', displayName: 'B' },
              ],
            }),
          ],
          {
            slots: [
              {
                slotKey: 'S1',
                entryId: 'e1',
                displayName: 'A',
                coveredByCompleteFixture: false,
              },
              {
                slotKey: 'S2',
                entryId: 'e9',
                displayName: 'Other',
                coveredByCompleteFixture: false,
              },
            ],
          },
          'draw-slot-1',
        ),
      );
      vi.mocked(releaseDrawAlignedPlacements).mockResolvedValue({
        releasedCount: 1,
        skippedCount: 1,
      });
      renderDialog(cupStage());
      await user.click(
        await screen.findByRole('button', {
          name: 'Libérer les 1 équipes tirées',
        }),
      );
      const confirm = await screen.findByRole('dialog', {
        name: 'Libérer les placements du tirage ?',
      });
      await user.click(
        within(confirm).getByRole('button', {
          name: 'Libérer les 1 équipes tirées',
        }),
      );
      await waitFor(() => {
        expect(
          screen.queryByRole('dialog', {
            name: 'Libérer les placements du tirage ?',
          }),
        ).not.toBeInTheDocument();
      });
      await vi.advanceTimersByTimeAsync(160);
      await waitFor(() => {
        expect(
          screen.getByRole('button', { name: 'Libérer les 1 équipes tirées' }),
        ).toHaveFocus();
      });
    });

    it('focuses footer after a failed release once the confirm closes', async () => {
      vi.useFakeTimers({ shouldAdvanceTime: true });
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      const placement = {
        groupId: 'g-a',
        groupDisplayName: 'A',
        entryId: 'e1',
        displayName: 'Alpha',
      };
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview(
          [
            groupDraw({
              status: 'Cancelled',
              groupPlacements: [placement],
              actions: releaseAvailable(1),
            }),
          ],
          { groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }] },
          'draw-1',
        ),
      );
      vi.mocked(releaseDrawAlignedPlacements).mockRejectedValue(
        new ApiError(409, 'Conflict', 'fail', 'Application.DrawReleaseBlocked'),
      );
      renderDialog(groupsStage());
      await user.click(
        await screen.findByRole('button', { name: 'Libérer #1 (1/1)' }),
      );
      const confirm = await screen.findByRole('dialog', {
        name: 'Libérer les placements du tirage ?',
      });
      await user.click(
        within(confirm).getByRole('button', {
          name: 'Libérer les placements du tirage',
        }),
      );
      await waitFor(() => {
        expect(within(confirm).getByRole('alert')).toBeInTheDocument();
      });
      await user.click(
        within(confirm).getAllByRole('button', { name: 'Fermer' })[0]!,
      );
      await waitFor(() => {
        expect(
          screen.queryByRole('dialog', {
            name: 'Libérer les placements du tirage ?',
          }),
        ).not.toBeInTheDocument();
      });
      const main = screen
        .getByRole('heading', { name: /Exécutions de tirage/ })
        .closest('[role="dialog"]') as HTMLElement;
      const dialogBody = main.querySelector('.structure-draw-dialog');
      expect(dialogBody).not.toBeNull();
      expect(
        within(dialogBody as HTMLElement).getByRole('alert'),
      ).toBeInTheDocument();
      await vi.advanceTimersByTimeAsync(160);
      await waitFor(() => {
        expect(
          screen.getByRole('button', { name: 'Libérer #1 (1/1)' }),
        ).toHaveFocus();
        expect(document.body).not.toHaveFocus();
      });
    });
  });

  it('disables Libérer and blocks Refaire when a Ready group has matches', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-live',
            status: 'Published',
            isApplied: true,
            actions: redoBlockedActions(
              'Application.DrawReleaseBlockedByAttachedMatches',
            ),
          }),
          groupDraw({
            id: 'draw-old',
            status: 'Cancelled',
            actions: releaseBlocked(
              1,
              'Application.DrawReleaseBlockedByAttachedMatches',
            ),
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
        ],
        {
          status: 'Ready',
          groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }],
        },
        'draw-old',
      ),
    );
    renderDialog(groupsStage({ status: 'Ready', matchCount: 2 }));
    expect(
      await screen.findByText(/ne peut pas être refait : des matchs/),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Refaire le tirage' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Annuler le tirage' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Libérer/ }),
    ).not.toBeInTheDocument();
  });

  it('keeps Libérer visible and disabled when a cancelled-only group is Ready with matches', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-old',
            status: 'Cancelled',
            actions: releaseBlocked(
              1,
              'Application.DrawReleaseBlockedByAttachedMatches',
            ),
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
        ],
        {
          status: 'Ready',
          groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }],
        },
        'draw-old',
      ),
    );
    renderDialog(groupsStage({ status: 'Ready', matchCount: 2 }));
    const release = await screen.findByRole('button', {
      name: 'Libérer #1 (1/1)',
    });
    expect(release).toBeDisabled();
    const reason = releaseAttachedMatchesError;
    const tile = screen.getByRole('button', {
      name: /Exécution #1/,
    });
    expect(within(tile).getByText('1/1 non libérés')).toBeInTheDocument();
    expect(within(tile).queryByText(reason)).not.toBeInTheDocument();
    expect(screen.getAllByText(reason)).toHaveLength(1);
    expect(
      screen.queryByRole('button', { name: 'Refaire le tirage' }),
    ).not.toBeInTheDocument();
  });

  it('shows the server 409 ahead of the client release reason', async () => {
    const user = userEvent.setup();
    const placement = {
      groupId: 'g-a',
      groupDisplayName: 'A',
      entryId: 'e1',
      displayName: 'Alpha',
    };
    const groups = [{ id: 'g-a', name: 'A', entryIds: ['e1'] }];
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            status: 'Cancelled',
            actions: releaseAvailable(1),
            groupPlacements: [placement],
          }),
        ],
        { groups },
        'draw-1',
      ),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview(
          [
            groupDraw({
              status: 'Cancelled',
              actions: releaseBlocked(
                1,
                'Application.DrawReleaseBlockedByAttachedMatches',
              ),
              groupPlacements: [placement],
            }),
          ],
          { status: 'Ready', groups },
          'draw-1',
        ),
      );
      throw new ApiError(
        409,
        'Conflict',
        'release failed',
        'Application.DrawReleaseBlockedByAttachedMatches',
      );
    });
    renderDialog(groupsStage({ matchCount: 2 }));
    await user.click(
      await screen.findByRole('button', { name: 'Libérer #1 (1/1)' }),
    );
    const confirm = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', {
        name: 'Libérer les placements du tirage',
      }),
    );
    const main = screen
      .getByRole('heading', { name: /Exécutions de tirage/ })
      .closest('[role="dialog"]') as HTMLElement;
    await waitFor(() => {
      expect(within(main).getAllByText(/Détachez-les/).length).toBeGreaterThan(
        0,
      );
    });
    expect(
      within(main).queryByText(/Libérer est indisponible/),
    ).not.toBeInTheDocument();
  });

  it('prefers a mutation error over the Refaire blocked reason', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Published',
            isApplied: true,
            actions: redoBlockedActions('Stage.MaterializedOpponentConflict'),
          }),
        ],
        {
          bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
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
          ],
          rounds: [
            {
              id: 'round-1',
              name: 'Tour',
              fixtures: [
                {
                  id: 'fx',
                  slotAKey: 'S1',
                  slotBKey: 'S2',
                  attachments: [{ matchId: 'm1', legIndex: 0 }],
                },
              ],
            },
          ],
        },
      ),
    );
    vi.mocked(cancelDraw).mockRejectedValue(
      new ApiError(
        409,
        'Conflict',
        'blocked',
        'Application.DrawReleaseFailure',
      ),
    );
    renderDialog(
      cupStage({ compositionEntryCount: 4, compositionCapacity: 4 }),
    );
    expect(
      await screen.findByText(/un match est rattaché/),
    ).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Annuler le tirage' }));
    const confirm = await screen.findByRole('dialog', {
      name: 'Annuler le tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', { name: 'Annuler le tirage' }),
    );
    const main = screen
      .getByRole('heading', { name: /Exécutions de tirage/ })
      .closest('[role="dialog"]') as HTMLElement;
    expect(
      await within(main).findByText(
        'Impossible de libérer les placements du tirage.',
      ),
    ).toBeInTheDocument();
    expect(
      within(main).queryByText(/un match est rattaché/),
    ).not.toBeInTheDocument();
  });

  it('explains release is blocked when a published draw occupies the phase', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-live',
            status: 'Published',
            isApplied: true,
            actions: appliedDrawActions(),
          }),
          groupDraw({
            id: 'draw-old',
            status: 'Cancelled',
            actions: releaseBlocked(1, 'Application.DrawReleaseFailure'),
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
        ],
        { groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }] },
      ),
    );
    renderDialog();
    const reason = 'Libération impossible : un tirage publié occupe la phase.';
    const cancelled = await screen.findByRole('button', {
      name: /Exécution #2/,
    });
    expect(within(cancelled).getByText('1/1 non libérés')).toBeInTheDocument();
    expect(within(cancelled).queryByText(reason)).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Libérer/ }),
    ).not.toBeInTheDocument();

    await user.click(cancelled);
    expect(screen.getByText(reason)).toBeInTheDocument();
    expect(screen.getAllByText('1/1 non libérés').length).toBeGreaterThan(0);
    expect(
      screen.queryByRole('button', { name: /Libérer/ }),
    ).not.toBeInTheDocument();
  });

  it('does not mark a cancelled execution as obsolete', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          id: 'draw-live',
          status: 'Published',
          isApplied: true,
          isObsoleteForConsumption: true,
          actions: appliedDrawActions(),
        }),
        groupDraw({
          id: 'draw-old',
          status: 'Cancelled',
          isObsoleteForConsumption: true,
        }),
      ]),
    );
    renderDialog();
    const cancelled = await screen.findByRole('button', {
      name: /Exécution #2/,
    });
    expect(cancelled).not.toHaveTextContent('Obsolète');
    await user.click(cancelled);
    expect(screen.getAllByText('Obsolète')).toHaveLength(1);
  });

  it('resets the selected execution when the dialog reopens', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({ id: 'draw-aaa', status: 'Cancelled' }),
        groupDraw({ id: 'draw-zzz', status: 'Draft' }),
      ]),
    );
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    function Harness() {
      const [open, setOpen] = useState(true);
      return (
        <QueryClientProvider client={queryClient}>
          <button type="button" onClick={() => setOpen(false)}>
            Fermer le dialogue
          </button>
          <button type="button" onClick={() => setOpen(true)}>
            Rouvrir
          </button>
          <StructureDrawDialog
            open={open}
            onClose={() => setOpen(false)}
            competitionId={competitionId}
            competitionStatus="Draft"
            stage={groupsStage()}
            structureStages={[groupsStage()]}
          />
        </QueryClientProvider>
      );
    }
    render(<Harness />);
    const cancelled = await screen.findByRole('button', {
      name: /Exécution #1/,
    });
    await user.click(cancelled);
    expect(cancelled).toHaveAttribute('aria-expanded', 'true');
    await user.click(
      screen.getByRole('button', { name: 'Fermer le dialogue' }),
    );
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    await user.click(screen.getByRole('button', { name: 'Rouvrir' }));
    expect(
      screen.getByRole('button', { name: /Exécution #1/ }),
    ).toHaveAttribute('aria-expanded', 'false');
  });

  it('shows attached-match guidance instead of release-first copy when Libérer is disabled', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-old',
            status: 'Cancelled',
            actions: releaseBlocked(
              1,
              'Application.DrawReleaseBlockedByAttachedMatches',
            ),
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
        ],
        {
          status: 'Ready',
          groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }],
        },
        'draw-old',
      ),
    );
    renderDialog(groupsStage({ status: 'Ready', matchCount: 2 }));
    expect(
      await screen.findByText('Aucun tirage en cours'),
    ).toBeInTheDocument();
    expect(screen.getAllByText(releaseAttachedMatchesError)).toHaveLength(1);
    expect(
      screen.queryByText(/Libérez d'abord les placements/),
    ).not.toBeInTheDocument();
  });

  it('keeps the footer unchanged when a disclosure tile is toggled', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({ id: 'draw-a', status: 'Cancelled' }),
        groupDraw({ id: 'draw-b', status: 'Draft' }),
      ]),
    );
    renderDialog();
    await screen.findByRole('button', { name: 'Relancer' });
    const footer = document.querySelector('.structure-draw-footer');
    expect(footer).not.toBeNull();
    const before = (footer as HTMLElement).innerHTML;
    await user.click(
      await screen.findByRole('button', { name: /Exécution #1/ }),
    );
    expect((footer as HTMLElement).innerHTML).toBe(before);
  });

  it('allows only one cancelled disclosure tile to stay open', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({ id: 'draw-a', status: 'Cancelled' }),
        groupDraw({ id: 'draw-b', status: 'Cancelled' }),
        groupDraw({ id: 'draw-c', status: 'Draft' }),
      ]),
    );
    renderDialog();
    const first = await screen.findByRole('button', {
      name: /Exécution #1/,
    });
    const second = await screen.findByRole('button', {
      name: /Exécution #2/,
    });
    await user.click(first);
    expect(first).toHaveAttribute('aria-expanded', 'true');
    expect(second).toHaveAttribute('aria-expanded', 'false');
    await user.click(second);
    expect(second).toHaveAttribute('aria-expanded', 'true');
    expect(first).toHaveAttribute('aria-expanded', 'false');
  });

  it('shows noActiveBody when cancelled draws are not releasable', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          status: 'Cancelled',
          groupPlacements: [],
        }),
      ]),
    );
    renderDialog();
    expect(
      await screen.findByText('Aucun tirage en cours'),
    ).toBeInTheDocument();
    expect(screen.getByText('Lancez un nouveau tirage')).toBeInTheDocument();
  });

  it('shows noActiveReleaseFirst when a releasable cancelled draw blocks launch', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-old',
            status: 'Cancelled',
            actions: releaseAvailable(1),
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
        ],
        { groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }] },
        'draw-old',
      ),
    );
    renderDialog();
    expect(
      await screen.findByText(
        "Libérez d'abord les placements de l'exécution #1",
      ),
    ).toBeInTheDocument();
  });

  it('shows releaseHeldByActive in cancelled detail when an active draft blocks release', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-aaa',
            status: 'Cancelled',
            actions: releaseAvailable(1),
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
          groupDraw({ id: 'draw-zzz', status: 'Draft' }),
        ],
        { groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }] },
        'draw-aaa',
      ),
    );
    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: /Exécution #1/ }),
    );
    expect(
      screen.getByText(
        "Abandonnez l'exécution active pour libérer ces placements",
      ),
    ).toBeInTheDocument();
  });

  it('returns focus to Lancer after Libérer without leaving focus on the body', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-old',
            status: 'Cancelled',
            actions: releaseAvailable(1),
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
          }),
        ],
        { groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }] },
        'draw-old',
      ),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockImplementation(async () => {
      vi.mocked(fetchStageOverview).mockResolvedValue(
        overview(
          [
            groupDraw({
              id: 'draw-old',
              status: 'Cancelled',
              actions: hiddenDrawActions(),
              groupPlacements: [
                {
                  groupId: 'g-a',
                  groupDisplayName: 'A',
                  entryId: 'e1',
                  displayName: 'Alpha',
                },
              ],
            }),
          ],
          { groups: [{ id: 'g-a', name: 'A', entryIds: [] }] },
          null,
        ),
      );
      return { releasedCount: 1, skippedCount: 0 };
    });
    renderDialog(groupsStage());
    await user.click(
      await screen.findByRole('button', { name: 'Libérer #1 (1/1)' }),
    );
    const confirm = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', {
        name: 'Libérer les placements du tirage',
      }),
    );
    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', {
          name: 'Libérer les placements du tirage ?',
        }),
      ).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Lancer le tirage' }),
      ).toBeEnabled();
    });
    await vi.advanceTimersByTimeAsync(160);
    expect(
      screen.getByRole('button', { name: 'Lancer le tirage' }),
    ).toHaveFocus();
    expect(document.body).not.toHaveFocus();
    vi.useRealTimers();
  });

  it('restores footer focus after Relancer when focus falls back to the body', async () => {
    const user = userEvent.setup();
    let finish: (value: {
      drawId: string;
      isResolved: boolean;
      isNoSolution: boolean;
    }) => void = () => undefined;
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([groupDraw()]));
    vi.mocked(rerunDrawGeneration).mockImplementation(
      () =>
        new Promise((resolve) => {
          finish = resolve;
        }),
    );
    renderDialog();
    const rerun = await screen.findByRole('button', { name: 'Relancer' });
    await user.click(rerun);
    await waitFor(() => {
      expect(rerun).toBeDisabled();
    });
    document.body.tabIndex = -1;
    document.body.focus();
    expect(document.body).toHaveFocus();
    finish({
      drawId: 'draw-1',
      isResolved: true,
      isNoSolution: false,
    });
    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Valider le tirage' }),
      ).toHaveFocus();
    });
    expect(screen.getByRole('button', { name: 'Relancer' })).not.toHaveFocus();
    expect(
      screen.getByRole('button', { name: 'Abandonner' }),
    ).not.toHaveFocus();
  });

  it('renders Abandonner from server actions when the phase is Running', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            status: 'Draft',
            actions: {
              cancel: { state: 'Available' },
              release: { state: 'Hidden', releasableCount: 0 },
              apply: { state: 'Hidden' },
              publishAndApply: { state: 'Hidden' },
              redo: { state: 'Hidden' },
            },
          }),
        ],
        { status: 'Running' },
      ),
    );

    renderDialog(groupsStage({ status: 'Running' }), true, 'Running');

    expect(
      await screen.findByRole('button', { name: 'Abandonner' }),
    ).toBeInTheDocument();
  });

  it('keeps Libérer visible and disabled from a Blocked release action', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          groupDraw({
            id: 'draw-old',
            status: 'Cancelled',
            groupPlacements: [
              {
                groupId: 'g-a',
                groupDisplayName: 'A',
                entryId: 'e1',
                displayName: 'Alpha',
              },
            ],
            actions: {
              cancel: { state: 'Hidden' },
              release: {
                state: 'Blocked',
                blockCode: 'Application.DrawReleaseBlockedByAttachedMatches',
                releasableCount: 1,
              },
              apply: { state: 'Hidden' },
              publishAndApply: { state: 'Hidden' },
              redo: { state: 'Hidden' },
            },
          }),
        ],
        {
          status: 'Draft',
          groups: [{ id: 'g-a', name: 'A', entryIds: ['e1'] }],
          releaseCandidateDrawId: 'draw-old',
        },
      ),
    );

    renderDialog(groupsStage());

    const release = await screen.findByRole('button', {
      name: 'Libérer #1 (1/1)',
    });
    expect(release).toBeDisabled();
    expect(screen.getByText(releaseAttachedMatchesError)).toBeInTheDocument();
  });

  it('does not recompute vacant places inside the dialog', () => {
    const source = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), 'StructureDrawDialog.tsx'),
      'utf8',
    );
    expect(source).not.toMatch(/countVacantBracketPlaces/);
  });

  it('T5 shows partial duo stats, and équipes × places when nothing is left to draw', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([], {
        slotDrawDisplayMetrics: {
          unplacedEntryCount: 2,
          vacantSlotKeyCount: 2,
          vacantSlotKeys: ['S3', 'S4'],
          alreadyPlacedEntryCount: 2,
          entryCount: 4,
          placeCount: 4,
        },
      }),
    );
    const { unmount } = render(
      <MemoryRouter>
        <QueryClientProvider
          client={
            new QueryClient({
              defaultOptions: { queries: { retry: false } },
            })
          }
        >
          <StructureDrawDialog
            open
            onClose={vi.fn()}
            competitionId={competitionId}
            competitionStatus="Draft"
            stage={cupStage({
              compositionEntryCount: 4,
              compositionCapacity: 4,
              drawCreateLaunch: { allowed: true, blockReasonCode: null },
            })}
            structureStages={[]}
          />
        </QueryClientProvider>
      </MemoryRouter>,
    );
    expect(await screen.findByText('À placer')).toBeInTheDocument();
    expect(screen.getByText('Places libres')).toBeInTheDocument();
    expect(screen.queryByText('Déjà en place')).not.toBeInTheDocument();
    expect(screen.queryByText('équipes')).not.toBeInTheDocument();
    unmount();

    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([], {
        slotDrawDisplayMetrics: {
          unplacedEntryCount: 0,
          vacantSlotKeyCount: 0,
          vacantSlotKeys: [],
          alreadyPlacedEntryCount: 4,
          entryCount: 4,
          placeCount: 4,
        },
      }),
    );
    renderDialog(
      cupStage({
        compositionEntryCount: 4,
        compositionCapacity: 4,
        drawCreateLaunch: { allowed: false, blockReasonCode: 'NoFreeSlot' },
      }),
    );
    expect(
      await screen.findByRole('button', { name: 'Lancer le tirage' }),
    ).toBeDisabled();
    expect(
      screen.getByText(
        'Toutes les places de cette phase sont déjà occupées ou réservées. Le tirage ne remplace pas les équipes déjà placées.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByText('équipes')).toBeInTheDocument();
    expect(screen.getByText('places')).toBeInTheDocument();
    expect(screen.queryByText('À placer')).not.toBeInTheDocument();
    expect(screen.queryByText('Places libres')).not.toBeInTheDocument();
  });

  it('T5 keeps a duo when a partial draw has no occupant yet', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            inputsIntent: 'Partial',
            displayMetrics: {
              unplacedEntryCount: 4,
              vacantSlotKeyCount: 4,
              vacantSlotKeys: ['S1', 'S2', 'S3', 'S4'],
              alreadyPlacedEntryCount: 0,
              entryCount: 4,
              placeCount: 4,
            },
          }),
        ],
        { bracketPairs: cupBracketPairs },
      ),
    );
    renderDialog(
      cupStage({ compositionEntryCount: 4, compositionCapacity: 4 }),
    );
    expect(await screen.findByText('équipes')).toBeInTheDocument();
    expect(screen.getByText('places')).toBeInTheDocument();
    expect(screen.queryByText('À placer')).not.toBeInTheDocument();
    expect(screen.queryByText('Déjà en place')).not.toBeInTheDocument();
  });

  it('T6 keeps a published partial history tile after a manual release on the phase', async () => {
    const user = userEvent.setup();
    const cells = [
      {
        slotKey: 'S1',
        cellState: 'Drawn' as const,
        entryId: 'e1',
        displayName: 'Belgium',
        origin: 'Draw' as const,
      },
      {
        slotKey: 'S2',
        cellState: 'OutsideDraw' as const,
        entryId: null,
        displayName: null,
        origin: null,
      },
    ];
    const draw = slotDraw({
      status: 'Cancelled',
      actions: hiddenDrawActions(),
      slotPlacements: [
        {
          slotKey: 'S1',
          entryId: 'e1',
          displayName: 'Belgium',
          origin: 'Draw',
        },
      ],
      slotDisplayCells: cells,
      displayMetrics: null,
    });
    let released = false;
    vi.mocked(fetchStageOverview).mockImplementation(async () =>
      overview([draw], {
        bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
        slots: [
          {
            slotKey: 'S1',
            entryId: released ? null : 'e1',
            displayName: released ? null : 'Belgium',
            coveredByCompleteFixture: false,
          },
          {
            slotKey: 'S2',
            entryId: released ? null : 'e9',
            displayName: released ? null : 'Other',
            coveredByCompleteFixture: false,
          },
        ],
      }),
    );
    const { queryClient } = renderDialog(cupStage());
    await user.click(
      await screen.findByRole('button', { name: /Exécution #1/ }),
    );
    const tile = await waitFor(() => {
      const row = document.querySelector('.draw-slot-rows li');
      expect(row).not.toBeNull();
      return row as HTMLElement;
    });
    expect(tile).toHaveAccessibleName('P1 · Belgium vs Hors de ce tirage');
    expect(tile).not.toHaveAccessibleName(/Place libre/);
    expect(tile).not.toHaveAccessibleName(/Déjà placée/);
    const before = tile.getAttribute('aria-label');
    released = true;
    await queryClient.invalidateQueries({
      queryKey: queryKeys.stages.detail(stageId),
    });
    await waitFor(() => {
      const row = document.querySelector('.draw-slot-rows li');
      expect(row).toHaveAttribute('aria-label', before);
    });
    expect(screen.queryByText('Place libre')).not.toBeInTheDocument();
    expect(screen.getByText('Belgium')).toBeInTheDocument();
    expect(screen.getByText('Hors de ce tirage')).toBeInTheDocument();
  });

  it('T7 does not show already placed on a group rerun', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        groupDraw({
          inputsIntent: 'Rerun',
          groupPlacements: [
            {
              groupId: 'g-a',
              groupDisplayName: 'A',
              entryId: 'e1',
              displayName: 'Alpha',
            },
          ],
        }),
      ]),
    );
    renderDialog(groupsStage());
    expect(await screen.findByText('Alpha')).toBeInTheDocument();
    expect(screen.queryByText('Déjà placée')).not.toBeInTheDocument();
    expect(screen.queryByText('À placer')).not.toBeInTheDocument();
  });

  it('T-cache invalidates the stage detail after a slot release', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview(
        [
          slotDraw({
            status: 'Cancelled',
            actions: releaseAvailable(1),
            slotPlacements: [
              { slotKey: 'S1', entryId: 'e1', displayName: 'Belgium' },
            ],
          }),
        ],
        {
          slots: [
            {
              slotKey: 'S1',
              entryId: 'e1',
              displayName: 'Belgium',
              coveredByCompleteFixture: false,
            },
          ],
        },
        'draw-slot-1',
      ),
    );
    vi.mocked(releaseDrawAlignedPlacements).mockResolvedValue({
      releasedCount: 1,
      skippedCount: 0,
    });
    const { queryClient } = renderDialog(cupStage());
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');
    await user.click(
      await screen.findByRole('button', {
        name: 'Libérer les 1 équipes tirées',
      }),
    );
    const confirm = await screen.findByRole('dialog', {
      name: 'Libérer les placements du tirage ?',
    });
    await user.click(
      within(confirm).getByRole('button', {
        name: 'Libérer les 1 équipes tirées',
      }),
    );
    await waitFor(() => {
      expect(invalidate).toHaveBeenCalledWith({
        queryKey: queryKeys.stages.detail(stageId),
        refetchType: 'active',
      });
    });
  });
});
