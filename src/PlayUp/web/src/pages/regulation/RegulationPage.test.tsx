import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { fetchStructureView, replaceCompetitionRegulation } from '../../api';
import { RegulationPage } from './RegulationPage';
import type {
  StructureStageDefaultsBinding,
  StructureStageHubSummary,
  StructureView,
} from '../../types';

vi.mock('../api', () => ({
  fetchStructureView: vi.fn(),
  replaceCompetitionRegulation: vi.fn(),
}));

const competitionId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const stageId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const stageFinaleId = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

function binding(parts: {
  matchDuration?: boolean;
  extraTime?: boolean;
  penaltyShootout?: boolean;
  administrativeResult?: boolean;
  points?: boolean | null;
  rankingCriteria?: boolean | null;
}): StructureStageDefaultsBinding {
  return {
    matchDuration: { isBound: parts.matchDuration ?? true },
    extraTime: { isBound: parts.extraTime ?? true },
    penaltyShootout: { isBound: parts.penaltyShootout ?? true },
    administrativeResult: { isBound: parts.administrativeResult ?? true },
    points: parts.points === null ? null : { isBound: parts.points ?? true },
    rankingCriteria:
      parts.rankingCriteria === null
        ? null
        : { isBound: parts.rankingCriteria ?? true },
  };
}

function groupesStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId,
    name: 'Groupes',
    status: 'Draft',
    teamCount: 8,
    matchCount: 12,
    groupCount: 2,
    roundCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: true,
    winPoints: 3,
    drawPoints: 1,
    lossPoints: 0,
    rankingCriteria: ['Points', 'Wins', 'GoalDifference'],
    hasDrawRules: true,
    drawMode: 'Random',
    numberOfPots: 4,
    hasQualificationRules: true,
    qualificationPathCount: 2,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    numberOfLegs: null,
    aggregateScoring: null,
    formatKind: 'Groups',
    defaultsBinding: binding({}),
    ...overrides,
  };
}

function finaleStage(
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary {
  return {
    stageId: stageFinaleId,
    name: 'Finale',
    status: 'Draft',
    teamCount: 2,
    matchCount: 0,
    groupCount: 0,
    roundCount: 1,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: true,
    extraTimeNumberOfPeriods: 2,
    extraTimeDurationPerPeriod: 15,
    hasPenaltyShootout: true,
    penaltyInitialKicksPerTeam: 5,
    hasStandingRules: false,
    winPoints: null,
    drawPoints: null,
    lossPoints: null,
    hasDrawRules: false,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: true,
    numberOfLegs: 2,
    aggregateScoring: true,
    hasAwayGoalsRule: true,
    hasTieExtraTime: true,
    hasTiePenaltyShootout: true,
    hasPlacementAwardRules: true,
    placementAwardCount: 2,
    placementAwards: [
      { rank: 1, outcome: 'Winner' },
      { rank: 2, outcome: 'Loser' },
    ],
    formatKind: 'Cup',
    defaultsBinding: binding({
      extraTime: false,
      penaltyShootout: false,
      points: null,
      rankingCriteria: null,
    }),
    ...overrides,
  };
}

function structureView(overrides: Partial<StructureView> = {}): StructureView {
  return {
    competitionId,
    name: 'Coupe',
    status: 'Draft',
    participants: {
      activeCount: 0,
      occupyingCount: 0,
      entries: [],
    },
    format: {
      kind: null,
      primaryStageId: null,
      primaryStageName: null,
      primaryStageStatus: null,
    },
    regulation: {
      minimumTeams: 8,
      maximumTeams: 16,
      durationPerPeriod: 45,
      numberOfPeriods: 2,
      winPoints: 3,
      drawPoints: 1,
      lossPoints: 0,
      allowedTypes: ['Yellow', 'Red'],
      halfTimeDuration: 15,
      hasExtraTime: false,
      hasPenaltyShootout: false,
      rankingCriteria: ['Points', 'GoalDifference', 'GoalsFor', 'HeadToHead'],
      forfeitWinnerGoals: 3,
      forfeitLoserGoals: 0,
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
    actions: ['ReplaceRegulation'],
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
    stages: [groupesStage(), finaleStage()],
    ...overrides,
  };
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[`/competitions/${competitionId}/regulation`]}
      >
        <Routes>
          <Route
            path="/competitions/:competitionId/regulation"
            element={<RegulationPage />}
          />
          <Route path="/stages/:stageId" element={<p>Stage</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('RegulationPage', () => {
  it('renders frame tiles, phases, and a single edit action', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Règlement' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(
      screen.getAllByRole('heading', { name: 'Match' }).length,
    ).toBeGreaterThanOrEqual(1);
    expect(
      screen.getAllByRole('heading', { name: 'Classement' }).length,
    ).toBeGreaterThanOrEqual(1);
    expect(
      screen.getByRole('heading', { name: 'Disciplinaire' }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('Équipes : 8 à 16')).toBeInTheDocument();
    expect(
      screen.getByLabelText(/Score administratif en cas de forfait/),
    ).toBeInTheDocument();
    expect(screen.getByText('Forfait')).toBeInTheDocument();
    expect(screen.getByText('Carton(s) autorisé(s)')).toBeInTheDocument();
    expect(screen.getByLabelText('Jaune')).toBeInTheDocument();
    expect(screen.getByLabelText('Rouge')).toBeInTheDocument();
    expect(screen.getByText('Barème de points')).toBeInTheDocument();
    expect(
      screen.getAllByText('Différence de buts').length,
    ).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('1re MT')).toBeInTheDocument();
    expect(screen.getByText('Pause')).toBeInTheDocument();
    expect(screen.queryByText('PR1')).not.toBeInTheDocument();
    expect(
      screen.getByLabelText(/Durée maximale du match 90 minutes/),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Modifier le règlement' }),
    ).toBeEnabled();
    expect(
      screen.getByRole('heading', { name: 'Groupes' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Finale' })).toBeInTheDocument();
    const finaleTile = screen
      .getByRole('heading', { name: 'Finale' })
      .closest('article');
    expect(finaleTile).not.toBeNull();
    expect(
      within(finaleTile!).queryByRole('heading', { name: 'Classement' }),
    ).not.toBeInTheDocument();
    expect(screen.getAllByText('Brouillon').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Aller-retour')).toBeInTheDocument();
    expect(screen.getByText('Cumul des scores')).toBeInTheDocument();
    expect(screen.getByText('Buts à l’extérieur')).toBeInTheDocument();
    expect(screen.getByText('Tirage aléatoire')).toBeInTheDocument();
    expect(screen.getByText('pots')).toBeInTheDocument();
    expect(screen.getAllByText('4').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Personnalisé')).toBeInTheDocument();
    expect(
      screen.queryByText('Diffère du règlement global'),
    ).not.toBeInTheDocument();
    expect(screen.queryByText('Personnalisation')).not.toBeInTheDocument();
    const overriddenTokens = document.querySelectorAll(
      '.regulation-rule-list__item--overridden',
    );
    expect(overriddenTokens.length).toBeGreaterThanOrEqual(2);
    expect(screen.getAllByText('2×45′').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/Prolongations/).length).toBeGreaterThanOrEqual(
      1,
    );
    expect(screen.getByText('· 2×15′')).toBeInTheDocument();
    expect(screen.getAllByText(/TAB/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('· 5 tirs')).toBeInTheDocument();
    expect(
      screen.getAllByRole('link', { name: 'Aller à la phase' }),
    ).toHaveLength(2);
  });

  it('renders an aggregate Confrontation line when rounds differ', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          groupesStage(),
          finaleStage({
            name: 'Phase finale',
            roundCount: 3,
            confrontationSegments: [
              {
                rounds: [
                  {
                    roundId: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
                    name: 'Finale',
                    sortOrder: 2,
                  },
                ],
                numberOfLegs: 1,
                aggregateScoring: false,
                hasAwayGoalsRule: false,
                hasTieExtraTime: false,
                hasTiePenaltyShootout: false,
              },
              {
                rounds: [
                  {
                    roundId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
                    name: 'Quarts de finale',
                    sortOrder: 0,
                  },
                  {
                    roundId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
                    name: 'Demis de finale',
                    sortOrder: 1,
                  },
                ],
                numberOfLegs: 2,
                aggregateScoring: true,
                hasAwayGoalsRule: true,
                hasTieExtraTime: true,
                hasTiePenaltyShootout: true,
              },
            ],
          }),
        ],
      }),
    );

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Phase finale' }),
    ).toBeInTheDocument();
    const phaseTile = screen
      .getByRole('heading', { name: 'Phase finale' })
      .closest('article');
    expect(phaseTile).not.toBeNull();
    expect(
      within(phaseTile!).getByText('Quarts de finale/Demis de finale'),
    ).toBeInTheDocument();
    expect(within(phaseTile!).getByText('Aller-retour')).toBeInTheDocument();
    expect(
      within(phaseTile!).getByText('Cumul des scores'),
    ).toBeInTheDocument();
    expect(
      within(phaseTile!).getByText('Buts à l’extérieur'),
    ).toBeInTheDocument();
    expect(
      within(phaseTile!).getByText('Finale', { exact: true }),
    ).toBeInTheDocument();
    expect(within(phaseTile!).getByText('Match unique')).toBeInTheDocument();
    const sectionTitles = phaseTile!.querySelectorAll(
      '.regulation-rule-section__title',
    );
    expect(sectionTitles[0]?.textContent).toBe(
      'Quarts de finale/Demis de finale',
    );
    expect(sectionTitles[1]?.textContent).toBe('Finale');
  });

  it('keeps Confrontation tokens when a single segment is homogeneous', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          groupesStage(),
          finaleStage({
            confrontationSegments: [
              {
                rounds: [
                  {
                    roundId: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
                    name: 'Finale',
                    sortOrder: 0,
                  },
                ],
                numberOfLegs: 2,
                aggregateScoring: true,
                hasAwayGoalsRule: true,
                hasTieExtraTime: true,
                hasTiePenaltyShootout: true,
              },
            ],
          }),
        ],
      }),
    );

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Finale' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Aller-retour')).toBeInTheDocument();
    expect(screen.getByText('Cumul des scores')).toBeInTheDocument();
    expect(screen.getByText('Buts à l’extérieur')).toBeInTheDocument();
  });

  it('shows a single exact capacity pill when min equals max', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        regulation: {
          ...structureView().regulation,
          minimumTeams: 8,
          maximumTeams: 8,
        },
      }),
    );

    renderPage();

    expect(
      await screen.findByLabelText('Effectif obligatoire : 8 équipes'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('La compétition exige exactement 8 équipes.'),
    ).toBeInTheDocument();
    expect(screen.queryByText('Min.')).not.toBeInTheDocument();
    expect(screen.queryByText('Max.')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Équipes : 8 à 8')).not.toBeInTheDocument();
  });

  it('shows a discipline empty state when no cards are allowed', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        regulation: {
          ...structureView().regulation,
          allowedTypes: [],
        },
      }),
    );

    renderPage();

    expect(
      await screen.findByText('Aucun carton autorisé'),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        'Aucun type de carton n’est autorisé pour cette compétition.',
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText('Carton(s) autorisé(s)')).not.toBeInTheDocument();
  });

  it('hides Classement when no classifying phase exists', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          finaleStage({
            matchCount: 1,
            numberOfLegs: 1,
            aggregateScoring: null,
          }),
        ],
      }),
    );

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Règlement' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: 'Classement' }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Disciplinaire' }),
    ).toBeInTheDocument();
  });

  it('opens the regulation editor from the page action', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
  });

  it('keeps the edit action visible but disabled when ReplaceRegulation is unavailable', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({ actions: [] }),
    );

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Règlement' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Modifier le règlement' }),
    ).toBeDisabled();
  });

  it('shows forfeit under phase Classement and draw seeds with constraints', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          groupesStage({
            hasExtraTime: true,
            extraTimeNumberOfPeriods: 2,
            extraTimeDurationPerPeriod: 15,
            hasPenaltyShootout: true,
            penaltyInitialKicksPerTeam: 5,
            rankingCriteria: [
              'Points',
              'GoalDifference',
              'GoalsFor',
              'HeadToHead',
            ],
            forfeitWinnerGoals: 3,
            forfeitLoserGoals: 0,
            numberOfSeeds: 2,
            drawConstraints: [
              {
                type: 'SameAssociationAvoidance',
                enforcement: 'Preferred',
              },
              {
                type: 'MaxSameAssociationPerGroup',
                enforcement: 'Required',
                maxPerGroup: 1,
              },
            ],
            hasQualificationRules: false,
            qualificationPathCount: 0,
          }),
        ],
        regulation: {
          ...structureView().regulation,
          hasExtraTime: true,
          extraTimeDurationPerPeriod: 15,
          extraTimeNumberOfPeriods: 2,
          hasPenaltyShootout: true,
          penaltyInitialKicksPerTeam: 5,
        },
      }),
    );

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Règlement' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByText('Diffère du règlement global'),
    ).not.toBeInTheDocument();
    expect(screen.queryByText('Personnalisé')).not.toBeInTheDocument();
    expect(screen.getAllByText('Forfait').length).toBeGreaterThanOrEqual(2);
    expect(
      screen.getByLabelText('Score administratif en cas de forfait : 3–0'),
    ).toBeInTheDocument();
    expect(screen.getByText('têtes de série')).toBeInTheDocument();
    expect(screen.getByText('Éviter la même association')).toBeInTheDocument();
    expect(screen.getByText(/Souhaité/)).toBeInTheDocument();
    expect(
      screen.getByText('Max. même association / groupe'),
    ).toBeInTheDocument();
    expect(screen.getByText(/Obligatoire/)).toBeInTheDocument();
    expect(screen.getByText(/max\. 1/)).toBeInTheDocument();
  });

  it('does not invent Swiss rounds when swissRoundCount is unknown', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          groupesStage({
            name: 'Suisse',
            teamCount: 16,
            matchCount: 0,
            groupCount: 0,
            hasExtraTime: true,
            extraTimeNumberOfPeriods: 2,
            extraTimeDurationPerPeriod: 15,
            hasPenaltyShootout: true,
            penaltyInitialKicksPerTeam: 5,
            rankingCriteria: [
              'Points',
              'GoalDifference',
              'GoalsFor',
              'HeadToHead',
            ],
            hasDrawRules: false,
            hasQualificationRules: false,
            qualificationPathCount: 0,
            formatKind: 'Swiss',
            swissRoundCount: null,
          }),
        ],
      }),
    );

    renderPage();

    expect(
      await screen.findByRole('heading', { name: 'Règlement' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Suisse' })).toBeInTheDocument();
    expect(screen.queryByLabelText(/Système suisse/)).not.toBeInTheDocument();
  });

  it('opens a sectioned editor without sync banner', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );

    const dialog = await screen.findByRole('dialog');
    expect(
      within(dialog).getByRole('heading', { name: 'Équipes' }),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByRole('heading', { name: 'Match' }),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByRole('heading', { name: 'Classement' }),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByRole('heading', { name: 'Disciplinaire' }),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByRole('heading', { name: 'Forfait' }),
    ).toBeInTheDocument();
    expect(within(dialog).getByText('Ordre de départage')).toBeInTheDocument();
    expect(
      within(dialog).queryByRole('heading', { name: 'Ordre de départage' }),
    ).not.toBeInTheDocument();
    expect(
      within(dialog).getByText('Carton(s) autorisé(s)'),
    ).toBeInTheDocument();
    expect(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    ).toBeDisabled();
    expect(
      within(dialog).getByLabelText(/Activer les prolongations/),
    ).not.toBeChecked();
  });

  it('shows personalized badge from DefaultsBinding even when values match the frame', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          groupesStage({
            defaultsBinding: binding({
              matchDuration: false,
              points: false,
              rankingCriteria: false,
            }),
          }),
        ],
      }),
    );

    renderPage();

    expect(await screen.findByText('Personnalisé')).toBeInTheDocument();
    expect(
      screen.getByText('Personnalisé').closest('.ds-tooltip-trigger') ??
        screen.getByText('Personnalisé'),
    ).toBeInTheDocument();
  });

  it('asks for a single impact confirm including Ready reopen copy', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({ status: 'Ready' }),
    );
    vi.mocked(replaceCompetitionRegulation).mockResolvedValue(
      undefined as never,
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    await user.click(
      within(dialog).getByLabelText(/Activer les prolongations/),
    );
    await user.click(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    );

    expect(
      await screen.findByRole('heading', {
        name: 'Enregistrer les modifications du règlement ?',
      }),
    ).toBeInTheDocument();
    expect(
      screen.getByText('La compétition repassera en Brouillon.'),
    ).toBeInTheDocument();
    const confirmHeading = await screen.findByRole('heading', {
      name: 'Enregistrer les modifications du règlement ?',
    });
    const confirmDialog = confirmHeading.closest('[role="dialog"]');
    expect(confirmDialog).not.toBeNull();
    expect(
      within(confirmDialog!).getByText('Mise à jour des phases'),
    ).toBeInTheDocument();
    expect(
      within(confirmDialog!).getByText(/Prolongations/),
    ).toBeInTheDocument();
    expect(
      within(confirmDialog!).getByText(/1 phase sera mise à jour/),
    ).toBeInTheDocument();
    expect(replaceCompetitionRegulation).not.toHaveBeenCalled();

    await user.click(
      within(confirmDialog!).getByRole('button', { name: 'Enregistrer' }),
    );

    await waitFor(() => {
      expect(replaceCompetitionRegulation).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({
          hasExtraTime: true,
          extraTimeDurationPerPeriod: 15,
          extraTimeNumberOfPeriods: 2,
        }),
      );
    });
  });

  it('projects duration inherit vs keep-override in the impact confirm', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());
    vi.mocked(replaceCompetitionRegulation).mockResolvedValue(
      undefined as never,
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    await user.click(
      within(dialog).getByLabelText(/Activer les prolongations/),
    );
    await user.click(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    );

    const confirmHeading = await screen.findByRole('heading', {
      name: 'Enregistrer les modifications du règlement ?',
    });
    const confirmDialog = confirmHeading.closest('[role="dialog"]');
    expect(confirmDialog).not.toBeNull();
    expect(
      within(confirmDialog!).getByText('Mise à jour des phases'),
    ).toBeInTheDocument();
    expect(
      within(confirmDialog!).getByText(/phase sera mise à jour/),
    ).toBeInTheDocument();
    expect(
      within(confirmDialog!).getByText(
        /phase personnalisée ne sera pas modifiée/i,
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Modifier la section Match' }),
    ).not.toBeInTheDocument();

    await user.click(
      within(confirmDialog!).getByRole('button', { name: 'Enregistrer' }),
    );
    await waitFor(() => {
      expect(replaceCompetitionRegulation).toHaveBeenCalled();
    });
  });

  it('asks to discard dirty edits when closing the editor', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    await user.click(
      within(dialog).getByLabelText(/Activer les prolongations/),
    );
    await user.click(within(dialog).getByRole('button', { name: 'Annuler' }));

    expect(
      await screen.findByRole('heading', {
        name: 'Quitter sans enregistrer ?',
      }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Abandonner' }));
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('keeps Enregistrer disabled when the form is untouched', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    expect(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    ).toBeDisabled();
    expect(
      screen.queryByRole('heading', {
        name: 'Enregistrer les modifications du règlement ?',
      }),
    ).not.toBeInTheDocument();
  });

  it('reports competition-only impact when only entry bounds change', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());
    vi.mocked(replaceCompetitionRegulation).mockResolvedValue(
      undefined as never,
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    const maxField = within(dialog).getByLabelText(/Maximum d’équipes/);
    const maxShell = maxField.closest('.ds-input-number');
    expect(maxShell).not.toBeNull();
    await user.click(
      within(maxShell as HTMLElement).getByRole('button', {
        name: 'Augmenter',
      }),
    );
    await user.click(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    );

    const confirmHeading = await screen.findByRole('heading', {
      name: 'Enregistrer les modifications du règlement ?',
    });
    const confirmDialog = confirmHeading.closest('[role="dialog"]');
    expect(confirmDialog).not.toBeNull();
    expect(
      within(confirmDialog!).getByText('Cadre de la compétition'),
    ).toBeInTheDocument();
    expect(
      within(confirmDialog!).getByText(
        /Mis à jour sur la compétition uniquement/,
      ),
    ).toBeInTheDocument();
  });

  it('reports no eligible stages when all phases are Running', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        stages: [
          groupesStage({ status: 'Running' }),
          finaleStage({ status: 'Running' }),
        ],
      }),
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    await user.click(
      within(dialog).getByLabelText(/Activer les prolongations/),
    );
    await user.click(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    );

    expect(
      await screen.findByText('Aucune phase ne sera mise à jour.'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Les phases déjà en cours ne seront pas modifiées.'),
    ).toBeInTheDocument();
  });

  it('shows soft-warns for unusual points and forfeit scores', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        regulation: {
          ...structureView().regulation,
          winPoints: 0,
          drawPoints: 1,
          lossPoints: 0,
          forfeitWinnerGoals: 0,
          forfeitLoserGoals: 1,
        },
      }),
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/Barème inhabituel/)).toBeInTheDocument();
    expect(
      within(dialog).getByText(/Score de forfait inhabituel/),
    ).toBeInTheDocument();
  });

  it('can save with no allowed cards', async () => {
    const user = userEvent.setup();
    vi.mocked(fetchStructureView).mockResolvedValue(structureView());
    vi.mocked(replaceCompetitionRegulation).mockResolvedValue(
      undefined as never,
    );

    renderPage();

    await user.click(
      await screen.findByRole('button', { name: 'Modifier le règlement' }),
    );
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('checkbox', { name: 'Jaune' }));
    await user.click(within(dialog).getByRole('checkbox', { name: 'Rouge' }));
    await user.click(
      within(dialog).getByRole('button', { name: 'Enregistrer' }),
    );
    const confirmHeading = await screen.findByRole('heading', {
      name: 'Enregistrer les modifications du règlement ?',
    });
    const confirmDialog = confirmHeading.closest('[role="dialog"]');
    expect(confirmDialog).not.toBeNull();
    await user.click(
      within(confirmDialog!).getByRole('button', { name: 'Enregistrer' }),
    );

    await waitFor(() => {
      expect(replaceCompetitionRegulation).toHaveBeenCalledWith(
        competitionId,
        expect.objectContaining({ allowedTypes: [] }),
      );
    });
  });

  it('renders one TAB kick pill per configured kick', async () => {
    vi.mocked(fetchStructureView).mockResolvedValue(
      structureView({
        regulation: {
          ...structureView().regulation,
          hasPenaltyShootout: true,
          penaltyInitialKicksPerTeam: 10,
        },
      }),
    );

    renderPage();

    expect(await screen.findByLabelText(/10 tirs au but/)).toBeInTheDocument();
    expect(document.querySelectorAll('.regulation-tab__kick')).toHaveLength(10);
  });
});
