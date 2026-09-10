import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, vi } from 'vitest';
import { fetchStructureView } from '../api';
import { CompetitionOverviewPage } from './CompetitionOverviewPage';
import {
  overviewIds,
  overviewSituation,
  overviewView,
  referenceStageGameRules,
} from '../test/overviewFixtures';
import type { OverviewView, StructureView } from '../types';

export {
  overviewIds,
  overviewSituation,
  overviewView,
  referenceStageGameRules,
};

export const { competitionId, stageId } = overviewIds;

export function renderOverviewPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/competitions/${competitionId}`]}>
        <Routes>
          <Route
            path="/competitions/:competitionId"
            element={<CompetitionOverviewPage />}
          />
          <Route
            path="/competitions/:competitionId/teams"
            element={<p>Teams route</p>}
          />
          <Route
            path="/competitions/:competitionId/structure"
            element={<p>Structure route</p>}
          />
          <Route
            path="/competitions/:competitionId/matches"
            element={<p>Match hub route</p>}
          />
          <Route
            path="/competitions/:competitionId/classements"
            element={<p>Classements route</p>}
          />
          <Route path="/stages/:stageId" element={<p>Stage route</p>} />
          <Route path="/matches/:matchId" element={<p>Match route</p>} />
          <Route path="/competitions" element={<p>List route</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

export function inProgressLayoutBase(
  overrides: Partial<OverviewView> = {},
): OverviewView {
  return overviewView({
    status: 'Running',
    cycleReading: { code: 'InProgress' },
    constructionDimensions: {
      ...overviewView().constructionDimensions,
      teams: {
        prominence: 'Condensed',
        facts: { activeCount: '4', minimumTeams: '2', maximumTeams: '64' },
      },
      structure: {
        prominence: 'Condensed',
        facts: {
          formatKind: 'Championship',
          groupCount: '0',
          roundCount: '0',
          matchdayCount: '34',
          slotCount: '0',
        },
      },
      regulation: {
        ...overviewView().constructionDimensions.regulation,
        prominence: 'Condensed',
      },
      matches: {
        prominence: 'Dominant',
        facts: { live: '0', scheduled: '0', finished: '0', total: '0' },
      },
    },
    operationalFocus: {
      ...overviewView().operationalFocus,
      referenceStageGameRules: referenceStageGameRules(),
    },
    naturalProgression: null,
    availableActions: [],
    attentionSummary: { count: 0, items: [] },
    ...overrides,
  });
}

export function expectOverviewRegionOrder(...regionTestIds: string[]) {
  const overview = document.querySelector('.overview');
  expect(overview).not.toBeNull();
  const elements = regionTestIds.map((id) =>
    overview!.querySelector(`[data-testid="${id}"]`),
  );
  for (const el of elements) {
    expect(el).not.toBeNull();
  }
  for (let i = 0; i < elements.length - 1; i++) {
    const relation = elements[i]!.compareDocumentPosition(elements[i + 1]!);
    expect(relation & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  }
}

export function expectOverviewRegionsAbsent(...regionTestIds: string[]) {
  const overview = document.querySelector('.overview');
  expect(overview).not.toBeNull();
  for (const id of regionTestIds) {
    expect(overview!.querySelector(`[data-testid="${id}"]`)).toBeNull();
  }
}

export function defaultOrgView(): StructureView {
  return {
    competitionId,
    name: 'Spring Cup',
    status: 'Draft',
    participants: {
      activeCount: 1,
      occupyingCount: 1,
      entries: [
        {
          entryId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
          displayName: 'FC Test',
          status: 'Active',
        },
      ],
    },
    format: {
      kind: null,
      primaryStageId: null,
      primaryStageName: null,
      primaryStageStatus: null,
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
    structure: {
      groupCount: 0,
      roundCount: 0,
      matchdayCount: 0,
      slotCount: 0,
      hasDrawRules: false,
      numberOfPots: null,
      matchGenerationFormat: 'SingleRoundRobin',
    },
    actions: [],
    readiness: {
      readyForNextSlice: false,
      readyForDraw: false,
      readyForMaterialization: false,
      readyForSchedule: false,
      readyForMatchOperation: false,
      readyForSchedulePath: false,
      attachedMatchCount: 0,
      blockers: [],
    },
    stages: [],
  };
}

export function setupDefaultStructureMock() {
  vi.mocked(fetchStructureView).mockResolvedValue(defaultOrgView());
}
