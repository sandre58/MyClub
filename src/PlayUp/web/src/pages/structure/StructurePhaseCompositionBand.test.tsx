import type { ComponentProps } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import { StructurePhaseCompositionBand } from './StructurePhaseCompositionBand';
import type { StructurePlacementOperationalSectionProps } from './structurePlacementOperationalProps';

vi.mock('./StructurePhaseOperationalStrip', () => ({
  StructurePhaseOperationalStrip: () => (
    <div data-testid="composition-operational-strip" />
  ),
}));

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

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
  formatKind: 'Cup',
};

const basePlacement: StructurePlacementOperationalSectionProps = {
  competitionId,
  placesN: 8,
  poolFilled: 0,
  occupiedPlacesCount: 0,
  occupancyReady: true,
  showDrawCta: false,
  showActivateDrawCta: false,
  showResolvedPoolGauge: false,
  canManualPlace: false,
  canEditDraw: false,
  showReleaseCta: false,
  hasNonCancelledDraw: false,
  activeDraw: false,
  blockPerformDrawCta: false,
  createBlockedShort: null,
  createBlockedReason: null,
  showCreateBlockedCaption: false,
  showPoolHint: false,
  releasableAlignedCount: 0,
  releasableTotal: 0,
  releasePending: false,
  deactivateDrawPending: false,
  drawOperationalReady: true,
  onOpenDrawWorkflow: vi.fn(),
  onActivateDraw: vi.fn(),
  onEditDrawParams: vi.fn(),
  onManualPlace: vi.fn(),
  onReleasePlacements: vi.fn(),
  onDeactivateDraw: vi.fn(),
};

function renderBand(
  overrides: Partial<ComponentProps<typeof StructurePhaseCompositionBand>> = {},
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const props: ComponentProps<typeof StructurePhaseCompositionBand> = {
    showCompositionBandEmpty: false,
    compositionGridClass: 'structure-phase-composition__grid',
    compositionEmptyAction: null,
    showQualifiersSection: false,
    structureHubBackgroundRefresh: false,
    qualifiersResolvedEyebrow: null,
    compositionComposeToolbar: null,
    feedGroups: [],
    inboundComposition: undefined,
    entries: [],
    formatKind: 'Cup',
    schematicReady: false,
    entryPlaceProjections: new Map(),
    renderAvalSourceAction: () => null,
    compositionPopulationGaugeFooter: null,
    sharedCompositionPopulationGauge: false,
    showTeamsSection: false,
    showCompositionContentEmpty: false,
    showPopulationColumn: false,
    showPlaceAssignmentSection: false,
    showCompositionDrawSection: false,
    entryProvenanceByEntryId: new Map(),
    compositionTeamRows: null,
    canCompose: false,
    teamsComposeActionLabel: '',
    compactIcon: 'ds-btn',
    openCompose: vi.fn(),
    teamsHasListContent: false,
    teamsRuntimePresentation: { kind: 'list' },
    capacityIssueBanner: null,
    stage: baseStage,
    placedEntryIds: new Set(),
    populationFeedVolume: 0,
    placementOperationalProps: null,
    showPlacementBlock: false,
    showDrawCta: false,
    showActivateDrawCta: false,
    competitionId,
    competitionStatus: 'Draft',
    structureStages: [baseStage],
    materializePresentation: null,
    stageHasAppliedDraw: false,
    compositionTileMode: 'prep',
    placesStripLayout: 'full',
    ...overrides,
  };

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StructurePhaseCompositionBand {...props} />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('StructurePhaseCompositionBand', () => {
  it('renders the empty-band hint when the composition band has no content', () => {
    renderBand({ showCompositionBandEmpty: true });

    expect(
      screen.getByText(
        /Ajouter des équipes à la phase ou alimenter les entrées/i,
      ),
    ).toBeInTheDocument();
  });

  it('renders a single empty state under Équipes in exploitation when the pool is empty', () => {
    renderBand({
      showTeamsSection: true,
      compositionTileMode: 'exploitation',
      teamsRuntimePresentation: { kind: 'empty' },
    });

    expect(
      screen.getByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Aucune équipe dans la phase', {
        selector: '.ds-empty__title',
      }),
    ).toBeInTheDocument();
    expect(
      screen.queryAllByText('Aucune équipe dans la phase'),
    ).toHaveLength(1);
  });

  it('renders a PanelHead-aligned draw title in the standalone draw column', () => {
    renderBand({
      showCompositionDrawSection: true,
      showPopulationColumn: true,
      compositionTileMode: 'exploitation',
      showDrawCta: true,
      placementOperationalProps: basePlacement,
    });

    expect(
      screen.getByRole('heading', { name: 'Tirage' }),
    ).toBeInTheDocument();
  });

  it('renders the place-assignment section and operational strip when affectation is shown', () => {
    renderBand({
      showPlaceAssignmentSection: true,
      showPopulationColumn: true,
      placementOperationalProps: basePlacement,
      showPlacementBlock: true,
    });

    expect(
      screen.getByRole('heading', { name: 'Affectation des places' }),
    ).toBeInTheDocument();
    expect(screen.getByTestId('composition-operational-strip')).toBeInTheDocument();
  });
});
