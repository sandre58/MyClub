import {
  QueryClient,
  QueryClientProvider,
  type UseMutationResult,
} from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { StructurePhaseFicheHeader } from './StructurePhaseFicheHeader';
import type { StructureStageHubSummary } from '../../../types';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
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

describe('StructurePhaseFicheHeader', () => {
  it('does not show edit forme in header (B2 F2 — hero chrome only)', () => {
    renderHeader({ ...baseStage, status: 'Draft' } as StructureStageHubSummary);
    expect(
      screen.queryByRole('button', { name: /Modifier la structure/i }),
    ).not.toBeInTheDocument();
  });

  it('hides rename affordance when canRename is false (Completed)', () => {
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
    expect(
      container.querySelector(
        '.structure-fiche__header-row--lifecycle .ds-status',
      ),
    ).toBeNull();
    expect(
      container.querySelector('.structure-fiche__header-row-end .ds-status'),
    ).toBeTruthy();
  });

  it('shows status on the identity row when lifecycle is absent', () => {
    const { container } = renderHeader(baseStage);
    expect(
      container.querySelector('.structure-fiche__header-row--lifecycle'),
    ).toBeNull();
    expect(screen.getByText(/Terminé/i)).toBeInTheDocument();
    expect(
      container.querySelector('.structure-fiche__header-row-end .ds-status'),
    ).toBeTruthy();
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

  it('shows no header remediation link for ConstructionFrozen', () => {
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
      screen.queryByRole('link', { name: /Voir la synthèse/i }),
    ).not.toBeInTheDocument();
  });

  it('shows no header remediation link for InitialRoundNotPlayable', () => {
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
    expect(
      screen.queryByRole('link', {
        name: /Aller au placement opérationnel/i,
      }),
    ).not.toBeInTheDocument();
  });

  it('hides placement remediation link when Start is blocked for cup entry pairs in Matchs tile', () => {
    const remediationHref = `/competitions/${competitionId}/structure?stage=${baseStage.stageId}&focus=operational-placement&manual=1`;
    const stage: StructureStageHubSummary = {
      ...baseStage,
      status: 'Ready',
      formatKind: 'Cup',
      lifecycle: {
        transition: {
          code: 'StartStage',
          enabled: false,
          disabledReasonCode: 'NoDeterminedConfrontations',
          remediation: {
            href: remediationHref,
            labelKey: 'NoDeterminedConfrontations',
          },
        },
      },
    };

    renderHeader(stage);

    expect(
      screen.queryByRole('link', { name: /Aller au placement opérationnel/i }),
    ).not.toBeInTheDocument();
  });
});
