import type { StructureSectionId } from './structureHubSections';

/** Query keys for Structure deep-links (Règlement → Structure navigation contract). */
export const STRUCTURE_STAGE_PARAM = 'stage';
export const STRUCTURE_SECTION_PARAM = 'section';
export const STRUCTURE_ROUND_PARAM = 'round';

const STRUCTURE_SECTIONS: StructureSectionId[] = [
  'construction',
  'qualification',
  'progression',
  'confrontation',
  'tirage',
  'matchs',
  'classement',
];

export function isStructureSectionId(value: string): value is StructureSectionId {
  return (STRUCTURE_SECTIONS as string[]).includes(value);
}

/**
 * Builds a Structure hub URL for a competition stage (+ optional section / round).
 * Omit section for phase Overview (Championship CTA unique).
 */
export function structureDeepLink(options: {
  competitionId: string;
  stageId: string;
  section?: StructureSectionId | null;
  roundId?: string | null;
}): string {
  const params = new URLSearchParams();
  params.set(STRUCTURE_STAGE_PARAM, options.stageId);
  if (options.section) {
    params.set(STRUCTURE_SECTION_PARAM, options.section);
  }
  if (options.roundId) {
    params.set(STRUCTURE_ROUND_PARAM, options.roundId);
  }
  return `/competitions/${options.competitionId}/structure?${params.toString()}`;
}

export function parseStructureDeepLink(search: string): {
  stageId: string | null;
  section: StructureSectionId | null;
  roundId: string | null;
} {
  const params = new URLSearchParams(search);
  const stageId = params.get(STRUCTURE_STAGE_PARAM);
  const sectionRaw = params.get(STRUCTURE_SECTION_PARAM);
  const section =
    sectionRaw && isStructureSectionId(sectionRaw) ? sectionRaw : null;
  return {
    stageId: stageId && stageId.length > 0 ? stageId : null,
    section,
    roundId: params.get(STRUCTURE_ROUND_PARAM),
  };
}
