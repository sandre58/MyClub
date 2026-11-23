import { describe, expect, it } from 'vitest';
import type { StageDraw } from '../../../types';
import {
  formatReleaseDrawnTeamsLabel,
  releaseDrawnTargetCount,
  resolveServerReleaseCta,
} from './drawActions';

const t = ((key: string, params?: Record<string, unknown>) => {
  if (key === 'draw:releaseDrawnTeam') {
    return "Libérer l'équipe tirée";
  }
  if (key === 'draw:releaseDrawnTeams') {
    return `Libérer les ${String(params?.n)} équipes tirées`;
  }
  return key;
}) as Parameters<typeof formatReleaseDrawnTeamsLabel>[0];

function cancelledDraw(
  id: string,
  kind: StageDraw['kind'],
  release: NonNullable<StageDraw['actions']>['release'],
  placementCount: number,
): StageDraw {
  return {
    id,
    kind,
    status: 'Cancelled',
    resolutionState: 'Resolved',
    slotPlacements:
      kind === 'Slot'
        ? Array.from({ length: placementCount }, (_, index) => ({
            slotKey: `S${index + 1}`,
            entryId: `e${index + 1}`,
            displayName: `Team ${index + 1}`,
          }))
        : [],
    groupPlacements:
      kind === 'Group'
        ? Array.from({ length: placementCount }, (_, index) => ({
            groupId: 'g1',
            entryId: `e${index + 1}`,
            displayName: `Team ${index + 1}`,
          }))
        : [],
    actions: {
      cancel: { state: 'Hidden' },
      release,
      apply: { state: 'Hidden' },
      publishAndApply: { state: 'Hidden' },
      redo: { state: 'Hidden' },
    },
  };
}

describe('releaseDrawnTargetCount', () => {
  it('uses server drawnTargetCount for Cup (14 drawn, 16 resolution rows)', () => {
    const draw = cancelledDraw(
      'd1',
      'Slot',
      { state: 'Available', releasableCount: 14, drawnTargetCount: 14 },
      16,
    );
    expect(releaseDrawnTargetCount(draw)).toBe(14);
  });

  it('uses server drawnTargetCount for Groups (6 drawn, 8 group results)', () => {
    const draw = cancelledDraw(
      'g1',
      'Group',
      { state: 'Available', releasableCount: 6, drawnTargetCount: 6 },
      8,
    );
    expect(releaseDrawnTargetCount(draw)).toBe(6);
  });
});

describe('formatReleaseDrawnTeamsLabel', () => {
  it('uses the same wording for Cup and Groups', () => {
    const cup = cancelledDraw(
      'cup',
      'Slot',
      { state: 'Available', releasableCount: 14, drawnTargetCount: 14 },
      16,
    );
    const group = cancelledDraw(
      'grp',
      'Group',
      { state: 'Available', releasableCount: 6, drawnTargetCount: 6 },
      8,
    );
    expect(formatReleaseDrawnTeamsLabel(t, cup, [cup])).toBe(
      'Libérer les 14 équipes tirées',
    );
    expect(formatReleaseDrawnTeamsLabel(t, group, [group])).toBe(
      'Libérer les 6 équipes tirées',
    );
  });

  it('adds execution suffix only when several cancelled draws are releasable', () => {
    const first = cancelledDraw(
      'd1',
      'Slot',
      { state: 'Available', releasableCount: 2, drawnTargetCount: 2 },
      2,
    );
    const second = cancelledDraw(
      'd2',
      'Slot',
      { state: 'Available', releasableCount: 1, drawnTargetCount: 1 },
      1,
    );
    const third: StageDraw = {
      id: 'd3',
      kind: 'Slot',
      status: 'Draft',
      resolutionState: 'NotResolved',
      slotPlacements: [],
    };
    const list = [first, second, third];
    expect(formatReleaseDrawnTeamsLabel(t, first, list)).toBe(
      'Libérer les 2 équipes tirées (#1)',
    );
    expect(formatReleaseDrawnTeamsLabel(t, second, list)).toBe(
      "Libérer l'équipe tirée (#2)",
    );
  });
});

describe('resolveServerReleaseCta total', () => {
  it('uses drawnTargetCount as denominator', () => {
    const draw = cancelledDraw(
      'd-ok',
      'Group',
      { state: 'Available', releasableCount: 6, drawnTargetCount: 6 },
      8,
    );
    const cta = resolveServerReleaseCta({
      releaseCandidateDrawId: 'd-ok',
      draws: [draw],
    });
    expect(cta.total).toBe(6);
    expect(cta.aligned).toBe(6);
  });
});
