/**
 * Readable lines for hub `drawCreateLaunch.blockDetail`.
 * Known fragments name a group, a pot, or teams already on the hub.
 * Anything else is ignored.
 */

const GUID =
  '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}';

const POT_CONFLICT = new RegExp(
  `^group:(${GUID}):pot:(\\d+):entries:(${GUID}),(${GUID})$`,
);
const RESERVED = new RegExp(`^group:(${GUID}):reserved:(\\d+)$`);
const OUTSIDE = new RegExp(`^group:(${GUID}):entry:(${GUID})$`);
const CAPACITY = new RegExp(
  `^group:(${GUID}):occupants:(\\d+):capacity:(\\d+)$`,
);

export type DrawCreateBlockDetailNames = {
  groupNameById: ReadonlyMap<string, string>;
  teamNameById: ReadonlyMap<string, string>;
};

type Translate = (key: string, options?: Record<string, unknown>) => string;

export function drawCreateBlockDetailNameIndex(input: {
  groups?: readonly { id: string; name: string }[] | null;
  roster: readonly { entryId: string; displayName?: string | null }[];
}): DrawCreateBlockDetailNames {
  const groupNameById = new Map<string, string>();
  for (const group of input.groups ?? []) {
    const name = group.name.trim();
    if (group.id.length > 0 && name.length > 0) {
      groupNameById.set(group.id.toLowerCase(), name);
    }
  }

  const teamNameById = new Map<string, string>();
  addNames(
    teamNameById,
    input.roster.map((entry) => entry.entryId),
    input.roster.map((entry) => entry.displayName ?? ''),
  );
  return { groupNameById, teamNameById };
}

export function formatDrawCreateBlockDetailLines(
  fragments: readonly string[] | null | undefined,
  names: DrawCreateBlockDetailNames,
  t: Translate,
): string[] {
  if (fragments == null || fragments.length === 0) {
    return [];
  }

  const lines: string[] = [];
  for (const fragment of fragments) {
    const line = formatFragment(fragment, names, t);
    if (line != null) {
      lines.push(line);
    }
  }
  return lines;
}

function addNames(
  target: Map<string, string>,
  ids: readonly string[] | null | undefined,
  names: readonly string[] | null | undefined,
) {
  if (ids == null || names == null) {
    return;
  }
  const count = Math.min(ids.length, names.length);
  for (let index = 0; index < count; index += 1) {
    const id = ids[index];
    const name = names[index]?.trim();
    if (id && name) {
      target.set(id.toLowerCase(), name);
    }
  }
}

function groupLabel(name: string, t: Translate): string {
  return t('place.group', { name });
}

function named(
  map: ReadonlyMap<string, string>,
  id: string,
): string | undefined {
  return map.get(id.toLowerCase());
}

function formatFragment(
  fragment: string,
  names: DrawCreateBlockDetailNames,
  t: Translate,
): string | null {
  const pot = POT_CONFLICT.exec(fragment);
  if (pot) {
    const group = named(names.groupNameById, pot[1]);
    const teamA = named(names.teamNameById, pot[3]);
    const teamB = named(names.teamNameById, pot[4]);
    if (!group || !teamA || !teamB) {
      return null;
    }
    return t('fiche.drawWorkflow.createBlocked.named.potConflict', {
      group: groupLabel(group, t),
      pot: pot[2],
      teamA,
      teamB,
    });
  }

  const reserved = RESERVED.exec(fragment);
  if (reserved) {
    const group = named(names.groupNameById, reserved[1]);
    if (!group) {
      return null;
    }
    return t('fiche.drawWorkflow.createBlocked.named.reserved', {
      group: groupLabel(group, t),
      count: Number(reserved[2]),
    });
  }

  const outside = OUTSIDE.exec(fragment);
  if (outside) {
    const group = named(names.groupNameById, outside[1]);
    const team = named(names.teamNameById, outside[2]);
    if (!group || !team) {
      return null;
    }
    return t('fiche.drawWorkflow.createBlocked.named.outside', {
      group: groupLabel(group, t),
      team,
    });
  }

  const capacity = CAPACITY.exec(fragment);
  if (capacity) {
    const group = named(names.groupNameById, capacity[1]);
    if (!group) {
      return null;
    }
    return t('fiche.drawWorkflow.createBlocked.named.capacity', {
      group: groupLabel(group, t),
      occupants: capacity[2],
      capacity: capacity[3],
    });
  }

  return null;
}
