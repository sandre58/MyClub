import type {
  OverviewAction,
  OverviewProminence,
  OverviewSituation,
  OverviewView,
} from '../types';
import { overviewActionKey } from './overviewActions';

/**
 * Presentation helpers for Overview composition.
 * Uses Read signals only — does not invent blockers, readiness, or transitions.
 */

const PROMINENCE_ORDER: Record<string, number> = {
  Dominant: 0,
  Present: 1,
  Condensed: 2,
  Absent: 3,
};

export function prominenceRank(prominence: string): number {
  return PROMINENCE_ORDER[prominence] ?? 1;
}

/** Absent sections must not consume layout space. */
export function isProminenceVisible(prominence: string): boolean {
  return prominence !== 'Absent';
}

export function isProminenceCondensed(prominence: string): boolean {
  return prominence === 'Condensed';
}

export function isProminenceDominant(prominence: string): boolean {
  return prominence === 'Dominant';
}

export type ConstructionSlot = 'teams' | 'structure' | 'regulation' | 'matches';

/** Stable presentation affinity: where an action code is shown (not a business rule). */
export function actionPresentationSlot(
  code: string,
): ConstructionSlot | 'operational' | 'closure' | 'navigation' | 'secondary' {
  switch (code) {
    case 'AddEntry':
    case 'RenameEntry':
    case 'WithdrawEntry':
    case 'DeleteEntry':
      return 'teams';
    case 'ConfigureStructure':
      return 'structure';
    case 'ReplaceRegulation':
      return 'regulation';
    case 'StartMatch':
    case 'FinishMatch':
      return 'matches';
    case 'PrepareStage':
    case 'StartStage':
    case 'PublishDraw':
    case 'PublishAndApplyDraw':
    case 'ApplyDraw':
    case 'MaterializeMatches':
    case 'MaterializeFromOccupiedSlots':
    case 'GenerateNextRound':
    case 'GenerateSchedule':
    case 'ApplySchedule':
    case 'ApplyProgression':
    case 'ApplyQualification':
      return 'operational';
    case 'CompleteCompetition':
    case 'ArchiveCompetition':
      return 'closure';
    case 'ContinueStructure':
    case 'OpenMatches':
    case 'OpenConsultation':
      return 'navigation';
    default:
      return 'secondary';
  }
}

export function actionsForSlot(
  actions: OverviewAction[],
  slot: ReturnType<typeof actionPresentationSlot>,
): OverviewAction[] {
  return actions.filter(
    (action) => actionPresentationSlot(action.code) === slot,
  );
}

export function findActionByCode(
  actions: OverviewAction[],
  code: string,
): OverviewAction | undefined {
  return actions.find((action) => action.code === code);
}

/** Actions already rendered on a contextual surface — excluded from the secondary strip. */
export function secondaryActions(
  all: OverviewAction[],
  renderedKeys: ReadonlySet<string>,
): OverviewAction[] {
  return all.filter(
    (action) =>
      !renderedKeys.has(overviewActionKey(action)) &&
      !isTeamAdminAction(action.code),
  );
}

export function sortConstructionSlots(view: OverviewView): ConstructionSlot[] {
  const dims = view.constructionDimensions;
  const entries: { slot: ConstructionSlot; prominence: string }[] = [
    { slot: 'teams', prominence: dims.teams.prominence },
    { slot: 'structure', prominence: dims.structure.prominence },
    { slot: 'regulation', prominence: dims.regulation.prominence },
    { slot: 'matches', prominence: dims.matches.prominence },
  ];

  return entries
    .filter((entry) => isProminenceVisible(entry.prominence))
    .sort((a, b) => prominenceRank(a.prominence) - prominenceRank(b.prominence))
    .map((entry) => entry.slot);
}

/** Primary overview actions for teams — admin mutations stay on Teams. */
export function primaryTeamActions(
  actions: OverviewAction[],
): OverviewAction[] {
  return actions.filter((action) => action.code === 'AddEntry');
}

/** Team admin codes: available in Read but not shown as Overview command buttons. */
export function isTeamAdminAction(code: string): boolean {
  return (
    code === 'RenameEntry' || code === 'WithdrawEntry' || code === 'DeleteEntry'
  );
}

export function actionsForStage(
  actions: OverviewAction[],
  stageId: string,
): OverviewAction[] {
  return actions.filter(
    (action) =>
      (action.code === 'PrepareStage' || action.code === 'StartStage') &&
      action.stageId === stageId,
  );
}

export function actionsForDraw(
  actions: OverviewAction[],
  stageId: string,
  drawId: string,
): OverviewAction[] {
  return actions.filter(
    (action) =>
      (action.code === 'PublishDraw' ||
        action.code === 'PublishAndApplyDraw' ||
        action.code === 'ApplyDraw') &&
      action.stageId === stageId &&
      action.drawId === drawId,
  );
}

/** Stage-scoped ops not tied to a single draw row (materialize / schedule). */
export function stageWideOperationalActions(
  actions: OverviewAction[],
): OverviewAction[] {
  return actions.filter(
    (action) =>
      action.code === 'MaterializeMatches' ||
      action.code === 'MaterializeFromOccupiedSlots' ||
      action.code === 'GenerateNextRound' ||
      action.code === 'GenerateSchedule' ||
      action.code === 'ApplySchedule' ||
      action.code === 'ApplyProgression' ||
      action.code === 'ApplyQualification',
  );
}

/** Prefer Blocking situations first for operational order (stable within nature). */
export function orderSituationsForDisplay(
  situations: OverviewSituation[],
): OverviewSituation[] {
  return [...situations].sort((a, b) => {
    const rank = (nature: string) => (nature === 'Blocking' ? 0 : 1);
    return rank(a.nature) - rank(b.nature);
  });
}

export function panelProminenceClass(prominence: string): string {
  if (isProminenceDominant(prominence)) {
    return 'ds-panel overview-dimension--dominant';
  }
  if (isProminenceCondensed(prominence)) {
    return 'ds-panel overview-dimension--condensed';
  }
  return 'ds-panel';
}

export type { OverviewProminence };
