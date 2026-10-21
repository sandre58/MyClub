// Product presentation severity — delegates to structureDiagnosticPresentationPolicy.

import type { StructureFormatKind, StageStatus } from '../../types';
import {
  structureDiagnosticToneFromPresentation,
  structureProductSeverityFromPresentation,
  type StructureDiagnosticPresentationContext,
} from './structureDiagnosticPresentationPolicy';

export type StructureProductSeverity = 'blocking' | 'warning';

export type StructureDiagnosticSeverityContext = {
  formatKind?: StructureFormatKind | null;
  stageStatus?: StageStatus | null;
  isEntryBaselineStage?: boolean;
  hasInboundQualProgFeed?: boolean;
  affectationEntryCount?: number;
  compositionEntryCount?: number;
  compositionCapacity?: number | null;
};

function toPresentationContext(
  context?: StructureDiagnosticSeverityContext,
): StructureDiagnosticPresentationContext {
  if (!context) {
    return {};
  }
  return {
    formatKind: context.formatKind,
    stageStatus: context.stageStatus,
    isEntryBaselineStage: context.isEntryBaselineStage,
    hasInboundQualProgFeed: context.hasInboundQualProgFeed,
    affectationEntryCount: context.affectationEntryCount,
  };
}

export function structureProductSeverity(
  code: string,
  context?: StructureDiagnosticSeverityContext,
): StructureProductSeverity {
  return structureProductSeverityFromPresentation(
    code,
    toPresentationContext(context),
  );
}

export function structureDiagnosticToneForCode(
  code: string,
  context?: StructureDiagnosticSeverityContext,
): 'danger' | 'attention' | 'progress' {
  return structureDiagnosticToneFromPresentation(
    code,
    toPresentationContext(context),
  );
}

export { structureDiagnosticPresentation } from './structureDiagnosticPresentationPolicy';
