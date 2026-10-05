import type { SchematicCase, StructureFormatKind } from '../../types';

function caseHasOccupant(place: SchematicCase): boolean {
  return Boolean(
    place.entry?.entryId?.trim() || place.assignment?.entryId?.trim(),
  );
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
    (c) => (c.formPosition.roundOrder ?? 0) === entryRound && caseHasOccupant(c),
  ).length;
}

function countGroupPlacesOccupied(cases: readonly SchematicCase[]): number {
  return cases.filter(
    (c) => c.formPosition.kind === 'GroupPlace' && caseHasOccupant(c),
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
  return null;
}
