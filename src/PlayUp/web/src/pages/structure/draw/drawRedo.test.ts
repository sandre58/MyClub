import { describe, expect, it } from 'vitest';
import type { StageDraw, StageRound, StageSlot } from '../../../types';
import { resolveAppliedDrawRedo } from './drawRedo';

function appliedGroup(overrides: Partial<StageDraw> = {}): StageDraw {
  return {
    id: 'draw-1',
    kind: 'Group',
    status: 'Published',
    resolutionState: 'Resolved',
    slotPlacements: [],
    isApplied: true,
    groupPlacements: [
      {
        groupId: 'g-a',
        groupDisplayName: 'A',
        entryId: 'e1',
        displayName: 'Alpha',
      },
    ],
    ...overrides,
  };
}

function appliedCup(): StageDraw {
  return {
    id: 'draw-cup',
    kind: 'Slot',
    status: 'Published',
    resolutionState: 'Resolved',
    isApplied: true,
    slotPlacements: [
      { slotKey: 'S1', entryId: 'e1', displayName: 'Alpha' },
      { slotKey: 'S2', entryId: 'e2', displayName: 'Beta' },
    ],
  };
}

const alignedSlots: StageSlot[] = [
  {
    slotKey: 'S1',
    entryId: 'e1',
    displayName: 'Alpha',
    coveredByCompleteFixture: true,
  },
  {
    slotKey: 'S2',
    entryId: 'e2',
    displayName: 'Beta',
    coveredByCompleteFixture: true,
  },
];

function rounds(attached: boolean): StageRound[] {
  return [
    {
      id: 'r1',
      name: 'Tour',
      fixtures: [
        {
          id: 'f1',
          slotAKey: 'S1',
          slotBKey: 'S2',
          attachments: attached ? [{ matchId: 'm1', legIndex: 0 }] : [],
        },
      ],
    },
  ];
}

describe('resolveAppliedDrawRedo', () => {
  it('allows Refaire for an applied group draw while the phase is Draft', () => {
    expect(
      resolveAppliedDrawRedo({
        formatKind: 'Groups',
        competitionStatus: 'Running',
        stageStatus: 'Draft',
        isApplied: true,
        draw: appliedGroup(),
        slots: [],
        bracketPairs: [],
        rounds: [],
      }),
    ).toEqual({ kind: 'available' });
  });

  it('hides Refaire for a group phase in Ready', () => {
    expect(
      resolveAppliedDrawRedo({
        formatKind: 'Groups',
        competitionStatus: 'Running',
        stageStatus: 'Ready',
        isApplied: true,
        draw: appliedGroup(),
        slots: [],
        bracketPairs: [],
        rounds: [],
      }),
    ).toEqual({ kind: 'blocked', reason: 'groupNotDraft' });
  });

  it('blocks Cup Refaire when a touched pair has a fixture attachment', () => {
    expect(
      resolveAppliedDrawRedo({
        formatKind: 'Cup',
        competitionStatus: 'Draft',
        stageStatus: 'Ready',
        isApplied: true,
        draw: appliedCup(),
        slots: alignedSlots,
        bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
        rounds: rounds(true),
      }),
    ).toEqual({ kind: 'blocked', reason: 'cupFixtureAttached' });
  });

  it('allows Cup Refaire when the touched pair has no attached match', () => {
    expect(
      resolveAppliedDrawRedo({
        formatKind: 'Cup',
        competitionStatus: 'Draft',
        stageStatus: 'Ready',
        isApplied: true,
        draw: appliedCup(),
        slots: alignedSlots,
        bracketPairs: [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
        rounds: rounds(false),
      }),
    ).toEqual({ kind: 'available' });
  });

  it('does not offer Refaire before the draw is applied', () => {
    expect(
      resolveAppliedDrawRedo({
        formatKind: 'Groups',
        competitionStatus: 'Draft',
        stageStatus: 'Draft',
        isApplied: false,
        draw: appliedGroup({ status: 'Draft', isApplied: false }),
        slots: [],
        bracketPairs: [],
        rounds: [],
      }),
    ).toEqual({ kind: 'hidden' });
  });
});
