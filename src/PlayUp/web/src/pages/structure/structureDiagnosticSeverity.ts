// Product severity (D5) — Host Severity / drawer nature should match this matrix.
// Lifecycle gates remain explicit in Application (Prepare, etc.).

export type StructureProductSeverity = 'blocking' | 'warning';

/** A4 revision + D5 chat arbitrations. */
const SEVERITY_BY_CODE: Record<string, StructureProductSeverity> = {
  SlotFeedMissing: 'blocking',
  OverCapacityEdge: 'warning',
  NonRootUnfed: 'warning',
  AuthoredAwardUnreachable: 'warning',
  ExpectedRankUnreachable: 'warning',
};

export function structureProductSeverity(code: string): StructureProductSeverity {
  return SEVERITY_BY_CODE[code] ?? 'blocking';
}
