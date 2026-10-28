import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  fetchStageOverview,
  fetchStageSchematic,
  replaceStageProgressionRules,
} from '../../../api';
import enStructure from '../../../i18n/locales/en/structure.json';
import frStructure from '../../../i18n/locales/fr/structure.json';
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

  it('blocks save and echoes a persisted duplicate round outcome', async () => {
    renderDialog(
      hubStage({
        stageId: sourceId,
        name: 'Demi-finales',
        hasProgressionRules: true,
        progressionPathCount: 2,
        structureIssues: ['DuplicateProgressionRoundOutcome'],
        progressionIntents: [
          {
            intentId: 'prog-a',
            order: 1,
            roundId: 'round-1',
            roundName: 'Tour principal',
            outcome: 'Winner',
            destinationStageId: destId,
            expandedPathCount: 1,
          },
          {
            intentId: 'prog-b',
            order: 2,
            roundId: 'round-1',
            roundName: 'Tour principal',
            outcome: 'Winner',
            destinationStageId: destId,
            expandedPathCount: 1,
          },
        ],
      }),
      hubStage({
        stageId: destId,
        name: 'Poules',
        formatKind: 'Groups',
      }),
    );

    expect(
      await screen.findAllByText('Tour et résultat déjà utilisés'),
    ).toHaveLength(2);
    expect(screen.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
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

  it('stacks pair confrontations and blocks save with a danger alert', async () => {
    const user = userEvent.setup();
    expect(frStructure.progression.placeMapHeading).toBe(
      'Association des places',
    );
    expect(frStructure.qualification.placeMapHeading).toBe(
      'Association des places',
    );
    expect(enStructure.progression.placeMapHeading).toBe('Place associations');
    expect(enStructure.qualification.placeMapHeading).toBe(
      'Place associations',
    );
    expect(enStructure.progression.placeMapHeading).not.toBe('Places');
    expect(enStructure.progression.placeMapHeading).not.toBe(
      frStructure.progression.placeMapHeading,
    );

    vi.mocked(fetchStageOverview).mockResolvedValue({
      ...overview(),
      rounds: [
        {
          id: 'round-1',
          name: 'Finale',
          fixtures: [
            {
              id: 'fx-1',
              slotAKey: 'S1',
              slotBKey: 'S2',
              attachments: [],
            },
            {
              id: 'fx-2',
              slotAKey: 'S3',
              slotBKey: 'S4',
              attachments: [],
            },
          ],
        },
      ],
      bracketPairs: [
        { pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' },
        { pairKey: 'P2', slotAKey: 'S3', slotBKey: 'S4' },
      ],
    });
    vi.mocked(fetchStageSchematic).mockImplementation(async (id: string) => {
      if (id === destId) {
        return {
          ...schematic(destId, 'Cup'),
          name: 'Tableau',
          cases: [
            { formPosition: { kind: 'CupSlot', slotKey: 'S1' } },
            { formPosition: { kind: 'CupSlot', slotKey: 'S2' } },
          ],
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
            destinationSlotKeys: ['', ''],
            expandedPathCount: 2,
          },
        ],
      }),
      hubStage({ stageId: destId, name: 'Tableau', formatKind: 'Cup' }),
    );

    const blocked = await screen.findByRole('alert');
    expect(blocked).toHaveClass('ds-alert--danger');
    expect(blocked).toHaveTextContent(
      /2 places à associer avant d.enregistrer/,
    );
    const save = screen.getByRole('button', { name: 'Enregistrer' });
    expect(save).toBeDisabled();

    await user.click(
      await screen.findByRole('button', { name: /Perdant — Finale/i }),
    );

    expect(
      await screen.findByText('Association des places'),
    ).toBeInTheDocument();
    expect(screen.queryByText('Emplacements')).not.toBeInTheDocument();

    const map = screen.getByRole('list', {
      name: 'Associer chaque match à une place',
    });
    expect(within(map).getByText('P1')).toHaveClass(
      'structure-qualification__place-map-primary',
    );
    expect(within(map).getByText('S1 vs S2')).toHaveClass(
      'structure-qualification__place-map-secondary',
    );
    expect(within(map).getByText('P2')).toHaveClass(
      'structure-qualification__place-map-primary',
    );
    expect(within(map).getByText('S3 vs S4')).toHaveClass(
      'structure-qualification__place-map-secondary',
    );
    expect(within(map).queryByText('P1 · S1 vs S2')).not.toBeInTheDocument();
    expect(
      within(map).getByRole('combobox', { name: 'Place pour P1 · S1 vs S2' }),
    ).toBeInTheDocument();

    await user.click(
      within(map).getByRole('combobox', { name: 'Place pour P1 · S1 vs S2' }),
    );
    await user.click(screen.getByRole('option', { name: 'S1' }));
    await user.click(
      within(map).getByRole('combobox', { name: 'Place pour P2 · S3 vs S4' }),
    );
    await user.click(screen.getByRole('option', { name: 'S2' }));

    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
      expect(save).toBeEnabled();
    });
  });

  it('stacks fixture confrontations when the round has no bracket pairs', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageOverview).mockResolvedValue({
      ...overview(),
      rounds: [
        {
          id: 'round-1',
          name: 'Finale',
          fixtures: [
            {
              id: 'fx-1',
              slotAKey: 'SA',
              slotBKey: 'SB',
              attachments: [],
            },
          ],
        },
      ],
      bracketPairs: [],
    });
    vi.mocked(fetchStageSchematic).mockImplementation(async (id: string) => {
      if (id === destId) {
        return {
          ...schematic(destId, 'Cup'),
          cases: [{ formPosition: { kind: 'CupSlot', slotKey: 'S1' } }],
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
            intentId: 'prog-fixture',
            order: 1,
            roundId: 'round-1',
            roundName: 'Finale',
            outcome: 'Loser',
            destinationStageId: destId,
            destinationSlotKeys: [''],
            expandedPathCount: 1,
          },
        ],
      }),
      hubStage({ stageId: destId, name: 'Tableau', formatKind: 'Cup' }),
    );

    await user.click(
      await screen.findByRole('button', { name: /Perdant — Finale/i }),
    );

    const map = await screen.findByRole('list', {
      name: 'Associer chaque match à une place',
    });
    expect(within(map).getByText('Match #1')).toHaveClass(
      'structure-qualification__place-map-primary',
    );
    expect(within(map).getByText('SA vs SB')).toHaveClass(
      'structure-qualification__place-map-secondary',
    );
    expect(
      within(map).queryByText('Match #1 · SA vs SB'),
    ).not.toBeInTheDocument();
    expect(
      within(map).getByRole('combobox', {
        name: 'Place pour Match #1 · SA vs SB',
      }),
    ).toBeInTheDocument();
  });
});
