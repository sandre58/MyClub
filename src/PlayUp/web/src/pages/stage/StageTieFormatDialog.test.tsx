import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { applyStageTieFormatEdits } from '../../api';
import i18n from '../../i18n';
import enRegulation from '../../i18n/locales/en/regulation.json';
import enStructure from '../../i18n/locales/en/structure.json';
import frRegulation from '../../i18n/locales/fr/regulation.json';
import frStructure from '../../i18n/locales/fr/structure.json';
import type { StructureStageHubSummary } from '../../types';
import { TieFormatDialog } from './StageTieFormatDialog';

vi.mock('../../api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api')>();
  return {
    ...actual,
    applyStageTieFormatEdits: vi.fn().mockResolvedValue(undefined),
  };
});

const competitionId = 'comp-tie-dialog';

function cupStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: 'stage-cup',
    name: 'Phase KO',
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
    hasTieFormat: true,
    formatKind: 'Cup',
    actions: ['ReplaceDefaultTieFormat', 'ReplaceRoundTieFormat'],
    defaultTieFormat: {
      numberOfLegs: 2,
      aggregateScoring: true,
      hasAwayGoalsRule: false,
      hasTieExtraTime: false,
      hasTiePenaltyShootout: true,
    },
    confrontationSegments: [
      {
        rounds: [
          {
            roundId: 'qf',
            name: 'Quarts',
            sortOrder: 0,
            inheritsDefaultTieFormat: false,
          },
          {
            roundId: 'sf',
            name: 'Demis',
            sortOrder: 1,
            inheritsDefaultTieFormat: false,
          },
        ],
        numberOfLegs: 1,
        aggregateScoring: false,
        hasAwayGoalsRule: false,
        hasTieExtraTime: false,
        hasTiePenaltyShootout: true,
      },
    ],
    ...overrides,
  } as StructureStageHubSummary;
}

function renderDialog(stage: StructureStageHubSummary) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <TieFormatDialog
        competitionId={competitionId}
        stage={stage}
        structureStages={[stage]}
        open
        onClose={vi.fn()}
      />
    </QueryClientProvider>,
  );
}

function saveLabel(lng: 'fr' | 'en') {
  return lng === 'fr' ? frRegulation.editor.save : enRegulation.editor.save;
}

async function openRoundOverrideTab(
  user: ReturnType<typeof userEvent.setup>,
  roundName: string,
  lng: 'fr' | 'en',
) {
  await user.click(
    screen.getByRole('button', { name: new RegExp(roundName, 'i') }),
  );
  return screen.getByRole('switch', {
    name:
      lng === 'fr'
        ? frStructure.regulation.tieOverrideRound
        : enStructure.regulation.tieOverrideRound,
  });
}

describe.each(['fr', 'en'] as const)(
  'TieFormatDialog override revert (%s)',
  (lng) => {
    const copy = lng === 'fr' ? frStructure : enStructure;

    beforeEach(async () => {
      await i18n.changeLanguage(lng);
      vi.mocked(applyStageTieFormatEdits).mockClear();
    });

    afterEach(async () => {
      await i18n.changeLanguage('fr');
    });

    it('decocher un override différent active Enregistrer et envoie clear', async () => {
      const user = userEvent.setup();
      renderDialog(cupStage());
      expect(
        screen.getByRole('button', { name: saveLabel(lng) }),
      ).toBeDisabled();

      const switchControl = await openRoundOverrideTab(user, 'Quarts', lng);
      expect(switchControl).toBeChecked();
      await user.click(switchControl);
      expect(switchControl).not.toBeChecked();
      expect(
        screen.queryByLabelText(copy.regulation.tieNavCustom),
      ).not.toBeInTheDocument();
      expect(
        screen.getByRole('button', { name: saveLabel(lng) }),
      ).toBeEnabled();

      await user.click(screen.getByRole('button', { name: saveLabel(lng) }));

      expect(applyStageTieFormatEdits).toHaveBeenCalledTimes(1);
      const payload = vi.mocked(applyStageTieFormatEdits).mock.calls[0]![1];
      expect(payload.roundEdits).toEqual(
        expect.arrayContaining([
          expect.objectContaining({ roundId: 'qf', clear: true }),
        ]),
      );
    });

    it('decocher un override égal à la phase active Enregistrer et envoie clear', async () => {
      const user = userEvent.setup();
      const stage = cupStage({
        defaultTieFormat: {
          numberOfLegs: 2,
          aggregateScoring: true,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: true,
        },
        confrontationSegments: [
          {
            rounds: [
              {
                roundId: 'eq',
                name: 'Égal phase',
                sortOrder: 0,
                inheritsDefaultTieFormat: false,
              },
              {
                roundId: 'other',
                name: 'Autre',
                sortOrder: 1,
                inheritsDefaultTieFormat: true,
              },
            ],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: true,
          },
        ],
      });
      renderDialog(stage);

      const switchControl = await openRoundOverrideTab(user, 'Égal phase', lng);
      await user.click(switchControl);
      await user.click(screen.getByRole('button', { name: saveLabel(lng) }));

      const payload = vi.mocked(applyStageTieFormatEdits).mock.calls[0]![1];
      expect(payload.roundEdits).toEqual([
        expect.objectContaining({ roundId: 'eq', clear: true }),
      ]);
    });

    it('héritier au chargement ne produit pas clear après check puis uncheck', async () => {
      const user = userEvent.setup();
      const stage = cupStage({
        confrontationSegments: [
          {
            rounds: [
              {
                roundId: 'inh',
                name: 'Héritant',
                sortOrder: 0,
                inheritsDefaultTieFormat: true,
              },
              {
                roundId: 'other',
                name: 'Autre',
                sortOrder: 1,
                inheritsDefaultTieFormat: false,
              },
            ],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: true,
          },
        ],
      });
      renderDialog(stage);

      const switchControl = await openRoundOverrideTab(user, 'Héritant', lng);
      expect(switchControl).not.toBeChecked();
      await user.click(switchControl);
      await user.click(switchControl);
      expect(switchControl).not.toBeChecked();
      expect(
        screen.getByRole('button', { name: saveLabel(lng) }),
      ).toBeDisabled();
    });

    it('re-cocher restaure les valeurs baseline de l’override', async () => {
      const user = userEvent.setup();
      renderDialog(cupStage());
      await openRoundOverrideTab(user, 'Quarts', lng);
      const legsTwo = screen.getByRole('radio', {
        name: copy.regulation.legsTwo,
      });
      await user.click(legsTwo);
      const switchControl = screen.getByRole('switch', {
        name: copy.regulation.tieOverrideRound,
      });
      await user.click(switchControl);
      await user.click(switchControl);
      expect(switchControl).toBeChecked();
      const legsOne = screen.getByRole('radio', {
        name: copy.regulation.legsOne,
      });
      expect(legsOne).toBeChecked();
    });
  },
);
