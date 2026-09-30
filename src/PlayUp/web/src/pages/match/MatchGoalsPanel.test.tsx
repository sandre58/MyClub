import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  correctRecordedGoal,
  fetchMatchDetail,
  fetchStructureView,
  fetchStageOverview,
  finishMatch,
  recordGoal,
  removeRecordedGoal,
  setRunningScore,
  startMatch,
} from '../../api';
import type {
  DeclaredParticipation,
  MatchDetail,
  StructureView,
  RecordedGoal,
  StageOverview,
} from '../../types';
import {
  adjustRunningScore,
  canMutateRecordedGoals,
} from './matchGoalsHelpers';
import { MatchPage } from './MatchPage';

vi.mock('../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api')>();
  return {
    ...actual,
    fetchMatchDetail: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchStructureView: vi.fn(),
    startMatch: vi.fn(),
    finishMatch: vi.fn(),
    setRunningScore: vi.fn(),
    recordGoal: vi.fn(),
    correctRecordedGoal: vi.fn(),
    removeRecordedGoal: vi.fn(),
  };
});

const matchId = '11111111-1111-1111-1111-111111111111';
const stageId = '22222222-2222-2222-2222-222222222222';
const competitionId = '33333333-3333-3333-3333-333333333333';
const fixtureId = '44444444-4444-4444-4444-444444444444';
const homeEntryId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const awayEntryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const dupontId = '11111111-1111-1111-1111-111111111101';
const martinId = '11111111-1111-1111-1111-111111111102';
const goalId = '99999999-9999-9999-9999-999999999901';

function participation(
  overrides: Partial<DeclaredParticipation> = {},
): DeclaredParticipation {
  return {
    memberId: dupontId,
    displayName: 'Dupont',
    side: 'Home',
    compositionStatus: 'Starter',
    jerseyNumber: 9,
    ...overrides,
  };
}

function goal(overrides: Partial<RecordedGoal> = {}): RecordedGoal {
  return {
    goalId,
    scorerMemberId: dupontId,
    scorerDisplayName: 'Dupont',
    creditedSide: 'Home',
    assisterMemberId: null,
    assisterDisplayName: null,
    isOwnGoal: false,
    ...overrides,
  };
}

function baseMatch(overrides: Partial<MatchDetail> = {}): MatchDetail {
  return {
    matchId,
    competitionId,
    stageId,
    status: 'Scheduled',
    home: { entryId: homeEntryId, displayName: 'Alpha' },
    away: { entryId: awayEntryId, displayName: 'Beta' },
    result: null,
    fixtureId,
    legIndex: 1,
    hasObservedLive: false,
    declaredParticipations: [
      participation(),
      participation({
        memberId: martinId,
        displayName: 'Martin',
        side: 'Away',
        compositionStatus: 'Starter',
        jerseyNumber: 10,
      }),
    ],
    recordedGoals: [],
    ...overrides,
  };
}

const emptyStructure: StructureView = {
  competitionId,
  name: 'Spring Cup',
  status: 'Ready',
  participants: {
    activeCount: 2,
    occupyingCount: 2,
    entries: [],
  },
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
  readiness: {
    readyForNextSlice: false,
    readyForDraw: false,
    readyForMaterialization: false,
    readyForSchedule: false,
    readyForMatchOperation: false,
    readyForSchedulePath: false,
    attachedMatchCount: 0,
    blockers: [],
  },
  stages: [],
};

const stageOverview: StageOverview = {
  id: stageId,
  competitionId,
  name: 'Journée 1',
  status: 'Draft',
  rounds: [],
  slots: [],
  draws: [],
};

function renderMatchPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/matches/${matchId}`]}>
        <Routes>
          <Route path="/matches/:matchId" element={<MatchPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('adjustRunningScore / canMutateRecordedGoals', () => {
  it('adjusts home/away with floor 0', () => {
    expect(
      adjustRunningScore({ homeGoals: 1, awayGoals: 2 }, 'Home', 1),
    ).toEqual({ homeGoals: 2, awayGoals: 2 });
    expect(
      adjustRunningScore({ homeGoals: 0, awayGoals: 1 }, 'Home', -1),
    ).toEqual({ homeGoals: 0, awayGoals: 1 });
    expect(adjustRunningScore(null, 'Away', 1)).toEqual({
      homeGoals: 0,
      awayGoals: 1,
    });
  });

  it('blocks create/remove UI after Finished∧Live', () => {
    expect(
      canMutateRecordedGoals(
        baseMatch({ status: 'Finished', hasObservedLive: true }),
      ),
    ).toBe(false);
    expect(
      canMutateRecordedGoals(
        baseMatch({ status: 'Finished', hasObservedLive: false }),
      ),
    ).toBe(true);
    expect(canMutateRecordedGoals(baseMatch({ status: 'Live' }))).toBe(true);
  });
});

describe('MatchPage goals (Lot 3)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview);
    vi.mocked(fetchStructureView).mockResolvedValue(emptyStructure);
    vi.mocked(startMatch).mockResolvedValue();
    vi.mocked(finishMatch).mockResolvedValue();
    vi.mocked(setRunningScore).mockResolvedValue();
    vi.mocked(recordGoal).mockResolvedValue();
    vi.mocked(correctRecordedGoal).mockResolvedValue();
    vi.mocked(removeRecordedGoal).mockResolvedValue();
  });

  it('Live: records a goal then bumps RunningScore (+1 credited side)', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(recordGoal).mock.calls.length > 0) {
        return baseMatch({
          status: 'Live',
          hasObservedLive: true,
          runningScore: { homeGoals: 1, awayGoals: 0 },
          recordedGoals: [goal()],
        });
      }
      return baseMatch({
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 0, awayGoals: 0 },
      });
    });

    renderMatchPage();

    const goalsHeading = await screen.findByRole('heading', { name: 'Buts' });
    const goalsPanel = goalsHeading.closest('section');
    expect(goalsPanel).not.toBeNull();

    await user.selectOptions(
      within(goalsPanel as HTMLElement).getByLabelText(/Buteur/i),
      dupontId,
    );
    await user.click(
      within(goalsPanel as HTMLElement).getByRole('button', {
        name: 'Enregistrer le but',
      }),
    );

    await waitFor(() => {
      expect(recordGoal).toHaveBeenCalledWith(matchId, {
        scorerMemberId: dupontId,
        creditedSide: 'Home',
        assisterMemberId: null,
      });
    });
    await waitFor(() => {
      expect(setRunningScore).toHaveBeenCalledWith(matchId, {
        homeGoals: 1,
        awayGoals: 0,
      });
    });
    expect(
      within(goalsPanel as HTMLElement).getByText('Dupont', {
        selector: '.match-goals__scorer',
      }),
    ).toBeInTheDocument();
  });

  it('Live: if RunningScore fails after RecordGoal, shows desync and still lists goal', async () => {
    const user = userEvent.setup();
    vi.mocked(setRunningScore).mockRejectedValue(
      new ApiError(409, 'RS failed', undefined, 'Match.InvalidTransition'),
    );
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(recordGoal).mock.calls.length > 0) {
        return baseMatch({
          status: 'Live',
          hasObservedLive: true,
          runningScore: { homeGoals: 0, awayGoals: 0 },
          recordedGoals: [goal()],
        });
      }
      return baseMatch({
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 0, awayGoals: 0 },
      });
    });

    renderMatchPage();

    const goalsHeading = await screen.findByRole('heading', { name: 'Buts' });
    const goalsPanel = goalsHeading.closest('section') as HTMLElement;

    await user.selectOptions(
      within(goalsPanel).getByLabelText(/Buteur/i),
      dupontId,
    );
    await user.click(
      within(goalsPanel).getByRole('button', { name: 'Enregistrer le but' }),
    );

    expect(
      await within(goalsPanel).findByText(
        /mise à jour du score live a échoué/i,
      ),
    ).toBeInTheDocument();
    expect(
      within(goalsPanel).getByText('Dupont', {
        selector: '.match-goals__scorer',
      }),
    ).toBeInTheDocument();
    expect(recordGoal).toHaveBeenCalled();
    expect(setRunningScore).toHaveBeenCalled();
  });

  it('Scheduled: records a goal without SetRunningScore', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(recordGoal).mock.calls.length > 0) {
        return baseMatch({ recordedGoals: [goal()] });
      }
      return baseMatch();
    });

    renderMatchPage();

    const goalsHeading = await screen.findByRole('heading', { name: 'Buts' });
    const goalsPanel = goalsHeading.closest('section') as HTMLElement;

    await user.selectOptions(
      within(goalsPanel).getByLabelText(/Buteur/i),
      dupontId,
    );
    await user.click(
      within(goalsPanel).getByRole('button', { name: 'Enregistrer le but' }),
    );

    await waitFor(() => {
      expect(recordGoal).toHaveBeenCalled();
    });
    expect(setRunningScore).not.toHaveBeenCalled();
  });

  it('Finished∧Live: goals are read-only (no create form)', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({
        status: 'Finished',
        hasObservedLive: true,
        result: {
          type: 'Played',
          homeGoals: 1,
          awayGoals: 0,
          extraTimePlayed: false,
          shootout: null,
        },
        recordedGoals: [goal()],
      }),
    );

    renderMatchPage();

    const goalsHeading = await screen.findByRole('heading', { name: 'Buts' });
    const goalsPanel = goalsHeading.closest('section') as HTMLElement;

    expect(
      within(goalsPanel).getByText('Dupont', {
        selector: '.match-goals__scorer',
      }),
    ).toBeInTheDocument();
    expect(
      within(goalsPanel).getByText(/buts ne sont plus modifiables/i),
    ).toBeInTheDocument();
    expect(
      within(goalsPanel).queryByRole('button', { name: 'Enregistrer le but' }),
    ).not.toBeInTheDocument();
    expect(
      within(goalsPanel).queryByRole('button', { name: 'Corriger' }),
    ).not.toBeInTheDocument();
  });

  it('removes a goal and decrements Live RunningScore', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(removeRecordedGoal).mock.calls.length > 0) {
        return baseMatch({
          status: 'Live',
          hasObservedLive: true,
          runningScore: { homeGoals: 0, awayGoals: 0 },
          recordedGoals: [],
        });
      }
      return baseMatch({
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 1, awayGoals: 0 },
        recordedGoals: [goal()],
      });
    });

    renderMatchPage();

    const goalsHeading = await screen.findByRole('heading', { name: 'Buts' });
    const goalsPanel = goalsHeading.closest('section') as HTMLElement;

    await user.click(
      within(goalsPanel).getByRole('button', { name: 'Retirer' }),
    );
    await user.click(
      within(goalsPanel).getByRole('button', { name: 'Confirmer le retrait' }),
    );

    await waitFor(() => {
      expect(removeRecordedGoal).toHaveBeenCalledWith(matchId, goalId);
    });
    await waitFor(() => {
      expect(setRunningScore).toHaveBeenCalledWith(matchId, {
        homeGoals: 0,
        awayGoals: 0,
      });
    });
  });
});
