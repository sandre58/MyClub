import type {
  CompetitionStatus,
  StageBracketPair,
  StageDraw,
  StageDrawActions,
  StageGroupOverview,
  StageOverview,
  StageRound,
  StageSlot,
  StageStatus,
} from '../types';

export interface StampDrawActionsContext {
  competitionStatus?: CompetitionStatus;
  attachedMatchCount?: number;
}

/**
 * Test-only stand-in for the server projection, so existing fixtures keep the
 * buttons they had before the client stopped deciding draw lifecycle.
 * Draws that already set `actions` are left unchanged.
 */
export function stampStageOverviewDrawActions(
  overview: StageOverview,
  context: StampDrawActionsContext = {},
): StageOverview {
  const competitionStatus = context.competitionStatus ?? 'Draft';
  const attachedMatchCount = context.attachedMatchCount ?? 0;
  const applied = overview.draws.some(
    (draw) =>
      draw.status === 'Published' && drawIsApplied(draw, overview.slots),
  );
  const draws = overview.draws.map((draw) =>
    draw.actions
      ? draw
      : {
          ...draw,
          actions: inferActions(draw, overview, {
            competitionStatus,
            attachedMatchCount,
            stageHasPublishedAppliedDraw: applied,
          }),
        },
  );
  const releaseCandidateDrawId =
    overview.releaseCandidateDrawId !== undefined
      ? overview.releaseCandidateDrawId
      : lastReleasableCancelledId(draws);
  return { ...overview, draws, releaseCandidateDrawId };
}

function inferActions(
  draw: StageDraw,
  overview: StageOverview,
  context: {
    competitionStatus: CompetitionStatus;
    attachedMatchCount: number;
    stageHasPublishedAppliedDraw: boolean;
  },
): StageDrawActions {
  const hidden = { state: 'Hidden' as const };
  if (
    context.competitionStatus === 'Completed' ||
    context.competitionStatus === 'Archived'
  ) {
    return {
      cancel: hidden,
      release: {
        state: 'Hidden',
        releasableCount: alignedCount(draw, overview),
      },
      apply: hidden,
      publishAndApply: hidden,
      redo: hidden,
    };
  }

  const stageOk = overview.status === 'Draft' || overview.status === 'Ready';
  const slotOrGroup = draw.kind === 'Slot' || draw.kind === 'Group';
  const resolved = draw.resolutionState === 'Resolved';
  const aligned = alignedCount(draw, overview);
  const applied = drawIsApplied(draw, overview.slots);

  const cancel =
    draw.status === 'Cancelled' ||
    (draw.status !== 'Draft' && draw.status !== 'Published')
      ? hidden
      : stageOk
        ? { state: 'Available' as const }
        : { state: 'Blocked' as const, blockCode: 'Stage.StructureLocked' };

  const release = inferRelease(draw, {
    stageStatus: overview.status,
    slotOrGroup,
    aligned,
    stageOk,
    attachedMatchCount: context.attachedMatchCount,
    stageHasPublishedAppliedDraw: context.stageHasPublishedAppliedDraw,
  });

  const publishAndApply =
    slotOrGroup && draw.status === 'Draft' && resolved && stageOk
      ? { state: 'Available' as const }
      : hidden;

  const apply =
    slotOrGroup &&
    draw.status === 'Published' &&
    resolved &&
    !applied &&
    stageOk
      ? { state: 'Available' as const }
      : hidden;

  const redo = inferRedo(draw, overview, {
    stageOk,
    applied,
    attachedMatchCount: context.attachedMatchCount,
  });

  return { cancel, release, apply, publishAndApply, redo };
}

function inferRelease(
  draw: StageDraw,
  input: {
    stageStatus: StageStatus;
    slotOrGroup: boolean;
    aligned: number;
    stageOk: boolean;
    attachedMatchCount: number;
    stageHasPublishedAppliedDraw: boolean;
  },
) {
  if (!input.slotOrGroup || draw.status !== 'Cancelled' || input.aligned <= 0) {
    return { state: 'Hidden' as const, releasableCount: input.aligned };
  }
  if (input.stageHasPublishedAppliedDraw) {
    return {
      state: 'Blocked' as const,
      blockCode: 'Application.DrawReleaseFailure',
      releasableCount: input.aligned,
    };
  }
  if (!input.stageOk) {
    return {
      state: 'Blocked' as const,
      blockCode: 'Stage.StructureLocked',
      releasableCount: input.aligned,
    };
  }
  if (
    draw.kind === 'Group' &&
    input.stageStatus === 'Ready' &&
    input.attachedMatchCount > 0
  ) {
    return {
      state: 'Blocked' as const,
      blockCode: 'Application.DrawReleaseBlockedByAttachedMatches',
      releasableCount: input.aligned,
    };
  }
  return { state: 'Available' as const, releasableCount: input.aligned };
}

function inferRedo(
  draw: StageDraw,
  overview: StageOverview,
  input: { stageOk: boolean; applied: boolean; attachedMatchCount: number },
) {
  const hidden = { state: 'Hidden' as const };
  if (!input.applied || draw.status !== 'Published') {
    return hidden;
  }
  if (!input.stageOk) {
    return { state: 'Blocked' as const, blockCode: 'Stage.StructureLocked' };
  }
  const groupDraw = draw.kind === 'Group';
  if (
    groupDraw &&
    overview.status === 'Ready' &&
    input.attachedMatchCount > 0
  ) {
    return {
      state: 'Blocked' as const,
      blockCode: 'Application.DrawReleaseBlockedByAttachedMatches',
    };
  }
  if (
    !groupDraw &&
    draw.kind === 'Slot' &&
    cupReleaseTouchesAttachedFixture(draw, overview)
  ) {
    return {
      state: 'Blocked' as const,
      blockCode: 'Stage.MaterializedOpponentConflict',
    };
  }
  return { state: 'Available' as const };
}

function lastReleasableCancelledId(draws: readonly StageDraw[]): string | null {
  let candidate: string | null = null;
  for (const draw of draws) {
    const release = draw.actions?.release;
    if (
      draw.status === 'Cancelled' &&
      release != null &&
      release.releasableCount >= 1
    ) {
      candidate = draw.id;
    }
  }
  return candidate;
}

function drawIsApplied(draw: StageDraw, slots: readonly StageSlot[]): boolean {
  if (draw.isApplied === true) {
    return true;
  }
  if (draw.isApplied === false) {
    return false;
  }
  return (
    draw.status === 'Published' &&
    draw.kind === 'Slot' &&
    isSlotApplied(draw, slots)
  );
}

function isSlotApplied(draw: StageDraw, slots: readonly StageSlot[]): boolean {
  if (draw.slotPlacements.length === 0) {
    return false;
  }
  const byKey = new Map(slots.map((slot) => [slot.slotKey, slot]));
  return draw.slotPlacements.every((placement) => {
    const slot = byKey.get(placement.slotKey);
    return slot?.entryId != null && idsEqual(slot.entryId, placement.entryId);
  });
}

function alignedCount(draw: StageDraw, overview: StageOverview): number {
  if (draw.resolutionState !== 'Resolved') {
    return 0;
  }
  if (draw.kind === 'Slot') {
    const byKey = new Map(overview.slots.map((slot) => [slot.slotKey, slot]));
    return draw.slotPlacements.filter((placement) => {
      const slot = byKey.get(placement.slotKey);
      return slot?.entryId != null && idsEqual(slot.entryId, placement.entryId);
    }).length;
  }
  if (draw.kind === 'Group') {
    return countGroup(draw, overview.groups ?? []);
  }
  return 0;
}

function countGroup(
  draw: StageDraw,
  groups: readonly StageGroupOverview[],
): number {
  const placements = draw.groupPlacements ?? [];
  const byGroup = new Map(
    groups.map((group) => [
      group.id.trim().toLowerCase(),
      new Set(group.entryIds.map((id) => id.trim().toLowerCase())),
    ]),
  );
  return placements.filter((placement) =>
    byGroup
      .get(placement.groupId.trim().toLowerCase())
      ?.has(placement.entryId.trim().toLowerCase()),
  ).length;
}

function cupReleaseTouchesAttachedFixture(
  draw: StageDraw,
  overview: StageOverview,
): boolean {
  return touches(
    alignedSlotKeys(draw, overview.slots),
    overview.bracketPairs ?? [],
    overview.rounds,
  );
}

function alignedSlotKeys(
  draw: StageDraw,
  slots: readonly StageSlot[],
): Set<string> {
  const byKey = new Map(slots.map((slot) => [slot.slotKey, slot]));
  const keys = new Set<string>();
  if (draw.kind !== 'Slot' || draw.resolutionState !== 'Resolved') {
    return keys;
  }
  for (const placement of draw.slotPlacements) {
    const slot = byKey.get(placement.slotKey);
    if (slot?.entryId != null && idsEqual(slot.entryId, placement.entryId)) {
      keys.add(placement.slotKey.trim());
    }
  }
  return keys;
}

function touches(
  aligned: Set<string>,
  pairs: readonly StageBracketPair[],
  rounds: readonly StageRound[],
): boolean {
  if (aligned.size === 0) {
    return false;
  }
  const touched = new Set<string>();
  for (const pair of pairs) {
    const slotA = pair.slotAKey.trim();
    const slotB = pair.slotBKey.trim();
    if (!aligned.has(slotA) && !aligned.has(slotB)) {
      continue;
    }
    if (slotA) {
      touched.add(slotA);
    }
    if (slotB) {
      touched.add(slotB);
    }
  }
  if (touched.size === 0) {
    return false;
  }
  for (const round of rounds) {
    for (const fixture of round.fixtures) {
      if (fixture.attachments.length === 0) {
        continue;
      }
      const slotA = fixture.slotAKey?.trim() ?? '';
      const slotB = fixture.slotBKey?.trim() ?? '';
      if ((slotA && touched.has(slotA)) || (slotB && touched.has(slotB))) {
        return true;
      }
    }
  }
  return false;
}

function idsEqual(
  a: string | null | undefined,
  b: string | null | undefined,
): boolean {
  if (a == null || b == null) {
    return false;
  }
  return a.trim().toLowerCase() === b.trim().toLowerCase();
}
