import type { StructureSectionId } from './structureHubSections';

/** Query keys for Structure deep-links (Regulation → Structure navigation contract). */
export const STRUCTURE_STAGE_PARAM = 'stage';
export const STRUCTURE_SECTION_PARAM = 'section';
export const STRUCTURE_ROUND_PARAM = 'round';
export const STRUCTURE_COMPOSE_PARAM = 'compose';

const STRUCTURE_SECTIONS: StructureSectionId[] = [
  'construction',
  'qualification',
  'progression',
  'confrontation',
  'tirage',
  'matchs',
  'classement',
];

export function isStructureSectionId(
  value: string,
): value is StructureSectionId {
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
  compose?: boolean;
}): string {
  const params = new URLSearchParams();
  params.set(STRUCTURE_STAGE_PARAM, options.stageId);
  if (options.section) {
    params.set(STRUCTURE_SECTION_PARAM, options.section);
  }
  if (options.roundId) {
    params.set(STRUCTURE_ROUND_PARAM, options.roundId);
  }
  if (options.compose) {
    params.set(STRUCTURE_COMPOSE_PARAM, '1');
  }
  return `/competitions/${options.competitionId}/structure?${params.toString()}`;
}

export function parseStructureDeepLink(search: string): {
  stageId: string | null;
  section: StructureSectionId | null;
  roundId: string | null;
  compose: boolean;
} {
  const params = new URLSearchParams(search);
  const stageId = params.get(STRUCTURE_STAGE_PARAM);
  const sectionRaw = params.get(STRUCTURE_SECTION_PARAM);
  const section =
    sectionRaw && isStructureSectionId(sectionRaw) ? sectionRaw : null;
  const composeRaw = params.get(STRUCTURE_COMPOSE_PARAM);
  return {
    stageId: stageId && stageId.length > 0 ? stageId : null,
    section,
    roundId: params.get(STRUCTURE_ROUND_PARAM),
    compose: composeRaw === '1' || composeRaw === 'true',
  };
}
