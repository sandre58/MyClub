// Group manual placement — seat interaction, available teams, and the pool counter.
// Cup places stay in manualPlacementUi.ts.

import type { SchematicCase, StructureEntry } from '../../../types';

const assignEntryToGroupAction = 'AssignEntryToGroup';
const clearEntryFromGroupAction = 'ClearEntryFromGroup';

export type GroupSeatReasonKey =
  | 'placeInvite'
  | 'removeInvite'
  | 'matchLocked'
  | 'reserved'
  | 'drawPending'
  | 'clearLocked'
  | 'qualProg'
  | 'draw';

export type GroupSeatInteraction =
  | { kind: 'place'; reasonKey: 'placeInvite' }
  | { kind: 'remove'; reasonKey: 'removeInvite' }
  | {
      kind: 'locked';
      reasonKey: Exclude<GroupSeatReasonKey, 'placeInvite' | 'removeInvite'>;
    }
  | { kind: 'none' };

type GroupSeatStage = {
  compositionEntryIds?: readonly string[] | null;
  compositionEntryPots?: readonly { entryId: string; pot: number }[] | null;
};

export type GroupPlacementChoice = {
  entryId: string;
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  pot: number | null;
};

function occupantId(place: SchematicCase): string | null {
  return place.entry?.entryId ?? place.assignment?.entryId ?? null;
}

function hasGroupActions(actions: readonly string[]): boolean {
  return (
    actions.includes(assignEntryToGroupAction) ||
    actions.includes(clearEntryFromGroupAction)
  );
}

/**
 * How a Groups seat responds. Absent placement actions (Running, Suspended, other formats)
 * yield `none` even when the seat mode is Editable. `excess` and `outsideComposition`
 * do not change the kind.
 */
export function resolveGroupSeatInteraction(
  place: SchematicCase,
  options: { actions?: readonly string[] | null },
): GroupSeatInteraction {
  if (place.formPosition.kind !== 'GroupPlace' || !place.seatMode) {
    return { kind: 'none' };
  }

  const actions = options.actions ?? [];
  if (!hasGroupActions(actions)) {
    return { kind: 'none' };
  }

  const occupied = occupantId(place) != null;
  const canAssign = actions.includes(assignEntryToGroupAction);
  const canClearAction = actions.includes(clearEntryFromGroupAction);

  if (!occupied) {
    if (place.seatMode === 'Editable') {
      return canAssign
        ? { kind: 'place', reasonKey: 'placeInvite' }
        : { kind: 'none' };
    }
    if (place.seatMode === 'MatchLocked') {
      return { kind: 'locked', reasonKey: 'matchLocked' };
    }
    if (place.seatMode === 'Reserved') {
      return { kind: 'locked', reasonKey: 'reserved' };
    }
    if (place.seatMode === 'DrawPending') {
      return { kind: 'locked', reasonKey: 'drawPending' };
    }
    return { kind: 'none' };
  }

  if (place.seatMode === 'Editable' && place.canClear === true) {
    return canClearAction
      ? { kind: 'remove', reasonKey: 'removeInvite' }
      : { kind: 'none' };
  }
  if (place.seatMode === 'Editable') {
    return { kind: 'locked', reasonKey: 'clearLocked' };
  }
  if (place.seatMode === 'QualProg') {
    return { kind: 'locked', reasonKey: 'qualProg' };
  }
  if (place.seatMode === 'Draw') {
    return { kind: 'locked', reasonKey: 'draw' };
  }
  if (place.seatMode === 'DrawPending') {
    return { kind: 'locked', reasonKey: 'drawPending' };
  }
  if (place.seatMode === 'MatchLocked') {
    return { kind: 'locked', reasonKey: 'matchLocked' };
  }
  if (place.seatMode === 'Reserved') {
    return { kind: 'locked', reasonKey: 'reserved' };
  }
  return { kind: 'none' };
}

/** Composition entries that are not in any group, with pot when the server sent one. */
export function listAvailableGroupEntries(
  stage: GroupSeatStage,
  cases: readonly SchematicCase[],
  entries: readonly StructureEntry[],
): GroupPlacementChoice[] {
  const placed = new Set<string>();
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    const id = occupantId(place);
    if (id) placed.add(id);
  }

  const entryById = new Map(entries.map((entry) => [entry.entryId, entry]));
  const pots = stage.compositionEntryPots;
  const potById = new Map((pots ?? []).map((row) => [row.entryId, row.pot]));
  const available = (stage.compositionEntryIds ?? [])
    .filter((id) => !placed.has(id))
    .map((id, compositionIndex) => {
      const entry = entryById.get(id);
      return {
        entryId: id,
        displayName:
          entry?.displayName?.trim() || entry?.shortName?.trim() || id,
        logoMediaId: entry?.logoMediaId,
        primaryColor: entry?.primaryColor,
        pot: pots == null ? null : (potById.get(id) ?? null),
        compositionIndex,
      };
    });

  if (pots != null) {
    available.sort((left, right) => {
      const potOrder =
        (left.pot ?? Number.MAX_SAFE_INTEGER) -
        (right.pot ?? Number.MAX_SAFE_INTEGER);
      return potOrder !== 0
        ? potOrder
        : left.compositionIndex - right.compositionIndex;
    });
  }

  return available.map(({ compositionIndex: _index, ...choice }) => choice);
}

/**
 * Occupied counts every projected member, including excess and outside composition.
 * Capacity is PlacesPerGroup, so a full group is not shown short of its places.
 */
export function groupSeatCounter(
  group: { id: string },
  cases: readonly SchematicCase[],
  placesPerGroup: number,
): { occupied: number; capacity: number } {
  const occupied = cases.filter((place) => {
    if (place.formPosition.kind !== 'GroupPlace') return false;
    if (place.formPosition.groupId !== group.id) return false;
    return occupantId(place) != null;
  }).length;
  return { occupied, capacity: placesPerGroup };
}
