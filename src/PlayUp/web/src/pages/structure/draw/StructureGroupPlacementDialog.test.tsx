import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ComponentProps } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  assignEntriesToGroup,
  assignEntryToGroup,
  clearEntryFromGroup,
  replaceEntryInGroup,
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
    assignEntriesToGroup: vi.fn(),
    clearEntryFromGroup: vi.fn(),
    replaceEntryInGroup: vi.fn(),
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
    compositionEntryIds: ['e1', 'e2', 'e3'],
    compositionEntryPots: [
      { entryId: 'e1', pot: 2 },
      { entryId: 'e2', pot: 1 },
      { entryId: 'e3', pot: 1 },
    ],
    placesPerGroup: 4,
    actions: [
      'AssignEntryToGroup',
      'AssignEntriesToGroup',
      'ClearEntryFromGroup',
      'ReplaceEntryInGroup',
    ],
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
          entries={[
            entry('e1', 'Alpha'),
            entry('e2', 'Bravo'),
            entry('e3', 'Charlie'),
          ]}
          cases={[
            groupPlace(),
            groupPlace({
              formPosition: {
                kind: 'GroupPlace',
                groupId,
                groupName: 'A',
                index: 2,
              },
            }),
          ]}
          place={groupPlace()}
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
    vi.mocked(assignEntriesToGroup).mockReset();
    vi.mocked(clearEntryFromGroup).mockReset();
    vi.mocked(replaceEntryInGroup).mockReset();
    vi.mocked(assignEntryToGroup).mockResolvedValue(undefined);
    vi.mocked(assignEntriesToGroup).mockResolvedValue(undefined);
    vi.mocked(clearEntryFromGroup).mockResolvedValue(undefined);
    vi.mocked(replaceEntryInGroup).mockResolvedValue(undefined);
  });

  it('places one team from an empty seat with PUT', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderDialog({ onClose });

    await user.click(screen.getByRole('radio', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(assignEntryToGroup).toHaveBeenCalledWith(stageId, groupId, 'e2');
    });
    expect(assignEntriesToGroup).not.toHaveBeenCalled();
    expect(onClose).toHaveBeenCalled();
  });

  it('batch-places teams in display order from the CTA', async () => {
    const user = userEvent.setup();
    renderDialog({ place: null });

    await user.click(screen.getByRole('checkbox', { name: 'Charlie' }));
    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Placer 2 équipes' }));

    await waitFor(() => {
      expect(assignEntriesToGroup).toHaveBeenCalledWith(stageId, groupId, [
        'e2',
        'e3',
      ]);
    });
  });

  it('fills the group with the first available teams', async () => {
    const user = userEvent.setup();
    renderDialog({ place: null });

    await user.click(
      screen.getByRole('button', { name: 'Compléter le groupe' }),
    );
    await user.click(screen.getByRole('button', { name: 'Placer 2 équipes' }));

    await waitFor(() => {
      expect(assignEntriesToGroup).toHaveBeenCalledWith(stageId, groupId, [
        'e2',
        'e3',
      ]);
    });
  });

  it('falls back to PUT when batch assign is unavailable', async () => {
    const user = userEvent.setup();
    renderDialog({
      place: null,
      stage: stage({
        actions: ['AssignEntryToGroup', 'ClearEntryFromGroup'],
      }),
    });

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(assignEntryToGroup).toHaveBeenCalledWith(stageId, groupId, 'e1');
    });
    expect(assignEntriesToGroup).not.toHaveBeenCalled();
  });

  it('replaces a team in one call from edit mode', async () => {
    const user = userEvent.setup();
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: occupied,
      cases: [
        occupied,
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 2,
          },
        }),
      ],
    });

    expect(
      screen.getByRole('dialog', {
        name: 'Modifier la place — Groupe A',
      }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(replaceEntryInGroup).toHaveBeenCalledWith(
        stageId,
        groupId,
        'e1',
        'e2',
      );
    });
    expect(clearEntryFromGroup).not.toHaveBeenCalled();
    expect(assignEntryToGroup).not.toHaveBeenCalled();
  });

  it('clears the seat with DELETE when selection is emptied', async () => {
    const user = userEvent.setup();
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: occupied,
      cases: [
        occupied,
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 2,
          },
        }),
      ],
    });

    await user.click(
      screen.getByRole('button', {
        name: /Retirer l.équipe de la place/,
      }),
    );
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(clearEntryFromGroup).toHaveBeenCalledWith(stageId, groupId, 'e1');
    });
  });

  it('names the refused team when batch assign returns entryId', async () => {
    const user = userEvent.setup();
    vi.mocked(assignEntriesToGroup).mockRejectedValueOnce(
      new ApiError(
        409,
        'conflict',
        'detail',
        'Stage.GroupFull',
        undefined,
        'e2',
      ),
    );
    renderDialog({ place: null });

    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Placer 1 équipe' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Bravo');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('shows group select when opened from the phase sheet without a seat', () => {
    const emptyB = groupPlace({
      formPosition: {
        kind: 'GroupPlace',
        groupId: 'group-b',
        groupName: 'B',
        index: 1,
      },
    });
    renderDialog({
      place: null,
      cases: [groupPlace(), emptyB],
    });
    expect(screen.getByRole('combobox', { name: 'Place' })).toBeInTheDocument();
  });

  it('asks before discarding a choice', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderDialog({ onClose });
    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
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
    ['Stage.StructureLocked', frErrors.Stage.StructureLocked],
  ] as const)('keeps the dialog open and shows %s', async (code, expected) => {
    const user = userEvent.setup();
    vi.mocked(assignEntryToGroup).mockRejectedValueOnce(
      new ApiError(409, 'conflict', 'detail', code),
    );
    renderDialog();

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(expected);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});
