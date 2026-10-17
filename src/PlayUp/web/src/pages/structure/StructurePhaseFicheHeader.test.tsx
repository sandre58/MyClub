import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { StructurePhaseFicheHeader } from './StructurePhaseFicheHeader';
import type { StructureStageHubSummary } from '../../types';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    startStage: vi.fn(),
    completeStage: vi.fn(),
    suspendStage: vi.fn(),
    resumeStage: vi.fn(),
    revertStageToReady: vi.fn(),
    revertStageToDraft: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const baseStage: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'Phase',
  status: 'Completed',
  teamCount: 8,
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

function renderHeader(stage: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructurePhaseFicheHeader
          stage={stage}
          competitionId={competitionId}
          structureStages={[stage]}
          formatLabel={null}
          editingName={false}
          nameDraft=""
          renameMutation={{
            isPending: false,
            isError: false,
            error: null,
            reset: vi.fn(),
          }}
          canRename={false}
          canRebuild={false}
          canRemove={false}
          compactIcon=""
          compactDangerIcon=""
          onNameDraftChange={vi.fn()}
          onBeginRename={vi.fn()}
          onCancelRename={vi.fn()}
          onCommitRename={vi.fn()}
          onOpenSkeleton={vi.fn()}
          onOpenRemove={vi.fn()}
        />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('StructurePhaseFicheHeader', () => {
  it('hides rename affordance when canRename is false (Δ-O4-1 Completed)', () => {
    renderHeader(baseStage);
    expect(
      screen.queryByRole('button', { name: /Renommer la phase/i }),
    ).not.toBeInTheDocument();
  });

  it('shows InitialRoundNotPlayable lifecycle copy and placement remediation in the header', () => {
    const remediationHref = `/competitions/${competitionId}/structure?stage=${baseStage.stageId}&focus=operational-placement&manual=1`;
    const stage: StructureStageHubSummary = {
      ...baseStage,
      status: 'Ready',
      formatKind: 'Cup',
      lifecycle: {
        transition: {
          code: 'StartStage',
          enabled: false,
          disabledReasonCode: 'InitialRoundNotPlayable',
          remediation: {
            href: remediationHref,
            labelKey: 'InitialRoundNotPlayable',
          },
        },
      },
    };

    renderHeader(stage);

    expect(
      screen.getByRole('button', { name: /Démarrer la phase/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Le premier tour n’est pas encore jouable/i),
    ).toBeInTheDocument();
    const remediationLink = screen.getByRole('link', {
      name: /Aller au placement opérationnel/i,
    });
    expect(remediationLink).toHaveAttribute('href', remediationHref);
    expect(remediationLink.getAttribute('href')).toContain('operational-placement');
    expect(remediationLink.getAttribute('href')).not.toContain('section=matchs');
  });
});
