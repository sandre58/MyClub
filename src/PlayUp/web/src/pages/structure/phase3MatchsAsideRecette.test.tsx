/**
 * Phase 3 recette — Matchs aside tile (SPA component render only).
 *
 * Product authority: O-MAT-VIS-1 + invariants I-1…I-8.
 * Verdict is on **presented** DOM in the aside strip (and one fiche header case), not
 * `shouldShowAsideMatchsTile` alone.
 *
 * Not a substitute for DevRunner + browser. Suspended (case 7) and Completed
 * « Voir les matchs » toolbar link are not replayed here — see phase-3-recette.md.
 *
 * O-MAT-IMPL-1: tile visibility follows `resolveAsideMatchsStripContent` (cases 3, 5, 6).
 */
import { QueryClient, QueryClientProvider, type UseMutationResult } from '@tanstack/react-query';
import { cleanup, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

import type { StructureMaterialize, StructureStageHubSummary } from '../../types';
import { classifyMaterializePresentation } from './structureMaterializeCopy';
import { StructurePhaseFicheHeader } from './StructurePhaseFicheHeader';
import { StructurePhaseOperationalStrip } from './StructurePhaseOperationalStrip';
import {
  shouldShowAsideMatchsTile,
  shouldShowMatchExploitationProgress,
  shouldShowProductionInMatchsSection,
} from './structureMatchsSectionUi';

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function baseStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    name: 'Championnat',
    status: 'Ready',
    formatKind: 'Championship',
    teamCount: 4,
    matchCount: 0,
    compositionCapacity: 8,
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
    ...overrides,
  };
}

function renderAsideStrip(
  stage: StructureStageHubSummary,
  competitionStatus: 'Draft' | 'Ready' | 'Running' = 'Ready',
) {
  const materialize = stage.materialize;
  const presentation =
    materialize == null
      ? null
      : classifyMaterializePresentation(materialize, competitionStatus, stage.status);
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructurePhaseOperationalStrip
          placement={null}
          showPlaces={false}
          showDraw={false}
          drawApplied={false}
          layoutVariant="aside-matchs"
          competitionId={competitionId}
          competitionStatus={competitionStatus}
          stage={stage}
          structureStages={[stage]}
          presentation={presentation}
        />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

function matchsSection() {
  return document.getElementById('operational-matchs');
}

function stripHasUsefulProductionBody(section: HTMLElement): boolean {
  const text = section.textContent?.trim() ?? '';
  const hasPrimaryMaterialize = Boolean(
    within(section).queryByRole('button', { name: /Créer les matchs|Matérialiser/i }),
  );
  const hasDemat = Boolean(
    within(section).queryByRole('button', { name: /Dématérialiser|Retirer/i }),
  );
  const hasProgress = Boolean(
    section.querySelector('.structure-operational-strip__ratio'),
  );
  const hasProductionEmpty = Boolean(
    section.querySelector('.structure-operational-strip__empty'),
  );
  const hasGauge = Boolean(section.querySelector('.structure-operational-ratio-gauge'));
  const hasRosterResolutionGauge = Boolean(
    section.querySelector('.structure-phase-match-tile__roster-resolution-gauge'),
  );
  return (
    hasPrimaryMaterialize ||
    hasDemat ||
    hasProgress ||
    hasProductionEmpty ||
    hasGauge ||
    hasRosterResolutionGauge ||
    text.length > 40
  );
}

describe('Phase 3 recette — Matchs aside (O-MAT-VIS-1)', () => {
  afterEach(() => {
    cleanup();
  });

  it('1 — Draft: tile hidden; lifecycle on fiche header (not in aside strip)', () => {
    const stage = baseStage({
      status: 'Draft',
      lifecycle: {
        transition: { code: 'PrepareStage', enabled: true },
      },
    });
    expect(shouldShowAsideMatchsTile(stage, null)).toBe(false);

    cleanup();
    render(
      <MemoryRouter>
        <QueryClientProvider
          client={
            new QueryClient({
              defaultOptions: { queries: { retry: false } },
            })
          }
        >
          <StructurePhaseFicheHeader
            stage={stage}
            competitionId={competitionId}
            structureStages={[stage]}
            formatLabel={null}
            editingName={false}
            nameDraft=""
            renameMutation={
              {
                isPending: false,
                isError: false,
                error: null,
                reset: () => {},
              } as unknown as UseMutationResult<void, Error, string, unknown>
            }
            canRename={true}
            canRemove={false}
            compactIcon=""
            compactDangerIcon=""
            onNameDraftChange={() => {}}
            onBeginRename={() => {}}
            onCancelRename={() => {}}
            onCommitRename={() => {}}
            onOpenRemove={() => {}}
          />
        </QueryClientProvider>
      </MemoryRouter>,
    );
    expect(
      screen.getByRole('button', { name: /Marquer comme prête/i }),
    ).toBeInTheDocument();
  });

  it('2 — Championship Ready partial: production empty state, no primary Materialize (seed qual-form-champ-ready-partial)', () => {
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
    const stage = baseStage({
      materialize,
      formatKind: 'Championship',
      compositionCapacity: 8,
      schematicOccupiedPlaceCount: 8,
      inboundComposition: {
        configuredVolume: 8,
        resolvedCount: 2,
        rules: [],
      },
    });
    const presentation = classifyMaterializePresentation(materialize, 'Ready', 'Ready');
    expect(shouldShowProductionInMatchsSection(stage, presentation)).toBe(true);

    renderAsideStrip(stage, 'Ready');
    const section = matchsSection()!;
    expect(
      section.querySelector('.structure-phase-match-tile__roster-resolution-gauge'),
    ).not.toBeNull();
    expect(
      within(section).getByLabelText(/Avancement de la production des matchs/i),
    ).toBeInTheDocument();
    expect(
      within(section).getByLabelText(/2 équipes résolues sur 8/i),
    ).toBeInTheDocument();
    expect(
      within(section).getByRole('button', { name: /Créer les matchs/i }),
    ).toBeDisabled();
    expect(
      screen.queryByRole('button', { name: /Démarrer la phase/i }),
    ).not.toBeInTheDocument();
    expect(stripHasUsefulProductionBody(section)).toBe(true);
  });

  it('3 — Championship Ready placesN only (no materialize): tile hidden (O-MAT-IMPL-1)', () => {
    const stage = baseStage({
      materialize: undefined,
      dematerialize: undefined,
    });
    expect(shouldShowAsideMatchsTile(stage, null)).toBe(false);
    renderAsideStrip(stage, 'Ready');
    const section = matchsSection()!;
    expect(stripHasUsefulProductionBody(section)).toBe(false);
  });

  it('4 — Championship Ready actionable production (seed qual-form-champ-ready-full)', () => {
    const materialize: StructureMaterialize = {
      commandCode: 'MaterializeMatches',
      labelVariant: 'create',
      determinedCount: 6,
      eligibleCount: 6,
      materializedCount: 0,
      commandEnabled: true,
      confrontations: [],
    };
    const stage = baseStage({ materialize });
    renderAsideStrip(stage, 'Ready');
    const section = matchsSection()!;
    expect(
      within(section).getByRole('button', { name: /Créer les matchs/i }),
    ).toBeInTheDocument();
    expect(stripHasUsefulProductionBody(section)).toBe(true);
  });

  it('5 — Swiss Ready (seed swiss-ready): no materialize; product hide — no useful aside body', () => {
    const stage = baseStage({
      formatKind: 'Swiss',
      swissRoundCount: 3,
      matchdayCount: 0,
      materialize: undefined,
    });
    expect(shouldShowAsideMatchsTile(stage, null)).toBe(false);
    renderAsideStrip(stage, 'Ready');
    const section = matchsSection()!;
    expect(
      within(section).queryByText(/tour|round/i),
    ).not.toBeInTheDocument();
    expect(stripHasUsefulProductionBody(section)).toBe(false);
  });

  it('6 — Ready lifecycle/composition signal only: no isolated useful Matchs body', () => {
    const stage = baseStage({
      compositionCapacity: undefined,
      materialize: undefined,
      lifecycle: {
        transition: {
          code: 'StartStage',
          enabled: false,
          disabledReasonCode: 'UnmaterializedConfrontations',
        },
      },
    });
    expect(shouldShowAsideMatchsTile(stage, null)).toBe(false);
    renderAsideStrip(stage, 'Ready');
    const section = matchsSection()!;
    expect(stripHasUsefulProductionBody(section)).toBe(false);
  });

  it('7 — Running: exploitation progress; no lifecycle CTA in aside (seed qual-form-champ-running)', () => {
    const stage = baseStage({
      status: 'Running',
      matchCount: 6,
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'exhausted',
        determinedCount: 6,
        eligibleCount: 0,
        materializedCount: 6,
        commandEnabled: false,
        confrontations: [],
      },
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: false },
        matchProgress: {
          total: 6,
          finished: 0,
          open: 6,
          cancelled: 0,
          canComplete: false,
        },
      },
    });
    expect(shouldShowMatchExploitationProgress(stage)).toBe(true);
    renderAsideStrip(stage, 'Running');
    const section = matchsSection()!;
    expect(stripHasUsefulProductionBody(section)).toBe(true);
    expect(
      within(section).queryByRole('button', { name: /Terminer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('8 — Completed: progress when rendered; no production CTAs (seed championship:finished pattern)', () => {
    const stage = baseStage({
      status: 'Completed',
      matchCount: 6,
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: true },
        matchProgress: {
          total: 6,
          finished: 6,
          open: 0,
          cancelled: 0,
          canComplete: true,
        },
      },
    });
    expect(shouldShowMatchExploitationProgress(stage)).toBe(true);
    const presentation = null;
    expect(shouldShowProductionInMatchsSection(stage, presentation)).toBe(false);
    renderAsideStrip(stage, 'Running');
    const section = matchsSection()!;
    expect(stripHasUsefulProductionBody(section)).toBe(true);
    expect(
      within(section).queryByRole('button', { name: /Créer les matchs/i }),
    ).not.toBeInTheDocument();
  });
});
