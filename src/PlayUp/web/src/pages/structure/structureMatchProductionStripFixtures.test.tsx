import type { ComponentProps } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';

import type {
  StructureMatchProductionPresentation,
  StructureStageHubSummary,
} from '../../types';
import runningGroupsPartialFixture from './__fixtures__/matchProduction/running-groups-partial.json';
import { StructurePhaseOperationalStrip } from './StructurePhaseOperationalStrip';

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const groupsStageBase: StructureStageHubSummary = {
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  name: 'Poules',
  status: 'Running',
  teamCount: 4,
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
};

function renderAsideMatchsStrip(stage: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const props: ComponentProps<typeof StructurePhaseOperationalStrip> = {
    placement: null,
    showPlaces: false,
    showDraw: false,
    drawApplied: false,
    structureHubBackgroundRefresh: false,
    layoutVariant: 'aside-matchs',
    competitionId,
    competitionStatus: 'Running',
    stage,
    structureStages: [stage],
    presentation: null,
  };

  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <StructurePhaseOperationalStrip {...props} />
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe('match production strip (assembler fixtures)', () => {
  it('running groups partial: frozenPartial 1/2, no primary gauge ratio row, no CTAs', () => {
    const presentation =
      runningGroupsPartialFixture as StructureMatchProductionPresentation;
    renderAsideMatchsStrip({
      ...groupsStageBase,
      matchProductionPresentation: presentation,
    });

    const section = document.getElementById('operational-matchs')!;
    expect(
      section.querySelector('.structure-operational-strip__ratio-filled'),
    ).toHaveTextContent('1');
    expect(
      within(section).getByText(
        /production figée tant que la phase est en cours/i,
      ),
    ).toBeInTheDocument();
    expect(
      within(section).queryByRole('button', { name: /Créer les matchs/i }),
    ).not.toBeInTheDocument();
    expect(
      within(section).queryByRole('button', { name: /Retirer les matchs/i }),
    ).not.toBeInTheDocument();
    expect(
      within(section).queryByText(/Aucune confrontation à afficher/i),
    ).not.toBeInTheDocument();
    expect(
      within(section).queryByText(/ne permet plus de créer les matchs/i),
    ).not.toBeInTheDocument();
  });

  it('running exploitation-only: no legacy materialize empty copy when primary gauge is hidden', () => {
    const presentation =
      runningGroupsPartialFixture as StructureMatchProductionPresentation;
    renderAsideMatchsStrip({
      ...groupsStageBase,
      matchCount: 2,
      materialize: {
        commandCode: 'MaterializeMatches',
        labelVariant: 'exhausted',
        determinedCount: 2,
        eligibleCount: 0,
        materializedCount: 2,
        commandEnabled: false,
        disabledReason: 'StructureNotMutable',
        confrontations: [],
      },
      matchProductionPresentation: {
        ...presentation,
        frozenPartial: null,
        showPrimaryGauge: false,
        primaryGauge: {
          ...presentation.primaryGauge,
          current: 2,
          capacity: 2,
          messageCode: 'Complete',
          messageKind: 'success',
        },
      },
    });

    const section = document.getElementById('operational-matchs')!;
    expect(
      within(section).queryByText(/Aucune confrontation à afficher/i),
    ).not.toBeInTheDocument();
    expect(
      within(section).queryByText(/ne permet plus de créer les matchs/i),
    ).not.toBeInTheDocument();
  });
});
