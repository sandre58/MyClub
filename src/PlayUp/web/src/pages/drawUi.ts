import type {
  StageDraw,
  StageDrawGroupPlacement,
  StageFixture,
  StageRound,
  StageSlot,
} from '../types';

export type GroupPlacementEntry = {
  entryId: string;
  displayName: string;
};

export type GroupPlacementRow = {
  groupId: string;
  groupLabel: string;
  entries: GroupPlacementEntry[];
};

/** Collapse flat groupPlacements into one row per group (list density, not nested cards). */
export function groupPlacementRows(
  placements: StageDrawGroupPlacement[],
  unknownEntry: string,
  unknownGroup: string,
): GroupPlacementRow[] {
  const byGroup = new Map<string, GroupPlacementRow>();
  for (const placement of placements) {
    let row = byGroup.get(placement.groupId);
    if (!row) {
      row = {
        groupId: placement.groupId,
        groupLabel: placement.groupDisplayName?.trim() || unknownGroup,
        entries: [],
      };
      byGroup.set(placement.groupId, row);
    }
    row.entries.push({
      entryId: placement.entryId,
      displayName: placement.displayName?.trim() || unknownEntry,
    });
  }
  return [...byGroup.values()];
}

/**
 * Why « Nouveau tirage » (G2) cannot run — mirrors Domain / DrawInputsFactory fail-closed
 * checks the SPA can see without calling Generate (avoids 400 + orphan Draft).
 */
export type DrawCreateBlockReason =
  | 'unsupported'
  | 'active'
  | 'emptyPool'
  | 'emptyPoolUpstream'
  | 'oddPool'
  | 'missingPots'
  | 'groupShape';

export type DrawCreateGate =
  | { ok: true }
  | { ok: false; reason: DrawCreateBlockReason };

/**
 * Client gate for Create+Generate. Encoding F: pool = CompositionEntries only.
 * Pairing: even pool. Group: pots ≥ 2 and entries = pots × groupCount.
 */
export function resolveDrawCreateGate(input: {
  kind: 'Group' | 'Pairing' | 'Slot' | null;
  hasActiveDraw: boolean;
  compositionEntryCount: number;
  /** False / undefined when the phase is fed by Qual/Prog (teams arrive from upstream). */
  isRootComposition?: boolean;
  numberOfPots: number | null | undefined;
  groupCount: number | null | undefined;
}): DrawCreateGate {
  if (input.kind == null) {
    return { ok: false, reason: 'unsupported' };
  }
  if (input.hasActiveDraw) {
    return { ok: false, reason: 'active' };
  }

  const pool = input.compositionEntryCount;
  if (pool <= 0) {
    return {
      ok: false,
      reason:
        input.isRootComposition === false ? 'emptyPoolUpstream' : 'emptyPool',
    };
  }

  if (input.kind === 'Pairing') {
    return pool % 2 === 0
      ? { ok: true }
      : { ok: false, reason: 'oddPool' };
  }

  if (input.kind === 'Group') {
    const pots = input.numberOfPots ?? null;
    const groups = input.groupCount ?? 0;
    if (pots == null || pots < 2) {
      return { ok: false, reason: 'missingPots' };
    }
    if (groups < 1 || pool !== pots * groups) {
      return { ok: false, reason: 'groupShape' };
    }
    return { ok: true };
  }

  // Slot — Structural V1 Cup uses Pairing; keep pool non-empty only.
  return { ok: true };
}

/**
 * Synthetic chrome flags — projection keeps three axes; UI does not teach the matrix
 * via three systematic badges (PublishAndApply decision).
 */
export type DrawChromeFlags = {
  showStatus: boolean;
  showResolution: boolean;
  showApplied: boolean;
};

/**
 * UI-only projection of DrawStatus × DrawResolutionState (+ derived Applied).
 * Status colours come from the shared badge tones (see ui.tsx).
 */
export type DrawUiProjection = {
  /** i18n key under the `draw` namespace (SPA owns copy). */
  messageKey: string;
  showResults: boolean;
  isApplied: boolean;
  chrome: DrawChromeFlags;
};

/** Topology badge states — mirrors server StructureDrawExecutionBadge. */
export type TopologyDrawExecutionBadge =
  | 'ToLaunch'
  | 'InProgress'
  | 'ToApply'
  | 'Applied';

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
 * Client mirror of StructureViewAssembler.ResolveDrawExecutionBadge.
 * Prefer server `stage.drawExecutionBadge` on the hub; use this for unit tests / SPA-only paths.
 */
export function resolveTopologyDrawExecutionBadge(
  hasDrawRules: boolean,
  draw: StageDraw | null,
  slots: StageSlot[],
  rounds: StageRound[] = [],
): TopologyDrawExecutionBadge | null {
  if (!hasDrawRules) {
    return null;
  }
  if (!draw) {
    return 'ToLaunch';
  }
  if (draw.status === 'Draft') {
    return 'InProgress';
  }
  if (draw.status === 'Published') {
    const applied = getDrawUiProjection(draw, slots, rounds).isApplied;
    return applied ? 'Applied' : 'ToApply';
  }
  return 'ToLaunch';
}

/**
 * Pure projection: server enums → message key + flags for conditional rendering.
 * Not a Domain state machine — only helps the component avoid nested if spaghetti.
 *
 * Prefer server `draw.isApplied` (DrawAppliedState) when present; else Slot/Pairing heuristics.
 */
export function getDrawUiProjection(
  draw: StageDraw,
  slots: StageSlot[],
  rounds: StageRound[] = [],
): DrawUiProjection {
  const isApplied =
    draw.resolutionState === 'Resolved' &&
    (typeof draw.isApplied === 'boolean'
      ? draw.isApplied
      : (draw.kind === 'Slot' && isSlotDrawApplied(draw, slots)) ||
        (draw.kind === 'Pairing' && isPairingDrawApplied(draw, rounds)));

  if (draw.status === 'Cancelled') {
    return {
      messageKey: 'cancelled',
      showResults: draw.resolutionState === 'Resolved',
      isApplied: false,
      chrome: { showStatus: true, showResolution: false, showApplied: false },
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NotResolved') {
    return {
      messageKey: 'draftNotResolved',
      showResults: false,
      isApplied: false,
      chrome: { showStatus: true, showResolution: true, showApplied: false },
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'Resolved') {
    return {
      messageKey: 'draftResolved',
      showResults: true,
      isApplied: false,
      // Brouillon · Résolu — phrase carries “ready to apply”
      chrome: { showStatus: true, showResolution: true, showApplied: false },
    };
  }

  if (draw.status === 'Draft' && draw.resolutionState === 'NoSolution') {
    return {
      messageKey: 'noSolution',
      showResults: false,
      isApplied: false,
      chrome: { showStatus: true, showResolution: true, showApplied: false },
    };
  }

  if (draw.status === 'Published' && draw.resolutionState === 'Resolved') {
    let messageKey = 'published';
    if (isApplied && draw.kind === 'Slot') {
      messageKey = 'publishedSlotApplied';
    } else if (isApplied && draw.kind === 'Group') {
      messageKey = 'publishedGroupApplied';
    } else if (isApplied && draw.kind === 'Pairing') {
      messageKey = 'publishedPairingApplied';
    }

    return {
      messageKey,
      showResults: true,
      isApplied,
      // Publié · (Appliqué when done) — no “Non appliqué” badge on happy recovery path
      chrome: {
        showStatus: true,
        showResolution: false,
        showApplied: isApplied,
      },
    };
  }

  return {
    messageKey: 'fallback',
    showResults: draw.resolutionState === 'Resolved',
    isApplied: false,
    chrome: { showStatus: true, showResolution: true, showApplied: false },
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
