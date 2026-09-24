import type { SchematicCase, SchematicFeedKind } from '../types';
import { placeChromeLabel } from './structurePlaceLabel';

/** How a Cup place responds to Placement manuel. */
export type ManualPlaceMode =
  | 'editable'
  | 'qualProg'
  | 'draw'
  | 'unavailable';

export function resolveManualPlaceMode(
  place: SchematicCase,
): ManualPlaceMode {
  if (place.formPosition.kind !== 'CupSlot' || !place.formPosition.slotKey) {
    return 'unavailable';
  }

  const feed = place.feedOrigin?.kind as SchematicFeedKind | undefined;
  if (feed === 'Qualification' || feed === 'Progression') {
    return 'qualProg';
  }
  if (feed === 'Draw') {
    return 'draw';
  }
  if (feed === 'Direct') {
    return 'editable';
  }
  // Empty place, or occupied without a structural/Draw feed → treat empty as editable;
  // occupied-without-Direct is not overwritable via Manuel (likely draw remnant).
  if (place.entry) {
    return 'unavailable';
  }
  return 'editable';
}

export type ManualPlaceOption = {
  slotKey: string;
  label: string;
  mode: ManualPlaceMode;
  entryId: string | null;
};

export function listCupManualPlaces(
  cases: readonly SchematicCase[],
): ManualPlaceOption[] {
  const options: ManualPlaceOption[] = [];
  for (const place of cases) {
    const slotKey = place.formPosition.slotKey?.trim();
    if (place.formPosition.kind !== 'CupSlot' || !slotKey) {
      continue;
    }
    options.push({
      slotKey,
      label: placeChromeLabel(place.formPosition) || slotKey,
      mode: resolveManualPlaceMode(place),
      entryId: place.entry?.entryId ?? place.assignment?.entryId ?? null,
    });
  }
  return options;
}

/** Entries already pinned on another editable/DA place (exclude current slot). */
export function occupiedEntryIds(
  places: readonly ManualPlaceOption[],
  exceptSlotKey?: string | null,
): Set<string> {
  const occupied = new Set<string>();
  for (const place of places) {
    if (!place.entryId) continue;
    if (exceptSlotKey && place.slotKey === exceptSlotKey) continue;
    occupied.add(place.entryId);
  }
  return occupied;
}
