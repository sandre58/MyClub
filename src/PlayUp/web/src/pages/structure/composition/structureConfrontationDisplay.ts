/**
 * Structure UX — confrontation labels.
 *
 * - Cup / bracket: {@link PairKey} alone (`P1`, …) is the organizer-facing identity.
 * - `Match #n` is a secondary label for championship, groups, or fixture-only paths
 *   when no PairKey is available — never combined with PairKey on the same line.
 */

export function structureMatchNumberInContainerVisible(
  pairKey: string | null | undefined,
  matchNumberInContainer: number | null | undefined,
): number | null {
  if (pairKey?.trim()) {
    return null;
  }
  if (matchNumberInContainer != null && matchNumberInContainer > 0) {
    return matchNumberInContainer;
  }
  return null;
}

export function structureProgressionSourceContext(
  pairKey: string | null | undefined,
  matchNumberInContainer: number | null | undefined,
  formatMatchNumber: (n: number) => string,
  formatUnknown: () => string,
): string {
  const pair = pairKey?.trim();
  if (pair) {
    return pair;
  }
  const n = structureMatchNumberInContainerVisible(
    pairKey,
    matchNumberInContainer,
  );
  if (n != null) {
    return formatMatchNumber(n);
  }
  return formatUnknown();
}

export function structureProgressionSortKey(
  pairKey: string | null | undefined,
  matchNumberInContainer: number | null | undefined,
): string {
  const pair = pairKey?.trim();
  if (pair) {
    return pair;
  }
  const n = structureMatchNumberInContainerVisible(
    pairKey,
    matchNumberInContainer,
  );
  return n != null ? String(n).padStart(8, '0') : '';
}
