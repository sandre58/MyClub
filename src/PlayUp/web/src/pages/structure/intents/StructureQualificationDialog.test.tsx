import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  fetchStageSchematic,
  replaceStageQualificationRules,
} from '../../../api';
import { structureReadiness } from '../../../test/structureTestFixtures';
import type {
  SchematicCase,
  StageSchematic,
  StructureStageHubSummary,
  StructureView,
} from '../../../types';
import { StructureQualificationDialog } from './StructureQualificationDialog';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    fetchStageSchematic: vi.fn(),
    replaceStageQualificationRules: vi.fn(),
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
    roundCount: 0,
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
    formatKind: 'Championship',
    compositionCapacity: null,
    affectationEntryCount: 0,
    actions: ['ReplaceQualificationRules'],
    ...partial,
  };
}

function structureView(
  source: StructureStageHubSummary,
  destination: StructureStageHubSummary,
): StructureView {
  return {
    competitionId,
    name: 'OverExtraction',
    status: 'Draft',
    participants: {
      activeCount: 4,
      occupyingCount: 4,
      entries: [],
    },
    format: {
      kind: 'Groups',
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
      groupCount: 2,
      roundCount: 0,
      matchdayCount: 0,
      slotCount: 0,
      hasDrawRules: true,
      numberOfPots: 4,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    stages: [source, destination],
    readiness: structureReadiness({ readyForNextSlice: true }),
    actions: [],
  };
}

function groupCase(
  groupId: string,
  name: string,
  index: number,
): SchematicCase {
  return {
    formPosition: {
      kind: 'GroupPlace',
      groupId,
      groupName: name,
      index,
    },
  };
}

function groupsSchematic(cases: SchematicCase[]): StageSchematic {
  return {
    stageId: sourceId,
    competitionId,
    name: 'Poules',
    status: 'Draft',
    formatKind: 'Groups',
    cases,
    connections: [],
  };
}

function overExtractionSource(): StructureStageHubSummary {
  return hubStage({
    stageId: sourceId,
    name: 'Poules',
    formatKind: 'Groups',
    groupCount: 2,
    placesPerGroup: 2,
    compositionCapacity: 4,
    hasQualificationRules: true,
    qualificationPathCount: 1,
    qualificationIntents: [
      {
        intentId: 'qual-each',
        order: 1,
        sourceKind: 'EachGroup',
        positionFrom: 1,
        positionTo: 3,
        destinationStageId: destId,
        destinationCount: 6,
      },
    ],
  });
}

function overExtractionDestination(): StructureStageHubSummary {
  return hubStage({
    stageId: destId,
    name: 'Phase suivante',
    formatKind: 'Championship',
    // Hub ResolvePlaces(Championship) = CountActiveEntries for this seed.
    compositionCapacity: 4,
  });
}

function renderDialog(
  source: StructureStageHubSummary,
  options?: {
    destination?: StructureStageHubSummary;
    openedFromDestinationStageId?: string | null;
  },
) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const destination = options?.destination ?? overExtractionDestination();
  render(
    <QueryClientProvider client={client}>
      <StructureQualificationDialog
        data={structureView(source, destination)}
        stage={source}
        open
        onClose={vi.fn()}
        openedFromDestinationStageId={
          options?.openedFromDestinationStageId ?? null
        }
      />
    </QueryClientProvider>,
  );
}

describe('StructureQualificationDialog hydrate / groups (2b)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(replaceStageQualificationRules).mockResolvedValue(undefined);
  });

  it('does not publish sessionReady when source schematic fails (no EachGroup → 0)', async () => {
    vi.mocked(fetchStageSchematic).mockRejectedValue(
      new Error('schematic down'),
    );

    renderDialog(overExtractionSource());

    expect(
      await screen.findByRole('heading', { name: /Qualifications — Poules/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole('dialog').closest('.ds-dialog')).toHaveAttribute(
      'data-size',
      'lg',
    );

    // Facts stay dashed — hydrate must not treat isLoading=false+error as ready.
    await waitFor(() => {
      const dashes = screen.getAllByText('—');
      expect(dashes.length).toBeGreaterThanOrEqual(2);
    });
    expect(screen.queryByText(/^6$/)).not.toBeInTheDocument();
  });

  it('hydrates EachGroup 1–3 with schematic groups → entryTotal 6 (not 0)', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageSchematic).mockImplementation(async (id: string) => {
      if (id === sourceId) {
        return groupsSchematic([
          groupCase('g1', 'A', 1),
          groupCase('g1', 'A', 2),
          groupCase('g2', 'B', 1),
          groupCase('g2', 'B', 2),
        ]);
      }
      return {
        stageId: destId,
        competitionId,
        name: 'Phase suivante',
        status: 'Draft',
        formatKind: 'Championship',
        cases: [],
        connections: [],
      };
    });

    renderDialog(overExtractionSource());

    // Primary fact = expandOccurrences volume after hydrate with groups.
    expect(
      await screen.findByText('6', {
        selector: '.structure-qualification__fact-value',
      }),
    ).toBeInTheDocument();
    // Champ destination normalizes to ForForm — summary is N qualifiés, not 0/N places.
    expect(
      screen.getByText(/Phase suivante · 6 qualifiés/i),
    ).toBeInTheDocument();
    expect(screen.queryByText(/0\/6 places/i)).not.toBeInTheDocument();

    // Meter lives in the expanded intent editor.
    await user.click(
      screen.getByRole('button', { name: /1er à 3e de chaque groupe/i }),
    );
    expect(
      await screen.findByRole('img', {
        name: /6 sur 4 places/i,
      }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('grid', { name: /associer chaque sélection/i }),
    ).not.toBeInTheDocument();
  });

  it('successful empty schematic yields expand 0 (Host Empty), not a hydrate race', async () => {
    vi.mocked(fetchStageSchematic).mockResolvedValue(groupsSchematic([]));

    renderDialog(overExtractionSource());

    expect(
      await screen.findByText('0', {
        selector: '.structure-qualification__fact-value',
      }),
    ).toBeInTheDocument();
    // Collapsed row has no whereCount when occCount is 0.
    expect(
      screen.queryByText(/Phase suivante · 6 qualifiés/i),
    ).not.toBeInTheDocument();
  });

  it('echoes a slot structure issue while the destination stage still exists', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageSchematic).mockResolvedValue(groupsSchematic([]));

    renderDialog(
      hubStage({
        stageId: sourceId,
        name: 'Poules',
        formatKind: 'Groups',
        hasQualificationRules: true,
        qualificationPathCount: 1,
        structureIssues: ['MissingQualificationDestinationSlot'],
        qualificationIntents: [
          {
            intentId: 'qual-pop',
            order: 1,
            sourceKind: 'Overall',
            positionFrom: 1,
            positionTo: 1,
            destinationStageId: destId,
          },
        ],
      }),
    );

    expect(
      await screen.findByText('Place de qualification manquante'),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Phase introuvable/i)).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Supprimer' }));
    const save = screen.getByRole('button', { name: 'Enregistrer' });
    await waitFor(() => expect(save).toBeEnabled());
    await user.click(save);

    await waitFor(() => {
      expect(replaceStageQualificationRules).toHaveBeenCalledWith(sourceId, {
        intents: [],
      });
    });
  });

  it('states Place ownership when opened from the destination phase', async () => {
    vi.mocked(fetchStageSchematic).mockResolvedValue(groupsSchematic([]));

    renderDialog(overExtractionSource(), {
      openedFromDestinationStageId: destId,
    });

    expect(
      await screen.findByText(
        /enregistrés sur Poules, pas sur la phase d’où vous avez ouvert/i,
      ),
    ).toBeInTheDocument();
  });

  it('does not describe Place as Cup-only when the destination has no labeled places', async () => {
    const user = userEvent.setup();
    const cupDestination = hubStage({
      stageId: destId,
      name: 'Tableau',
      formatKind: 'Cup',
    });
    vi.mocked(fetchStageSchematic).mockImplementation(async (id: string) => {
      if (id === destId) {
        return {
          stageId: destId,
          competitionId,
          name: 'Tableau',
          status: 'Draft',
          formatKind: 'Cup',
          cases: [
            {
              formPosition: { kind: 'CupSlot', index: 1 },
            },
          ],
          connections: [],
        };
      }
      return groupsSchematic([]);
    });

    renderDialog(
      hubStage({
        stageId: sourceId,
        name: 'Poules',
        formatKind: 'Groups',
        qualificationIntents: [
          {
            intentId: 'qual-place',
            order: 1,
            sourceKind: 'Overall',
            positionFrom: 1,
            positionTo: 1,
            destinationStageId: destId,
            destinationSlotKeys: ['S1'],
          },
        ],
      }),
      { destination: cupDestination },
    );

    await user.click(
      await screen.findByRole('button', {
        name: /1er du classement général/i,
      }),
    );

    expect(
      await screen.findByText(
        /Cette phase de destination n’expose pas de places adressables/i,
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(/phases Coupe dont la structure/i),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('radiogroup', { name: 'Phase de destination' }),
    ).toBeInTheDocument();
  });

  it('writes a clicked place on the source and keeps the opened-from copy', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStageSchematic).mockImplementation(async (id: string) => {
      if (id === destId) {
        return {
          stageId: destId,
          competitionId,
          name: 'Tableau',
          status: 'Draft',
          formatKind: 'Cup',
          cases: [
            {
              formPosition: {
                kind: 'CupSlot',
                slotKey: 'SF-1-A',
                roundName: 'Demi-finale',
                pairOrdinal: 1,
                side: 'A',
              },
            },
          ],
          connections: [],
        };
      }
      return groupsSchematic([]);
    });

    renderDialog(
      hubStage({
        stageId: sourceId,
        name: 'Poules',
        formatKind: 'Groups',
        hasQualificationRules: true,
        qualificationPathCount: 1,
        qualificationIntents: [
          {
            intentId: 'qual-place',
            order: 1,
            sourceKind: 'Overall',
            positionFrom: 1,
            positionTo: 1,
            destinationStageId: destId,
            destinationSlotKeys: [''],
          },
        ],
      }),
      {
        destination: hubStage({
          stageId: destId,
          name: 'Tableau',
          formatKind: 'Cup',
        }),
        openedFromDestinationStageId: destId,
      },
    );

    const openedFromCopy =
      /enregistrés sur Poules, pas sur la phase d’où vous avez ouvert/i;
    expect(await screen.findByText(openedFromCopy)).toBeInTheDocument();

    await user.click(
      await screen.findByRole('button', { name: /1er du classement général/i }),
    );
    const tray = await screen.findByRole('listbox', { name: /à placer/i });
    await user.click(
      within(tray).getByRole('option', { name: /1er du classement général/i }),
    );
    await user.click(
      within(
        screen.getByRole('grid', { name: /associer chaque sélection/i }),
      ).getByRole('button', { name: /Demi-finale 1 · côté A/i }),
    );

    expect(screen.getByText(openedFromCopy)).toBeInTheDocument();
    expect(
      screen.getByText('1/1', {
        selector: '.structure-place-map-assigner__counter',
      }),
    ).toBeInTheDocument();

    const save = screen.getByRole('button', { name: 'Enregistrer' });
    await waitFor(() => expect(save).toBeEnabled());
    await user.click(save);

    await waitFor(() => {
      expect(replaceStageQualificationRules).toHaveBeenCalledWith(
        sourceId,
        expect.objectContaining({
          intents: [
            expect.objectContaining({
              destinationStageId: destId,
              destinationSlotKeys: ['SF-1-A'],
              destinationGroupIds: null,
              destinationForm: false,
            }),
          ],
        }),
      );
    });
    expect(
      vi
        .mocked(replaceStageQualificationRules)
        .mock.calls.every((call) => call[0] === sourceId),
    ).toBe(true);
  });
});
