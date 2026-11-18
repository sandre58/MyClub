import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import i18n from '../../../i18n';
import type { StructureStageHubSummary } from '../../../types';
import { StructurePhaseDrawHint } from './StructurePhaseDrawHint';

await i18n.changeLanguage('fr');

function stage(
  partial: Partial<StructureStageHubSummary>,
): StructureStageHubSummary {
  return {
    stageId: 's1',
    name: 'Phase',
    status: 'Draft',
    teamCount: 8,
    matchCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasDrawRules: true,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    ...partial,
  };
}

describe('StructurePhaseDrawHint full grid', () => {
  it('shows that a complete Groups grid does not require a draw', () => {
    const { rerender } = render(
      <StructurePhaseDrawHint
        stage={stage({ formatKind: 'Groups', drawExecutionBadge: null })}
      />,
    );
    expect(
      screen.getByText('Non requis — grille complète'),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Non requis — grille complète').parentElement,
    ).toHaveClass('structure-phase-draw-hint--done');

    rerender(
      <StructurePhaseDrawHint
        stage={stage({ formatKind: 'Groups', drawExecutionBadge: null })}
        variant="topology"
      />,
    );
    expect(
      screen.getByText('Non requis — grille complète').parentElement,
    ).toHaveClass('structure-topology__draw-signal--done');
  });

  it('leaves Cup on the launch copy when the badge is absent', () => {
    render(
      <StructurePhaseDrawHint
        stage={stage({ formatKind: 'Cup', drawExecutionBadge: null })}
      />,
    );
    expect(screen.getByText('Tirage à lancer')).toBeInTheDocument();
    expect(
      screen.queryByText('Non requis — grille complète'),
    ).not.toBeInTheDocument();
  });
});
