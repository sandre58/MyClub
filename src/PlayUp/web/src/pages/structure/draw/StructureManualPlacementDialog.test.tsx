import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ComponentProps } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, assignEntryToSlot, clearSlotAssignment } from '../../../api';
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
          initialSlotKey="A"
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
    vi.mocked(clearSlotAssignment).mockReset();
    vi.mocked(clearSlotAssignment).mockResolvedValue(undefined);
  });

  it('exposes team tiles in a single-select group', () => {
    renderDialog({ initialSlotKey: 'A' });
    expect(
      screen.getByRole('radiogroup', { name: 'Équipe' }),
    ).toBeInTheDocument();
    expect(screen.getAllByRole('radio')).toHaveLength(2);
  });

  it('keeps Retirer on the team section row and clears the selection when enabled', async () => {
    const user = userEvent.setup();
    renderDialog({ initialSlotKey: 'A' });

    const remove = screen.getByRole('button', { name: 'Retirer' });
    expect(remove.className).toContain('ds-btn--destructive');
    expect(remove).toBeDisabled();
    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    expect(remove).toBeEnabled();
    await user.click(remove);
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

  it('sends the occupant from open as expectedEntryId', async () => {
    const user = userEvent.setup();
    renderDialog({
      initialSlotKey: 'A',
      cases: [
        cupSlot('A', {
          entry: { entryId: 'e1', displayName: 'Alpha' },
          assignment: { entryId: 'e1', displayName: 'Alpha' },
          feedOrigin: { kind: 'Direct' },
        }),
        cupSlot('B'),
      ],
    });

    await user.click(screen.getByRole('radio', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() =>
      expect(assignEntryToSlot).toHaveBeenCalledWith(stageId, 'A', 'e2', 'e1'),
    );
  });

  it('sends null when assigning a team to a free place', async () => {
    const user = userEvent.setup();
    renderDialog({ initialSlotKey: 'A' });
    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));
    await waitFor(() =>
      expect(assignEntryToSlot).toHaveBeenCalledWith(stageId, 'A', 'e1', null),
    );
  });

  it('sends the opening occupant when clearing a place', async () => {
    const user = userEvent.setup();
    renderDialog({
      initialSlotKey: 'A',
      cases: [
        cupSlot('A', {
          entry: { entryId: 'e1', displayName: 'Alpha' },
          assignment: { entryId: 'e1', displayName: 'Alpha' },
          feedOrigin: { kind: 'Direct' },
        }),
        cupSlot('B'),
      ],
    });
    await user.click(screen.getByRole('button', { name: 'Retirer' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));
    await waitFor(() =>
      expect(clearSlotAssignment).toHaveBeenCalledWith(stageId, 'A', 'e1'),
    );
  });

  it('keeps a stale 409 in the footer until the next action', async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.mocked(assignEntryToSlot).mockRejectedValueOnce(
      new ApiError(
        409,
        'conflict',
        'detail',
        'Application.SlotAssignmentStale',
      ),
    );
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');
    const stage = hubStage();
    const { rerender } = renderDialog(
      { initialSlotKey: 'A', stage, structureStages: [stage] },
      queryClient,
    );

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    const dialog = screen.getByRole('dialog');
    await waitFor(() =>
      expect(within(dialog).getByRole('status')).toHaveTextContent(
        'La place a été modifiée entre-temps. Mise à jour…',
      ),
    );
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.getAllByText(/Mise à jour…/)).toHaveLength(1);
    const announcer = dialog.querySelector(
      '.ds-visually-hidden[aria-live="polite"]',
    );
    expect(announcer?.textContent?.trim() ?? '').not.toContain('Mise à jour');
    expect(invalidate).toHaveBeenCalled();
    expect(dialog).toBeInTheDocument();
    await waitFor(
      () => {
        expect(screen.getByRole('radio', { name: 'Alpha' })).toHaveFocus();
      },
      { timeout: 3000 },
    );

    rerender(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <StructureManualPlacementDialog
            open
            onClose={vi.fn()}
            competitionId="comp-1"
            stage={stage}
            entries={[entry('e1', 'Alpha'), entry('e2', 'Bravo')]}
            cases={[
              cupSlot('A', {
                entry: { entryId: 'e2', displayName: 'Bravo' },
                assignment: { entryId: 'e2', displayName: 'Bravo' },
                feedOrigin: { kind: 'Direct' },
              }),
              cupSlot('B'),
            ]}
            structureStages={[stage]}
            initialSlotKey="A"
          />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(within(dialog).getByRole('status')).toHaveTextContent(
      'La place a changé : sélection mise à jour.',
    );
    expect(
      screen.getAllByText('La place a changé : sélection mise à jour.'),
    ).toHaveLength(1);
    expect(screen.getByRole('radio', { name: 'Bravo' })).toHaveAttribute(
      'aria-checked',
      'true',
    );

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    expect(within(dialog).queryByRole('status')).not.toBeInTheDocument();
  });

  it('shows SlotEntryUniqueConflict via MutationError and keeps the dialog open', async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.mocked(assignEntryToSlot).mockRejectedValueOnce(
      new ApiError(
        409,
        'conflict',
        'detail',
        'Application.SlotEntryUniqueConflict',
      ),
    );
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');
    renderDialog({ initialSlotKey: 'A' }, queryClient);

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    const dialog = screen.getByRole('dialog');
    await waitFor(() =>
      expect(within(dialog).getByRole('alert')).toHaveTextContent(
        'Cette équipe occupe déjà une autre place de la phase. Actualisez et réessayez.',
      ),
    );
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    expect(invalidate).toHaveBeenCalled();
    expect(dialog).toBeInTheDocument();
    expect(
      screen.getAllByText(
        'Cette équipe occupe déjà une autre place de la phase. Actualisez et réessayez.',
      ),
    ).toHaveLength(1);
  });
});
