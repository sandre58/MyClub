import type { SchematicCase, StructureFormatKind } from '../../types';

function caseHasResolvedTeam(place: SchematicCase): boolean {
  return Boolean(
    place.entry?.entryId?.trim() || place.assignment?.entryId?.trim(),
  );
}

/** Place holds a team, draw slot, or Qual/Prog construction (matches schematic chrome). */
function casePlaceIsOccupied(place: SchematicCase): boolean {
  if (caseHasResolvedTeam(place)) {
    return true;
  }
  return Boolean(place.feedOrigin?.kind);
}

function countCupEntryRoundOccupied(cases: readonly SchematicCase[]): number {
  const cupCases = cases.filter((c) => c.formPosition.kind === 'CupSlot');
  if (cupCases.length === 0) {
    return 0;
  }
  const entryRound = Math.min(
    ...cupCases.map((c) => c.formPosition.roundOrder ?? 0),
  );
  return cupCases.filter(
    (c) => (c.formPosition.roundOrder ?? 0) === entryRound && casePlaceIsOccupied(c),
  ).length;
}

function countGroupPlacesOccupied(cases: readonly SchematicCase[]): number {
  return cases.filter(
    (c) => c.formPosition.kind === 'GroupPlace' && casePlaceIsOccupied(c),
  ).length;
}

/**
 * Occupied places vs Places N from schematic (entry round for Cup).
 * Returns null when capacity is unknown; caller should gate on schematic success.
 */
export function countSchematicOccupiedPlaces(
  cases: readonly SchematicCase[],
  formatKind: StructureFormatKind | null | undefined,
  placesN: number | null,
): number | null {
  if (placesN == null || placesN <= 0) {
    return null;
  }
  if (formatKind === 'Cup') {
    return countCupEntryRoundOccupied(cases);
  }
  if (formatKind === 'Groups') {
    return countGroupPlacesOccupied(cases);
  }
  if (formatKind === 'Championship' || formatKind === 'Swiss') {
    return cases.filter(
      (c) => c.formPosition.kind === 'RosterPlace' && casePlaceIsOccupied(c),
    ).length;
  }
  return null;
}
