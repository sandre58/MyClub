import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { dematerializePhase, materializeMatches } from '../../api';
import type { StructureStageHubSummary } from '../../types';
import { classifyMaterializePresentation } from './structureMaterializeCopy';
import { StructureProductionOperationalSection } from './StructureProductionOperationalSection';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    materializeMatches: vi.fn(),
    materializeCupFromOccupiedSlots: vi.fn(),
    dematerializePhase: vi.fn(),
  };
});

const baseStage: StructureStageHubSummary = {
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

function renderSection(
  stage: StructureStageHubSummary,
  competitionStatus: 'Draft' | 'Running' = 'Draft',
) {
  const materialize = stage.materialize;
  const presentation =
    materialize == null
      ? null
      : classifyMaterializePresentation(
          materialize,
          competitionStatus,
          stage.status,
        );
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructureProductionOperationalSection
          competitionId="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
          competitionStatus={competitionStatus}
          stage={stage}
          presentation={presentation}
        />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('StructureProductionOperationalSection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('executes global materialize from the tile primary action', async () => {
    vi.mocked(materializeMatches).mockResolvedValue({
      createdCount: 1,
      attachedMatchIds: ['m1'],
      alreadyComplete: false,
    });
    const user = userEvent.setup();
    renderSection({
      ...baseStage,
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'create',
        determinedCount: 6,
        eligibleCount: 6,
        materializedCount: 0,
        commandEnabled: true,
        confrontations: [],
      },
    });

    expect(screen.getByText('match créés')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Créer les matchs/i }));
    expect(materializeMatches).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('shows frozen gap in the gauge without a materialize primary action', () => {
    renderSection(
      {
        ...baseStage,
        status: 'Running',
        materialize: {
          commandCode: 'MaterializeMatches',
          labelVariant: 'exhausted',
          determinedCount: 6,
          eligibleCount: 0,
          materializedCount: 3,
          commandEnabled: false,
          disabledReason: 'StructureNotMutable',
          confrontations: [],
        },
      },
      'Running',
    );

    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('6')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Créer les matchs/i }),
    ).not.toBeInTheDocument();
  });

  it('shows blocked state with read disabledReason when present', () => {
    renderSection({
      ...baseStage,
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'idle',
        determinedCount: 0,
        eligibleCount: 0,
        materializedCount: 0,
        commandEnabled: false,
        disabledReason: 'NoDeterminedConfrontations',
        confrontations: [],
      },
    });

    expect(
      screen.getByText(/Aucune confrontation n’est prête à être matérialisée/i),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Créer les matchs/i }),
    ).toBeDisabled();
  });

  it('opens dematerialize confirm and calls dematerializePhase', async () => {
    vi.mocked(dematerializePhase).mockResolvedValue({
      removedMatchCount: 3,
      removedRoundNumber: null,
    });
    const user = userEvent.setup();
    renderSection({
      ...baseStage,
      dematerialize: { enabled: true, scope: 'FullPhase' },
    });

    await user.click(
      screen.getByRole('button', { name: /Retirer les matchs/i }),
    );
    const dialog = screen.getByRole('dialog');
    await user.click(
      within(dialog).getByRole('button', { name: /Retirer les matchs/i }),
    );
    expect(dematerializePhase).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('disables dematerialize toolbar control when read blocks it', () => {
    renderSection({
      ...baseStage,
      dematerialize: {
        enabled: false,
        scope: 'FullPhase',
        disabledReason: 'MatchLifecycleStarted',
      },
    });

    expect(
      screen.getByRole('button', { name: /Retirer les matchs/i }),
    ).toBeDisabled();
  });
});
