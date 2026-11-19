import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
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
    const { queryClient } = renderDialog({ onClose });
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');

    expect(screen.getByText('Groupe A')).toBeInTheDocument();
    const dialog = screen.getByRole('dialog', {
      name: 'Placement manuel — Phase de groupes',
    });
    expect(within(dialog).getAllByText('Pot 1').length).toBeGreaterThan(0);
    expect(within(dialog).getByText('Pot 2')).toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(assignEntryToGroup).toHaveBeenCalledWith(stageId, groupId, 'e2');
    });
    expect(assignEntriesToGroup).not.toHaveBeenCalled();
    expect(onClose).toHaveBeenCalled();
    await waitFor(() => expect(invalidate).toHaveBeenCalled());
  });

  it('hides pots when the server sends none', () => {
    renderDialog({
      stage: stage({ compositionEntryPots: null, hasDrawRules: false }),
    });
    expect(screen.queryByText(/^Pot /)).not.toBeInTheDocument();
    expect(screen.getByText('Alpha')).toBeInTheDocument();
  });

  it('says when every composition team is already placed and when the composition is empty', () => {
    const placed = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
    });
    const { unmount } = renderDialog({
      stage: stage({
        compositionEntryIds: ['e1', 'e2'],
        compositionEntryPots: null,
      }),
      entries: [entry('e1', 'Alpha'), entry('e2', 'Bravo')],
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
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 2,
          },
        }),
      ],
      place: groupPlace({
        formPosition: {
          kind: 'GroupPlace',
          groupId,
          groupName: 'A',
          index: 2,
        },
      }),
    });
    expect(
      screen.getByText(
        'Toutes les équipes de la composition sont déjà placées.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Placer \d+ équipe|Enregistrer/ }),
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
    expect(
      screen.getByRole('combobox', { name: 'Groupe' }),
    ).toBeInTheDocument();
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

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(expected);
    expect(screen.getByRole('alert')).not.toHaveTextContent(code);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    await waitFor(() => expect(invalidate).toHaveBeenCalled());
  });

  it('stays open in blocked state when the CTA batch fails and targets disappear', async () => {
    const user = userEvent.setup();
    vi.mocked(assignEntriesToGroup).mockRejectedValueOnce(
      new ApiError(409, 'locked', 'detail', 'Stage.StructureLocked'),
    );
    const { rerender, queryClient } = renderDialog({ place: null });

    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Placer 1 équipe' }));

    expect(await screen.findByRole('alert')).toBeInTheDocument();

    rerender(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <StructureGroupPlacementDialog
            open
            onClose={vi.fn()}
            competitionId="comp-1"
            stage={stage({ actions: [] })}
            structureStages={[stage({ actions: [] })]}
            entries={[
              entry('e1', 'Alpha'),
              entry('e2', 'Bravo'),
              entry('e3', 'Charlie'),
            ]}
            cases={[]}
            place={null}
          />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('Placement indisponible')).toBeInTheDocument();
  });

  it('shows batch facts and locks tiles at capacity', async () => {
    const user = userEvent.setup();
    renderDialog({ place: null });

    const freeCount = document.querySelector(
      '[data-fact="free"] .structure-qualification__fact-value',
    );
    expect(freeCount).toHaveTextContent('2');

    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('checkbox', { name: 'Charlie' }));
    const locked = screen.getByRole('checkbox', { name: 'Alpha' });
    await waitFor(() =>
      expect(locked).toHaveAttribute('aria-disabled', 'true'),
    );
  });

  it('clears batch selection when the group changes', async () => {
    const user = userEvent.setup();
    const emptyB = groupPlace({
      formPosition: {
        kind: 'GroupPlace',
        groupId: 'group-b',
        groupName: 'B',
        index: 1,
      },
    });
    renderDialog({ place: null, cases: [groupPlace(), emptyB] });

    await user.click(screen.getByRole('checkbox', { name: 'Alpha' }));
    await user.click(screen.getByRole('combobox', { name: 'Groupe' }));
    await user.click(screen.getByRole('option', { name: /Groupe B/ }));
    expect(screen.queryByRole('checkbox', { name: 'Alpha' })).not.toBeChecked();
  });

  it('falls back to PUT when placesPerGroup is absent', async () => {
    const user = userEvent.setup();
    renderDialog({
      place: null,
      stage: stage({ placesPerGroup: null }),
    });

    await user.click(screen.getByRole('radio', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(assignEntryToGroup).toHaveBeenCalledWith(stageId, groupId, 'e1');
    });
  });

  it('disables save when edit selection matches the occupant', () => {
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
    expect(screen.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
  });

  it('announces seatChanged and reads the new occupant on EntryNotFound', async () => {
    const user = userEvent.setup();
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    vi.mocked(replaceEntryInGroup).mockRejectedValueOnce(
      new ApiError(409, 'gone', 'detail', 'Stage.EntryNotFound'),
    );

    const { rerender, queryClient } = renderDialog({
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

    await user.click(screen.getByRole('radio', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    const replaced = groupPlace({
      entry: { entryId: 'e2', displayName: 'Bravo' },
      canClear: true,
    });
    rerender(
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
              replaced,
              groupPlace({
                formPosition: {
                  kind: 'GroupPlace',
                  groupId,
                  groupName: 'A',
                  index: 2,
                },
              }),
            ]}
            place={occupied}
          />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(
      screen.getByText('La place a changé. Vérifiez la sélection.'),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Enregistrer' }),
    ).not.toBeInTheDocument();
  });

  it('does not reset focus to the first tile when toggling batch selection', async () => {
    const user = userEvent.setup();
    renderDialog({ place: null });

    const alpha = screen.getByRole('checkbox', { name: 'Alpha' });
    const bravo = screen.getByRole('checkbox', { name: 'Bravo' });
    const charlie = screen.getByRole('checkbox', { name: 'Charlie' });
    await user.click(charlie);
    expect(charlie).toHaveFocus();
    await user.click(bravo);
    expect(bravo).toHaveFocus();
    expect(alpha).not.toHaveFocus();
  });

  it('focuses the selected occupant tile in edit mode', async () => {
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

    await waitFor(() =>
      expect(
        screen.getByRole('button', {
          name: /Retirer l.équipe de la place/,
        }),
      ).toHaveFocus(),
    );
  });

  it('locks batch selection at one when an inbound feed blocks the other free seat', async () => {
    const user = userEvent.setup();
    const cases = [
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 2 },
        entry: { entryId: 'e2', displayName: 'Bravo' },
      }),
      groupPlace({
        formPosition: {
          kind: 'GroupPlace',
          groupId,
          groupName: 'A',
          index: 3,
        },
        feedOrigin: { kind: 'Qualification', sourceStageId: 'src' },
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 4 },
      }),
    ];
    renderDialog({
      place: null,
      cases,
      stage: stage({ compositionEntryIds: ['e1', 'e2', 'e3', 'e4'] }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
        entry('e4', 'Delta'),
      ],
    });

    await user.click(screen.getByRole('checkbox', { name: 'Charlie' }));
    expect(screen.getByRole('checkbox', { name: 'Delta' })).toHaveAttribute(
      'aria-disabled',
      'true',
    );
    expect(
      screen.queryByRole('button', { name: 'Placer 2 équipes' }),
    ).not.toBeInTheDocument();
  });

  it('replaces using the anchored subject after seat compaction', async () => {
    const user = userEvent.setup();
    const occupied = groupPlace({
      formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    const { rerender, queryClient } = renderDialog({
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

    await user.click(screen.getByRole('radio', { name: 'Bravo' }));
    const compacted = groupPlace({
      formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    rerender(
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
              compacted,
              groupPlace({
                formPosition: {
                  kind: 'GroupPlace',
                  groupId,
                  groupName: 'A',
                  index: 2,
                },
              }),
            ]}
            place={occupied}
          />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));
    await waitFor(() => {
      expect(replaceEntryInGroup).toHaveBeenCalledWith(
        stageId,
        groupId,
        'e1',
        'e2',
      );
    });
  });

  it('announces a cleared selection and keeps focus inside the dialog', async () => {
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
      screen.getByRole('button', { name: /Retirer l.équipe de la place/ }),
    );
    expect(screen.getByText('Sélection effacée')).toBeInTheDocument();
    const dialog = screen.getByRole('dialog');
    await waitFor(() =>
      expect(dialog.contains(document.activeElement)).toBe(true),
    );
  });

  it('uses the group placement lead instead of the cup wording', () => {
    renderDialog({ place: null });
    expect(
      screen.getByText(
        'Choisissez une équipe de la composition de cette phase.',
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText(/coupe/i)).not.toBeInTheDocument();
  });

  it('links the refused batch tile to the alert message', async () => {
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

    const alert = await screen.findByRole('alert');
    const bravo = screen.getByRole('checkbox', { name: 'Bravo' });
    const describedBy = bravo.getAttribute('aria-describedby');
    expect(describedBy).toBeTruthy();
    expect(alert).toHaveAttribute('id', describedBy);
    expect(alert).toHaveTextContent(frErrors.Stage.GroupFull);
  });
});
