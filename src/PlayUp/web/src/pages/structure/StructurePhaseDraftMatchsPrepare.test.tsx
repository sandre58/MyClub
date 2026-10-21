import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { prepareStage } from '../../api';
import type { StructureStageHubSummary } from '../../types';
import { StructurePhaseDraftMatchsPrepare } from './StructurePhaseMatchsLifecycleBlock';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    prepareStage: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

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

function renderDraft(stage: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StructurePhaseDraftMatchsPrepare
          competitionId={competitionId}
          stage={stage}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('StructurePhaseDraftMatchsPrepare', () => {
  it('shows stage-not-ready message when prepare is blocked', () => {
    renderDraft({
      ...baseStage,
      lifecycle: {
        transition: {
          code: 'PrepareStage',
          enabled: false,
          disabledReasonCode: 'StageNotReady',
        },
      },
    });

    expect(screen.getByText(/complétez la structure/i)).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /marquer comme prête/i }),
    ).toBeDisabled();
  });

  it('calls prepareStage when prepare is enabled', async () => {
    const user = userEvent.setup();
    vi.mocked(prepareStage).mockResolvedValue(undefined);

    renderDraft({
      ...baseStage,
      lifecycle: {
        transition: { code: 'PrepareStage', enabled: true },
      },
    });

    await user.click(
      screen.getByRole('button', { name: /marquer comme prête/i }),
    );
    expect(prepareStage).toHaveBeenCalledWith(baseStage.stageId);
  });
});
