import type { ComponentProps } from 'react';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import type { StageDraw, StructureStageHubSummary } from '../../../types';
import { drawActions, hiddenDrawActions } from '../../../test/withDrawActions';
import { renderWithI18n } from '../../../test/renderWithI18n';
import { DRAW_RELEASE_ATTACHED_MATCHES } from '../draw/drawActions';
import { StructureStaleDrawResolutionActions } from './StructureStaleDrawResolutionActions';

const structureStages: readonly StructureStageHubSummary[] = [
  {
    stageId: '11111111-1111-1111-1111-111111111111',
    name: 'Cup',
    status: 'Draft',
    teamCount: 4,
    matchCount: 0,
    groupCount: 0,
    roundCount: 0,
    numberOfPeriods: 2,
    durationPerPeriod: 45,
    hasExtraTime: false,
    hasPenaltyShootout: false,
    hasStandingRules: false,
    hasDrawRules: true,
    hasQualificationRules: false,
    qualificationPathCount: 0,
    hasProgressionRules: false,
    progressionPathCount: 0,
    hasTieFormat: false,
    formatKind: 'Cup',
    compositionCapacity: null,
    affectationEntryCount: 0,
    actions: [],
  },
];

function appliedDraw(actions: ReturnType<typeof drawActions>): StageDraw {
  return {
    id: 'draw-1',
    kind: 'Slot',
    status: 'Published',
    resolutionState: 'Resolved',
    slotPlacements: [],
    actions,
  };
}

const noop = () => {};

type Props = ComponentProps<typeof StructureStaleDrawResolutionActions>;

function renderActions(overrides: Partial<Props> = {}) {
  const props: Props = {
    activeDraw: appliedDraw(
      drawActions({
        redo: { state: 'Available' },
        release: { state: 'Available', releasableCount: 2 },
      }),
    ),
    releaseCta: {
      draw: null,
      aligned: 0,
      total: 0,
      disabled: true,
      disabledReason: null,
    },
    redoPending: false,
    releasePending: false,
    redoError: null,
    releaseError: null,
    confirmRedoOpen: false,
    confirmReleaseOpen: false,
    structureStages,
    onOpenConfirmRedo: noop,
    onOpenConfirmRelease: noop,
    onCancelConfirmRedo: noop,
    onCancelConfirmRelease: noop,
    onConfirmRedo: noop,
    onConfirmRelease: noop,
    ...overrides,
  };

  return renderWithI18n(<StructureStaleDrawResolutionActions {...props} />);
}

describe('StructureStaleDrawResolutionActions', () => {
  it('shows Refaire and Libérer when server allows both', () => {
    renderActions();

    expect(
      screen.getByRole('button', { name: 'Refaire le tirage' }),
    ).toBeEnabled();
    expect(
      screen.getByRole('button', { name: /Libérer les placements/i }),
    ).toHaveTextContent('Libérer les placements (2)');
  });

  it('disables Refaire when redo is blocked by server guard', () => {
    renderActions({
      activeDraw: appliedDraw(
        drawActions({
          redo: {
            state: 'Blocked',
            blockCode: DRAW_RELEASE_ATTACHED_MATCHES,
          },
          release: { state: 'Available', releasableCount: 1 },
        }),
      ),
    });

    expect(
      screen.getByRole('button', { name: 'Refaire le tirage' }),
    ).toBeDisabled();
  });

  it('disables Libérer when release action is blocked', () => {
    renderActions({
      activeDraw: appliedDraw(
        drawActions({
          redo: { state: 'Available' },
          release: {
            state: 'Blocked',
            releasableCount: 3,
            blockCode: DRAW_RELEASE_ATTACHED_MATCHES,
          },
        }),
      ),
    });

    expect(
      screen.getByRole('button', { name: 'Libérer les placements' }),
    ).toBeDisabled();
  });

  it('requests redo confirmation before mutating', async () => {
    const user = userEvent.setup();
    const onOpenConfirmRedo = vi.fn();
    renderActions({ onOpenConfirmRedo });

    await user.click(screen.getByRole('button', { name: 'Refaire le tirage' }));
    expect(onOpenConfirmRedo).toHaveBeenCalledTimes(1);
  });

  it('confirms redo from dialog', async () => {
    const user = userEvent.setup();
    const onConfirmRedo = vi.fn();
    renderActions({ confirmRedoOpen: true, onConfirmRedo });

    const dialog = screen.getByRole('dialog');
    await user.click(
      within(dialog).getByRole('button', { name: 'Refaire le tirage' }),
    );
    expect(onConfirmRedo).toHaveBeenCalledTimes(1);
  });

  it('requests release confirmation before mutating', async () => {
    const user = userEvent.setup();
    const onOpenConfirmRelease = vi.fn();
    renderActions({ onOpenConfirmRelease });

    await user.click(
      screen.getByRole('button', { name: /Libérer les placements/i }),
    );
    expect(onOpenConfirmRelease).toHaveBeenCalledTimes(1);
  });

  it('hides Refaire when redo action is hidden', () => {
    renderActions({
      activeDraw: appliedDraw(hiddenDrawActions()),
    });

    expect(
      screen.queryByRole('button', { name: 'Refaire le tirage' }),
    ).not.toBeInTheDocument();
  });
});
