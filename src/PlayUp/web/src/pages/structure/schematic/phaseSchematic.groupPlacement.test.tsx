import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { TOOLTIP_DELAY_OPEN_MS } from '../../../design-system/components/Tooltip';
import i18n from '../../../i18n';
import type { SchematicCase, StageSchematic } from '../../../types';
import { PhaseSchematic } from './phaseSchematic';

await i18n.changeLanguage('fr');

const actions = ['AssignEntryToGroup', 'ClearEntryFromGroup'];

function seat(
  groupId: string,
  groupName: string,
  index: number,
  partial: Partial<SchematicCase> = {},
): SchematicCase {
  return {
    formPosition: { kind: 'GroupPlace', groupId, groupName, index },
    entry: null,
    assignment: null,
    seatMode: 'Editable',
    canClear: false,
    ...partial,
  };
}

function schematic(cases: SchematicCase[]): StageSchematic {
  return {
    stageId: 's1',
    competitionId: 'c1',
    name: 'Poules',
    status: 'Draft',
    formatKind: 'Groups',
    cases,
    connections: [],
  };
}

describe('Groups schematic placement', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('clicks only place and remove seats and shows the pool counter', async () => {
    const user = userEvent.setup();
    const onGroupPlaceActivate = vi.fn();
    const onPlaceActivate = vi.fn();
    render(
      <PhaseSchematic
        schematic={schematic([
          seat('g-a', 'A', 1, {
            entry: { entryId: 'e1', displayName: 'Alpha' },
            seatMode: 'Draw',
            canClear: false,
          }),
          seat('g-a', 'A', 2, {
            entry: { entryId: 'e2', displayName: 'Bravo' },
            canClear: true,
          }),
          seat('g-a', 'A', 3, {
            entry: { entryId: 'e3', displayName: 'Charlie' },
            canClear: true,
          }),
          seat('g-a', 'A', 4, {
            entry: { entryId: 'e4', displayName: 'Delta' },
            canClear: true,
          }),
          seat('g-a', 'A', 5, {
            entry: { entryId: 'e5', displayName: 'Extra' },
            canClear: true,
            excess: true,
          }),
          seat('g-b', 'B', 1),
        ])}
        placesPerGroup={4}
        groupActions={actions}
        onGroupPlaceActivate={onGroupPlaceActivate}
        onPlaceActivate={onPlaceActivate}
      />,
    );

    expect(screen.getByText('5/4 · En trop')).toBeInTheDocument();
    expect(screen.getAllByText('En trop').length).toBeGreaterThan(0);
    expect(
      screen.getAllByRole('button', { name: 'Cliquer pour placer une équipe' }),
    ).toHaveLength(1);
    expect(
      screen.getAllByRole('button', { name: 'Cliquer pour retirer l’équipe' }),
    ).toHaveLength(4);

    await user.click(
      screen.getByRole('button', { name: 'Cliquer pour placer une équipe' }),
    );
    expect(onGroupPlaceActivate).toHaveBeenCalledOnce();
    expect(onPlaceActivate).not.toHaveBeenCalled();
    expect(
      screen.queryByRole('button', { name: 'Alpha' }),
    ).not.toBeInTheDocument();
  });

  it('shows the draw lock before click and does not call the seat manual', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      configurable: true,
      value: (query: string) => ({
        matches:
          query.includes('hover: hover') && query.includes('pointer: fine'),
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <PhaseSchematic
        schematic={schematic([
          seat('g-a', 'A', 1, {
            entry: { entryId: 'e1', displayName: 'Alpha' },
            seatMode: 'Draw',
            canClear: false,
          }),
        ])}
        placesPerGroup={4}
        groupActions={actions}
        onGroupPlaceActivate={vi.fn()}
      />,
    );

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
    await user.hover(screen.getByText('Alpha'));
    await act(async () => {
      vi.advanceTimersByTime(TOOLTIP_DELAY_OPEN_MS);
    });
    const tip = await screen.findByRole('tooltip');
    expect(tip).toHaveTextContent(
      'Cette équipe a été placée par le tirage appliqué. Elle est protégée.',
    );
    expect(tip).not.toHaveTextContent('à la main');
    expect(tip).not.toHaveTextContent('manuellement');
  });

  it('offers no group action when the server actions are absent', () => {
    render(
      <PhaseSchematic
        schematic={schematic([
          seat('g-a', 'A', 1, { seatMode: 'Editable', canClear: true }),
        ])}
        placesPerGroup={4}
        groupActions={[]}
        onGroupPlaceActivate={vi.fn()}
      />,
    );
    expect(
      screen.queryByRole('button', { name: /Cliquer pour/ }),
    ).not.toBeInTheDocument();
  });

  it('keeps an empty Cup place clickable', async () => {
    const user = userEvent.setup();
    const onPlaceActivate = vi.fn();
    render(
      <PhaseSchematic
        schematic={{
          stageId: 's1',
          competitionId: 'c1',
          name: 'Coupe',
          status: 'Draft',
          formatKind: 'Cup',
          connections: [],
          cases: [
            {
              formPosition: { kind: 'CupSlot', slotKey: 'R1-P1-A' },
              entry: null,
              assignment: null,
            },
          ],
        }}
        onPlaceActivate={onPlaceActivate}
      />,
    );
    const button = screen.getByRole('button');
    await user.click(button);
    expect(onPlaceActivate).toHaveBeenCalledOnce();
  });
});
