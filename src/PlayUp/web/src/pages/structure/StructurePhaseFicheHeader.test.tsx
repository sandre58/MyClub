import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { StructurePhaseFicheHeader } from './StructurePhaseFicheHeader';
import type { StructureStageHubSummary } from '../../types';

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

describe('StructurePhaseFicheHeader', () => {
  it('hides rename affordance when canRename is false (Δ-O4-1 Completed)', () => {
    render(
      <StructurePhaseFicheHeader
        stage={baseStage}
        competitionId="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
        structureStages={[baseStage]}
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
      />,
    );

    expect(
      screen.queryByRole('button', { name: /Renommer la phase/i }),
    ).not.toBeInTheDocument();
  });
});
