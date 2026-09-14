import type { StructureStageHubSummary } from '../types';

/**
 * Places N (target cardinality at T) — topology, hero, Entrées rail.
 * Prefer server compositionCapacity (ResolvePlaces). Client fallbacks use form facts only.
 * Never invent N from k / occupants / Draw alone.
 */
export function resolvePlacesN(stage: StructureStageHubSummary): number | null {
  if (stage.compositionCapacity != null && stage.compositionCapacity > 0) {
    return stage.compositionCapacity;
  }

  const kind = stage.formatKind;
  if (kind === 'Cup' || (kind == null && (stage.slotCount ?? 0) > 0)) {
    const slots = stage.slotCount ?? 0;
    return slots > 0 ? slots : null;
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
