import { describe, expect, it } from 'vitest';
import type { StageBracketPair, StageDraw, StageSlot } from '../../types';
import {
  areDrawRulesLockedByExecution,
  drawExecutionNumber,
  getDrawUiProjection,
  groupPlacementRows,
  pickActiveDraw,
  pickDefaultDrawId,
  projectSlotDrawResult,
  resolveDrawCreateBlockPresentation,
  resolveDrawCreateGate,
  resolveDrawDetailGuidance,
  resolveDrawDetailHeaderChips,
  resolveTopologyDrawExecutionBadge,
  slotConfrontationRows,
  slotsHaveOccupants,
  sortDrawsNewestFirst,
} from './drawUi';

function draw(partial: Partial<StageDraw> & Pick<StageDraw, 'id'>): StageDraw {
  return {
    kind: 'Slot',
    status: 'Draft',
    resolutionState: 'NotResolved',
    slotPlacements: [],
    ...partial,
  };
}

describe('groupPlacementRows', () => {
  it('orders groups by label (A before B) regardless of placement order', () => {
    const rows = groupPlacementRows(
      [
        {
          groupId: 'g-b',
          groupDisplayName: 'B',
          entryId: 'e2',
          displayName: 'Équipe 2',
        },
        {
          groupId: 'g-a',
          groupDisplayName: 'A',
          entryId: 'e1',
          displayName: 'Équipe 1',
        },
      ],
      '?',
      '?',
    );

    expect(rows.map((row) => row.groupLabel)).toEqual(['A', 'B']);
  });
});

describe('slotConfrontationRows', () => {
  const placements = [
    {
      slotKey: 'S1',
      entryId: 'e1',
      displayName: 'Belgium',
      logoMediaId: 'logo-be',
    },
    {
      slotKey: 'S2',
      entryId: 'e2',
      displayName: 'Poland',
    },
    {
      slotKey: 'S3',
      entryId: 'e3',
      displayName: 'Turkey',
    },
    {
      slotKey: 'S4',
      entryId: 'e4',
      displayName: 'Denmark',
    },
  ];

  const pairs: StageBracketPair[] = [
    { pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' },
    { pairKey: 'P2', slotAKey: 'S3', slotBKey: 'S4' },
  ];

  it('pairs from BracketPairs in structural order without match numbers', () => {
    const rows = slotConfrontationRows(placements, pairs, '?');

    expect(rows).toHaveLength(2);
    expect(rows[0]).toMatchObject({
      key: 'P1',
      sideA: {
        displayName: 'Belgium',
        logoMediaId: 'logo-be',
        slotKey: 'S1',
      },
      sideB: { displayName: 'Poland', slotKey: 'S2' },
    });
    expect(rows[1]).toMatchObject({
      key: 'P2',
      sideA: { displayName: 'Turkey', entryId: 'e3' },
      sideB: { displayName: 'Denmark', entryId: 'e4' },
    });
  });

  it('keeps placements without a covering BracketPair as unpaired', () => {
    const cupPlacements = [
      { slotKey: 'S2', entryId: 'e2', displayName: 'Poland' },
      { slotKey: 'S1', entryId: 'e1', displayName: 'Belgium' },
      { slotKey: 'S3', entryId: 'e3', displayName: 'Turkey' },
    ];

    const result = projectSlotDrawResult(
      cupPlacements,
      [{ pairKey: 'P1', slotAKey: 'S1', slotBKey: 'S2' }],
      '?',
    );

    expect(result.confrontations).toHaveLength(1);
    expect(result.confrontations[0]?.key).toBe('P1');
    expect(result.unpaired.map((side) => side.slotKey)).toEqual(['S3']);
    expect(result.unpaired[0]?.displayName).toBe('Turkey');
  });

  it('does not invent pairs from *-A/*-B stems or empty BracketPairs', () => {
    const legacy = [
      { slotKey: 'R16-1-A', entryId: 'e1', displayName: 'Belgium' },
      { slotKey: 'R16-1-B', entryId: 'e2', displayName: 'Poland' },
    ];

    const result = projectSlotDrawResult(legacy, [], '?');

    expect(result.confrontations).toEqual([]);
    expect(result.unpaired.map((s) => s.slotKey)).toEqual([
      'R16-1-A',
      'R16-1-B',
    ]);
  });
});

describe('getDrawUiProjection masterChip', () => {
  it('shows Résolu on Draft resolved (not Appliqué from leftover occupancy)', () => {
    const projection = getDrawUiProjection(
      draw({
        id: 'draft-after-cancel',
        kind: 'Group',
        status: 'Draft',
        resolutionState: 'Resolved',
        isApplied: true,
      }),
    );

    expect(projection.isApplied).toBe(false);
    expect(projection.masterChip).toEqual({
      kind: 'resolution',
      state: 'Resolved',
    });
  });

  it('shows Appliqué only for Published draws that are applied', () => {
    const projection = getDrawUiProjection(
      draw({
        id: 'published-applied',
        kind: 'Group',
        status: 'Published',
        resolutionState: 'Resolved',
        isApplied: true,
      }),
    );

    expect(projection.isApplied).toBe(true);
    expect(projection.masterChip).toEqual({ kind: 'applied' });
  });

  it('shows Publié for Published not applied (recovery)', () => {
    expect(
      getDrawUiProjection(
        draw({
          id: 'published-pending',
          kind: 'Group',
          status: 'Published',
          resolutionState: 'Resolved',
          isApplied: false,
        }),
      ).masterChip,
    ).toEqual({ kind: 'lifecycle', status: 'Published' });
  });

  it('shows no rail chip for Draft not resolved', () => {
    expect(
      getDrawUiProjection(
        draw({
          id: 'draft-empty',
          status: 'Draft',
          resolutionState: 'NotResolved',
        }),
      ).masterChip,
    ).toBeNull();
  });

  it('shows Sans solution then Annulé with priority', () => {
    expect(
      getDrawUiProjection(
        draw({
          id: 'no-sol',
          status: 'Draft',
          resolutionState: 'NoSolution',
        }),
      ).masterChip,
    ).toEqual({ kind: 'resolution', state: 'NoSolution' });

    expect(
      getDrawUiProjection(
        draw({
          id: 'cancelled',
          status: 'Cancelled',
          resolutionState: 'Resolved',
          isApplied: true,
        }),
      ).masterChip,
    ).toEqual({ kind: 'lifecycle', status: 'Cancelled' });
  });
});

describe('resolveDrawDetailHeaderChips', () => {
  it('shows Publié · Appliqué when published and applied', () => {
    expect(
      resolveDrawDetailHeaderChips(
        draw({
          id: 'applied',
          status: 'Published',
          resolutionState: 'Resolved',
        }),
        true,
      ),
    ).toEqual([
      { kind: 'lifecycle', status: 'Published' },
      { kind: 'applied' },
    ]);
  });

  it('shows Publié only when published and not applied', () => {
    expect(
      resolveDrawDetailHeaderChips(
        draw({
          id: 'pending',
          status: 'Published',
          resolutionState: 'Resolved',
        }),
        false,
      ),
    ).toEqual([{ kind: 'lifecycle', status: 'Published' }]);
  });

  it('shows Résolu for draft resolved', () => {
    expect(
      resolveDrawDetailHeaderChips(
        draw({
          id: 'draft-ok',
          status: 'Draft',
          resolutionState: 'Resolved',
        }),
        false,
      ),
    ).toEqual([{ kind: 'resolution', state: 'Resolved' }]);
  });

  it('shows Sans solution without lifecycle chip', () => {
    expect(
      resolveDrawDetailHeaderChips(
        draw({
          id: 'no-sol',
          status: 'Draft',
          resolutionState: 'NoSolution',
        }),
        false,
      ),
    ).toEqual([{ kind: 'resolution', state: 'NoSolution' }]);
  });
});

describe('resolveDrawDetailGuidance', () => {
  it('omits Applied phrase (tooltip on Appliqué chip instead)', () => {
    const ui = getDrawUiProjection(
      draw({
        id: 'applied',
        kind: 'Group',
        status: 'Published',
        resolutionState: 'Resolved',
        isApplied: true,
      }),
    );
    expect(resolveDrawDetailGuidance(ui)).toBeNull();
  });

  it('uses warning alert for Published not applied', () => {
    const ui = getDrawUiProjection(
      draw({
        id: 'pending',
        kind: 'Group',
        status: 'Published',
        resolutionState: 'Resolved',
        isApplied: false,
      }),
    );
    expect(resolveDrawDetailGuidance(ui)).toEqual({
      kind: 'alert',
      tone: 'warning',
      messageKey: 'published',
    });
  });

  it('uses warning alert for NoSolution', () => {
    const ui = getDrawUiProjection(
      draw({
        id: 'no-sol',
        status: 'Draft',
        resolutionState: 'NoSolution',
      }),
    );
    expect(resolveDrawDetailGuidance(ui)).toEqual({
      kind: 'alert',
      tone: 'warning',
      messageKey: 'noSolution',
    });
  });

  it('projects Draft+NotResolved as generation interrupted with body', () => {
    const ui = getDrawUiProjection(
      draw({
        id: 'orphan',
        status: 'Draft',
        resolutionState: 'NotResolved',
      }),
    );
    expect(ui.messageKey).toBe('generationInterrupted');
    expect(resolveDrawDetailGuidance(ui)).toEqual({
      kind: 'alert',
      tone: 'warning',
      messageKey: 'generationInterrupted',
      bodyMessageKey: 'generationInterruptedBody',
    });
  });

  it('omits Cancelled phrase (badge + EmptyState when no result)', () => {
    const ui = getDrawUiProjection(
      draw({
        id: 'cancelled-empty',
        status: 'Cancelled',
        resolutionState: 'NotResolved',
      }),
    );
    expect(ui.messageKey).toBe('cancelled');
    expect(ui.showResults).toBe(false);
    expect(resolveDrawDetailGuidance(ui)).toBeNull();
  });

  it('omits draftResolved phrase (tooltip on Résolu chip instead)', () => {
    const ui = getDrawUiProjection(
      draw({
        id: 'draft-ok',
        status: 'Draft',
        resolutionState: 'Resolved',
      }),
    );
    expect(resolveDrawDetailGuidance(ui)).toBeNull();
  });
});

describe('resolveDrawCreateGate', () => {
  it('blocks empty Slot pools as emptyPool (not countMismatch)', () => {
    expect(
      resolveDrawCreateGate({
        kind: 'Slot',
        hasActiveDraw: false,
        compositionEntryCount: 0,
        isRootComposition: true,
        numberOfPots: null,
        groupCount: 0,
        slotCount: 16,
      }),
    ).toEqual({ ok: false, reason: 'emptyPool' });

    expect(
      resolveDrawCreateGate({
        kind: 'Slot',
        hasActiveDraw: false,
        compositionEntryCount: 0,
        isRootComposition: false,
        numberOfPots: null,
        groupCount: 0,
        slotCount: 16,
      }),
    ).toEqual({ ok: false, reason: 'emptyPoolUpstream' });
  });

  it('requires composition == Places for Slot', () => {
    expect(
      resolveDrawCreateGate({
        kind: 'Slot',
        hasActiveDraw: false,
        compositionEntryCount: 4,
        numberOfPots: null,
        groupCount: 0,
        slotCount: 4,
      }),
    ).toEqual({ ok: true });

    expect(
      resolveDrawCreateGate({
        kind: 'Slot',
        hasActiveDraw: false,
        compositionEntryCount: 4,
        numberOfPots: null,
        groupCount: 0,
        slotCount: 16,
      }),
    ).toEqual({ ok: false, reason: 'countMismatch' });
  });

  it('requires pots × groups for Group kind', () => {
    expect(
      resolveDrawCreateGate({
        kind: 'Group',
        hasActiveDraw: false,
        compositionEntryCount: 8,
        numberOfPots: null,
        groupCount: 2,
      }),
    ).toEqual({ ok: false, reason: 'missingPots' });

    expect(
      resolveDrawCreateGate({
        kind: 'Group',
        hasActiveDraw: false,
        compositionEntryCount: 8,
        numberOfPots: 4,
        groupCount: 2,
      }),
    ).toEqual({ ok: true });

    expect(
      resolveDrawCreateGate({
        kind: 'Group',
        hasActiveDraw: false,
        compositionEntryCount: 7,
        numberOfPots: 4,
        groupCount: 2,
      }),
    ).toEqual({ ok: false, reason: 'groupShape' });
  });

  it('blocks when an active draw already exists', () => {
    expect(
      resolveDrawCreateGate({
        kind: 'Slot',
        hasActiveDraw: true,
        compositionEntryCount: 4,
        numberOfPots: null,
        groupCount: 0,
        slotCount: 4,
      }),
    ).toEqual({ ok: false, reason: 'active' });
  });

  it('does not treat vacant Qual/Prog feeds as occupants', () => {
    expect(
      slotsHaveOccupants([
        { entryId: null },
        { entryId: undefined },
        { entryId: null },
      ]),
    ).toBe(false);

    expect(
      resolveDrawCreateGate({
        kind: 'Slot',
        hasActiveDraw: false,
        compositionEntryCount: 16,
        numberOfPots: null,
        groupCount: 0,
        slotCount: 16,
        hasOccupiedSlots: slotsHaveOccupants([
          { entryId: null },
          { entryId: null },
        ]),
      }),
    ).toEqual({ ok: true });
  });

  it('maps create-block presentation tones', () => {
    expect(resolveDrawCreateBlockPresentation('active')).toEqual({
      kind: 'quiet',
    });
    expect(resolveDrawCreateBlockPresentation('emptyPool')).toEqual({
      kind: 'inline',
      tone: 'info',
      action: false,
    });
    expect(resolveDrawCreateBlockPresentation('occupiedSlots')).toEqual({
      kind: 'inline',
      tone: 'warning',
      action: true,
    });
  });
});

describe('drawExecutionNumber / sortDrawsNewestFirst', () => {
  it('numbers oldest as #1 even when API sends newest first', () => {
    const draws = [
      draw({ id: 'd2', status: 'Draft' }),
      draw({ id: 'd1', status: 'Cancelled' }),
    ];
    expect(drawExecutionNumber(draws, 'd1')).toBe(1);
    expect(drawExecutionNumber(draws, 'd2')).toBe(2);
    expect(sortDrawsNewestFirst(draws).map((d) => d.id)).toEqual(['d2', 'd1']);
  });
});

describe('pickActiveDraw', () => {
  it('returns null when empty or only cancelled', () => {
    expect(pickActiveDraw([])).toBeNull();
    expect(pickActiveDraw([draw({ id: '1', status: 'Cancelled' })])).toBeNull();
  });

  it('prefers newest non-cancelled', () => {
    const active = pickActiveDraw([
      draw({ id: 'd1', status: 'Published', resolutionState: 'Resolved' }),
      draw({ id: 'd2', status: 'Cancelled' }),
      draw({ id: 'd3', status: 'Draft' }),
    ]);
    expect(active?.id).toBe('d3');
  });

  it('picks newest by id even when payload is reverse-ordered', () => {
    const active = pickActiveDraw([
      draw({ id: 'd3', status: 'Draft' }),
      draw({ id: 'd1', status: 'Cancelled' }),
    ]);
    expect(active?.id).toBe('d3');
  });
});

describe('pickDefaultDrawId', () => {
  it('falls back to cancelled when that is all there is', () => {
    expect(pickDefaultDrawId([draw({ id: 'c', status: 'Cancelled' })])).toBe(
      'c',
    );
  });
});

describe('resolveTopologyDrawExecutionBadge', () => {
  const slots: StageSlot[] = [
    {
      slotKey: 'S1',
      entryId: 'e1',
      displayName: 'A',
      coveredByCompleteFixture: false,
    },
  ];

  it('is null without DrawRules', () => {
    expect(
      resolveTopologyDrawExecutionBadge(false, draw({ id: '1' }), slots),
    ).toBeNull();
  });

  it('maps ToLaunch / InProgress / ToApply / Applied', () => {
    expect(resolveTopologyDrawExecutionBadge(true, null, slots)).toBe(
      'ToLaunch',
    );

    expect(
      resolveTopologyDrawExecutionBadge(
        true,
        draw({ id: '1', status: 'Draft' }),
        slots,
      ),
    ).toBe('InProgress');

    expect(
      resolveTopologyDrawExecutionBadge(
        true,
        draw({
          id: '2',
          status: 'Published',
          resolutionState: 'Resolved',
          slotPlacements: [
            { slotKey: 'S1', entryId: 'other', displayName: 'B' },
          ],
        }),
        slots,
      ),
    ).toBe('ToApply');

    expect(
      resolveTopologyDrawExecutionBadge(
        true,
        draw({
          id: '3',
          status: 'Published',
          resolutionState: 'Resolved',
          slotPlacements: [{ slotKey: 'S1', entryId: 'e1', displayName: 'A' }],
        }),
        slots,
      ),
    ).toBe('Applied');
  });
});

describe('areDrawRulesLockedByExecution', () => {
  it('is unlocked with no draws or only NotResolved draft', () => {
    expect(areDrawRulesLockedByExecution([])).toBe(false);
    expect(
      areDrawRulesLockedByExecution([
        draw({ id: '1', status: 'Draft', resolutionState: 'NotResolved' }),
      ]),
    ).toBe(false);
  });

  it('locks after Generate (Resolved or NoSolution) while non-cancelled', () => {
    expect(
      areDrawRulesLockedByExecution([
        draw({ id: '1', status: 'Draft', resolutionState: 'Resolved' }),
      ]),
    ).toBe(true);
    expect(
      areDrawRulesLockedByExecution([
        draw({ id: '1', status: 'Draft', resolutionState: 'NoSolution' }),
      ]),
    ).toBe(true);
    expect(
      areDrawRulesLockedByExecution([
        draw({ id: '1', status: 'Published', resolutionState: 'Resolved' }),
      ]),
    ).toBe(true);
  });

  it('unlocks after Cancel even if resolution was generated', () => {
    expect(
      areDrawRulesLockedByExecution([
        draw({
          id: '1',
          status: 'Cancelled',
          resolutionState: 'Resolved',
        }),
      ]),
    ).toBe(false);
  });
});
