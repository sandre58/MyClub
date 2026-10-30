import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { DrawSlotConfrontationGrid } from './DrawSlotConfrontationGrid';
import {
  buildCupDrawDisplayFromDto,
  type SlotConfrontationRow,
} from './drawUi';

const rows: SlotConfrontationRow[] = [
  {
    key: 'P1',
    sideA: { slotKey: 'S1', entryId: 'e1', displayName: 'Belgium' },
    sideB: { slotKey: 'S2', entryId: 'e2', displayName: 'Poland' },
  },
  {
    key: 'P2',
    sideA: { slotKey: 'S3', entryId: 'e3', displayName: 'Turkey' },
    sideB: { slotKey: 'S4', entryId: 'e4', displayName: 'Denmark' },
  },
];

describe('DrawSlotConfrontationGrid', () => {
  it('renders MatchRows with a muted pair key and an S1 vs S2 aside', () => {
    const { container } = render(
      <DrawSlotConfrontationGrid
        rows={rows}
        ariaLabel="Résultat"
        unpaired={[
          {
            slotKey: 'S9',
            entryId: 'e9',
            displayName: 'Spain',
          },
        ]}
        unpairedLabel="Placements"
      />,
    );

    expect(
      screen.getByRole('listitem', { name: 'P1 · S1 vs S2' }),
    ).toBeInTheDocument();
    expect(screen.getByText('P1')).toBeInTheDocument();
    expect(screen.getByText('S1 vs S2')).toBeInTheDocument();
    expect(screen.getByText('Belgium')).toBeInTheDocument();
    expect(screen.getByText('Poland')).toBeInTheDocument();
    expect(container.querySelector('.ds-interactive-row__chevron')).toBeNull();
    expect(container.querySelector('.draw-confrontation')).toBeNull();
    expect(container.querySelectorAll('.draw-slot-rows > li')).toHaveLength(3);
    expect(
      screen.getByRole('listitem', { name: 'Spain · S9' }),
    ).toBeInTheDocument();
    expect(screen.getAllByText('S9')).toHaveLength(1);
    expect(screen.queryByText('—')).not.toBeInTheDocument();
    expect(container.querySelectorAll('[data-density="compact"]')).toHaveLength(
      3,
    );
  });

  it('T2 exposes Déjà placée and Place libre, including under RTL', () => {
    const projection = buildCupDrawDisplayFromDto(
      [
        {
          slotKey: 'S1',
          cellState: 'AlreadyPlaced',
          entryId: 'e1',
          displayName: 'Belgium',
          origin: 'Manual',
        },
        {
          slotKey: 'S2',
          cellState: 'Free',
          entryId: null,
          displayName: null,
          origin: null,
        },
      ],
      [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
      {
        unknownEntry: 'Équipe',
        alreadyPlaced: 'Déjà placée',
        freePlace: 'Place libre',
        reserved: 'Réservée',
        withdrawn: 'Retirée',
        outsideDraw: 'Hors de ce tirage',
        originManualHint:
          'Origine d’affichage : manuelle ou issue d’un tirage précédent.',
      },
    );
    render(
      <div dir="rtl">
        <DrawSlotConfrontationGrid rows={projection.confrontations} />
      </div>,
    );
    const item = screen.getByRole('listitem');
    expect(item.closest('[dir="rtl"]')).toHaveAttribute('dir', 'rtl');
    expect(item).toHaveAccessibleName(/Déjà placée/);
    expect(item).toHaveAccessibleName(/Place libre/);
    expect(screen.queryByText('Déjà placée')).not.toBeInTheDocument();
    expect(screen.getByText('Belgium')).toHaveClass(
      'structure-draw-slot__name--placed',
    );
    expect(screen.getByText('Place libre')).toBeInTheDocument();
  });
});
