import type { ComponentProps } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import { classifyMaterializePresentation } from './structureMaterializeCopy';
import { StructurePhaseOperationalStrip } from './StructurePhaseOperationalStrip';
import type { StructurePlacementOperationalSectionProps } from './StructurePlacementOperationalSection';

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const basePlacement: StructurePlacementOperationalSectionProps = {
  competitionId,
  placesN: 8,
  poolFilled: 8,
  occupiedPlacesCount: 4,
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
};

const baseStage: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'Cup',
  status: 'Draft',
  teamCount: 8,
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
  materialize: {
    commandCode: 'MaterializeFromOccupiedSlots',
    enabled: true,
    materializedCount: 1,
    targetCount: 2,
    labelVariant: 'partial',
  },
};

function renderStrip(
  overrides: {
    placement?: StructurePlacementOperationalSectionProps | null;
    showPlaces?: boolean;
    showDraw?: boolean;
    drawApplied?: boolean;
    stage?: StructureStageHubSummary;
  } = {},
) {
  const stage = overrides.stage ?? baseStage;
  const materialize = stage.materialize;
  const presentation =
    materialize == null
      ? null
      : classifyMaterializePresentation(materialize, 'Draft', stage.status);
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  const props: ComponentProps<typeof StructurePhaseOperationalStrip> = {
    placement: overrides.placement ?? basePlacement,
    showPlaces: overrides.showPlaces ?? true,
    showDraw: overrides.showDraw ?? true,
    drawApplied: overrides.drawApplied ?? false,
    competitionId,
    competitionStatus: 'Draft',
    stage,
    presentation,
  };

  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructurePhaseOperationalStrip {...props} />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('StructurePhaseOperationalStrip', () => {
  it('renders three numbered sections when places, draw, and matches are visible', () => {
    renderStrip();

    expect(document.getElementById('operational-strip-1-title')).toHaveTextContent('1');
    expect(document.getElementById('operational-strip-2-title')).toHaveTextContent('2');
    expect(document.getElementById('operational-strip-3-title')).toHaveTextContent('3');

    expect(screen.getByRole('heading', { name: 'Places' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Tirage' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Matchs' })).toBeInTheDocument();
  });

  it('renumbers sections when places is hidden', () => {
    renderStrip({ showPlaces: false });

    expect(document.querySelector('#operational-strip-3-title')).not.toBeInTheDocument();
    expect(document.getElementById('operational-strip-1-title')).toHaveTextContent('1');
    expect(document.getElementById('operational-strip-2-title')).toHaveTextContent('2');
    expect(screen.getByRole('heading', { name: 'Tirage' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Matchs' })).toBeInTheDocument();
  });

  it('shows manual placement and draw launch actions in their sections', () => {
    renderStrip();

    const placesSection = document
      .getElementById('operational-strip-1-title')!
      .closest('section')!;
    expect(
      within(placesSection).getByRole('button', { name: /Placer manuellement/i }),
    ).toBeInTheDocument();

    const drawSection = document
      .getElementById('operational-strip-2-title')!
      .closest('section')!;
    expect(
      within(drawSection).getByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
  });

  it('shows activate draw empty state when draw is not yet enabled', () => {
    renderStrip({
      placement: {
        ...basePlacement,
        showDrawCta: false,
        showActivateDrawCta: true,
        showResolvedPoolGauge: false,
      },
      showDraw: true,
    });

    expect(
      screen.getByRole('button', { name: /Activer le tirage/i }),
    ).toBeInTheDocument();
  });
});
