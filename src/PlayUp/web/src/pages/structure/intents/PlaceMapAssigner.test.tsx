import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { Dialog } from '../../../design-system/components/Dialog';
import { PlaceMapAssigner } from './PlaceMapAssigner';
import {
  fillPlaceMap,
  groupPlaceMapSections,
  placeMapBoardScrolls,
} from './placeMapAssignerModel';

const boardAria = 'Associer les places';

function source(index: number) {
  return { key: `s${index}`, label: `Source ${index + 1}` };
}

function place(index: number, sectionLabel: string | null = null) {
  return {
    id: `p${index}`,
    label: `Place ${index + 1}`,
    sectionLabel,
  };
}

function renderAssigner({
  sourceCount = 2,
  placeCount = 2,
  assignments = ['', ''],
  uniquePlaces = true,
  sectionLabel = null as string | null,
  onAssignmentsChange = vi.fn(),
}: {
  sourceCount?: number;
  placeCount?: number;
  assignments?: string[];
  uniquePlaces?: boolean;
  sectionLabel?: string | null;
  onAssignmentsChange?: (next: string[]) => void;
} = {}) {
  const sources = Array.from({ length: sourceCount }, (_, index) =>
    source(index),
  );
  const places = Array.from({ length: placeCount }, (_, index) =>
    place(index, sectionLabel),
  );
  render(
    <PlaceMapAssigner
      sources={sources}
      places={places}
      assignments={assignments}
      uniquePlaces={uniquePlaces}
      heading="Emplacements"
      boardAriaLabel={boardAria}
      fillLabel="Remplir"
      fillHint="Les choix déjà faits sont conservés."
      onAssignmentsChange={onAssignmentsChange}
    />,
  );
  return { onAssignmentsChange, sources, places };
}

function Stateful({
  initial,
  uniquePlaces = true,
  places,
  sources,
}: {
  initial: string[];
  uniquePlaces?: boolean;
  places: { id: string; label: string; sectionLabel?: string | null }[];
  sources: { key: string; label: string }[];
}) {
  const [assignments, setAssignments] = useState(initial);
  return (
    <PlaceMapAssigner
      sources={sources}
      places={places}
      assignments={assignments}
      uniquePlaces={uniquePlaces}
      boardAriaLabel={boardAria}
      fillLabel="Remplir"
      fillHint="Les choix déjà faits sont conservés."
      onAssignmentsChange={setAssignments}
    />
  );
}

describe('place map model', () => {
  it('fills empty slot indexes without overwriting a manual pick', () => {
    expect(fillPlaceMap(['KEEP', '', ''], ['KEEP', 'B', 'C'], true)).toEqual([
      'KEEP',
      'B',
      'C',
    ]);
  });

  it('reuses group ids when filling empties', () => {
    expect(fillPlaceMap(['G', ''], ['G'], false)).toEqual(['G', 'G']);
  });

  it('sections cup boards from 8 places and scrolls from 16', () => {
    const seven = Array.from({ length: 7 }, (_, index) =>
      place(index, 'Huitièmes'),
    );
    expect(groupPlaceMapSections(seven, 'Autres places')).toEqual([
      { label: null, places: seven },
    ]);

    const eight = Array.from({ length: 8 }, (_, index) =>
      place(index, index < 4 ? 'Huitièmes' : 'Quarts'),
    );
    expect(
      groupPlaceMapSections(eight, 'Autres places').map((s) => s.label),
    ).toEqual(['Huitièmes', 'Quarts']);
    expect(placeMapBoardScrolls(15)).toBe(false);
    expect(placeMapBoardScrolls(16)).toBe(true);
  });
});

describe('PlaceMapAssigner', () => {
  it('writes the place id on the source chosen from the cell popover', async () => {
    const user = userEvent.setup();
    const { onAssignmentsChange } = renderAssigner();

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Place 1, libre' }));
    const popover = screen.getByRole('listbox', { name: /sources à placer/i });
    await user.click(
      within(popover).getByRole('option', { name: 'Choisir Source 2' }),
    );

    expect(onAssignmentsChange).toHaveBeenCalledTimes(1);
    expect(onAssignmentsChange).toHaveBeenCalledWith(['', 'p0']);
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('closes the popover on Escape without writing', async () => {
    const user = userEvent.setup();
    const { onAssignmentsChange } = renderAssigner();

    await user.click(screen.getByRole('button', { name: 'Place 1, libre' }));
    expect(
      screen.getByRole('listbox', { name: /sources à placer/i }),
    ).toBeInTheDocument();
    await user.keyboard('{Escape}');

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(onAssignmentsChange).not.toHaveBeenCalled();
  });

  it('lets Escape close the popover before the dialog closes', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <div className="ds-root">
        <Dialog
          open
          onClose={onClose}
          title="Places"
          closeLabel="Fermer"
          size="lg"
        >
          <PlaceMapAssigner
            sources={[source(0), source(1)]}
            places={[place(0), place(1)]}
            assignments={['', '']}
            uniquePlaces
            boardAriaLabel={boardAria}
            fillLabel="Remplir"
            fillHint="hint"
            onAssignmentsChange={vi.fn()}
          />
        </Dialog>
      </div>,
    );

    await user.click(screen.getByRole('button', { name: 'Place 1, libre' }));
    expect(
      screen.getByRole('listbox', { name: /sources à placer/i }),
    ).toBeInTheDocument();
    await user.keyboard('{Escape}');

    expect(onClose).not.toHaveBeenCalled();
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();

    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('clears a filled cell back onto the source index', async () => {
    const user = userEvent.setup();
    const onAssignmentsChange = vi.fn();
    renderAssigner({
      sourceCount: 1,
      placeCount: 1,
      assignments: ['p0'],
      onAssignmentsChange,
    });

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    await user.click(
      screen.getByRole('button', { name: 'Retirer Source 1 de Place 1' }),
    );
    expect(onAssignmentsChange).toHaveBeenCalledWith(['']);
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('clears a filled cup hole and reopens the popover to remap it', async () => {
    const user = userEvent.setup();
    render(
      <Stateful
        initial={['p0', '']}
        sources={[source(0), source(1)]}
        places={[place(0), place(1)]}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Place 1, Source 1' }));
    const popover = screen.getByRole('listbox', { name: /sources à placer/i });
    expect(
      within(popover).getByRole('option', { name: 'Choisir Source 1' }),
    ).toBeInTheDocument();
    await user.click(
      within(popover).getByRole('option', { name: 'Choisir Source 2' }),
    );

    expect(
      screen.getByRole('button', { name: 'Place 1, Source 2' }),
    ).toBeInTheDocument();
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('fills empties from the toolbar without overwriting', async () => {
    const user = userEvent.setup();
    const onAssignmentsChange = vi.fn();
    render(
      <PlaceMapAssigner
        sources={[
          { key: 'kept', label: 'Déjà' },
          { key: 'empty', label: 'Vide' },
        ]}
        places={[
          { id: 'KEEP', label: 'Gardée' },
          { id: 'NEXT', label: 'Suivante' },
        ]}
        assignments={['KEEP', '']}
        uniquePlaces
        boardAriaLabel={boardAria}
        fillLabel="Remplir"
        fillHint="Les choix déjà faits sont conservés."
        onAssignmentsChange={onAssignmentsChange}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Remplir' }));
    expect(onAssignmentsChange).toHaveBeenCalledWith(['KEEP', 'NEXT']);
  });

  it('allows the same group on several sources', async () => {
    const user = userEvent.setup();
    render(
      <Stateful
        uniquePlaces={false}
        initial={['', '']}
        sources={[source(0), source(1)]}
        places={[{ id: 'G', label: 'Groupe A', sectionLabel: null }]}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Groupe A, libre' }));
    await user.click(
      within(
        screen.getByRole('listbox', { name: /sources à placer/i }),
      ).getByRole('option', { name: 'Choisir Source 1' }),
    );
    await user.click(
      screen.getByRole('button', { name: 'Groupe A, Source 1' }),
    );
    await user.click(
      within(
        screen.getByRole('listbox', { name: /sources à placer/i }),
      ).getByRole('option', { name: 'Choisir Source 2' }),
    );

    const hole = screen.getByRole('button', {
      name: 'Groupe A, Source 1, Source 2',
    });
    expect(hole).toBeInTheDocument();
    await user.click(hole);
    expect(
      screen.getByText(/toutes les sources sont placées/i),
    ).toBeInTheDocument();
  });

  it('lists only unplaced sources for a free cup hole', async () => {
    const user = userEvent.setup();
    renderAssigner({
      assignments: ['p0', ''],
      placeCount: 2,
    });

    await user.click(screen.getByRole('button', { name: 'Place 2, libre' }));
    const popover = screen.getByRole('listbox', { name: /sources à placer/i });
    expect(
      within(popover).getByRole('option', { name: 'Choisir Source 2' }),
    ).toBeInTheDocument();
    expect(
      within(popover).queryByRole('option', { name: /Source 1/ }),
    ).not.toBeInTheDocument();
  });

  it('exposes a grid and a live count, not a radiogroup, and scrolls at 16', async () => {
    const user = userEvent.setup();
    const places = Array.from({ length: 16 }, (_, index) =>
      place(index, index < 8 ? 'Huitièmes' : 'Quarts'),
    );
    render(
      <PlaceMapAssigner
        sources={Array.from({ length: 16 }, (_, index) => source(index))}
        places={places}
        assignments={Array.from({ length: 16 }, () => '')}
        uniquePlaces
        boardAriaLabel={boardAria}
        fillLabel="Remplir"
        fillHint="hint"
        onAssignmentsChange={vi.fn()}
      />,
    );

    const grid = screen.getByRole('grid', { name: boardAria });
    expect(grid).toHaveAttribute('data-sectioned', 'true');
    expect(screen.queryByRole('radiogroup')).not.toBeInTheDocument();
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(
      document.querySelector('.structure-place-map-assigner__tray'),
    ).toBeNull();
    expect(screen.getByText('Huitièmes')).toBeInTheDocument();
    expect(screen.getByText('Quarts')).toBeInTheDocument();
    expect(screen.getByText('0/16')).toBeInTheDocument();
    expect(screen.getByText('0 sur 16 placées')).toBeInTheDocument();

    const board = document.querySelector(
      '.structure-place-map-assigner__board',
    );
    const toolbar = document.querySelector(
      '.structure-place-map-assigner__toolbar',
    );
    expect(board).toHaveAttribute('data-scroll', 'true');
    expect(board?.contains(toolbar)).toBe(false);

    const first = within(grid).getAllByRole('button')[0];
    first?.focus();
    await user.keyboard('{ArrowRight}');
    expect(within(grid).getAllByRole('button')[1]).toHaveFocus();
  });

  it('does not section a board under 8 places', () => {
    renderAssigner({
      sourceCount: 7,
      placeCount: 7,
      assignments: Array.from({ length: 7 }, () => ''),
      sectionLabel: 'Huitièmes',
    });

    expect(screen.getByRole('grid')).toHaveAttribute('data-sectioned', 'false');
    expect(screen.queryByText('Huitièmes')).not.toBeInTheDocument();
    expect(
      document.querySelector('.structure-place-map-assigner__board'),
    ).toHaveAttribute('data-scroll', 'false');
  });
});
