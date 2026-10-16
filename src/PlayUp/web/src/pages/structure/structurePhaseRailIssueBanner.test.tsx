import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import type { StructureStageHubSummary, StructureView } from '../../types';
import { StructurePhaseRailIssueBanner } from './structurePhaseRailIssueBanner';

const stage: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'Phase',
  status: 'Draft',
  teamCount: 4,
  matchCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: false,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  progressionPathCount: 0,
  formatKind: 'Cup',
};

const data = {
  competitionId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  status: 'Draft',
  format: { primaryStageId: stage.stageId },
  stages: [stage],
  participants: { entries: [] },
} as StructureView;

describe('StructurePhaseRailIssueBanner', () => {
  it('renders nothing when codes are empty', () => {
    const { container } = render(
      <StructurePhaseRailIssueBanner codes={[]} data={data} stage={stage} />,
    );
    expect(container.firstChild).toBeNull();
  });

  it('renders a status row per diagnostic code', () => {
    render(
      <StructurePhaseRailIssueBanner
        codes={['PopulationShortfall']}
        data={data}
        stage={stage}
      />,
    );
    expect(screen.getByRole('list')).toBeTruthy();
    expect(screen.getByRole('status')).toBeTruthy();
  });

  it('requests paused issue copy when stage is Suspended', () => {
    const suspendedStage = { ...stage, status: 'Suspended' as const };
    render(
      <StructurePhaseRailIssueBanner
        codes={['PopulationShortfall']}
        data={{ ...data, stages: [suspendedStage] }}
        stage={suspendedStage}
      />,
    );
    expect(
      screen.getByText(/constat en pause/i),
    ).toBeTruthy();
  });
});
