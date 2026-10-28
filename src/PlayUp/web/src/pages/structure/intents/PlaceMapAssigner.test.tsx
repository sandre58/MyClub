import { useState } from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Dialog } from '../../../design-system/components/Dialog';
import {
  PlaceMapAssigner,
  PlaceMapAssignerSkeleton,
  type PlaceMapAssignerPlace,
  type PlaceMapAssignerSource,
} from './PlaceMapAssigner';

const sources: PlaceMapAssignerSource[] = [
  { key: 'a', label: 'Alpha' },
  { key: 'b', label: 'Bravo' },
];

const places: PlaceMapAssignerPlace[] = [
  {
    id: 'SF-A',
    label: 'Demi-finale 1 · côté A',
    sectionLabel: 'Demi-finale',
  },
  {
    id: 'SF-B',
    label: 'Demi-finale 1 · côté B',
    sectionLabel: 'Demi-finale',
  },
  { id: 'F-A', label: 'Finale · côté A', sectionLabel: 'Finale' },
];

function Harness({
  uniquePlaces = true,
  initial = ['', ''],
  placeList = places,
}: {
  uniquePlaces?: boolean;
  initial?: string[];
  placeList?: PlaceMapAssignerPlace[];
}) {
  const [assignments, setAssignments] = useState(initial);
  return (
    <>
      <PlaceMapAssigner
        sources={sources}
        places={placeList}
        assignments={assignments}
        onAssignmentsChange={setAssignments}
        uniquePlaces={uniquePlaces}
        heading="Emplacements"
        listAriaLabel="Associer chaque sélection à une place"
        fillLabel="Remplir"
        fillHint="Remplit les vides sans écraser"
      />
      <output data-testid="keys">{assignments.join('|')}</output>
    </>
  );
}

describe('PlaceMapAssigner', () => {
  it('lists sources and does not render a destination grid', () => {
    render(<Harness />);

    expect(screen.queryByRole('grid')).not.toBeInTheDocument();
    const list = screen.getByRole('list', {
      name: 'Associer chaque sélection à une place',
    });
    expect(within(list).getByText('Alpha')).toBeInTheDocument();
    expect(within(list).getByText('Bravo')).toBeInTheDocument();
    expect(screen.getByText('0/2')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Place pour Alpha, vide' }),
    ).toBeInTheDocument();
  });

  it('writes the chosen place on that source index', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(
      screen.getByRole('button', { name: 'Place pour Bravo, vide' }),
    );
    const menu = await screen.findByRole('listbox', {
      name: 'Places pour Bravo',
    });
    expect(
      within(menu).getByRole('group', { name: 'Demi-finale' }),
    ).toBeInTheDocument();
    await user.click(
      within(menu).getByRole('option', { name: 'Demi-finale 1 · côté A' }),
    );

    expect(screen.getByTestId('keys').textContent).toBe('|SF-A');
    expect(screen.getByText('1/2')).toBeInTheDocument();
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('keeps Cup places unique and lets Groups reuse a pool', async () => {
    const user = userEvent.setup();
    const { rerender } = render(<Harness key="cup" initial={['SF-A', '']} />);

    await user.click(
      screen.getByRole('button', {
        name: 'Place pour Bravo, vide',
      }),
    );
    const blocked = await screen.findByRole('option', {
      name: 'Demi-finale 1 · côté A',
    });
    expect(blocked).toBeDisabled();
    await user.click(
      screen.getByRole('option', { name: 'Demi-finale 1 · côté B' }),
    );
    expect(screen.getByTestId('keys').textContent).toBe('SF-A|SF-B');

    rerender(
      <Harness
        key="groups"
        uniquePlaces={false}
        initial={['grp-a', '']}
        placeList={[{ id: 'grp-a', label: 'Groupe A', sectionLabel: null }]}
      />,
    );
    await user.click(
      screen.getByRole('button', { name: 'Place pour Bravo, vide' }),
    );
    await user.click(await screen.findByRole('option', { name: 'Groupe A' }));
    expect(screen.getByTestId('keys').textContent).toBe('grp-a|grp-a');
  });

  it('fills empty indexes without overwriting a manual choice', async () => {
    const user = userEvent.setup();
    render(<Harness initial={['F-A', '']} />);

    await user.click(screen.getByRole('button', { name: 'Remplir' }));

    expect(screen.getByTestId('keys').textContent).toBe('F-A|SF-A');
  });

  it('moves in the place menu with the keyboard and clears with Backspace', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    const alpha = screen.getByRole('button', {
      name: 'Place pour Alpha, vide',
    });
    alpha.focus();
    await user.keyboard('{ArrowDown}');
    const menu = await screen.findByRole('listbox', {
      name: 'Places pour Alpha',
    });
    const first = within(menu).getByRole('option', {
      name: 'Demi-finale 1 · côté A',
    });
    await waitFor(() => expect(first).toHaveFocus());
    await user.keyboard('{ArrowDown}');
    const second = within(menu).getByRole('option', {
      name: 'Demi-finale 1 · côté B',
    });
    await waitFor(() => expect(second).toHaveFocus());
    await user.keyboard('{Enter}');

    expect(screen.getByTestId('keys').textContent).toBe('SF-B|');
    const filled = screen.getByRole('button', {
      name: 'Place pour Alpha, Demi-finale 1 · côté B',
    });
    filled.focus();
    await user.keyboard('{Backspace}');
    expect(screen.getByTestId('keys').textContent).toBe('|');
  });

  it('closes the place menu on Escape before the dialog', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <Dialog open onClose={onClose} title="Qualifications" closeLabel="Fermer">
        <Harness />
      </Dialog>,
    );

    await user.click(
      screen.getByRole('button', { name: 'Place pour Alpha, vide' }),
    );
    expect(
      await screen.findByRole('listbox', { name: 'Places pour Alpha' }),
    ).toBeInTheDocument();

    await user.keyboard('{Escape}');
    await waitFor(() => {
      expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();

    await user.keyboard('{Escape}');
    await waitFor(() => {
      expect(onClose).toHaveBeenCalledTimes(1);
    });
  });

  it('shows a loading list without a grid', () => {
    render(<PlaceMapAssignerSkeleton cells={2} />);
    expect(screen.queryByRole('grid')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Chargement…')).toHaveAttribute(
      'aria-busy',
      'true',
    );
  });
});
