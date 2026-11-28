import type { ComponentProps } from 'react';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import type { StructureStageHubSummary } from '../../../types';
import { StructurePhaseRegulationStrip } from './StructurePhaseRegulationStrip';

const baseStage = (
  overrides: Partial<StructureStageHubSummary> = {},
): StructureStageHubSummary => ({
  stageId: 'stage-1',
  name: 'Phase',
  status: 'Ready',
  teamCount: 8,
  matchCount: 0,
  numberOfPeriods: 2,
  durationPerPeriod: 45,
  halfTimeDuration: 15,
  hasExtraTime: false,
  hasPenaltyShootout: false,
  hasDrawRules: false,
  hasQualificationRules: false,
  qualificationPathCount: 0,
  hasProgressionRules: false,
  progressionPathCount: 0,
  hasTieFormat: true,
  winPoints: 3,
  drawPoints: 1,
  lossPoints: 0,
  rankingCriteria: ['Points'],
  ...overrides,
});

function renderStrip(
  props: Partial<ComponentProps<typeof StructurePhaseRegulationStrip>> = {},
) {
  const onEditMatch = vi.fn();
  const onEditStanding = vi.fn();
  const onEditConfrontation = vi.fn();
  render(
    <MemoryRouter>
      <StructurePhaseRegulationStrip
        stage={baseStage()}
        regulationHref="/competitions/c1/regulation"
        matchBound={true}
        standingBound={true}
        showStanding={true}
        showConfrontation={true}
        canEditMatch={true}
        canRebind={false}
        canEditStanding={true}
        canEditTie={true}
        onEditMatch={onEditMatch}
        onEditStanding={onEditStanding}
        onEditConfrontation={onEditConfrontation}
        {...props}
      />
    </MemoryRouter>,
  );
  return { onEditMatch, onEditStanding, onEditConfrontation };
}

describe('StructurePhaseRegulationStrip', () => {
  it('renders three regulation panels with section headings and open-regulation link', () => {
    renderStrip();

    expect(
      screen.getAllByText('Règles héritées du règlement général').length,
    ).toBeGreaterThanOrEqual(1);
    expect(screen.getByRole('heading', { name: 'Match' })).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Classement' }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { name: 'Format de confrontation' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Règlement' })).toHaveAttribute(
      'href',
      '/competitions/c1/regulation',
    );
  });

  it('invokes match edit from the match panel toolbar', async () => {
    const user = userEvent.setup();
    const { onEditMatch } = renderStrip();

    const matchPanel = document.getElementById('tile-match')!;
    await user.click(
      within(matchPanel).getByRole('button', { name: /Modifier/i }),
    );
    expect(onEditMatch).toHaveBeenCalledTimes(1);
  });

  it('shows confrontation format scope hint under Format de confrontation', () => {
    renderStrip({
      stage: baseStage({
        defaultTieFormat: {
          numberOfLegs: 2,
          aggregateScoring: true,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        },
        confrontationSegments: [
          {
            rounds: [
              {
                roundId: 'a',
                name: 'Finale',
                sortOrder: 1,
                inheritsDefaultTieFormat: true,
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
                roundId: 'b',
                name: 'Demi',
                sortOrder: 0,
                inheritsDefaultTieFormat: false,
              },
            ],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        ],
      }),
    });

    expect(
      screen.getByText('Certains tours ont leur propre format.'),
    ).toBeInTheDocument();
  });

  it('renders confrontation digest in main style without colon separators', () => {
    renderStrip({
      stage: baseStage({
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
                roundId: 'f',
                name: 'Finale',
                sortOrder: 0,
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
      }),
    });

    const confrontationPanel = document.getElementById('tile-confrontation')!;
    const panelText = confrontationPanel.textContent ?? '';
    expect(panelText).toContain('Aller-retour');
    expect(panelText).toContain('TAB');
    expect(panelText).toContain('Match unique');
    expect(panelText).not.toMatch(/Phase\s*:/);
    expect(panelText).not.toContain('·');
    expect(panelText).not.toContain('+');

    const rows = confrontationPanel.querySelectorAll(
      '.structure-regulation-digest__fact--row',
    );
    expect(rows.length).toBeGreaterThanOrEqual(2);

    const finaleRow = within(confrontationPanel)
      .getByText('Finale', { exact: true })
      .closest('.structure-regulation-digest__fact--row') as HTMLElement;
    expect(finaleRow.textContent).toContain('Match unique');
    expect(finaleRow.textContent).not.toContain('Aller-retour');
  });

  it('shows sans TAB on a round row when phase has shootout and round does not', () => {
    renderStrip({
      stage: baseStage({
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
                roundId: 'f',
                name: 'Finale',
                sortOrder: 0,
                inheritsDefaultTieFormat: false,
              },
            ],
            numberOfLegs: 1,
            aggregateScoring: false,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        ],
      }),
    });

    const confrontationPanel = document.getElementById('tile-confrontation')!;
    const finaleRow = within(confrontationPanel)
      .getByText('Finale', { exact: true })
      .closest('.structure-regulation-digest__fact--row') as HTMLElement;
    expect(finaleRow.textContent).toMatch(/sans TAB/);
  });

  it('keeps uniform confrontation hint when stored overrides match phase signature', () => {
    renderStrip({
      stage: baseStage({
        defaultTieFormat: {
          numberOfLegs: 2,
          aggregateScoring: true,
          hasAwayGoalsRule: false,
          hasTieExtraTime: false,
          hasTiePenaltyShootout: false,
        },
        confrontationSegments: [
          {
            rounds: [
              {
                roundId: 'a',
                name: 'Finale',
                sortOrder: 0,
                inheritsDefaultTieFormat: false,
              },
            ],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
          {
            rounds: [
              {
                roundId: 'b',
                name: 'Demi',
                sortOrder: 1,
                inheritsDefaultTieFormat: true,
              },
            ],
            numberOfLegs: 2,
            aggregateScoring: true,
            hasAwayGoalsRule: false,
            hasTieExtraTime: false,
            hasTiePenaltyShootout: false,
          },
        ],
      }),
    });

    expect(
      screen.getByText('Même format sur tous les tours.'),
    ).toBeInTheDocument();
    expect(
      screen.queryByText('Certains tours ont leur propre format.'),
    ).not.toBeInTheDocument();
    const confrontationPanel = document.getElementById('tile-confrontation')!;
    expect(
      within(confrontationPanel).getByText('Aller-retour'),
    ).toBeInTheDocument();
    expect(
      within(confrontationPanel).queryByText('Finale', { exact: true }),
    ).not.toBeInTheDocument();
  });

  it('shows phase-specific binding hint when match rules are not frame-bound', () => {
    renderStrip({ matchBound: false });

    expect(
      screen.getByText('Règles adaptées à cette phase'),
    ).toBeInTheDocument();
  });

  it('omits standing panel when showStanding is false', () => {
    renderStrip({ showStanding: false, standingBound: null });

    expect(
      screen.queryByRole('heading', { name: /Classement/i }),
    ).not.toBeInTheDocument();
    expect(document.getElementById('tile-match')).toBeInTheDocument();
  });

  it('hides standing edit when ReplaceStandingRules is not on hub actions', () => {
    renderStrip({ canEditStanding: false, canRebind: false });

    const standingPanel = document.getElementById('tile-standing')!;
    expect(
      within(standingPanel).queryByRole('button', { name: /Modifier/i }),
    ).not.toBeInTheDocument();
  });
});
