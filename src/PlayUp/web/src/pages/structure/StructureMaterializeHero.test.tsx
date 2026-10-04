import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { materializeMatches } from '../../api';
import type { StructureStageHubSummary } from '../../types';
import { StructureMaterializeHero } from './StructureMaterializeHero';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    materializeMatches: vi.fn(),
    materializeCupFromOccupiedSlots: vi.fn(),
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

function renderHero(
  stage: StructureStageHubSummary,
  competitionStatus: 'Draft' | 'Running' = 'Draft',
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructureMaterializeHero
          competitionId="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
          competitionStatus={competitionStatus}
          stage={stage}
        />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('StructureMaterializeHero', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('executes global materialize from the hero CTA with ratio only', async () => {
    vi.mocked(materializeMatches).mockResolvedValue({
      createdCount: 1,
      attachedMatchIds: ['m1'],
      alreadyComplete: false,
    });
    const user = userEvent.setup();
    renderHero({
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

    expect(screen.getByText('matchs créés')).toBeInTheDocument();
    expect(screen.queryByText(/à créer/i)).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Créer les matchs/i }));
    expect(materializeMatches).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('renders nothing for terminal (hero stat lives in phase meta)', () => {
    const { container } = renderHero({
      ...baseStage,
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'exhausted',
        determinedCount: 6,
        eligibleCount: 0,
        materializedCount: 6,
        commandEnabled: false,
        confrontations: [],
      },
    });

    expect(container).toBeEmptyDOMElement();
  });

  it('renders nothing for frozen incomplete', () => {
    const { container } = renderHero(
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

    expect(container).toBeEmptyDOMElement();
  });

  it('does not render a matches link in the production band', () => {
    renderHero({
      ...baseStage,
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'complete',
        determinedCount: 6,
        eligibleCount: 2,
        materializedCount: 4,
        commandEnabled: true,
        confrontations: [],
      },
    });

    expect(
      screen.queryByRole('link', { name: /Voir les matchs de la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('shows blocked state with read disabledReason when present', () => {
    renderHero({
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
    expect(screen.queryByText(/Place pas encore alimentée/i)).not.toBeInTheDocument();
  });
});
