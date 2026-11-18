import {
  QueryClient,
  QueryClientProvider,
  type UseMutationResult,
} from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import i18n from '../../../i18n';
import type { StructureStageHubSummary } from '../../../types';
import { StructurePhaseFicheHeader } from './StructurePhaseFicheHeader';

await i18n.changeLanguage('fr');

const stage: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'Phase de groupes',
  status: 'Draft',
  teamCount: 8,
  matchCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: true,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
  formatKind: 'Groups',
  drawExecutionBadge: null,
};

function renderHeader(next: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructurePhaseFicheHeader
          stage={next}
          competitionId="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
          structureStages={[next]}
          formatLabel={null}
          editingName={false}
          nameDraft=""
          renameMutation={
            {
              isPending: false,
              isError: false,
              error: null,
              reset: vi.fn(),
            } as unknown as UseMutationResult<void, Error, string, unknown>
          }
          canRename={false}
          compactIcon=""
          onNameDraftChange={vi.fn()}
          onBeginRename={vi.fn()}
          onCancelRename={vi.fn()}
          onCommitRename={vi.fn()}
        />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('StructurePhaseFicheHeader group draw hint', () => {
  it('shows that a complete Groups grid does not require a draw', () => {
    renderHeader(stage);
    expect(
      screen.getByText('Non requis — grille complète'),
    ).toBeInTheDocument();
  });

  it('does not repeat the launch badge while a draw is still to launch', () => {
    renderHeader({ ...stage, drawExecutionBadge: 'ToLaunch' });
    expect(screen.queryByText('Tirage à lancer')).not.toBeInTheDocument();
  });
});
