import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { StructureView } from '../../types';
import { StructureReadinessAttributedBlockerBanner } from './StructureReadinessAttributedBlockerBanner';

function viewWithCupBlocker(
  ownerStageId: string,
  otherStageId: string,
): StructureView {
  return {
    competitionId: 'comp',
    name: 'Test',
    status: 'Draft',
    participants: { activeCount: 0, occupyingCount: 0, entries: [] },
    format: {
      kind: 'Groups',
      primaryStageId: otherStageId,
      primaryStageName: 'Other',
      primaryStageStatus: 'Draft',
    },
    structure: {
      groupCount: 2,
      roundCount: 0,
      matchdayCount: 0,
      slotCount: 0,
      hasDrawRules: false,
      numberOfPots: null,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    regulation: {
      minimumTeams: 2,
      maximumTeams: 64,
      durationPerPeriod: 45,
      numberOfPeriods: 2,
      winPoints: 3,
      drawPoints: 1,
      lossPoints: 0,
    },
    readiness: {
      readyForNextSlice: false,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers: ['CupBracketInvalid'],
      blockerDetails: [
        { code: 'CupBracketInvalid', stageId: ownerStageId },
      ],
    },
    actions: [],
    stages: [],
  };
}

describe('StructureReadinessAttributedBlockerBanner', () => {
  it('shows diagnostic on owner stage only', () => {
    const owner = 'cup-owner';
    const groups = 'groups-primary';
    const data = viewWithCupBlocker(owner, groups);

    const { container: ownerMount } = render(
      <StructureReadinessAttributedBlockerBanner data={data} stageId={owner} />,
    );
    const ownerBanner = ownerMount.querySelector(
      '[data-testid="structure-readiness-attributed-blockers"]',
    );
    expect(ownerBanner).not.toBeNull();
    expect(ownerBanner?.textContent?.length).toBeGreaterThan(0);

    const { container: otherMount } = render(
      <StructureReadinessAttributedBlockerBanner data={data} stageId={groups} />,
    );
    expect(
      otherMount.querySelector('[data-testid="structure-readiness-attributed-blockers"]'),
    ).toBeNull();
  });
});
