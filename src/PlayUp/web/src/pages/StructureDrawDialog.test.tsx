import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  createAndGenerateDraw,
  fetchStageOverview,
  publishDraw,
} from '../api';
import type { StageOverview, StructureStageHubSummary } from '../types';
import { StructureDrawDialog } from './StructureDrawDialog';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    createAndGenerateDraw: vi.fn(),
    publishDraw: vi.fn(),
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

function overview(draws: StageOverview['draws']): StageOverview {
  return {
    id: stageId,
    competitionId,
    name: 'Groupes',
    status: 'Draft',
    slots: [],
    rounds: [],
    draws,
  };
}

function renderDialog(
  stage = groupsStage(),
  open = true,
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
        stage={stage}
      />
    </QueryClientProvider>,
  );
  return { onClose, queryClient };
}

describe('StructureDrawDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows empty state and Create when there are no draws', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(overview([]));

    renderDialog();

    expect(
      await screen.findByRole('heading', { name: 'Exécutions de tirage' }),
    ).toBeInTheDocument();
    expect(
      await screen.findByText('Aucune exécution de tirage pour cette phase.'),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Nouveau tirage' }),
    ).toBeInTheDocument();
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
  });

  it('uses detail-only layout for a single draw (no history rail)', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        {
          id: 'draw-1',
          kind: 'Group',
          status: 'Draft',
          resolutionState: 'Resolved',
          pairings: [],
          slotPlacements: [],
        },
      ]),
    );

    renderDialog();

    expect(
      await screen.findByText('Tirage résolu mais non publié.'),
    ).toBeInTheDocument();
    expect(
      screen.queryByLabelText('Historique des exécutions'),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Publier le tirage' }),
    ).toBeInTheDocument();
  });

  it('shows master-detail when more than one draw exists', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        {
          id: 'draw-1',
          kind: 'Group',
          status: 'Cancelled',
          resolutionState: 'Resolved',
          pairings: [],
          slotPlacements: [],
        },
        {
          id: 'draw-2',
          kind: 'Group',
          status: 'Draft',
          resolutionState: 'Resolved',
          pairings: [],
          slotPlacements: [],
        },
      ]),
    );

    renderDialog();

    const history = await screen.findByLabelText('Historique des exécutions');
    expect(within(history).getByText('Exécution #2')).toBeInTheDocument();
    expect(within(history).getByText('Exécution #1')).toBeInTheDocument();

    await user.click(within(history).getByText('Exécution #1'));
    expect(
      await screen.findByText(
        'Ce tirage a été annulé. Un nouveau tirage est nécessaire pour recommencer.',
      ),
    ).toBeInTheDocument();
  });

  it('publishes the selected draft draw', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview([
        {
          id: 'draw-1',
          kind: 'Group',
          status: 'Draft',
          resolutionState: 'Resolved',
          pairings: [],
          slotPlacements: [],
        },
      ]),
    );
    vi.mocked(publishDraw).mockResolvedValue(undefined);

    renderDialog();
    await user.click(
      await screen.findByRole('button', { name: 'Publier le tirage' }),
    );

    await waitFor(() => {
      expect(publishDraw).toHaveBeenCalledWith(stageId, 'draw-1');
    });
  });
});
