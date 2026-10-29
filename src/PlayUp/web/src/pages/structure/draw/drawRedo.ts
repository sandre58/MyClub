import type {
  CompetitionStatus,
  StageBracketPair,
  StageDraw,
  StageRound,
  StageSlot,
  StageStatus,
  StructureFormatKind,
} from '../../../types';
import {
  allowsCompetitionLifecycleMutation,
  allowsStageDraftOrReadyMutation,
  groupReleaseBlockedByAttachedMatches,
} from '../../stage/lifecycleGates';

/** Cancel succeeded and a later Refaire step did not. The draw id is unchanged. */
export class DrawRedoStoppedError extends Error {
  readonly step: 'release' | 'preview';
  readonly cause: unknown;

  constructor(step: 'release' | 'preview', cause: unknown) {
    super(
      step === 'release'
        ? 'Draw redo stopped before placements were released'
        : 'Draw redo stopped before a new preview',
    );
    this.name = 'DrawRedoStoppedError';
    this.step = step;
    this.cause = cause;
  }
}

/** Why « Refaire le tirage » stays hidden after an applied draw. */
export type DrawRedoBlockReason =
  'cupFixtureAttached' | 'groupMatchesAttached' | 'lifecycle';

export type AppliedDrawRedo =
  | { kind: 'hidden' }
  | { kind: 'available' }
  | { kind: 'blocked'; reason: DrawRedoBlockReason };

function idsEqual(
  a: string | null | undefined,
  b: string | null | undefined,
): boolean {
  if (a == null || b == null) {
    return false;
  }
  return a.trim().toLowerCase() === b.trim().toLowerCase();
}

/** Slot keys release would clear — placements still sitting on the stage. */
export function alignedDrawSlotKeys(
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

/**
 * Cup release is refused when a pair touched by those slots already has a match.
 * A pair is touched when either of its places is still aligned with the draw.
 */
export function cupReleaseTouchesAttachedFixture(input: {
  draw: StageDraw;
  slots: readonly StageSlot[];
  bracketPairs: readonly StageBracketPair[];
  rounds: readonly StageRound[];
}): boolean {
  const aligned = alignedDrawSlotKeys(input.draw, input.slots);
  if (aligned.size === 0) {
    return false;
  }
  const touched = new Set<string>();
  for (const pair of input.bracketPairs) {
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
  for (const round of input.rounds) {
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

/**
 * Refaire = cancel, release, then a new preview.
 * Group and Cup share the Draft|Ready lifecycle gate used by Libérer.
 * Cup release still stops when a touched pair already has a fixture attachment.
 */
export function resolveAppliedDrawRedo(input: {
  formatKind: StructureFormatKind | null | undefined;
  competitionStatus: CompetitionStatus;
  stageStatus: StageStatus;
  isApplied: boolean;
  draw: StageDraw;
  slots: readonly StageSlot[];
  bracketPairs: readonly StageBracketPair[];
  rounds: readonly StageRound[];
  /** Host stage.matchCount. Group + Ready + matches blocks Refaire. */
  attachedMatchCount?: number;
}): AppliedDrawRedo {
  if (!input.isApplied || input.draw.status !== 'Published') {
    return { kind: 'hidden' };
  }

  const lifecycleOk =
    allowsCompetitionLifecycleMutation(input.competitionStatus) &&
    allowsStageDraftOrReadyMutation(input.stageStatus);
  if (!lifecycleOk) {
    return { kind: 'blocked', reason: 'lifecycle' };
  }

  const groupDraw =
    input.formatKind === 'Groups' || input.draw.kind === 'Group';
  if (
    groupDraw &&
    groupReleaseBlockedByAttachedMatches({
      drawKind: 'Group',
      stageStatus: input.stageStatus,
      attachedMatchCount: input.attachedMatchCount ?? 0,
    })
  ) {
    return { kind: 'blocked', reason: 'groupMatchesAttached' };
  }

  const cupDraw = input.formatKind === 'Cup' || input.draw.kind === 'Slot';
  if (
    cupDraw &&
    !groupDraw &&
    cupReleaseTouchesAttachedFixture({
      draw: input.draw,
      slots: input.slots,
      bracketPairs: input.bracketPairs,
      rounds: input.rounds,
    })
  ) {
    return { kind: 'blocked', reason: 'cupFixtureAttached' };
  }

  return { kind: 'available' };
}
