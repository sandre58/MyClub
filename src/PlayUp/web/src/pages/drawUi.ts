import type { StageDraw, StageFixture, StageRound, StageSlot } from '../types';

/**
 * UI-only projection of DrawStatus × DrawResolutionState (+ derived Applied).
 * Status colours come from the shared badge tones (see ui.tsx).
 */
export type DrawUiProjection = {
  /** i18n key under the `draw` namespace (SPA owns copy). */
  messageKey: string;
  showResults: boolean;
  isApplied: boolean;
};

/**
 * Single primary summary for the Structure Tirage tile (not a lifecycle strip).
 * Priority: Applied > Published > Resolved > Draft (NoSolution stays distinct).
 */
export type DrawTilePrimarySummary =
  | 'applied'
  | 'published'
  | 'resolved'
  | 'draft'
  | 'noSolution';

/**
 * Current non-cancelled draw for Structure chrome (newest first).
 * Cancelled-only history → null (capacity / config, not engagement).
 */
export function pickActiveDraw(draws: StageDraw[]): StageDraw | null {
  if (draws.length === 0) {
    return null;
  }
  const newestFirst = [...draws].reverse();
  return newestFirst.find((d) => d.status !== 'Cancelled') ?? null;
}

/** Default selection id for the Tirage dialog (prefers active, else newest). */
export function pickDefaultDrawId(draws: StageDraw[]): string | null {
  if (draws.length === 0) {
    return null;
  }
  const newestFirst = [...draws].reverse();
  const active = newestFirst.find((d) => d.status !== 'Cancelled');
  return (active ?? newestFirst[0])?.id ?? null;
}

/**
 * One product-facing summary for the Tirage tile — never “effectué”.
 */
export function getDrawTilePrimarySummary(
  draw: StageDraw,
  slots: StageSlot[],
  rounds: StageRound[] = [],
): DrawTilePrimarySummary {
  const projection = getDrawUiProjection(draw, slots, rounds);
  if (projection.isApplied) {
    return 'applied';
  }
  if (draw.status === 'Published') {
    return 'published';
  }
  if (draw.resolutionState === 'Resolved') {
    return 'resolved';
  }
  if (draw.resolutionState === 'NoSolution') {
    return 'noSolution';
  }
  return 'draft';
}

/**
 * Pure projection: server enums → message key + flags for conditional rendering.
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
      (draw.kind === 'Pairing' && isPairingDrawApplied(draw, rounds)));

  if (draw.status === 'Cancelled') {
    return {
      messageKey: 'cancelled',
      showResults: draw.resolutionState === 'Resolved',
      isApplied: false,
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NotResolved') {
    return {
      messageKey: 'draftNotResolved',
      showResults: false,
      isApplied: false,
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'Resolved') {
    return {
      messageKey: 'draftResolved',
      showResults: true,
      isApplied: false,
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NoSolution') {
    return {
      messageKey: 'noSolution',
      showResults: false,
      isApplied: false,
    };
  }

  if (draw.status === 'Published' && draw.resolutionState === 'Resolved') {
    let messageKey = 'published';
    if (isApplied && draw.kind === 'Slot') {
      messageKey = 'publishedSlotApplied';
    } else if (isApplied && draw.kind === 'Pairing') {
      messageKey = 'publishedPairingApplied';
    }

    return {
      messageKey,
      showResults: true,
      isApplied,
    };
  }

  return {
    messageKey: 'fallback',
    showResults: draw.resolutionState === 'Resolved',
    isApplied: false,
  };
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
    return false;
  }

  if (draw.slotPlacements.length === 0) {
    return false;
  }

  const byKey = new Map(slots.map((slot) => [slot.slotKey, slot]));

  return draw.slotPlacements.every((placement) => {
    const slot = byKey.get(placement.slotKey);
    return slot?.entryId != null && slot.entryId === placement.entryId;
  });
}

/**
 * Fixtures in StageOverview order: rounds then fixtures within each round.
 * Host/repository reorder by SortOrder on load — this is the stable 1:1 source for Pairing Apply.
 */
export function listStageFixturesInOrder(rounds: StageRound[]): StageFixture[] {
  return rounds.flatMap((round) => round.fixtures);
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
    return null;
  }

  const fixtures = listStageFixturesInOrder(rounds);
  if (
    draw.pairings.length === 0 ||
    fixtures.length === 0 ||
    draw.pairings.length !== fixtures.length
  ) {
    return null;
  }

  return fixtures.map((fixture) => fixture.id);
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
  const fixtureIds = resolvePairingFixtureIds(draw, rounds);
  if (fixtureIds === null) {
    return false;
  }

  const fixtures = listStageFixturesInOrder(rounds);
  return fixtures.every((fixture) => fixture.attachments.length > 0);
}
