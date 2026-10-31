import type { StructureResolutionOccupant } from '../../../types';

export type StructureResolutionOccupantIndex = {
  bySlotKey: ReadonlyMap<string, StructureResolutionOccupant>;
  byPopulationEntryId: ReadonlyMap<string, StructureResolutionOccupant>;
  /** Key: `${groupId}:${entryId}` */
  byGroupMember: ReadonlyMap<string, StructureResolutionOccupant>;
};

function groupMemberKey(groupId: string, entryId: string): string {
  return `${groupId}:${entryId}`;
}

export function buildStructureResolutionOccupantIndex(
  occupants: StructureResolutionOccupant[] | null | undefined,
): StructureResolutionOccupantIndex | null {
  if (occupants == null || occupants.length === 0) {
    return null;
  }

  const bySlotKey = new Map<string, StructureResolutionOccupant>();
  const byPopulationEntryId = new Map<string, StructureResolutionOccupant>();
  const byGroupMember = new Map<string, StructureResolutionOccupant>();

  for (const occupant of occupants) {
    switch (occupant.target) {
      case 'Slot': {
        const slotKey = occupant.slotKey?.trim();
        if (slotKey) {
          bySlotKey.set(slotKey, occupant);
        }
        break;
      }
      case 'Group': {
        const groupId = occupant.groupId?.trim();
        if (groupId) {
          byGroupMember.set(
            groupMemberKey(groupId, occupant.entryId),
            occupant,
          );
        }
        break;
      }
      case 'Population':
        byPopulationEntryId.set(occupant.entryId, occupant);
        break;
      default: {
        const _exhaustive: never = occupant.target;
        return _exhaustive;
      }
    }
  }

  return { bySlotKey, byPopulationEntryId, byGroupMember };
}

export function resolutionOccupantNeedsBadge(
  occupant: StructureResolutionOccupant | null | undefined,
): occupant is StructureResolutionOccupant {
  if (occupant == null) {
    return false;
  }
  return (
    occupant.state === 'Perime' ||
    occupant.state === 'Indetermine' ||
    occupant.state === 'NonVerifiee'
  );
}

export function lookupSlotResolutionOccupant(
  index: StructureResolutionOccupantIndex | null | undefined,
  slotKey: string | null | undefined,
): StructureResolutionOccupant | null {
  if (index == null || !slotKey?.trim()) {
    return null;
  }
  return index.bySlotKey.get(slotKey.trim()) ?? null;
}

export function lookupPopulationResolutionOccupant(
  index: StructureResolutionOccupantIndex | null | undefined,
  entryId: string | null | undefined,
): StructureResolutionOccupant | null {
  if (index == null || !entryId?.trim()) {
    return null;
  }
  return index.byPopulationEntryId.get(entryId.trim()) ?? null;
}

export function lookupGroupResolutionOccupant(
  index: StructureResolutionOccupantIndex | null | undefined,
  groupId: string | null | undefined,
  entryId: string | null | undefined,
): StructureResolutionOccupant | null {
  if (index == null || !groupId?.trim() || !entryId?.trim()) {
    return null;
  }
  return (
    index.byGroupMember.get(groupMemberKey(groupId.trim(), entryId.trim())) ??
    null
  );
}

export function lookupSchematicCaseResolutionOccupant(
  index: StructureResolutionOccupantIndex | null | undefined,
  formPosition: {
    kind: 'CupSlot' | 'GroupPlace' | 'RosterPlace';
    slotKey?: string | null;
    groupId?: string | null;
  },
  entryId: string | null | undefined,
): StructureResolutionOccupant | null {
  if (index == null) {
    return null;
  }
  if (formPosition.kind === 'GroupPlace') {
    return lookupGroupResolutionOccupant(index, formPosition.groupId, entryId);
  }
  if (formPosition.slotKey?.trim()) {
    return lookupSlotResolutionOccupant(index, formPosition.slotKey);
  }
  return lookupPopulationResolutionOccupant(index, entryId);
}

export function hasStaleTirageResolutionOccupant(
  occupants: StructureResolutionOccupant[] | null | undefined,
): boolean {
  return (occupants ?? []).some(
    (occupant) =>
      occupant.provenanceKind === 'Tirage' && occupant.state === 'Perime',
  );
}
