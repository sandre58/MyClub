import type { DropDownButtonItem } from '../../../design-system/components/DropDownButton';
import type {
  SchematicCase,
  SchematicFeedKind,
  StructureEntry,
} from '../../../types';
import { placeChromeLabel } from './structurePlaceLabel';

/** How a Cup place responds to manual placement. */
export type ManualPlaceMode = 'editable' | 'qualProg' | 'draw' | 'unavailable';

/** Enough of a phase to decide whether its path may be overwritten. */
export type ManualPlacementStage = {
  stageId: string;
  status?: string | null;
};

/**
 * A qualification or progression path may be overwritten only when its source
 * is definitive: completed, or absent from the loaded phase list (deleted).
 * Omitting the list keeps the path reserved, including for a partial read.
 */
export function sourceAllowsManualOverwrite(
  sourceStageId: string | null | undefined,
  stages: readonly ManualPlacementStage[] | undefined,
): boolean {
  if (!sourceStageId || !stages) {
    return false;
  }
  const source = stages.find((stage) => stage.stageId === sourceStageId);
  if (!source) {
    return true;
  }
  return source.status === 'Completed';
}

export function resolveManualPlaceMode(
  place: SchematicCase,
  stages?: readonly ManualPlacementStage[],
): ManualPlaceMode {
  if (place.formPosition.kind !== 'CupSlot' || !place.formPosition.slotKey) {
    return 'unavailable';
  }

  const feed = place.feedOrigin?.kind as SchematicFeedKind | undefined;
  if (feed === 'Qualification' || feed === 'Progression') {
    return sourceAllowsManualOverwrite(place.feedOrigin?.sourceStageId, stages)
      ? 'editable'
      : 'qualProg';
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
  stages?: readonly ManualPlacementStage[],
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
      mode: resolveManualPlaceMode(place, stages),
      entryId: place.entry?.entryId ?? place.assignment?.entryId ?? null,
    });
  }
  return options;
}

/** Entries already pinned on another editable/DA place (exclude current slot). */
export type CupManualPlacementMenuItem = DropDownButtonItem & {
  slotKey: string;
};

export function listCupManualPlacementMenuItems(
  cases: readonly SchematicCase[],
  stages: readonly ManualPlacementStage[] | undefined,
  entries: readonly StructureEntry[],
  labels: {
    free: string;
    occupiedPrimary: (placeLabel: string) => string;
    occupiedAria: (placeLabel: string, teamName: string) => string;
  },
): CupManualPlacementMenuItem[] {
  const entryById = new Map(entries.map((entry) => [entry.entryId, entry]));
  const items: CupManualPlacementMenuItem[] = [];
  for (const place of listCupManualPlaces(cases, stages)) {
    const occupiedId = place.entryId;
    if (occupiedId) {
      const entry = entryById.get(occupiedId);
      const teamName =
        entry?.displayName?.trim() || entry?.shortName?.trim() || labels.free;
      items.push({
        slotKey: place.slotKey,
        value: `cup:${place.slotKey}`,
        label: labels.occupiedPrimary(place.label),
        secondary: teamName,
        'aria-label': labels.occupiedAria(place.label, teamName),
        disabled: place.mode !== 'editable',
      });
      continue;
    }
    items.push({
      slotKey: place.slotKey,
      value: `cup:${place.slotKey}`,
      label: `${place.label} - ${labels.free}`,
      secondary: labels.free,
      'aria-label': `${place.label} — ${labels.free}`,
      disabled: place.mode !== 'editable',
    });
  }
  return items;
}

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
