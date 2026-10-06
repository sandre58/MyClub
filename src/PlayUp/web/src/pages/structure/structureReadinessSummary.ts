// -----------------------------------------------------------------------
// Structure readiness summary (E1–E5) — business-family synthesis.
// Detection ≠ diagnostic ≠ correction.
// Banner = problems on an existing structure (stages.length >= 1).
// Birth (0 phase) = empty state only — MissingStage / InsufficientParticipants stay Host-only.
// -----------------------------------------------------------------------

import type { StructureStageHubSummary, StructureView } from '../../types';
import { resolvePlacesN } from './structurePlaces';
import { inboundPopulationConfiguredVolume } from './structurePopulationVolume';
import {
  STRUCTURE_DIAGNOSTIC_FAMILIES,
  countStructureIssuesByFamily,
  isSlotFeedAwaitingDraw,
  projectPresentedStageIssueCodes,
  structureDiagnosticFamily,
  structureDiagnosticMeta,
  type StructureDiagnosticFamily,
} from './structureDiagnosticPresentation';
import { collectAllStructureReadinessActionItems } from './structureReadinessActions';
import {
  stageHasEngagedMissingPots,
  stagePresentedIssueCodes,
} from './structureStagePresentedIssues';

/** Ambient kinds on a topology tile (unified pastille). Not structureIssues. */
export type StageAmbientKind =
  | 'graph'
  | 'feed'
  | 'capacity'
  | 'execution'
  | 'result'
  | 'drawPots';

/**
 * Header synthesis tone — visual gravity only (vocabulary stays family-based).
 * Order: danger > attention > none.
 */
export type StructureHeaderTone = 'danger' | 'attention' | 'none';

export type StructureFamilyCount = {
  family: StructureDiagnosticFamily;
  count: number;
};

export type StructureReadinessSummary = {
  /** Issue counts by business family (header vocabulary). */
  familyCounts: StructureFamilyCount[];
  /** Presented diagnostics visible on Structure (projection + synthèses). */
  presentedProblemCount: number;
  /** Diagnostics with a banner CTA (full list, not capped at 3). */
  actionableCount: number;
  /** @deprecated Use presentedProblemCount — kept for tests during transition. */
  totalProblemCount: number;
  /** Phases without graph/shortfall but Groups + engaged draw + missing pots (E1/E6). */
  missingPotsPhaseCount: number;
  /** CupBracketInvalid on an existing structure. */
  shapeInvalid: boolean;
  /** True when the PageHead readiness banner should render. */
  hasAny: boolean;
  firstAttentionStageId: string | null;
  /**
   * Worst-signal header chrome. Explicit classification — never derived from
   * child component colours.
   */
  headerTone: StructureHeaderTone;
};

/** Explicit signal → gravity. Source of truth for headerTone (not vocabulary). */
export function classifyReadinessHeaderTone(args: {
  codes: readonly string[];
  missingPotsPhaseCount: number;
  shapeInvalid: boolean;
}): StructureHeaderTone {
  if (args.shapeInvalid) return 'danger';
  const fromCodes = headerToneFromCodes(args.codes);
  if (fromCodes === 'danger') return 'danger';
  if (
    fromCodes === 'attention' ||
    args.missingPotsPhaseCount > 0 ||
    args.codes.length > 0
  ) {
    return 'attention';
  }
  return 'none';
}

function headerToneFromCodes(codes: readonly string[]): StructureHeaderTone {
  let hasDanger = false;
  let hasNonDanger = false;
  for (const code of codes) {
    const meta = structureDiagnosticMeta(code);
    if (!meta) continue;
    if (meta.tone === 'danger') hasDanger = true;
    else hasNonDanger = true;
  }
  if (hasDanger) return 'danger';
  if (hasNonDanger) return 'attention';
  return 'none';
}

/** U1 occupancy gap: entries − Places N. Null when Places N unknown. */
export function stagePopulationGap(
  data: StructureView,
  stage: StructureStageHubSummary,
): number | null {
  const places = resolvePlacesN(stage);
  if (places == null || places <= 0) return null;
  const entries =
    (stage.affectationEntryCount ?? 0) +
    inboundPopulationConfiguredVolume(data, stage.stageId);
  return entries - places;
}

export function stageHasPopulationShortfall(
  data: StructureView,
  stage: StructureStageHubSummary,
): boolean {
  const gap = stagePopulationGap(data, stage);
  return gap != null && gap < 0;
}

export { stageHasEngagedMissingPots } from './structureStagePresentedIssues';

/** Ambient signals for a tile — one kind per family present on the stage (UX-projected). */
export function stageAmbientKinds(
  data: StructureView,
  stage: StructureStageHubSummary,
): StageAmbientKind[] {
  const kinds: StageAmbientKind[] = [];
  const seen = new Set<StructureDiagnosticFamily>();
  const presented = projectPresentedStageIssueCodes(
    stage.structureIssues ?? [],
    stage,
  );
  for (const code of presented) {
    const family = structureDiagnosticFamily(code);
    if (!family || seen.has(family)) continue;
    seen.add(family);
    kinds.push(family);
  }
  if (
    !seen.has('capacity') &&
    stageHasPopulationShortfall(data, stage) &&
    !presented.includes('PopulationShortfall')
  ) {
    kinds.push('capacity');
  }
  if (stageHasEngagedMissingPots(stage)) {
    kinds.push('drawPots');
  }
  return kinds;
}

/**
 * PageHead synthesis by business family (presented problems only).
 * Birth (0 phase) yields an empty summary — empty state owns that UX.
 */
export function computeStructureReadinessSummary(
  data: StructureView,
  stages: StructureStageHubSummary[],
): StructureReadinessSummary {
  if (stages.length === 0) {
    return {
      familyCounts: [],
      presentedProblemCount: 0,
      actionableCount: 0,
      totalProblemCount: 0,
      missingPotsPhaseCount: 0,
      shapeInvalid: false,
      hasAny: false,
      firstAttentionStageId: null,
      headerTone: 'none',
    };
  }

  const presentedCodes: string[] = [];
  let missingPotsPhaseCount = 0;
  let firstAttentionStageId: string | null = null;

  for (const stage of stages) {
    const presented = stagePresentedIssueCodes(data, stage);
    if (presented.length > 0) {
      firstAttentionStageId ??= stage.stageId;
      presentedCodes.push(...presented);
    }
    if (stageHasEngagedMissingPots(stage)) {
      missingPotsPhaseCount += 1;
      firstAttentionStageId ??= stage.stageId;
    }
  }

  const byFamily = countStructureIssuesByFamily(presentedCodes);
  const familyCounts: StructureFamilyCount[] = STRUCTURE_DIAGNOSTIC_FAMILIES.filter(
    (family) => byFamily[family] > 0,
  ).map((family) => ({ family, count: byFamily[family] }));

  const shapeInvalid = data.readiness.blockers.includes('CupBracketInvalid');

  const presentedProblemCount =
    presentedCodes.length +
    missingPotsPhaseCount +
    (shapeInvalid ? 1 : 0);

  const hasAny = presentedProblemCount > 0;

  const actionableCount = collectAllStructureReadinessActionItems(
    data,
    stages,
  ).length;

  return {
    familyCounts,
    presentedProblemCount,
    actionableCount,
    totalProblemCount: presentedProblemCount,
    missingPotsPhaseCount,
    shapeInvalid,
    hasAny,
    firstAttentionStageId,
    headerTone: classifyReadinessHeaderTone({
      codes: presentedCodes,
      missingPotsPhaseCount,
      shapeInvalid,
    }),
  };
}

/** @deprecated — prefer isSlotFeedAwaitingDraw from diagnostic presentation. */
export { isSlotFeedAwaitingDraw };
