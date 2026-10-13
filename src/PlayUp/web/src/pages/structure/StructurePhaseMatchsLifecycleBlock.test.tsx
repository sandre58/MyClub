import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import {
  prepareStage,
  resumeStage,
  suspendStage,
  startStage,
  revertStageToReady,
} from '../../api';
import type { StructureStageHubSummary } from '../../types';
import { StructurePhaseMatchsLifecycleBlock } from './StructurePhaseMatchsLifecycleBlock';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    prepareStage: vi.fn(),
    startStage: vi.fn(),
    completeStage: vi.fn(),
    revertStageToDraft: vi.fn(),
    suspendStage: vi.fn(),
    resumeStage: vi.fn(),
    revertStageToReady: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function renderBlock(stage: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StructurePhaseMatchsLifecycleBlock
          competitionId={competitionId}
          stage={stage}
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

describe('StructurePhaseMatchsLifecycleBlock', () => {
  it('renders nothing when phase is completed', () => {
    const { container } = renderBlock({
      ...baseStage,
      status: 'Completed',
      lifecycle: { transition: { code: null, enabled: false } },
    });

    expect(container).toBeEmptyDOMElement();
  });

  it('shows running complete action without open-matches banner', () => {
    renderBlock({
      ...baseStage,
      status: 'Running',
      lifecycle: {
        transition: {
          code: 'CompleteStage',
          enabled: false,
          disabledReasonCode: 'OpenMatches',
        },
        matchProgress: {
          total: 3,
          finished: 1,
          open: 2,
          cancelled: 0,
          canComplete: false,
        },
      },
    });

    expect(
      screen.queryByText(/des matchs ne sont pas terminés/i),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /terminer la phase/i }),
    ).toBeDisabled();
  });

  it('calls startStage when start is enabled', async () => {
    const user = userEvent.setup();
    vi.mocked(startStage).mockResolvedValue(undefined);

    renderBlock({
      ...baseStage,
      status: 'Ready',
      lifecycle: {
        transition: { code: 'StartStage', enabled: true },
      },
    });

    await user.click(screen.getByRole('button', { name: /démarrer la phase/i }));
    expect(startStage).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('shows disabled suspend control when revert to ready is blocked', () => {
    renderBlock({
      ...baseStage,
      status: 'Running',
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: true },
        revertTransition: {
          code: 'SuspendStage',
          enabled: false,
          disabledReasonCode: 'MatchLiveInProgress',
        },
      },
    });

    expect(
      screen.getByRole('button', { name: /suspendre la phase/i }),
    ).toBeDisabled();
  });

  it('shows cancel start after suspend when running with scheduled matches only', async () => {
    const user = userEvent.setup();
    vi.mocked(revertStageToReady).mockResolvedValue(undefined);

    renderBlock({
      ...baseStage,
      status: 'Running',
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: false },
        cancelStartTransition: { code: 'RevertToReadyStage', enabled: true },
        revertTransition: { code: 'SuspendStage', enabled: true },
      },
    });

    const cancel = screen.getByRole('button', { name: /annuler le démarrage/i });
    const suspend = screen.getByRole('button', { name: /suspendre la phase/i });
    expect(suspend.compareDocumentPosition(cancel) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    await user.click(cancel);
    expect(revertStageToReady).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('shows disabled cancel start when sportive engagement blocks revert', () => {
    renderBlock({
      ...baseStage,
      status: 'Running',
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: true },
        cancelStartTransition: {
          code: 'RevertToReadyStage',
          enabled: false,
          disabledReasonCode: 'MatchLifecycleStarted',
        },
      },
    });

    expect(
      screen.getByRole('button', { name: /annuler le démarrage/i }),
    ).toBeDisabled();
  });

  it('calls resumeStage when resume is enabled', async () => {
    const user = userEvent.setup();
    vi.mocked(resumeStage).mockResolvedValue(undefined);

    renderBlock({
      ...baseStage,
      status: 'Suspended',
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: false },
        revertTransition: { code: 'ResumeStage', enabled: true },
      },
    });

    await user.click(screen.getByRole('button', { name: /reprendre la phase/i }));
    expect(resumeStage).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('calls suspendStage when suspend is enabled', async () => {
    const user = userEvent.setup();
    vi.mocked(suspendStage).mockResolvedValue(undefined);

    renderBlock({
      ...baseStage,
      status: 'Running',
      lifecycle: {
        transition: { code: 'CompleteStage', enabled: false },
        revertTransition: { code: 'SuspendStage', enabled: true },
      },
    });

    await user.click(screen.getByRole('button', { name: /suspendre la phase/i }));
    expect(suspendStage).toHaveBeenCalledWith(baseStage.stageId);
  });

  it('shows disabled revert control without inline ProductionPresent copy', () => {
    renderBlock({
      ...baseStage,
      status: 'Ready',
      lifecycle: {
        transition: { code: 'StartStage', enabled: true },
        revertTransition: {
          code: 'RevertToDraftStage',
          enabled: false,
          disabledReasonCode: 'ProductionPresent',
        },
      },
    });

    expect(
      screen.getByRole('button', { name: /revenir en brouillon/i }),
    ).toBeDisabled();
    expect(screen.queryByText(/matchs rattachés/i)).not.toBeInTheDocument();
  });

  it('renders nothing in Draft (prepare lives in MatchesColumn)', () => {
    const { container } = renderBlock({
      ...baseStage,
      lifecycle: {
        transition: { code: 'PrepareStage', enabled: false },
      },
    });
    expect(container).toBeEmptyDOMElement();
  });
});
