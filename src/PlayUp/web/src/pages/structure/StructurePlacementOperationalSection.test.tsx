import type { ComponentProps } from 'react';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { StructurePlacementOperationalSection } from './StructurePlacementOperationalSection';

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function renderPlacement(
  overrides: Partial<
    ComponentProps<typeof StructurePlacementOperationalSection>
  > = {},
) {
  const props: ComponentProps<typeof StructurePlacementOperationalSection> =
    {
      competitionId,
      placesN: 8,
      poolFilled: 4,
      occupiedPlacesCount: 2,
      occupancyReady: true,
      showDrawCta: true,
      showActivateDrawCta: false,
      showResolvedPoolGauge: true,
      canManualPlace: true,
      canEditDraw: true,
      showReleaseCta: false,
      hasNonCancelledDraw: false,
      activeDraw: false,
      blockPerformDrawCta: false,
      createBlockedShort: null,
      createBlockedReason: null,
      showCreateBlockedCaption: false,
      showPoolHint: true,
      releasableAlignedCount: 0,
      releasableTotal: 0,
      releasePending: false,
      deactivateDrawPending: false,
      onOpenDrawWorkflow: vi.fn(),
      onActivateDraw: vi.fn(),
      onEditDrawParams: vi.fn(),
      onManualPlace: vi.fn(),
      onReleasePlacements: vi.fn(),
      onDeactivateDraw: vi.fn(),
      ...overrides,
    };

  return render(
    <MemoryRouter>
      <StructurePlacementOperationalSection {...props} />
    </MemoryRouter>,
  );
}

describe('StructurePlacementOperationalSection', () => {
  it('labels Places and Draw groups when both clusters are visible', () => {
    renderPlacement();

    const placesGroup = screen.getByRole('group', { name: 'Places' });
    const drawGroup = screen.getByRole('group', { name: 'Tirage' });

    expect(within(placesGroup).getByText(/places occupées/i)).toBeInTheDocument();
    expect(within(placesGroup).getByRole('button', { name: /Placer/i })).toBeInTheDocument();

    expect(within(drawGroup).getByText(/équipes résolues/i)).toBeInTheDocument();
    expect(
      within(drawGroup).getByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
  });

  it('shows only the Draw group when occupancy cluster is hidden', () => {
    renderPlacement({
      occupancyReady: true,
      occupiedPlacesCount: null,
      canManualPlace: false,
    });

    expect(screen.queryByRole('group', { name: 'Places' })).not.toBeInTheDocument();
    expect(screen.getByRole('group', { name: 'Tirage' })).toBeInTheDocument();
  });
});
