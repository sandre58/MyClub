import {
  applyDraw,
  applyProgressionOutcome,
  applyQualification,
  archiveCompetition,
  completeCompetition,
  materializeMatches,
  prepareCompetition,
  prepareStage,
  publishDraw,
  startCompetition,
  startMatch,
  startStage,
} from '../api'
import type { CockpitAction, CockpitDrawFocus, CockpitView } from '../types'

/**
 * Outcome of a Cockpit action click.
 * - execute: call an existing Host command, then invalidate cockpit
 * - navigate: open a specialized space (params insufficient for a safe POST)
 * - unsupported: show as label only (should not happen for projected actions)
 */
export type CockpitActionIntent =
  | { kind: 'execute'; run: () => Promise<unknown> }
  | { kind: 'navigate'; to: string }
  | { kind: 'unsupported' }

/**
 * Map a backend-projected action to execute vs navigate.
 * Availability is decided by the Read; this only chooses how to carry it out.
 */
export function resolveCockpitActionIntent(
  action: CockpitAction,
  view: CockpitView,
): CockpitActionIntent {
  const competitionId = view.competitionId
  const stageId = action.stageId ?? undefined
  const drawId = action.drawId ?? undefined
  const matchId = action.matchId ?? undefined
  const fixtureId = action.fixtureId ?? undefined

  switch (action.code) {
    case 'PrepareStage':
      return stageId
        ? { kind: 'execute', run: () => prepareStage(stageId) }
        : { kind: 'unsupported' }

    case 'StartStage':
      return stageId
        ? { kind: 'execute', run: () => startStage(stageId) }
        : { kind: 'unsupported' }

    case 'PublishDraw':
      return stageId && drawId
        ? { kind: 'execute', run: () => publishDraw(stageId, drawId) }
        : stageId
          ? { kind: 'navigate', to: `/stages/${stageId}` }
          : { kind: 'unsupported' }

    case 'ApplyDraw': {
      if (!stageId || !drawId) {
        return stageId
          ? { kind: 'navigate', to: `/stages/${stageId}` }
          : { kind: 'unsupported' }
      }
      const draw = findDraw(view, stageId, drawId)
      // Pairing Apply needs ordered fixture ids — Stage page owns that mapping.
      if (draw?.kind === 'Pairing') {
        return { kind: 'navigate', to: `/stages/${stageId}` }
      }
      return {
        kind: 'execute',
        run: () => applyDraw(stageId, drawId, { fixtureIds: [] }),
      }
    }

    case 'MaterializeMatches':
      return stageId
        ? { kind: 'execute', run: () => materializeMatches(stageId) }
        : { kind: 'unsupported' }

    case 'MaterializeFromOccupiedSlots':
      return stageId
        ? { kind: 'navigate', to: `/stages/${stageId}` }
        : { kind: 'unsupported' }

    case 'StartMatch':
      return matchId
        ? { kind: 'execute', run: () => startMatch(matchId) }
        : { kind: 'unsupported' }

    case 'FinishMatch':
      return matchId
        ? { kind: 'navigate', to: `/matches/${matchId}` }
        : { kind: 'unsupported' }

    case 'ApplyProgression':
      return stageId && fixtureId
        ? {
            kind: 'execute',
            run: () => applyProgressionOutcome(stageId, fixtureId),
          }
        : matchId
          ? { kind: 'navigate', to: `/matches/${matchId}` }
          : stageId
            ? { kind: 'navigate', to: `/stages/${stageId}` }
            : { kind: 'unsupported' }

    case 'ApplyQualification':
      return stageId
        ? { kind: 'execute', run: () => applyQualification(stageId) }
        : { kind: 'unsupported' }

    case 'PrepareCompetition':
      return {
        kind: 'execute',
        run: () => prepareCompetition(competitionId),
      }

    case 'StartCompetition':
      return {
        kind: 'execute',
        run: () => startCompetition(competitionId),
      }

    case 'CompleteCompetition':
      return {
        kind: 'execute',
        run: () => completeCompetition(competitionId, 'Normal'),
      }

    case 'ArchiveCompetition':
      return {
        kind: 'execute',
        run: () => archiveCompetition(competitionId),
      }

    case 'AddEntry':
    case 'ConfigureStructure':
    case 'ReplaceRegulation':
    case 'RenameEntry':
    case 'WithdrawEntry':
    case 'ExcludeEntry':
    case 'ContinueOrganisation':
      return {
        kind: 'navigate',
        to: `/competitions/${competitionId}/organisation`,
      }

    case 'GenerateSchedule':
    case 'ApplySchedule':
      return stageId
        ? { kind: 'navigate', to: `/stages/${stageId}` }
        : { kind: 'navigate', to: `/competitions/${competitionId}/matches` }

    case 'OpenMatches':
      return {
        kind: 'navigate',
        to: `/competitions/${competitionId}/matches`,
      }

    case 'OpenConsultation':
      return {
        kind: 'navigate',
        to: `/competitions/${competitionId}/classements`,
      }

    default:
      return { kind: 'unsupported' }
  }
}

function findDraw(
  view: CockpitView,
  stageId: string,
  drawId: string,
): CockpitDrawFocus | undefined {
  return view.operationalFocus.draws.find(
    (draw) => draw.stageId === stageId && draw.drawId === drawId,
  )
}

/** Stable React key for an action row (code + optional ids). */
export function cockpitActionKey(action: CockpitAction): string {
  return [
    action.code,
    action.stageId ?? '',
    action.drawId ?? '',
    action.matchId ?? '',
    action.fixtureId ?? '',
  ].join(':')
}
