import type {
  StructureStageDefaultsBinding,
  StructureStageHubSummary,
  StructureFormatKind,
} from '../types';

/** Domain-aligned section ids for Structure N2 (not artificial chrome groups). */
export type StructureSectionId =
  | 'construction'
  | 'qualification'
  | 'progression'
  | 'confrontation'
  | 'tirage'
  | 'matchs'
  | 'classement';

export function isMatchFrameBound(
  binding: StructureStageDefaultsBinding | undefined,
): boolean {
  if (!binding) {
    return true;
  }
  return (
    binding.matchDuration.isBound &&
    binding.extraTime.isBound &&
    binding.penaltyShootout.isBound &&
    binding.administrativeResult.isBound
  );
}

export function isStandingFrameBound(
  binding: StructureStageDefaultsBinding | undefined,
): boolean | null {
  if (!binding) {
    return true;
  }
  if (binding.points === null && binding.rankingCriteria === null) {
    return null;
  }
  const pointsBound = binding.points?.isBound ?? true;
  const criteriaBound = binding.rankingCriteria?.isBound ?? true;
  return pointsBound && criteriaBound;
}

/**
 * Domains reachable from Overview for this phase (S0: no inter-domain switcher).
 * Absent model → omitted. Matches/Standing stay separate Overview pointers.
 */
export function relevantPhaseSections(
  stage: StructureStageHubSummary,
): StructureSectionId[] {
  const sections: StructureSectionId[] = ['construction'];
  const actions = stage.actions ?? [];

  if (
    stage.hasQualificationRules ||
    actions.includes('ReplaceQualificationRules')
  ) {
    sections.push('qualification');
  }
  if (
    stage.hasProgressionRules ||
    actions.includes('ReplaceProgressionRules')
  ) {
    sections.push('progression');
  }
  if (stage.hasTieFormat) {
    sections.push('confrontation');
  }
  if (stageNeedsDrawSection(stage)) {
    sections.push('tirage');
  }

  return sections;
}

function stageNeedsDrawSection(stage: StructureStageHubSummary): boolean {
  // Engaged mechanism only (DrawRules). Format alone ≠ obligation / chrome.
  // Active Draw without rules = edge case handled on the stage card via overview fetch.
  return stage.hasDrawRules;
}

export function constructionSummaryFacts(stage: StructureStageHubSummary): {
  formatKind: StructureFormatKind | null;
  groupCount: number;
  teamCount: number;
  roundCount: number;
  legs: number | null;
} {
  return {
    formatKind: stage.formatKind ?? null,
    groupCount: stage.groupCount ?? 0,
    teamCount: stage.teamCount,
    roundCount: stage.roundCount ?? 0,
    legs: stage.hasTieFormat ? (stage.numberOfLegs ?? null) : null,
  };
}
