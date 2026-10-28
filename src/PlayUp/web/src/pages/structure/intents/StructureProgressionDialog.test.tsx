import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  fetchStageOverview,
  fetchStageSchematic,
  replaceStageProgressionRules,
} from '../../../api';
import { structureReadiness } from '../../../test/structureTestFixtures';
import type {
  StageOverview,
  StageSchematic,
  StructureStageHubSummary,
  StructureView,
} from '../../../types';
import { StructureProgressionDialog } from './StructureProgressionDialog';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
    fetchStageSchematic: vi.fn(),
    replaceStageProgressionRules: vi.fn(),
  };
});

const sourceId = '11111111-1111-1111-1111-111111111111';
const destId = '22222222-2222-2222-2222-222222222222';
const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function hubStage(
  partial: Partial<StructureStageHubSummary> &
    Pick<StructureStageHubSummary, 'stageId' | 'name'>,
): StructureStageHubSummary {
  return {
    status: 'Draft',
    teamCount: 0,
    matchCount: 0,
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
    compositionCapacity: null,
    affectationEntryCount: 0,
    actions: ['ReplaceProgressionRules'],
    ...partial,
  };
}

function structureView(
  source: StructureStageHubSummary,
  destination: StructureStageHubSummary,
): StructureView {
  return {
    competitionId,
    name: 'Coupe',
    status: 'Draft',
    participants: {
      activeCount: 2,
      occupyingCount: 2,
      entries: [],
    },
    format: {
      kind: 'Cup',
      primaryStageId: source.stageId,
      primaryStageName: source.name,
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
    stages: [source, destination],
    readiness: structureReadiness(),
    actions: [],
  };
}

function overview(): StageOverview {
  return {
    id: sourceId,
    competitionId,
    name: 'Demi',
    status: 'Draft',
    slots: [],
    rounds: [{ id: 'round-1', name: 'Finale', fixtures: [] }],
    draws: [],
    bracketPairs: [],
  };
}

function schematic(
  stageId: string,
  formatKind: StageSchematic['formatKind'],
): StageSchematic {
  return {
    stageId,
    competitionId,
    name: stageId,
    status: 'Draft',
    formatKind,
    cases: [],
    connections: [],
  };
}

function renderDialog(
  source: StructureStageHubSummary,
  destination: StructureStageHubSummary,
) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  render(
    <QueryClientProvider client={client}>
      <StructureProgressionDialog
        data={structureView(source, destination)}
        stage={source}
        open
        onClose={vi.fn()}
      />
    </QueryClientProvider>,
  );
}

describe('StructureProgressionDialog chrome', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(replaceStageProgressionRules).mockResolvedValue(undefined);
    vi.mocked(fetchStageOverview).mockResolvedValue(overview());
    vi.mocked(fetchStageSchematic).mockImplementation(async (id: string) =>
      schematic(id, id === destId ? 'Cup' : 'Cup'),
    );
  });

  it('keeps the phase title at dialog size lg', async () => {
    renderDialog(
      hubStage({ stageId: sourceId, name: 'Demi' }),
      hubStage({ stageId: destId, name: 'Finale aval', formatKind: 'Cup' }),
    );

    expect(
      await screen.findByRole('heading', { name: 'Progression — Demi' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('dialog').closest('.ds-dialog')).toHaveAttribute(
      'data-size',
      'lg',
    );
  });

  it('echoes a slot structure issue while the destination stage still exists', async () => {
    const user = userEvent.setup();
    const destination = hubStage({
      stageId: destId,
      name: 'Phase suivante',
      formatKind: 'Championship',
    });

    renderDialog(
      hubStage({
        stageId: sourceId,
        name: 'Demi',
        hasProgressionRules: true,
        progressionPathCount: 1,
        structureIssues: ['MissingProgressionDestinationSlot'],
        progressionIntents: [
          {
            intentId: 'prog-pop',
            order: 1,
            roundId: 'round-1',
            roundName: 'Finale',
            outcome: 'Winner',
            destinationStageId: destId,
            expandedPathCount: 1,
          },
        ],
      }),
      destination,
    );

    expect(
      await screen.findByText('Place de progression manquante'),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Phase introuvable/i)).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Supprimer' }));
    const save = screen.getByRole('button', { name: 'Enregistrer' });
    await waitFor(() => expect(save).toBeEnabled());
    await user.click(save);

    await waitFor(() => {
      expect(replaceStageProgressionRules).toHaveBeenCalledWith(sourceId, {
        intents: null,
      });
    });
  });

  it('does not describe Place as Cup-only when the destination has no labeled places', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageSchematic).mockImplementation(async (id: string) => {
      if (id === destId) {
        return {
          ...schematic(destId, 'Cup'),
          cases: [{ formPosition: { kind: 'CupSlot', index: 1 } }],
        };
      }
      return schematic(id, 'Cup');
    });

    renderDialog(
      hubStage({
        stageId: sourceId,
        name: 'Demi',
        progressionIntents: [
          {
            intentId: 'prog-place',
            order: 1,
            roundId: 'round-1',
            roundName: 'Finale',
            outcome: 'Loser',
            destinationStageId: destId,
            destinationSlotKeys: ['S1'],
            expandedPathCount: 1,
          },
        ],
      }),
      hubStage({ stageId: destId, name: 'Tableau', formatKind: 'Cup' }),
    );

    await user.click(
      await screen.findByRole('button', { name: /Perdant — Finale/i }),
    );

    expect(
      await screen.findByText(
        /Cette phase de destination n’expose pas de places adressables/i,
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/phases Coupe dont la structure/i),
    ).not.toBeInTheDocument();
  });
});
