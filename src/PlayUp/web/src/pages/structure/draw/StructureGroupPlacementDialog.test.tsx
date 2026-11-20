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
  replaceEntryInGroup,
  setGroupEntries,
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
    setGroupEntries: vi.fn(),
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
      'SetGroupEntries',
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
    canClear: true,
    ...partial,
  };
}

const groupBId = 'group-b';

function groupBPlace(partial: Partial<SchematicCase> = {}): SchematicCase {
  return {
    formPosition: {
      kind: 'GroupPlace',
      groupId: groupBId,
      groupName: 'B',
      index: 1,
    },
    entry: null,
    assignment: null,
    seatMode: 'Editable',
    canClear: true,
    ...partial,
  };
}

function batchFactValue(key: string): string {
  return (
    document
      .querySelector(
        `[data-fact="${key}"] .structure-qualification__fact-value`,
      )
      ?.textContent?.trim() ?? ''
  );
}

function batchFacts() {
  return {
    places: batchFactValue('places'),
    occupied: batchFactValue('occupied'),
  };
}

const placementDialogTitle = (target: string) =>
  `Placement — Phase de groupes — ${target}`;

function renderDialog(
  props: Partial<ComponentProps<typeof StructureGroupPlacementDialog>> = {},
  queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  }),
) {
  const resolvedPlace = props.place !== undefined ? props.place : null;
  const resolvedFixedGroupId =
    props.fixedGroupId !== undefined
      ? props.fixedGroupId
      : props.place == null
        ? groupId
        : null;
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
          {...props}
          place={resolvedPlace}
          fixedGroupId={resolvedFixedGroupId}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
  return { ...view, queryClient };
}

describe('StructureGroupPlacementDialog', () => {
  beforeEach(() => {
    vi.mocked(assignEntryToGroup).mockReset();
    vi.mocked(setGroupEntries).mockReset();
    vi.mocked(clearEntryFromGroup).mockReset();
    vi.mocked(replaceEntryInGroup).mockReset();
    vi.mocked(assignEntryToGroup).mockResolvedValue(undefined);
    vi.mocked(setGroupEntries).mockResolvedValue(undefined);
    vi.mocked(clearEntryFromGroup).mockResolvedValue(undefined);
    vi.mocked(replaceEntryInGroup).mockResolvedValue(undefined);
  });

  it('places one team from the batch dialog with setGroupEntries', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const { queryClient } = renderDialog({ onClose });
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');

    const dialog = screen.getByRole('dialog', {
      name: placementDialogTitle('Groupe A'),
    });
    expect(within(dialog).getAllByText('Pot 1').length).toBeGreaterThan(0);
    expect(within(dialog).getByText('Pot 2')).toBeInTheDocument();

    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(setGroupEntries).toHaveBeenCalledWith(
        stageId,
        groupId,
        ['e2'],
        [],
      );
    });
    expect(assignEntryToGroup).not.toHaveBeenCalled();
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
      place: null,
      fixedGroupId: groupId,
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
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(setGroupEntries).toHaveBeenCalledWith(
        stageId,
        groupId,
        ['e2', 'e3'],
        [],
      );
    });
  });

  it('uses per-seat edit when SetGroupEntries is unavailable', () => {
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: occupied,
      fixedGroupId: null,
      stage: stage({
        actions: [
          'AssignEntryToGroup',
          'ClearEntryFromGroup',
          'ReplaceEntryInGroup',
        ],
      }),
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
    expect(screen.getByRole('radiogroup')).toBeInTheDocument();
    const remove = screen.getByRole('button', { name: 'Retirer' });
    expect(remove).toBeInTheDocument();
    expect(remove).toBeEnabled();
    expect(
      screen.queryByRole('button', { name: 'Compléter le groupe' }),
    ).not.toBeInTheDocument();
  });

  it('always shows Compléter and Retirer on the team section row in batch mode', () => {
    renderDialog({ place: null });
    expect(
      screen.getByRole('button', { name: 'Compléter le groupe' }),
    ).toBeInTheDocument();
    const remove = screen.getByRole('button', { name: 'Retirer' });
    expect(remove).toBeInTheDocument();
    expect(remove).toBeDisabled();
  });

  it('fills the group with the first available teams', async () => {
    const user = userEvent.setup();
    renderDialog({ place: null });

    await user.click(
      screen.getByRole('button', { name: 'Compléter le groupe' }),
    );
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(setGroupEntries).toHaveBeenCalledWith(
        stageId,
        groupId,
        ['e2', 'e3'],
        [],
      );
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
    expect(setGroupEntries).not.toHaveBeenCalled();
  });

  it('replaces a team in one batch save', async () => {
    const user = userEvent.setup();
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: null,
      fixedGroupId: groupId,
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
        name: placementDialogTitle('Groupe A'),
      }),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('checkbox', { name: 'Alpha' }));
    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(setGroupEntries).toHaveBeenCalledWith(
        stageId,
        groupId,
        ['e2'],
        ['e1'],
      );
    });
    expect(replaceEntryInGroup).not.toHaveBeenCalled();
    expect(clearEntryFromGroup).not.toHaveBeenCalled();
    expect(assignEntryToGroup).not.toHaveBeenCalled();
  });

  it('removes a member via batch uncheck then save', async () => {
    const user = userEvent.setup();
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: null,
      fixedGroupId: groupId,
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

    await user.click(screen.getByRole('checkbox', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => {
      expect(setGroupEntries).toHaveBeenCalledWith(
        stageId,
        groupId,
        [],
        ['e1'],
      );
    });
    expect(clearEntryFromGroup).not.toHaveBeenCalled();
  });

  it('names the refused team when batch assign returns entryId', async () => {
    const user = userEvent.setup();
    vi.mocked(setGroupEntries).mockRejectedValueOnce(
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
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Bravo');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('shows a fixed group title when opened from the menu without a seat', () => {
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
      fixedGroupId: groupId,
    });
    expect(
      screen.queryByRole('combobox', { name: 'Groupe' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('dialog', {
        name: placementDialogTitle('Groupe A'),
      }),
    ).toBeInTheDocument();
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
    vi.mocked(setGroupEntries).mockRejectedValueOnce(
      new ApiError(409, 'conflict', 'detail', code),
    );
    const { queryClient } = renderDialog();
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');

    await user.click(screen.getByRole('checkbox', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(expected);
    expect(screen.getByRole('alert')).not.toHaveTextContent(code);
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    await waitFor(() => expect(invalidate).toHaveBeenCalled());
  });

  it('stays open in blocked state when the CTA batch fails and targets disappear', async () => {
    const user = userEvent.setup();
    vi.mocked(setGroupEntries).mockRejectedValueOnce(
      new ApiError(409, 'locked', 'detail', 'Stage.StructureLocked'),
    );
    const { rerender, queryClient } = renderDialog({ place: null });

    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

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
            fixedGroupId={groupId}
          />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('Placement indisponible')).toBeInTheDocument();
    const blockedHost = screen
      .getByText('Placement indisponible')
      .closest('[tabindex="-1"]');
    await waitFor(() => expect(blockedHost).toHaveFocus(), { timeout: 3000 });
  });

  it('opens batch mode on the chosen group even when another group is full', () => {
    const fullA = [1, 2, 3, 4].map((index) =>
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index },
        entry: { entryId: `e${index}`, displayName: `T${index}` },
      }),
    );
    const partialB = groupBPlace({
      formPosition: {
        kind: 'GroupPlace',
        groupId: groupBId,
        groupName: 'B',
        index: 1,
      },
    });
    renderDialog({
      place: null,
      fixedGroupId: groupBId,
      cases: [...fullA, partialB],
      stage: stage({
        compositionEntryIds: ['e1', 'e2', 'e3', 'e4', 'e5', 'e6', 'e7'],
      }),
      entries: [
        entry('e1', 'T1'),
        entry('e2', 'T2'),
        entry('e3', 'T3'),
        entry('e4', 'T4'),
        entry('e5', 'Echo'),
        entry('e6', 'Foxtrot'),
        entry('e7', 'Golf'),
      ],
    });

    expect(
      screen.getByRole('dialog', {
        name: placementDialogTitle('Groupe B'),
      }),
    ).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Echo' })).toBeInTheDocument();
  });

  it.each([
    {
      label: '4/2/1',
      cases: [
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 1,
          },
          entry: { entryId: 'e1', displayName: 'Alpha' },
        }),
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 2,
          },
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
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 4,
          },
        }),
      ],
      expected: {
        places: '4',
        occupied: '2',
      },
    },
    {
      label: '4/3/2',
      cases: [
        ...[1, 2, 3].map((index) =>
          groupPlace({
            formPosition: {
              kind: 'GroupPlace',
              groupId,
              groupName: 'A',
              index,
            },
            entry: {
              entryId: `e${index}`,
              displayName: ['Alpha', 'Bravo', 'Charlie'][index - 1],
            },
          }),
        ),
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 4,
          },
          feedOrigin: { kind: 'Qualification', sourceStageId: 'src' },
        }),
      ],
      expected: {
        places: '4',
        occupied: '3',
      },
    },
    {
      label: '4/5/0',
      cases: [
        ...[1, 2, 3, 4].map((index) =>
          groupPlace({
            formPosition: {
              kind: 'GroupPlace',
              groupId,
              groupName: 'A',
              index,
            },
            entry: { entryId: `e${index}`, displayName: `T${index}` },
          }),
        ),
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 5,
          },
          entry: { entryId: 'e5', displayName: 'Echo' },
          excess: true,
        }),
      ],
      expected: {
        places: '4',
        occupied: '5',
      },
    },
  ])('shows batch facts for case $label', ({ cases: schematic, expected }) => {
    renderDialog({
      place: null,
      cases: schematic,
      stage: stage({
        compositionEntryIds: ['e1', 'e2', 'e3', 'e4', 'e5'],
      }),
      entries: [
        entry('e1', 'T1'),
        entry('e2', 'T2'),
        entry('e3', 'T3'),
        entry('e4', 'T4'),
        entry('e5', 'Echo'),
      ],
    });
    expect(batchFacts()).toEqual({
      places: expected.places,
      occupied: expected.occupied,
    });
  });

  it('shows group members checked and clearable in batch mode', () => {
    const cases = [
      ...[1, 2, 3].map((index) =>
        groupPlace({
          formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index },
          entry: {
            entryId: `e${index}`,
            displayName: ['Alpha', 'Bravo', 'Charlie'][index - 1],
          },
        }),
      ),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 4 },
      }),
    ];
    renderDialog({ place: null, cases });

    for (const name of ['Alpha', 'Bravo', 'Charlie']) {
      const tile = screen.getByRole('checkbox', { name });
      expect(tile).toHaveAttribute('aria-checked', 'true');
      expect(tile).not.toHaveAttribute('aria-disabled', 'true');
    }
  });

  it('treats locked members as display-only in batch placement', async () => {
    const user = userEvent.setup();
    const cases = [
      ...[1, 2, 3].map((index) =>
        groupPlace({
          formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index },
          entry: {
            entryId: `e${index}`,
            displayName: ['Alpha', 'Bravo', 'Charlie'][index - 1],
          },
          canClear: false,
        }),
      ),
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

    const member = screen.getByRole('checkbox', { name: 'Alpha' });
    member.focus();
    expect(member).toHaveFocus();

    await user.click(screen.getByRole('checkbox', { name: 'Delta' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));
    await waitFor(() => {
      expect(setGroupEntries).toHaveBeenCalledWith(
        stageId,
        groupId,
        ['e1', 'e2', 'e3', 'e4'],
        ['e1', 'e2', 'e3'],
      );
    });
  });

  it('closes batch placement without discard when only members are shown', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const cases = [
      ...[1, 2, 3].map((index) =>
        groupPlace({
          formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index },
          entry: {
            entryId: `e${index}`,
            displayName: ['Alpha', 'Bravo', 'Charlie'][index - 1],
          },
        }),
      ),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 4 },
      }),
    ];
    renderDialog({
      place: null,
      cases,
      onClose,
      stage: stage({ compositionEntryIds: ['e1', 'e2', 'e3'] }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
      ],
    });
    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /Enregistrer/ })).toBeNull();
    });
    await user.click(screen.getByRole('button', { name: 'Annuler' }));
    expect(onClose).toHaveBeenCalled();
    expect(
      screen.queryByRole('dialog', { name: 'Quitter sans enregistrer ?' }),
    ).not.toBeInTheDocument();
  });

  it('grays out teams from another group without affecting batch selection', async () => {
    const user = userEvent.setup();
    const cases = [
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 2 },
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 3 },
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 4 },
      }),
      groupBPlace({
        formPosition: {
          kind: 'GroupPlace',
          groupId: groupBId,
          groupName: 'B',
          index: 1,
        },
        entry: { entryId: 'e2', displayName: 'Bravo' },
      }),
    ];
    renderDialog({
      place: null,
      cases,
      stage: stage({ compositionEntryIds: ['e1', 'e2', 'e3'] }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
      ],
    });

    const other = screen.getByRole('checkbox', { name: 'Bravo' });
    expect(other).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByText('Dans le Groupe B')).toBeInTheDocument();

    await user.click(
      screen.getByRole('button', { name: 'Compléter le groupe' }),
    );
    expect(setGroupEntries).not.toHaveBeenCalled();
    expect(screen.getByRole('checkbox', { name: 'Alpha' })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    expect(screen.getByRole('checkbox', { name: 'Charlie' })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    expect(screen.getByRole('checkbox', { name: 'Bravo' })).toHaveAttribute(
      'aria-checked',
      'false',
    );
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));
    await waitFor(() => {
      expect(setGroupEntries).toHaveBeenCalledWith(
        stageId,
        groupId,
        expect.arrayContaining(['e1', 'e3']),
        [],
      );
    });
    expect(vi.mocked(setGroupEntries).mock.calls[0]?.[2]).toHaveLength(2);
    expect(setGroupEntries).not.toHaveBeenCalledWith(
      stageId,
      groupId,
      expect.arrayContaining(['e2']),
    );
  });

  it('shows batch facts and locks tiles at capacity', async () => {
    const user = userEvent.setup();
    renderDialog({ place: null });

    expect(batchFacts()).toEqual({ places: '4', occupied: '0' });

    await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
    await user.click(screen.getByRole('checkbox', { name: 'Charlie' }));
    const locked = screen.getByRole('checkbox', { name: 'Alpha' });
    await waitFor(() =>
      expect(locked).toHaveAttribute('aria-disabled', 'true'),
    );
  });

  it('keeps the target group fixed when opened from the menu', () => {
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
      fixedGroupId: groupId,
    });
    expect(
      screen.getByRole('dialog', {
        name: placementDialogTitle('Groupe A'),
      }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('combobox', { name: 'Groupe' }),
    ).not.toBeInTheDocument();
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

  it('disables save when batch selection matches current members', () => {
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: null,
      fixedGroupId: groupId,
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
      fixedGroupId: null,
      stage: stage({
        actions: [
          'AssignEntryToGroup',
          'ClearEntryFromGroup',
          'ReplaceEntryInGroup',
        ],
      }),
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
    expect(screen.getByRole('radio', { name: 'Bravo' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
  });

  // Temporary CI retry; remove once the root cause is fixed.
  // Notion: https://app.notion.com/p/41d75fb92d1081eca471dd6b3c461260
  it(
    'does not reset focus to the first tile when toggling batch selection',
    { retry: 2 },
    async () => {
      const user = userEvent.setup();
      renderDialog({ place: null });

      await user.click(screen.getByRole('checkbox', { name: 'Charlie' }));
      await waitFor(() =>
        expect(screen.getByRole('checkbox', { name: 'Charlie' })).toHaveFocus(),
      );
      await user.click(screen.getByRole('checkbox', { name: 'Bravo' }));
      await waitFor(() =>
        expect(screen.getByRole('checkbox', { name: 'Bravo' })).toHaveFocus(),
      );
      expect(screen.getByRole('checkbox', { name: 'Alpha' })).not.toHaveFocus();
    },
  );

  it('focuses the selected occupant tile in legacy edit mode', async () => {
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: occupied,
      fixedGroupId: null,
      stage: stage({
        actions: [
          'AssignEntryToGroup',
          'ClearEntryFromGroup',
          'ReplaceEntryInGroup',
        ],
      }),
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
      expect(screen.getByRole('radio', { name: 'Alpha' })).toHaveFocus(),
    );
  });

  it('locks batch selection at one when an inbound feed blocks the other free seat', async () => {
    const user = userEvent.setup();
    const cases = [
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
        entry: { entryId: 'e1', displayName: 'Alpha' },
        canClear: false,
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 2 },
        entry: { entryId: 'e2', displayName: 'Bravo' },
        canClear: false,
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
    const ySeat = groupPlace({
      formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
      entry: { entryId: 'e2', displayName: 'Bravo' },
      canClear: true,
    });
    const xSeat = groupPlace({
      formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 2 },
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    const { rerender, queryClient } = renderDialog({
      place: xSeat,
      fixedGroupId: null,
      stage: stage({
        actions: [
          'AssignEntryToGroup',
          'ClearEntryFromGroup',
          'ReplaceEntryInGroup',
        ],
      }),
      cases: [ySeat, xSeat],
    });

    await user.click(screen.getByRole('radio', { name: 'Charlie' }));
    const compactedX = groupPlace({
      formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    const replaceStage = stage({
      actions: [
        'AssignEntryToGroup',
        'ClearEntryFromGroup',
        'ReplaceEntryInGroup',
      ],
    });
    rerender(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <StructureGroupPlacementDialog
            open
            onClose={vi.fn()}
            competitionId="comp-1"
            stage={replaceStage}
            structureStages={[replaceStage]}
            entries={[
              entry('e1', 'Alpha'),
              entry('e2', 'Bravo'),
              entry('e3', 'Charlie'),
            ]}
            cases={[
              compactedX,
              groupPlace({
                formPosition: {
                  kind: 'GroupPlace',
                  groupId,
                  groupName: 'A',
                  index: 2,
                },
              }),
            ]}
            place={xSeat}
            fixedGroupId={null}
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
        'e3',
      );
    });
    expect(replaceEntryInGroup).not.toHaveBeenCalledWith(
      stageId,
      groupId,
      'e2',
      expect.anything(),
    );
  });

  it('announces a cleared selection and keeps focus inside the dialog', async () => {
    const user = userEvent.setup();
    const occupied = groupPlace({
      entry: { entryId: 'e1', displayName: 'Alpha' },
      canClear: true,
    });
    renderDialog({
      place: occupied,
      fixedGroupId: null,
      stage: stage({
        actions: [
          'AssignEntryToGroup',
          'ClearEntryFromGroup',
          'ReplaceEntryInGroup',
        ],
      }),
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

    const remove = screen.getByRole('button', { name: 'Retirer' });
    expect(remove).toBeEnabled();
    await user.click(remove);
    expect(remove).toBeDisabled();
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

  it('allows swap on a full group when a clearable member is unchecked', async () => {
    const user = userEvent.setup();
    const cases = [1, 2, 3, 4].map((index) =>
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index },
        entry: {
          entryId: `e${index}`,
          displayName: ['Alpha', 'Bravo', 'Charlie', 'Delta'][index - 1],
        },
        canClear: index === 4,
      }),
    );
    renderDialog({
      place: null,
      cases,
      stage: stage({ compositionEntryIds: ['e1', 'e2', 'e3', 'e4', 'e5'] }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
        entry('e4', 'Delta'),
        entry('e5', 'Echo'),
      ],
    });

    await user.click(screen.getByRole('checkbox', { name: 'Delta' }));
    await user.click(screen.getByRole('checkbox', { name: 'Echo' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    await waitFor(() => expect(setGroupEntries).toHaveBeenCalled());
    const [, , payload, expected] = vi
      .mocked(setGroupEntries)
      .mock.calls.at(-1)!;
    expect(payload).toEqual(expect.arrayContaining(['e1', 'e2', 'e3', 'e5']));
    expect(payload).toHaveLength(4);
    expect(expected).toEqual(expect.arrayContaining(['e1', 'e2', 'e3', 'e4']));
  });

  it('disables save when additions exceed the net cap on a full group', async () => {
    const user = userEvent.setup();
    const cases = [1, 2, 3, 4].map((index) =>
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index },
        entry: {
          entryId: `e${index}`,
          displayName: ['Alpha', 'Bravo', 'Charlie', 'Delta'][index - 1],
        },
        canClear: true,
      }),
    );
    renderDialog({
      place: null,
      cases,
      stage: stage({
        compositionEntryIds: ['e1', 'e2', 'e3', 'e4', 'e5', 'e6'],
      }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
        entry('e4', 'Delta'),
        entry('e5', 'Echo'),
        entry('e6', 'Foxtrot'),
      ],
    });

    await user.click(screen.getByRole('checkbox', { name: 'Echo' }));
    await user.click(screen.getByRole('checkbox', { name: 'Foxtrot' }));
    expect(screen.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
  });

  it('announces hub membership refresh in batch mode (A1)', async () => {
    const { rerender } = renderDialog({
      place: null,
      cases: [
        groupPlace({
          entry: { entryId: 'e4', displayName: 'Delta' },
        }),
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 2,
          },
        }),
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 3,
          },
        }),
        groupPlace({
          formPosition: {
            kind: 'GroupPlace',
            groupId,
            groupName: 'A',
            index: 4,
          },
        }),
      ],
      stage: stage({ compositionEntryIds: ['e1', 'e2', 'e3', 'e4'] }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
        entry('e4', 'Delta'),
      ],
    });

    rerender(
      <QueryClientProvider
        client={
          new QueryClient({
            defaultOptions: {
              queries: { retry: false },
              mutations: { retry: false },
            },
          })
        }
      >
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
              entry('e4', 'Delta'),
            ]}
            cases={[
              groupPlace({
                entry: { entryId: 'e4', displayName: 'Delta' },
              }),
              groupPlace({
                entry: { entryId: 'e1', displayName: 'Alpha' },
                formPosition: {
                  kind: 'GroupPlace',
                  groupId,
                  groupName: 'A',
                  index: 2,
                },
              }),
              groupPlace({
                formPosition: {
                  kind: 'GroupPlace',
                  groupId,
                  groupName: 'A',
                  index: 3,
                },
              }),
              groupPlace({
                formPosition: {
                  kind: 'GroupPlace',
                  groupId,
                  groupName: 'A',
                  index: 4,
                },
              }),
            ]}
            place={null}
            fixedGroupId={groupId}
          />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByRole('checkbox', { name: 'Alpha' })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    expect(screen.getByText('Le groupe a changé')).toBeInTheDocument();
  });

  it('surfaces membership stale without reseeding before hub refresh (M2)', async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.mocked(setGroupEntries).mockRejectedValueOnce(
      new ApiError(
        409,
        'conflict',
        'detail',
        'Application.GroupMembershipStale',
      ),
    );
    renderDialog(
      {
        place: null,
        cases: [
          groupPlace({
            entry: { entryId: 'e1', displayName: 'Alpha' },
          }),
          groupPlace({
            formPosition: {
              kind: 'GroupPlace',
              groupId,
              groupName: 'A',
              index: 2,
            },
          }),
          groupPlace({
            formPosition: {
              kind: 'GroupPlace',
              groupId,
              groupName: 'A',
              index: 3,
            },
          }),
          groupPlace({
            formPosition: {
              kind: 'GroupPlace',
              groupId,
              groupName: 'A',
              index: 4,
            },
          }),
        ],
      },
      queryClient,
    );
    await user.click(screen.getByRole('checkbox', { name: 'Alpha' }));
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));
    expect(await screen.findByText('Le groupe a changé')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Alpha' })).toHaveAttribute(
      'aria-checked',
      'false',
    );
  });

  it('Retirer clears decheckable selection and announces removals', async () => {
    const user = userEvent.setup();
    const cases = [
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 1 },
        entry: { entryId: 'e1', displayName: 'Alpha' },
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 2 },
        entry: { entryId: 'e2', displayName: 'Bravo' },
        canClear: false,
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 3 },
      }),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 4 },
      }),
    ];
    renderDialog({
      place: null,
      cases,
      stage: stage({ compositionEntryIds: ['e1', 'e2', 'e3'] }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
      ],
    });
    await user.click(screen.getByRole('checkbox', { name: 'Charlie' }));
    await user.click(screen.getByRole('button', { name: 'Retirer' }));
    await waitFor(() => {
      expect(
        screen.getByText('1 équipe sera retirée', { exact: false }),
      ).toBeInTheDocument();
    });
    expect(screen.getByRole('checkbox', { name: 'Alpha' })).toHaveAttribute(
      'aria-checked',
      'false',
    );
    expect(screen.getByRole('checkbox', { name: 'Bravo' })).toHaveAttribute(
      'aria-checked',
      'true',
    );
    expect(screen.getByRole('checkbox', { name: 'Charlie' })).toHaveAttribute(
      'aria-checked',
      'false',
    );
  });

  it('disables Retirer and keeps members checked in additions-only mode (Q11)', () => {
    const cases = [
      ...[1, 2, 3, 4].map((index) =>
        groupPlace({
          formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index },
          entry: {
            entryId: `e${index}`,
            displayName: ['Alpha', 'Bravo', 'Charlie', 'Delta'][index - 1],
          },
          canClear: false,
        }),
      ),
      groupPlace({
        formPosition: { kind: 'GroupPlace', groupId, groupName: 'A', index: 5 },
        seatMode: 'MatchLocked',
      }),
    ];
    renderDialog({
      place: null,
      cases,
      stage: stage({
        status: 'Ready',
        compositionEntryIds: ['e1', 'e2', 'e3', 'e4', 'e5'],
      }),
      entries: [
        entry('e1', 'Alpha'),
        entry('e2', 'Bravo'),
        entry('e3', 'Charlie'),
        entry('e4', 'Delta'),
        entry('e5', 'Echo'),
      ],
    });
    expect(screen.getByRole('button', { name: 'Retirer' })).toBeDisabled();
    for (const name of ['Alpha', 'Bravo', 'Charlie', 'Delta']) {
      expect(screen.getByRole('checkbox', { name })).toHaveAttribute(
        'aria-checked',
        'true',
      );
    }
  });

  it('links the refused batch tile to the alert message', async () => {
    const user = userEvent.setup();
    vi.mocked(setGroupEntries).mockRejectedValueOnce(
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
    await user.click(screen.getByRole('button', { name: 'Enregistrer' }));

    const alert = await screen.findByRole('alert');
    const bravo = screen.getByRole('checkbox', { name: 'Bravo' });
    const describedBy = bravo.getAttribute('aria-describedby');
    expect(describedBy).toBeTruthy();
    expect(alert).toHaveAttribute('id', describedBy);
    expect(alert).toHaveTextContent(frErrors.Stage.GroupFull);
  });
});
