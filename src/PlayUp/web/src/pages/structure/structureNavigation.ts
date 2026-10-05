import type { StructureSectionId } from './structureHubSections';

/** Query keys for Structure deep-links (Regulation → Structure navigation contract). */
export const STRUCTURE_STAGE_PARAM = 'stage';
export const STRUCTURE_SECTION_PARAM = 'section';
export const STRUCTURE_ROUND_PARAM = 'round';
export const STRUCTURE_COMPOSE_PARAM = 'compose';
export const STRUCTURE_FOCUS_PARAM = 'focus';
export const STRUCTURE_MANUAL_PARAM = 'manual';
export const STRUCTURE_DRAW_PARAM = 'draw';

/** Scroll target on the phase fiche after navigation (element id). */
export type StructureFocusId =
  | 'operational-placement'
  | 'rail-exits'
  | 'rail-attribution'
  | 'rail-entries';

const STRUCTURE_FOCUS_IDS: StructureFocusId[] = [
  'operational-placement',
  'rail-exits',
  'rail-attribution',
  'rail-entries',
];

export function isStructureFocusId(value: string): value is StructureFocusId {
  return (STRUCTURE_FOCUS_IDS as string[]).includes(value);
}

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
  focus?: StructureFocusId | null;
  manual?: boolean;
  /** Opens the draw execution workflow dialog when the phase supports it. */
  openDrawWorkflow?: boolean;
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
  if (options.focus) {
    params.set(STRUCTURE_FOCUS_PARAM, options.focus);
  }
  if (options.manual) {
    params.set(STRUCTURE_MANUAL_PARAM, '1');
  }
  if (options.openDrawWorkflow) {
    params.set(STRUCTURE_DRAW_PARAM, '1');
  }
  return `/competitions/${options.competitionId}/structure?${params.toString()}`;
}

export function parseStructureDeepLink(search: string): {
  stageId: string | null;
  section: StructureSectionId | null;
  roundId: string | null;
  compose: boolean;
  focus: StructureFocusId | null;
  manual: boolean;
  openDrawWorkflow: boolean;
} {
  const params = new URLSearchParams(search);
  const stageId = params.get(STRUCTURE_STAGE_PARAM);
  const sectionRaw = params.get(STRUCTURE_SECTION_PARAM);
  const section =
    sectionRaw && isStructureSectionId(sectionRaw) ? sectionRaw : null;
  const composeRaw = params.get(STRUCTURE_COMPOSE_PARAM);
  const focusRaw = params.get(STRUCTURE_FOCUS_PARAM);
  const focus =
    focusRaw && isStructureFocusId(focusRaw) ? focusRaw : null;
  const manualRaw = params.get(STRUCTURE_MANUAL_PARAM);
  const drawRaw = params.get(STRUCTURE_DRAW_PARAM);
  return {
    stageId: stageId && stageId.length > 0 ? stageId : null,
    section,
    roundId: params.get(STRUCTURE_ROUND_PARAM),
    compose: composeRaw === '1' || composeRaw === 'true',
    focus,
    manual: manualRaw === '1' || manualRaw === 'true',
    openDrawWorkflow: drawRaw === '1' || drawRaw === 'true',
  };
}
