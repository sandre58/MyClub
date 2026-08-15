import type { StageDraw, StageSlot } from '../types'

/** UI-only projection of DrawStatus × DrawResolutionState (+ derived Applied for Slot). */
export type DrawUiProjection = {
  message: string
  showResults: boolean
  isApplied: boolean
  statusTone: 'neutral' | 'live' | 'finished' | 'other'
}

/**
 * Pure projection: server enums → copy + flags for conditional rendering.
 * Not a Domain state machine — only helps the component avoid nested if spaghetti.
 *
 * Domain state = authoritative rules in .NET.
 * Server state = StageOverview in TanStack Query.
 * Derived UI = values computed during render from that snapshot.
 */
export function getDrawUiProjection(
  draw: StageDraw,
  slots: StageSlot[],
): DrawUiProjection {
  const isApplied =
    draw.kind === 0 &&
    draw.resolutionState === 1 &&
    isSlotDrawApplied(draw, slots)

  const statusTone =
    draw.status === 2
      ? 'other'
      : draw.status === 1
        ? 'live'
        : 'neutral'

  if (draw.status === 2) {
    return {
      message:
        'This draw was cancelled. A new draw is required to run again.',
      showResults: draw.resolutionState === 1,
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 0 && draw.resolutionState === 0) {
    return {
      message: 'Draw in preparation — no result yet.',
      showResults: false,
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 0 && draw.resolutionState === 1) {
    return {
      message: 'Draw resolved but not published.',
      showResults: true,
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 0 && draw.resolutionState === 2) {
    return {
      message: 'No admissible solution was found for this draw.',
      showResults: false,
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 1 && draw.resolutionState === 1) {
    return {
      message: isApplied
        ? 'Published draw. Placements match the current stage slots.'
        : 'Published draw.',
      showResults: true,
      isApplied,
      statusTone,
    }
  }

  return {
    message: 'Draw status is available below.',
    showResults: draw.resolutionState === 1,
    isApplied: false,
    statusTone,
  }
}

/**
 * Derived UI (Slot only): placements already occupy the matching stage slots.
 * Compares entry identities from the same StageOverview — not a Domain status.
 */
export function isSlotDrawApplied(
  draw: StageDraw,
  slots: StageSlot[],
): boolean {
  if (draw.kind !== 0 || draw.resolutionState !== 1) {
    return false
  }

  if (draw.slotPlacements.length === 0) {
    return false
  }

  const byKey = new Map(slots.map((slot) => [slot.slotKey, slot]))

  return draw.slotPlacements.every((placement) => {
    const slot = byKey.get(placement.slotKey)
    return slot?.entryId != null && slot.entryId === placement.entryId
  })
}
