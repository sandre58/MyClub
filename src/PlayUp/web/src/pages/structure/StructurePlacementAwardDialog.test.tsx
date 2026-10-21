import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchStageOverview, replaceStagePlacementAwardRules } from '../../api';
import { structureReadiness } from '../../test/structureTestFixtures';
import type {
  StageOverview,
  StructureStageHubSummary,
  StructureView,
} from '../../types';
import { StructurePlacementAwardDialog } from './StructurePlacementAwardDialog';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    replaceStagePlacementAwardRules: vi.fn(),
  };
});

const stageId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function cupStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId,
    name: 'Finale',
    status: 'Draft',
    teamCount: 2,
    matchCount: 0,
    groupCount: 0,
    roundCount: 1,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: false,
    hasDrawRules: false,
    compositionEntryCount: 2,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: true,
    formatKind: 'Cup',
    compositionCapacity: 2,
    affectationEntryCount: 2,
    actions: ['ReplacePlacementAwardRules'],
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

function structureView(stage: StructureStageHubSummary): StructureView {
  return {
    competitionId,
    name: 'Test',
    status: 'Draft',
    participants: {
      activeCount: 2,
      occupyingCount: 2,
      entries: [],
    },
    format: {
      kind: 'Cup',
      primaryStageId: stageId,
      primaryStageName: stage.name,
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
      roundCount: 1,
      matchdayCount: 0,
      slotCount: 2,
      hasDrawRules: false,
      numberOfPots: null,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    stages: [stage],
    readiness: structureReadiness(),
    actions: [],
  };
}

function overview(overrides: Partial<StageOverview> = {}): StageOverview {
  return {
    id: stageId,
    competitionId,
    name: 'Finale',
    status: 'Draft',
    slots: [],
    rounds: [{ id: 'round-1', name: 'Finale', fixtures: [] }],
    draws: [],
    bracketPairs: [],
    ...overrides,
  };
}

function renderDialog(stage: StructureStageHubSummary) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const onClose = vi.fn();
  render(
    <QueryClientProvider client={client}>
      <StructurePlacementAwardDialog
        data={structureView(stage)}
        stage={stage}
        open
        onClose={onClose}
      />
    </QueryClientProvider>,
  );
  return { onClose };
}

describe('StructurePlacementAwardDialog orphan remediation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(replaceStagePlacementAwardRules).mockResolvedValue(undefined);
  });

  it('shows orphan awards when catalogue is empty and allows clear save', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview({ bracketPairs: [] }),
    );

    renderDialog(
      cupStage({
        placementAwards: [
          {
            rank: 1,
            outcome: 'Winner',
            sourcePairKey: 'P2',
            sourceLabel: 'P2',
          },
        ],
        hasPlacementAwardRules: true,
        placementAwardCount: 1,
        structureIssues: ['AuthoredAwardUnreachable'],
      }),
    );

    expect(
      await screen.findByRole('heading', {
        name: /Attribution des places/i,
      }),
    ).toBeInTheDocument();
    expect(
      await screen.findByText(/Paire inconnue|Unknown pair/i),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/Aucune paire structurelle|No structural pairs/i),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Supprimer|Delete/i }));

    const save = screen.getByRole('button', { name: /Enregistrer|Save/i });
    await waitFor(() => expect(save).toBeEnabled());
    await user.click(save);

    await waitFor(() => {
      expect(replaceStagePlacementAwardRules).toHaveBeenCalledWith(stageId, {
        paths: null,
      });
    });
  });

  it('shows dead pair among remaining pairs (Cas B) and clears on remove+save', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview({
        bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
      }),
    );

    renderDialog(
      cupStage({
        placementAwards: [
          {
            rank: 1,
            outcome: 'Winner',
            sourcePairKey: 'P2',
            sourceLabel: 'P2',
          },
        ],
        hasPlacementAwardRules: true,
        placementAwardCount: 1,
      }),
    );

    const tileTitle = await screen.findByText(/Paire inconnue|Unknown pair/i);
    expect(tileTitle).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Supprimer|Delete/i }));
    const save = screen.getByRole('button', { name: /Enregistrer|Save/i });
    await waitFor(() => expect(save).toBeEnabled());
    await user.click(save);

    await waitFor(() => {
      expect(replaceStagePlacementAwardRules).toHaveBeenCalledWith(stageId, {
        paths: null,
      });
    });
  });

  it('keeps empty-no-pair only when there are no awards', async () => {
    vi.mocked(fetchStageOverview).mockResolvedValue(
      overview({ bracketPairs: [] }),
    );

    renderDialog(cupStage({ placementAwards: [], placementAwardCount: 0 }));

    expect(
      await screen.findByText(/Aucune paire structurelle|No structural pairs/i),
    ).toBeInTheDocument();
  });
});
