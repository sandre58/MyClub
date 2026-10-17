import type { ComponentProps } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { dematerializePhase, materializeMatches } from '../../api';
import type { StructureMaterialize, StructureStageHubSummary } from '../../types';
import { classifyMaterializePresentation } from './structureMaterializeCopy';
import { StructurePhaseOperationalStrip } from './StructurePhaseOperationalStrip';
import type { StructurePlacementOperationalSectionProps } from './structurePlacementOperationalProps';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    materializeMatches: vi.fn(),
    materializeCupFromOccupiedSlots: vi.fn(),
    dematerializePhase: vi.fn(),
  };
});

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
  drawOperationalReady: true,
  onOpenDrawWorkflow: vi.fn(),
  onActivateDraw: vi.fn(),
  onEditDrawParams: vi.fn(),
  onManualPlace: vi.fn(),
  onReleasePlacements: vi.fn(),
  onDeactivateDraw: vi.fn(),
};

const cupMaterializePartial: StructureMaterialize = {
  commandCode: 'MaterializeFromOccupiedSlots',
  labelVariant: 'create',
  determinedCount: 2,
  eligibleCount: 1,
  materializedCount: 1,
  commandEnabled: true,
  confrontations: [],
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
  materialize: cupMaterializePartial,
};

const leagueStageBase: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'League',
  status: 'Draft',
  teamCount: 2,
  matchCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: false,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
};

function renderStrip(
  overrides: {
    placement?: StructurePlacementOperationalSectionProps | null;
    showPlaces?: boolean;
    showDraw?: boolean;
    drawApplied?: boolean;
    stage?: StructureStageHubSummary;
    competitionStatus?: 'Draft' | 'Running';
    structureHubBackgroundRefresh?: boolean;
    layoutVariant?: ComponentProps<
      typeof StructurePhaseOperationalStrip
    >['layoutVariant'];
    placesStripLayout?: ComponentProps<
      typeof StructurePhaseOperationalStrip
    >['placesStripLayout'];
  } = {},
) {
  const stage = overrides.stage ?? baseStage;
  const competitionStatus = overrides.competitionStatus ?? 'Draft';
  const materialize = stage.materialize;
  const presentation =
    materialize == null
      ? null
      : classifyMaterializePresentation(materialize, competitionStatus, stage.status);
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  const props: ComponentProps<typeof StructurePhaseOperationalStrip> = {
    placement: overrides.placement ?? basePlacement,
    showPlaces: overrides.showPlaces ?? true,
    showDraw: overrides.showDraw ?? true,
    drawApplied: overrides.drawApplied ?? false,
    structureHubBackgroundRefresh: overrides.structureHubBackgroundRefresh ?? false,
    layoutVariant: overrides.layoutVariant,
    placesStripLayout: overrides.placesStripLayout,
    competitionId,
    competitionStatus,
    stage,
    structureStages: overrides.structureStages ?? [stage],
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

function renderMatchesStrip(
  stage: StructureStageHubSummary,
  competitionStatus: 'Draft' | 'Running' = 'Draft',
) {
  return renderStrip({
    layoutVariant: 'aside-matchs',
    showPlaces: false,
    showDraw: false,
    placement: null,
    stage,
    competitionStatus,
  });
}

describe('StructurePhaseOperationalStrip', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it('composition layout variant omits the matchs column', () => {
    const { container } = renderStrip({ layoutVariant: 'composition' });
    const strip = container.querySelector('.structure-operational-strip');
    expect(strip).toHaveAttribute('data-layout-variant', 'composition');
    expect(strip).toHaveAttribute('data-section-count', '2');
    expect(
      screen.queryByRole('heading', { name: /Matchs/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /Places/i }),
    ).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Tirage/i })).toBeInTheDocument();
  });

  it('aside-matchs layout variant omits placement and draw columns', () => {
    const { container } = renderStrip({ layoutVariant: 'aside-matchs' });
    const strip = container.querySelector('.structure-operational-strip');
    expect(strip).toHaveAttribute('data-layout-variant', 'aside-matchs');
    expect(strip).toHaveAttribute('data-section-count', '1');
    expect(
      screen.queryByRole('heading', { name: /Tirage/i }),
    ).not.toBeInTheDocument();
  });

  it('aside-matchs places materialize actions below the production gauge on one row', () => {
    const materialize: StructureMaterialize = {
      commandCode: 'MaterializeMatches',
      labelVariant: 'create',
      determinedCount: 2,
      eligibleCount: 2,
      materializedCount: 0,
      commandEnabled: true,
      confrontations: [],
    };
    const { container } = renderStrip({
      layoutVariant: 'aside-matchs',
      showPlaces: false,
      showDraw: false,
      placement: null,
      competitionStatus: 'Running',
      stage: {
        ...leagueStageBase,
        status: 'Ready',
        materialize,
        lifecycle: {
          transition: { code: 'StartStage', enabled: true },
        },
      },
    });

    const actions = container.querySelector(
      '.structure-operational-strip__production-actions--tile',
    );
    expect(actions).not.toBeNull();
    const matchesSection = document.getElementById('operational-matchs')!;
    const gauge = within(matchesSection).getByLabelText(
      /Avancement de la production des matchs/i,
    );
    expect(
      gauge.compareDocumentPosition(actions!) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    const primaryBtn = within(actions as HTMLElement).getByRole('button', {
      name: /Créer les matchs/i,
    });
    expect(primaryBtn.className).toMatch(/ds-btn--primary/);
  });

  it('renders composition place-assignment sections when places and draw are visible', () => {
    renderStrip({ layoutVariant: 'composition' });

    expect(
      screen.getByRole('button', { name: /Placer manuellement/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /Matchs/i }),
    ).not.toBeInTheDocument();
  });

  it('omits places actions from composition when places column is hidden', () => {
    renderStrip({ layoutVariant: 'composition', showPlaces: false });

    expect(
      screen.queryByRole('button', { name: /Placer manuellement/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Lancer le tirage/i }),
    ).toBeInTheDocument();
  });

  it('composition exploitation draw shows view workflow only', () => {
    renderStrip({
      layoutVariant: 'composition',
      showPlaces: false,
      stage: { ...baseStage, status: 'Running' },
      placement: {
        ...basePlacement,
        stageStatus: 'Running',
        compositionTileMode: 'exploitation',
        canManualPlace: false,
        canEditDraw: false,
        drawReadinessChecklist: [
          { id: 'teamsFillPlaces' as const, met: false, current: 4, capacity: 8 },
        ],
      },
    });

    expect(
      screen.queryByRole('button', { name: /Lancer le tirage/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('list', {
        name: /Prérequis pour effectuer le tirage/i,
      }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Voir le tirage/i }),
    ).toBeInTheDocument();
  });

  it('shows manual placement and draw launch actions in their sections', () => {
    renderStrip({ layoutVariant: 'composition' });

    expect(
      screen.getByRole('button', { name: /Placer manuellement/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Lancer le tirage/i }),
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

  describe('Δ-O4-2 Completed — draw strip read-only final state', () => {
    const completedCupStage: StructureStageHubSummary = {
      ...baseStage,
      status: 'Completed',
    };

    it('hides perform / activate / setup controls but keeps view-draw when applied', () => {
      renderStrip({
        layoutVariant: 'composition',
        stage: completedCupStage,
        drawApplied: true,
        placement: {
          ...basePlacement,
          stageStatus: 'Completed',
          canEditDraw: false,
          showActivateDrawCta: false,
          showResolvedPoolGauge: false,
          showReleaseCta: false,
          showCreateBlockedCaption: false,
          showPoolHint: false,
          canManualPlace: false,
        },
      });

      expect(
        screen.getByRole('button', { name: /Voir le tirage/i }),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Lancer le tirage/i }),
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Activer le tirage/i }),
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Paramètres/i }),
      ).not.toBeInTheDocument();
    });

    it('shows no perform CTA when draw was not applied', () => {
      renderStrip({
        layoutVariant: 'composition',
        stage: completedCupStage,
        drawApplied: false,
        placement: {
          ...basePlacement,
          stageStatus: 'Completed',
          canEditDraw: false,
          showActivateDrawCta: false,
          showResolvedPoolGauge: false,
          showReleaseCta: false,
          showCreateBlockedCaption: false,
          showPoolHint: false,
          canManualPlace: false,
        },
      });

      expect(
        screen.queryByRole('button', { name: /Lancer le tirage/i }),
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Voir le tirage/i }),
      ).not.toBeInTheDocument();
    });
  });

  describe('Δ-03 Ready — P1 hidden, P3 preserved', () => {
    const readyCupStage: StructureStageHubSummary = {
      ...baseStage,
      status: 'Ready',
    };

    it('hides P1 setup controls but keeps perform-draw CTA when rules are engaged', () => {
      renderStrip({
        layoutVariant: 'composition',
        stage: readyCupStage,
        placement: {
          ...basePlacement,
          canEditDraw: false,
          showDrawCta: true,
          showActivateDrawCta: false,
        },
      });

      expect(
        screen.getByRole('button', { name: /Lancer le tirage/i }),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Paramètres/i }),
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Désactiver le tirage/i }),
      ).not.toBeInTheDocument();
    });

    it('shows draw readiness checklist read-only when P1 authoring is off', () => {
      const drawReadinessChecklist = [
        { id: 'teamsFillPlaces' as const, met: false, current: 4, capacity: 8 },
        { id: 'teamsAllKnown' as const, met: true, current: 4, capacity: 4 },
        { id: 'bracketPlacesFree' as const, met: true },
      ];

      renderStrip({
        layoutVariant: 'composition',
        stage: readyCupStage,
        placement: {
          ...basePlacement,
          stageStatus: 'Ready',
          canEditDraw: false,
          showDrawCta: true,
          showActivateDrawCta: false,
          drawReadinessChecklist,
        },
      });

      expect(
        screen.getByRole('list', {
          name: /Prérequis pour effectuer le tirage/i,
        }),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Paramètres/i }),
      ).not.toBeInTheDocument();
    });

    it('does not show draw readiness checklist when stage is Running', () => {
      const runningStage: StructureStageHubSummary = {
        ...baseStage,
        status: 'Running',
      };
      const drawReadinessChecklist = [
        { id: 'teamsFillPlaces' as const, met: false, current: 4, capacity: 8 },
        { id: 'teamsAllKnown' as const, met: true, current: 4, capacity: 4 },
        { id: 'bracketPlacesFree' as const, met: true },
      ];

      renderStrip({
        layoutVariant: 'composition',
        stage: runningStage,
        placement: {
          ...basePlacement,
          stageStatus: 'Running',
          canEditDraw: false,
          showDrawCta: true,
          showActivateDrawCta: false,
          drawReadinessChecklist,
        },
      });

      expect(
        screen.queryByRole('list', {
          name: /Prérequis pour effectuer le tirage/i,
        }),
      ).not.toBeInTheDocument();
    });

    it('hides activate draw when P1 authoring is off and draw is not engaged', () => {
      renderStrip({
        layoutVariant: 'composition',
        stage: readyCupStage,
        placement: {
          ...basePlacement,
          canEditDraw: false,
          showDrawCta: false,
          showActivateDrawCta: false,
          showResolvedPoolGauge: false,
        },
      });

      expect(
        screen.queryByRole('button', { name: /Activer le tirage/i }),
      ).not.toBeInTheDocument();
    });

    it('keeps view-draw workflow when draw is applied without P1 toolbar', () => {
      renderStrip({
        layoutVariant: 'composition',
        stage: readyCupStage,
        drawApplied: true,
        placement: {
          ...basePlacement,
          canEditDraw: false,
          showDrawCta: true,
          showActivateDrawCta: false,
        },
      });

      expect(
        screen.getByRole('button', { name: /Voir le tirage/i }),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Paramètres/i }),
      ).not.toBeInTheDocument();
    });
  });

  describe('matches cluster (materialize / dematerialize)', () => {
    it('executes global materialize from the strip primary action', async () => {
      vi.mocked(materializeMatches).mockResolvedValue({
        createdCount: 1,
        attachedMatchIds: ['m1'],
        alreadyComplete: false,
      });
      const user = userEvent.setup();
      const materialize: StructureMaterialize = {
        commandCode: 'MaterializeMatches',
        labelVariant: 'create',
        determinedCount: 6,
        eligibleCount: 6,
        materializedCount: 0,
        commandEnabled: true,
        confrontations: [],
      };
      renderMatchesStrip({
        ...leagueStageBase,
        status: 'Ready',
        lifecycle: { transition: { code: 'StartStage', enabled: true } },
        materialize,
      });

      expect(screen.getByText('créé')).toBeInTheDocument();
      expect(
        screen.queryByRole('link', { name: /Voir les matchs/i }),
      ).not.toBeInTheDocument();
      await user.click(screen.getByRole('button', { name: /Créer les matchs/i }));
      expect(materializeMatches).toHaveBeenCalledWith(leagueStageBase.stageId);
    });

    it('shows roster-incomplete empty state when phase composition is partial', () => {
      const materialize: StructureMaterialize = {
        commandCode: 'MaterializeMatches',
        labelVariant: 'idle',
        determinedCount: 0,
        eligibleCount: 0,
        materializedCount: 0,
        commandEnabled: false,
        disabledReason: 'PhaseRosterIncomplete',
        confrontations: [],
      };
      renderMatchesStrip({
        ...leagueStageBase,
        status: 'Ready',
        materialize,
        lifecycle: {
          transition: {
            code: 'StartStage',
            enabled: false,
            disabledReasonCode: 'UnmaterializedConfrontations',
          },
        },
      });

      const matchesSection = document.getElementById('operational-matchs')!;
      expect(
        within(matchesSection).getByText(
          /La composition de la phase est incomplète/i,
        ),
      ).toBeInTheDocument();
    });

    it('shows blocked production empty state when confrontations are not determined yet', () => {
      const materialize: StructureMaterialize = {
        commandCode: 'MaterializeMatches',
        labelVariant: 'idle',
        determinedCount: 0,
        eligibleCount: 0,
        materializedCount: 0,
        commandEnabled: false,
        disabledReason: 'NoDeterminedConfrontations',
        confrontations: [],
      };
      renderMatchesStrip({
        ...leagueStageBase,
        status: 'Ready',
        materialize,
        lifecycle: {
          transition: {
            code: 'StartStage',
            enabled: false,
            disabledReasonCode: 'UnmaterializedConfrontations',
          },
        },
      });

      const matchesSection = document.getElementById('operational-matchs')!;
      expect(
        within(matchesSection).getByText(
          /Aucune confrontation n’est encore déterminée/i,
        ),
      ).toBeInTheDocument();
      expect(
        matchesSection.querySelector('.structure-operational-strip__empty'),
      ).not.toBeNull();
    });

    it('shows frozen gap in the gauge without a materialize primary action', () => {
      const materialize: StructureMaterialize = {
        commandCode: 'MaterializeMatches',
        labelVariant: 'exhausted',
        determinedCount: 6,
        eligibleCount: 0,
        materializedCount: 3,
        commandEnabled: false,
        disabledReason: 'StructureNotMutable',
        confrontations: [],
      };
      renderMatchesStrip(
        {
          ...leagueStageBase,
          status: 'Running',
          matchCount: 3,
          materialize,
          lifecycle: {
            transition: { code: 'CompleteStage', enabled: false },
            matchProgress: {
              total: 3,
              finished: 3,
              open: 0,
              cancelled: 0,
              canComplete: true,
            },
          },
        },
        'Running',
      );

      expect(
        screen.queryByLabelText(/Avancement de la production des matchs/i),
      ).not.toBeInTheDocument();
      const matchesSection = document.getElementById('operational-matchs')!;
      expect(
        within(matchesSection).getByLabelText(/Avancement des matchs/i),
      ).toBeInTheDocument();
      expect(
        within(matchesSection).getByText(/Tous les matchs sont terminés/i),
      ).toBeInTheDocument();
      expect(
        within(matchesSection).queryByText(/matchs terminés sur/i),
      ).not.toBeInTheDocument();
    });

    it('Suspended frozen partial: no materialize primary (CTRL-O3-MAT)', () => {
      const materialize: StructureMaterialize = {
        commandCode: 'MaterializeMatches',
        labelVariant: 'exhausted',
        determinedCount: 6,
        eligibleCount: 0,
        materializedCount: 3,
        commandEnabled: false,
        disabledReason: 'StructureNotMutable',
        confrontations: [],
      };
      renderMatchesStrip(
        {
          ...leagueStageBase,
          status: 'Suspended',
          matchCount: 3,
          materialize,
          lifecycle: {
            revertTransition: { code: 'ResumeStage', enabled: true },
            matchProgress: {
              total: 3,
              finished: 1,
              open: 2,
              cancelled: 0,
              canComplete: false,
            },
          },
        },
        'Running',
      );

      const matchesSection = document.getElementById('operational-matchs')!;
      expect(
        within(matchesSection).queryByRole('button', { name: /matérialiser/i }),
      ).not.toBeInTheDocument();
    });

    it('shows closed ratio when matches are finished or cancelled', () => {
      renderMatchesStrip(
        {
          ...leagueStageBase,
          status: 'Running',
          matchCount: 3,
          lifecycle: {
            transition: { code: 'CompleteStage', enabled: true },
            matchProgress: {
              total: 3,
              finished: 1,
              open: 0,
              cancelled: 2,
              canComplete: true,
            },
          },
        },
        'Running',
      );

      const matchesSection = document.getElementById('operational-matchs')!;
      const ratioFilled = matchesSection.querySelector(
        '.structure-operational-strip__ratio-filled',
      );
      expect(ratioFilled?.textContent).toBe('3');
      expect(
        within(matchesSection).getByText(/Tous les matchs sont terminés/i),
      ).toBeInTheDocument();
      expect(within(matchesSection).getByText(/^Terminés$/i)).toBeInTheDocument();
    });

    it('shows in-progress copy instead of match stat summary', () => {
      renderMatchesStrip(
        {
          ...leagueStageBase,
          status: 'Running',
          matchCount: 14,
          lifecycle: {
            transition: { code: 'CompleteStage', enabled: false },
            matchProgress: {
              total: 28,
              finished: 14,
              open: 14,
              cancelled: 0,
              canComplete: false,
            },
          },
        },
        'Running',
      );

      const matchesSection = document.getElementById('operational-matchs')!;
      expect(
        within(matchesSection).getByText(
          /La phase est en cours\. Il reste des matchs à jouer\./i,
        ),
      ).toBeInTheDocument();
      expect(
        within(matchesSection).queryByText(/14 en cours/i),
      ).not.toBeInTheDocument();
    });

    it('opens dematerialize confirm and calls dematerializePhase', async () => {
      vi.mocked(dematerializePhase).mockResolvedValue({
        removedMatchCount: 3,
        removedRoundNumber: null,
      });
      const user = userEvent.setup();
      const materialize: StructureMaterialize = {
        commandCode: 'MaterializeMatches',
        labelVariant: 'exhausted',
        determinedCount: 6,
        eligibleCount: 0,
        materializedCount: 3,
        commandEnabled: false,
        confrontations: [],
      };
      renderMatchesStrip(
        {
          ...leagueStageBase,
          status: 'Running',
          matchCount: 3,
          materialize,
          dematerialize: { enabled: true, scope: 'FullPhase' },
          lifecycle: {
            transition: { code: 'CompleteStage', enabled: false },
            matchProgress: {
              total: 3,
              finished: 0,
              open: 3,
              cancelled: 0,
              canComplete: false,
            },
          },
        },
        'Running',
      );

      await user.click(
        screen.getByRole('button', { name: /Retirer les matchs/i }),
      );
      const dialog = screen.getByRole('dialog');
      await user.click(
        within(dialog).getByRole('button', { name: /Retirer les matchs/i }),
      );
      expect(dematerializePhase).toHaveBeenCalledWith(leagueStageBase.stageId);
    });

    it('keeps matches section content visible while materialize is pending', async () => {
      vi.mocked(materializeMatches).mockImplementation(
        () => new Promise(() => {}),
      );
      const user = userEvent.setup();
      const materialize: StructureMaterialize = {
        commandCode: 'MaterializeMatches',
        labelVariant: 'create',
        determinedCount: 6,
        eligibleCount: 6,
        materializedCount: 0,
        commandEnabled: true,
        confrontations: [],
      };
      const { container } = renderMatchesStrip({
        ...leagueStageBase,
        status: 'Ready',
        lifecycle: { transition: { code: 'StartStage', enabled: true } },
        materialize,
      });

      await user.click(screen.getByRole('button', { name: /Créer les matchs/i }));

      const matchesSection = document.getElementById('operational-matchs')!;
      expect(
        within(matchesSection).getByLabelText(/Avancement de la production des matchs/i),
      ).toBeInTheDocument();
      expect(
        container.querySelector('.structure-operational-strip__section-sync'),
      ).toBeNull();
    });

    it('keeps draw section content visible while deactivate is pending', () => {
      const { container } = renderStrip({
        placement: {
          ...basePlacement,
          deactivateDrawPending: true,
          drawOperationalReady: true,
        },
      });

      expect(
        screen.getByRole('button', { name: /Lancer le tirage/i }),
      ).toBeInTheDocument();
      expect(
        container.querySelector('.structure-operational-strip__section-sync'),
      ).toBeNull();
      const deactivateBtn = screen.getByRole('button', {
        name: /Désactiver le tirage/i,
      });
      expect(deactivateBtn.querySelector('.ds-spinner')).not.toBeNull();
    });

    it('shows regional loader while draw overview is initially loading', () => {
      const { container } = renderStrip({
        placement: {
          ...basePlacement,
          drawOperationalReady: false,
        },
      });

      expect(
        container.querySelector('.structure-operational-strip__section-sync'),
      ).not.toBeNull();
      expect(
        screen.queryByRole('button', { name: /Lancer le tirage/i }),
      ).not.toBeInTheDocument();
    });

    it('keeps placement metrics visible during hub background refresh', () => {
      const { container } = renderStrip({
        layoutVariant: 'composition',
        structureHubBackgroundRefresh: true,
      });

      expect(
        screen.getByRole('button', { name: /placer manuellement/i }),
      ).toBeInTheDocument();
      expect(
        container.querySelector('.structure-operational-strip__section-sync'),
      ).toBeNull();
      expect(
        container.querySelectorAll('.structure-operational-strip__section-chip-spinner'),
      ).toHaveLength(1);
    });

    it('does not insert sync status inside the strip grid (avoids spurious section separators)', () => {
      const { container } = renderStrip({
        layoutVariant: 'aside-matchs',
        structureHubBackgroundRefresh: true,
        showPlaces: false,
        showDraw: false,
        placement: null,
        stage: {
          ...leagueStageBase,
          status: 'Ready',
          materialize: {
            commandCode: 'MaterializeMatches',
            labelVariant: 'create',
            determinedCount: 2,
            eligibleCount: 2,
            materializedCount: 0,
            commandEnabled: true,
            confrontations: [],
          },
          lifecycle: {
            transition: { code: 'StartStage', enabled: true },
          },
        },
        competitionStatus: 'Running',
      });

      const strip = container.querySelector('.structure-operational-strip')!;
      expect(
        strip.querySelector(':scope > .ds-visually-hidden[role="status"]'),
      ).toBeNull();
      expect(
        container.querySelector('.ds-visually-hidden[role="status"]'),
      ).toBeInTheDocument();
      const matchesSection = document.getElementById('operational-matchs')!;
      expect(strip.firstElementChild).toBe(matchesSection);
    });

    it('shows Swiss guidance above gauge without separate banner or generate CTA', () => {
      renderMatchesStrip(
        {
          ...leagueStageBase,
          formatKind: 'Swiss',
          status: 'Running',
          swissRoundCount: 3,
          matchdayCount: 0,
          materialize: undefined,
          lifecycle: {
            transition: { code: 'CompleteStage', enabled: false },
            matchProgress: {
              total: 0,
              finished: 0,
              cancelled: 0,
              open: 0,
              canComplete: false,
            },
          },
        },
        'Running',
      );

      const matchesSection = document.getElementById('operational-matchs')!;
      expect(
        within(matchesSection).getByText(/Aucun tour généré pour le moment/i),
      ).toBeInTheDocument();
      expect(
        screen.queryByRole('button', { name: /Générer la prochaine ronde/i }),
      ).not.toBeInTheDocument();
    });

    describe('aside-matchs lifecycle separation (omitLifecycle)', () => {
      const placementRemediationHref = `/competitions/${competitionId}/structure?stage=${leagueStageBase.stageId}&focus=operational-placement&manual=1`;

      function cupReadyStage(
        overrides: Partial<StructureStageHubSummary>,
      ): StructureStageHubSummary {
        return {
          ...leagueStageBase,
          formatKind: 'Cup',
          status: 'Ready',
          hasDrawRules: true,
          ...overrides,
        };
      }

      it('omits lifecycle actions and block copy in operational-matchs when Start is blocked (InitialRoundNotPlayable)', () => {
        renderMatchesStrip(
          cupReadyStage({
            materialize: {
              commandCode: 'MaterializeFromOccupiedSlots',
              labelVariant: 'idle',
              determinedCount: 0,
              eligibleCount: 0,
              materializedCount: 2,
              commandEnabled: false,
              disabledReason: 'NoDeterminedConfrontations',
              confrontations: [],
            },
            lifecycle: {
              transition: {
                code: 'StartStage',
                enabled: false,
                disabledReasonCode: 'InitialRoundNotPlayable',
                remediation: {
                  href: placementRemediationHref,
                  labelKey: 'InitialRoundNotPlayable',
                },
              },
            },
          }),
          'Draft',
        );

        const matchesSection = document.getElementById('operational-matchs')!;
        expect(
          within(matchesSection).queryByRole('button', {
            name: /Démarrer la phase/i,
          }),
        ).not.toBeInTheDocument();
        expect(
          within(matchesSection).queryByText(
            /Le premier tour n’est pas encore jouable/i,
          ),
        ).not.toBeInTheDocument();
        expect(
          within(matchesSection).getByText(
            /Aucune confrontation n’est encore déterminée/i,
          ),
        ).toBeInTheDocument();
      });

      it('keeps production actions in operational-matchs while omitting Start (partial cup)', () => {
        renderMatchesStrip(
          cupReadyStage({
            materialize: cupMaterializePartial,
            lifecycle: {
              transition: {
                code: 'StartStage',
                enabled: false,
                disabledReasonCode: 'UnmaterializedConfrontations',
              },
            },
          }),
          'Draft',
        );

        const matchesSection = document.getElementById('operational-matchs')!;
        expect(
          within(matchesSection).getByRole('button', {
            name: /Créer les matchs/i,
          }),
        ).toBeInTheDocument();
        expect(
          within(matchesSection).queryByRole('button', {
            name: /Démarrer la phase/i,
          }),
        ).not.toBeInTheDocument();
        expect(
          within(matchesSection).queryByText(
            /Matérialisez toutes les confrontations éligibles/i,
          ),
        ).not.toBeInTheDocument();
      });
    });

    it('hides dematerialize action when read blocks it', () => {
      renderMatchesStrip({
        ...leagueStageBase,
        materialize: {
          commandCode: 'MaterializeMatches',
          labelVariant: 'idle',
          determinedCount: 0,
          eligibleCount: 0,
          materializedCount: 0,
          commandEnabled: false,
          confrontations: [],
        },
        dematerialize: {
          enabled: false,
          scope: 'FullPhase',
          disabledReason: 'MatchLifecycleStarted',
        },
      });

      expect(
        screen.queryByRole('button', { name: /Retirer les matchs/i }),
      ).not.toBeInTheDocument();
    });
  });
});
