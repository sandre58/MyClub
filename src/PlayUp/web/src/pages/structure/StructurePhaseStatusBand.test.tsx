import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { prepareStage, startStage } from '../../api';
import type { StructureStageHubSummary } from '../../types';
import { StructurePhaseStatusBand } from './StructurePhaseStatusBand';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    prepareStage: vi.fn(),
    startStage: vi.fn(),
    completeStage: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function renderBand(stage: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StructurePhaseStatusBand
          competitionId={competitionId}
          stage={stage}
          structureHubRefreshing={false}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const baseStage: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'League',
  status: 'Draft',
  teamCount: 4,
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

describe('StructurePhaseStatusBand', () => {
  it('renders nothing when phase is completed', () => {
    const { container } = renderBand({
      ...baseStage,
      status: 'Completed',
      lifecycle: { transition: { code: null, enabled: false } },
    });

    expect(container).toBeEmptyDOMElement();
  });

  it('shows draft stepper labels without stage-not-ready banner', () => {
    renderBand({
      ...baseStage,
      lifecycle: {
        transition: {
          code: 'PrepareStage',
          enabled: false,
          disabledReasonCode: 'StageNotReady',
        },
        prepareChecklist: {
          allSatisfied: false,
          items: [{ code: 'Entries', applicable: true, satisfied: false }],
        },
      },
    });

    expect(screen.getByText(/^réglement$/i)).toBeInTheDocument();
    expect(screen.getByText(/^équipes$/i)).toBeInTheDocument();
    expect(
      screen.queryByText(/n’est pas prête à être marquée comme prête/i),
    ).toBeNull();
    expect(screen.getByRole('button', { name: /préparer/i })).toBeDisabled();
  });

  it('shows running hero stats without open-matches banner', () => {
    renderBand({
      ...baseStage,
      status: 'Running',
      lifecycle: {
        transition: {
          code: 'CompleteStage',
          enabled: false,
          disabledReasonCode: 'OpenMatches',
        },
        matchProgress: {
          total: 20,
          finished: 12,
          open: 7,
          cancelled: 1,
          canComplete: false,
        },
      },
    });

    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText(/^terminés$/i)).toBeInTheDocument();
    expect(screen.getByText(/^restants$/i)).toBeInTheDocument();
    expect(screen.queryByText(/matchs en cours/i)).toBeNull();
    expect(
      screen.queryByText(/chaque match doit être terminé ou annulé/i),
    ).toBeNull();
  });

  it('shows ready hint and start action', async () => {
    vi.mocked(startStage).mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderBand({
      ...baseStage,
      status: 'Ready',
      lifecycle: {
        transition: { code: 'StartStage', enabled: true },
      },
    });

    expect(
      screen.getByText(/la phase est prête à démarrer/i),
    ).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /^démarrer$/i }));
    expect(startStage).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('calls prepare when enabled', async () => {
    vi.mocked(prepareStage).mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderBand({
      ...baseStage,
      lifecycle: {
        transition: { code: 'PrepareStage', enabled: true },
        prepareChecklist: {
          allSatisfied: true,
          items: [{ code: 'Entries', applicable: true, satisfied: true }],
        },
      },
    });

    await user.click(screen.getByRole('button', { name: /préparer/i }));
    expect(prepareStage).toHaveBeenCalledWith(baseStage.stageId);
  });
});
