import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchStageOverview } from '../../../api';
import i18n from '../../../i18n';
import enStructure from '../../../i18n/locales/en/structure.json';
import frStructure from '../../../i18n/locales/fr/structure.json';
import { structureReadiness } from '../../../test/structureTestFixtures';
import type {
  StageOverview,
  StructureStageHubSummary,
  StructureView,
} from '../../../types';
import { StructureDrawDialog } from '../draw/StructureDrawDialog';
import {
  MatchRulesDialog,
  StandingRulesDialog,
} from '../../stage/StageMatchStandingDialogs';
import { TieFormatDialog } from '../../stage/StageTieFormatDialog';
import { DrawRulesDialog } from './StructureRegulationDialogs';

vi.mock('../../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api')>();
  return {
    ...actual,
    fetchStageOverview: vi.fn(),
  };
});

const phaseName = 'Demi-finales';
const stageId = 'stage-headers';
const competitionId = 'comp-headers';

const stage = {
  stageId,
  name: phaseName,
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
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: false,
  formatKind: 'Groups',
  placesPerGroup: 4,
} as StructureStageHubSummary;

const data = {
  competitionId,
  name: 'Coupe',
  status: 'Draft',
  participants: { activeCount: 0, occupyingCount: 0, entries: [] },
  format: {
    kind: 'Groups',
    primaryStageId: stageId,
    primaryStageName: phaseName,
    primaryStageStatus: 'Draft',
  },
  regulation: {
    minimumTeams: 2,
    maximumTeams: 32,
    durationPerPeriod: 45,
    numberOfPeriods: 2,
    winPoints: 3,
    drawPoints: 1,
    lossPoints: 0,
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
  actions: [],
  readiness: structureReadiness(),
  stages: [stage],
} as StructureView;

function overview(): StageOverview {
  return {
    id: stageId,
    competitionId,
    name: phaseName,
    status: 'Draft',
    slots: [],
    rounds: [],
    draws: [],
    bracketPairs: [],
  };
}

function renderDialog(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>,
  );
}

function withPhase(template: string) {
  return template.replaceAll('{{phase}}', phaseName);
}

function expectHeader(title: string, hint: string) {
  const dialog = screen.getByRole('dialog', { name: title });
  const titleId = dialog.getAttribute('aria-labelledby');
  const hintId = dialog.getAttribute('aria-describedby');
  expect(titleId).toBeTruthy();
  expect(hintId).toBeTruthy();
  const titleNode = document.getElementById(titleId!);
  const hintNode = document.getElementById(hintId!);
  expect(titleNode?.textContent).toBe(title);
  expect(titleNode?.textContent).toContain(`— ${phaseName}`);
  expect(hintNode?.textContent).toBe(hint);
  expect(hintNode?.textContent).not.toContain(phaseName);
}

const catalogs = { fr: frStructure, en: enStructure } as const;

describe.each(['fr', 'en'] as const)(
  'regulation dialog headers (%s)',
  (lng) => {
    const copy = catalogs[lng];

    beforeEach(async () => {
      await i18n.changeLanguage(lng);
      vi.mocked(fetchStageOverview).mockResolvedValue(overview());
    });

    afterEach(async () => {
      await i18n.changeLanguage('fr');
    });

    it('puts the phase in the match rules title and a hint under it', () => {
      renderDialog(
        <MatchRulesDialog data={data} stage={stage} open onClose={vi.fn()} />,
      );

      expectHeader(
        withPhase(copy.regulation.matchTitle),
        copy.regulation.matchHeaderHint,
      );
    });

    it('puts the phase in the standing rules title and a hint under it', () => {
      renderDialog(
        <StandingRulesDialog
          data={data}
          stage={stage}
          open
          onClose={vi.fn()}
        />,
      );

      expectHeader(
        withPhase(copy.regulation.standingTitle),
        copy.regulation.standingHeaderHint,
      );
    });

    it('puts the phase in the tie format title and a hint under it', () => {
      renderDialog(
        <TieFormatDialog
          competitionId={competitionId}
          stage={stage}
          structureStages={[stage]}
          open
          onClose={vi.fn()}
        />,
      );

      expectHeader(
        withPhase(copy.regulation.tieTitle),
        copy.regulation.tieHeaderHint,
      );
    });

    it('puts the phase in the draw settings title and a hint under it', () => {
      renderDialog(
        <DrawRulesDialog
          competitionId={competitionId}
          stage={stage}
          structureStages={[stage]}
          intent="params"
          open
          onClose={vi.fn()}
        />,
      );

      expectHeader(
        withPhase(copy.regulation.drawParamsTitle),
        copy.regulation.drawParamsHeaderHint,
      );
    });

    it('puts the phase in the activate-draw title and a hint under it', () => {
      renderDialog(
        <DrawRulesDialog
          competitionId={competitionId}
          stage={stage}
          structureStages={[stage]}
          intent="activate"
          open
          onClose={vi.fn()}
        />,
      );

      expectHeader(
        withPhase(copy.regulation.drawActivateTitle),
        copy.regulation.drawActivateHeaderHint,
      );
    });

    it('puts the phase in the draw executions title and a hint under it', async () => {
      renderDialog(
        <StructureDrawDialog
          open
          onClose={vi.fn()}
          competitionId={competitionId}
          competitionStatus="Draft"
          stage={stage}
          structureStages={[stage]}
        />,
      );

      expect(
        await screen.findByRole('heading', {
          name: withPhase(copy.fiche.drawWorkflow.title),
        }),
      ).toBeInTheDocument();
      expectHeader(
        withPhase(copy.fiche.drawWorkflow.title),
        copy.regulation.drawExecHeaderHint,
      );
    });
  },
);
