import { describe, expect, it } from 'vitest';
import type { StageDraw, StageRound, StageSlot } from '../types';
import {
  getDrawTilePrimarySummary,
  pickActiveDraw,
  pickDefaultDrawId,
} from './drawUi';

function draw(partial: Partial<StageDraw> & Pick<StageDraw, 'id'>): StageDraw {
  return {
    kind: 'Slot',
    status: 'Draft',
    resolutionState: 'NotResolved',
    pairings: [],
    slotPlacements: [],
    ...partial,
  };
}

describe('pickActiveDraw', () => {
  it('returns null when empty or only cancelled', () => {
    expect(pickActiveDraw([])).toBeNull();
    expect(
      pickActiveDraw([draw({ id: '1', status: 'Cancelled' })]),
    ).toBeNull();
  });

  it('prefers newest non-cancelled', () => {
    const active = pickActiveDraw([
      draw({ id: 'old', status: 'Published', resolutionState: 'Resolved' }),
      draw({ id: 'cancelled', status: 'Cancelled' }),
      draw({ id: 'new', status: 'Draft' }),
    ]);
    expect(active?.id).toBe('new');
  });
});

describe('pickDefaultDrawId', () => {
  it('falls back to cancelled when that is all there is', () => {
    expect(pickDefaultDrawId([draw({ id: 'c', status: 'Cancelled' })])).toBe(
      'c',
    );
  });
});

describe('getDrawTilePrimarySummary', () => {
  const slots: StageSlot[] = [
    {
      slotKey: 'S1',
      entryId: 'e1',
      displayName: 'A',
      coveredByCompleteFixture: false,
    },
  ];
  const rounds: StageRound[] = [];

  it('orders Applied > Published > Resolved > Draft', () => {
    expect(
      getDrawTilePrimarySummary(
        draw({
          id: '1',
          status: 'Published',
          resolutionState: 'Resolved',
          slotPlacements: [{ slotKey: 'S1', entryId: 'e1', displayName: 'A' }],
        }),
        slots,
        rounds,
      ),
    ).toBe('applied');

    expect(
      getDrawTilePrimarySummary(
        draw({
          id: '2',
          status: 'Published',
          resolutionState: 'Resolved',
          slotPlacements: [{ slotKey: 'S1', entryId: 'other', displayName: 'B' }],
        }),
        slots,
        rounds,
      ),
    ).toBe('published');

    expect(
      getDrawTilePrimarySummary(
        draw({ id: '3', status: 'Draft', resolutionState: 'Resolved' }),
        slots,
        rounds,
      ),
    ).toBe('resolved');

    expect(
      getDrawTilePrimarySummary(
        draw({ id: '4', status: 'Draft', resolutionState: 'NotResolved' }),
        slots,
        rounds,
      ),
    ).toBe('draft');

    expect(
      getDrawTilePrimarySummary(
        draw({ id: '5', status: 'Draft', resolutionState: 'NoSolution' }),
        slots,
        rounds,
      ),
    ).toBe('noSolution');
  });
});
