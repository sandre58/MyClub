// -----------------------------------------------------------------------
// Structure readiness summary (E1–E5) — phase-unit exclusive counts.
// Detection ≠ diagnostic ≠ correction. Population shortfall never from Host blockers.
// Banner = problems on an existing structure (stages.length >= 1).
// Birth (0 phase) = empty state only — MissingStage / InsufficientParticipants stay Host-only.
// -----------------------------------------------------------------------

import type { StructureStageHubSummary, StructureView } from '../../types';
import { resolvePlacesN } from './structurePlaces';
import { inboundPopulationConfiguredVolume } from './structurePopulationVolume';

/** Ambient kinds on a topology tile (unified pastille). Not structureIssues. */
export type StageAmbientKind = 'graph' | 'population' | 'drawPots';

/**
 * Header synthesis tone — functional gravity, not child CSS.
 * Order: danger > attention > none.
 */
export type StructureHeaderTone = 'danger' | 'attention' | 'none';

export type StructureReadinessSummary = {
  /** Phases with ≥1 structureIssue. */
  structuralPhaseCount: number;
  /** Phases without structureIssue but U1 shortfall. */
  incompletePhaseCount: number;
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

/** Explicit signal → gravity. Source of truth for headerTone. */
export function classifyReadinessHeaderTone(args: {
  structuralPhaseCount: number;
  incompletePhaseCount: number;
  missingPotsPhaseCount: number;
  shapeInvalid: boolean;
}): StructureHeaderTone {
  // StructureIssue / shape → danger
  if (args.structuralPhaseCount > 0 || args.shapeInvalid) {
    return 'danger';
  }
  // PopulationShortfall / MissingPotRules → attention (never draw execution state)
  if (args.incompletePhaseCount > 0 || args.missingPotsPhaseCount > 0) {
    return 'attention';
  }
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

/**
 * E1 / E6 — MissingPotRules construction gap.
 * Strictly aligned with Host: Groups && HasDrawRules && NumberOfPots == null.
 * Draw execution (Draft / Published) is never a header/ambient construction signal.
 */
export function stageHasEngagedMissingPots(
  stage: StructureStageHubSummary,
): boolean {
  return (
    stage.formatKind === 'Groups' &&
    stage.hasDrawRules &&
    stage.numberOfPots == null
  );
}

/** Ambient signals for a tile — does not invent structureIssues. */
export function stageAmbientKinds(
  data: StructureView,
  stage: StructureStageHubSummary,
): StageAmbientKind[] {
  const kinds: StageAmbientKind[] = [];
  if ((stage.structureIssues ?? []).length > 0) {
    kinds.push('graph');
  }
  if (stageHasPopulationShortfall(data, stage)) {
    kinds.push('population');
  }
  if (stageHasEngagedMissingPots(stage)) {
    kinds.push('drawPots');
  }
  return kinds;
}

/**
 * Exclusive phase buckets for PageHead synthesis.
 * A phase with structureIssues counts only as structural (not incomplete / draw).
 * Birth (0 phase) yields an empty summary — empty state owns that UX.
 */
export function computeStructureReadinessSummary(
  data: StructureView,
  stages: StructureStageHubSummary[],
): StructureReadinessSummary {
  if (stages.length === 0) {
    return {
      structuralPhaseCount: 0,
      incompletePhaseCount: 0,
      missingPotsPhaseCount: 0,
      shapeInvalid: false,
      hasAny: false,
      firstAttentionStageId: null,
      headerTone: 'none',
    };
  }

  let structuralPhaseCount = 0;
  let incompletePhaseCount = 0;
  let missingPotsPhaseCount = 0;
  let firstAttentionStageId: string | null = null;

  for (const stage of stages) {
    const hasGraph = (stage.structureIssues ?? []).length > 0;
    if (hasGraph) {
      structuralPhaseCount += 1;
      firstAttentionStageId ??= stage.stageId;
      continue;
    }
    if (stageHasPopulationShortfall(data, stage)) {
      incompletePhaseCount += 1;
      firstAttentionStageId ??= stage.stageId;
      continue;
    }
    if (stageHasEngagedMissingPots(stage)) {
      missingPotsPhaseCount += 1;
      firstAttentionStageId ??= stage.stageId;
    }
  }

  // Host birth/competition blockers (MissingStage, InsufficientParticipants, …)
  // are not Structure banner signals — correction locus is elsewhere or empty state.
  const shapeInvalid = data.readiness.blockers.includes('CupBracketInvalid');

  const hasAny =
    structuralPhaseCount > 0 ||
    incompletePhaseCount > 0 ||
    missingPotsPhaseCount > 0 ||
    shapeInvalid;

  const headerTone = classifyReadinessHeaderTone({
    structuralPhaseCount,
    incompletePhaseCount,
    missingPotsPhaseCount,
    shapeInvalid,
  });

  return {
    structuralPhaseCount,
    incompletePhaseCount,
    missingPotsPhaseCount,
    shapeInvalid,
    hasAny,
    firstAttentionStageId,
    headerTone,
  };
}
