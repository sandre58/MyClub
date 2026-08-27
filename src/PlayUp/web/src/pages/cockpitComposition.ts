import type {
  CockpitAction,
  CockpitProminence,
  CockpitSituation,
  CockpitView,
} from '../types'
import { cockpitActionKey } from './cockpitActions'

/**
 * Presentation helpers for Cockpit composition.
 * Uses Read signals only — does not invent blockers, readiness, or transitions.
 */

const PROMINENCE_ORDER: Record<string, number> = {
  Dominant: 0,
  Present: 1,
  Condensed: 2,
  Absent: 3,
}

export function prominenceRank(prominence: string): number {
  return PROMINENCE_ORDER[prominence] ?? 1
}

/** Absent sections must not consume layout space. */
export function isProminenceVisible(prominence: string): boolean {
  return prominence !== 'Absent'
}

export function isProminenceCondensed(prominence: string): boolean {
  return prominence === 'Condensed'
}

export function isProminenceDominant(prominence: string): boolean {
  return prominence === 'Dominant'
}

export type ConstructionSlot = 'teams' | 'structure' | 'regulation' | 'matches'

/** Stable presentation affinity: where an action code is shown (not a business rule). */
export function actionPresentationSlot(code: string): ConstructionSlot | 'operational' | 'closure' | 'navigation' | 'secondary' {
  switch (code) {
    case 'AddEntry':
    case 'RenameEntry':
    case 'WithdrawEntry':
    case 'ExcludeEntry':
      return 'teams'
    case 'ConfigureStructure':
      return 'structure'
    case 'ReplaceRegulation':
      return 'regulation'
    case 'StartMatch':
    case 'FinishMatch':
      return 'matches'
    case 'PrepareStage':
    case 'StartStage':
    case 'PublishDraw':
    case 'ApplyDraw':
    case 'MaterializeMatches':
    case 'MaterializeFromOccupiedSlots':
    case 'GenerateNextRound':
    case 'GenerateSchedule':
    case 'ApplySchedule':
    case 'ApplyProgression':
    case 'ApplyQualification':
      return 'operational'
    case 'CompleteCompetition':
    case 'ArchiveCompetition':
      return 'closure'
    case 'ContinueOrganisation':
    case 'OpenMatches':
    case 'OpenConsultation':
      return 'navigation'
    default:
      return 'secondary'
  }
}

export function actionsForSlot(
  actions: CockpitAction[],
  slot: ReturnType<typeof actionPresentationSlot>,
): CockpitAction[] {
  return actions.filter((action) => actionPresentationSlot(action.code) === slot)
}

export function findActionByCode(
  actions: CockpitAction[],
  code: string,
): CockpitAction | undefined {
  return actions.find((action) => action.code === code)
}

/** Actions already rendered on a contextual surface — excluded from the secondary strip. */
export function secondaryActions(
  all: CockpitAction[],
  renderedKeys: ReadonlySet<string>,
): CockpitAction[] {
  return all.filter(
    (action) =>
      !renderedKeys.has(cockpitActionKey(action)) && !isTeamAdminAction(action.code),
  )
}

export function sortConstructionSlots(view: CockpitView): ConstructionSlot[] {
  const dims = view.constructionDimensions
  const entries: { slot: ConstructionSlot; prominence: string }[] = [
    { slot: 'teams', prominence: dims.teams.prominence },
    { slot: 'structure', prominence: dims.structure.prominence },
    { slot: 'regulation', prominence: dims.regulation.prominence },
    { slot: 'matches', prominence: dims.matches.prominence },
  ]

  return entries
    .filter((entry) => isProminenceVisible(entry.prominence))
    .sort((a, b) => prominenceRank(a.prominence) - prominenceRank(b.prominence))
    .map((entry) => entry.slot)
}

/** Primary cockpit actions for teams — admin mutations stay in Organisation. */
export function primaryTeamActions(actions: CockpitAction[]): CockpitAction[] {
  return actions.filter((action) => action.code === 'AddEntry')
}

/** Team admin codes: available in Read but not shown as Cockpit command buttons. */
export function isTeamAdminAction(code: string): boolean {
  return code === 'RenameEntry' || code === 'WithdrawEntry' || code === 'ExcludeEntry'
}

export function actionsForStage(
  actions: CockpitAction[],
  stageId: string,
): CockpitAction[] {
  return actions.filter(
    (action) =>
      (action.code === 'PrepareStage' || action.code === 'StartStage') &&
      action.stageId === stageId,
  )
}

export function actionsForDraw(
  actions: CockpitAction[],
  stageId: string,
  drawId: string,
): CockpitAction[] {
  return actions.filter(
    (action) =>
      (action.code === 'PublishDraw' || action.code === 'ApplyDraw') &&
      action.stageId === stageId &&
      action.drawId === drawId,
  )
}

/** Stage-scoped ops not tied to a single draw row (materialize / schedule). */
export function stageWideOperationalActions(actions: CockpitAction[]): CockpitAction[] {
  return actions.filter((action) =>
    action.code === 'MaterializeMatches' ||
    action.code === 'MaterializeFromOccupiedSlots' ||
    action.code === 'GenerateNextRound' ||
    action.code === 'GenerateSchedule' ||
    action.code === 'ApplySchedule' ||
    action.code === 'ApplyProgression' ||
    action.code === 'ApplyQualification',
  )
}

/**
 * Closure presentation from cycle + closureHint only.
 * Construction + not completable → hidden (not a pilotage topic).
 */
export type ClosurePresentation = 'full' | 'condensed' | 'hidden'

export function closurePresentation(view: CockpitView): ClosurePresentation {
  const cycle = view.cycleReading.code
  const hint = view.closureHint

  if (hint.canCompleteNormally) {
    return 'full'
  }

  if (cycle === 'Construction') {
    return 'hidden'
  }

  if (cycle === 'InProgress' || cycle === 'Completed' || cycle === 'Archived') {
    return hint.blockerCodes.length > 0 || cycle === 'Completed' || cycle === 'Archived'
      ? 'full'
      : 'condensed'
  }

  return 'condensed'
}

export type OperationalBlock = 'stages' | 'draws' | 'counts'

/**
 * Which operational blocks to show — cycle + empty data (presentation only).
 */
export function operationalBlocks(view: CockpitView): OperationalBlock[] {
  const cycle = view.cycleReading.code
  const focus = view.operationalFocus
  const blocks: OperationalBlock[] = []

  if (focus.stages.length > 0) {
    blocks.push('stages')
  }

  if (focus.draws.length > 0) {
    blocks.push('draws')
  }

  const showMatchOps =
    cycle === 'InProgress' ||
    cycle === 'Completed' ||
    cycle === 'Archived' ||
    focus.matchCounts.total > 0

  if (showMatchOps && focus.matchCounts.total > 0) {
    blocks.push('counts')
  }

  return blocks
}

export function shouldShowOperationalSection(view: CockpitView): boolean {
  return operationalBlocks(view).length > 0
}

/** Prefer Blocking situations first for pilotage order (stable within nature). */
export function orderSituationsForDisplay(
  situations: CockpitSituation[],
): CockpitSituation[] {
  return [...situations].sort((a, b) => {
    const rank = (nature: string) => (nature === 'Blocking' ? 0 : 1)
    return rank(a.nature) - rank(b.nature)
  })
}

export function panelProminenceClass(prominence: string): string {
  if (isProminenceDominant(prominence)) {
    return 'ds-panel overview-dimension--dominant'
  }
  if (isProminenceCondensed(prominence)) {
    return 'ds-panel overview-dimension--condensed'
  }
  return 'ds-panel'
}

/** @deprecated Use panelProminenceClass — legacy card classes during migration. */
export function cardProminenceClass(prominence: string): string {
  return panelProminenceClass(prominence)
}

export type { CockpitProminence }
