import type {
  SchematicCase,
  StageDrawGroupPlacement,
  StageSchematic,
} from '../../../types';
import { compareGroupLabels, groupPlacementRows } from '../draw/drawUi';

/** One placed team inside a groups schematic card. */
export type GroupsSchematicEntry = {
  id: string;
  displayName: string;
  logoMediaId?: string | null;
  primaryColor?: string | null;
  /**
   * 0-based seat when empty places sit between teams.
   * Omitted entries pack from the first seat; the rest of `capacity` are ghosts.
   */
  seat?: number;
};

/** Presentational groups × teams model. Ghosts fill `capacity` beyond entries. */
export type GroupsSchematicGroup = {
  id: string;
  name: string;
  capacity: number;
  entries: GroupsSchematicEntry[];
};

function resolvedTeam(place: SchematicCase): GroupsSchematicEntry | null {
  const id = place.assignment?.entryId ?? place.entry?.entryId;
  const displayName =
    place.assignment?.displayName?.trim() ||
    place.entry?.displayName?.trim() ||
    '';
  if (!id || !displayName) {
    return null;
  }
  return {
    id,
    displayName,
    logoMediaId: place.assignment?.logoMediaId,
    primaryColor: place.assignment?.primaryColor,
  };
}

/**
 * Phase sheet adapter. `casesByGroupId` keeps each seat's source case so the
 * sheet can keep feed labels, tooltips, and withdrawn chrome. The model itself
 * only lists resolved teams; other seats are ghosts.
 */
export function groupsSchematicFromStageSchematic(
  schematic: Pick<StageSchematic, 'cases'>,
  groupLabel: (name: string) => string,
): {
  groups: GroupsSchematicGroup[];
  casesByGroupId: ReadonlyMap<string, SchematicCase[]>;
} {
  const byGroup = new Map<string, SchematicCase[]>();
  for (const place of schematic.cases) {
    const id = place.formPosition.groupId ?? '';
    const list = byGroup.get(id) ?? [];
    list.push(place);
    byGroup.set(id, list);
  }

  const casesByGroupId = new Map<string, SchematicCase[]>();
  const groups = [...byGroup.entries()]
    .map(([id, cases]) => {
      const ordered = [...cases].sort(
        (a, b) => (a.formPosition.index ?? 0) - (b.formPosition.index ?? 0),
      );
      casesByGroupId.set(id, ordered);
      const rawName = ordered[0]?.formPosition.groupName ?? id;
      const entries: GroupsSchematicEntry[] = [];
      ordered.forEach((place, seat) => {
        const team = resolvedTeam(place);
        if (team) {
          entries.push({ ...team, seat });
        }
      });
      return {
        id,
        name: groupLabel(rawName),
        capacity: ordered.length,
        entries,
      };
    })
    .sort((a, b) => compareGroupLabels(a.name, b.name));

  return { groups, casesByGroupId };
}

/**
 * Draw dialog adapter. Fed by the draft (or applied) group placements plus
 * the phase groups. Empty seats up to `placesPerGroup` render as ghosts.
 */
export function groupsSchematicFromDrawDraft(input: {
  placements: readonly StageDrawGroupPlacement[];
  stageGroups: readonly { id: string; name: string }[];
  placesPerGroup: number | null | undefined;
  unknownEntry: string;
  unknownGroup: string;
  groupLabel: (name: string) => string;
}): GroupsSchematicGroup[] {
  const rows = groupPlacementRows(
    [...input.placements],
    input.unknownEntry,
    input.unknownGroup,
  );
  const rowById = new Map(rows.map((row) => [row.groupId, row]));
  const stageById = new Map(
    input.stageGroups.map((group) => [group.id, group]),
  );
  const ids = new Set<string>([
    ...input.stageGroups.map((group) => group.id),
    ...rows.map((row) => row.groupId),
  ]);

  const perGroup =
    input.placesPerGroup != null && input.placesPerGroup > 0
      ? input.placesPerGroup
      : null;

  return [...ids]
    .map((id) => {
      const row = rowById.get(id);
      const stageGroup = stageById.get(id);
      const rawName =
        stageGroup?.name?.trim() || row?.groupLabel || input.unknownGroup;
      const entries: GroupsSchematicEntry[] = (row?.entries ?? []).map(
        (entry) => ({
          id: entry.entryId,
          displayName: entry.displayName,
          logoMediaId: entry.logoMediaId,
          primaryColor: entry.primaryColor,
        }),
      );
      const capacity =
        perGroup != null ? Math.max(perGroup, entries.length) : entries.length;
      return {
        id,
        name: input.groupLabel(rawName),
        capacity,
        entries,
      };
    })
    .filter((group) => group.capacity > 0 || group.entries.length > 0)
    .sort((a, b) => compareGroupLabels(a.name, b.name));
}

export function groupsSchematicEntryAt(
  group: GroupsSchematicGroup,
  seat: number,
): GroupsSchematicEntry | undefined {
  if (group.entries.some((entry) => entry.seat != null)) {
    return group.entries.find((entry) => entry.seat === seat);
  }
  return group.entries[seat];
}
