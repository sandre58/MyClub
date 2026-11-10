import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { ApiError } from '../../../api/http';
import { confirmCascadeReplace, previewCascadeReplace } from '../../../api';
import type {
  StructureResolutionOccupant,
  StructureStageHubSummary,
} from '../../../types';
import { StructureCascadeReplaceAction } from './StructureCascadeReplaceAction';
import { useStructureCascadeReplace } from './structureCascadeReplaceContext';
import { StructureResolutionFreshnessBadge } from './StructureResolutionFreshnessBadge';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    previewCascadeReplace: vi.fn(),
    confirmCascadeReplace: vi.fn(),
  };
});

const stageId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const entryId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const matchId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';

const occupant: StructureResolutionOccupant = {
  target: 'Slot',
  entryId,
  provenanceKind: 'Qualification',
  sourcePathId: 'Q1',
  state: 'Perime',
  slotKey: 'Champ',
  stalenessCause: 'ResultatAmont',
  cascadeReplaceable: true,
};

const stage = {
  stageId,
  name: 'Finale',
  status: 'Running',
  teamCount: 2,
  matchCount: 1,
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

function ReplaceBadge() {
  const cascade = useStructureCascadeReplace();
  return (
    <StructureResolutionFreshnessBadge
      occupant={occupant}
      onReplace={cascade?.requestReplace}
    />
  );
}

function renderAction() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <StructureCascadeReplaceAction
        stage={stage}
        competitionId="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
      >
        <ReplaceBadge />
      </StructureCascadeReplaceAction>
    </QueryClientProvider>,
  );
}

describe('StructureCascadeReplaceAction', () => {
  it('lists the played matches before confirming', async () => {
    vi.mocked(previewCascadeReplace).mockResolvedValue({
      versionToken: 'token-1',
      playedMatches: [
        {
          matchId,
          stageId,
          homeEntryId: entryId,
          awayEntryId: null,
        },
      ],
      replacements: [],
    });
    vi.mocked(confirmCascadeReplace).mockResolvedValue({
      versionToken: 'token-1',
      playedMatches: [],
      replacements: [],
    });
    const user = userEvent.setup();
    renderAction();

    expect(
      screen.queryByRole('button', { name: 'Remplacer Champ' }),
    ).toBeNull();
    await user.click(
      screen.getByRole('button', {
        name: "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
      }),
    );
    expect(await screen.findByText(`Match ${matchId}`)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Remplacer' }));
    expect(confirmCascadeReplace).toHaveBeenCalledWith(stageId, {
      target: 'Slot',
      entryId,
      slotKey: 'Champ',
      groupId: null,
      versionToken: 'token-1',
    });
  });

  it('shows the server refusal and disables confirm', async () => {
    vi.mocked(previewCascadeReplace).mockResolvedValue({
      versionToken: 'token-1',
      playedMatches: [],
      replacements: [],
    });
    vi.mocked(confirmCascadeReplace).mockRejectedValue(
      new ApiError(
        409,
        'Completed phase blocks the cascade',
        undefined,
        'Application.CascadeReplaceCompleted',
      ),
    );
    const user = userEvent.setup();
    renderAction();

    expect(
      screen.queryByRole('button', { name: 'Remplacer Champ' }),
    ).toBeNull();
    await user.click(
      screen.getByRole('button', {
        name: "Périmé — Résultat amont modifié. Cliquer pour remplacer l'équipe par l'occupant attendu",
      }),
    );
    await user.click(await screen.findByRole('button', { name: 'Remplacer' }));

    expect(
      await screen.findByText(
        'Une phase terminée bloque le remplacement en cascade. (409)',
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remplacer' })).toBeDisabled();
  });
});
