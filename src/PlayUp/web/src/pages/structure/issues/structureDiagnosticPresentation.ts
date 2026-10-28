// -----------------------------------------------------------------------
// Structure diagnostic → presentation projection (family / tile / CTA).
// Analyzer facts stay unchanged; Host only maps codes for Structure UX.
// Product severity drives diagnostic tone; lifecycle gates stay in Host.
// -----------------------------------------------------------------------

import {
  structureDiagnosticToneForCode,
  type StructureDiagnosticSeverityContext,
} from './structureDiagnosticSeverity';

/** Business family for Structure header synthesis. */
export type StructureDiagnosticFamily =
  'graph' | 'feed' | 'capacity' | 'execution' | 'result';

/** Canonical fiche tile that owns the diagnostic detail. */
export type StructureDiagnosticTile =
  | 'places'
  | 'exits'
  | 'capacity'
  | 'confrontation'
  | 'standing'
  | 'attribution';

/** Correction entry (dialog / action) for a diagnostic. */
export type StructureDiagnosticCta =
  | 'places'
  | 'affectation'
  | 'qualification'
  | 'progression'
  | 'sorties'
  | 'tieFormat'
  | 'standing'
  | 'attribution'
  | 'structure';

/**
 * Visual gravity on Structure surfaces (not Analyzer Nature / Gate).
 * danger = construction error; attention = inconsistency; progress = expected incomplete.
 */
export type StructureDiagnosticTone = 'danger' | 'attention' | 'progress';

export type StructureDiagnosticMeta = {
  family: StructureDiagnosticFamily;
  tile: StructureDiagnosticTile;
  cta: StructureDiagnosticCta;
  /**
   * Default tone for tile routing only — chrome uses {@link structureDiagnosticTone}.
   */
  tone: StructureDiagnosticTone;
  /** Default i18n key under structure.graph.issues.* (may be overridden for context). */
  titleKey: string;
};

const BY_CODE: Record<string, StructureDiagnosticMeta> = {
  SlotFeedMissing: {
    family: 'feed',
    tile: 'places',
    cta: 'places',
    tone: 'attention',
    titleKey: 'graph.issues.SlotFeedMissing',
  },
  SlotFeedInvalid: {
    family: 'feed',
    tile: 'places',
    cta: 'places',
    tone: 'danger',
    titleKey: 'graph.issues.SlotFeedInvalid',
  },
  SlotFeedMultiple: {
    family: 'feed',
    tile: 'places',
    cta: 'places',
    tone: 'danger',
    titleKey: 'graph.issues.SlotFeedMultiple',
  },
  CycleDetected: {
    family: 'graph',
    tile: 'exits',
    cta: 'sorties',
    tone: 'danger',
    titleKey: 'graph.issues.CycleDetected',
  },
  RoutingNondeterminism: {
    family: 'graph',
    tile: 'exits',
    cta: 'qualification',
    tone: 'danger',
    titleKey: 'graph.issues.RoutingNondeterminism',
  },
  NonRootUnfed: {
    family: 'feed',
    tile: 'capacity',
    cta: 'affectation',
    tone: 'progress',
    titleKey: 'graph.issues.NonRootUnfed',
  },
  AwaitingInboundQualification: {
    family: 'feed',
    tile: 'capacity',
    cta: 'affectation',
    tone: 'progress',
    titleKey: 'graph.issues.AwaitingInboundQualification',
  },
  DanglingQualificationTarget: {
    family: 'graph',
    tile: 'exits',
    cta: 'qualification',
    tone: 'danger',
    titleKey: 'graph.issues.DanglingQualificationTarget',
  },
  DanglingProgressionTarget: {
    family: 'graph',
    tile: 'exits',
    cta: 'progression',
    tone: 'danger',
    titleKey: 'graph.issues.DanglingProgressionTarget',
  },
  MissingQualificationDestinationSlot: {
    family: 'graph',
    tile: 'exits',
    cta: 'qualification',
    tone: 'danger',
    titleKey: 'graph.issues.MissingQualificationDestinationSlot',
  },
  MissingQualificationDestinationGroup: {
    family: 'graph',
    tile: 'exits',
    cta: 'qualification',
    tone: 'danger',
    titleKey: 'graph.issues.MissingQualificationDestinationGroup',
  },
  MissingProgressionDestinationSlot: {
    family: 'graph',
    tile: 'exits',
    cta: 'progression',
    tone: 'danger',
    titleKey: 'graph.issues.MissingProgressionDestinationSlot',
  },
  MissingProgressionDestinationGroup: {
    family: 'graph',
    tile: 'exits',
    cta: 'progression',
    tone: 'danger',
    titleKey: 'graph.issues.MissingProgressionDestinationGroup',
  },
  DuplicateProgressionRoundOutcome: {
    family: 'graph',
    tile: 'exits',
    cta: 'progression',
    tone: 'danger',
    titleKey: 'graph.issues.DuplicateProgressionRoundOutcome',
  },
  OverCapacityEdge: {
    family: 'capacity',
    tile: 'capacity',
    cta: 'structure',
    tone: 'danger',
    titleKey: 'graph.issues.OverCapacityEdge',
  },
  OverExtraction: {
    family: 'capacity',
    tile: 'exits',
    cta: 'qualification',
    tone: 'danger',
    titleKey: 'graph.issues.OverExtraction',
  },
  PopulationShortfall: {
    family: 'capacity',
    tile: 'capacity',
    cta: 'affectation',
    tone: 'progress',
    titleKey: 'graph.issues.PopulationShortfall',
  },
  IncompleteEntryRound: {
    family: 'execution',
    tile: 'confrontation',
    cta: 'places',
    tone: 'danger',
    titleKey: 'graph.issues.IncompleteEntryRound',
  },
  NoDeterminedConfrontations: {
    family: 'execution',
    tile: 'confrontation',
    cta: 'places',
    tone: 'danger',
    titleKey: 'graph.issues.NoDeterminedConfrontations',
  },
  UnmaterializedConfrontations: {
    family: 'execution',
    tile: 'confrontation',
    cta: 'places',
    tone: 'attention',
    titleKey: 'graph.issues.UnmaterializedConfrontations',
  },
  PhaseRosterIncomplete: {
    family: 'capacity',
    tile: 'capacity',
    cta: 'affectation',
    tone: 'attention',
    titleKey: 'graph.issues.PhaseRosterIncomplete',
  },
  GroupsNotReady: {
    family: 'execution',
    tile: 'confrontation',
    cta: 'places',
    tone: 'attention',
    titleKey: 'graph.issues.GroupsNotReady',
  },
  SourcePopulationShortfall: {
    family: 'capacity',
    tile: 'capacity',
    cta: 'affectation',
    tone: 'progress',
    titleKey: 'graph.issues.SourcePopulationShortfall',
  },
  ConfrontationUndecidable: {
    family: 'execution',
    tile: 'confrontation',
    cta: 'tieFormat',
    tone: 'danger',
    titleKey: 'graph.issues.ConfrontationUndecidable',
  },
  AuthoredAwardUnreachable: {
    family: 'result',
    tile: 'attribution',
    cta: 'attribution',
    tone: 'attention',
    titleKey: 'graph.issues.AuthoredAwardUnreachable',
  },
  ExpectedRankUnreachable: {
    family: 'result',
    tile: 'attribution',
    cta: 'attribution',
    tone: 'attention',
    titleKey: 'graph.issues.ExpectedRankUnreachable',
  },
  MissingPotRules: {
    family: 'execution',
    tile: 'confrontation',
    cta: 'structure',
    tone: 'attention',
    titleKey: 'enums.attentionSource.MissingPotRules',
  },
  CupBracketInvalid: {
    family: 'execution',
    tile: 'confrontation',
    cta: 'structure',
    tone: 'danger',
    titleKey: 'enums.attentionSource.CupBracketInvalid',
  },
};

export const STRUCTURE_DIAGNOSTIC_FAMILIES: StructureDiagnosticFamily[] = [
  'graph',
  'feed',
  'capacity',
  'execution',
  'result',
];

export function structureDiagnosticMeta(
  code: string,
): StructureDiagnosticMeta | null {
  return BY_CODE[code] ?? null;
}

export function structureDiagnosticFamily(
  code: string,
): StructureDiagnosticFamily | null {
  return structureDiagnosticMeta(code)?.family ?? null;
}

export function structureDiagnosticTile(
  code: string,
): StructureDiagnosticTile | null {
  return structureDiagnosticMeta(code)?.tile ?? null;
}

export function structureDiagnosticCta(
  code: string,
): StructureDiagnosticCta | null {
  return structureDiagnosticMeta(code)?.cta ?? null;
}

/**
 * Host: SlotFeedMissing is expected while draw is engaged and not published.
 * Analyzer still emits the code — UX projection is silenced.
 */
export function isSlotFeedAwaitingDraw(stage: {
  hasDrawRules: boolean;
  drawExecutionBadge?: 'ToLaunch' | 'InProgress' | 'ToApply' | 'Applied' | null;
}): boolean {
  if (!stage.hasDrawRules) return false;
  const badge = stage.drawExecutionBadge;
  return badge !== 'ToApply' && badge !== 'Applied';
}

/** Groups pool gaps stay expected until draw results are applied (read parity). */
export function deferGroupsNotReadyPresentation(stage: {
  hasDrawRules: boolean;
  drawExecutionBadge?: 'ToLaunch' | 'InProgress' | 'ToApply' | 'Applied' | null;
}): boolean {
  return stage.hasDrawRules && stage.drawExecutionBadge !== 'Applied';
}

/** Host title key for a structure issue. */
export function structureDiagnosticTitleKey(code: string): string {
  return structureDiagnosticMeta(code)?.titleKey ?? `graph.issues.${code}`;
}

/** Effective UX tone — derived from product severity, not the stored tone alone. */
export function structureDiagnosticTone(
  code: string,
  context?: StructureDiagnosticSeverityContext,
): StructureDiagnosticTone {
  return structureDiagnosticToneForCode(code, context);
}

export {
  structureDiagnosticPresentation,
  structureProductSeverity,
} from './structureDiagnosticSeverity';
export type { StructureDiagnosticSeverityContext } from './structureDiagnosticSeverity';

/** Scroll / rail / domain-tile element id for a canonical tile. */
export function structureDiagnosticTileElementId(
  tile: StructureDiagnosticTile,
): string {
  switch (tile) {
    case 'places':
      return 'operational-placement';
    case 'exits':
      return 'rail-exits';
    case 'capacity':
      return 'rail-entries';
    case 'confrontation':
      return 'tile-confrontation';
    case 'standing':
      return 'tile-standing';
    case 'attribution':
      return 'rail-attribution';
  }
}

/** Issues owned by a fiche tile (canonical surface). */
export function structureIssuesForTile(
  codes: readonly string[] | null | undefined,
  tile: StructureDiagnosticTile,
): string[] {
  return (codes ?? []).filter((code) => structureDiagnosticTile(code) === tile);
}

/**
 * Stage-level UX filter before family counts / banners.
 * Silences SlotFeedMissing when draw is engaged (workflow-expected empty places).
 * Then merges RESULT AuthoredAward + ExpectedRank into one presented unit.
 */
export function projectPresentedStageIssueCodes(
  codes: readonly string[],
  stage: {
    hasDrawRules: boolean;
    drawExecutionBadge?:
      'ToLaunch' | 'InProgress' | 'ToApply' | 'Applied' | null;
  },
): string[] {
  let filtered = [...codes];
  if (isSlotFeedAwaitingDraw(stage)) {
    filtered = filtered.filter((code) => code !== 'SlotFeedMissing');
  }
  if (deferGroupsNotReadyPresentation(stage)) {
    filtered = filtered.filter((code) => code !== 'GroupsNotReady');
  }
  if (filtered.includes('PopulationShortfall')) {
    filtered = filtered.filter((code) => code !== 'NonRootUnfed');
  }
  return projectPresentedIssueCodes(filtered);
}

/**
 * Project Analyzer codes to user-facing problem units (Header ↔ fiche same count).
 * AuthoredAward absorbs ExpectedRank when both present.
 */
export function projectPresentedIssueCodes(codes: readonly string[]): string[] {
  if (codes.includes('AuthoredAwardUnreachable')) {
    return codes.filter((code) => code !== 'ExpectedRankUnreachable');
  }
  return [...codes];
}

export type StructureIssueBannerOptions = {
  /** Meter already shows shortfall (Il manque N) — suppress NonRootUnfed echo. */
  meterExpressesPopulationAbsence?: boolean;
  /** Draw engaged → silence SlotFeedMissing on Places. */
  silenceSlotFeedMissing?: boolean;
};

/**
 * Codes to render as banners on a fiche tile.
 * Analyzer detection is unchanged — presentation only.
 */
export function structureIssueBannersForTile(
  codes: readonly string[] | null | undefined,
  tile: StructureDiagnosticTile,
  options?: StructureIssueBannerOptions,
): string[] {
  let forTile = structureIssuesForTile(codes, tile);

  if (tile === 'places' && options?.silenceSlotFeedMissing) {
    forTile = forTile.filter((code) => code !== 'SlotFeedMissing');
  }

  if (tile === 'capacity') {
    forTile = forTile.filter(
      (code) => code !== 'PopulationShortfall' && code !== 'OverCapacityEdge',
    );
    if (options?.meterExpressesPopulationAbsence) {
      forTile = forTile.filter((code) => code !== 'NonRootUnfed');
    }
  }

  if (tile === 'attribution') {
    forTile = projectPresentedIssueCodes(forTile);
  }

  return forTile;
}

/** Count presented problems by business family (already projected codes). */
export function countStructureIssuesByFamily(
  codes: readonly string[],
): Record<StructureDiagnosticFamily, number> {
  const counts: Record<StructureDiagnosticFamily, number> = {
    graph: 0,
    feed: 0,
    capacity: 0,
    execution: 0,
    result: 0,
  };
  for (const code of codes) {
    const family = structureDiagnosticFamily(code);
    if (family) counts[family] += 1;
  }
  return counts;
}
