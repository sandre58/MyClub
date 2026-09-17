// -----------------------------------------------------------------------
// U4 — Place address labels (Cup).
// Long label (rail/dialog) = topology; chrome (schematic) = SlotKey (C2).
// Backend supplies structural facts; SPA localizes side only (not RoundName).
// -----------------------------------------------------------------------

import type {
  SchematicCase,
  SchematicFormPosition,
  StageSchematic,
} from '../types';

export type PlaceTranslate = (
  key: string,
  opts?: Record<string, unknown>,
) => string;

/** Stable API identity for Progression Place (Domain ForSlot). */
export function cupPlaceApiIdentity(
  formPosition: SchematicFormPosition,
): string | null {
  const slot = formPosition.slotKey?.trim();
  return slot || null;
}

/**
 * Chrome schématique (C2) — SlotKey Domain when present; else compact topology.
 * Never invents tour abbreviations (8e / DF).
 */
export function placeChromeLabel(
  formPosition: SchematicFormPosition,
): string | null {
  if (formPosition.kind !== 'CupSlot') return null;
  const slot = cupPlaceApiIdentity(formPosition);
  if (slot) return slot;
  const side = normalizeSide(formPosition.side);
  if (!side) return null;
  const ordinal = formPosition.pairOrdinal;
  if (ordinal != null && ordinal >= 1) {
    return `${ordinal}·${side}`;
  }
  return side;
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
 * `{Tour} {ordinal} · côté {A|B}` or `{Tour} · côté {A|B}`.
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

export function areProgressionPlacesLabeled(
  schematic: StageSchematic | null | undefined,
): boolean {
  if (!schematic || schematic.formatKind !== 'Cup') return false;
  return schematic.cases.some((c) => isCupPlaceTargetable(c.formPosition));
}

export type LabeledCupPlace = {
  apiIdentity: string;
  label: string;
};

/** Targetable Cup places for Progression dialog (SlotKey + long label when possible). */
export function listLabeledCupPlaces(
  schematic: StageSchematic | null | undefined,
  t: PlaceTranslate,
): LabeledCupPlace[] {
  if (!schematic || schematic.formatKind !== 'Cup') return [];
  const out: LabeledCupPlace[] = [];
  const seen = new Set<string>();
  for (const c of schematic.cases) {
    if (!isCupPlaceTargetable(c.formPosition)) continue;
    const id = cupPlaceApiIdentity(c.formPosition)!;
    if (seen.has(id)) continue;
    seen.add(id);
    const label = placeDisplayLabel(c.formPosition, t);
    if (!label) continue;
    out.push({ apiIdentity: id, label });
  }
  return out;
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
