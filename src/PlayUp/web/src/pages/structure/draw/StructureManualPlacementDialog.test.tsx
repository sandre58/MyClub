import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ComponentProps } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, assignEntryToSlot } from '../../../api';
import type {
  SchematicCase,
  StructureEntry,
  StructureStageHubSummary,
} from '../../../types';
import { StructureManualPlacementDialog } from './StructureManualPlacementDialog';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    assignEntryToSlot: vi.fn(),
    clearSlotAssignment: vi.fn(),
  };
});

const stageId = 'stage-cup';

function hubStage(): StructureStageHubSummary {
  return {
    stageId,
    name: 'Finale',
    status: 'Draft',
    teamCount: 2,
    matchCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Cup',
    compositionEntryIds: ['e1', 'e2'],
    actions: ['AssignEntryToSlot', 'ClearSlotAssignment'],
  };
}

function cupSlot(
  slotKey: string,
  partial: Partial<SchematicCase> = {},
): SchematicCase {
  return {
    formPosition: { kind: 'CupSlot', slotKey },
    entry: null,
    assignment: null,
    seatMode: 'Editable',
    canClear: false,
    ...partial,
  };
}

function entry(id: string, displayName: string): StructureEntry {
  return { entryId: id, displayName, status: 'Active' };
}

function renderDialog(
  props: Partial<ComponentProps<typeof StructureManualPlacementDialog>> = {},
  queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  }),
) {
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StructureManualPlacementDialog
          open
          onClose={vi.fn()}
          competitionId="comp-1"
          stage={hubStage()}
          entries={[entry('e1', 'Alpha'), entry('e2', 'Bravo')]}
          cases={[cupSlot('A'), cupSlot('B')]}
          structureStages={[hubStage()]}
          {...props}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('StructureManualPlacementDialog', () => {
  beforeEach(() => {
    vi.mocked(assignEntryToSlot).mockReset();
    vi.mocked(assignEntryToSlot).mockResolvedValue(undefined);
  });

  it('exposes team tiles in a single-select group', () => {
    renderDialog({ initialSlotKey: 'A' });
    expect(
      screen.getByRole('radiogroup', { name: 'Équipe' }),
    ).toBeInTheDocument();
    expect(screen.getAllByRole('radio')).toHaveLength(2);
  });

  it('shows the clear control with the manualPlacement.clearSelection label', async () => {
    const user = userEvent.setup();
    renderDialog({ initialSlotKey: 'A' });

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    const clear = screen.getByRole('button', {
      name: 'Effacer la sélection',
    });
    expect(clear).toBeInTheDocument();
    await user.click(clear);
    expect(
      screen.queryByRole('radio', { checked: true }),
    ).not.toBeInTheDocument();
    expect(screen.getByText('Sélection effacée')).toBeInTheDocument();
  });

  it('uses the unified placement title with a locked cup target', () => {
    renderDialog({ initialSlotKey: 'A' });
    expect(
      screen.getByRole('dialog', {
        name: 'Placement — Finale — A',
      }),
    ).toBeInTheDocument();
  });

  it('shows cup occupancy stats that follow the current selection', async () => {
    const user = userEvent.setup();
    renderDialog({ initialSlotKey: 'A' });
    const facts = document.querySelector('.structure-manual-placement__facts');
    expect(facts).toHaveAttribute('data-tone', 'attention');
    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    expect(facts).toHaveAttribute('data-tone', 'success');
  });

  it('grays out teams already placed on another cup slot (A9)', () => {
    renderDialog({
      initialSlotKey: 'A',
      cases: [
        cupSlot('A'),
        cupSlot('B', {
          entry: { entryId: 'e2', displayName: 'Bravo' },
          assignment: { entryId: 'e2', displayName: 'Bravo' },
        }),
      ],
    });
    const elsewhere = screen.getByRole('radio', { name: 'Bravo' });
    expect(elsewhere).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByText(/Sur B/)).toBeInTheDocument();
  });

  it('refreshes the structure hub on assignment error', async () => {
    const user = userEvent.setup();
    vi.mocked(assignEntryToSlot).mockRejectedValueOnce(
      new ApiError(409, 'conflict', 'detail', 'Stage.StructureLocked'),
    );
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');
    renderDialog({ initialSlotKey: 'A' }, queryClient);

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
    await waitFor(() => expect(invalidate).toHaveBeenCalled());
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});
