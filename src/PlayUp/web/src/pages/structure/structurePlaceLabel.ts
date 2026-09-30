// -----------------------------------------------------------------------
// Place address labels — Cup (Slot), Groups A1 (Group), Champ/Swiss (Form).
// UX Place = Placement destination picker (Slot | Group | Form).
// Long label (rail/dialog) = topology; chrome (schematic) = SlotKey (C2).
// Backend supplies structural facts; SPA localizes side only (not RoundName).
// -----------------------------------------------------------------------

import type {
  SchematicCase,
  SchematicFormPosition,
  StageSchematic,
  StructureFormatKind,
} from '../../types';

export type PlaceTranslate = (
  key: string,
  opts?: Record<string, unknown>,
) => string;

/** Place destination grain: Cup SlotKey, Groups poule, or Forme. */
export type PlaceGrain = 'slot' | 'group' | 'form';

/** Maps structure format to Place grain (null when not Place-eligible). */
export function placeGrainForFormat(
  formatKind: StructureFormatKind | string | null | undefined,
): PlaceGrain | null {
  switch (formatKind) {
    case 'Cup':
      return 'slot';
    case 'Groups':
      return 'group';
    case 'Championship':
    case 'Swiss':
      return 'form';
    default:
      return null;
  }
}

/** Stable API identity for Progression Place (Domain ForSlot). */
export function cupPlaceApiIdentity(
  formPosition: SchematicFormPosition,
): string | null {
  const slot = formPosition.slotKey?.trim();
  return slot || null;
}

/**
 * Schematic Place chrome — Domain SlotKey only (I2).
 * Never invents `ordinal·side` as a Place address (that is confrontation identity).
 */
export function placeChromeLabel(
  formPosition: SchematicFormPosition,
): string | null {
  if (formPosition.kind !== 'CupSlot') return null;
  return cupPlaceApiIdentity(formPosition);
}

/** Long-label facts: RoundName + side (topology or fixture-linked). */
export function hasCupPlaceAddressFacts(
  formPosition: SchematicFormPosition,
): boolean {
  if (formPosition.kind !== 'CupSlot') return false;
  const round = formPosition.roundName?.trim();
  const side = normalizeSide(formPosition.side);
  return !!round && !!side;
}

export function isCupPlaceTargetable(
  formPosition: SchematicFormPosition,
): boolean {
  return !!cupPlaceApiIdentity(formPosition);
}

/**
 * Long recipe for rail / dialog:
 * `{Round} {ordinal} · side {A|B}` or `{Round} · side {A|B}`.
 */
export function placeDisplayLabel(
  formPosition: SchematicFormPosition,
  t: PlaceTranslate,
): string | null {
  if (!hasCupPlaceAddressFacts(formPosition)) {
    return placeChromeLabel(formPosition);
  }
  const roundName = formPosition.roundName!.trim();
  const side = normalizeSide(formPosition.side)!;
  const sideText = t('place.side', { side });
  const ordinal = formPosition.pairOrdinal;
  if (ordinal != null && ordinal >= 1) {
    return t('place.cupWithOrdinal', {
      round: roundName,
      ordinal,
      side: sideText,
    });
  }
  return t('place.cup', { round: roundName, side: sideText });
}

/** True when the schematic exposes at least one Place-targetable address. */
export function areProgressionPlacesLabeled(
  schematic: StageSchematic | null | undefined,
): boolean {
  if (!schematic) return false;
  if (schematic.formatKind === 'Cup') {
    return schematic.cases.some((c) => isCupPlaceTargetable(c.formPosition));
  }
  if (schematic.formatKind === 'Groups') {
    return schematic.cases.some((c) => !!c.formPosition.groupId?.trim());
  }
  if (
    schematic.formatKind === 'Championship' ||
    schematic.formatKind === 'Swiss'
  ) {
    // Form grain — the Forme itself is the Place; no RosterPlace invented.
    return true;
  }
  return false;
}

/**
 * Downstream peers whose form currently exposes addressable Places
 * (Cup slots, Groups pools, or Champ/Swiss form).
 */
export function filterPlaceEligiblePeers<T extends { stageId: string }>(
  peers: readonly T[],
  schematicById: ReadonlyMap<string, StageSchematic | undefined>,
): T[] {
  return peers.filter((peer) =>
    areProgressionPlacesLabeled(schematicById.get(peer.stageId)),
  );
}

export type LabeledPlace = {
  apiIdentity: string;
  /** Chrome / select label (SlotKey or localized group title). */
  label: string;
  /** Long topology label for dialog description when distinct from chrome. */
  description: string | null;
  grain: PlaceGrain;
};

/** Targetable Cup places for Progression dialog (chrome = SlotKey; long = description). */
export function listLabeledCupPlaces(
  schematic: StageSchematic | null | undefined,
  t: PlaceTranslate,
): LabeledPlace[] {
  if (!schematic || schematic.formatKind !== 'Cup') return [];
  const out: LabeledPlace[] = [];
  const seen = new Set<string>();
  for (const c of schematic.cases) {
    if (!isCupPlaceTargetable(c.formPosition)) continue;
    const id = cupPlaceApiIdentity(c.formPosition)!;
    if (seen.has(id)) continue;
    seen.add(id);
    const chrome = placeChromeLabel(c.formPosition) ?? id;
    const long = placeDisplayLabel(c.formPosition, t);
    out.push({
      apiIdentity: id,
      label: chrome,
      description: long && long !== chrome ? long : null,
      grain: 'slot',
    });
  }
  return out;
}

/**
 * Place picker options for the destination schematic.
 * Cup → SlotKeys; Groups → groupId; Champ/Swiss → single Form option.
 */
export function listLabeledPlaces(
  schematic: StageSchematic | null | undefined,
  t: PlaceTranslate,
): LabeledPlace[] {
  if (!schematic) return [];
  if (schematic.formatKind === 'Cup') {
    return listLabeledCupPlaces(schematic, t);
  }
  if (schematic.formatKind === 'Groups') {
    return listLabeledGroupPlaces(schematic, t);
  }
  if (
    schematic.formatKind === 'Championship' ||
    schematic.formatKind === 'Swiss'
  ) {
    return listLabeledFormPlaces(schematic, t);
  }
  return [];
}

/** Groups A1 — one Place option per stable groupId (duplicates allowed at map time). */
export function listLabeledGroupPlaces(
  schematic: StageSchematic | null | undefined,
  t: PlaceTranslate,
): LabeledPlace[] {
  if (!schematic || schematic.formatKind !== 'Groups') return [];
  const out: LabeledPlace[] = [];
  const seen = new Set<string>();
  for (const c of schematic.cases) {
    const id = c.formPosition.groupId?.trim();
    if (!id || seen.has(id)) continue;
    seen.add(id);
    const name = c.formPosition.groupName?.trim() || id;
    out.push({
      apiIdentity: id,
      label: t('place.group', { name }),
      description: null,
      grain: 'group',
    });
  }
  return out;
}

/** Champ/Swiss — one Form Place (Domain ForForm); never invents RosterPlace addresses. */
export function listLabeledFormPlaces(
  schematic: StageSchematic | null | undefined,
  t: PlaceTranslate,
): LabeledPlace[] {
  if (
    !schematic ||
    (schematic.formatKind !== 'Championship' &&
      schematic.formatKind !== 'Swiss')
  ) {
    return [];
  }
  return [
    {
      apiIdentity: schematic.stageId,
      label: t('place.form'),
      description: null,
      grain: 'form',
    },
  ];
}

export function findCaseByPlaceIdentity(
  schematic: StageSchematic | null | undefined,
  identity: string | null | undefined,
): SchematicCase | null {
  const key = identity?.trim();
  if (!schematic || !key) return null;
  return (
    schematic.cases.find((c) => cupPlaceApiIdentity(c.formPosition) === key) ??
    null
  );
}

/** Rail / summary chip — same id as schematic chrome (C2), not the long topology string. */
export function placeChromeForDestinationSlotKey(
  schematic: StageSchematic | null | undefined,
  destinationSlotKey: string | null | undefined,
): string | null {
  const c = findCaseByPlaceIdentity(schematic, destinationSlotKey);
  return c ? placeChromeLabel(c.formPosition) : null;
}

/** Long topology label (dialog description / title tooltip). */
export function placeLabelForDestinationSlotKey(
  schematic: StageSchematic | null | undefined,
  destinationSlotKey: string | null | undefined,
  t: PlaceTranslate,
): string | null {
  const c = findCaseByPlaceIdentity(schematic, destinationSlotKey);
  return c ? placeDisplayLabel(c.formPosition, t) : null;
}

function normalizeSide(side: string | null | undefined): 'A' | 'B' | null {
  const s = side?.trim().toUpperCase();
  if (s === 'A' || s === 'B') return s;
  return null;
}
