import type { CockpitCycleCode } from '../types'

/** UI cycle phases (20.0 vernacular) — typographic line, not a stepper. */
export const UI_CYCLE_PHASES = [
  'Preparation',
  'Calendar',
  'InProgress',
  'Completed',
] as const

export type UiCyclePhase = (typeof UI_CYCLE_PHASES)[number]

/**
 * Maps Read cycle code → active UI phase index.
 * Calendrier awaits an explicit Read signal — not inferred in React.
 */
export function activeUiCyclePhaseIndex(code: CockpitCycleCode | string): number {
  switch (code) {
    case 'Construction':
      return 0
    case 'InProgress':
      return 2
    case 'Completed':
    case 'Archived':
      return 3
    default:
      return 0
  }
}

/** Shell header pill + inline labels — keyed by Read cycle code. */
export function cycleUiLabelKey(code: CockpitCycleCode | string): string {
  return `cycleUi.${code}`
}
