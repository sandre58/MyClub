import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { ApiError } from '../../../api/http';
import {
  confirmResolutionRelease,
  previewResolutionRelease,
} from '../../../api';
import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { StructureResolutionReleaseAction } from './StructureResolutionReleaseAction';
import { useStructureResolutionRelease } from './structureResolutionReleaseContext';

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
      screen.getByText('Le classement ne désigne aucune équipe'),
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
          awayEntryId: null,
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

    expect(await screen.findByText('Manche 1 — domicile')).toBeInTheDocument();
    expect(
      screen.getByText("Le côté de l'équipe redevient à déterminer :"),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Libérer' })).toHaveClass(
      'ds-btn--destructive',
    );
    expect(
      screen.getByRole('link', { name: 'Jouer le match' }),
    ).toHaveAttribute('href', `/matches/${matchId}`);
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
        'Une affectation directe n’est pas libérée. (409)',
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('dialog', { name: 'Libérer cette place ?' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Libérer' })).toBeDisabled();
  });
});
