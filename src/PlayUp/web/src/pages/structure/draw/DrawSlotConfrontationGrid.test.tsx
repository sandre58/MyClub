import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { DrawSlotConfrontationGrid } from './DrawSlotConfrontationGrid';
import type { SlotConfrontationRow } from './drawUi';

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
  });
});
