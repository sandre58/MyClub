// -----------------------------------------------------------------------
// Place-map assignment model (Qual + Prog).
// `assignments[i]` is the place id for source i. Writes stay on that index.
// -----------------------------------------------------------------------

import {
  fillEmptyPlaceKeysAllowingReuse,
  fillEmptyPlaceSlotKeys,
} from './structurePlaceMapping';

/** Cup density: section the board by round / block from this many places. */
export const PLACE_MAP_SECTION_MIN = 8;

/** Cup density: scroll the board only; the source tray stays outside. */
export const PLACE_MAP_SCROLL_MIN = 16;

/** Visual and keyboard columns. Kept in lockstep (no separate mobile grid). */
export const PLACE_MAP_COLUMNS = 2;

export type PlaceMapSection<T> = {
  label: string | null;
  places: T[];
};

export function placeMapBoardScrolls(placeCount: number): boolean {
  return placeCount >= PLACE_MAP_SCROLL_MIN;
}

/**
 * Group destination cells for the board.
 * Below the section threshold, or when no round/block label exists, one flat group.
 */
export function groupPlaceMapSections<
  T extends { sectionLabel?: string | null },
>(places: readonly T[], otherLabel: string): PlaceMapSection<T>[] {
  if (places.length < PLACE_MAP_SECTION_MIN) {
    return [{ label: null, places: [...places] }];
  }
  if (!places.some((place) => place.sectionLabel?.trim())) {
    return [{ label: null, places: [...places] }];
  }

  const order: string[] = [];
  const buckets = new Map<string, T[]>();
  for (const place of places) {
    const label = place.sectionLabel?.trim() || otherLabel;
    let bucket = buckets.get(label);
    if (!bucket) {
      bucket = [];
      buckets.set(label, bucket);
      order.push(label);
    }
    bucket.push(place);
  }
  return order.map((label) => ({
    label,
    places: buckets.get(label) ?? [],
  }));
}

export function chunkPlaceMapRows<T>(
  places: readonly T[],
  columns: number,
): T[][] {
  const size = Math.max(1, columns);
  const rows: T[][] = [];
  for (let index = 0; index < places.length; index += size) {
    rows.push(places.slice(index, index + size));
  }
  return rows;
}

/** Source-indexed keys, padded to `sourceCount`. */
export function alignPlaceAssignments(
  assignments: readonly string[],
  sourceCount: number,
): string[] {
  return Array.from(
    { length: Math.max(0, sourceCount) },
    (_, index) => assignments[index] ?? '',
  );
}

/**
 * Write `placeId` onto source `sourceIndex`.
 * Cup/slot (`uniquePlaces`) refuses a place id already used by another source.
 * Returns null when the write is rejected. Never reindexes by destination.
 */
export function assignPlaceToSource(
  assignments: readonly string[],
  sourceCount: number,
  sourceIndex: number,
  placeId: string,
  uniquePlaces: boolean,
): string[] | null {
  if (sourceIndex < 0 || sourceIndex >= sourceCount) return null;
  const id = placeId.trim();
  if (!id) return null;
  const next = alignPlaceAssignments(assignments, sourceCount);
  if (
    uniquePlaces &&
    next.some((key, index) => index !== sourceIndex && key.trim() === id)
  ) {
    return null;
  }
  next[sourceIndex] = id;
  return next;
}

/** Clear the place id at `sourceIndex` (the source returns to the tray). */
export function clearSourceAssignment(
  assignments: readonly string[],
  sourceCount: number,
  sourceIndex: number,
): string[] {
  const next = alignPlaceAssignments(assignments, sourceCount);
  if (sourceIndex >= 0 && sourceIndex < sourceCount) {
    next[sourceIndex] = '';
  }
  return next;
}

export function canFillPlaceMap(
  assignments: readonly string[],
  placeIds: readonly string[],
  uniquePlaces: boolean,
): boolean {
  if (!assignments.some((key) => !key.trim())) return false;
  const ids = placeIds.map((id) => id.trim()).filter((id) => id.length > 0);
  if (ids.length === 0) return false;
  if (!uniquePlaces) return true;
  const used = new Set(
    assignments.map((key) => key.trim()).filter((key) => key.length > 0),
  );
  return ids.some((id) => !used.has(id));
}

/**
 * Fill empty source indexes in view order.
 * Slot grain skips ids already used. Group grain may reuse.
 * A non-empty index is left unchanged.
 */
export function fillPlaceMap(
  assignments: readonly string[],
  placeIds: readonly string[],
  uniquePlaces: boolean,
): string[] {
  const ids = placeIds.map((id) => id.trim()).filter((id) => id.length > 0);
  const current = assignments.map((key) => key ?? '');
  return uniquePlaces
    ? fillEmptyPlaceSlotKeys(current, ids)
    : fillEmptyPlaceKeysAllowingReuse(current, ids);
}
