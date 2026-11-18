// Group manual placement — seat interaction, available teams, and the pool counter.
// Cup places stay in manualPlacementUi.ts.

import type { SchematicCase, StructureEntry } from '../../../types';

const assignEntryToGroupAction = 'AssignEntryToGroup';
const assignEntriesToGroupAction = 'AssignEntriesToGroup';
const clearEntryFromGroupAction = 'ClearEntryFromGroup';
const replaceEntryInGroupAction = 'ReplaceEntryInGroup';

export { assignEntriesToGroupAction, replaceEntryInGroupAction };

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
  | { kind: 'edit'; reasonKey: 'removeInvite' }
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
      ? { kind: 'edit', reasonKey: 'removeInvite' }
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
export type GroupManualTarget = {
  groupId: string;
  groupName: string;
  /** i18n group label, e.g. "Groupe A". */
  label: string;
  /** Representative empty editable seat in the group. */
  place: SchematicCase;
};

/** Whether the phase exposes at least one group with an empty `place` seat. */
export function canOfferGroupManualPlacement(
  cases: readonly SchematicCase[],
  actions: readonly string[],
): boolean {
  return listGroupManualTargets(cases, actions, () => '').length > 0;
}

/** Display label for a group from any schematic seat (not only placeable targets). */
export function resolveGroupDisplayLabel(
  cases: readonly SchematicCase[],
  groupId: string,
  labelForGroup: (name: string) => string,
): string {
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    if (place.formPosition.groupId !== groupId) continue;
    const name = place.formPosition.groupName?.trim() || groupId;
    return labelForGroup(name);
  }
  return labelForGroup(groupId);
}

export function listGroupManualTargets(
  cases: readonly SchematicCase[],
  actions: readonly string[],
  labelForGroup: (name: string) => string,
): GroupManualTarget[] {
  const buckets = new Map<string, { name: string; places: SchematicCase[] }>();
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    const id = place.formPosition.groupId?.trim();
    if (!id) continue;
    const name = place.formPosition.groupName?.trim() || id;
    const bucket = buckets.get(id) ?? { name, places: [] };
    bucket.places.push(place);
    buckets.set(id, bucket);
  }

  const targets: GroupManualTarget[] = [];
  for (const [groupId, { name, places }] of buckets) {
    const editable = places.find(
      (seat) => resolveGroupSeatInteraction(seat, { actions }).kind === 'place',
    );
    if (!editable) continue;
    targets.push({
      groupId,
      groupName: name,
      label: labelForGroup(name),
      place: editable,
    });
  }
  targets.sort((left, right) =>
    left.label.localeCompare(right.label, undefined, {
      numeric: true,
      sensitivity: 'base',
    }),
  );
  return targets;
}

export function resolveGroupManualPlace(
  cases: readonly SchematicCase[],
  groupId: string,
  actions: readonly string[],
): SchematicCase | null {
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    if (place.formPosition.groupId !== groupId) continue;
    if (resolveGroupSeatInteraction(place, { actions }).kind === 'place') {
      return place;
    }
  }
  return null;
}

/** Whether the group still accepts a manual place after hub refresh. */
export function groupManualPlaceStillAllowed(
  cases: readonly SchematicCase[],
  groupId: string,
  actions: readonly string[],
): boolean {
  return resolveGroupManualPlace(cases, groupId, actions) != null;
}

/** Editable empty seats that accept a manual place (Reserved excluded). */
export function groupBatchCap(
  cases: readonly SchematicCase[],
  groupId: string,
  actions: readonly string[],
): number {
  return cases.filter((place) => {
    if (place.formPosition.kind !== 'GroupPlace') return false;
    if (place.formPosition.groupId !== groupId) return false;
    if (place.seatMode !== 'Editable') return false;
    if (occupantId(place) != null) return false;
    return resolveGroupSeatInteraction(place, { actions }).kind === 'place';
  }).length;
}

/** Selected ids in display order (pot, then composition). */
export function orderBatchSelection(
  available: readonly GroupPlacementChoice[],
  selectedIds: readonly string[],
): string[] {
  const selected = new Set(selectedIds);
  return available
    .filter((row) => selected.has(row.entryId))
    .map((row) => row.entryId);
}

export function purgeUnavailableSelection(
  selectedIds: readonly string[],
  available: readonly GroupPlacementChoice[],
): { ids: string[]; removedCount: number } {
  const allowed = new Set(available.map((row) => row.entryId));
  const ids = selectedIds.filter((id) => allowed.has(id));
  return { ids, removedCount: selectedIds.length - ids.length };
}

export function resolveSeatLive(
  cases: readonly SchematicCase[],
  groupId: string,
  index: number,
): SchematicCase | null {
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    if (place.formPosition.groupId !== groupId) continue;
    if (place.formPosition.index !== index) continue;
    return place;
  }
  return null;
}

export function canBatchAssignToGroup(
  actions: readonly string[],
  placesPerGroup: number | null | undefined,
): boolean {
  return (
    actions.includes(assignEntriesToGroupAction) &&
    placesPerGroup != null &&
    placesPerGroup > 0
  );
}

export function canReplaceInGroup(actions: readonly string[]): boolean {
  return actions.includes(replaceEntryInGroupAction);
}

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
