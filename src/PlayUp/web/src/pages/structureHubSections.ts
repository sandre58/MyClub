import type {
  OrganisationStageDefaultsBinding,
  OrganisationStageHubSummary,
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

/** Major switcher destinations — Matchs/Classement stay Overview pointers / secondary drill-in. */
export const STRUCTURE_SWITCHER_SECTIONS: StructureSectionId[] = [
  'construction',
  'qualification',
  'progression',
  'confrontation',
  'tirage',
];

export function isMatchFrameBound(
  binding: OrganisationStageDefaultsBinding | undefined,
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
  binding: OrganisationStageDefaultsBinding | undefined,
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
 * Sections present for this phase. Absent model → omitted (no disabled chrome).
 * Matchs/Classement are never major switcher items.
 */
export function relevantSwitcherSections(
  stage: OrganisationStageHubSummary,
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

function stageNeedsDrawSection(stage: OrganisationStageHubSummary): boolean {
  if (stage.hasDrawRules) {
    return true;
  }
  const kind = stage.formatKind;
  return kind === 'Groups' || kind === 'Cup';
}

export function constructionSummaryFacts(stage: OrganisationStageHubSummary): {
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
