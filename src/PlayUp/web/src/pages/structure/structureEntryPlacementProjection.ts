import {
  placeChromeLabel,
  placeDisplayLabel,
  type PlaceTranslate,
} from './structurePlaceLabel';
import type {
  SchematicCase,
  SchematicFormPosition,
  StructureFormatKind,
} from '../../types';

export type EntryPlaceProjection = {
  /** Short chrome labels (one per resolved schematic place). */
  labels: readonly string[];
  /** Optional long topology per label when distinct from chrome. */
  details: readonly (string | null)[];
};

function resolvedEntryId(place: SchematicCase): string | null {
  const id =
    place.assignment?.entryId?.trim() || place.entry?.entryId?.trim() || null;
  return id;
}

/**
 * Per-entry placement chrome (check, unplaced opacity) — Cup and Groups only.
 * Championship/Swiss: aggregate gauge only (Phase 0 matrix).
 */
export function showPerEntryPlacement(
  formatKind: StructureFormatKind | null | undefined,
): boolean {
  return formatKind === 'Cup' || formatKind === 'Groups';
}

/** Entry IDs with a nominative schematic place (not feed-only structural occupancy). */
export function placedEntryIdsFromSchematic(
  cases: readonly SchematicCase[],
  formatKind: StructureFormatKind | null | undefined,
): ReadonlySet<string> {
  const ids = new Set<string>();
  if (!showPerEntryPlacement(formatKind) || cases.length === 0) {
    return ids;
  }

  if (formatKind === 'Cup') {
    const cupCases = cases.filter((c) => c.formPosition.kind === 'CupSlot');
    if (cupCases.length === 0) {
      return ids;
    }
    const entryRound = Math.min(
      ...cupCases.map((c) => c.formPosition.roundOrder ?? 0),
    );
    for (const place of cupCases) {
      if ((place.formPosition.roundOrder ?? 0) !== entryRound) {
        continue;
      }
      const entryId = resolvedEntryId(place);
      if (entryId) {
        ids.add(entryId);
      }
    }
    return ids;
  }

  if (formatKind === 'Groups') {
    for (const place of cases) {
      if (place.formPosition.kind !== 'GroupPlace') {
        continue;
      }
      const entryId = resolvedEntryId(place);
      if (entryId) {
        ids.add(entryId);
      }
    }
  }

  return ids;
}

export function isEntryPlacedInPhase(
  entryId: string,
  placedIds: ReadonlySet<string>,
  formatKind: StructureFormatKind | null | undefined,
  schematicReady: boolean,
): boolean {
  if (!showPerEntryPlacement(formatKind) || !schematicReady) {
    return false;
  }
  return placedIds.has(entryId);
}

function groupPlaceShortLabel(
  formPosition: SchematicFormPosition,
  t: PlaceTranslate,
): string | null {
  if (formPosition.kind !== 'GroupPlace') {
    return null;
  }
  const name =
    formPosition.groupName?.trim() || formPosition.groupId?.trim() || null;
  if (!name) {
    return null;
  }
  const base = t('place.group', { name });
  const index = formPosition.index;
  if (index != null && index >= 1) {
    return `${base} · ${index}`;
  }
  return base;
}

function cupPlaceLabels(
  formPosition: SchematicFormPosition,
  t: PlaceTranslate,
): { short: string; detail: string | null } | null {
  const short = placeChromeLabel(formPosition);
  if (!short) {
    return null;
  }
  const long = placeDisplayLabel(formPosition, t);
  const detail = long && long !== short ? long : null;
  return { short, detail };
}

function addPlaceForEntry(
  map: Map<string, EntryPlaceProjection>,
  entryId: string,
  short: string,
  detail: string | null,
) {
  const existing = map.get(entryId);
  if (!existing) {
    map.set(entryId, { labels: [short], details: [detail] });
    return;
  }
  if (existing.labels.includes(short)) {
    return;
  }
  map.set(entryId, {
    labels: [...existing.labels, short],
    details: [...existing.details, detail],
  });
}

/** Same placement rules as `placedEntryIdsFromSchematic`, with display labels for tooltips. */
export function entryPlaceProjectionFromSchematic(
  cases: readonly SchematicCase[],
  formatKind: StructureFormatKind | null | undefined,
  t: PlaceTranslate,
): ReadonlyMap<string, EntryPlaceProjection> {
  const map = new Map<string, EntryPlaceProjection>();
  if (!showPerEntryPlacement(formatKind) || cases.length === 0) {
    return map;
  }

  if (formatKind === 'Cup') {
    const cupCases = cases.filter((c) => c.formPosition.kind === 'CupSlot');
    if (cupCases.length === 0) {
      return map;
    }
    const entryRound = Math.min(
      ...cupCases.map((c) => c.formPosition.roundOrder ?? 0),
    );
    for (const place of cupCases) {
      if ((place.formPosition.roundOrder ?? 0) !== entryRound) {
        continue;
      }
      const entryId = resolvedEntryId(place);
      if (!entryId) {
        continue;
      }
      const labels = cupPlaceLabels(place.formPosition, t);
      if (!labels) {
        continue;
      }
      addPlaceForEntry(map, entryId, labels.short, labels.detail);
    }
    return map;
  }

  if (formatKind === 'Groups') {
    for (const place of cases) {
      if (place.formPosition.kind !== 'GroupPlace') {
        continue;
      }
      const entryId = resolvedEntryId(place);
      if (!entryId) {
        continue;
      }
      const short = groupPlaceShortLabel(place.formPosition, t);
      if (!short) {
        continue;
      }
      addPlaceForEntry(map, entryId, short, null);
    }
  }

  return map;
}

export function primaryPlaceLabel(
  projection: EntryPlaceProjection | undefined,
): string | null {
  if (!projection || projection.labels.length === 0) {
    return null;
  }
  return projection.labels.join(' · ');
}

export function placeTooltipDetail(
  projection: EntryPlaceProjection | undefined,
): string | null {
  if (!projection || projection.labels.length === 0) {
    return null;
  }
  const parts: string[] = [];
  for (let i = 0; i < projection.labels.length; i += 1) {
    const short = projection.labels[i];
    const detail = projection.details[i];
    parts.push(detail ? `${short} — ${detail}` : short);
  }
  return parts.join(' · ');
}
