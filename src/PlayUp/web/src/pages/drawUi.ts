import type { StageDraw, StageFixture, StageRound, StageSlot } from '../types'

/** UI-only projection of DrawStatus × DrawResolutionState (+ derived Applied). */
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
  rounds: StageRound[] = [],
): DrawUiProjection {
  const isApplied =
    draw.resolutionState === 'Resolved' &&
    ((draw.kind === 'Slot' && isSlotDrawApplied(draw, slots)) ||
      (draw.kind === 'Pairing' && isPairingDrawApplied(draw, rounds)))

  const statusTone =
    draw.status === 'Cancelled'
      ? 'other'
      : draw.status === 'Published'
        ? 'live'
        : 'neutral'

  if (draw.status === 'Cancelled') {
    return {
      message:
        'This draw was cancelled. A new draw is required to run again.',
      showResults: draw.resolutionState === 'Resolved',
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NotResolved') {
    return {
      message: 'Draw in preparation — no result yet.',
      showResults: false,
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'Resolved') {
    return {
      message: 'Draw resolved but not published.',
      showResults: true,
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NoSolution') {
    return {
      message: 'No admissible solution was found for this draw.',
      showResults: false,
      isApplied: false,
      statusTone,
    }
  }

  if (draw.status === 'Published' && draw.resolutionState === 'Resolved') {
    let appliedMessage = 'Published draw.'
    if (isApplied && draw.kind === 'Slot') {
      appliedMessage =
        'Published draw. Placements match the current stage slots.'
    } else if (isApplied && draw.kind === 'Pairing') {
      appliedMessage =
        'Published draw. Target fixtures already have attached matches.'
    }

    return {
      message: appliedMessage,
      showResults: true,
      isApplied,
      statusTone,
    }
  }

  return {
    message: 'Draw status is available below.',
    showResults: draw.resolutionState === 'Resolved',
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
  if (draw.kind !== 'Slot' || draw.resolutionState !== 'Resolved') {
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

/**
 * Fixtures in StageOverview order: rounds then fixtures within each round.
 * Host/repository reorder by SortOrder on load — this is the stable 1:1 source for Pairing Apply.
 */
export function listStageFixturesInOrder(
  rounds: StageRound[],
): StageFixture[] {
  return rounds.flatMap((round) => round.fixtures)
}

/**
 * Strict automap: pairing[i] → fixture[i] only when counts match and both > 0.
 * Returns null when the UI must block Apply (ambiguous / incomplete mapping).
 */
export function resolvePairingFixtureIds(
  draw: StageDraw,
  rounds: StageRound[],
): string[] | null {
  if (draw.kind !== 'Pairing' || draw.resolutionState !== 'Resolved') {
    return null
  }

  const fixtures = listStageFixturesInOrder(rounds)
  if (
    draw.pairings.length === 0 ||
    fixtures.length === 0 ||
    draw.pairings.length !== fixtures.length
  ) {
    return null
  }

  return fixtures.map((fixture) => fixture.id)
}

/**
 * Lightweight Pairing “applied” heuristic from StageOverview only:
 * same count as automap + every target fixture already has at least one attachment.
 * Does not verify entry identities (would need the matches query).
 */
export function isPairingDrawApplied(
  draw: StageDraw,
  rounds: StageRound[],
): boolean {
  const fixtureIds = resolvePairingFixtureIds(draw, rounds)
  if (fixtureIds === null) {
    return false
  }

  const fixtures = listStageFixturesInOrder(rounds)
  return fixtures.every((fixture) => fixture.attachments.length > 0)
}
