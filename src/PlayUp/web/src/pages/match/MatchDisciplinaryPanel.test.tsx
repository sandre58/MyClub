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
  recordDisciplinaryEvent,
  removeRecordedDisciplinaryEvent,
  setRunningScore,
  startMatch,
} from '../../api';
import type {
  DeclaredParticipation,
  MatchDetail,
  StructureView,
  RecordedDisciplinaryEvent,
  StageOverview,
} from '../../types';
import { canMutateRecordedDisciplinaryEvents } from './MatchDisciplinaryPanel';
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
    recordDisciplinaryEvent: vi.fn(),
    removeRecordedDisciplinaryEvent: vi.fn(),
  };
});

const matchId = '11111111-1111-1111-1111-111111111111';
const stageId = '22222222-2222-2222-2222-222222222222';
const competitionId = '33333333-3333-3333-3333-333333333333';
const fixtureId = '44444444-4444-4444-4444-444444444444';
const homeEntryId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const awayEntryId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const dupontId = '11111111-1111-1111-1111-111111111101';
const coachId = '11111111-1111-1111-1111-111111111199';
const eventId = '99999999-9999-9999-9999-999999999901';

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

function sheet(): DeclaredParticipation[] {
  return [
    participation(),
    participation({
      memberId: coachId,
      displayName: 'Coach',
      compositionStatus: 'Bench',
      jerseyNumber: null,
    }),
  ];
}

function disciplinaryEvent(
  overrides: Partial<RecordedDisciplinaryEvent> = {},
): RecordedDisciplinaryEvent {
  return {
    disciplinaryEventId: eventId,
    memberId: dupontId,
    memberDisplayName: 'Dupont',
    type: 'Yellow',
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
    declaredParticipations: sheet(),
    recordedGoals: [],
    recordedSubstitutions: [],
    recordedDisciplinaryEvents: [],
    ...overrides,
  };
}

function structureWithTypes(
  allowedTypes: Array<'Yellow' | 'Red' | 'White'> = ['Yellow', 'Red'],
): StructureView {
  return {
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
      allowedTypes,
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
      readyForNextSlice: true,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: true,
      readyForSchedulePath: false,
      attachedMatchCount: 1,
      blockers: [],
    },
    stages: [],
  };
}

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
          <Route
            path="/competitions/:competitionId/structure"
            element={<p>Structure route</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('canMutateRecordedDisciplinaryEvents', () => {
  it('allows #4-like window including Scheduled', () => {
    expect(
      canMutateRecordedDisciplinaryEvents(baseMatch({ status: 'Scheduled' })),
    ).toBe(true);
    expect(
      canMutateRecordedDisciplinaryEvents(baseMatch({ status: 'Live' })),
    ).toBe(true);
    expect(
      canMutateRecordedDisciplinaryEvents(
        baseMatch({
          status: 'Finished',
          hasObservedLive: false,
          runningScore: null,
        }),
      ),
    ).toBe(true);
    expect(
      canMutateRecordedDisciplinaryEvents(
        baseMatch({ status: 'Finished', hasObservedLive: true }),
      ),
    ).toBe(false);
    expect(
      canMutateRecordedDisciplinaryEvents(baseMatch({ status: 'Cancelled' })),
    ).toBe(false);
  });
});

describe('MatchPage discipline (Lot 1)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(fetchStageOverview).mockResolvedValue(stageOverview);
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureWithTypes(['Yellow', 'Red']),
    );
    vi.mocked(startMatch).mockResolvedValue();
    vi.mocked(finishMatch).mockResolvedValue();
    vi.mocked(setRunningScore).mockResolvedValue();
    vi.mocked(recordDisciplinaryEvent).mockResolvedValue();
    vi.mocked(removeRecordedDisciplinaryEvent).mockResolvedValue();
  });

  it('Live: records Yellow for sheet member without touching RunningScore', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(recordDisciplinaryEvent).mock.calls.length > 0) {
        return baseMatch({
          recordedDisciplinaryEvents: [disciplinaryEvent()],
        });
      }
      return baseMatch();
    });

    renderMatchPage();

    const heading = await screen.findByRole('heading', { name: 'Discipline' });
    const panel = heading.closest('section') as HTMLElement;

    await user.selectOptions(
      await within(panel).findByLabelText(/^Personne$/i),
      dupontId,
    );
    await user.selectOptions(
      await within(panel).findByLabelText(/^Type$/i),
      'Yellow',
    );
    await user.click(
      await within(panel).findByRole('button', { name: 'Enregistrer le fait' }),
    );

    await waitFor(() => {
      expect(recordDisciplinaryEvent).toHaveBeenCalledWith(matchId, {
        memberId: dupontId,
        type: 'Yellow',
      });
    });
    expect(setRunningScore).not.toHaveBeenCalled();
    expect(within(panel).getByText(/Jaune · Dupont/)).toBeInTheDocument();
  });

  it('Scheduled: still allows Create (#4-like)', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(
      baseMatch({
        status: 'Scheduled',
        hasObservedLive: false,
        runningScore: null,
      }),
    );

    renderMatchPage();

    const heading = await screen.findByRole('heading', { name: 'Discipline' });
    const panel = heading.closest('section') as HTMLElement;

    expect(
      await within(panel).findByRole('button', { name: 'Enregistrer le fait' }),
    ).toBeInTheDocument();
    expect(
      within(panel).queryByText(/ne sont plus modifiables/i),
    ).not.toBeInTheDocument();
  });

  it('allowedTypes []: explicit noneAllowed, no Create form', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(structureWithTypes([]));
    vi.mocked(fetchMatchDetail).mockResolvedValue(baseMatch());

    renderMatchPage();

    const heading = await screen.findByRole('heading', { name: 'Discipline' });
    const panel = heading.closest('section') as HTMLElement;

    expect(
      await within(panel).findByText(/Aucun type disciplinaire autorisé/i),
    ).toBeInTheDocument();
    expect(
      within(panel).getByRole('link', { name: /Configurer le règlement/i }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/structure`);
    expect(
      within(panel).queryByRole('button', { name: 'Enregistrer le fait' }),
    ).not.toBeInTheDocument();
  });

  it('type select only lists AllowedTypes (no White when not allowed)', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(baseMatch());

    renderMatchPage();

    const heading = await screen.findByRole('heading', { name: 'Discipline' });
    const panel = heading.closest('section') as HTMLElement;
    const typeSelect = await within(panel).findByLabelText(/^Type$/i);

    expect(
      within(typeSelect).getByRole('option', { name: 'Jaune' }),
    ).toBeInTheDocument();
    expect(
      within(typeSelect).getByRole('option', { name: 'Rouge' }),
    ).toBeInTheDocument();
    expect(
      within(typeSelect).queryByRole('option', { name: 'Blanc' }),
    ).not.toBeInTheDocument();
  });

  it('offers every sheet member including Staff-like Bench', async () => {
    vi.mocked(fetchMatchDetail).mockResolvedValue(baseMatch());

    renderMatchPage();

    const heading = await screen.findByRole('heading', { name: 'Discipline' });
    const panel = heading.closest('section') as HTMLElement;
    const memberSelect = await within(panel).findByLabelText(/^Personne$/i);

    expect(
      within(memberSelect).getByRole('option', { name: /Dupont/ }),
    ).toBeInTheDocument();
    expect(
      within(memberSelect).getByRole('option', { name: /Coach/ }),
    ).toBeInTheDocument();
  });

  it('removes a disciplinary event with consequence confirm', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchMatchDetail).mockImplementation(async () => {
      if (vi.mocked(removeRecordedDisciplinaryEvent).mock.calls.length > 0) {
        return baseMatch({ recordedDisciplinaryEvents: [] });
      }
      return baseMatch({
        recordedDisciplinaryEvents: [disciplinaryEvent()],
      });
    });

    renderMatchPage();

    const heading = await screen.findByRole('heading', { name: 'Discipline' });
    const panel = heading.closest('section') as HTMLElement;

    await user.click(within(panel).getByRole('button', { name: 'Retirer' }));
    await user.click(
      within(panel).getByRole('button', { name: 'Confirmer le retrait' }),
    );

    await waitFor(() => {
      expect(removeRecordedDisciplinaryEvent).toHaveBeenCalledWith(
        matchId,
        eventId,
      );
    });
  });
});
