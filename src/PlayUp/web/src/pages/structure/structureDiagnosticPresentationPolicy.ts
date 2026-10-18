// Presentation policy (signed contract + impact-rules). Does not affect server gates.

import type { StageStatus, StructureFormatKind } from '../../types';
import { isNonClassifyingStructurePhase } from './structureStageClassification';

export type StructurePresentationPhaseBucket =
  | 'Draft'
  | 'Ready'
  | 'Running'
  | 'hidden';

export type StructurePresentationSeverity = 'hidden' | 'warning' | 'blocking';

export type StructurePresentationTone = 'progress' | 'attention' | 'danger' | null;

export type OperationalImpact = 'unknown' | 'absent' | 'established';

export type StructureDiagnosticPresentationContext = {
  formatKind?: StructureFormatKind | null;
  stageStatus?: StageStatus | null;
  /** First / entry baseline stage — NonRootUnfed progress never applies. */
  isEntryBaselineStage?: boolean;
  /** Qual or prog path targets this stage (mirrors HasInboundFeed). */
  hasInboundQualProgFeed?: boolean;
  affectationEntryCount?: number;
  operationalImpact?: OperationalImpact;
};

export type StructureDiagnosticPresentationResult = {
  severity: StructurePresentationSeverity;
  tone: StructurePresentationTone;
  titleKeyShort: string;
  titleKeyLong: string;
};

const DANGER_MATRIX_CODES = new Set([
  'CycleDetected',
  'RoutingNondeterminism',
  'OverExtraction',
  'ConfrontationUndecidable',
  'SlotFeedMissing',
  'SlotFeedInvalid',
  'SlotFeedMultiple',
  'DanglingQualificationTarget',
  'DanglingProgressionTarget',
  'MissingQualificationDestinationSlot',
  'MissingQualificationDestinationGroup',
  'MissingProgressionDestinationSlot',
  'MissingProgressionDestinationGroup',
]);

export function structurePresentationPhaseBucket(
  stageStatus?: StageStatus | null,
): StructurePresentationPhaseBucket {
  switch (stageStatus) {
    case 'Ready':
      return 'Ready';
    case 'Running':
      return 'Running';
    case 'Suspended':
      return 'Running';
    case 'Completed':
      return 'hidden';
    case 'Draft':
    default:
      return 'Draft';
  }
}

export function resolveIncompletenessExpectedNonRootUnfed(
  context: StructureDiagnosticPresentationContext,
): boolean {
  if (context.stageStatus !== 'Draft') {
    return false;
  }
  if (context.isEntryBaselineStage) {
    return false;
  }
  if (context.hasInboundQualProgFeed) {
    return false;
  }
  if ((context.affectationEntryCount ?? 0) > 0) {
    return false;
  }
  return true;
}

type MatrixCell = 'danger' | 'attention' | 'progress';

function resolveMatrixCell(
  code: string,
  bucket: StructurePresentationPhaseBucket,
  context: StructureDiagnosticPresentationContext,
): MatrixCell {
  if (bucket === 'hidden') {
    return 'attention';
  }

  if (
    code === 'IncompleteEntryRound'
    || code === 'UnmaterializedConfrontations'
    || code === 'NoExploitableInitialConfrontations'
    || code === 'GroupsNotReady'
  ) {
    return 'danger';
  }

  if (code === 'OverCapacityEdge' || code === 'ExpectedRankUnreachable') {
    return 'attention';
  }

  if (code === 'PopulationShortfall' || code === 'SourcePopulationShortfall') {
    return bucket === 'Ready' ? 'danger' : 'attention';
  }

  if (code === 'NonRootUnfed') {
    if (
      bucket === 'Draft' &&
      resolveIncompletenessExpectedNonRootUnfed(context)
    ) {
      return 'progress';
    }
    return 'attention';
  }

  if (code === 'AuthoredAwardUnreachable') {
    if (
      context.formatKind != null &&
      !isNonClassifyingStructurePhase({ formatKind: context.formatKind })
    ) {
      return 'attention';
    }
    return 'danger';
  }

  if (code === 'SlotFeedMissing') {
    return bucket === 'Draft' ? 'attention' : 'danger';
  }

  if (DANGER_MATRIX_CODES.has(code)) {
    return 'danger';
  }

  return 'danger';
}

function applyImpactPromotion(
  _code: string,
  cell: MatrixCell,
  context: StructureDiagnosticPresentationContext,
): MatrixCell {
  if (cell === 'danger' || cell === 'progress') {
    return cell;
  }
  if (context.operationalImpact !== 'established') {
    return cell;
  }
  // Registry empty in step 3 — no promotion rules shipped.
  return cell;
}

function cellToPresentation(
  cell: MatrixCell,
  code: string,
): StructureDiagnosticPresentationResult {
  if (cell === 'danger') {
    return {
      severity: 'blocking',
      tone: 'danger',
      titleKeyShort: titleKeyShort(code),
      titleKeyLong: titleKeyLong(code),
    };
  }
  if (cell === 'progress') {
    return {
      severity: 'warning',
      tone: 'progress',
      titleKeyShort: titleKeyShort(code),
      titleKeyLong: titleKeyLong(code),
    };
  }
  return {
    severity: 'warning',
    tone: 'attention',
    titleKeyShort: titleKeyShort(code),
    titleKeyLong: titleKeyLong(code),
  };
}

export function titleKeyLong(code: string): string {
  return `graph.issues.${code}`;
}

export function titleKeyShort(code: string): string {
  return `graph.issuesShort.${code}`;
}

/**
 * Single presentation entry point (SPA). Masking (draw, projection) stays upstream.
 */
export function structureDiagnosticPresentation(
  code: string,
  context: StructureDiagnosticPresentationContext = {},
): StructureDiagnosticPresentationResult {
  const bucket = structurePresentationPhaseBucket(context.stageStatus);
  if (bucket === 'hidden') {
    return {
      severity: 'hidden',
      tone: null,
      titleKeyShort: titleKeyShort(code),
      titleKeyLong: titleKeyLong(code),
    };
  }

  const cell = applyImpactPromotion(
    code,
    resolveMatrixCell(code, bucket, context),
    context,
  );
  return cellToPresentation(cell, code);
}

/** @deprecated Use structureDiagnosticPresentation — kept for incremental migration. */
export function structureProductSeverityFromPresentation(
  code: string,
  context?: StructureDiagnosticPresentationContext,
): 'blocking' | 'warning' {
  const { severity } = structureDiagnosticPresentation(code, context ?? {});
  if (severity === 'hidden' || severity === 'warning') {
    return 'warning';
  }
  return 'blocking';
}

/** @deprecated Use structureDiagnosticPresentation.tone */
export function structureDiagnosticToneFromPresentation(
  code: string,
  context?: StructureDiagnosticPresentationContext,
): 'danger' | 'attention' | 'progress' {
  const { tone, severity } = structureDiagnosticPresentation(
    code,
    context ?? {},
  );
  if (severity === 'blocking' || tone === 'danger') {
    return 'danger';
  }
  if (tone === 'progress') {
    return 'progress';
  }
  return 'attention';
}
