import {
  QueryClient,
  QueryClientProvider,
  type UseMutationResult,
} from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { StructurePhaseFicheHeader } from './StructurePhaseFicheHeader';
import type { StructureStageHubSummary } from '../../types';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    prepareStage: vi.fn(),
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

function renderHeader(
  stage: StructureStageHubSummary,
  options?: { editingName?: boolean },
) {
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
          editingName={options?.editingName ?? false}
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

  it('uses two structural rows when lifecycle is shown', () => {
    const stage: StructureStageHubSummary = {
      ...baseStage,
      status: 'Draft',
      lifecycle: {
        transition: { code: 'PrepareStage', enabled: true },
      },
    };
    const { container } = renderHeader(stage);
    expect(
      container.querySelector('.structure-fiche__header-row--identity'),
    ).toBeTruthy();
    expect(
      container.querySelector('.structure-fiche__header-row--lifecycle'),
    ).toBeTruthy();
    expect(
      screen.getByRole('button', { name: /Marquer comme prête/i }),
    ).toBeInTheDocument();
  });

  it('shows Completed badge on identity row without lifecycle row', () => {
    const { container } = renderHeader(baseStage);
    expect(
      container.querySelector('.structure-fiche__header-row--lifecycle'),
    ).toBeNull();
    expect(screen.getByText(/Terminé/i)).toBeInTheDocument();
  });

  it('hides lifecycle row while renaming', () => {
    const stage: StructureStageHubSummary = {
      ...baseStage,
      status: 'Draft',
      lifecycle: {
        transition: { code: 'PrepareStage', enabled: true },
      },
    };
    const { container } = renderHeader(stage, { editingName: true });
    expect(
      container.querySelector('.structure-fiche__header-row--lifecycle'),
    ).toBeNull();
    expect(
      screen.queryByRole('button', { name: /Marquer comme prête/i }),
    ).not.toBeInTheDocument();
  });

  it('shows overview remediation link without block paragraph for ConstructionFrozen', () => {
    const stage: StructureStageHubSummary = {
      ...baseStage,
      status: 'Draft',
      lifecycle: {
        transition: {
          code: 'PrepareStage',
          enabled: false,
          disabledReasonCode: 'ConstructionFrozen',
        },
      },
    };

    renderHeader(stage);

    expect(
      screen.getByRole('button', { name: /Marquer comme prête/i }),
    ).toBeDisabled();
    expect(
      screen.queryByText(/Complétez la structure de cette phase/i),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: /Voir la synthèse/i }),
    ).toHaveAttribute('href', `/competitions/${competitionId}/overview`);
  });

  it('shows placement remediation link without long block copy for InitialRoundNotPlayable', () => {
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
      screen.queryByText(/Le premier tour n’est pas encore jouable/i),
    ).not.toBeInTheDocument();
    const remediationLink = screen.getByRole('link', {
      name: /Aller au placement opérationnel/i,
    });
    expect(remediationLink).toHaveAttribute('href', remediationHref);
  });
});
