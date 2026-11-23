import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  addDeclaredParticipation,
  fetchMatchDetail,
  fetchMatchesByStage,
  fetchStageOverview,
  fetchStructureView,
  finishMatch,
  recordDisciplinaryEvent,
  recordGoal,
  recordSubstitution,
  startMatch,
} from '../../api';
import type {
  DeclaredParticipation,
  DisciplinaryType,
  MatchDetail,
  StageOverview,
  StructureView,
} from '../../types';
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
    finishMatch: vi.fn(),
    recordGoal: vi.fn(),
    recordSubstitution: vi.fn(),
    recordDisciplinaryEvent: vi.fn(),
    addDeclaredParticipation: vi.fn(),
  };
});

const matchId = '11111111-1111-1111-1111-111111111111';
const stageId = '22222222-2222-2222-2222-222222222222';
const competitionId = '33333333-3333-3333-3333-333333333333';
const homeEntryId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const awayEntryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const memberId = '11111111-1111-1111-1111-111111111101';

const stageOverview: StageOverview = {
  id: stageId,
  competitionId,
  name: 'Journée 1',
  status: 'Running',
  rounds: [],
  slots: [],
  draws: [],
};

const structureFixture: StructureView = {
  competitionId,
  name: 'Cup',
  status: 'Ready' as const,
  participants: {
    activeCount: 2,
    occupyingCount: 2,
    entries: [
      {
        entryId: homeEntryId,
        displayName: 'Alpha',
        status: 'Active' as const,
        declaredMembers: [
          {
            memberId,
            displayName: 'Dupont',
            role: 'Player' as const,
          },
        ],
      },
      {
        entryId: awayEntryId,
        displayName: 'Beta',
        status: 'Active' as const,
        declaredMembers: [],
      },
    ],
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
    allowedTypes: ['Yellow', 'Red'] satisfies DisciplinaryType[],
  },
  structure: {
    groupCount: 0,
    roundCount: 0,
    matchdayCount: 0,
    slotCount: 0,
    hasDrawRules: false,
    numberOfPots: null,
    matchGenerationFormat: 'SingleRoundRobin' as const,
  },
  actions: [],
  readiness: structureReadiness(),
  stages: [],
};

const notFound = () =>
  new ApiError(404, 'Match not found', undefined, 'Application.MatchNotFound');

function participation(): DeclaredParticipation {
  return {
    memberId,
    displayName: 'Dupont',
    side: 'Home',
    compositionStatus: 'Starter',
    jerseyNumber: 9,
  };
}

type MutationCase = {
  name: string;
  match: MatchDetail;
  run: (user: ReturnType<typeof userEvent.setup>) => Promise<void>;
  mock: () => { mockRejectedValue: (error: unknown) => void };
};

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

function scheduledMatch(): MatchDetail {
  return {
    matchId,
    competitionId,
    stageId,
    status: 'Scheduled',
    home: { entryId: homeEntryId, displayName: 'Alpha' },
    away: { entryId: awayEntryId, displayName: 'Beta' },
    result: null,
    fixtureId: '44444444-4444-4444-4444-444444444444',
    legIndex: 1,
    declaredParticipations: [participation()],
    recordedGoals: [],
    recordedSubstitutions: [],
    recordedDisciplinaryEvents: [],
  };
}

describe('matchNotFoundNavigation (A7)', () => {
  const cases: MutationCase[] = [
    {
      name: 'start',
      match: scheduledMatch(),
      mock: () => vi.mocked(startMatch),
      run: async (user) => {
        await user.click(
          await screen.findByRole('button', { name: 'Démarrer le match' }),
        );
      },
    },
    {
      name: 'finish',
      match: {
        ...scheduledMatch(),
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 1, awayGoals: 0 },
      },
      mock: () => vi.mocked(finishMatch),
      run: async (user) => {
        await user.click(
          await screen.findByRole('button', { name: 'Terminer le match' }),
        );
      },
    },
    {
      name: 'record goal',
      match: {
        ...scheduledMatch(),
        status: 'Live',
        hasObservedLive: true,
        runningScore: { homeGoals: 0, awayGoals: 0 },
      },
      mock: () => vi.mocked(recordGoal),
      run: async (user) => {
        const panel = (
          await screen.findByRole('heading', { name: 'Buts' })
        ).closest('section') as HTMLElement;
        await user.selectOptions(
          within(panel).getByLabelText(/Buteur/i),
          memberId,
        );
        await user.click(
          within(panel).getByRole('button', { name: 'Enregistrer le but' }),
        );
      },
    },
    {
      name: 'record substitution',
      match: {
        ...scheduledMatch(),
        status: 'Live',
        hasObservedLive: true,
        declaredParticipations: [
          participation(),
          {
            ...participation(),
            memberId: '11111111-1111-1111-1111-111111111102',
            displayName: 'Martin',
            compositionStatus: 'Bench',
          },
        ],
      },
      mock: () => vi.mocked(recordSubstitution),
      run: async (user) => {
        const panel = (
          await screen.findByRole('heading', { name: 'Remplacements' })
        ).closest('section') as HTMLElement;
        await user.selectOptions(
          within(panel).getByLabelText(/Sortant/i),
          memberId,
        );
        await user.selectOptions(
          within(panel).getByLabelText(/Entrant/i),
          '11111111-1111-1111-1111-111111111102',
        );
        await user.click(
          within(panel).getByRole('button', {
            name: 'Enregistrer le remplacement',
          }),
        );
      },
    },
    {
      name: 'record discipline',
      match: {
        ...scheduledMatch(),
        status: 'Live',
        hasObservedLive: true,
      },
      mock: () => vi.mocked(recordDisciplinaryEvent),
      run: async (user) => {
        const panel = (
          await screen.findByRole('heading', { name: 'Discipline' })
        ).closest('section') as HTMLElement;
        await user.selectOptions(
          await within(panel).findByRole('combobox', { name: /Personne/i }),
          memberId,
        );
        await user.selectOptions(
          await within(panel).findByRole('combobox', { name: /Type/i }),
          'Yellow',
        );
        await user.click(
          await within(panel).findByRole('button', {
            name: 'Enregistrer le fait',
          }),
        );
      },
    },
    {
      name: 'match sheet',
      match: { ...scheduledMatch(), declaredParticipations: [] },
      mock: () => vi.mocked(addDeclaredParticipation),
      run: async (user) => {
        await screen.findByRole('heading', { name: 'Feuille de match' });
        const homeHeading = await screen.findByRole('heading', {
          name: 'Domicile',
        });
        const homeColumn = homeHeading.closest(
          '.match-sheet__side',
        ) as HTMLElement;
        await user.selectOptions(
          await within(homeColumn).findByLabelText(/Joueur/i),
          memberId,
        );
        await user.click(
          await within(homeColumn).findByRole('button', {
            name: 'Ajouter à la feuille',
          }),
        );
      },
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview);
    vi.mocked(fetchMatchesByStage).mockResolvedValue([]);
    vi.mocked(fetchStructureView).mockResolvedValue(structureFixture);
  });

  it.each(cases.map((c) => [c.name, c] as const))(
    'navigates to stage matches for %s',
    async (_, mutationCase) => {
      vi.mocked(fetchMatchDetail).mockResolvedValue(mutationCase.match);
      mutationCase.mock().mockRejectedValue(notFound());

      const user = userEvent.setup();
      renderRoutes(`/matches/${matchId}`);
      await mutationCase.run(user);

      await waitFor(() => {
        expect(screen.getByRole('alert')).toHaveTextContent(
          'Match introuvable.',
        );
      });
      expect(
        await screen.findByRole('heading', { name: /Journée 1/i }),
      ).toBeInTheDocument();
    },
  );
});
