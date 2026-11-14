import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const navigate = vi.fn();

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => navigate,
  };
});

import { ApiError } from '../../../api/http';
import {
  confirmResolutionRelease,
  previewResolutionRelease,
} from '../../../api';
import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { scheduleStructureRefresh } from '../structureInvalidation';
import { StructureResolutionReleaseAction } from './StructureResolutionReleaseAction';
import { useStructureResolutionRelease } from './structureResolutionReleaseContext';

vi.mock('../structureInvalidation', () => ({
  scheduleStructureRefresh: vi.fn(),
}));

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    previewResolutionRelease: vi.fn(),
    confirmResolutionRelease: vi.fn(),
  };
});

const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const entryId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const matchId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';

const occupant: StructureResolutionOccupant = {
  target: 'Slot',
  entryId,
  provenanceKind: 'Qualification',
  sourcePathId: 'Q|seed',
  state: 'Indetermine',
  slotKey: 'Dernier',
  indeterminacyCause: 'SelectionVide',
  releasable: true,
  cascadeReplaceable: false,
};

const stage = {
  stageId,
  name: 'Classement',
  status: 'Running',
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
  resolutionOccupants: [occupant],
} as StructureStageHubSummary;

function ReleaseTrigger({
  target = occupant,
}: {
  target?: StructureResolutionOccupant;
}) {
  const release = useStructureResolutionRelease();
  return (
    <button type="button" onClick={() => release?.requestRelease(target)}>
      open
    </button>
  );
}

function renderAction(target?: StructureResolutionOccupant) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StructureResolutionReleaseAction
          stage={stage}
          competitionId="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
        >
          <ReleaseTrigger target={target} />
        </StructureResolutionReleaseAction>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const emptyPlan = {
  versionToken: 'token-1',
  releases: [],
  detachedMatches: [],
  affectedOccupants: [],
};

describe('StructureResolutionReleaseAction', () => {
  beforeEach(() => {
    navigate.mockReset();
  });

  it('confirms a release that changes no match', async () => {
    vi.mocked(previewResolutionRelease).mockResolvedValue(emptyPlan);
    vi.mocked(confirmResolutionRelease).mockResolvedValue(emptyPlan);
    const user = userEvent.setup();
    renderAction();

    await user.click(screen.getByRole('button', { name: 'open' }));

    expect(
      await screen.findByRole('dialog', { name: 'Libérer cette place ?' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        'Le classement amont ne désigne plus aucune équipe pour cette place.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Aucun match ne sera modifié.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Libérer' })).toHaveClass(
      'ds-btn--primary',
    );

    await user.click(screen.getByRole('button', { name: 'Libérer' }));
    expect(confirmResolutionRelease).toHaveBeenCalledWith(stageId, {
      target: 'Slot',
      entryId,
      slotKey: 'Dernier',
      groupId: null,
      versionToken: 'token-1',
    });
  });

  it('names the unplayed match and links to the source match', async () => {
    vi.mocked(previewResolutionRelease).mockResolvedValue({
      ...emptyPlan,
      detachedMatches: [
        {
          matchId,
          stageId,
          legIndex: 1,
          side: 'Home',
          homeEntryId: entryId,
          awayEntryId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          homeName: 'Nord',
          awayName: 'Est',
        },
      ],
    });
    const user = userEvent.setup();
    renderAction({
      ...occupant,
      indeterminacyCause: 'MatchNonJoue',
      actionMatchId: matchId,
    });

    await user.click(screen.getByRole('button', { name: 'open' }));

    expect(
      await screen.findByText(
        'Manche 1 (Nord – Est) : le côté domicile redevient à déterminer.',
      ),
    ).toBeInTheDocument();
    expect(screen.getByText('Dans ces matchs non joués :')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Libérer' })).toHaveClass(
      'ds-btn--primary',
    );
    expect(
      screen.getByRole('button', { name: 'Jouer le match' }),
    ).toBeInTheDocument();
  });

  it('closes without releasing when the secondary action is used', async () => {
    vi.mocked(previewResolutionRelease).mockResolvedValue({
      ...emptyPlan,
      detachedMatches: [
        {
          matchId,
          stageId,
          legIndex: 1,
          side: 'Home',
          homeEntryId: entryId,
          awayEntryId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          homeName: 'Nord',
          awayName: 'Est',
        },
      ],
    });
    const user = userEvent.setup();
    renderAction({
      ...occupant,
      indeterminacyCause: 'MatchNonJoue',
      actionMatchId: matchId,
      actionMatchLabel: 'Nord – Est',
    });

    await user.click(screen.getByRole('button', { name: 'open' }));
    await user.click(screen.getByRole('button', { name: /^Jouer le match$/ }));

    expect(confirmResolutionRelease).not.toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(`/matches/${matchId}`);
    await waitFor(() => {
      expect(
        screen.queryByRole('dialog', { name: 'Libérer cette place ?' }),
      ).not.toBeInTheDocument();
    });
  });

  it('keeps a refusal in the dialog footer and disables confirm', async () => {
    vi.mocked(previewResolutionRelease).mockResolvedValue(emptyPlan);
    vi.mocked(confirmResolutionRelease).mockRejectedValue(
      new ApiError(
        409,
        'Direct assignment',
        undefined,
        'Application.ResolutionReleaseDirectAssignment',
      ),
    );
    const user = userEvent.setup();
    renderAction();

    await user.click(screen.getByRole('button', { name: 'open' }));
    await user.click(await screen.findByRole('button', { name: 'Libérer' }));

    expect(
      await screen.findByText(
        'Cette place est fixée à la main : retirez l’affectation directe. (409)',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('dialog', { name: 'Libérer cette place ?' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Libérer' })).toBeDisabled();
    expect(scheduleStructureRefresh).toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Annuler' }));
    expect(scheduleStructureRefresh).toHaveBeenCalledTimes(2);
  });

  it('shows a preview refusal in the dialog and refreshes the structure', async () => {
    vi.mocked(previewResolutionRelease).mockRejectedValue(
      new ApiError(
        404,
        'Missing',
        undefined,
        'Application.ResolutionReleaseOccupantNotFound',
      ),
    );
    const user = userEvent.setup();
    renderAction();

    await user.click(screen.getByRole('button', { name: 'open' }));

    expect(
      await screen.findByRole('dialog', { name: 'Libérer cette place ?' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Il n’y a rien à libérer. (404)'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Libérer' })).toBeDisabled();
    expect(scheduleStructureRefresh).toHaveBeenCalledTimes(1);

    await user.click(screen.getByRole('button', { name: 'Annuler' }));
    expect(scheduleStructureRefresh).toHaveBeenCalledTimes(2);
  });

  it('names the team that leaves the composition and a downstream place', async () => {
    vi.mocked(previewResolutionRelease).mockResolvedValue({
      ...emptyPlan,
      releases: [
        {
          stageId,
          target: 'Slot',
          slotKey: 'Dernier',
          groupId: null,
          entryId,
          entryName: 'Centre',
        },
        {
          stageId,
          target: 'Population',
          slotKey: null,
          groupId: null,
          entryId,
          entryName: 'Centre',
        },
      ],
      affectedOccupants: [
        {
          stageId: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
          target: 'Slot',
          slotKey: 'Suite',
          groupId: null,
          entryId,
          stateBefore: 'Perime',
          stateAfter: 'AJour',
          entryName: 'Centre',
          stageName: 'Aval',
          placeLabel: 'Suite',
        },
      ],
    });
    const user = userEvent.setup();
    renderAction();

    await user.click(screen.getByRole('button', { name: 'open' }));

    expect(
      await screen.findByText(
        'Centre quittera aussi la composition de cette phase.',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Centre — Suite (Aval) : Périmé → À jour'),
    ).toBeInTheDocument();
  });
});
