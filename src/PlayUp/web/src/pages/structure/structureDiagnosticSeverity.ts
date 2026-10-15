// Product severity (D5) — Host Severity / drawer nature should match this matrix.
// Lifecycle gates remain explicit in Application (Prepare, etc.).

import type { StructureFormatKind } from '../../types';
import { isNonClassifyingStructurePhase } from './structureStageClassification';

export type StructureProductSeverity = 'blocking' | 'warning';

export type StructureDiagnosticSeverityContext = {
  formatKind?: StructureFormatKind | null;
};

/** A4 revision + D5; G-UI1 / G-UI2 (§11A signed policy). */
const SEVERITY_BY_CODE: Record<string, StructureProductSeverity> = {
  SlotFeedMissing: 'blocking',
  OverCapacityEdge: 'warning',
  NonRootUnfed: 'warning',
  ExpectedRankUnreachable: 'warning',
  PopulationShortfall: 'warning',
  SourcePopulationShortfall: 'warning',
};

export function structureProductSeverity(
  code: string,
  context?: StructureDiagnosticSeverityContext,
): StructureProductSeverity {
  if (code === 'AuthoredAwardUnreachable') {
    if (
      context &&
      !isNonClassifyingStructurePhase({ formatKind: context.formatKind })
    ) {
      return 'warning';
    }
    return 'blocking';
  }

  return SEVERITY_BY_CODE[code] ?? 'blocking';
}
