import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  fetchMatchDetail,
  fetchStructureView,
  fetchStageOverview,
  finishMatch,
  recordSubstitution,
  removeRecordedSubstitution,
  setRunningScore,
  startMatch,
} from '../../api';
import type {
  DeclaredParticipation,
  MatchDetail,
  StructureView,
  RecordedSubstitution,
  StageOverview,
} from '../../types';
import {
  canMutateRecordedSubstitutions,
  deriveOnFieldMembers,
} from './matchSubstitutionsHelpers';
import { MatchPage } from './MatchPage';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    fetchMatchDetail: vi.fn(),
    fetchStageOverview: vi.fn(),
    fetchStructureView: vi.fn(),
    startMatch: vi.fn(),
    finishMatch: vi.fn(),
    setRunningScore: vi.fn(),
    recordSubstitution: vi.fn(),
    removeRecordedSubstitution: vi.fn(),
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
const bernardId = '11111111-1111-1111-1111-111111111103';
const subId = '99999999-9999-9999-9999-999999999901';

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

function sheetHome(): DeclaredParticipation[] {
  return [
    participation(),
    participation({
      memberId: martinId,
      displayName: 'Martin',
      compositionStatus: 'Bench',
      jerseyNumber: 12,
    }),
    participation({
      memberId: bernardId,
      displayName: 'Bernard',
      compositionStatus: 'Starter',
      jerseyNumber: 7,
    }),
  ];
}

function substitution(
  overrides: Partial<RecordedSubstitution> = {},
): RecordedSubstitution {
  return {
    substitutionId: subId,
    side: 'Home',
    outMemberId: dupontId,
    outDisplayName: 'Dupont',
    inMemberId: martinId,
    inDisplayName: 'Martin',
    ...overrides,
  };
}

function baseMatch(overrides: Partial<MatchDetail> = {}): MatchDetail {
  return {
    matchId,
    competitionId,
    stageId,
    status: 'Live',
    home: { entryId: homeEntryId, displayName: 'Alpha' },
    away: { entryId: awayEntryId, displayName: 'Beta' },
    result: null,
    fixtureId,
    legIndex: 1,
    hasObservedLive: true,
    runningScore: { homeGoals: 0, awayGoals: 0 },
    declaredParticipations: sheetHome(),
    recordedGoals: [],
    recordedSubstitutions: [],
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

describe('deriveOnFieldMembers / canMutateRecordedSubstitutions', () => {
  it('starts from Starters and replays ordered substitutions', () => {
    const onField = deriveOnFieldMembers(sheetHome(), [substitution()], 'Home');
    expect(onField.has(dupontId)).toBe(false);
    expect(onField.has(martinId)).toBe(true);
    expect(onField.has(bernardId)).toBe(true);
  });

  it('supports upToExclusive for Correct at an index', () => {
    const before = deriveOnFieldMembers(
      sheetHome(),
      [substitution()],
      'Home',
      0,
    );
    expect(before.has(dupontId)).toBe(true);
    expect(before.has(martinId)).toBe(false);
  });

  it('allows Live and Finished∧¬Live only', () => {
    expect(canMutateRecordedSubstitutions(baseMatch({ status: 'Live' }))).toBe(
      true,
    );
    expect(
      canMutateRecordedSubstitutions(
        baseMatch({ status: 'Finished', hasObservedLive: false }),
      ),
    ).toBe(true);
    expect(
      canMutateRecordedSubstitutions(
        baseMatch({ status: 'Finished', hasObservedLive: true }),
      ),
    ).toBe(false);
    expect(
      canMutateRecordedSubstitutions(baseMatch({ status: 'Scheduled' })),
    ).toBe(false);
  });
});

describe('MatchPage substitutions (Lot 1)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview);
    vi.mocked(fetchStructureView).mockResolvedValue(emptyStructure);
    vi.mocked(startMatch).mockResolvedValue();
    vi.mocked(finishMatch).mockResolvedValue();
    vi.mocked(setRunningScore).mockResolvedValue();
    vi.mocked(recordSubstitution).mockResolvedValue();
    vi.mocked(removeRecordedSubstitution).mockResolvedValue();
  });

  it('Live: records Dupont → Martin without touching RunningScore', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(recordSubstitution).mock.calls.length > 0) {
        return baseMatch({ recordedSubstitutions: [substitution()] });
      }
      return baseMatch();
    });

    renderMatchPage();

    const heading = await screen.findByRole('heading', {
      name: 'Remplacements',
    });
    const panel = heading.closest('section') as HTMLElement;

    await user.selectOptions(
      within(panel).getByLabelText(/Sortant/i),
      dupontId,
    );
    await user.selectOptions(
      within(panel).getByLabelText(/Entrant/i),
      martinId,
    );
    await user.click(
      within(panel).getByRole('button', {
        name: 'Enregistrer le remplacement',
      }),
    );

    await waitFor(() => {
      expect(recordSubstitution).toHaveBeenCalledWith(matchId, {
        outMemberId: dupontId,
        inMemberId: martinId,
        side: 'Home',
      });
    });
    expect(setRunningScore).not.toHaveBeenCalled();
    expect(
      within(panel).getByText(/Dupont → Martin/, {
        selector: '.match-subs__pair',
      }),
    ).toBeInTheDocument();
  });

  it('Scheduled: read-only (Domain refuses Create)', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({
        status: 'Scheduled',
        hasObservedLive: false,
        runningScore: null,
      }),
    );

    renderMatchPage();

    const heading = await screen.findByRole('heading', {
      name: 'Remplacements',
    });
    const panel = heading.closest('section') as HTMLElement;

    expect(
      within(panel).getByText(/ne sont plus modifiables/i),
    ).toBeInTheDocument();
    expect(
      within(panel).queryByRole('button', {
        name: 'Enregistrer le remplacement',
      }),
    ).not.toBeInTheDocument();
  });

  it('Finished∧Live: read-only (CorrectAfterFinish hors V1)', async () => {
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
        recordedSubstitutions: [substitution()],
      }),
    );

    renderMatchPage();

    const heading = await screen.findByRole('heading', {
      name: 'Remplacements',
    });
    const panel = heading.closest('section') as HTMLElement;

    expect(
      within(panel).getByText(/Dupont → Martin/, {
        selector: '.match-subs__pair',
      }),
    ).toBeInTheDocument();
    expect(
      within(panel).queryByRole('button', { name: 'Corriger' }),
    ).not.toBeInTheDocument();
    expect(
      within(panel).queryByRole('button', {
        name: 'Enregistrer le remplacement',
      }),
    ).not.toBeInTheDocument();
  });

  it('removes a substitution with consequence confirm', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(removeRecordedSubstitution).mock.calls.length > 0) {
        return baseMatch({ recordedSubstitutions: [] });
      }
      return baseMatch({ recordedSubstitutions: [substitution()] });
    });

    renderMatchPage();

    const heading = await screen.findByRole('heading', {
      name: 'Remplacements',
    });
    const panel = heading.closest('section') as HTMLElement;

    await user.click(within(panel).getByRole('button', { name: 'Retirer' }));
    await user.click(
      within(panel).getByRole('button', { name: 'Confirmer le retrait' }),
    );

    await waitFor(() => {
      expect(removeRecordedSubstitution).toHaveBeenCalledWith(matchId, subId);
    });
  });

  it('Finished∧¬Live: allows create (reconstruction)', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(recordSubstitution).mock.calls.length > 0) {
        return baseMatch({
          status: 'Finished',
          hasObservedLive: false,
          runningScore: null,
          result: {
            type: 'Played',
            homeGoals: 0,
            awayGoals: 0,
            extraTimePlayed: false,
            shootout: null,
          },
          recordedSubstitutions: [substitution()],
        });
      }
      return baseMatch({
        status: 'Finished',
        hasObservedLive: false,
        runningScore: null,
        result: {
          type: 'Played',
          homeGoals: 0,
          awayGoals: 0,
          extraTimePlayed: false,
          shootout: null,
        },
      });
    });

    renderMatchPage();

    const heading = await screen.findByRole('heading', {
      name: 'Remplacements',
    });
    const panel = heading.closest('section') as HTMLElement;

    await user.selectOptions(
      within(panel).getByLabelText(/Sortant/i),
      dupontId,
    );
    await user.selectOptions(
      within(panel).getByLabelText(/Entrant/i),
      martinId,
    );
    await user.click(
      within(panel).getByRole('button', {
        name: 'Enregistrer le remplacement',
      }),
    );

    await waitFor(() => {
      expect(recordSubstitution).toHaveBeenCalled();
    });
  });
});
