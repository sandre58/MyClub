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

function groupSeatBuckets(
  cases: readonly SchematicCase[],
): Map<string, { name: string; places: SchematicCase[] }> {
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
  return buckets;
}

function sortedGroupIds(
  buckets: Map<string, { name: string; places: SchematicCase[] }>,
  labelForGroup: (name: string) => string,
): string[] {
  return [...buckets.entries()]
    .sort((left, right) =>
      labelForGroup(left[1].name).localeCompare(
        labelForGroup(right[1].name),
        undefined,
        { numeric: true, sensitivity: 'base' },
      ),
    )
    .map(([id]) => id);
}

export type GroupStatusSuffix =
  'complete' | 'reserved' | 'match' | 'draw' | null;

/** Suffix for group pickers (batch): complete only when every seat is occupied. */
export function resolveGroupStatusSuffix(
  cases: readonly SchematicCase[],
  groupId: string,
  _placesPerGroup: number,
): GroupStatusSuffix {
  const seats = cases.filter(
    (place) =>
      place.formPosition.kind === 'GroupPlace' &&
      place.formPosition.groupId === groupId,
  );
  const empty = seats.filter((place) => occupantId(place) == null);
  if (empty.length === 0) {
    return 'complete';
  }
  if (empty.some((place) => place.seatMode === 'MatchLocked')) {
    return 'match';
  }
  if (empty.some((place) => place.seatMode === 'Reserved')) {
    return 'reserved';
  }
  if (empty.some((place) => place.seatMode === 'DrawPending')) {
    return 'draw';
  }
  return null;
}

export type GroupSelectTarget = {
  groupId: string;
  groupName: string;
  label: string;
  suffix: GroupStatusSuffix;
};

/** Every group in display order, including full or read-only groups (batch Select). */
export function listGroupSelectTargets(
  cases: readonly SchematicCase[],
  placesPerGroup: number,
  labelForGroup: (name: string) => string,
): GroupSelectTarget[] {
  const buckets = groupSeatBuckets(cases);
  return sortedGroupIds(buckets, labelForGroup).map((groupId) => {
    const { name } = buckets.get(groupId)!;
    return {
      groupId,
      groupName: name,
      label: labelForGroup(name),
      suffix: resolveGroupStatusSuffix(cases, groupId, placesPerGroup),
    };
  });
}

export function firstGroupIdWithBatchCap(
  cases: readonly SchematicCase[],
  actions: readonly string[],
  labelForGroup: (name: string) => string,
): string | null {
  for (const groupId of sortedGroupIds(
    groupSeatBuckets(cases),
    labelForGroup,
  )) {
    if (groupBatchCap(cases, groupId, actions) > 0) {
      return groupId;
    }
  }
  return null;
}

/** Whether the phase exposes at least one group with manual batch capacity. */
export function canOfferGroupManualPlacement(
  cases: readonly SchematicCase[],
  actions: readonly string[],
): boolean {
  for (const [groupId] of groupSeatBuckets(cases)) {
    if (groupBatchCap(cases, groupId, actions) > 0) {
      return true;
    }
  }
  return false;
}

/** Occupants of the selected group (display only in batch; not part of selection). */
export function listGroupMemberEntries(
  stage: GroupSeatStage,
  cases: readonly SchematicCase[],
  groupId: string,
  entries: readonly StructureEntry[],
): GroupPlacementChoice[] {
  const entryById = new Map(entries.map((entry) => [entry.entryId, entry]));
  const pots = stage.compositionEntryPots;
  const potById = new Map((pots ?? []).map((row) => [row.entryId, row.pot]));
  const members: GroupPlacementChoice[] = [];
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    if (place.formPosition.groupId !== groupId) continue;
    const id = occupantId(place);
    if (!id) continue;
    const entry = entryById.get(id);
    members.push({
      entryId: id,
      displayName:
        place.assignment?.displayName?.trim() ||
        place.entry?.displayName?.trim() ||
        entry?.displayName?.trim() ||
        entry?.shortName?.trim() ||
        id,
      logoMediaId: place.assignment?.logoMediaId ?? entry?.logoMediaId ?? null,
      primaryColor:
        place.assignment?.primaryColor ?? entry?.primaryColor ?? null,
      pot: pots == null ? null : (potById.get(id) ?? null),
    });
  }
  members.sort((left, right) =>
    left.displayName.localeCompare(right.displayName, undefined, {
      numeric: true,
      sensitivity: 'base',
    }),
  );
  const seen = new Set<string>();
  return members.filter((row) => {
    if (seen.has(row.entryId)) return false;
    seen.add(row.entryId);
    return true;
  });
}

export type OtherGroupPlacementChoice = GroupPlacementChoice & {
  groupLabel: string;
};

/** Teams placed in a group other than the one being edited (batch Q7). */
export function listOtherGroupEntries(
  stage: GroupSeatStage,
  cases: readonly SchematicCase[],
  excludeGroupId: string,
  entries: readonly StructureEntry[],
  labelForGroup: (name: string) => string,
): OtherGroupPlacementChoice[] {
  const entryById = new Map(entries.map((entry) => [entry.entryId, entry]));
  const pots = stage.compositionEntryPots;
  const potById = new Map((pots ?? []).map((row) => [row.entryId, row.pot]));
  const other: OtherGroupPlacementChoice[] = [];
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    const groupId = place.formPosition.groupId?.trim();
    if (!groupId || groupId === excludeGroupId) continue;
    const id = occupantId(place);
    if (!id) continue;
    const entry = entryById.get(id);
    const groupName = place.formPosition.groupName?.trim() || groupId;
    other.push({
      entryId: id,
      displayName:
        place.assignment?.displayName?.trim() ||
        place.entry?.displayName?.trim() ||
        entry?.displayName?.trim() ||
        entry?.shortName?.trim() ||
        id,
      logoMediaId: place.assignment?.logoMediaId ?? entry?.logoMediaId ?? null,
      primaryColor:
        place.assignment?.primaryColor ?? entry?.primaryColor ?? null,
      pot: pots == null ? null : (potById.get(id) ?? null),
      groupLabel: labelForGroup(groupName),
    });
  }
  other.sort((left, right) =>
    left.displayName.localeCompare(right.displayName, undefined, {
      numeric: true,
      sensitivity: 'base',
    }),
  );
  const seen = new Set<string>();
  return other.filter((row) => {
    if (seen.has(row.entryId)) return false;
    seen.add(row.entryId);
    return true;
  });
}

/** Empty seats in the group that carry an inbound feed (batch fact « réservées »). */
export function countGroupReservedFeedSeats(
  cases: readonly SchematicCase[],
  groupId: string,
): number {
  return cases.filter((place) => {
    if (place.formPosition.kind !== 'GroupPlace') return false;
    if (place.formPosition.groupId !== groupId) return false;
    if (occupantId(place) != null) return false;
    return place.feedOrigin?.kind != null;
  }).length;
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
  const buckets = groupSeatBuckets(cases);

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

/** Editable empty seats that accept a manual place (Reserved and inbound feeds excluded). */
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
    if (place.feedOrigin?.kind) return false;
    return resolveGroupSeatInteraction(place, { actions }).kind === 'place';
  }).length;
}

/** Group seat currently holding {@link entryId}, if any. */
export function findGroupSeatByEntryId(
  cases: readonly SchematicCase[],
  groupId: string,
  entryId: string,
): SchematicCase | null {
  for (const place of cases) {
    if (place.formPosition.kind !== 'GroupPlace') continue;
    if (place.formPosition.groupId !== groupId) continue;
    if (occupantId(place) === entryId) return place;
  }
  return null;
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
