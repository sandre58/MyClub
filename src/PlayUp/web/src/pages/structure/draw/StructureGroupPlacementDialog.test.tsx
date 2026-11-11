import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ComponentProps } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  assignEntryToGroup,
  clearEntryFromGroup,
} from '../../../api';
import frErrors from '../../../i18n/locales/fr/errors.json';
import type {
  SchematicCase,
  StructureEntry,
  StructureStageHubSummary,
} from '../../../types';
import { StructureGroupPlacementDialog } from './StructureGroupPlacementDialog';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    assignEntryToGroup: vi.fn(),
    clearEntryFromGroup: vi.fn(),
  };
});

const stageId = 'stage-1';
const groupId = 'group-a';

function stage(
  partial: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId,
    name: 'Phase de groupes',
    status: 'Draft',
    teamCount: 2,
    matchCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasDrawRules: true,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Groups',
    compositionEntryIds: ['e1', 'e2'],
    compositionEntryPots: [
      { entryId: 'e1', pot: 2 },
      { entryId: 'e2', pot: 1 },
    ],
    actions: ['AssignEntryToGroup', 'ClearEntryFromGroup'],
    ...partial,
  };
}

function entry(id: string, displayName: string): StructureEntry {
  return { entryId: id, displayName, status: 'Active' };
}

function groupPlace(partial: Partial<SchematicCase> = {}): SchematicCase {
  return {
    formPosition: {
      kind: 'GroupPlace',
      groupId,
      groupName: 'A',
      index: 1,
    },
    entry: null,
    assignment: null,
    seatMode: 'Editable',
    canClear: false,
    ...partial,
  };
}

function renderDialog(
  props: Partial<ComponentProps<typeof StructureGroupPlacementDialog>> = {},
  queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  }),
) {
  const view = render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StructureGroupPlacementDialog
          open
          onClose={vi.fn()}
          competitionId="comp-1"
          stage={stage()}
          structureStages={[stage()]}
          entries={[entry('e1', 'Alpha'), entry('e2', 'Bravo')]}
          cases={[]}
          place={groupPlace()}
          intent="place"
          {...props}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
  return { ...view, queryClient };
}

describe('StructureGroupPlacementDialog', () => {
  beforeEach(() => {
    vi.mocked(assignEntryToGroup).mockReset();
    vi.mocked(clearEntryFromGroup).mockReset();
    vi.mocked(assignEntryToGroup).mockResolvedValue(undefined);
    vi.mocked(clearEntryFromGroup).mockResolvedValue(undefined);
  });

  it('places the chosen team and refreshes', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const { queryClient } = renderDialog({ onClose });
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');

    expect(
      screen.getByRole('dialog', { name: 'Placer une équipe — Groupe A' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Pot 1')).toBeInTheDocument();
    expect(screen.getByText('Pot 2')).toBeInTheDocument();

    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(assignEntryToGroup).toHaveBeenCalledWith(stageId, groupId, 'e2');
    });
    expect(onClose).toHaveBeenCalled();
    await waitFor(() => {
      expect(invalidate).toHaveBeenCalled();
    });
  });

  it('hides pots when the server sends none', () => {
    renderDialog({
      stage: stage({ compositionEntryPots: null, hasDrawRules: false }),
    });
    expect(screen.queryByText(/^Pot /)).not.toBeInTheDocument();
    expect(screen.getByText('Alpha')).toBeInTheDocument();
  });

  it('removes the occupant after confirmation', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderDialog({
      onClose,
      intent: 'remove',
      place: groupPlace({
        entry: { entryId: 'e1', displayName: 'Alpha' },
        canClear: true,
      }),
      cases: [
        groupPlace({
          entry: { entryId: 'e1', displayName: 'Alpha' },
          canClear: true,
        }),
      ],
    });

    await user.click(screen.getByRole('button', { name: 'Retirer du groupe' }));
    const confirm = screen.getByRole('dialog', { name: 'Retirer du groupe' });
    expect(confirm).toHaveTextContent('Retirer Alpha du Groupe A ?');
    await user.click(
      within(confirm).getByRole('button', { name: 'Retirer du groupe' }),
    );

    await waitFor(() => {
      expect(clearEntryFromGroup).toHaveBeenCalledWith(stageId, groupId, 'e1');
    });
    expect(onClose).toHaveBeenCalled();
  });

  it('says when every composition team is already placed and when the composition is empty', () => {
    const placed = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
    });
    const { unmount } = renderDialog({
      cases: [
        placed,
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId: 'group-b',
            groupName: 'B',
            index: 1,
          },
          entry: { entryId: 'e2', displayName: 'Bravo' },
        }),
      ],
    });
    expect(
      screen.getByText(
        'Toutes les équipes de la composition sont déjà placées.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Enregistrer' }),
    ).not.toBeInTheDocument();
    unmount();

    renderDialog({
      stage: stage({ compositionEntryIds: [], compositionEntryPots: null }),
    });
    expect(
      screen.getByText('Ajoutez des équipes à la composition de la phase.'),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Enregistrer' }),
    ).not.toBeInTheDocument();
  });

  it('asks before discarding a choice', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderDialog({ onClose });
    await user.click(screen.getByRole('checkbox', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Annuler' }));
    expect(
      screen.getByRole('dialog', { name: 'Quitter sans enregistrer ?' }),
    ).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: 'Abandonner' }));
    expect(onClose).toHaveBeenCalled();
  });

  it.each([
    [
      'Application.GroupSeatReservedByInboundFeed',
      frErrors.Application.GroupSeatReservedByInboundFeed,
    ],
    [
      'Application.ManualGroupPlacementBlockedByActiveDraw',
      frErrors.Application.ManualGroupPlacementBlockedByActiveDraw,
    ],
    ['Stage.StructureLocked', frErrors.Stage.StructureLocked],
    ['Stage.GroupFull', frErrors.Stage.GroupFull],
  ] as const)('keeps the dialog open and shows %s', async (code, expected) => {
    const user = userEvent.setup();
    vi.mocked(assignEntryToGroup).mockRejectedValueOnce(
      new ApiError(409, 'conflict', 'detail', code),
    );
    const { queryClient } = renderDialog();
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');

    await user.click(screen.getByRole('checkbox', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(expected);
    expect(screen.getByRole('alert')).not.toHaveTextContent(code);
    expect(
      screen.getByRole('dialog', { name: 'Placer une équipe — Groupe A' }),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(invalidate).toHaveBeenCalled();
    });
  });
});
