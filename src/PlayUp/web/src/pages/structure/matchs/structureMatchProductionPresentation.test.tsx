import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureStageHubSummary } from '../../../types';
import { StructurePhaseOperationalStrip } from '../fiche/StructurePhaseOperationalStrip';
import { attachMatchProductionPresentation } from './structureMatchProductionPresentation.testFixtures';
import { readyMatchProductionHub } from './structureMatchProductionPresentation.testPayloads';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    materializeMatches: vi.fn(),
    materializeCupFromOccupiedSlots: vi.fn(),
    dematerializePhase: vi.fn(),
  };
});

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const baseStage: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'Cup',
  status: 'Ready',
  teamCount: 2,
  matchCount: 2,
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
  materialize: {
    commandCode: 'MaterializeFromOccupiedSlots',
    labelVariant: 'exhausted',
    determinedCount: 1,
    eligibleCount: 0,
    materializedCount: 1,
    commandEnabled: false,
    confrontations: [],
  },
};

function renderAside(stage: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructurePhaseOperationalStrip
          placement={null}
          showPlaces={false}
          showDraw={false}
          drawApplied={false}
          layoutVariant="aside-matchs"
          competitionId={competitionId}
          competitionStatus="Ready"
          stage={stage}
          structureStages={[stage]}
          presentation="terminal"
        />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('structureMatchProductionPresentation UI', () => {
  it('renders followUp when provided by the hub projection', () => {
    const stage = attachMatchProductionPresentation(
      baseStage,
      readyMatchProductionHub({
        primaryCurrent: 2,
        primaryCapacity: 2,
        primaryMessageCode: 'Complete',
        primaryMessageKind: 'success',
        materializeVisible: false,
        materializeEnabled: false,
        followUp: {
          messageCode: 'FollowUpExhausted',
          messageKind: 'info',
        },
      }),
    );
    renderAside(stage);
    expect(
      screen.getByText(/confrontations pourront apparaître ensuite/i),
    ).toBeInTheDocument();
  });

  it('shows dematerialize CTA disabled without legacy dematerialize DTO', () => {
    const stage = attachMatchProductionPresentation(
      { ...baseStage, dematerialize: undefined },
      readyMatchProductionHub({
        primaryCurrent: 2,
        primaryCapacity: 2,
        primaryMessageCode: 'Complete',
        primaryMessageKind: 'success',
        materializeVisible: false,
        materializeEnabled: false,
        dematerializeVisible: true,
        dematerializeEnabled: false,
        dematerializeDisabledReason: 'StructureNotMutable',
      }),
    );
    renderAside(stage);
    const btn = screen.getByRole('button', { name: /Dématérialiser|Retirer/i });
    expect(btn).toBeDisabled();
  });
});
