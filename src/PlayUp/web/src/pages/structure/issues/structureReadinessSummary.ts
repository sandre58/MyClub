// -----------------------------------------------------------------------
// Structure readiness summary — business-family synthesis.
// Detection ≠ diagnostic ≠ correction.
// Banner = problems on an existing structure (stages.length >= 1).
// Birth (0 phase) = empty state only — MissingStage / InsufficientParticipants stay Host-only.
// -----------------------------------------------------------------------

import type { StructureStageHubSummary, StructureView } from '../../../types';
import { resolvePlacesN } from '../composition/structurePlaces';
import { inboundPopulationConfiguredVolume } from '../composition/structurePopulationVolume';
import {
  STRUCTURE_DIAGNOSTIC_FAMILIES,
  countStructureIssuesByFamily,
  structureDiagnosticFamily,
  structureDiagnosticMeta,
  structureDiagnosticTone,
  type StructureDiagnosticFamily,
} from './structureDiagnosticPresentation';
import { structureDiagnosticContextFromStage } from './structureDiagnosticContext';
import { collectAllStructureReadinessActionItems } from './structureReadinessActions';
import { readinessHasBlockerCode } from './structureReadinessBlockerDetails';
import { stagePresentedIssueCodes } from './structureStagePresentedIssues';

/** Ambient kinds on a topology tile (unified pastille). Not structureIssues. */
export type StageAmbientKind =
  'graph' | 'feed' | 'capacity' | 'execution' | 'result' | 'drawPots';

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
  /**
   * Phases whose presented codes include MissingPotRules.
   * Already included in presentedProblemCount.
   */
  missingPotsPhaseCount: number;
  /** CupBracketInvalid on an existing structure. */
  shapeInvalid: boolean;
  /** True when the PageHead readiness banner should render. */
  hasAny: boolean;
  /** PageHead negative band — danger-level structure signals only (not warning-only). */
  showPageHeadNegative: boolean;
  firstAttentionStageId: string | null;
  /**
   * Worst-signal header chrome. Explicit classification — never derived from
   * child component colours.
   */
  headerTone: StructureHeaderTone;
  /**
   * True when every hub-presented structure contributor (codes + missing pots)
   * belongs to a Suspended non-completed stage. False when shape invalid or any
   * contributor is on Draft / Ready / Running.
   */
  presentedStructureProblemsSuspendedOnly: boolean;
};

/** Explicit signal → gravity. Source of truth for headerTone (not vocabulary). */
export function classifyReadinessHeaderTone(args: {
  codeTone?: StructureHeaderTone;
  missingPotsPhaseCount: number;
  shapeInvalid: boolean;
}): StructureHeaderTone {
  if (args.shapeInvalid) return 'danger';
  if (args.codeTone === 'danger') return 'danger';
  if (args.codeTone === 'attention' || args.missingPotsPhaseCount > 0) {
    return 'attention';
  }
  return 'none';
}

function headerToneFromStages(
  data: StructureView,
  stages: StructureStageHubSummary[],
): StructureHeaderTone {
  let hasDanger = false;
  let hasNonDanger = false;
  for (const stage of stages) {
    const presented = stagePresentedIssueCodes(data, stage);
    for (const code of presented) {
      if (!structureDiagnosticMeta(code)) continue;
      const tone = structureDiagnosticTone(
        code,
        structureDiagnosticContextFromStage(data, stage),
      );
      if (tone === 'progress') {
        continue;
      }
      if (tone === 'danger') hasDanger = true;
      else hasNonDanger = true;
    }
  }
  if (hasDanger) return 'danger';
  if (hasNonDanger) return 'attention';
  return 'none';
}

/** Occupancy gap: entries − Places N. Null when Places N unknown. */
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

/**
 * Hub synthesis (not selected fiche stage): are all presented structure problems
 * contributed only by Suspended phases?
 */
export function presentedStructureProblemsSuspendedOnly(
  data: StructureView,
  stages: StructureStageHubSummary[],
): boolean {
  if (readinessHasBlockerCode(data, 'CupBracketInvalid')) {
    return false;
  }

  let hasContributor = false;

  for (const stage of stages) {
    if (stage.status === 'Completed') {
      continue;
    }
    const presented = stagePresentedIssueCodes(data, stage);
    if (presented.length === 0) {
      continue;
    }
    hasContributor = true;
    if (stage.status !== 'Suspended') {
      return false;
    }
  }

  return hasContributor;
}

/** Ambient signals for a tile — one kind per family present on the stage (UX-projected). */
export function stageAmbientKinds(
  data: StructureView,
  stage: StructureStageHubSummary,
): StageAmbientKind[] {
  if (stage.status === 'Completed') {
    return [];
  }
  const kinds: StageAmbientKind[] = [];
  const seen = new Set<StructureDiagnosticFamily>();
  const presented = stagePresentedIssueCodes(data, stage);
  const context = structureDiagnosticContextFromStage(data, stage);
  for (const code of presented) {
    // Catalogue family is execution; draw pots are a separate ambient kind.
    if (code === 'MissingPotRules') {
      continue;
    }
    if (structureDiagnosticTone(code, context) === 'progress') {
      continue;
    }
    const family = structureDiagnosticFamily(code);
    if (!family || seen.has(family)) continue;
    seen.add(family);
    kinds.push(family);
  }
  if (presented.includes('MissingPotRules')) {
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
      missingPotsPhaseCount: 0,
      shapeInvalid: false,
      hasAny: false,
      showPageHeadNegative: false,
      firstAttentionStageId: null,
      headerTone: 'none',
      presentedStructureProblemsSuspendedOnly: false,
    };
  }

  const presentedCodes: string[] = [];
  let missingPotsPhaseCount = 0;
  let firstAttentionStageId: string | null = null;

  for (const stage of stages) {
    if (stage.status === 'Completed') {
      continue;
    }
    const diagnosticContext = structureDiagnosticContextFromStage(data, stage);
    const presented = stagePresentedIssueCodes(data, stage).filter(
      (code) => structureDiagnosticTone(code, diagnosticContext) !== 'progress',
    );
    if (presented.length > 0) {
      firstAttentionStageId ??= stage.stageId;
      presentedCodes.push(...presented);
    }
    if (presented.includes('MissingPotRules')) {
      missingPotsPhaseCount += 1;
    }
  }

  // MissingPotRules is drawPots ambient, not the execution family.
  const byFamily = countStructureIssuesByFamily(
    presentedCodes.filter((code) => code !== 'MissingPotRules'),
  );
  const familyCounts: StructureFamilyCount[] =
    STRUCTURE_DIAGNOSTIC_FAMILIES.filter((family) => byFamily[family] > 0).map(
      (family) => ({ family, count: byFamily[family] }),
    );

  const shapeInvalid = readinessHasBlockerCode(data, 'CupBracketInvalid');

  const presentedProblemCount = presentedCodes.length + (shapeInvalid ? 1 : 0);

  const hasAny = presentedProblemCount > 0;

  const actionableCount = collectAllStructureReadinessActionItems(
    data,
    stages,
  ).length;

  const headerTone = classifyReadinessHeaderTone({
    codeTone: headerToneFromStages(data, stages),
    missingPotsPhaseCount,
    shapeInvalid,
  });

  return {
    familyCounts,
    presentedProblemCount,
    actionableCount,
    missingPotsPhaseCount,
    shapeInvalid,
    hasAny,
    showPageHeadNegative: shapeInvalid || headerTone === 'danger',
    firstAttentionStageId,
    headerTone,
    presentedStructureProblemsSuspendedOnly:
      presentedStructureProblemsSuspendedOnly(data, stages),
  };
}
