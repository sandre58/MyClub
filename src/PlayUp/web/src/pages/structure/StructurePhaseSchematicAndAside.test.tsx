import { screen } from '@testing-library/react';
import { renderWithI18n } from '../../test/renderWithI18n';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import type { StructureStageHubSummary } from '../../types';
import { StructurePhaseSchematicAndAside } from './StructurePhaseSchematicAndAside';
import type { StructurePhaseHeroFact } from './StructurePhaseFicheHeader';

vi.mock('./PhaseSchematic', () => ({
  PhaseSchematic: () => null,
}));

vi.mock('./StructureSchematicViewport', () => ({
  StructureSchematicViewport: ({
    children,
  }: {
    children: (schematic: { cases: [] }) => React.ReactNode;
  }) => children({ cases: [] }),
}));

const stage: StructureStageHubSummary = {
  stageId: 's1',
  name: 'Phase',
  status: 'Draft',
  teamCount: 0,
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
  formatKind: 'Cup',
  roundCount: 2,
};

const heroFacts: StructurePhaseHeroFact[] = [
  {
    statKey: 'rounds',
    value: 2,
    icon: <span />,
    label: 'tours',
  },
];

function renderSchematic(
  overrides: Partial<{
    showHeroStats: boolean;
    canRebuild: boolean;
    onOpenSkeleton: () => void;
  }> = {},
) {
  const onOpenSkeleton = overrides.onOpenSkeleton ?? vi.fn();
  return renderWithI18n(
    <StructurePhaseSchematicAndAside
      showHeroStats={overrides.showHeroStats ?? true}
      heroFacts={heroFacts}
      canRebuild={overrides.canRebuild ?? false}
      onOpenSkeleton={onOpenSkeleton}
      schematicQuery={{
        data: undefined,
        isError: false,
        isFetching: false,
        isSuccess: false,
      }}
      stage={stage}
      hasExits={false}
      canManualPlace={false}
      onManualPlaceActivate={() => {}}
      schematicIssuesBanner={undefined}
      showAsideMatchsSection={false}
      structureHubBackgroundRefresh={false}
      placementOperationalProps={null}
      showPlacementBlock={false}
      showDrawCta={false}
      showActivateDrawCta={false}
      competitionId="c1"
      competitionStatus="Draft"
      structureStages={[stage]}
      materializePresentation={null}
      stageHasAppliedDraw={false}
      showOutRail={false}
      showSortiesRail={false}
      showAttributionRail={false}
      sortiesEditControl={null}
      exitsBanners={null}
      outboundGroups={[]}
      attributionEditControl={null}
      attributionBanners={null}
      hasAttribution={false}
      placementAwards={[]}
    />,
  );
}

describe('StructurePhaseSchematicAndAside hero chrome', () => {
  it('shows ghost edit forme beside stats when canRebuild', async () => {
    const onOpenSkeleton = vi.fn();
    const user = userEvent.setup();
    renderSchematic({ canRebuild: true, onOpenSkeleton });

    const button = screen.getByRole('button', {
      name: /Modifier l.organisation/i,
    });
    expect(button).toHaveClass('structure-phase-hero__edit-forme');
    await user.click(button);
    expect(onOpenSkeleton).toHaveBeenCalledOnce();
  });

  it('shows edit forme without stats when rebuild allowed and no hero stats', () => {
    renderSchematic({ showHeroStats: false, canRebuild: true });
    expect(
      screen.getByRole('button', { name: /Modifier l.organisation/i }),
    ).toBeInTheDocument();
    expect(screen.queryByRole('list')).not.toBeInTheDocument();
  });
});
