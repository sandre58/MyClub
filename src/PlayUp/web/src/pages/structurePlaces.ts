import type { StructureStageHubSummary } from '../types';

/**
 * Places N (target cardinality at T) — topology, hero, Entries rail.
 * Prefer server compositionCapacity (ResolvePlaces). Client fallbacks use form facts only.
 * Never invent N from k / occupants / Draw alone.
 *
 * Cup: entry places (1st-round). Multi-round total slotCount is form units, not Places.
 */
export function resolvePlacesN(stage: StructureStageHubSummary): number | null {
  if (stage.compositionCapacity != null && stage.compositionCapacity > 0) {
    return stage.compositionCapacity;
  }

  const kind = stage.formatKind;
  if (kind === 'Cup' || (kind == null && (stage.slotCount ?? 0) > 0)) {
    return resolveCupEntryPlacesFallback(stage.slotCount ?? 0, stage.roundCount ?? 0);
  }

  if (kind === 'Groups' || (stage.groupCount ?? 0) > 0) {
    const groups = stage.groupCount ?? 0;
    const perGroup = stage.placesPerGroup ?? stage.numberOfPots;
    if (groups > 0 && perGroup != null && perGroup >= 2) {
      return groups * perGroup;
    }
  }

  return null;
}

/** Mirror of Application ResolveCupEntryPlaces when compositionCapacity is absent. */
export function resolveCupEntryPlacesFallback(
  slotCount: number,
  roundCount: number,
): number | null {
  if (slotCount <= 0) {
    return null;
  }
  if (roundCount <= 1) {
    return slotCount;
  }

  const fullTreeSlots = 2 ** (roundCount + 1) - 2;
  if (slotCount === fullTreeSlots) {
    return 2 ** roundCount;
  }

  const firstRoundSlots = 2 ** roundCount;
  if (slotCount === firstRoundSlots) {
    return firstRoundSlots;
  }

  if (slotCount >= 2 && (slotCount + 2) % 2 === 0) {
    const entryPlaces = (slotCount + 2) / 2;
    if (entryPlaces >= 2 && Number.isInteger(Math.log2(entryPlaces))) {
      return entryPlaces;
    }
  }

  return null;
}

/**
 * Structural places per group for Groups schematic / topology grid.
 * Never invents 1 when capacity is unknown.
 */
export function resolvePlacesPerGroup(
  stage: StructureStageHubSummary,
): number | null {
  const groups = stage.groupCount ?? 0;
  if (groups <= 0) {
    return null;
  }

  if (stage.placesPerGroup != null && stage.placesPerGroup >= 2) {
    return stage.placesPerGroup;
  }

  const places = resolvePlacesN(stage);
  if (places != null && places > 0 && places % groups === 0) {
    return places / groups;
  }

  const pots = stage.numberOfPots;
  if (pots != null && pots >= 2) {
    return pots;
  }

  const occupied = stage.teamCount;
  if (occupied > 0 && occupied % groups === 0) {
    return occupied / groups;
  }

  return null;
}
