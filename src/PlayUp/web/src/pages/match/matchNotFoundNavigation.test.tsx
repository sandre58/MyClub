import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  fetchMatchDetail,
  fetchMatchesByStage,
  fetchStageOverview,
  fetchStructureView,
  startMatch,
} from '../../api';
import type { MatchDetail, StageOverview } from '../../types';
import { structureReadiness } from '../../test/structureTestFixtures';
import { StageMatchesPage } from '../stage/StageMatchesPage';
import { MatchPage } from './MatchPage';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchMatchDetail: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchStructureView: vi.fn(),
    fetchMatchesByStage: vi.fn(),
    startMatch: vi.fn(),
  };
});

const matchId = '11111111-1111-1111-1111-111111111111';
const stageId = '22222222-2222-2222-2222-222222222222';
const competitionId = '33333333-3333-3333-3333-333333333333';

const stageOverview: StageOverview = {
  id: stageId,
  competitionId,
  name: 'Journée 1',
  status: 'Running',
  rounds: [],
  slots: [],
  draws: [],
};

function scheduledMatch(): MatchDetail {
  return {
    matchId,
    competitionId,
    stageId,
    status: 'Scheduled',
    home: { entryId: 'home', displayName: 'Alpha' },
    away: { entryId: 'away', displayName: 'Beta' },
    result: null,
    fixtureId: '44444444-4444-4444-4444-444444444444',
    legIndex: 1,
    declaredParticipations: [],
    recordedGoals: [],
  };
}

function renderRoutes(initialEntry: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/matches/:matchId" element={<MatchPage />} />
          <Route
            path="/stages/:stageId/matches"
            element={<StageMatchesPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('matchNotFoundNavigation (A7)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview);
    vi.mocked(fetchMatchesByStage).mockResolvedValue([]);
    vi.mocked(fetchStructureView).mockResolvedValue({
      competitionId,
      name: 'Cup',
      status: 'Ready',
      participants: { activeCount: 0, occupyingCount: 0, entries: [] },
      format: {
        kind: null,
        primaryStageId: null,
        primaryStageName: null,
        primaryStageStatus: null,
      },
      regulation: {
        minimumTeams: 2,
        maximumTeams: 64,
        durationPerPeriod: 45,
        numberOfPeriods: 2,
        winPoints: 3,
        drawPoints: 1,
        lossPoints: 0,
      },
      structure: {
        groupCount: 0,
        roundCount: 0,
        matchdayCount: 0,
        slotCount: 0,
        hasDrawRules: false,
        numberOfPots: null,
        matchGenerationFormat: 'SingleRoundRobin',
      },
      actions: [],
      readiness: structureReadiness(),
      stages: [],
    });
    vi.mocked(fetchMatchDetail).mockResolvedValue(scheduledMatch());
    vi.mocked(startMatch).mockRejectedValue(
      new ApiError(
        404,
        'Match not found',
        undefined,
        'Application.MatchNotFound',
      ),
    );
  });

  it('redirects to stage matches with an accessible notice on MatchNotFound', async () => {
    const user = userEvent.setup();
    renderRoutes(`/matches/${matchId}`);

    await user.click(
      await screen.findByRole('button', { name: 'Démarrer le match' }),
    );

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Match introuvable.');
    });
    expect(
      screen.queryByRole('button', { name: 'Démarrer le match' }),
    ).toBeNull();
  });
});
